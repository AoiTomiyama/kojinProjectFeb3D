using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class UpgradeCatalogTests
{
    private const string CatalogPath = "Assets/GameData/Progression/InGameUpgrades.asset";
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
    private static T Field<T>(object target, string field) => (T)target.GetType().GetField(field, Private).GetValue(target);

    [Test]
    public void CatalogRetainsAllElevenAuthoredEffectsAndStableIds()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<UpgradeCatalogConfiguration>(CatalogPath);
        var items = catalog.CreateSnapshot();
        Assert.AreEqual(11, items.Length);
        CollectionAssert.AreEquivalent(Enumerable.Range(1, 11), items.Select(item => item.Id));
        // 移行前の11ボタンから読み取った効果を照合する。表記と効果が異なる既存値も変更しない。
        float[] damage = { 1, 1, 1, 1.5f, .5f, 2.5f, 1, .9f, .5f, 2, .7f };
        float[] cooldown = { 1, 1.3f, 1, 1.8f, .7f, 1, 1, .9f, 1, 1, 1 };
        float[] health = { 1, 1, 1, 1, 1, .5f, 1.3f, 1, 2, 1, 1 };
        for (int i = 0; i < items.Length; i++)
        {
            Assert.AreEqual(damage[i], items[i].Effect.DamageMultiply);
            Assert.AreEqual(cooldown[i], items[i].Effect.CoolTimeMultiply);
            Assert.AreEqual(health[i], items[i].Effect.MaxHealthMultiply);
            Assert.IsTrue(items[i].IsEligible(0));
        }
        Assert.AreEqual(3, items[0].Effect.RicochetAdd);
        Assert.AreEqual(.7f, items[0].Effect.SpeedMultiply);
        Assert.AreEqual(1.6f, items[1].Effect.SpeedMultiply);
        Assert.AreEqual(.75f, items[2].Effect.ReloadTimeMultiply);
        Assert.AreEqual(10, items[4].Effect.MaxAmmoSizeAdd);
        Assert.AreEqual(1.3f, items[7].Effect.SpeedAdd);
        Assert.AreEqual(-3, items[9].Effect.MaxAmmoSizeAdd);
        Assert.AreEqual(2, items[10].Effect.MaxAmmoSizeAdd);
        Assert.AreEqual(2, items[10].Effect.SyncBulletAdd);
        items[0] = null;
        Assert.IsNotNull(catalog.CreateSnapshot()[0]);
    }

    [Test]
    public void CatalogRejectsInvalidReferencesAndIdsAndFiltersConditions()
    {
        var catalog = ScriptableObject.CreateInstance<UpgradeCatalogConfiguration>();
        var definition = ScriptableObject.CreateInstance<UpgradeDefinitionConfiguration>();
        try
        {
            Set(catalog, "_definitions", new[] { definition });
            Assert.Throws<InvalidOperationException>(() => catalog.CreateSnapshot());
            Set(definition, "_id", 42);
            Set(definition, "_minimumLevel", 2);
            Assert.IsFalse(definition.IsEligible(1));
            Assert.IsTrue(definition.IsEligible(2));
            Set(definition, "_available", false);
            Assert.IsFalse(definition.IsEligible(31));
            Set(catalog, "_definitions", new[] { definition, definition });
            Assert.Throws<InvalidOperationException>(() => catalog.CreateSnapshot());
            Set(catalog, "_definitions", new UpgradeDefinitionConfiguration[] { null });
            Assert.Throws<InvalidOperationException>(() => catalog.CreateSnapshot());
        }
        finally { Object.DestroyImmediate(catalog); Object.DestroyImmediate(definition); }
    }

    [UnityTest]
    public IEnumerator CatalogAdditionNeedsNoSceneButtonAndRebuildKeepsSelectionAndState()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
        foreach (var attack in Object.FindObjectsOfType<EnemyAttackGameplay>()) attack.enabled = false;
        foreach (var move in Object.FindObjectsOfType<PlayerMoveGameplay>()) move.enabled = false;
        foreach (var attack in Object.FindObjectsOfType<PlayerAttackGameplay>()) attack.enabled = false;
        var authoredManager = Object.FindObjectOfType<LevelUpCoordinatorGameplay>();
        var authoredView = authoredManager.GetComponent<LevelUpViewPresentation>();
        Assert.AreEqual(0, Field<VerticalLayoutGroup>(authoredView, "_buttonLayoutGroup").transform.childCount);
        var catalog = Object.Instantiate(AssetDatabase.LoadAssetAtPath<UpgradeCatalogConfiguration>(CatalogPath));
        var originalItems = catalog.CreateSnapshot();
        var extra = Object.Instantiate(originalItems[0]);
        Set(extra, "_id", 100);
        Set(extra, "_title", "追加テスト");
        Set(extra, "_minimumLevel", 2);
        Set(extra, "_effect", new UpgradeParametersConfiguration
        {
            MaxHealthAdd = 10, MaxHealthMultiply = 1, MoveSpeedMultiply = 1,
            DamageMultiply = 1, SpeedMultiply = 1, CoolTimeMultiply = 1, ReloadTimeMultiply = 1
        });
        Set(catalog, "_definitions", originalItems.Concat(new[] { extra }).ToArray());
        Set(authoredManager, "_catalog", catalog);
        string catalogJson = EditorJsonUtility.ToJson(catalog);
        string extraJson = EditorJsonUtility.ToJson(extra);
        yield return new EnterPlayMode();
        yield return null;
        var manager = Object.FindObjectOfType<LevelUpCoordinatorGameplay>();
        var view = manager.GetComponent<LevelUpViewPresentation>();
        Assert.AreEqual(12, Field<Button[]>(view, "_buttons").Length);
        Assert.IsFalse(Field<int[]>(manager, "_candidateIds").Contains(100));
        manager.GainExperience(int.MaxValue);
        manager.GainExperience(0);
        manager.GainExperience(0);
        manager.TryReroll();
        Assert.IsTrue(Field<int[]>(manager, "_candidateIds").Contains(100));
        Set(manager, "_selectedIds", new[] { 100, 1, 2 });
        view.ShowCandidates(new[] { 100, 1, 2 });
        manager.OpenCloseUpgradeMenu();
        int picks = manager.PickCount, tokens = manager.RerollToken;
        var old = Field<Button[]>(view, "_buttons").First(button => button.GetComponent<UpgradeButtonPresentation>().Definition.Id == 100);
        for (int i = 0; i < 3; i++) manager.RefreshUpgradeButtons();
        Assert.AreEqual(picks, manager.PickCount);
        Assert.AreEqual(tokens, manager.RerollToken);
        Assert.AreEqual(12, Field<VerticalLayoutGroup>(view, "_buttonLayoutGroup").transform.childCount);
        old.onClick.Invoke();
        Assert.AreEqual(picks, manager.PickCount, "旧ボタンは接続解除済み");
        var chosen = Field<Button[]>(view, "_buttons").First(button => button.GetComponent<UpgradeButtonPresentation>().Definition.Id == 100);
        Assert.IsTrue(chosen.GetComponentsInChildren<TextMeshProUGUI>().Any(label => label.text == extra.Title));
        view.gameObject.SetActive(false);
        view.gameObject.SetActive(true);
        yield return null;
        Assert.AreEqual(3, Field<Button[]>(view, "_buttons").Count(button => button.gameObject.activeSelf));
        int health = Object.FindObjectOfType<PlayerCoreGameplay>().MaxHealth;
        chosen.onClick.Invoke();
        Assert.AreEqual(picks - 1, manager.PickCount, "再生成後もクリック1回で選択1回");
        Assert.AreEqual(health + 10, Object.FindObjectOfType<PlayerCoreGameplay>().MaxHealth);
        Assert.AreEqual(catalogJson, EditorJsonUtility.ToJson(catalog));
        Assert.AreEqual(extraJson, EditorJsonUtility.ToJson(extra));
        LogAssert.NoUnexpectedReceived();
        yield return new ExitPlayMode();
        Object.DestroyImmediate(catalog);
        Object.DestroyImmediate(extra);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (EditorApplication.isPlaying) yield return new ExitPlayMode();
        EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
    }

    [UnityTest]
    public IEnumerator RepresentativeUpgradeMenuRendersWithPrefabStyle()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
        foreach (var attack in Object.FindObjectsOfType<EnemyAttackGameplay>()) attack.enabled = false;
        foreach (var move in Object.FindObjectsOfType<PlayerMoveGameplay>()) move.enabled = false;
        foreach (var attack in Object.FindObjectsOfType<PlayerAttackGameplay>()) attack.enabled = false;
        yield return new EnterPlayMode();
        yield return null;
        var manager = Object.FindObjectOfType<LevelUpCoordinatorGameplay>();
        var view = manager.GetComponent<LevelUpViewPresentation>();
        view.ShowCandidates(new[] { 9, 5, 11 });
        manager.OpenCloseUpgradeMenu();
        var canvas = Field<Image>(view, "_upgradePanel").GetComponentInParent<Canvas>();
        var camera = Object.FindObjectOfType<Camera>();
        var target = new RenderTexture(1280, 720, 24);
        var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        var previousTarget = RenderTexture.active;
        var previousCameraTarget = camera.targetTexture;
        var previousMode = canvas.renderMode;
        var previousCamera = canvas.worldCamera;
        try
        {
            // 画面なし検証でもOverlayを実カメラへ接続し、文字と生成列をGPUで描画する。
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            pixels.Apply();
            Assert.That(pixels.GetPixels().Select(color => color.grayscale).Max() - pixels.GetPixels().Select(color => color.grayscale).Min(), Is.GreaterThan(.1f));
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "kojin-upgrade-menu.png"), pixels.EncodeToPNG());
            var template = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/UpgradeButton.prefab");
            foreach (var button in Field<Button[]>(view, "_buttons").Where(button => button.gameObject.activeSelf))
            {
                Assert.AreEqual(template.GetComponent<RectTransform>().sizeDelta, button.GetComponent<RectTransform>().sizeDelta);
                var definition = button.GetComponent<UpgradeButtonPresentation>().Definition;
                var texts = button.GetComponentsInChildren<TextMeshProUGUI>();
                Assert.IsTrue(texts.Any(text => text.text == definition.Title));
                foreach (var column in definition.AdditionalColumns) Assert.IsTrue(texts.Any(text => text.text == column.Label));
            }
        }
        finally
        {
            RenderTexture.active = previousTarget;
            camera.targetTexture = previousCameraTarget;
            canvas.renderMode = previousMode;
            canvas.worldCamera = previousCamera;
            Object.DestroyImmediate(pixels);
            target.Release();
            Object.DestroyImmediate(target);
        }
        LogAssert.NoUnexpectedReceived();
        yield return new ExitPlayMode();
    }
}
