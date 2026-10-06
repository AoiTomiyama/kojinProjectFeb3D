using System;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>スクリプトの改名・移動で失われやすいアセット参照とUIの呼び出し先を確認する。</summary>
public class ProjectAssetReferenceTests
{
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
}
