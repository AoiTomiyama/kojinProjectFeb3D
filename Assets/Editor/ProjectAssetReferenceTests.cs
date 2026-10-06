using System;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEditor.Build.Reporting;

/// <summary>スクリプトの改名・移動で失われやすいアセット参照とUIの呼び出し先を確認する。</summary>
public class ProjectAssetReferenceTests
{
    // CIでも同じシーン・ターゲットを使える入口。出力はリポジトリ外の一時領域へ置く。
    public static void BuildWindowsPlayer()
    {
        string output = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "kojin-static-player", "kojinProjectFeb3D.exe");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
            target = BuildTarget.StandaloneWindows64,
            locationPathName = output,
            options = BuildOptions.Development
        });
        Debug.Log($"Windows build: {report.summary.result}; errors={report.summary.totalErrors}; output={output}");
        EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    [Test]
    public void GameScriptsResolveTheirTypes()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets/Scripts" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            // enum・interface・値型も含むため、まずランタイムアセンブリの型として解決する。
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            Type type = Type.GetType(name + ", Assembly-CSharp");
            Assert.IsNotNull(type, path);
            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                Assert.AreEqual(type, script.GetClass(), path);
        }
    }

    [Test]
    public void GamePrefabsHaveNoMissingScriptsOrButtonMethods()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefab" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (var transform in prefab.GetComponentsInChildren<Transform>(true))
                Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject), path);
            foreach (var button in prefab.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                {
                    var target = button.onClick.GetPersistentTarget(i);
                    string method = button.onClick.GetPersistentMethodName(i);
                    Assert.IsNotNull(target, path + ": " + method);
                    Assert.IsTrue(target.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Any(candidate => candidate.Name == method), path + ": " + method);
                }
            }
        }
    }

    [Test]
    public void GameSettingsResolveTheirScriptTypes()
    {
        Assert.IsTrue(AssetDatabase.IsValidFolder("Assets/GameData"));
        // 壊れた型のアセットが検索から漏れないよう、型を絞らず設定ファイルを確認する。
        foreach (string guid in AssetDatabase.FindAssets("", new[] { "Assets/GameData" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".asset", StringComparison.Ordinal)) continue;
            var setting = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            Assert.IsNotNull(setting, path);
            Assert.IsNotNull(MonoScript.FromScriptableObject(setting).GetClass(), path);
        }
    }

    [Test]
    public void AuthoredWeaponsAndBulletCatalogHaveUsableInitialSettings()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:WeaponDefinitionConfiguration", new[] { "Assets/GameData" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinitionConfiguration>(path);
            Assert.That(weapon.MaxBulletCount, Is.GreaterThan(0), path);
            Assert.That(weapon.SynchronousBulletCount, Is.GreaterThan(0), path);
            Assert.That(weapon.CoolDown, Is.GreaterThanOrEqualTo(0), path);
            Assert.That(weapon.ReloadTime, Is.GreaterThan(0), path);
            Assert.That(weapon.BulletParametersConfiguration.Duration, Is.GreaterThanOrEqualTo(1), path);
        }
        foreach (string guid in AssetDatabase.FindAssets("t:BulletPrefabCatalogConfiguration", new[] { "Assets/GameData" }))
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BulletPrefabCatalogConfiguration>(AssetDatabase.GUIDToAssetPath(guid));
            Assert.AreEqual(catalog.Mappings.Count, catalog.Mappings.Select(mapping => mapping.Type).Distinct().Count());
            foreach (BulletTypeDomain kind in Enum.GetValues(typeof(BulletTypeDomain)))
            {
                var prefab = catalog.GetGameObject(kind);
                Assert.IsNotNull(prefab, kind.ToString());
                var bullet = prefab.GetComponent<BulletShotGameplay>();
                Assert.IsNotNull(bullet);
                Assert.IsNotNull(prefab.GetComponent<Rigidbody>());
                Assert.IsNotNull(prefab.GetComponent<Collider>());
                var serialized = new SerializedObject(bullet);
                foreach (string field in new[] { "_hitParticle", "_damageText", "_shootClip" })
                    Assert.IsNotNull(serialized.FindProperty(field).objectReferenceValue, prefab.name + ": " + field);
            }
        }
    }
}
