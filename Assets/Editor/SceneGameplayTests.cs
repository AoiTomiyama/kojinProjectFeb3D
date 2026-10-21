using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// 型を限定しないプール契約と、有効化前の設定順を検証する派生弾。
public class BulletInitializationProbeTests : PooledAttackBaseGameplay
{
    public int InitializationCount { get; private set; }
    public int ShotCount { get; private set; }
    public AudioSource AudioSource { get; private set; }
    public int DamageOnEnable { get; private set; }
    public Vector3 PositionOnEnable { get; private set; }
    public Vector3 ForwardOnEnable { get; private set; }

    public override void OnInitialize(BulletInitializationContextGameplay context)
    {
        Assert.IsFalse(gameObject.activeInHierarchy);
        InitializationCount++;
        AudioSource = context.AudioSource;
    }

    private void OnEnable()
    {
        Assert.AreEqual(1, InitializationCount);
        DamageOnEnable = Parameter.Damage;
        PositionOnEnable = transform.position;
        ForwardOnEnable = transform.forward;
    }

    public override void OnGetFromPool()
    {
        Assert.IsTrue(gameObject.activeInHierarchy);
        ShotCount++;
    }
}

/// <summary>保存済みシーンとPrefabを使い、入力機器なしでゲーム機能の接続を確認する。</summary>
public class SceneGameplayTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [UnityTest]
    public IEnumerator DeathTransitionsRemainOnceAcrossNotificationReentryAndReenable()
    {
        PrepareScene();
        yield return new EnterPlayMode();
        yield return null;
        var manager = Object.FindObjectOfType<LevelUpCoordinatorGameplay>();
        var enemySource = Object.FindObjectOfType<EnemyCoreGameplay>();
        var playerSource = Object.FindObjectOfType<PlayerCoreGameplay>();
        playerSource.Move.enabled = false;
        playerSource.Attack.enabled = false;
        // 新しい個体のAwake/Startを通し、元個体の死亡履歴を引き継がないことも確認する。
        foreach (var source in new MonoBehaviour[] { enemySource, playerSource })
        {
            for (int reentryPath = 0; reentryPath < 3; reentryPath++)
            {
                var instance = Object.Instantiate(source.gameObject);
                var player = instance.GetComponent<PlayerCoreGameplay>();
                if (player != null)
                {
                    player.Move.enabled = false;
                    player.Attack.enabled = false;
                    instance.GetComponent<Rigidbody>().isKinematic = true;
                }
                yield return null;
                var core = (MonoBehaviour)(player != null ? (MonoBehaviour)player : instance.GetComponent<EnemyCoreGameplay>());
                AssertDeathTransitionOnce(core, manager, reentryPath);
                Object.Destroy(instance);
                // 別の敵のStartへ複数Playerの検索条件を持ち込まない。
                yield return null;
            }
        }
        LogAssert.NoUnexpectedReceived();
    }

    private static void AssertDeathTransitionOnce(MonoBehaviour core, LevelUpCoordinatorGameplay manager, int reentryPath)
    {
        var player = core as PlayerCoreGameplay;
        var enemy = core as EnemyCoreGameplay;
        var target = (IDamageableDomain)core;
        Func<int> health = () => player != null ? player.Health : enemy.Health;
        int initialHealth = player != null ? player.MaxHealth : enemy.MaxHealth;
        Assert.AreEqual(initialHealth, health(), "新個体の初期体力");
        core.gameObject.SetActive(false);
        core.gameObject.SetActive(true);
        target.Damage(1);
        Assert.AreEqual(initialHealth - 1, health(), "生存中の再有効化後も通常被弾できる");
        target.Damage(0);
        target.Damage(-1);
        Assert.AreEqual(initialHealth, health(), "既存の0/負ダメージの演算を維持");

        // 不活性なテスト用演出で、粒子寿命やフレーム更新に依存せず生成回数を数える。
        var effect = new GameObject("DeathOnceProbe-" + core.GetInstanceID());
        effect.SetActive(false);
        Set(core, "_deathEffect", effect);
        Func<int> effectCount = () => Resources.FindObjectsOfTypeAll<GameObject>()
            .Count(item => item.name == effect.name + "(Clone)" && item.scene.IsValid());
        int healthNotifications = 0, deathNotifications = 0, rewardNotifications = 0;
        Action onHealth = () =>
        {
            healthNotifications++;
            // 各経路は一度だけ再入し、旧実装でも無限再帰にしない。
            if (healthNotifications == 1 && reentryPath != 2)
                target.Damage(reentryPath == 1 ? health() + 1 : 1);
        };
        if (player != null) player.OnHealthChanged += onHealth;
        else enemy.OnHealthChanged += onHealth;
        var died = player != null ? player.OnDied : enemy.OnDied;
        died.AddListener(() =>
        {
            deathNotifications++;
            // 死亡通知の順序を変えず、通知中はまだ有効である。
            Assert.IsTrue(core.gameObject.activeSelf);
            Assert.AreEqual(player != null ? 1 : 0, effectCount(), "既存の死亡演出と通知の順序");
            if (deathNotifications == 1 && reentryPath == 2) target.Damage(1);
        });
        int killsBefore = Field<int>(manager, "_killCount");
        int tokensBefore = manager.RerollToken;
        var progression = Field<ExperienceProgressionDomain>(manager, "_progression");
        var oracle = new ExperienceProgressionDomain(Field<System.Collections.Generic.List<int>>(manager, "_requireExpList"));
        if (enemy != null)
        {
            // 実管理側への既存購読を残し、報酬通知自身からの再入も追加する。
            Action<int> reward = amount =>
            {
                rewardNotifications++;
                if (rewardNotifications == 1 && reentryPath == 2) target.Damage(1);
            };
            Set(enemy, "OnDeath", Field<Action<int>>(enemy, "OnDeath") + reward);
        }
        int experienceBefore = progression.CurrentExperience;
        int levelBefore = progression.Level;
        int picksBefore = manager.PickCount;
        target.Damage(reentryPath == 1 ? 1 : health() + 1);
        Assert.IsFalse(core.gameObject.activeSelf);
        Assert.AreEqual(-1, health(), "致死値を0へ丸めない");
        Assert.AreEqual(reentryPath == 1 ? 2 : 1, healthNotifications, "各被弾の体力通知だけを発行");
        Assert.AreEqual(1, deathNotifications);
        Assert.AreEqual(enemy != null ? 1 : 0, rewardNotifications);
        Assert.AreEqual(1, effectCount());
        Assert.AreEqual(killsBefore + (enemy != null ? 1 : 0), Field<int>(manager, "_killCount"));
        Assert.AreEqual(tokensBefore + (enemy != null ? 1 : 0), manager.RerollToken);
        // 管理側の実際の累積経験値を独立したルール計算と比較する。
        if (enemy != null)
        {
            int totalExperience = Field<System.Collections.Generic.List<int>>(manager, "_requireExpList")
                .Take(levelBefore).Sum() + experienceBefore + Field<int>(enemy, "_expAmount");
            oracle.Gain(totalExperience);
            Assert.AreEqual(oracle.Level, progression.Level);
            Assert.AreEqual(oracle.CurrentExperience, progression.CurrentExperience);
            Assert.AreEqual(picksBefore + oracle.Level - levelBefore, manager.PickCount);
        }
        else
        {
            Assert.AreEqual(experienceBefore, progression.CurrentExperience);
            Assert.AreEqual(levelBefore, progression.Level);
        }
        int notificationsAfterDeath = healthNotifications;
        for (int i = 0; i < 3; i++) target.Damage(1);
        Assert.AreEqual(-1, health());
        Assert.AreEqual(notificationsAfterDeath, healthNotifications);
        // 公開setterや単なる再有効化は、死亡履歴を取り消す復活APIではない。
        if (player != null) player.MaxHealth += 1;
        else enemy.Health = initialHealth;
        int rewrittenHealth = health();
        int notificationsAfterSetter = healthNotifications;
        core.gameObject.SetActive(true);
        target.Damage(rewrittenHealth + 1);
        Assert.AreEqual(rewrittenHealth, health());
        Assert.AreEqual(notificationsAfterSetter, healthNotifications);
        Assert.AreEqual(1, deathNotifications);
        Assert.AreEqual(1, effectCount());
        Assert.AreEqual(killsBefore + (enemy != null ? 1 : 0), Field<int>(manager, "_killCount"));
        Assert.AreEqual(tokensBefore + (enemy != null ? 1 : 0), manager.RerollToken);
        if (player != null) player.OnHealthChanged -= onHealth;
        else enemy.OnHealthChanged -= onHealth;
        core.gameObject.SetActive(false);
        foreach (var clone in Resources.FindObjectsOfTypeAll<GameObject>()
            .Where(item => item.name == effect.name + "(Clone)" && item.scene.IsValid())) Object.Destroy(clone);
        Object.Destroy(effect);
    }

    [UnityTest]
    public IEnumerator ProgressionButtonsHealthAndPrefabDefaultsStayConsistent()
    {
        PrepareScene();
        yield return new EnterPlayMode();
        yield return null;
        var player = Object.FindObjectOfType<PlayerCoreGameplay>();
        player.Move.enabled = false;
        var manager = Object.FindObjectOfType<LevelUpCoordinatorGameplay>();
        var view = manager.GetComponent<LevelUpViewPresentation>();
        var buttons = Field<Button[]>(view, "_buttons");
        Assert.AreEqual(11, buttons.Length);
        Assert.AreEqual(3, buttons.Count(b => b.gameObject.activeSelf));
        Assert.IsFalse(manager.IsMenuActivated);
        // 保存されたonClickを実際に呼び、配線だけでなく強化窓口までの動作を確認する。
        var menuButton = Object.FindObjectsOfType<Button>(true).First(b => HasCall(b, "OpenCloseUpgradeMenu"));
        menuButton.onClick.Invoke();
        yield return null;
        Assert.IsTrue(manager.IsMenuActivated);
        Assert.AreEqual(3, buttons.Count(b => b.gameObject.activeInHierarchy));
        var ui = player.GetComponent<PlayerStatusViewPresentation>();
        var healthText = Field<TextMeshProUGUI>(ui, "_healthText");
        var healthBar = Field<Image>(ui, "_healthImage");
        Assert.AreEqual(50, player.Health);
        Assert.AreEqual("50/50", healthText.text);
        player.Damage(10);
        Assert.AreEqual("40/50", healthText.text);
        Assert.AreEqual(0.8f, healthBar.fillAmount, 0.001f);

        var progression = Field<ExperienceProgressionDomain>(manager, "_progression");
        int expectedExperience = 0;
        var enemies = Object.FindObjectsOfType<EnemyCoreGameplay>();
        Assert.That(enemies.Length, Is.GreaterThanOrEqualTo(2));
        foreach (var enemy in enemies.Take(2))
        {
            expectedExperience += Field<int>(enemy, "_expAmount");
            enemy.Damage(enemy.Health);
            Assert.IsFalse(enemy.gameObject.activeSelf);
        }
        var oracle = new ExperienceProgressionDomain(Field<System.Collections.Generic.List<int>>(manager, "_requireExpList"));
        oracle.Gain(expectedExperience);
        Assert.AreEqual(oracle.Level, progression.Level);
        Assert.AreEqual(oracle.CurrentExperience, progression.CurrentExperience);
        Assert.AreEqual(2, manager.RerollToken);
        Assert.AreEqual("2", Field<TextMeshProUGUI>(view, "_killCountText").text);
        manager.GainExperience(int.MaxValue);
        Assert.IsTrue(progression.IsAtMaxLevel);
        Assert.AreEqual(31, manager.PickCount);
        Assert.AreEqual(1f, Field<Image>(view, "_expBar").fillAmount);
        manager.GainExperience(1);
        Assert.AreEqual(31, manager.PickCount);
        Assert.Throws<ArgumentOutOfRangeException>(() => manager.GainExperience(-1));
        Assert.AreEqual(4, manager.RerollToken);
        var rerollButton = Object.FindObjectsOfType<Button>(true).First(b => HasCall(b, "TryReroll"));
        rerollButton.onClick.Invoke();
        Assert.AreEqual(1, manager.RerollToken);
        Assert.AreEqual(3, buttons.Count(b => b.gameObject.activeSelf));

        var definition = Field<WeaponDefinitionConfiguration>(player.Attack, "_definition");
        string originalDefinition = EditorJsonUtility.ToJson(definition);
        string originalStats = EditorJsonUtility.ToJson(player.InitialStats);
        var powerUp = new UpgradeParametersConfiguration
        {
            MaxHealthAdd = 10, MaxHealthMultiply = 1, MoveSpeedMultiply = 1.5f,
            DamageAdd = 3, DamageMultiply = 2, SpeedAdd = 2, SpeedMultiply = 1,
            MaxAmmoSizeAdd = 2, SyncBulletAdd = 1, RicochetAdd = 1,
            CoolTimeMultiply = 1, ReloadTimeMultiply = 1
        };
        float oldSpeed = player.Move.Speed;
        var oldBullet = Field<BulletParametersConfiguration>(player.Attack, "_bulletParameter");
        int oldCapacity = player.Attack.MaxBulletCount;
        var chosen = buttons.First(b => b.gameObject.activeSelf);
        var presentation = chosen.GetComponent<UpgradeButtonPresentation>();
        var temporaryUpgrade = Object.Instantiate(presentation.Definition);
        Set(temporaryUpgrade, "_effect", powerUp);
        Field<System.Collections.Generic.Dictionary<int, UpgradeDefinitionConfiguration>>(manager, "_definitionsById")[temporaryUpgrade.Id] = temporaryUpgrade;
        presentation.Bind(temporaryUpgrade, Field<Action<int>>(presentation, "_onSelected"));
        // 再抽選で初めて有効になったボタンのStartを待つ。
        yield return null;
        chosen.onClick.Invoke();
        Object.Destroy(temporaryUpgrade);
        Assert.AreEqual(30, manager.PickCount);
        Assert.AreEqual(3, buttons.Count(b => b.gameObject.activeSelf));
        Assert.AreEqual(60, player.MaxHealth);
        Assert.AreEqual("60/60", healthText.text);
        Assert.AreEqual(1f, healthBar.fillAmount);
        Assert.AreEqual(oldSpeed * 1.5f, player.Move.Speed);
        Assert.AreEqual(oldCapacity + 2, player.Attack.MaxBulletCount);
        Assert.AreEqual(player.Attack.MaxBulletCount.ToString(), Field<TextMeshProUGUI>(ui, "_ammoText").text);
        Assert.AreEqual((oldBullet.Damage + 3) * 2, Field<BulletParametersConfiguration>(player.Attack, "_bulletParameter").Damage);
        Assert.AreEqual(originalDefinition, EditorJsonUtility.ToJson(definition));
        Assert.AreEqual(originalStats, EditorJsonUtility.ToJson(player.InitialStats));

        // Prefabの基本体力とシーン設定の差を、実際に初期化した別個体で確認する。
        var clone = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Player.prefab"));
        clone.GetComponent<PlayerMoveGameplay>().enabled = false;
        clone.GetComponent<PlayerAttackGameplay>().enabled = false;
        yield return null;
        Assert.AreEqual(100, clone.GetComponent<PlayerCoreGameplay>().Health);
        Assert.AreEqual(10, clone.GetComponent<PlayerAttackGameplay>().MaxBulletCount);
        Object.Destroy(clone);
        menuButton.onClick.Invoke();
        Assert.IsFalse(manager.IsMenuActivated);

        // 候補不足のエラー経路でも残高を失わない。
        manager.GainExperience(0);
        manager.GainExperience(0);
        int tokens = manager.RerollToken;
        Set(manager, "_definitions", Field<UpgradeDefinitionConfiguration[]>(manager, "_definitions").Take(2).ToArray());
        LogAssert.Expect(LogType.Error, "強化候補のボタンが3件未満のため抽選できません。");
        manager.TryReroll();
        Assert.AreEqual(tokens, manager.RerollToken);
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator ShootingReloadCancellationAndPoolReuseWorkForBothAttackers()
    {
        PrepareScene();
        // シーンの保存文字列5と異なる初期装弾数にする。設定アセット自体は変更しない。
        var authoredAttack = Object.FindObjectOfType<PlayerAttackGameplay>();
        var temporaryDefinition = Object.Instantiate(Field<WeaponDefinitionConfiguration>(authoredAttack, "_definition"));
        Set(temporaryDefinition, "_maxBulletCount", 7);
        Set(authoredAttack, "_definition", temporaryDefinition);
        yield return new EnterPlayMode();
        yield return null;
        var player = Object.FindObjectOfType<PlayerCoreGameplay>();
        player.Move.enabled = false;
        var attack = player.Attack;
        var ammoText = Field<TextMeshProUGUI>(player.GetComponent<PlayerStatusViewPresentation>(), "_ammoText");
        Assert.AreEqual("7", ammoText.text);
        // 衝突による寿命短縮を避け、射撃状態と返却を独立して検証する。
        Set(attack, "_muzzle", new GameObject("TestMuzzle").transform);
        Field<Transform>(attack, "_muzzle").position = new Vector3(0, 100, 0);
        Call(attack, "Shoot");
        Assert.AreEqual("6", ammoText.text);
        var pool = Object.FindObjectOfType<BulletPoolInfrastructure>();
        Assert.That(pool.GetComponentsInChildren<BulletShotGameplay>().Length, Is.GreaterThan(0));
        foreach (var bullet in pool.GetComponentsInChildren<BulletShotGameplay>()) bullet.OnReturnToPool();

        var enemyAttack = Object.FindObjectOfType<EnemyAttackGameplay>();
        // 既存のStartを使い、入力・自動Updateを進めずに同じ非同期待機へ入る。
        Call(enemyAttack, "Start");
        Call(enemyAttack, "OnEnable");
        Set(enemyAttack, "_muzzle", Field<Transform>(attack, "_muzzle"));
        foreach (MonoBehaviour source in new MonoBehaviour[] { attack, enemyAttack })
        {
            var state = Field<WeaponAmmoStateDomain>(source, "_ammo");
            state.SetTimings(0.08f, 0.12f);
            foreach (int remaining in new[] { 2, 0 })
            {
                state.SetRemainingAmmo(remaining);
                Call(source, "WaitShootCooldownAsync", Field<CancellationTokenSource>(source, "_cts").Token);
                Assert.IsFalse(state.IsReady);
                // 中断中に元の待機が満了しても残弾を復元しない。
                Call(source, "OnDisable");
                float deadline = Time.time + 0.2f;
                while (Time.time < deadline) yield return null;
                Assert.IsFalse(state.IsReady);
                Assert.AreEqual(remaining, state.RemainingAmmo);
                Call(source, "OnEnable");
                deadline = Time.time + 0.3f;
                while (Time.time < deadline) yield return null;
                Assert.IsTrue(state.IsReady);
                Assert.AreEqual(remaining == 0 ? state.Capacity : remaining, state.RemainingAmmo);
                int before = state.RemainingAmmo;
                Call(source, "Shoot");
                Assert.That(state.RemainingAmmo, Is.LessThan(before));
                foreach (var bullet in pool.GetComponentsInChildren<BulletShotGameplay>()) bullet.OnReturnToPool();
            }
        }
        attack.RemainBulletCount = 0;
        Call(attack, "WaitShootCooldownAsync", Field<CancellationTokenSource>(attack, "_cts").Token);
        float finish = Time.time + 0.3f;
        while (Time.time < finish) yield return null;
        Assert.AreEqual("7", ammoText.text);
        foreach (var kind in new[] { BulletTypeDomain.PlayerBullet, BulletTypeDomain.EnemyBullet })
        {
            var bullet = pool.Get(kind);
            Assert.AreSame(Field<AudioSource>(pool, "_soundEffects"), Field<AudioSource>(bullet, "_aus"));
            bullet.Parameter = new BulletParametersConfiguration { Speed = 1, Duration = 1f };
            bullet.transform.position = new Vector3(0, 100, 0);
            bullet.OnGetFromPool();
            finish = Time.time + 1.2f;
            while (Time.time < finish) yield return null;
            Assert.IsFalse(bullet.gameObject.activeSelf, "寿命後の返却");
        }
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator EnemyLineOfSightStopsAndShootsOnlyWithoutObstruction()
    {
        PrepareScene();
        yield return new EnterPlayMode();
        yield return null;
        var player = Object.FindObjectOfType<PlayerCoreGameplay>();
        player.Move.enabled = false;
        player.GetComponent<Rigidbody>().isKinematic = true;
        var enemy = Object.FindObjectOfType<EnemyCoreGameplay>();
        foreach (var other in Object.FindObjectsOfType<EnemyCoreGameplay>())
            if (other != enemy) other.gameObject.SetActive(false);
        var move = enemy.GetComponent<EnemyMoveGameplay>();
        var attack = enemy.GetComponent<EnemyAttackGameplay>();
        Call(move, "Start");
        Call(attack, "Start");
        Call(attack, "OnEnable");
        var agent = enemy.GetComponent<NavMeshAgent>();
        Assert.IsTrue(agent.isOnNavMesh);
        Vector3 origin = enemy.transform.position;
        // 既存NavMesh上で近い点を選び、距離・壁・他の敵に左右されない射線を用意する。
        Assert.IsTrue(NavMesh.SamplePosition(origin + Vector3.forward * 3, out var navHit, 5, NavMesh.AllAreas));
        player.transform.position = navHit.position + Vector3.up * 0.5f;
        enemy.transform.LookAt(player.transform);
        var muzzle = new GameObject("TestEnemyMuzzle").transform;
        muzzle.position = origin + Vector3.up * 0.5f + enemy.transform.forward;
        Set(attack, "_muzzle", muzzle);
        Set(enemy, "_shootRange", 20f);
        Set(move, "_detectRange", 20f);
        Physics.SyncTransforms();
        Assert.AreEqual(player.gameObject.layer, enemy.PlayerLayer);
        Assert.AreEqual(1 << player.gameObject.layer, enemy.PlayerLayerMask);
        var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blocker.transform.position = (muzzle.position + player.transform.position) / 2;
        blocker.transform.localScale = Vector3.one * 0.5f;
        Physics.SyncTransforms();
        var ammo = Field<WeaponAmmoStateDomain>(attack, "_ammo");
        int before = ammo.RemainingAmmo;
        Call(attack, "Update");
        Assert.AreEqual(before, ammo.RemainingAmmo, "遮蔽物があると撃たない");
        Call(move, "Update");
        Assert.That(Vector3.Distance(agent.destination, player.transform.position), Is.LessThan(1f));
        Object.DestroyImmediate(blocker);
        Physics.SyncTransforms();
        Assert.IsTrue(Physics.Raycast(muzzle.position, player.transform.position - muzzle.position, out var hit, 20));
        Assert.AreEqual(enemy.PlayerLayer, hit.collider.gameObject.layer, "射線の最初の接触先");
        Call(move, "Update");
        // SetDestinationの経路更新を次のフレームで確定させる。
        yield return null;
        Assert.That(Vector3.ProjectOnPlane(agent.destination - enemy.transform.position, Vector3.up).magnitude,
            Is.LessThan(0.2f), "射程内で停止（NavMeshの床面へ投影した位置）");
        Call(attack, "Update");
        Assert.That(ammo.RemainingAmmo, Is.LessThan(before), "遮蔽がなくなると発砲");
        foreach (var bullet in Object.FindObjectsOfType<BulletShotGameplay>()) bullet.OnReturnToPool();
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator RealBulletCollisionDealsDamageShowsTextAndReturnsToPool()
    {
        PrepareScene();
        yield return new EnterPlayMode();
        yield return null;
        var player = Object.FindObjectOfType<PlayerCoreGameplay>();
        player.Move.enabled = false;
        player.GetComponent<Rigidbody>().isKinematic = true;
        player.transform.position = new Vector3(0, 100, 0);
        var pool = Object.FindObjectOfType<BulletPoolInfrastructure>();
        var bullet = pool.Get(BulletTypeDomain.EnemyBullet);
        bullet.transform.position = player.transform.position + Vector3.back * 2;
        bullet.transform.forward = Vector3.forward;
        bullet.Parameter = new BulletParametersConfiguration { Damage = 7, Speed = 10, Duration = 2, RicochetCount = 0 };
        Physics.SyncTransforms();
        int health = player.Health;
        bullet.gameObject.SetActive(true);
        bullet.OnGetFromPool();
        float deadline = Time.time + 1f;
        while (player.Health == health && Time.time < deadline) yield return null;
        Assert.AreEqual(health - 7, player.Health);
        Assert.IsFalse(bullet.gameObject.activeSelf);
        Assert.IsTrue(Object.FindObjectsOfType<TextMeshPro>().Any(text => text.text == "7"));
        deadline = Time.time + 1f;
        while (Time.time < deadline) yield return null;
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (Application.isPlaying) yield return new ExitPlayMode();
        EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
    }

    [UnityTest]
    public IEnumerator NavigationRegistersForAllEnemiesWhenSceneLoadsDuringPlay()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
        // この確認では移動のStartを実行する。ほかのテスト用の移動無効化を持ち込まない。
        foreach (var attack in Object.FindObjectsOfType<EnemyAttackGameplay>()) attack.enabled = false;
        yield return new EnterPlayMode();
        yield return null;
        int agentCount = Object.FindObjectsOfType<NavMeshAgent>().Length;
        // Single読込みで旧Surfaceを破棄し、Playerと同じ実行中の登録を確認する。
        var load = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("InGame");
        while (!load.isDone) yield return null;
        var agents = Object.FindObjectsOfType<NavMeshAgent>();
        // EditModeのテスト更新とPlayer更新は別のため、フレーム数ではなく初期化の完了を待つ。
        float deadline = Time.realtimeSinceStartup + 5f;
        while (agents.Any(agent => !agent.enabled) && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.AreEqual(agentCount, agents.Length);
        foreach (var agent in agents)
        {
            Assert.IsTrue(agent.enabled, agent.name + ": 移動初期化でAgentを有効化");
            Assert.IsTrue(agent.isOnNavMesh, agent.name + ": シーン読込み後のNavMesh接続");
            Assert.IsTrue(agent.SetDestination(agent.transform.position));
        }
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator AimCameraAndMovementReferencesWorkWithoutInteractiveInput()
    {
        PrepareScene();
        yield return new EnterPlayMode();
        yield return null;
        var player = Object.FindObjectOfType<PlayerCoreGameplay>();
        var manager = Object.FindObjectOfType<LevelUpCoordinatorGameplay>();
        var camera = Object.FindObjectOfType<Camera>();
        var aim = Object.FindObjectOfType<PlayerAimPointerGameplay>();
        aim.enabled = false;
        player.Move.enabled = false;
        Assert.AreSame(camera, Field<Camera>(aim, "_camera"));
        Assert.AreSame(manager, Field<LevelUpCoordinatorGameplay>(aim, "_lvUpManager"));
        Assert.AreSame(camera.transform, Field<Transform>(player.Move, "_camera"));
        Assert.AreEqual(player.InitialStats.MoveSpeed, player.Move.Speed);
        var follow = Object.FindObjectOfType<CameraFollowPresentation>();
        follow.enabled = false;
        player.transform.position += Vector3.right * 2;
        Call(follow, "Update");
        Assert.AreEqual(Field<Transform>(follow, "_target").position + Field<Vector3>(follow, "_offset"), follow.transform.position);

        // バッチ環境の実際のマウス位置から出るレイに検証用の面を置く。
        // 入力値を直接書き換えず、照準のRaycastとメニュー中の停止を確認する。
        var ray = camera.ScreenPointToRay(Input.mousePosition);
        var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        target.layer = 0;
        target.transform.position = ray.GetPoint(5);
        target.transform.localScale = Vector3.one;
        Set(aim, "mask", (LayerMask)1);
        Set(aim, "_raycastMaxDistance", 10f);
        Physics.SyncTransforms();
        Assert.IsTrue(Physics.Raycast(ray, out var hit, 10, 1));
        Call(aim, "Update");
        Assert.That(Vector3.Distance(hit.point, aim.transform.position), Is.LessThan(0.001f));
        manager.OpenCloseUpgradeMenu();
        Vector3 oldAim = aim.transform.position;
        target.transform.position = ray.GetPoint(7);
        Physics.SyncTransforms();
        Call(aim, "Update");
        Assert.AreEqual(oldAim, aim.transform.position);
        manager.OpenCloseUpgradeMenu();
        Call(aim, "Update");
        Assert.That(Vector3.Distance(oldAim, aim.transform.position), Is.GreaterThan(1f));

        Set(player.Move, "_lookAt", aim.transform);
        Call(player.Move, "LookAt");
        Vector3 flatTarget = aim.transform.position;
        flatTarget.y = player.transform.position.y;
        Assert.That(Vector3.Dot(player.transform.forward, (flatTarget - player.transform.position).normalized), Is.GreaterThan(0.999f));
        var line = player.GetComponent<LineRenderer>();
        Assert.AreEqual(player.transform.position, line.GetPosition(0));
        Assert.AreEqual(flatTarget, line.GetPosition(1));
        Call(player.Move, "Move");
        foreach (var billboard in Object.FindObjectsOfType<CameraBillboardPresentation>())
        {
            Call(billboard, "Update");
            Assert.That(Vector3.Dot(billboard.transform.forward,
                (billboard.transform.position - camera.transform.position).normalized), Is.GreaterThan(0.999f));
        }
        // 入力を保持した射撃経路も、メニュー中は発射せず閉じた後に発射する。
        var testMuzzle = new GameObject("InputPathMuzzle").transform;
        testMuzzle.position = new Vector3(0, 100, 0);
        Set(player.Attack, "_muzzle", testMuzzle);
        manager.OpenCloseUpgradeMenu();
        Set(player.Attack, "_isPressedShootButton", true);
        int ammo = player.Attack.RemainBulletCount;
        Call(player.Attack, "Update");
        Assert.AreEqual(ammo, player.Attack.RemainBulletCount);
        manager.OpenCloseUpgradeMenu();
        Call(player.Attack, "Update");
        Assert.AreEqual(ammo - 1, player.Attack.RemainBulletCount);
        Set(player.Attack, "_isPressedShootButton", false);
        foreach (var bullet in Object.FindObjectsOfType<BulletShotGameplay>()) bullet.OnReturnToPool();
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator WeaponDefinitionsSelectEitherPoolForBothAttackers()
    {
        PrepareScene();
        yield return new EnterPlayMode();
        yield return null;
        var player = Object.FindObjectOfType<PlayerCoreGameplay>();
        player.Move.enabled = false;
        player.Attack.enabled = false;
        var enemy = Object.FindObjectOfType<EnemyAttackGameplay>();
        Call(enemy, "Start");
        var pool = Object.FindObjectOfType<BulletPoolInfrastructure>();
        var muzzle = new GameObject("DefinitionSelectionMuzzle").transform;
        muzzle.position = new Vector3(0, 100, 0);
        foreach (MonoBehaviour attacker in new MonoBehaviour[] { player.Attack, enemy })
        {
            var definition = Object.Instantiate(Field<WeaponDefinitionConfiguration>(attacker, "_definition"));
            Set(attacker, "_definition", definition);
            Set(attacker, "_muzzle", muzzle);
            Set(attacker, "_synchronousBulletCount", 1);
            foreach (var kind in new[] { BulletTypeDomain.PlayerBullet, BulletTypeDomain.EnemyBullet })
            {
                Set(definition, "_bulletType", kind);
                var expected = pool.Get(kind);
                pool.Release(kind, expected);
                Field<WeaponAmmoStateDomain>(attacker, "_ammo").SetRemainingAmmo(2);
                Call(attacker, "Shoot");
                Assert.IsTrue(expected.gameObject.activeSelf, attacker.GetType().Name + ": " + kind);
                Assert.AreEqual(1, Field<WeaponAmmoStateDomain>(attacker, "_ammo").RemainingAmmo);
                Assert.AreEqual(muzzle.position, expected.transform.position);
                expected.OnReturnToPool();
                Assert.IsFalse(expected.gameObject.activeSelf);
            }
            Object.Destroy(definition);
        }
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator DerivedBulletReceivesContextOnceBeforeActivationAndResetsEachShot()
    {
        PrepareScene();
        yield return new EnterPlayMode();
        yield return null;
        Object.FindObjectOfType<PlayerAttackGameplay>().enabled = false;
        var prefab = new GameObject("ProbeBullet");
        prefab.SetActive(false);
        prefab.AddComponent<BulletInitializationProbeTests>();
        var catalog = ScriptableObject.CreateInstance<BulletPrefabCatalogConfiguration>();
        catalog.Mappings = new System.Collections.Generic.List<BulletPrefabCatalogConfiguration.BulletPrefabMappingConfiguration>
        {
            new BulletPrefabCatalogConfiguration.BulletPrefabMappingConfiguration { Type = BulletTypeDomain.PlayerBullet, Prefab = prefab }
        };
        var owner = new GameObject("ProbePool");
        var pool = owner.AddComponent<BulletPoolInfrastructure>();
        pool.enabled = false;
        Set(pool, "_objectDatabase", catalog);
        Set(pool, "_initCount", 1);
        // 無音の派生弾は音源なしで初期化・再利用できる。
        Call(pool, "Start");
        var bullet = (BulletInitializationProbeTests)pool.Get(BulletTypeDomain.PlayerBullet);
        Assert.AreEqual(1, bullet.InitializationCount);
        Assert.IsNull(bullet.AudioSource);
        Assert.IsFalse(bullet.gameObject.activeSelf);
        pool.Release(BulletTypeDomain.PlayerBullet, bullet);
        for (int shot = 1; shot <= 2; shot++)
        {
            var ammo = new WeaponAmmoStateDomain(1, 0, 1);
            owner.transform.position = new Vector3(0, 100 + shot, 0);
            BulletFireSequenceGameplay.Fire(pool, BulletTypeDomain.PlayerBullet,
                new BulletParametersConfiguration { Damage = shot, Speed = shot, Duration = 10 },
                owner.transform, Vector3.right, 1, 0, ammo);
            Assert.AreEqual(1, bullet.InitializationCount);
            Assert.AreEqual(shot, bullet.ShotCount);
            Assert.AreEqual(shot, bullet.DamageOnEnable);
            Assert.AreEqual(owner.transform.position, bullet.PositionOnEnable);
            Assert.That(Vector3.Distance(Vector3.right, bullet.ForwardOnEnable), Is.LessThan(0.0001f));
            bullet.OnReturnToPool();
            Assert.IsFalse(bullet.gameObject.activeSelf);
        }
        Object.Destroy(owner);
        Object.Destroy(prefab);
        Object.Destroy(catalog);
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator SilentBulletShotWorksWithoutAudioAndResetsVelocityOnReuse()
    {
        PrepareScene();
        yield return new EnterPlayMode();
        yield return null;
        Object.FindObjectOfType<PlayerAttackGameplay>().enabled = false;
        var pool = Object.FindObjectOfType<BulletPoolInfrastructure>();
        var bullet = (BulletShotGameplay)pool.Get(BulletTypeDomain.PlayerBullet);
        Set(bullet, "_shootClip", null);
        Set(bullet, "_aus", null);
        for (int shot = 1; shot <= 2; shot++)
        {
            bullet.Parameter = new BulletParametersConfiguration { Speed = shot * 3, Duration = 10 };
            bullet.transform.position = new Vector3(0, 100, 0);
            bullet.transform.forward = Vector3.right;
            bullet.gameObject.SetActive(true);
            bullet.OnGetFromPool();
            Assert.That(Vector3.Distance(Vector3.right * shot * 3, bullet.GetComponent<Rigidbody>().velocity), Is.LessThan(0.0001f));
            bullet.OnReturnToPool();
            Assert.AreSame(bullet, pool.Get(BulletTypeDomain.PlayerBullet));
        }
        pool.Release(BulletTypeDomain.PlayerBullet, bullet);
        LogAssert.NoUnexpectedReceived();
    }

    private static void PrepareScene()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity");
        foreach (var attack in Object.FindObjectsOfType<EnemyAttackGameplay>()) attack.enabled = false;
        foreach (var move in Object.FindObjectsOfType<EnemyMoveGameplay>()) move.enabled = false;
    }
    private static bool HasCall(Button button, string method) => Enumerable.Range(0, button.onClick.GetPersistentEventCount())
        .Any(i => button.onClick.GetPersistentMethodName(i) == method);
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Private).GetValue(owner);
    private static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Private).SetValue(owner, value);
    private static void Call(object owner, string name, params object[] args) => owner.GetType().GetMethod(name, Private).Invoke(owner, args);
}
