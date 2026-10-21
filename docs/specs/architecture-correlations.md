# スクリプト依存・イベントとデータ授受

タグ: `現行仕様` `相関図` `イベント購読` `データフロー` `Mermaid`

対象版と範囲は[索引](architecture-diagrams.md)。実線`-->`は呼出元/読取元→依存先、点線`-.->`は発生元→通知先のデータ/イベント。ラベルを優先し、点線をコンパイル依存の方向として読まない。実コードは[対応表](architecture-script-catalog.md)参照。

## 操作・射撃・移動・表示

<!-- diagram-id: architecture-correlations-1 -->
```mermaid
flowchart TB
    subgraph GroupFoundation["規則と共通基底"]
        Ammo[各攻撃が所有するWeaponAmmoStateDomain]
        Spread[BulletSpreadCalculatorDomain]
    end
    subgraph GroupImplementation["制御と具体実装"]
        Move[PlayerMoveGameplay]
        Aim[PlayerAimPointerGameplay]
        PA[PlayerAttackGameplay]
        EA[EnemyAttackGameplay]
        EM[EnemyMoveGameplay]
        EC[EnemyCoreGameplay]
        PC[PlayerCoreGameplay]
        Fire[BulletFireSequenceGameplay]
        Pool[BulletPoolInfrastructure]
        Bullet[BulletShotGameplay]
        PUI[PlayerStatusViewPresentation]
        EUI[EnemyHealthViewPresentation]
        Manager[LevelUpCoordinatorGameplay]
    end
    subgraph GroupExternal["外部APIと検証境界"]
        Input[Unity Input：axis・mouse・Fire]
        Physics[Physics・Rigidbody・NavMeshAgent]
    end
    Input -.->|移動・方向| Move
    Input -.->|Ray用mouse座標| Aim
    Input -.->|Fireフラグ| PA
    Move -->|AddForce・LineRenderer| Physics
    Aim -->|Raycast・hit position| Physics
    Aim -->|メニュー状態| Manager
    PA -->|メニュー状態| Manager
    PA --> Ammo
    EA --> Ammo
    PA -->|武器定義の弾種・値・muzzle| Fire
    EA -->|武器定義の弾種・値・muzzle| Fire
    EM -->|対象・射程・mask| EC
    EA -->|対象・射程・mask| EC
    EM -->|感知・追跡・停止| Physics
    EA -->|距離・射線| Physics
    Fire --> Spread
    Fire -->|Get| Pool
    Pool -->|生成・共通初期化| Bullet
    Fire -->|Parameter・Transform・OnGetFromPool| Bullet
    Bullet -->|衝突先のIDamageableDomain.Damage| PC
    Bullet -->|衝突先のIDamageableDomain.Damage| EC
    Bullet -.->|OnReturnToPool・Release| Pool
    PC -.->|OnHealthChanged| PUI
    PA -.->|残弾int・待機秒float| PUI
    EC -.->|OnHealthChanged| EUI
    PUI -->|体力/最大値・Attack参照| PC
    PUI -->|残弾と待機読取| PA
    EUI -->|体力/最大値| EC
```

[操作相関SVG](diagrams/correlation-combat.svg)。PlayerUIはPCからHealth/MaxHealthとAttack参照を取得し、残弾/待機はPAから読む。移動はInput軸＋Transform、照準Pointerはレイヒット位置を更新し、PlayerMoveの`_lookAt`がある場合にその位置へ向く。配線による参照は[必須参照](component-dependencies.md)を参照する。

## 報酬・強化定義・候補・動的UI

<!-- diagram-id: architecture-correlations-2 -->
```mermaid
flowchart LR
    subgraph GroupFoundation["規則と共通基底"]
        Exp[ExperienceProgressionDomain]
        Select[UpgradeCandidateSelectionDomain]
    end
    subgraph GroupImplementation["制御と具体実装"]
        Enemy[EnemyCoreGameplay]
        Manager[LevelUpCoordinatorGameplay]
        View[LevelUpViewPresentation]
        Core[PlayerCoreGameplay]
        Move[PlayerMoveGameplay]
        Attack[PlayerAttackGameplay]
    end
    subgraph GroupConnection["設定と接続契約"]
        Catalog[UpgradeCatalogConfiguration]
        Def[UpgradeDefinitionConfiguration]
        Button[UpgradeButtonPresentation]
        SceneUI[シーンButton.onClick]
    end
    subgraph GroupExternal["外部APIと検証境界"]
        Random[UnityEngine.Random.Range]
    end
    Enemy -.->|Startで接続したOnDeath：経験値int| Manager
    Manager -->|Gain・選択回数消費| Exp
    Manager -->|token付与・TryDraw/TryReroll| Select
    Manager -->|CreateSnapshot| Catalog
    Catalog -->|SO参照配列をcopy| Def
    Manager -->|ID・IsEligible・Effect| Def
    Manager -->|乱数Funcを渡す| Random
    Select -.->|重複しない3候補ID| Manager
    Exp -.->|Level・Progress・AvailableUpgradeCount| Manager
    Manager -->|確定した経験値/kill/token/回数・候補ID| View
    View -->|Prefab生成・Bind・Unbind・破棄| Button
    Button -->|定義タイトル・列・ID読取| Def
    Button -.->|BindのAction：選択ID| Manager
    SceneUI -.->|TryReroll・OpenCloseUpgradeMenu| Manager
    Manager -->|条件検証・候補解除後にEffect値| Core
    Core -->|MaxHealth・Health更新| Core
    Core -->|Speed倍率| Move
    Core -->|武器強化値| Attack
```

[進行相関SVG](diagrams/correlation-progression.svg)。通常の動的ボタンは`SelectUpgrade`を通る。公開の旧`ApplyPowerUp`入口は選択回数検証を内包せず、図の通常経路と同一視しない。Viewは進行のイベントを購読せずCoordinatorからShow*を呼ばれる。DOTween完了を経験値確定の条件にしない。

## 購読と参照の寿命

| 接続 | 登録/取得 | 解除/終端 | データと注意 |
| --- | --- | --- | --- |
| PlayerUI→Core/Attack | Startで参照/初期化しOnEnableを実行。以後OnEnableで購読と現在値の復元 | OnDisableで全4通知を解除、待機TweenをKill | 体力通知は引数なし、残弾int、2待機通知float。初回OnEnableは初期化前なら購読しない |
| EnemyUI→EnemyCore | Start初期化後と以後OnEnableで購読 | OnDisable解除 | Health通知後Coreの現在値を読む |
| EnemyCore→Coordinator | Startで同一シーン一意探索、private OnDeathにGainExperience登録 | 通常OnDisableで解除/再登録しない。敵個体の寿命に結び付く | 再有効化による二重登録を行わない。死亡はAwake以外で解除しない |
| Button→Coordinator | View.CreateButtonsでBind、Actionが管理側SelectUpgradeを捕捉 | 再生成時Unbind→inactive→親分離→Destroy | 古いUIからの選択を防ぐ。PrefabのonClickはUpgradeを呼ぶ |
| Bullet→Pool | factory callbackで返却Actionを注入、閉包がpoolを捕捉 | 弾のDestroyで個体寿命終了 | Releaseはinactiveにする。activeSelfチェックで重複返却を防ぐ |
| Attack/Bullet非同期 | OnEnable/発射でCTSを生成、UniTask.DelayにTokenを渡す | OnDisable Cancel/Dispose（弾はnullへ） | Attack再有効化は未完了待機を初めから再開始、弾は発射ごとに寿命を設定 |
| Camera/Scene参照 | StartやAwakeで取得/Inspector指定 | Unityのscene/Object寿命 | RequireUniqueは同一シーンの活動中Objectだけを探索、Sceneロード時の永続serviceは持たない |

両Coreのpublic Actionフィールドは`event`キーワードではない。UnityEvent.OnDiedの購読者はInspectorまたは外部登録で決まり、特定のゲーム終了機能を実装済みとして仮定しない。
