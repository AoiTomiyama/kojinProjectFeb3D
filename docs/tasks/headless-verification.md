# GUI操作なしでのゲーム機能検証

タグ: `検証証拠` `再開情報` `Unity Test Runner` `純粋C#` `戦闘` `強化` `ビルド`

## 最新結果（2026-10-06）

- Unity: プロジェクト指定の **2022.3.62f2**。
- Test Runner: Edit Modeから必要に応じてPlayへ移行し、**56件成功・失敗0件・スキップ0件、Editor終了コード0**。
- 実行XML: ローカル一時ディレクトリの `kojin-static-verification-final.xml`。ログ: `kojin-static-verification-final.log`。最終ログにC#コンパイルエラー、MissingReferenceException、NullReferenceException、ObjectDisposedExceptionはない。
- 設定: Domain Reload・Scene Reloadを無効にした現行Editor設定を使用。各Playテストの終了時に停止してシーンを開き直し、複数回の開始・停止も通過した。テスト用のシーン変更は保存しない。
- 文字コード: 今回の追加分を含むGit管理対象テキスト274件を厳密なUTF-8デコードで確認し、不正な文字列0件。バイナリは対象外。.metaのGUID152件に重複0件。
- Windows x64 Developmentビルド: **Succeeded・ビルドエラー0件・Editor終了コード0**。ログはローカル一時ディレクトリの `kojin-static-player-build-final.log`。
- ビルド版の起動: `-batchmode -nographics` で10秒間起動を継続し、C#例外と敵のNavMesh接続アサーション失敗は0件。確認後にプロセスを停止したため、自然終了の成否を示す結果ではない。ログは `kojin-static-player-startup-final.log`。描画無効に伴うシェーダー非対応ログと、起動時のNavMesh生成診断7件は下記に区別して記録する。

## 自動検証の範囲

| テストクラス | 件数 | 確認した動作 | 対応タスク |
| --- | ---: | --- | --- |
| `DomainRulesTests` | 12 | 経験値の正確な閾値、連続・複数レベル上昇、int上限、設定値の複製、不正値と選択回数の消費。候補の重複防止・不足・不正乱数・元配列の保持、再抽選成功時だけの3トークン消費。残弾の範囲、発射間隔・再装填・待機再開始・待機中の強化、個体間の独立、拡散角と残弾不足 | D-01、D-03、D-09、A-01、A-02、A-04、A-05 |
| `ProjectAssetReferenceTests` | 4 | ゲーム型の解決、Prefabの欠落スクリプトとButton呼出先、設定型、武器の有効な初期値、全弾種のPrefab・Rigidbody・Collider・音・命中エフェクト・ダメージ表示の参照 | D-05、A-06、命名・配置整理 |
| `SceneDependencyTests` | 32 | 同じシーンの参照取得、0件・2件の診断、別シーン・非アクティブ個体の除外。Inspector参照、同一GameObjectの必須部品、死亡エフェクト、射撃音の欠落診断。Start前の攻撃無効化、プール欠落時の元の例外保持と安全な無効化 | D-05、D-09、A-07 |
| `SceneGameplayTests` | 6 | 保存されたButton.onClickによるメニュー開閉・再抽選・強化、候補不足時のエラーと残高保持。敵2体の死亡から経験値・撃破数へ接続、最大レベル。被弾・体力/速度/攻撃/装弾数強化とUI、設定アセット不変、単体Prefab体力100とシーン体力50。表示文字列5に対して初期装弾数7で開始し、発射・再装填表示を確認。両攻撃者の待機中断・再開・再射撃、弾の寿命返却、AudioSource受渡し。NavMesh上の敵の追跡・射程内停止、遮蔽あり/なしの発砲。実際の敵弾衝突による7ダメージと数値表示・返却。照準Raycast、メニュー中の照準停止と射撃抑止、追従位置、プレイヤーの向きと照準線、ビルボードの向き。実行中のシーン再読込み後、敵7体すべてのNavMesh接続と移動先設定 | D-01〜D-09、A-01〜A-07 |
| `UiEventLifetimeTests` | 1 | 初期表示、UIとGameObjectの反復無効化・再有効化、購読数、現在値復元、Tween中断・復元、UI破棄後の購読解除 | D-06、D-08、A-08 |
| `BulletLifetimeTests` | 1 | 返却→再取得→発射初期化前に破棄する操作と、破棄済み弾のプール破棄処理を両弾種で実行 | D-10、D-11 |

テストは `Assets/Editor/` に保存し、専用の一時検証スクリプトを介さずUnity Test Runnerで再実行できる。入力や実行順を制御するため、一部のテストは既存のprivateメソッド・フィールドをリフレクションで操作する。実運用のメソッド、物理衝突、NavMesh、保存されたシーン・Prefab・Buttonの呼出先を使用し、テスト専用の公開APIをランタイムへ追加していない。

## 発見して修正した問題

- **D-11: 弾の返却後の二重キャンセル。** `BulletShotGameplay.OnDisable()` がCancellationTokenSourceを破棄しても参照を残していた。返却済みの弾を再取得し、発射初期化前に破棄すると、破棄済み参照へのCancelでObjectDisposedExceptionが発生した。
- 無効化時に参照をnullにして解消。`BulletLifetimeTests` が両弾種で同じ操作を行い、修正後の最終一式で成功した。
- 過去の命中エフェクトMissingReferenceExceptionは、今回の実際の弾衝突テストでは再現していない。この結果だけで過去の発生原因を断定しない。
- バッチビルドの初回は、未保存シーンへ戻ろうとするlilToonの最適化で `Scene file not found: ''` が出た。ビルド入口で有効なシーンを開き、BuildResultだけでなくtotalErrorsも成功条件にすることで、再実行時は最適化の例外0件・ビルドエラー0件となった。

## 残る診断と追加候補

### [ ] V-01 ビルド版の初回NavMesh生成診断を抑制する

- 観測: 初回シーン読込み時に `Failed to create agent because there is no valid NavMesh` が7件出る。Editorの通常開始・再生中のシーン再読込みテストでは同じログは出なかった。
- 現在の機能確認: `EnemyMoveGameplay.Start()` のDebug.Assertで、Developmentビルドでも初期化時にNavMeshへ接続済みであることを確認した。さらにEditorの実行中シーン読込み後、敵7体全員でisOnNavMeshとSetDestinationが成功した。NavMeshデータの恒常的な欠落や、移動機能の失敗は今回確認されていない。
- 調査根拠: Navigation 1.1.6のNavMeshSurfaceはOnEnableでAddDataを呼ぶ。ログはその登録とAgentの生成順序に関係する可能性があるが、ネイティブ処理の順序まで追跡して原因を確定していない。
- 追加候補: Agentを有効にするタイミングをNavMesh登録後へ揃え、Editor・Player双方で起動診断が0件になるか確認する。機能の不具合修正済みタスクD-02・D-05とは別の、診断整理の候補とする。

### [ ] V-02 外部VFXシェーダーのpow警告を確認する

- 観測: `Vefects/SH_Vefects_VFX_AdvTrail` の418・426・698・701行で、powの底が負になる可能性を示すd3d11コンパイル警告4件が出る。ビルドは成功し、シェーダーコンパイルエラーは0件。
- 根拠: `Assets/AssetStoreTools/Vefects/Trails VFX URP/VFX/Shaders/SH_Vefects_VFX_AdvTrail.shader`。底にはMask/Erosionテクスチャのr成分を使っている。警告だけから負値の発生や表示不具合を断定しない。
- 追加候補: 使用するテクスチャ形式と値域を確認し、負値が入り得る設定に対して処理方針を決める。外部アセットの表示変更は実際の描画で比較する。

`-nographics` の起動はNull Graphics Deviceを使用するため、UI・粒子・URP等のシェーダーが非対応と出る。これは画面なし起動の制約であり、通常GPUでの描画成功・失敗を示す検証ではない。

## 再実行

プロジェクトを開いていない状態で、指定Editorに次の引数を渡す。

```text
-batchmode -projectPath "<プロジェクト>" -runTests -testPlatform editmode -testResults "<結果XML>" -logFile "<実行ログ>"
```

Test Runnerでは `-quit` を付けず、Editorプロセスの終了コードとXMLの件数・失敗内容を確認する。クラス単位の再実行は `-testFilter SceneGameplayTests` などを追加する。

Windowsビルドの再実行入口:

```text
-batchmode -projectPath "<プロジェクト>" -executeMethod ProjectAssetReferenceTests.BuildWindowsPlayer -logFile "<ビルドログ>"
```

出力先はローカル一時ディレクトリの `kojin-static-player/kojinProjectFeb3D.exe`。有効なBuild SettingsのシーンをWindows x64 Developmentとしてビルドし、成功時0・失敗時1でEditorを終了する。

## 人による確認が必要な範囲

この自動検証では、実際のキーボード・マウスの操作感、画面レイアウトと演出の見やすさ、射撃音の聴取、長時間プレイの難易度と性能を判断しない。移動は参照・速度・照準方向・無入力時の処理までを確認し、方向キーを押しての移動感は別途確認する。D-01〜D-11の機能・例外条件の自動確認と、これらの体験品質の確認を区別する。

## 関連文書

- [設計レビューの改修タスク](program-design-review.md)
- [拡張性向上タスク](architecture-improvement.md)
- [プレイヤー能力値と強化の現行仕様](../specs/player-stats-and-upgrades.md)
