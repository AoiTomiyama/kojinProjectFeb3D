# 追加実装前の拡張性セルフレビューと再開情報

タグ: `設計レビュー` `拡張性` `追加実装` `再開情報` `純粋C#`

- 記録日: 2026-10-06。
- 準備の再照合日: 2026-10-07。依頼ID: `prepare-サバイバー-20261007`。基点は文書コミット `5607f99`、ゲームコードは `1ed6417` から差分なし。
- 照合版: `1ed6417`。レビュー対象の `Assets/Scripts/` に未コミット差分はない。
- 対象: 戦闘、経験値・強化、敵行動、UI通知、設定とシーン参照。
- 方法: 現行コードと設計文書の静的照合。以下の条件の実行再現や、新しいテスト・ビルドは行っていない。既存の実行証拠は [機能検証記録](headless-verification.md) を参照する。
- 状態: R-03の強化定義・UI分離は2026-10-07の依頼 `survivor-upgrade-ui-20261007` で採用。実装・検証は [強化一覧UIの作業記録](upgrade-catalog-ui.md) を参照する。2026-10-10にR-01の一度きり保証も採用・実装した。他のR候補とA-10〜A-13の全面的な純粋C#化は未採用。以下の指摘本文はレビュー時の状態を示す。

## 結論と現在の土台

追加実装を進められる土台はできている。現在の機能別の軽量なレイヤード設計を継続し、追加する機能に関係する境界から改善する。

- 経験値・レベル、候補抽選、弾数・待機状態、拡散角は純粋C#へ分離され、Unity操作を行う側から計算を呼ぶ依存方向になっている。
- ScriptableObjectの初期設定を個体の実行時値へコピーし、強化で共有設定を変更しない。
- 体力・弾数UIは確定した状態を表示し、有効期間に合わせて購読・解除する。再表示時は現在値を読み直す。
- 発射手順と弾プールは共通化済み。ただし、新しい弾の初期化順や武器からの弾種選択には制約が残る。

採用済みの設計方針は [目標アーキテクチャ](../knowledge/target-architecture.md)、現行の所有者・依存先は [ゲーム機能の責務](../specs/gameplay-architecture.md) を正本とする。

## 指摘と改善候補

R番号はこのレビュー内の参照IDであり、採用済みタスク番号ではない。

### R-01【対応済み】死亡処理の一度きり保証

- 現行対応（2026-10-10）: 両Coreの死亡状態を体力通知前に確定し、死亡後のDamageと通知中の再入による重複発行を防ぐ。公開体力APIと通常強化を維持し、A-10の全面共通化・復活resetは含めない。[変更範囲と検証証拠](death-once-verification.md)。

- 根拠: [EnemyCoreGameplay](../../Assets/Scripts/Enemy/Gameplay/EnemyCoreGameplay.cs) の `Damage()`（51行付近）、[PlayerCoreGameplay](../../Assets/Scripts/Player/Gameplay/PlayerCoreGameplay.cs) の `Damage()`（74行付近）。体力が0以下なら死亡処理を実行し、死亡済み状態を確認しない。
- 条件・影響: 死亡後も保持した参照からダメージを渡すと、敵の経験値・撃破数・トークン、死亡通知・演出が再度発生する可能性がある。GameObjectの無効化は直接呼出しを禁止しない。範囲攻撃・継続ダメージで重要になる。
- 改善案: 生存から死亡への遷移を一度だけ確定し、経験値・演出は結果に応じてUnity側が実行する。既存候補A-10に対応する。
- 完了条件案: 致死ダメージ後の連続呼出しでも死亡通知・報酬は1回。通常被弾と最大体力強化を維持する。復活・敵の再利用を導入する場合は死亡状態のリセット時点も定義する。

### R-02【中】弾が設定より先に有効化される

- 根拠: [BulletPoolInfrastructure](../../Assets/Scripts/Combat/Infrastructure/BulletPoolInfrastructure.cs) の `OnGetFromPool()`（62行付近）が有効化し、その後 [BulletFireSequenceGameplay](../../Assets/Scripts/Combat/Gameplay/BulletFireSequenceGameplay.cs) の `Fire()` がパラメーター・位置・向きを設定して弾の発射初期化を呼ぶ。
- 条件・影響: 新しい弾がUnityの `OnEnable()` で追尾対象や演出を初期化すると、未設定値や前回の発射値を読む可能性がある。現行のBulletShotは専用の初期化を後から呼んでいる。
- 改善案: 取得、配置・パラメーター設定、有効化・発射開始の順序を共通契約として明示する。プールの事前生成・返却も確認する。
- 完了条件案: 初回・再取得の双方で、有効化時点に今回の設定が揃う。速度・音・寿命・命中回数の初期化と返却を維持する。既存の `BulletLifetimeTests` と関連シーンテストを利用する。

### R-03【中】強化候補の定義がUIのボタン構成に依存する

- 根拠: [LevelUpCoordinatorGameplay](../../Assets/Scripts/Progression/Gameplay/LevelUpCoordinatorGameplay.cs) の `Start()`（38行付近）がViewの候補数から配列添字をIDにする。[LevelUpViewPresentation](../../Assets/Scripts/Progression/Presentation/LevelUpViewPresentation.cs) の `Initialize()` は子Buttonを候補元として取得する。強化値は各 [UpgradeButtonPresentation](../../Assets/Scripts/Progression/Presentation/UpgradeButtonPresentation.cs) に設定されている。
- 条件・影響: レア度・取得条件・取得済み強化の除外・保存対応を追加すると、候補管理と表示構成の双方へ変更が広がる。現在のIDは永続化用の安定したIDではない。
- 改善案: 安定したIDを持つ強化定義をゲーム側で管理し、UIへ抽選結果を渡す。計算規則の分離はA-11と関連するが、強化定義の分離は別の変更目的として切り分ける。
- 完了条件案: UIの並びを変更しても強化IDと内容は不変。重複防止、候補不足時の残高保持、選択回数、適用結果、共有設定の不変を維持する。

### R-04【中】武器設定だけでは新しい弾種を選べない

- 根拠: [PlayerAttackGameplay](../../Assets/Scripts/Player/Gameplay/PlayerAttackGameplay.cs) の `Shoot()`（148行付近）と [EnemyAttackGameplay](../../Assets/Scripts/Enemy/Gameplay/EnemyAttackGameplay.cs) の `Shoot()`（87行付近）が弾種を固定する。[WeaponDefinitionConfiguration](../../Assets/Scripts/Combat/Configuration/WeaponDefinitionConfiguration.cs) に弾種設定はない。プールの音声注入も `component is BulletShotGameplay`（35行付近）に限定される。
- 条件・影響: 別の弾種や基底クラスの新しい派生実装を使うと、カタログ・Prefab以外に攻撃側やプールも変更する必要がある。
- 改善案: 武器定義へ弾種を追加し、実装ごとに必要な初期化情報を受け取る契約を設ける。全ての弾に音を必須とするかは追加仕様に合わせて決める。
- 完了条件案: 武器設定で既存弾・新しい弾を選べる。既存武器は従来の弾種を保持する。派生弾の初期化・再利用・音声の有無を確認する。

### R-05【中・追加内容次第】敵行動とシーン参照の前提が固定される

- 根拠: [EnemyMoveGameplay](../../Assets/Scripts/Enemy/Gameplay/EnemyMoveGameplay.cs) とEnemyAttackGameplayの各 `Update()` が感知・射程・射線から個別に判断する。[SceneReferenceResolverInfrastructure](../../Assets/Scripts/Scene/Infrastructure/SceneReferenceResolverInfrastructure.cs) の `RequireUnique()` は同じシーン内に対象が1件であることを要求する。敵はPlayerCore、移動・表示はCamera等をこの方法で取得する。
- 条件・影響: 逃走・巡回・行動段階の追加で判断が分散する。複数プレイヤー・追加カメラ・別シーンの共通サービスを導入すると参照条件を満たさなくなる。現在の単一プレイヤー構成の前提自体を現行不具合とは扱わない。
- 改善案: 行動選択の分離はA-12に対応する。参照は複数化が必要になった対象から明示的に指定する。この2件は独立した変更として扱う。
- 完了条件案: 追跡・停止・射撃と遮蔽判定を維持し、新しい行動の切替条件を直接検証できる。複数候補があっても意図した参照先を選べ、未設定時に対象が分かる診断を出す。

## 着手順の判断

1. 範囲攻撃・継続ダメージを追加するならR-01を先に扱う。
2. 新しい弾の実装を追加するならR-02を先に扱い、R-04を組み合わせる。
3. 強化の種類・条件・保存を広げるならR-03を扱う。
4. 敵行動や対象数を広げるならR-05の必要な部分を扱う。

A-10〜A-13の採用状態は [既存の改善タスク](architecture-improvement.md) で管理する。今回の記録で採用状態は変えていない。実装時は変更・維持する動作を示し、変更目的ごとに既存テスト・関連仕様を揃えてコミットする。

## 再開チェックポイント

最新状態（2026-10-10）: R-01の一度きり保証は実装・61件の一式検証・Windowsビルド完了。[証拠と変更範囲](death-once-verification.md) を参照する。R-03も完了。A-10全面共通化・他候補とV-02の未完了状態は維持する。以下の2026-10-07以前の段落は当時の状態を示す。

最新状態（2026-10-07）: R-03の定義・UI分離は実装・検証済み。追加手順・実行証拠・残課題は [強化一覧UIの作業記録](upgrade-catalog-ui.md) を参照する。以下のV-02差分は引き続き未完了として保持する。

2026-10-07に現行コード・Git状態・既存結果XML/ログを再照合した。R-01〜R-05の前提は維持され、改善候補は未採用。今回は準備文書のみ更新し、機能実装・Unity検証の再実行は行っていない。

- 現在地: V-01は `1ed6417` で完了。V-02は修正とビルド確認まで済み、描画比較の再確認・コミットが残る。実行結果と再実行手順は [V-02の記録](headless-verification.md) を参照する。
- V-02の差分: 対象シェーダーと既存.metaはステージ済み（本体はステージ後の改行調整あり）。`Assets/Editor/VfxShaderRenderingTests.cs` と.metaは未追跡。`docs/specs/gameplay-architecture.md` と `docs/tasks/headless-verification.md` に関連記録の差分がある。
- 検証に伴う差分: `Assets/Settings/URP-Balanced.asset`、`URP-HighFidelity.asset`、`URP-Performant.asset` と `ProjectSettings/ProjectSettings.asset` にEditorの自動書換えが残る。再開時に差分を確認して扱い、利用者の追加変更があれば保護する。
- 次の操作: `git status --short` と差分を確認し、V-02の描画比較を再実行できるEditor状態か確認する。前回は同じプロジェクトがEditorで開かれていてバッチ起動が失敗した。開いているEditorを勝手に停止しない。
- V-02完了条件: 正常なGPU描画で代表マテリアル2件の変更前後を比較し、証拠を更新してV-02単独でコミットする。初回比較の全面ピンク画像は無効な証拠として記録済み。
- 追加実装: このレビューの記録は完了。次のユーザー指示で追加機能または採用する改善案が決まったら、上記の対応と完了条件を参照して着手する。

## 追加実装の準備結果（2026-10-07）

### 準備済み範囲と変更箇所

| 追加する機能 | 変更する入口・配置 | 設計上の制約と関連候補 | 再利用する検証 |
| --- | --- | --- | --- |
| 強化・成長 | ProgressionのDomain/Gameplay/Configuration、PlayerCore/PlayerAttack、強化ボタン・初期設定 | 候補IDは起動時のButton添字。既存能力値の調整はボタン設定で可能。条件・レア度・保存対応はR-03、計算規則はA-11を検討 | `DomainRulesTests`、`SceneGameplayTests`、UIを変える場合は `UiEventLifetimeTests` |
| 武器・弾 | CombatのConfiguration/Gameplay/Infrastructure、両Attack、`Assets/GameData/Combat/`、弾Prefab | 固定弾種、音声注入の型依存、有効化が設定より先。R-02/R-04。新しい弾種のenum値は既存値を維持 | `DomainRulesTests`、`ProjectAssetReferenceTests`、`BulletLifetimeTests`、`SceneGameplayTests` |
| 敵・行動 | EnemyのGameplay、敵Prefab/派生Prefab、武器設定 | Coreが対象を取得し、Move/Attackが個別に物理判定する。行動段階はR-05/A-12。新規出現位置はAgentがNavMeshへ接続できる条件を満たす必要がある | `SceneDependencyTests`、`SceneGameplayTests` の追跡・遮蔽・NavMesh読込み確認 |
| 被弾・範囲攻撃・復活 | 両Coreの `Damage()`、`IDamageableDomain`、BulletShotの衝突処理 | 死亡済み参照への連続呼出しはR-01。復活・敵再利用は体力・死亡状態・報酬付与のリセット仕様を先に決める | `SceneGameplayTests`、新規条件は既存Editorテスト基盤へ追加 |
| 終了・再挑戦・継続出現 | 両Coreの死亡通知、Progressionへの報酬接続、InGameシーンが既存接続点 | 該当する進行管理・終了/再挑戦コードは `Assets/Scripts/` 内で確認できない。担当機能・状態所有者を仕様に応じて決める。R-01や敵再利用の扱いは採用する方式次第 | シーン遷移・複数開始停止の既存テスト手順、Windowsビルド/起動確認を再利用 |

新規コードは機能→レイヤーの配置・接尾辞規則に従う。純粋C#はUnityを参照せず、入力・衝突・Physics・NavMesh・Prefab操作はUnity側に残す。可変状態は個体が所有し、共有ScriptableObjectへ書き戻さない。UIは結果を表示する。アセット編集ではGUIDと既存参照を維持する。詳細は [配置規則](../specs/script-layout-and-naming.md) と [現行の拡張手順](../specs/gameplay-architecture.md) を参照する。

### 阻害要因の区別

- **新規実装の仕様が未指定**: 対象・動作・完了条件を確定できないため、新機能の実装着手は待機。準備自体は完了。
- **V-02の完了を阻む確認不足**: 修正後の描画比較2件の有効な結果がない。前回のEditor競合は履歴であり、今回のプロセス確認時にはUnity Editorは0件だった。再実行直前に再確認する。V-02が全ての追加機能を技術的に阻害するとは判断しないが、同じ描画/設定を変更する場合は先に比較の基点を確定する。
- **条件付きの設計課題**: 範囲攻撃等はR-01、新しい弾の有効化処理はR-02、条件付き強化はR-03、弾種選択はR-04、複数対象/行動段階はR-05を先に検討する。全候補の実装を一律の前提条件にしない。
- **既存差分の整理**: ステージ済みシェーダー、未追跡テスト、URP/PlayerSettings、V-02関連文書の差分は内容確認済みで保持。この準備コミットへ含めない。自動書換えを取り消すか採用するかはV-02完了時に現在の差分を再確認して決める。

### 照合した有効な証拠

ローカル一時ディレクトリの既存証拠を確認した。以下は2026-10-06の実行結果であり、今回再実行した結果ではない。

- `kojin-static-verification-final.xml`: 56件成功・失敗0。V-01着手前の一式。
- `kojin-v02-tests.xml`: XMLは58件成功・失敗0だがGPU比較2件は全面ピンクで無効。既存56件のゲーム機能確認と区別する。現在の比較テストに追加されたURP指定・同期コンパイル・ピンク/非有限値の拒否は、その後の有効な実行結果が未取得。
- `kojin-v02-build.log`: `Windows build: Succeeded; errors=0`。シェーダー4ガードの修正後ビルド。表示品質の保証にはしない。
- `kojin-v02-render-final.log`: 同じプロジェクトのEditor競合でテスト開始前に中断。対応する結果XMLは存在しない。

### 再利用する実行手順

指定Editorは `ProjectSettings/ProjectVersion.txt` のUnity 2022.3.62f2。プロジェクトが別Editorで開かれていないことを確認し、既存のTest Runnerへ次の引数を渡す。

```text
-batchmode -projectPath "<project>" -runTests -testPlatform editmode -testFilter "<test class>" -testResults "<results.xml>" -logFile "<run.log>"
```

V-02再開は `-testFilter VfxShaderRenderingTests` を使用し、`-nographics` と `-quit` は付けない。Cosmos_02/Earth_01のPNGと非空描画、エラー拒否、RGBA最大差0.0001以下を確認する。影描画の比較は現行テストの対象外で、影側のガードはコード照合・ビルドで確認する。追加機能の関連テストをまとめ、必要な全体検証では `-testFilter` を省略する。終了コードとXMLの結果を確認し、成功後は新しい変更・未解決懸念がなければ再実行しない。

ビルド入口は `ProjectAssetReferenceTests.BuildWindowsPlayer`。詳細な引数、出力先、画面なし起動の制約は [検証手順](headless-verification.md) を参照する。使い捨ての検証スクリプトは追加しない。

### ユーザーから必要な仕様と次の操作

次の依頼には、追加したい機能、発動条件・入力、結果・数値、対象（プレイヤー/敵/武器）、既存動作との関係、成功と判断する操作例を含める。機能に応じて次も決める。

- 武器・弾: 既存弾の値変更か新しい挙動か、切替方法、命中/寿命/反射、音・演出。
- 強化: 効果と加算乗算順、候補条件、重複/上限、保存の要否。
- 敵・進行: 出現条件・位置・数量、対象と行動段階、終了条件、再挑戦時に戻す状態。

ハブから対象仕様が届いたら、Git差分とV-02状態を再確認し、該当するレビュー候補を選んで変更・維持する動作と完了条件を示す。修正候補の採用が未確定なら提案のまま扱い、新機能を推測で実装しない。V-02を再開する場合は既存比較テストから続け、独立した目的でコミットする。
