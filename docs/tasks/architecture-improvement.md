# 拡張性を高める設計改善タスク

タグ: `設計改善` `純粋C#` `責務分離` `Unity` `再開情報`

## 記録の前提

- 対象: `Assets/Scripts/` の現行コード。2026-10-01 の静的調査と設計検討に基づく。A-01〜A-08 は実装・検証済み、A-09 は現行コードの静的照合と文書化を完了した（2026-10-04）。
- A-01〜A-09 は採用済みのタスク。A-10 以降は純粋C#化を広げるための追加候補で、採用は未決定。
- 完了後に目指す責務と依存方向は [目標アーキテクチャと機能追加の指針](../knowledge/target-architecture.md) に記録する。各タスクの実装状況はこの一覧で管理する。
- 既存の不具合修正と再生確認は [プログラム設計レビューの改修タスク](program-design-review.md) に記録する。特に関連する D-01〜D-09 の再生確認結果を、対応箇所の改修前に残す。
- 「純粋C#」は `MonoBehaviour`、`UnityEngine`、シーン、Prefab、Unityのイベント関数に依存しないクラスを指す。Unityの入力・物理・描画・オブジェクト寿命は既存のコンポーネント側が担当する。
- 設定値とプレイ中に変わる値を混同しない。移行時は現行の加算・乗算順、再装填時の挙動、最大レベル到達時の扱いを維持し、変更が必要なら仕様を先に明記する。

## 採用済みタスク

### [x] A-01 経験値とレベルの計算を純粋C#へ分離

- 現状: `LevelUpCoordinatorGameplay.GainExperience()` が経験値の加算、レベル判定、強化選択回数の更新、UIと演出を続けて行う。
- 作業: 必要経験値、現在経験値、レベル、獲得した強化選択回数を扱うクラスを分離する。`LevelUpCoordinatorGameplay` は敵撃破からの入力と結果の表示を担当する。
- 完了条件: 連続獲得、複数レベル上昇、最終レベル、無効な必要経験値で、現行仕様に沿った結果をUnityなしで検証できる。シーン上の表示も維持する。
- 参照: `Assets/Scripts/Progression/Gameplay/LevelUpCoordinatorGameplay.cs`、`docs/specs/player-stats-and-upgrades.md`。
- 実施結果（2026-10-01）: `ExperienceProgressionDomain` に経験値、レベル、未使用の強化選択回数を移し、強化ボタンは回数の消費が成功した場合に適用する。.NET コンソールで境界値と連続獲得を検証し、Unity 2022.3.62f2 のPlayモードで現行シーンのレベル・バー・選択回数の表示を確認した。証拠と動作は [現行仕様](../specs/player-stats-and-upgrades.md) に記録した。

### [x] A-02 強化候補の抽選を純粋C#へ分離

- 現状: `LevelUpCoordinatorGameplay.Reroll()` がボタンの表示変更と候補の抽選を同時に行う。
- 作業: 候補IDから重複のない3件を選ぶ処理を分離し、乱数の与え方を明示する。トークン消費は抽選成功後に確定する。ボタンの表示・順序変更はUnity側に置く。
- 完了条件: 候補不足、重複防止、トークン不足と抽選失敗をUnityなしで検証できる。選択後と再抽選後の表示をシーンで確認する。
- 依存: A-03 と表示責務の境界を合わせる。
- 実施結果（2026-10-01）: `UpgradeCandidateSelectionDomain` が候補IDの抽選と再抽選トークンを管理する。.NET コンソールで重複防止・候補不足・残高維持を検証し、Unity 2022.3.62f2 のPlayモードで初回・再抽選後・強化選択後の3件表示と、候補不足時のエラー・残高維持を確認した。GUI操作の確認は D-01 に残す。

### [x] A-03 経験値・強化UIを表示専用に整理

- 現状: `LevelUpCoordinatorGameplay` がゲーム状態と、経験値バー・レベル文字・候補ボタンの表示を保持する。
- 作業: 確定した状態を表示するコンポーネントを分ける。DOTween は表示を変えるだけにし、計算結果の確定を待たせない。
- 完了条件: 経験値、レベル、強化選択回数、候補、トークンの表示が状態と一致する。表示開始順やアニメーションの中断で状態が変わらない。
- 依存: A-01、A-02 の結果を受け取れる形にする。
- 実施結果（2026-10-01）: `LevelUpUIView` がバー・文字・候補ボタン・メニューの表示を担当し、`LevelUpCoordinatorGameplay` は確定済みの状態を渡す。`InGame.unity` の参照を表示側へ移設した。Unity 2022.3.62f2 のバッチPlayで改修前後の表示を比較し、演出中断時も経験値状態が変わらないことを確認した。詳細は [現行仕様](../specs/player-stats-and-upgrades.md) を参照する。

### [x] A-04 プレイヤーと敵の弾数・再装填状態を共通化

- 現状: `PlayerAttackGameplay` と `EnemyAttackGameplay` に残弾、発射間隔、再装填の判定が重複している。
- 作業: 残弾と射撃可能状態の遷移を純粋C#クラスへまとめる。待機時間の実行とキャンセルはUnity側で管理する。プレイヤーと敵で異なる入力・射撃条件はそれぞれに残す。
- 完了条件: 発射、弾切れ、再装填、無効化・再有効化時の状態遷移をUnityなしで検証し、両者の射撃をシーンで確認する。
- 参照: `Assets/Scripts/Player/Gameplay/PlayerAttackGameplay.cs`、`Assets/Scripts/Enemy/Gameplay/EnemyAttackGameplay.cs`。
- 実施結果（2026-10-01）: 純粋C#の `WeaponAmmoStateDomain` に残弾、射撃可能状態、発射間隔と再装填の判定を集約し、両攻撃コンポーネントは非同期待機とキャンセル、射撃条件を担当する。.NET コンソールで状態遷移を検証し、Unity 2022.3.62f2 のバッチPlayで改修前後の両者の射撃、弾切れ、再装填、無効化・再有効化を比較した。結果と既存の命中エフェクト例外は [現行仕様](../specs/player-stats-and-upgrades.md) に記録した。

### [x] A-05 弾の拡散計算と発射手順を分離

- 現状: `PlayerAttackGameplay.Shoot()` と `EnemyAttackGameplay.Shoot()` が弾の取得、角度計算、配置、初期化をほぼ同じ手順で行う。
- 作業: 弾数と拡散角から各弾の角度を求める計算を純粋C#へ分離する。角度から向きへの変換、弾プールの取得、Transformの設定、効果音を伴う初期化はUnity側の共通処理へまとめる。
- 完了条件: 単発・複数発・弾数不足時の角度と発射数を検証できる。プレイヤー・敵の弾の向き、消費弾数、初期化をシーンで確認する。
- 依存: A-04 の弾数管理と発射可能判定を使用する。
- 実施結果（2026-10-02）: `BulletSpreadCalculatorDomain` が同時発射数・残弾から発射数と角度を算出し、`BulletFireSequenceGameplay` が両攻撃コンポーネントの弾プール取得、配置、初期化、残弾消費を共通化した。単発・複数発・残弾不足の角度と発射数を .NET コンソールで確認し、両者の弾の向き、弾数、発射時初期化を Unity 2022.3.62f2 のバッチPlayで確認した。動作と証拠は [現行仕様](../specs/player-stats-and-upgrades.md) に記録した。

### [x] A-06 設定値と実行時の能力値を分離

- 現状: `BulletParametersConfiguration` と `UpgradeParametersConfiguration` はシリアライズ可能な構造体で、`PlayerAttackGameplay` が初期値を保持しながら強化で書き換える。
- 作業: Inspectorで編集する初期設定と、プレイ中に変わる弾・武器・プレイヤーの状態の所有者を明確にする。`ScriptableObject` を採用する場合も、共有アセット自体を実行時の状態として変更しない。
- 完了条件: 再開始時に初期設定へ戻り、複数の使用者が互いの実行時値を意図せず共有しない。現在の強化結果を維持する。
- 参照: `Assets/Scripts/Combat/Configuration/BulletParametersConfiguration.cs`、`Assets/Scripts/Progression/Configuration/UpgradeParametersConfiguration.cs`、`Assets/Scripts/Player/Gameplay/PlayerCoreGameplay.cs`。
- 実施結果（2026-10-03）: プレイヤーの初期体力・移動速度と、プレイヤー・敵の武器初期値を ScriptableObject に移し、Prefab・派生Prefab・シーンの上書き値を対応する設定アセットへ移行した。強化で変わる体力・速度・武器値と残弾は各コンポーネントの実行時値と `WeaponAmmoStateDomain` に保持する。強化ボタンの `UpgradeParametersConfiguration` は値型の読み取り用設定として維持した。Unity 2022.3.62f2 のバッチPlayで既存のシーン設定、強化、複数個体の独立性、Play停止・再開による初期値への復帰を確認した。詳細は [現行仕様](../specs/player-stats-and-upgrades.md) を参照する。

### [x] A-07 シーン参照の依存関係を整理

- 現状: 複数のコンポーネントが `SceneReferenceResolverInfrastructure.RequireUnique<T>()` でシーン内の管理クラスを検索する。
- 作業: 必須参照ごとに、同一オブジェクト・Inspector指定・初期化時の検索のどれで取得するかを決める。検索が必要な箇所は件数の検証を維持する。
- 完了条件: コンポーネントが必要とする参照先をコードまたはInspectorから追え、欠落時に対象を特定できる。Prefabとシーンの参照を検証する。
- 参照: `Assets/Scripts/Scene/Infrastructure/SceneReferenceResolverInfrastructure.cs` と各呼び出し元。
- 実施結果（2026-10-03）: 同じGameObjectの部品はGetComponent、必須部品とInspector参照は呼び出し側のnullチェックで取得方法を明示した。シーン検索の共通処理は別シーンを除外し、欠落・重複時には要求元と必要な型を診断する。PrefabとシーンのGUID・配置は維持した。共通検証導入時にUnity 2022.3.62f2でPrefab・シーン起動・射撃を検証し、呼び出し側のnullチェックへの簡素化後は差分レビューとコンパイルで確認した。[参照一覧](../specs/component-dependencies.md) と [検証結果](../specs/player-stats-and-upgrades.md) を記録した。

### [x] A-08 イベント購読の寿命を統一

- 現状: `PlayerUIViewer` と `EnemyUIViewer` が体力変更を購読し、攻撃側も弾数・待機時間を通知する。匿名関数で登録して解除していない箇所がある。
- 作業: 購読と解除の時点をコンポーネントの寿命に合わせる。再有効化時には現在値を表示して、過去の通知を待たない。
- 完了条件: 無効化・再有効化や破棄後に通知が重複せず、初期値と変更後のUIが一致する。
- 参照: `Assets/Scripts/Player/PlayerUIViewer.cs`、`Assets/Scripts/Enemy/EnemyUIViewer.cs`。
- 実施結果（2026-10-03）: 初回のStartで参照を確定し、以後はOnEnableで購読・現在値の表示、OnDisableで名前付きメソッドの解除と待機バーのTween停止を行う。プレイヤーの待機種類・時間は攻撃側から読み取り、UI再有効化時にバーを復元する。Unity 2022.3.62f2 の Test Runner でInGameシーンの初期表示、通知解除、3回の再有効化、GameObject切り替え、無効期間の変更と再表示、待機バー復元、UI破棄後の通知を確認した。継続利用するテストと証拠は [現行仕様](../specs/player-stats-and-upgrades.md) に記録した。

### [x] A-09 設計境界と依存方向を文書化

- 現状: コンポーネントの責務はコードから読み取れるが、ゲームルール、Unity操作、表示の境界を一覧できない。
- 作業: 機能ごとに状態の所有者、依存先、通知、設定値と実行時値、Unity固有処理を現行仕様として記録する。
- 完了条件: 新しい強化、弾種、敵の行動を追加する際、変更箇所と検証箇所を文書から辿れる。実装変更に合わせて更新する。
- 実施結果（2026-10-04）: [ゲーム機能の責務・依存方向と拡張箇所](../specs/gameplay-architecture.md) に、現行の状態所有者、設定と実行時値、依存方向、通知と寿命、Unity固有処理を記録した。強化・弾種・敵行動それぞれの変更コード、設定・Prefabと確認項目をリンクし、純粋C#化済みの範囲と追加候補を区別した。`b7539d8` の関連コード・シーン・Prefabを静的照合し、文書の相対リンクと差分を確認した。文書変更のためUnityの再実行は行っていない。
- 次の候補: 採用済みA-01〜A-09は完了。A-10〜A-13は引き続き採用未決定で、必要な範囲を選んでから実装する。既存のDタスクの再生確認は別一覧で継続管理する。

## 純粋C#化の追加候補（採用未決定）

次の候補は A-01〜A-09 の範囲を広げる独立した変更案。採用するIDを決めてからチェック対象のタスクに移す。

### A-10 体力と死亡判定の共通ルール

- `PlayerCoreGameplay` と `EnemyCoreGameplay` の体力変更・被ダメージ・死亡判定を、純粋C#の状態クラスで扱う。死亡エフェクト、無効化、経験値付与はUnity側に残す。
- 確認対象: 初期体力、最大体力変更、0以下の被ダメージ、死亡通知の一度きりの扱い。現行の死亡時仕様を先に確定する。

### A-11 強化値の計算規則

- `PlayerCoreGameplay.ApplyPowerUp()` と `PlayerAttackGameplay.ApplyPowerUp()` に分かれる加算・乗算、端数処理、下限値を純粋C#へ分離する。適用先のコンポーネントを決める処理は残す。
- 確認対象: 複数回の強化、負の加算、乗算と整数化の順序。A-06 の実行時能力値と組み合わせる。

### A-12 敵の行動選択

- `EnemyMoveGameplay` と `EnemyAttackGameplay` の「感知・射程内・射線あり」の判定結果を入力として、追跡・停止・射撃の選択だけを純粋C#へ分離する。Physics、Raycast、NavMeshAgent の呼び出しはUnity側に残す。
- 確認対象: 遮蔽物、射程の境界、プレイヤー未検出時の選択。敵の行動が複雑になった場合に優先する。

### A-13 弾の命中結果と反射回数の判定

- `BulletShotGameplay` の命中回数と返却条件、ダメージ量の決定を純粋C#へ分離する。衝突検出、`IDamageableDomain` 呼び出し、エフェクト生成、プール返却はUnity側に残す。
- 確認対象: 初回命中、反射上限、再利用時の命中回数リセット。弾種ごとの効果が増える場合に優先する。

## 着手順の目安

1. 関連する D-01〜D-09 の再生確認結果を残す。
2. A-01 と A-03 で計算と表示の分離を確かめ、A-02 を合わせる。
3. A-04 と A-05 で重複する射撃処理を整理し、A-06 で設定値と実行時値の境界を定める。
4. A-07、A-08、A-09 は各機能の変更に合わせて小さく進める。追加候補は採用後に関連タスクと実施順を決める。
