# R-01 死亡処理の一度きり保証と検証記録

タグ: `R-01` `死亡` `再入` `検証証拠` `設計判断` `再開情報`

- 実装日: 2026-10-10。依頼: `implementation-plan-20261010-サバイバー-01` の実装工程、Task `task_e89fc88bf3de`。
- 対象差分: 親HEAD `4023086e4624c96dc950cb0bff7507cb31d223c7` に対する `EnemyCoreGameplay.cs`、`PlayerCoreGameplay.cs`、`SceneGameplayTests.cs` とこの記録・関連現行仕様/タスク状態の文書。R-01を独立コミットする。
- 状態: 実装・必要なTest Runner・Windowsビルド完了。R-01以外の候補は進めていない。

## 成果と判断

両Coreの非シリアライズprivate `_isDead` をAwakeで初期化し、最初の致死Damageの体力通知より前に確定する。各Damageはローカルの `becameDead` を保持するため、非致死の体力通知から別の致死Damageが入った場合も、外側が死亡副作用を再発行しない。死亡後は被弾を無視する。敵の経験値・撃破数・トークン・死亡通知・演出、プレイヤーの死亡通知・演出は個体ごとに1回となる。

体力通知と死亡副作用の既存順序、公開Health/MaxHealth、0/負ダメージの既存演算、通常被弾UI、最大体力強化時の全回復と共有SO不変を維持した。OnEnable、Start再呼出し、公開体力setterは死亡履歴を解除しない。復活・敵プール再利用のreset APIは含めない。通知先が例外を投げても死亡状態を巻き戻して副作用を再試行しない。

単なる有効状態の確認や通知後のフラグ確定では同期再入を防げないため、確定時点を通知より前に置いた。今回必要なのは一度の遷移を記憶するboolだけであり、体力全体の純粋C#共通状態クラス化（A-10）は公開API/初期化/強化規則の移行を伴う別候補として未採用のまま残す。現行仕様は [体力・死亡遷移](../specs/player-stats-and-upgrades.md)、関連候補は [A-10](architecture-improvement.md) を参照する。

## 回帰の内容

既存 [SceneGameplayTests](../../Assets/Editor/SceneGameplayTests.cs) に `DeathTransitionsRemainOnceAcrossNotificationReentryAndReenable` を追加した。既存の実シーン準備、EnterPlayMode、TearDown、リフレクションの補助を再利用し、テスト用アセットや公開APIを増やしていない。

敵・プレイヤー各3経路（計6）を新しい個体で確認する。

- 致死DamageのOnHealthChangedから再Damage。
- 非致死DamageのOnHealthChangedから致死Damage（外側の誤った再発行を防ぐ）。
- OnDiedから再Damage、敵では経験値通知OnDeathからも再Damage。

各経路で体力通知回数、死亡通知1回、演出生成1回と既存順序、敵の経験値・レベル・選択回数・撃破数・トークンの増加を検証する。演出は不活性なシーン内の一時GameObjectを使い、粒子寿命に依存せず生成数を数える。再入購読には回数上限があり、旧実装でも無限再帰にならない。

通常被弾、0/負ダメージ、生存中の再有効化、新個体のAwake/Start初期体力、死亡後の連続Damage、公開体力の書換えと再有効化でも死亡履歴を解除しないことを確認した。通常強化・UI・共有SOの不変は既存のSceneGameplay/UiEventLifetime/UpgradeCatalogテストも通過した。旧実装のUnity再実行は行っておらず、旧問題経路は静的根拠と区別する。

## 実行条件と結果

指定Unity **2022.3.62f2**、現行のDomain Reload/Scene Reload無効設定を使用。GUI操作は行わず、実行前にUnity Editorプロセスがないことを確認し、既存Editorの停止は行っていない。ログとXMLはリポジトリ外のローカル一時ディレクトリ（`%TEMP%`）に保存した。

| 検証 | 結果 | 証拠 | 所要時間 |
| --- | --- | --- | --- |
| 既存7クラス＋追加回帰1ケース | 61件成功、失敗0、スキップ0、Editor終了コード0 | `kojin-r01-tests.xml` / `kojin-r01-tests.log` | プロセス96.7秒、XMLのテスト実行34.09秒 |
| Windows x64 Developmentビルド | Succeeded、errors=0、Editor終了コード0 | `kojin-r01-build.log` | プロセス34.1秒 |

Test Runnerの引数（`<project>` は対象リポジトリ、出力は外部一時領域）:

```text
-batchmode -projectPath "<project>" -runTests -testPlatform editmode -testFilter "DomainRulesTests;ProjectAssetReferenceTests;SceneDependencyTests;SceneGameplayTests;UiEventLifetimeTests;BulletLifetimeTests;UpgradeCatalogTests" -testResults "<temp>/kojin-r01-tests.xml" -logFile "<temp>/kojin-r01-tests.log"
```

Test Runnerに `-quit` は付けない。既存60ケース＋今回1ケースを同じ起動で確認した。未完了V-02の `VfxShaderRenderingTests` はこの一式へ含めていない。

ビルドは既存 `ProjectAssetReferenceTests.BuildWindowsPlayer` を `-batchmode -projectPath "<project>" -executeMethod ProjectAssetReferenceTests.BuildWindowsPlayer -logFile "<temp>/kojin-r01-build.log"` で実行した。ビルドメソッドが成否に応じてEditorを終了する。出力は `<temp>/kojin-static-player/kojinProjectFeb3D.exe`。

両ログにC#コンパイルエラー、NullReferenceException、MissingReferenceException、ObjectDisposedExceptionはない。結果XMLと実際の起動プロセス終了コードを確認した。ビルド成功はプレイヤーの手動操作や画面品質の確認を意味しない。今回の変更に描画/入力仕様の変更はなく、操作・見た目の追加評価は要求しない。

効率記録: Unity起動2回、合計130.8秒、再検証0回、検証失敗0件、仕様取り違えによる手戻り0件。成功後の同一条件再実行は行っていない。

## 既存差分保全と再開情報

着手時のV-02関連10ファイル（ステージ済みシェーダー/.meta、未追跡比較テスト/.meta、URP3件、PlayerSettings、gameplay-architecture/headless-verification文書）を検証後にバイト比較し、全て不変を確認した。既存.meta、Prefab、シーン、設定アセットをR-01コミットへ含めない。

V-02の既存文書hunkを混ぜないため、R-01の証拠はこの専用文書へ集約し、差分のない現行仕様・既存タスクリストから参照する。V-02の残作業は [既存検証記録](headless-verification.md)、他候補は [セルフレビュー](extensibility-self-review.md) を参照する。pushなし。R-01の追加仕様判断・未解決の検証失敗はない。次の依頼まで待機し、復活/再利用やA-10全体は新しい対象仕様が決まった時に独立して扱う。
