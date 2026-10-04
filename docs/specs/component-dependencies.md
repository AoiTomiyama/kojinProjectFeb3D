# コンポーネントの必須参照と取得範囲

タグ: `現行仕様` `依存関係` `シーン参照` `Prefab` `Unity` `A-07`

## 取得方法

同じGameObjectの部品は呼び出し側の `GetComponent<T>()` で取得する。必須の部品とInspector参照は、初期化時または使用時にその場でnullを確認し、欠落した参照を含むエラーを出す。任意の参照はnullなら処理を省略する。この確認のための共通ヘルパーは作らない。

`Assets/Scripts/SceneReferenceResolver.cs` の `RequireUnique<T>()` は要求元と同じシーンのアクティブなGameObjectから1件取得する。無効化されたコンポーネントも、GameObjectがアクティブなら検索対象になる。0件または複数件なら、シーン名、要求元、必要な型と件数を含むエラーを出す。nullチェックだけでは複数配置を検出できないため、シーン検索の件数検証は共通処理として維持する。

取得済みのCoreが破棄された場合、購読解除のためのアクセスで再検索はしない。

体力・弾数UIは初回のStartで通知元を保持し、有効な期間だけ購読する。OnDisableでは保持した参照から解除し、再有効化時は現在値を表示する。待機バーの復元に必要な状態はPlayerAttackが読み取り専用で公開する。詳細は [UIの購読期間](player-stats-and-upgrades.md#体力弾数uiの購読期間) を参照する。

## 現行の依存関係

機能ごとの状態の所有者、通知と寿命、拡張時の変更箇所は [ゲーム機能の責務・依存方向と拡張箇所](gameplay-architecture.md) を参照する。この一覧は必須参照の取得方法を示す。

| 要求元 | 同じGameObjectから取得 | Inspector指定・外部から渡す参照 | 同じシーンで1件検索 |
| --- | --- | --- | --- |
| `PlayerCore` | `PlayerMove`、`PlayerAttack` | `PlayerInitialStats`、死亡エフェクト | なし |
| `PlayerMove` | `PlayerCore`、`Rigidbody`、`LineRenderer` | `_lookAt`（任意） | `Camera` |
| `PlayerAttack` | 共通基底の `PlayerCore` | `WeaponDefinition`、発射口 | 弾プール、レベル管理 |
| `EnemyCore` | なし | 死亡エフェクト | `PlayerCore`、レベル管理 |
| `EnemyAttack`、`EnemyMove` | 共通基底の `EnemyCore`、移動側の `NavMeshAgent` | 攻撃側の武器設定・発射口 | 攻撃側の弾プール |
| `PlayerUIViewer`、`EnemyUIViewer` | 共通基底のCore。プレイヤーUIはCoreが取得した攻撃部品を使用 | バー、文字などの表示先 | なし |
| `LevelUpSystemManager` | `LevelUpUIView` | 必要経験値リスト | `PlayerCore` |
| `LevelUpUIView` | ボタン配置先の子から候補ボタンを取得 | バー、文字、パネル、ボタン配置先 | なし |
| `CursorPointer`、`LookAtCamera` | なし | 照準の物理判定設定 | `Camera`、照準側のレベル管理 |
| `UpgradeButtonBehaviour` | なし | 値型の強化設定 | レベル管理 |
| `FollowObject` | なし | 追従対象、位置の補正値 | なし |
| `BulletObjectPoolManager` | 生成した弾の `PooledAttackBase` | 弾データベース、効果音用 `AudioSource` | なし |
| `BulletShotBehaviour` | `Rigidbody` | 命中エフェクト、ダメージ文字、発射音。効果音出力はプールから注入 | なし |

Coreと行動部品は同じGameObjectに置く。シーン固有の参照をPrefabへ直接保存できない動的な敵・弾では、初期化時のシーン検索と生成元からの注入を使用する。`PlayerMove._lookAt` は未設定なら旋回を行わない既存仕様を維持する。

加算ロードした別シーンや `DontDestroyOnLoad` のオブジェクトは `RequireUnique<T>()` の取得先にしない。将来シーンをまたぐサービスを導入する場合は、生成元やInspectorから渡す依存関係を別途明示する。

## 設定と検証

`InGame.unity` はプレイヤー、Camera、レベル管理、弾プールを各1件配置し、弾プールの効果音出力を設定する。`Player.prefab`、`Enemy.prefab`、派生PrefabはCoreと行動部品を同じGameObjectに保持し、武器設定と発射口への参照を保存する。敵UIはPrefab、プレイヤーUIはシーン側で同じGameObjectに配置する。参照整理では既存のアセットGUIDや配置を変更しない。

死亡エフェクトと命中エフェクト・ダメージ文字は、使用時にInspector参照を検証する。プレイヤーの死亡エフェクトは `InGame` の配置個体に設定されるため、Prefab単体の生成時には要求しない。

検証結果は [プレイヤー能力値と強化の現行仕様](player-stats-and-upgrades.md) と [設計改善タスク](../tasks/architecture-improvement.md) のA-07に記録する。
