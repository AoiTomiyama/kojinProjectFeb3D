# 自作スクリプト・クラス対応表

タグ: `現行仕様` `スクリプト一覧` `責務` `依存関係` `実装根拠`

対象版/日付/差分範囲は[図式索引](architecture-diagrams.md)。37ランタイムファイルを全件掲載。型名の各リンクが実ファイルで、根拠入口は依存列に記す。基底/契約欄はC#宣言であり、UnityのRequireComponentとは区別する。設定SOは共有参照、Domain状態は各所有者がnewする。

## Camera/Presentation

| 型/実ファイル | 基底・契約・形式 | 役割 | 主要依存/根拠入口 |
| --- | --- | --- | --- |
| [CameraBillboardPresentation](../../Assets/Scripts/Camera/Presentation/CameraBillboardPresentation.cs) | MonoBehaviour | 対象をカメラに正対させる | Start/Update：`RequireUnique<Camera>`、Transform |
| [CameraFollowPresentation](../../Assets/Scripts/Camera/Presentation/CameraFollowPresentation.cs) | MonoBehaviour | 指定対象への追従 | Start/Update：Inspector Transformとoffset |

## Combat/Configuration

| 型/実ファイル | 基底・契約・形式 | 役割 | 主要依存/根拠入口 |
| --- | --- | --- | --- |
| [BulletParametersConfiguration](../../Assets/Scripts/Combat/Configuration/BulletParametersConfiguration.cs) | Serializable struct | 弾のダメージ・速度・寿命・反射設定値 | Duration：Unity属性/Mathf、個体へ値copy |
| [BulletPrefabCatalogConfiguration](../../Assets/Scripts/Combat/Configuration/BulletPrefabCatalogConfiguration.cs) | ScriptableObject | 弾種とPrefabの対応検索 | Mappings/GetGameObject：BulletTypeDomain、GameObject。入れ子struct BulletPrefabMappingConfiguration(Type/Prefab) |
| [WeaponDefinitionConfiguration](../../Assets/Scripts/Combat/Configuration/WeaponDefinitionConfiguration.cs) | ScriptableObject | 武器の弾種・初期容量・発数・角度・待機・弾設定 | getters：BulletType、BulletParametersConfiguration。弾の値は個体へcopy |

## Combat/Contracts

| 型/実ファイル | 基底・契約・形式 | 役割 | 主要依存/根拠入口 |
| --- | --- | --- | --- |
| [IDamageableDomain](../../Assets/Scripts/Combat/Contracts/IDamageableDomain.cs) | interface | 衝突対象の被弾契約 | Damage(int)：EnemyCore/PlayerCoreが実装 |

## Combat/Domain

| 型/実ファイル | 基底・契約・形式 | 役割 | 主要依存/根拠入口 |
| --- | --- | --- | --- |
| [BulletSpreadCalculatorDomain](../../Assets/Scripts/Combat/Domain/BulletSpreadCalculatorDomain.cs) | static class | 残弾制限した発数と拡散角 | GetShotCount/GetAngle：数値だけ、Unity非依存 |
| [BulletTypeDomain](../../Assets/Scripts/Combat/Domain/BulletTypeDomain.cs) | enum | PlayerBullet/EnemyBulletの弾種キー | カタログ・pool・攻撃の識別 |
| [WeaponAmmoStateDomain](../../Assets/Scripts/Combat/Domain/WeaponAmmoStateDomain.cs) | sealed class | 個体ごとの残弾・射撃可能・待機種類 | TryConsumeShot/StartWait/CompleteWait：WeaponWaitKindDomain、Systemのみ |
| [WeaponWaitKindDomain](../../Assets/Scripts/Combat/Domain/WeaponWaitKindDomain.cs) | enum | None/Cooldown/Reloadの待機値 | WeaponAmmoStateDomain、両Attack |

## Combat/Gameplay

| 型/実ファイル | 基底・契約・形式 | 役割 | 主要依存/根拠入口 |
| --- | --- | --- | --- |
| [BulletFireSequenceGameplay](../../Assets/Scripts/Combat/Gameplay/BulletFireSequenceGameplay.cs) | static class | 両陣営共通の発射手順 | Fire：Spread、Pool、PooledAttackBase、Ammo、Transform/Quaternion、任意Action<int> |
| [BulletShotGameplay](../../Assets/Scripts/Combat/Gameplay/BulletShotGameplay.cs) | PooledAttackBaseGameplay | 速度・寿命・命中・演出と返却 | OnInitialize/OnGetFromPool/OnCollisionEnter/OnDisable：Rigidbody、UniTask/CTS、IDamageable、AudioSource、TMP |
| [BulletInitializationContextGameplay](../../Assets/Scripts/Combat/Gameplay/BulletInitializationContextGameplay.cs) | readonly struct | プール生成時に渡すシーンの依存 | 任意のAudioSource。必要性は派生弾が判断 |
| [PooledAttackBaseGameplay](../../Assets/Scripts/Combat/Gameplay/PooledAttackBaseGameplay.cs) | abstract MonoBehaviour | pool対象の設定・初期化フック・返却契約 | Parameter、OnReturnToPool、抽象OnInitialize(context)/OnGetFromPool |

## Combat/Infrastructure

| 型/実ファイル | 基底・契約・形式 | 役割 | 主要依存/根拠入口 |
| --- | --- | --- | --- |
| [BulletPoolInfrastructure](../../Assets/Scripts/Combat/Infrastructure/BulletPoolInfrastructure.cs) | MonoBehaviour | 弾種ごとの生成・再利用・音注入 | Start/InitPool/Get/Release：ObjectPool、カタログ、PooledAttackBase、BulletInitializationContext、AudioSource |

## Effects/Presentation

| 型/実ファイル | 基底・契約・形式 | 役割 | 主要依存/根拠入口 |
| --- | --- | --- | --- |
| [EffectSelfDestructionPresentation](../../Assets/Scripts/Effects/Presentation/EffectSelfDestructionPresentation.cs) | MonoBehaviour | 生成した演出Objectを終了する入口 | DestroySelf：Destroy(gameObject)。呼出元はアセットのイベント配線、図では特定の起動元を仮定しない |

## Enemy/Gameplay

| 型/実ファイル | 基底・契約・形式 | 役割 | 主要依存/根拠入口 |
| --- | --- | --- | --- |
| [EnemyAttackGameplay](../../Assets/Scripts/Enemy/Gameplay/EnemyAttackGameplay.cs) | EnemyComponentBaseGameplay | 敵射線/射程に応じた射撃と取消待機 | Awake/Start/Update/OnEnable/OnDisable：Core、WeaponDefinition、Ammo、Fire、Pool、Physics、UniTask/CTS |
| [EnemyComponentBaseGameplay](../../Assets/Scripts/Enemy/Gameplay/EnemyComponentBaseGameplay.cs) | MonoBehaviour | 同じObjectのEnemyCore参照を遅延取得/cache | Core：GetComponent、取得済みフラグ。破棄済み参照を再探索しない |
| [EnemyCoreGameplay](../../Assets/Scripts/Enemy/Gameplay/EnemyCoreGameplay.cs) | MonoBehaviour + IDamageableDomain | 体力・死亡ラッチ・対象/射程/レイヤー・死亡報酬 | Awake/Start/Damage：Resolver、PlayerCore、Coordinator、Action/UnityEvent、effect Prefab |
| [EnemyMoveGameplay](../../Assets/Scripts/Enemy/Gameplay/EnemyMoveGameplay.cs) | EnemyComponentBaseGameplay | 感知・NavMesh追跡・射程内停止/旋回 | Start/Update：Core、NavMeshAgent、Physics。StartでAgent有効化 |

## Enemy/Presentation

| 型/実ファイル | 基底・契約・形式 | 役割 | 主要依存/根拠入口 |
| --- | --- | --- | --- |
| [EnemyHealthViewPresentation](../../Assets/Scripts/Enemy/Presentation/EnemyHealthViewPresentation.cs) | EnemyComponentBaseGameplay | 体力比率を描画、購読期間を管理 | Start/OnEnable/OnDisable：Core.OnHealthChanged、UI Image |

## Player/Configuration

| 型/実ファイル | 基底・契約・形式 | 役割 | 主要依存/根拠入口 |
| --- | --- | --- | --- |
| [PlayerInitialStatsConfiguration](../../Assets/Scripts/Player/Configuration/PlayerInitialStatsConfiguration.cs) | sealed ScriptableObject | 最大体力・移動速度の初期値 | MaxHealth/MoveSpeed getter：Core/Moveがcopy |

## Player/Gameplay

| 型/実ファイル | 基底・契約・形式 | 役割 | 主要依存/根拠入口 |
| --- | --- | --- | --- |
| [PlayerAimPointerGameplay](../../Assets/Scripts/Player/Gameplay/PlayerAimPointerGameplay.cs) | MonoBehaviour | mouse rayヒット位置へ照準指標を移動 | Start/Update：Resolver、Camera、Coordinator、Input/Physics/Transform |
| [PlayerAttackGameplay](../../Assets/Scripts/Player/Gameplay/PlayerAttackGameplay.cs) | PlayerComponentBaseGameplay | 入力/メニューに応じた射撃・武器強化・UI通知 | Awake/Start/Update/Wait系/ApplyPowerUp/OnDisable：WeaponDefinition、Ammo、Fire、Pool、Coordinator、UniTask/CTS |
| [PlayerComponentBaseGameplay](../../Assets/Scripts/Player/Gameplay/PlayerComponentBaseGameplay.cs) | MonoBehaviour | 同じObjectのPlayerCore参照を遅延取得/cache | Core：GetComponent、取得済みフラグ |
| [PlayerCoreGameplay](../../Assets/Scripts/Player/Gameplay/PlayerCoreGameplay.cs) | MonoBehaviour + IDamageableDomain | 体力/死亡ラッチ・強化の振分 | Awake/Start/Damage/ApplyPowerUp：InitialStats、Move、Attack、Action/UnityEvent、effect Prefab |
| [PlayerMoveGameplay](../../Assets/Scripts/Player/Gameplay/PlayerMoveGameplay.cs) | PlayerComponentBaseGameplay | 入力移動・照準旋回・射線表示、個体速度 | Awake/Start/Update：Core.InitialStats、Rigidbody、LineRenderer、Camera/Resolver、Input、lookAt Transform |

## Player/Presentation

| 型/実ファイル | 基底・契約・形式 | 役割 | 主要依存/根拠入口 |
| --- | --- | --- | --- |
| [PlayerStatusViewPresentation](../../Assets/Scripts/Player/Presentation/PlayerStatusViewPresentation.cs) | PlayerComponentBaseGameplay | 体力/残弾/待機表示・購読と復元 | Start/OnEnable/OnDisable：Core/Attack、UI/TMP、DOTween。表示のみ |

## Progression/Configuration

| 型/実ファイル | 基底・契約・形式 | 役割 | 主要依存/根拠入口 |
| --- | --- | --- | --- |
| [UpgradeCatalogConfiguration](../../Assets/Scripts/Progression/Configuration/UpgradeCatalogConfiguration.cs) | sealed ScriptableObject | 強化定義一覧とID/参照の検証 | CreateSnapshot：UpgradeDefinition配列copy、正ID/重複/欠落検証 |
| [UpgradeDefinitionConfiguration](../../Assets/Scripts/Progression/Configuration/UpgradeDefinitionConfiguration.cs) | sealed ScriptableObject | ID/表示列/候補条件/効果の共有定義 | IsEligible/getters：UpgradeParameters、入れ子Serializable struct DisplayColumnConfiguration(Label/Value) |
| [UpgradeParametersConfiguration](../../Assets/Scripts/Progression/Configuration/UpgradeParametersConfiguration.cs) | Serializable struct | 強化の加算/乗算値 | Core/Attackに渡す値型。取得済み状態なし |

## Progression/Domain

| 型/実ファイル | 基底・契約・形式 | 役割 | 主要依存/根拠入口 |
| --- | --- | --- | --- |
| [ExperienceProgressionDomain](../../Assets/Scripts/Progression/Domain/ExperienceProgressionDomain.cs) | sealed class | 経験値・レベル・選択回数の確定 | Gain/TrySpendUpgradeChoice：必要経験値配列copy、Systemのみ |
| [UpgradeCandidateSelectionDomain](../../Assets/Scripts/Progression/Domain/UpgradeCandidateSelectionDomain.cs) | sealed class | 再抽選token・重複しない3ID選択 | GrantRerollToken/TryDraw/TryReroll：int配列、乱数Func、Systemのみ |

## Progression/Gameplay

| 型/実ファイル | 基底・契約・形式 | 役割 | 主要依存/根拠入口 |
| --- | --- | --- | --- |
| [LevelUpCoordinatorGameplay](../../Assets/Scripts/Progression/Gameplay/LevelUpCoordinatorGameplay.cs) | MonoBehaviour | 撃破・進行・メニュー・候補ID・強化適用の調整 | Awake/Start/GainExperience/SelectUpgrade/TryReroll：Progression/Selection、Catalog/Definition、View、Player、Resolver、Random |

## Progression/Presentation

| 型/実ファイル | 基底・契約・形式 | 役割 | 主要依存/根拠入口 |
| --- | --- | --- | --- |
| [LevelUpViewPresentation](../../Assets/Scripts/Progression/Presentation/LevelUpViewPresentation.cs) | MonoBehaviour | 進行値描画と強化ボタン生成/破棄 | Initialize/CreateButtons/Show*：UpgradeButton Prefab、Definition、UI/TMP、DOTween、AudioSource |
| [UpgradeButtonPresentation](../../Assets/Scripts/Progression/Presentation/UpgradeButtonPresentation.cs) | MonoBehaviour | 定義の表示列生成、選択IDを通知 | Bind/Unbind/Upgrade：Definition、Action<int>、TMP/UI。Prefab onClick接続 |

## Scene/Infrastructure

| 型/実ファイル | 基底・契約・形式 | 役割 | 主要依存/根拠入口 |
| --- | --- | --- | --- |
| [SceneReferenceResolverInfrastructure](../../Assets/Scripts/Scene/Infrastructure/SceneReferenceResolverInfrastructure.cs) | static class | 同一シーン活動中の一意参照を要求/診断 | `RequireUnique<T>`：Unity Object.FindObjectsByType、scene filter。global cacheなし |


## Editor・テスト・外部の境界

ランタイムからUnityEditor/NUnit/テストへの参照はない。既存テスト7クラスはR-01検証で61件成功した対象。今回は文書の構文/参照照合のみでテストを再実行していない。

| ファイル | 役割/入口 |
| --- | --- |
| [BulletLifetimeTests](../../Assets/Editor/BulletLifetimeTests.cs) | 弾の寿命取消・返却と破棄 |
| [DomainRulesTests](../../Assets/Editor/DomainRulesTests.cs) | 4純粋C#ルールの境界条件 |
| [ProjectAssetReferenceTests](../../Assets/Editor/ProjectAssetReferenceTests.cs) | Scene/Prefab/SO/GUID等の参照、BuildWindowsPlayer：Windows Developmentビルド入口 |
| [SceneDependencyTests](../../Assets/Editor/SceneDependencyTests.cs) | シーン一意参照と欠落/重複診断 |
| [SceneGameplayTests](../../Assets/Editor/SceneGameplayTests.cs) | シーン射撃・移動・進行とR-01死亡/再入の回帰 |
| [UiEventLifetimeTests](../../Assets/Editor/UiEventLifetimeTests.cs) | UI購読解除・再有効化・表示復元 |
| [UpgradeCatalogEditorInfrastructure](../../Assets/Editor/UpgradeCatalogEditorInfrastructure.cs) | CustomEditor：一覧のSerializedObject編集、CreateSnapshot検証。ランタイムUIではない |
| [UpgradeCatalogTests](../../Assets/Editor/UpgradeCatalogTests.cs) | SO一覧/候補条件/動的ボタンと選択回帰 |
| [VfxShaderRenderingTests](../../Assets/Editor/VfxShaderRenderingTests.cs) | 未追跡V-02差分の描画検証。完了済み7クラス/61件に含めない |

UnityEngine/AI/Pool/UI、TMP、UniTask、DOTweenは外部実装として扱う。URP/lilToon/VFXは描画資産境界。外部ソース内部のクラス図やAssets/AssetStoreTools全件は対象外。このUnity対象の自作ランタイムにC++/Python実装はない。生成手順に用いたPython/Nodeはゲーム依存として追加しない。
