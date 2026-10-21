# 機能別のクラス図

タグ: `現行仕様` `クラス設計` `継承` `状態所有` `Mermaid`

確認日/対象版/差分の範囲は[図式の索引](architecture-diagrams.md)。凡例: `基底 <|-- 派生`は継承、`契約 <|.. 実装`はinterface実装、`所有者 *-- 値`は個体ごとに生成/保持するC#状態、`参照元 --> 先`は呼出/読取参照、`..>`はその手順だけの利用。Unityコンポーネント間の参照はUnityによる寿命管理のためcompositionを使わない。SOの参照共有にもcompositionを使わない。全型のファイル/責務は[対応表](architecture-script-catalog.md)。メンバーは関係を説明する主要入口のみ。

## Player/Enemy：体力・移動・攻撃・表示

各図のnamespace囲みは責務の説明用で、実装のC#名前空間ではない。Foundationは規則と自作共通基底、Implementationは制御と具体実装、Connectionは設定と接続契約、ExternalはUnity基底や外部ライブラリを表す。詳細は[配色の凡例](architecture-diagrams.md#責務分類と配色の凡例)を参照。

<!-- diagram-id: architecture-classes-1 -->
```mermaid
classDiagram
    namespace Foundation {
        class PlayerComponentBaseGameplay {
          #PlayerCoreGameplay Core
        }
        class EnemyComponentBaseGameplay {
          #EnemyCoreGameplay Core
        }
        class WeaponAmmoStateDomain
    }
    namespace Implementation {
        class PlayerCoreGameplay {
          -bool _isDead
          +int Health
          +int MaxHealth
          +Damage(int)
          +ApplyPowerUp(UpgradeParametersConfiguration)
          +OnHealthChanged
          +OnDied
        }
        class EnemyCoreGameplay {
          -bool _isDead
          +int Health
          +Transform Target
          +Damage(int)
          -OnDeath
          +OnHealthChanged
          +OnDied
        }
        class PlayerMoveGameplay {
          +float Speed
        }
        class PlayerAttackGameplay {
          +ApplyPowerUp(UpgradeParametersConfiguration)
          +OnAmmoCountChanged
          +OnCoolDownBegin
          +OnReloadBegin
        }
        class PlayerStatusViewPresentation
        class EnemyMoveGameplay
        class EnemyAttackGameplay
        class EnemyHealthViewPresentation
    }
    namespace Connection {
        class IDamageableDomain {
          <<interface>>
          +Damage(int)
        }
        class PlayerInitialStatsConfiguration
        class SceneReferenceResolverInfrastructure
    }
    namespace External {
        class MonoBehaviour
    }
    MonoBehaviour <|-- PlayerCoreGameplay
    MonoBehaviour <|-- EnemyCoreGameplay
    MonoBehaviour <|-- PlayerComponentBaseGameplay
    MonoBehaviour <|-- EnemyComponentBaseGameplay
    IDamageableDomain <|.. PlayerCoreGameplay
    IDamageableDomain <|.. EnemyCoreGameplay
    PlayerComponentBaseGameplay <|-- PlayerMoveGameplay
    PlayerComponentBaseGameplay <|-- PlayerAttackGameplay
    PlayerComponentBaseGameplay <|-- PlayerStatusViewPresentation
    EnemyComponentBaseGameplay <|-- EnemyMoveGameplay
    EnemyComponentBaseGameplay <|-- EnemyAttackGameplay
    EnemyComponentBaseGameplay <|-- EnemyHealthViewPresentation
    PlayerComponentBaseGameplay --> PlayerCoreGameplay : 同一ObjectのCoreをcache
    EnemyComponentBaseGameplay --> EnemyCoreGameplay : 同一ObjectのCoreをcache
    PlayerCoreGameplay --> PlayerMoveGameplay : 強化時に速度を変更
    PlayerCoreGameplay --> PlayerAttackGameplay : 武器強化を委譲
    PlayerCoreGameplay --> PlayerInitialStatsConfiguration : 初期値を複製
    PlayerAttackGameplay *-- WeaponAmmoStateDomain : 個体ごとの残弾状態
    EnemyAttackGameplay *-- WeaponAmmoStateDomain : 別の残弾状態
    PlayerStatusViewPresentation --> PlayerAttackGameplay : 購読と読取
    EnemyCoreGameplay --> PlayerCoreGameplay : 追跡対象のTransform
    EnemyCoreGameplay ..> SceneReferenceResolverInfrastructure : Startで一意探索
```

[体力・操作クラスSVG](diagrams/classes-actors.svg)。Coreの`_isDead`はAwakeで初期化。死亡Damageは体力通知前に確定し、ローカル`becameDead`だけが副作用を発行する。Health/MaxHealthの変更、通常OnEnableでは解除しない。通常被弾の通知/最大体力強化は維持。[シーケンス](architecture-sequences.md)で敵とプレイヤーの死亡順序を区別する。

## Combat：射撃・弾の生成と返却

<!-- diagram-id: architecture-classes-2 -->
```mermaid
classDiagram
    namespace Foundation {
        class PooledAttackBaseGameplay {
          <<abstract>>
          +BulletParametersConfiguration Parameter
          +Action OnReturnToPool
          +OnInitialize(BulletInitializationContextGameplay)*
          +OnGetFromPool()*
        }
        class WeaponAmmoStateDomain {
          +int Capacity
          +int RemainingAmmo
          +bool IsReady
          +WeaponWaitKindDomain PendingWait
          +TryConsumeShot() bool
          +StartWait() WeaponWaitKindDomain
          +CompleteWait() bool
        }
        class BulletSpreadCalculatorDomain {
          <<static>>
          +GetShotCount()
          +GetAngle()
        }
    }
    namespace Implementation {
        class BulletShotGameplay {
          -int _hitCount
          -CancellationTokenSource _cts
          +OnInitialize(BulletInitializationContextGameplay)
          +OnGetFromPool()
        }
        class BulletPoolInfrastructure {
          +Get(BulletTypeDomain)
        }
        class BulletFireSequenceGameplay {
          <<static>>
          +Fire()
        }
    }
    namespace Connection {
        class BulletPrefabCatalogConfiguration {
          +Mappings
          +GetGameObject(BulletTypeDomain)
        }
        class BulletPrefabMappingConfiguration {
          <<struct>>
          +BulletTypeDomain Type
          +GameObject Prefab
        }
        class WeaponDefinitionConfiguration {
          +BulletTypeDomain BulletType
        }
        class BulletInitializationContextGameplay {
          <<struct>>
          +AudioSource AudioSource
        }
        class BulletParametersConfiguration {
          <<struct>>
          +int Damage
          +float Speed
          +float Duration
          +int RicochetCount
        }
        class IDamageableDomain {
          <<interface>>
          +Damage(int)
        }
    }
    namespace External {
        class MonoBehaviour
        class ScriptableObject
        class ObjectPool {
          <<UnityLibrary>>
        }
    }
    MonoBehaviour <|-- PooledAttackBaseGameplay
    PooledAttackBaseGameplay <|-- BulletShotGameplay
    MonoBehaviour <|-- BulletPoolInfrastructure
    ScriptableObject <|-- BulletPrefabCatalogConfiguration
    ScriptableObject <|-- WeaponDefinitionConfiguration
    BulletPoolInfrastructure *-- ObjectPool : 弾種ごとのpool
    ObjectPool --> PooledAttackBaseGameplay : 活動中と待機中の個体を管理
    BulletPoolInfrastructure --> BulletPrefabCatalogConfiguration : prefab検索
    BulletPrefabCatalogConfiguration *-- BulletPrefabMappingConfiguration : List内の値
    PooledAttackBaseGameplay *-- BulletParametersConfiguration : 発射値のコピー
    WeaponDefinitionConfiguration *-- BulletParametersConfiguration : 設定値
    BulletFireSequenceGameplay ..> BulletSpreadCalculatorDomain : 発数と角度
    BulletFireSequenceGameplay ..> BulletPoolInfrastructure : 弾取得
    BulletFireSequenceGameplay ..> WeaponAmmoStateDomain : 残弾消費
    BulletFireSequenceGameplay ..> PooledAttackBaseGameplay : 配置と発射初期化
    BulletShotGameplay ..> IDamageableDomain : Collisionで取得し被弾通知
    BulletPoolInfrastructure ..> BulletInitializationContextGameplay : 生成時に渡す
    PooledAttackBaseGameplay ..> BulletInitializationContextGameplay : 初期化引数
```

[射撃クラスSVG](diagrams/classes-combat.svg)。Mappingはカタログファイル内の値型、ObjectPoolはUnity外部クラス。弾のRigidbody/Transform/音源はUnity側に所属し図では省略。pool作成時に生成親・音源・初期化・返却delegateを設定する。FireはGet（非活動）→値と配置→有効化→OnGetFromPoolの順。GoF Template MethodやAbstract Factoryとは断定しない。

## Progression：状態・定義・表示・選択

<!-- diagram-id: architecture-classes-3 -->
```mermaid
classDiagram
    namespace Foundation {
        class ExperienceProgressionDomain {
          +int Level
          +int AvailableUpgradeCount
          +Gain(int) int
          +TrySpendUpgradeChoice() bool
        }
        class UpgradeCandidateSelectionDomain {
          +int RerollTokens
          +TryDraw() bool
          +TryReroll() bool
        }
    }
    namespace Implementation {
        class LevelUpCoordinatorGameplay {
          -int _killCount
          -bool _isMenuActivated
          -int[] _selectedIds
          +GainExperience(int)
          +TryReroll()
          -SelectUpgrade(int)
        }
        class LevelUpViewPresentation {
          +CreateButtons()
          +ShowCandidates()
          +ShowProgress()
        }
        class PlayerCoreGameplay
    }
    namespace Connection {
        class UpgradeButtonPresentation {
          +Bind()
          +Unbind()
          +Upgrade()
        }
        class UpgradeCatalogConfiguration {
          +CreateSnapshot()
        }
        class UpgradeDefinitionConfiguration {
          +int Id
          +string Title
          +UpgradeParametersConfiguration Effect
          +IsEligible(int) bool
        }
        class UpgradeParametersConfiguration {
          <<struct>>
        }
        class DisplayColumnConfiguration {
          <<struct>>
          +string Label
          +string Value
        }
    }
    namespace External {
        class MonoBehaviour
        class ScriptableObject
    }
    MonoBehaviour <|-- LevelUpCoordinatorGameplay
    MonoBehaviour <|-- LevelUpViewPresentation
    MonoBehaviour <|-- UpgradeButtonPresentation
    ScriptableObject <|-- UpgradeCatalogConfiguration
    ScriptableObject <|-- UpgradeDefinitionConfiguration
    LevelUpCoordinatorGameplay *-- ExperienceProgressionDomain : Awakeで生成
    LevelUpCoordinatorGameplay *-- UpgradeCandidateSelectionDomain : Awakeで生成
    LevelUpCoordinatorGameplay --> LevelUpViewPresentation : 同一Objectに結果描画
    LevelUpCoordinatorGameplay --> UpgradeCatalogConfiguration : 定義配列のsnapshot
    UpgradeCatalogConfiguration --> UpgradeDefinitionConfiguration : 共有SO参照
    LevelUpCoordinatorGameplay --> UpgradeDefinitionConfiguration : ID辞書と条件・効果
    UpgradeDefinitionConfiguration *-- UpgradeParametersConfiguration : 効果値
    UpgradeDefinitionConfiguration *-- DisplayColumnConfiguration : 表示列値
    LevelUpViewPresentation --> UpgradeButtonPresentation : prefab生成・管理・破棄
    UpgradeButtonPresentation --> UpgradeDefinitionConfiguration : 表示とID
    UpgradeButtonPresentation --> LevelUpCoordinatorGameplay : Bindで渡したcallback経由
    LevelUpCoordinatorGameplay --> PlayerCoreGameplay : 効果を適用
```

[進行クラスSVG](diagrams/classes-progression.svg)。候補ID/メニュー/撃破数はCoordinator、経験値/残選択回数はProgression、再抽選トークンはSelection、表示部品はViewが管理する。定義SOの配列とID辞書は参照のコピー。表示列値型はUpgradeDefinitionファイル内の入れ子型。[定義/UI仕様](upgrade-catalog-and-ui.md)に条件と追加手順がある。
