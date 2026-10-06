using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading;
using DG.Tweening;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class UiEventLifetimeTests
{
    [UnityTest]
    public IEnumerator ViewersSubscribeOnlyWhileEnabledAndRestoreCurrentState()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
        // 射撃・移動による被弾を止め、実際のシーン参照を使ってUIの寿命だけを確認する。
        foreach (var attack in UnityEngine.Object.FindObjectsOfType<EnemyAttack>()) attack.enabled = false;
        foreach (var move in UnityEngine.Object.FindObjectsOfType<EnemyMove>()) move.enabled = false;
        yield return new EnterPlayMode();
        yield return null;

        var player = UnityEngine.Object.FindObjectOfType<PlayerCore>();
        var attackSource = player.Attack;
        var playerUi = player.GetComponent<PlayerUIViewer>();
        var enemyUi = UnityEngine.Object.FindObjectOfType<EnemyUIViewer>();
        var enemy = enemyUi.GetComponent<EnemyCore>();
        var healthText = Field<TextMeshProUGUI>(playerUi, "_healthText");
        var ammoText = Field<TextMeshProUGUI>(playerUi, "_ammoText");
        var enemyBar = Field<Image>(enemyUi, "_healthImage");
        Assert.AreEqual($"{player.MaxHealth}/{player.MaxHealth}", healthText.text);
        Assert.AreEqual(attackSource.MaxBulletCount.ToString(), ammoText.text);
        Assert.AreEqual(1f, enemyBar.fillAmount);

        for (int cycle = 0; cycle < 3; cycle++)
        {
            AssertSubscriptions(player, attackSource, playerUi, enemy, enemyUi, 1);
            playerUi.enabled = false;
            enemyUi.enabled = false;
            AssertSubscriptions(player, attackSource, playerUi, enemy, enemyUi, 0);
            string oldHealth = healthText.text;
            string oldAmmo = ammoText.text;
            float oldEnemyHealth = enemyBar.fillAmount;
            player.Health = 10 + cycle;
            attackSource.RemainBulletCount = cycle;
            enemy.Health = 10 + cycle;
            // 無効期間は状態だけが変わり、表示は通知を受けない。
            Assert.AreEqual(oldHealth, healthText.text);
            Assert.AreEqual(oldAmmo, ammoText.text);
            Assert.AreEqual(oldEnemyHealth, enemyBar.fillAmount);
            playerUi.enabled = true;
            enemyUi.enabled = true;
            Assert.AreEqual($"{player.Health}/{player.MaxHealth}", healthText.text);
            Assert.AreEqual(attackSource.RemainBulletCount.ToString(), ammoText.text);
            Assert.AreEqual((float)enemy.Health / enemy.MaxHealth, enemyBar.fillAmount, 0.001f);
        }

        // GameObject全体の無効化・再有効化でも、部品だけを切り替えた場合と同じ寿命になる。
        player.gameObject.SetActive(false);
        enemy.gameObject.SetActive(false);
        AssertSubscriptions(player, attackSource, playerUi, enemy, enemyUi, 0);
        player.gameObject.SetActive(true);
        enemy.gameObject.SetActive(true);
        AssertSubscriptions(player, attackSource, playerUi, enemy, enemyUi, 1);
        player.Health = 25;
        attackSource.RemainBulletCount = 1;
        enemy.Health = 25;
        Assert.AreEqual($"25/{player.MaxHealth}", healthText.text);
        Assert.AreEqual("1", ammoText.text);
        Assert.AreEqual(25f / enemy.MaxHealth, enemyBar.fillAmount, 0.001f);

        foreach (var kind in new[] { WeaponWaitKindDomain.Cooldown, WeaponWaitKindDomain.Reload })
        {
            attackSource.RemainBulletCount = kind == WeaponWaitKindDomain.Reload ? 0 : 1;
            // 入力を模擬する代わりに、射撃後と同じ既存の非同期待機を開始する。
            typeof(PlayerAttack).GetMethod("WaitShootCooldownAsync", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(attackSource, new object[] { Field<CancellationTokenSource>(attackSource, "_cts").Token });
            var bar = Field<Image>(playerUi, kind == WeaponWaitKindDomain.Reload ? "_reloadTimeImage" : "_coolDownTimeImage");
            Assert.IsTrue(DOTween.IsTweening(bar));
            playerUi.enabled = false;
            Assert.IsFalse(DOTween.IsTweening(bar));
            float resumeAt = Time.time + attackSource.WaitDurationSeconds * 0.25f;
            while (Time.time < resumeAt) yield return null;
            Assert.AreEqual(kind, attackSource.WaitKind);
            playerUi.enabled = true;
            Assert.That(bar.fillAmount, Is.GreaterThan(0f).And.LessThan(1f));
            Assert.IsTrue(DOTween.IsTweening(bar));
            float finishAt = Time.time + attackSource.RemainingWaitSeconds + 0.1f;
            while (Time.time < finishAt) yield return null;
            Assert.AreEqual(WeaponWaitKindDomain.None, attackSource.WaitKind);
            Assert.AreEqual(1f, bar.fillAmount, 0.001f);
        }

        UnityEngine.Object.Destroy(playerUi);
        UnityEngine.Object.Destroy(enemyUi);
        yield return null;
        AssertSubscriptions(player, attackSource, playerUi, enemy, enemyUi, 0);
        // 通知元を生存させたままUIを破棄しても、残ったコールバックが例外を出さない。
        player.Health = 20;
        attackSource.RemainBulletCount = 1;
        attackSource.OnReloadBegin?.Invoke(1f);
        attackSource.OnCoolDownBegin?.Invoke(1f);
        enemy.Health = 20;
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (Application.isPlaying) yield return new ExitPlayMode();
        // テスト用の一時的なシーン変更を保存しない。
        EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
    }

    private static T Field<T>(object owner, string name) =>
        (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);

    private static void AssertSubscriptions(PlayerCore player, PlayerAttack attack, PlayerUIViewer playerUi,
        EnemyCore enemy, EnemyUIViewer enemyUi, int expected)
    {
        Assert.AreEqual(expected, Count(player.OnHealthChanged, playerUi), "プレイヤー体力");
        Assert.AreEqual(expected, Count(attack.OnAmmoCountChanged, playerUi), "弾数");
        Assert.AreEqual(expected, Count(attack.OnReloadBegin, playerUi), "再装填");
        Assert.AreEqual(expected, Count(attack.OnCoolDownBegin, playerUi), "発射間隔");
        Assert.AreEqual(expected, Count(enemy.OnHealthChanged, enemyUi), "敵体力");
    }

    private static int Count(Delegate notification, object target) =>
        notification?.GetInvocationList().Count(handler => ReferenceEquals(handler.Target, target)) ?? 0;
}
