# スクリプトの機能別配置とレイヤー接尾辞

タグ: `現行仕様` `命名規則` `フォルダー構造` `設計境界` `Unity`

- 決定・適用日: 2026-10-06
- 対象: プロジェクト固有のゲームコード `Assets/Scripts/`。Editor用テストは `Assets/Editor/` に置き、`Tests` 接尾辞を使う。外部ライブラリ・サンプルコードは各パッケージの命名に従う。
- 根拠: 機能名を先頭に残し、名前順で検索しやすくするというユーザーの決定。責務の区分は [ゲーム機能の設計](gameplay-architecture.md) に合わせる。

## 命名規則

型名・ファイル名は **対象機能 + 役割 + レイヤー名** とする。例: `PlayerStatusViewPresentation.cs`、`WeaponAmmoStateDomain.cs`。レイヤー名は末尾に付け、省略しない。インターフェースは先頭の `I` を維持する。

| 接尾辞 | 所属条件 | 例 |
| --- | --- | --- |
| `Domain` | Unity非依存のルール・状態・計算・契約 | `WeaponAmmoStateDomain`、`BulletTypeDomain`、`IDamageableDomain` |
| `Gameplay` | ゲーム進行、入力・物理・時間待機、部品間の調整 | `PlayerAttackGameplay`、`LevelUpCoordinatorGameplay` |
| `Presentation` | 表示・演出、UI操作の受付 | `PlayerStatusViewPresentation`、`UpgradeButtonPresentation` |
| `Configuration` | 初期設定やInspector向けの設定データ | `WeaponDefinitionConfiguration`、`UpgradeParametersConfiguration` |
| `Infrastructure` | プールや参照取得などの基盤 | `BulletPoolInfrastructure`、`SceneReferenceResolverInfrastructure` |

`View`、`State`、`Calculator`、`Coordinator`、`Base` など役割を表す語を残す。敵Coreの体力計算など、Unity操作を含む既存実装はGameplayに分類する。純粋C#へ抽出した際には、新しいクラスをDomainに置く。分類だけを理由に処理を移さない。

`BulletParametersConfiguration` はUnityの属性とMathfを使用する設定値型であり、Domainとは扱わない。`UpgradeParametersConfiguration` は純粋な値型だが、強化の計算ではなく入力設定を表すためConfigurationに分類する。ネストした `BulletPrefabMappingConfiguration` も設定型として命名する。

## フォルダー構造

機能を上位、その中にレイヤーを置く。複数機能が使う処理は用途に沿った機能へまとめる。空のレイヤーフォルダーは作らない。

```text
Assets/
├─ Scripts/
│  ├─ Player/       Configuration / Gameplay / Presentation
│  ├─ Enemy/        Gameplay / Presentation
│  ├─ Progression/  Configuration / Domain / Gameplay / Presentation
│  ├─ Combat/       Configuration / Contracts / Domain / Gameplay / Infrastructure
│  ├─ Camera/       Presentation
│  ├─ Effects/      Presentation
│  └─ Scene/        Infrastructure
├─ GameData/
│  ├─ Player/       プレイヤー初期体力・速度の設定アセット
│  └─ Combat/       武器設定・弾Prefab対応表の設定アセット
└─ Editor/          継続利用するEditorテスト
```

- PlayerとEnemyは専用の部品を持つ。残弾・拡散・弾・ダメージ契約はCombat、経験値と強化選択はProgressionに置く。
- Contractsは共有する呼び出し契約の置き場であり、独立したレイヤーではない。現在の `IDamageableDomain` はUnity非依存の契約である。
- 基底クラスは使用する機能へ置く。プレイヤー用基底はPlayer/Gameplay、敵用基底はEnemy/Gameplay、プール弾用基底はCombat/Gameplay。
- ScriptableObjectのC#定義はScripts内のConfiguration、値を保存した `.asset` はGameDataに置く。設定アセットの名前には個体・用途を表す名前を使い、型の接尾辞を強制しない。
- この変更はフォルダーと型名による区分であり、名前空間・asmdefは追加していない。

## 移行時に維持するもの

既存のC#33ファイルと設定アセット8件は、対応する `.meta` を一緒に移動し、41件のGUIDとメタデータ内容を保持した。既存のシリアライズされたフィールド名、公開メソッド名、ゲーム上の計算・挙動は維持する。武器待機の列挙型は `WeaponWaitKindDomain.cs` に分けた。

UnityEventが保存する型名はシーン・Prefab内の `m_TargetAssemblyTypeName` を更新する。ScriptableObjectとMonoBehaviourの参照は既存GUIDを使う。弾対応表は `GameData/Combat/BulletPrefabCatalog.asset` に移し、アセット名も合わせる。コード・テスト・現行仕様の参照先を同時に更新する。

今後ファイルを追加するときは機能・所有する状態・Unity依存の有無から配置と接尾辞を決め、[設計の拡張手順](gameplay-architecture.md#機能追加時の変更箇所と確認) と関連する機能仕様を更新する。

## 検証と再開情報

実施結果・確認の制約・コミットの分割予定は [整理作業の記録](../tasks/script-layout-migration.md) に記録する。アセット参照の確認は `ProjectAssetReferenceTests`、シーンの起動とUI通知の確認は `UiEventLifetimeTests` を継続利用する。

## 改名・移動一覧

以下は移行前の型名と移行先。旧名は履歴照合用であり、現行コードから使用しない。

| 旧名 | 現在の型名とファイル |
| --- | --- |
| `BulletFireSequence` | [BulletFireSequenceGameplay](../../Assets/Scripts/Combat/Gameplay/BulletFireSequenceGameplay.cs) |
| `BulletObjectPoolManager` | [BulletPoolInfrastructure](../../Assets/Scripts/Combat/Infrastructure/BulletPoolInfrastructure.cs) |
| `BulletParameter` | [BulletParametersConfiguration](../../Assets/Scripts/Combat/Configuration/BulletParametersConfiguration.cs) |
| `BulletShotBehaviour` | [BulletShotGameplay](../../Assets/Scripts/Combat/Gameplay/BulletShotGameplay.cs) |
| `BulletSpread` | [BulletSpreadCalculatorDomain](../../Assets/Scripts/Combat/Domain/BulletSpreadCalculatorDomain.cs) |
| `BulletTypeEnum` | [BulletTypeDomain](../../Assets/Scripts/Combat/Domain/BulletTypeDomain.cs) |
| `CursorPointer` | [PlayerAimPointerGameplay](../../Assets/Scripts/Player/Gameplay/PlayerAimPointerGameplay.cs) |
| `EnemyAttack` | [EnemyAttackGameplay](../../Assets/Scripts/Enemy/Gameplay/EnemyAttackGameplay.cs) |
| `EnemyComponentBase` | [EnemyComponentBaseGameplay](../../Assets/Scripts/Enemy/Gameplay/EnemyComponentBaseGameplay.cs) |
| `EnemyCore` | [EnemyCoreGameplay](../../Assets/Scripts/Enemy/Gameplay/EnemyCoreGameplay.cs) |
| `EnemyMove` | [EnemyMoveGameplay](../../Assets/Scripts/Enemy/Gameplay/EnemyMoveGameplay.cs) |
| `EnemyUIViewer` | [EnemyHealthViewPresentation](../../Assets/Scripts/Enemy/Presentation/EnemyHealthViewPresentation.cs) |
| `EnumToObjectDatabase` | [BulletPrefabCatalogConfiguration](../../Assets/Scripts/Combat/Configuration/BulletPrefabCatalogConfiguration.cs) |
| `ExperienceProgression` | [ExperienceProgressionDomain](../../Assets/Scripts/Progression/Domain/ExperienceProgressionDomain.cs) |
| `FollowObject` | [CameraFollowPresentation](../../Assets/Scripts/Camera/Presentation/CameraFollowPresentation.cs) |
| `IDamageable` | [IDamageableDomain](../../Assets/Scripts/Combat/Contracts/IDamageableDomain.cs) |
| `LevelUpSystemManager` | [LevelUpCoordinatorGameplay](../../Assets/Scripts/Progression/Gameplay/LevelUpCoordinatorGameplay.cs) |
| `LevelUpUIView` | [LevelUpViewPresentation](../../Assets/Scripts/Progression/Presentation/LevelUpViewPresentation.cs) |
| `LookAtCamera` | [CameraBillboardPresentation](../../Assets/Scripts/Camera/Presentation/CameraBillboardPresentation.cs) |
| `PlayerAttack` | [PlayerAttackGameplay](../../Assets/Scripts/Player/Gameplay/PlayerAttackGameplay.cs) |
| `PlayerComponentBase` | [PlayerComponentBaseGameplay](../../Assets/Scripts/Player/Gameplay/PlayerComponentBaseGameplay.cs) |
| `PlayerCore` | [PlayerCoreGameplay](../../Assets/Scripts/Player/Gameplay/PlayerCoreGameplay.cs) |
| `PlayerInitialStats` | [PlayerInitialStatsConfiguration](../../Assets/Scripts/Player/Configuration/PlayerInitialStatsConfiguration.cs) |
| `PlayerMove` | [PlayerMoveGameplay](../../Assets/Scripts/Player/Gameplay/PlayerMoveGameplay.cs) |
| `PlayerUIViewer` | [PlayerStatusViewPresentation](../../Assets/Scripts/Player/Presentation/PlayerStatusViewPresentation.cs) |
| `PooledAttackBase` | [PooledAttackBaseGameplay](../../Assets/Scripts/Combat/Gameplay/PooledAttackBaseGameplay.cs) |
| `PowerUpParameter` | [UpgradeParametersConfiguration](../../Assets/Scripts/Progression/Configuration/UpgradeParametersConfiguration.cs) |
| `SceneReferenceResolver` | [SceneReferenceResolverInfrastructure](../../Assets/Scripts/Scene/Infrastructure/SceneReferenceResolverInfrastructure.cs) |
| `SelfDestruction` | [EffectSelfDestructionPresentation](../../Assets/Scripts/Effects/Presentation/EffectSelfDestructionPresentation.cs) |
| `UpgradeButtonBehaviour` | [UpgradeButtonPresentation](../../Assets/Scripts/Progression/Presentation/UpgradeButtonPresentation.cs) |
| `UpgradeCandidateSelection` | [UpgradeCandidateSelectionDomain](../../Assets/Scripts/Progression/Domain/UpgradeCandidateSelectionDomain.cs) |
| `WeaponAmmoState` | [WeaponAmmoStateDomain](../../Assets/Scripts/Combat/Domain/WeaponAmmoStateDomain.cs) |
| `WeaponDefinition` | [WeaponDefinitionConfiguration](../../Assets/Scripts/Combat/Configuration/WeaponDefinitionConfiguration.cs) |
| `WeaponWaitKind` | [WeaponWaitKindDomain](../../Assets/Scripts/Combat/Domain/WeaponWaitKindDomain.cs)（元の残弾管理ファイルから分離） |
| `EnumToGameObjectPair` | `BulletPrefabMappingConfiguration`（弾Prefab対応表のネスト型） |
