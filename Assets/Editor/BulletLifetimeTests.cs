using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public class BulletLifetimeTests
{
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
            fired.transform.position = new Vector3(0, 100, 0);
            fired.Parameter = new BulletParametersConfiguration { Duration = 1, Speed = 1 };
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
