using System.Collections;
using System.Reflection;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class BulletEnableObserver : MonoBehaviour
{
    public readonly List<int> ObservedDamage = new List<int>();

    private void OnEnable()
    {
        var shot = GetComponent<BulletShotGameplay>();
        if (shot != null) ObservedDamage.Add(shot.Parameter.Damage);
    }
}

public class BulletLifetimeTests
{
    [UnityTest]
    public IEnumerator ActivePrefabDoesNotEnableUntilFirstAndReusedBulletsAreConfigured()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
        foreach (var attack in Object.FindObjectsOfType<EnemyAttackGameplay>()) attack.enabled = false;
        foreach (var move in Object.FindObjectsOfType<EnemyMoveGameplay>()) move.enabled = false;
        yield return new EnterPlayMode();
        yield return null;

        var pool = Object.FindObjectOfType<BulletPoolInfrastructure>();
        var databaseField = typeof(BulletPoolInfrastructure).GetField("_objectDatabase", BindingFlags.Instance | BindingFlags.NonPublic);
        var initCountField = typeof(BulletPoolInfrastructure).GetField("_initCount", BindingFlags.Instance | BindingFlags.NonPublic);
        var originalDatabase = (BulletPrefabCatalogConfiguration)databaseField.GetValue(pool);
        int originalInitCount = (int)initCountField.GetValue(pool);
        var testDatabase = Object.Instantiate(originalDatabase);
        GameObject testPrefab = null;
        try
        {
            var prefab = originalDatabase.GetGameObject(BulletTypeDomain.PlayerBullet);
            Assert.IsTrue(prefab.activeSelf, "回帰条件としてPlayerBullet Prefabは活動状態であること。");
            testPrefab = Object.Instantiate(prefab);
            var expectedObserver = testPrefab.AddComponent<BulletEnableObserver>();
            expectedObserver.ObservedDamage.Clear();
            int mappingIndex = testDatabase.Mappings.FindIndex(mapping => mapping.Type == BulletTypeDomain.PlayerBullet);
            var mapping = testDatabase.Mappings[mappingIndex];
            mapping.Prefab = testPrefab;
            testDatabase.Mappings[mappingIndex] = mapping;

            databaseField.SetValue(pool, testDatabase);
            initCountField.SetValue(pool, 0);
            typeof(BulletPoolInfrastructure).GetMethod("InitPool", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(pool, null);

            var first = pool.Get(BulletTypeDomain.PlayerBullet) as BulletShotGameplay;
            Assert.IsNotNull(first);
            var observer = first.GetComponent<BulletEnableObserver>();
            Assert.IsNotNull(observer);
            Assert.IsFalse(first.gameObject.activeInHierarchy);
            Assert.IsEmpty(observer.ObservedDamage,
                "活動中の検証Prefabを非活動ルートへ生成した時点でOnEnableが発生しないこと。");
            first.Parameter = new BulletParametersConfiguration { Damage = 41, Duration = 1, Speed = 1 };
            first.gameObject.SetActive(true);
            CollectionAssert.AreEqual(new[] { 41 }, observer.ObservedDamage,
                "初回の有効化で設定済みDamageを観測すること。");
            first.OnGetFromPool();
            first.OnReturnToPool();

            observer.ObservedDamage.Clear();
            var reused = pool.Get(BulletTypeDomain.PlayerBullet);
            Assert.AreSame(first, reused);
            Assert.IsFalse(reused.gameObject.activeInHierarchy);
            Assert.IsEmpty(observer.ObservedDamage, "再取得時点ではOnEnableが発生しないこと。");
            reused.Parameter = new BulletParametersConfiguration { Damage = 47, Duration = 1, Speed = 1 };
            reused.gameObject.SetActive(true);
            CollectionAssert.AreEqual(new[] { 47 }, observer.ObservedDamage,
                "再利用時も設定後の値をOnEnableが観測すること。");
            reused.OnGetFromPool();
            reused.OnReturnToPool();
            LogAssert.NoUnexpectedReceived();
        }
        finally
        {
            databaseField.SetValue(pool, originalDatabase);
            initCountField.SetValue(pool, originalInitCount);
            if (testPrefab != null) Object.Destroy(testPrefab);
            Object.Destroy(testDatabase);
        }
    }

    [UnityTest]
    public IEnumerator PoolReturnsInactiveBulletsUntilParametersAreSet()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
        foreach (var attack in Object.FindObjectsOfType<EnemyAttackGameplay>()) attack.enabled = false;
        foreach (var move in Object.FindObjectsOfType<EnemyMoveGameplay>()) move.enabled = false;
        yield return new EnterPlayMode();
        yield return null;
        var pool = Object.FindObjectOfType<BulletPoolInfrastructure>();
        foreach (var kind in new[] { BulletTypeDomain.PlayerBullet, BulletTypeDomain.EnemyBullet })
        {
            for (int use = 0; use < 2; use++)
            {
                var bullet = pool.Get(kind) as BulletShotGameplay;
                Assert.IsNotNull(bullet);
                Assert.IsFalse(bullet.gameObject.activeInHierarchy);
                bullet.Parameter = new BulletParametersConfiguration { Damage = 37 + use, Duration = 1, Speed = 1 };
                bullet.transform.position = new Vector3(0, 100, 0);
                bullet.gameObject.SetActive(true);
                bullet.OnGetFromPool();
                bullet.OnReturnToPool();
            }
        }
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator ReturnedBulletCanBeDestroyedBeforeNextShotInitialization()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
        foreach (var attack in Object.FindObjectsOfType<EnemyAttackGameplay>()) attack.enabled = false;
        foreach (var move in Object.FindObjectsOfType<EnemyMoveGameplay>()) move.enabled = false;
        yield return new EnterPlayMode();
        yield return null;
        var pool = Object.FindObjectOfType<BulletPoolInfrastructure>();
        foreach (var kind in new[] { BulletTypeDomain.PlayerBullet, BulletTypeDomain.EnemyBullet })
        {
            var fired = pool.Get(kind);
            Assert.IsFalse(fired.gameObject.activeInHierarchy);
            fired.transform.position = new Vector3(0, 100, 0);
            fired.Parameter = new BulletParametersConfiguration { Duration = 1, Speed = 1 };
            fired.gameObject.SetActive(true);
            fired.OnGetFromPool();
            fired.OnReturnToPool();
            var reused = pool.Get(kind);
            Assert.AreSame(fired, reused);
            // 発射初期化を挟まず破棄すると、返却時に破棄したCTSへ再びCancelする経路になる。
            Object.Destroy(reused.gameObject);
            yield return null;
            Assert.DoesNotThrow(() => typeof(BulletPoolInfrastructure)
                .GetMethod("OnDisposePoolObject", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(pool, new object[] { reused }));
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
