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
    public BulletShotGameplay Shot;
    public readonly List<int> ObservedDamage = new List<int>();

    private void OnEnable()
    {
        if (Shot != null) ObservedDamage.Add(Shot.Parameter.Damage);
    }
}

public class BulletLifetimeTests
{
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
            BulletShotGameplay firstBullet = null;
            BulletEnableObserver observer = null;
            for (int use = 0; use < 2; use++)
            {
                var bullet = pool.Get(kind) as BulletShotGameplay;
                Assert.IsNotNull(bullet);
                Assert.IsFalse(bullet.gameObject.activeInHierarchy);
                if (use == 0)
                {
                    firstBullet = bullet;
                    observer = bullet.gameObject.AddComponent<BulletEnableObserver>();
                    observer.Shot = bullet;
                }
                else
                {
                    Assert.AreSame(firstBullet, bullet, "返却した同一弾を再利用すること。");
                    observer.ObservedDamage.Clear();
                }
                int expectedDamage = 37 + use;
                bullet.Parameter = new BulletParametersConfiguration { Damage = expectedDamage, Duration = 1, Speed = 1 };
                bullet.transform.position = new Vector3(0, 100, 0);
                bullet.gameObject.SetActive(true);
                CollectionAssert.AreEqual(new[] { expectedDamage }, observer.ObservedDamage,
                    "OnEnableが初回・再利用ごとの設定値を読むこと。");
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
