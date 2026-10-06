using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class SceneDependencyTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [Test]
    public void ResolverRejectsMissingAndDuplicateAndIgnoresInactiveAndOtherScenes()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
        var otherScene = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        Camera outside = null;
        var requester = new GameObject("Requester").transform;
        SceneManager.MoveGameObjectToScene(requester.gameObject, scene);
        try
        {
            var error = Assert.Throws<InvalidOperationException>(() => SceneReferenceResolverInfrastructure.RequireUnique<Camera>(requester));
            StringAssert.Contains("Requester", error.Message);
            StringAssert.Contains("Camera", error.Message);
            StringAssert.Contains("0 件", error.Message);
            outside = new GameObject("OtherSceneCamera").AddComponent<Camera>();
            SceneManager.MoveGameObjectToScene(outside.gameObject, otherScene);
            Assert.Throws<InvalidOperationException>(() => SceneReferenceResolverInfrastructure.RequireUnique<Camera>(requester));
            var camera = new GameObject("LocalCamera").AddComponent<Camera>();
            SceneManager.MoveGameObjectToScene(camera.gameObject, scene);
            camera.enabled = false;
            Assert.AreSame(camera, SceneReferenceResolverInfrastructure.RequireUnique<Camera>(requester));
            var duplicate = new GameObject("DuplicateCamera").AddComponent<Camera>();
            SceneManager.MoveGameObjectToScene(duplicate.gameObject, scene);
            error = Assert.Throws<InvalidOperationException>(() => SceneReferenceResolverInfrastructure.RequireUnique<Camera>(requester));
            StringAssert.Contains("2 件", error.Message);
            duplicate.gameObject.SetActive(false);
            Assert.AreSame(camera, SceneReferenceResolverInfrastructure.RequireUnique<Camera>(requester));
        }
        finally
        {
            if (outside != null) Object.DestroyImmediate(outside.gameObject);
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [TestCase(typeof(PlayerAttackGameplay), "_definition", "Awake")]
    [TestCase(typeof(EnemyAttackGameplay), "_definition", "Awake")]
    [TestCase(typeof(PlayerCoreGameplay), "_initialStats", "Awake")]
    [TestCase(typeof(PlayerAttackGameplay), "_muzzle", "Start")]
    [TestCase(typeof(EnemyAttackGameplay), "_muzzle", "Start")]
    [TestCase(typeof(BulletPoolInfrastructure), "_objectDatabase", "Start")]
    [TestCase(typeof(BulletPoolInfrastructure), "_soundEffects", "Start")]
    [TestCase(typeof(PlayerStatusViewPresentation), "_healthImage", "Start")]
    [TestCase(typeof(PlayerStatusViewPresentation), "_healthText", "Start")]
    [TestCase(typeof(PlayerStatusViewPresentation), "_ammoText", "Start")]
    [TestCase(typeof(PlayerStatusViewPresentation), "_reloadTimeImage", "Start")]
    [TestCase(typeof(PlayerStatusViewPresentation), "_coolDownTimeImage", "Start")]
    [TestCase(typeof(CameraFollowPresentation), "_target", "Start")]
    [TestCase(typeof(EnemyHealthViewPresentation), "_healthImage", "Start")]
    public void MissingSerializedDependencyReportsItsSource(Type type, string field, string method)
    {
        EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
        var owner = Object.FindObjectOfType(type);
        var info = type.GetField(field, Private);
        object original = info.GetValue(owner);
        try
        {
            info.SetValue(owner, null);
            var wrapper = Assert.Throws<TargetInvocationException>(() => type.GetMethod(method, Private).Invoke(owner, null));
            Assert.IsInstanceOf<InvalidOperationException>(wrapper.InnerException);
            StringAssert.Contains(type.Name + "." + field, wrapper.InnerException.Message);
            var disable = type.GetMethod("OnDisable", Private);
            if (disable != null) Assert.DoesNotThrow(() => disable.Invoke(owner, null));
        }
        finally { info.SetValue(owner, original); }
    }

    [TestCase("_expBar")]
    [TestCase("_levelText")]
    [TestCase("_killCountText")]
    [TestCase("_tokenCountText")]
    [TestCase("_pickUpgradeCountText")]
    [TestCase("_upgradePanel")]
    [TestCase("_hasPickupNotice")]
    [TestCase("_buttonLayoutGroup")]
    public void ProgressionViewReportsEachMissingDisplay(string field)
    {
        EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
        var view = Object.FindObjectOfType<LevelUpViewPresentation>();
        var info = typeof(LevelUpViewPresentation).GetField(field, Private);
        var original = info.GetValue(view);
        try
        {
            info.SetValue(view, null);
            var error = Assert.Throws<InvalidOperationException>(() => view.Initialize(false, 0, 0, 0, 0, 0));
            StringAssert.Contains(nameof(LevelUpViewPresentation) + "." + field, error.Message);
        }
        finally { info.SetValue(view, original); }
    }

    [Test]
    public void BulletRejectsMissingAudioBeforeStartingLifetime()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/PlayerBullet.prefab");
        var instance = Object.Instantiate(prefab);
        try
        {
            var bullet = instance.GetComponent<BulletShotGameplay>();
            bullet.OnInitialize();
            Assert.Throws<ArgumentNullException>(() => bullet.SetAudioSource(null));
            var error = Assert.Throws<InvalidOperationException>(() => bullet.OnGetFromPool());
            StringAssert.Contains("AudioSource", error.Message);
            var audio = instance.AddComponent<AudioSource>();
            bullet.SetAudioSource(audio);
            typeof(BulletShotGameplay).GetField("_shootClip", Private).SetValue(bullet, null);
            error = Assert.Throws<InvalidOperationException>(() => bullet.OnGetFromPool());
            StringAssert.Contains("_shootClip", error.Message);
            Assert.DoesNotThrow(() => instance.SetActive(false));
        }
        finally { Object.DestroyImmediate(instance); }
    }

    [TestCase(typeof(PlayerComponentBaseGameplay), typeof(PlayerCoreGameplay))]
    [TestCase(typeof(EnemyComponentBaseGameplay), typeof(EnemyCoreGameplay))]
    public void LocalCoreLookupReportsMissingComponent(Type type, Type required)
    {
        var instance = new GameObject("MissingCoreFixture");
        try
        {
            var owner = instance.AddComponent(type);
            var property = type.GetProperty("Core", Private);
            var error = Assert.Throws<TargetInvocationException>(() => property.GetValue(owner));
            Assert.IsInstanceOf<InvalidOperationException>(error.InnerException);
            StringAssert.Contains(required.Name, error.InnerException.Message);
            StringAssert.Contains(instance.name, error.InnerException.Message);
        }
        finally { Object.DestroyImmediate(instance); }
    }

    [TestCase(typeof(PlayerCoreGameplay))]
    [TestCase(typeof(EnemyCoreGameplay))]
    public void MissingDeathEffectPreservesOriginalDiagnostic(Type type)
    {
        EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
        var owner = Object.FindObjectOfType(type);
        var field = type.GetField("_deathEffect", Private);
        var original = field.GetValue(owner);
        try
        {
            field.SetValue(owner, null);
            var error = Assert.Throws<TargetInvocationException>(() => type.GetMethod("Damage").Invoke(owner, new object[] { int.MaxValue }));
            Assert.IsInstanceOf<InvalidOperationException>(error.InnerException);
            StringAssert.Contains(type.Name + "._deathEffect", error.InnerException.Message);
        }
        finally { field.SetValue(owner, original); }
    }

    [TestCase(typeof(PlayerMoveGameplay))]
    [TestCase(typeof(PlayerAttackGameplay))]
    public void PlayerCoreRejectsMissingAbilityComponent(Type removedType)
    {
        var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Player.prefab"));
        try
        {
            Object.DestroyImmediate(instance.GetComponent(removedType));
            var error = Assert.Throws<TargetInvocationException>(() => typeof(PlayerCoreGameplay)
                .GetMethod("Awake", Private).Invoke(instance.GetComponent<PlayerCoreGameplay>(), null));
            Assert.IsInstanceOf<InvalidOperationException>(error.InnerException);
            StringAssert.Contains(removedType.Name, error.InnerException.Message);
        }
        finally { Object.DestroyImmediate(instance); }
    }

    [Test]
    public void PlayerMoveRejectsMissingLineRenderer()
    {
        var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Player.prefab"));
        try
        {
            Object.DestroyImmediate(instance.GetComponent<LineRenderer>());
            var error = Assert.Throws<TargetInvocationException>(() => typeof(PlayerMoveGameplay)
                .GetMethod("Start", Private).Invoke(instance.GetComponent<PlayerMoveGameplay>(), null));
            Assert.IsInstanceOf<InvalidOperationException>(error.InnerException);
            StringAssert.Contains("LineRenderer", error.InnerException.Message);
        }
        finally { Object.DestroyImmediate(instance); }
    }

    [UnityTest]
    public IEnumerator AttacksCanDisableBeforeStartAndKeepOriginalMissingPoolError()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
        foreach (var attack in Object.FindObjectsOfType<EnemyAttackGameplay>()) attack.enabled = false;
        foreach (var move in Object.FindObjectsOfType<EnemyMoveGameplay>()) move.enabled = false;
        var playerAttack = Object.FindObjectOfType<PlayerAttackGameplay>();
        playerAttack.enabled = false;
        Object.FindObjectOfType<BulletPoolInfrastructure>().gameObject.SetActive(false);
        yield return new EnterPlayMode();
        yield return null;
        foreach (var type in new[] { typeof(PlayerAttackGameplay), typeof(EnemyAttackGameplay) })
        {
            var owner = Object.FindObjectOfType(type);
            Assert.DoesNotThrow(() => type.GetMethod("Update", Private).Invoke(owner, null));
            var wrapper = Assert.Throws<TargetInvocationException>(() => type.GetMethod("Start", Private).Invoke(owner, null));
            Assert.IsInstanceOf<InvalidOperationException>(wrapper.InnerException);
            StringAssert.Contains(nameof(BulletPoolInfrastructure), wrapper.InnerException.Message);
            Assert.DoesNotThrow(() => type.GetMethod("OnDisable", Private).Invoke(owner, null));
        }
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (Application.isPlaying) yield return new ExitPlayMode();
        EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
    }
}
