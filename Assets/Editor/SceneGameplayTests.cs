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

/// <summary>保存済みシーンとPrefabを使い、入力機器なしでゲーム機能の接続を確認する。</summary>
public class SceneGameplayTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

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
        FieldInfo powerUpField = typeof(UpgradeButtonPresentation).GetField("_powerUp", Private);
        powerUpField.SetValue(chosen.GetComponent<UpgradeButtonPresentation>(), powerUp);
        // 再抽選で初めて有効になったボタンのStartを待つ。
        yield return null;
        chosen.onClick.Invoke();
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
        Set(manager, "_candidateIds", new[] { 0, 1 });
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
