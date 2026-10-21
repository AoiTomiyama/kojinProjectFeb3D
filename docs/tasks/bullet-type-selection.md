# CBT-02 武器定義からの弾種選択

タグ: `CBT-02` `R-04` `弾種` `初期化契約` `プール` `検証証拠`

実施日: 2026-10-21。固定uid: R-04。実装コミット: `24f5d19`、基点: `0b846ee`。ハブはコード差分と完了条件、結果XMLの最新53件成功を照合して承認した。

## 現行動作と設計判断

両攻撃部品は `WeaponDefinitionConfiguration.BulletType` を共通発射手順へ渡す。既存5定義には、プレイヤー用2件へPlayerBullet、敵用3件へEnemyBulletを保存した。弾の数値、発数、残弾消費、拡散角、待機の処理は従来の共通手順を使う。

`PooledAttackBaseGameplay.OnInitialize(BulletInitializationContextGameplay)` は非活動の生成時に一度だけ呼び、任意のシーン音源を渡す。プールから具体的なBulletShotGameplayへの型判定を除去した。発射ごとにParameter・位置・向きを設定し、有効化後にOnGetFromPoolで状態をリセットする。

BulletShotGameplayは射撃音クリップが未設定なら無音で動作する。設定済みなら音源を必須とし、不足時は寿命待機・速度設定を始める前にエラーを出す。既存Prefabの音声・演出参照とGUIDは保持した。音源を使わない派生弾は、音源なしのプールでも利用できる。

追加手順とコードへの参照は[現行仕様](../specs/gameplay-architecture.md#新しい弾種を追加する)、依存は[必須参照](../specs/component-dependencies.md)、型一覧は[対応表](../specs/architecture-script-catalog.md)を参照する。

## 実行条件と結果

Unity 2022.3.62f2、Windows、通常GPU、Test RunnerのEdit ModeからPlayへ移行する既存方式を使用した。対象はSceneGameplayTests・SceneDependencyTests・ProjectAssetReferenceTests・BulletLifetimeTests。CLI引数は `-batchmode -projectPath <project> -runTests -testPlatform editmode -testFilter <対象> -testResults <XML> -logFile <ログ>`。

| 実行 | 件数と結果 | プロセス終了コード | Test Runner時間 |
| --- | --- | --- | --- |
| 初回3クラス | 45件中41成功、4失敗 | 2 | 35.683秒 |
| 修正後4クラス | 48件中46成功、2失敗 | 2 | 40.768秒 |
| 残る2件と更新した参照テスト | 11件すべて成功 | 0 | 3.770秒 |

対象53件の最新結果はすべて成功した。最終実行は、派生弾の生成テスト、敵射線テスト、ProjectAssetReferenceTestsの9件を含む。残る42件は修正後4クラス実行の成功結果を使う。成功後の同条件再実行は行っていない。

確認した内容は、既存5武器の弾種保持、両攻撃側から両弾種への切替、残弾消費、具体型に依存しない派生弾への初期化、初回有効化前の設定、初期化一回・発射時リセット、無音・音源不足・設定済み音源、衝突とダメージ、寿命・無効化・再取得である。

初回失敗は、新規検証部品のEditorファイル配置、方向・速度の浮動小数点の厳密比較、既存衝突テストの有効化不足、既存敵射線テストのNavMesh経路更新待ち不足による。部品を既存テストファイルへ統合し、比較に許容差を設け、衝突前の有効化と経路更新の1フレーム待機を追加した。ゲーム仕様の取り違えによる変更は0回。再検証2回、Test Runner時間の合計は80.221秒。

ログとXMLはローカル一時フォルダーの `survivor-r04-tests`、`survivor-r04-tests-final`、`survivor-r04-tests-recheck`（各 `.log` / `.xml`）。最終2回のC#コンパイルエラー・警告・例外は0件。Unityが保存したURP2設定とPlayerSettingsの自動更新は差分確認後に復元した。音の聴取とプレイヤービルドは今回の検証に含めていない。

設計図はMetisでクラス図・相関図・射撃シーケンスと対応説明を更新した。全11図・140要素のvalidateは `ok=true`、`errors=[]`、`missing=[]`、sync再実行は `changed=[]`。結果はローカル一時フォルダーの `survivor-r04-metis-validate.json` と `survivor-r04-metis-sync.json`。
