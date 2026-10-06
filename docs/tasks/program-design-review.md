# プログラム設計レビューの改修タスク

タグ: `設計レビュー` `戦闘` `強化` `経験値` `Unity`

## 記録の前提

- 対象: `Assets/Scenes/InGame.unity` と `Assets/Scripts/` の現行コード。
- 根拠: 2026-09-30 時点のコード・Unity YAML の静的調査。Unity Editor での再生確認は未実施。
- 状態: D-01〜D-07 のコード・設定修正は実施済みで、指定バージョンの Unity Editor による再生確認待ち。チェックは完了条件を確認した後に付ける。
- 進め方: 上から順に対応する。各タスクで必要な範囲の修正と検証を行う。
- 静的確認: `InGame.unity` に強化候補 Prefab は11件あり、必要経験値は31件すべて正の値。`git diff --check` は通過。指定 Unity 2022.3.28f1 がないため、Unity Editor の再生確認は保留。`dotnet build Assembly-CSharp.csproj --no-restore` は同バージョンの Unity Source Generator がなく失敗した。
- 2026-10-01 の自己レビューで追加した D-08 以降は、コードとシーン設定から導いた未修正の不具合候補。再生による再現確認は未実施。
- 起動確認（2026-10-01）: 作業ツリーで Unity バージョンが未コミットの `2022.3.62f2` に変更されている状態で、同バージョンの Editor が `InGame.unity` を読み込み、Play モードへ2回移行したことを `Editor.log` で確認した。ログ内に `error CS` と `Compilation failed` はない。D-01〜D-07 の個別操作と期待動作は、この起動記録だけでは確認できないため未完了のままにする。

## 優先度: 高

### [ ] D-01 強化候補ボタンの初期化を修正する

- 実施状況: 非アクティブな子ボタンも収集し、候補不足時はエラーを出す。重複しない3件の抽選に変更し、抽選失敗時はトークンを消費しない。コード確認済み、再生確認待ち。
- 追加確認（2026-10-01）: A-02 改修前の Unity 2022.3.62f2 バッチPlayで、`InGame.unity` の候補11件から初回3件を表示し、トークン3件の再抽選後も3件、強化を1件選んだ後も3件を表示することを確認した。候補不足とGUIでの操作はこの確認に含めていないため、チェックは保留する。
- 変更前の問題: `LevelUpSystemManager.Start()` は強化パネルを非表示にした後、既定の `GetComponentsInChildren<Button>()` で候補を収集して `Reroll()` を呼んでいた。非アクティブな子ボタンが収集されず、候補選択が失敗する可能性が高かった。
- 根拠: `Assets/Scripts/LevelUpSystemManager.cs` の `Start()`、`Reroll()`。`InGame.unity` では `LayoutGroup` が強化パネルの子にある。
- 完了条件: パネルの初期表示状態に依存せず全候補を取得できる。候補が3件未満なら明確なエラーを出し、無限ループや不正な抽選をしない。
- 確認: シーン開始後に強化メニューを開き、3つの候補が表示される。選択後と再抽選後にも3つ表示される。

### [ ] D-02 敵の射線判定でレイヤー番号を使う

- 実施状況: 番号 `PlayerLayer` とマスク `PlayerLayerMask` を分け、範囲判定と Raycast で使い分けるよう変更。コード確認済み、再生確認待ち。
- 変更前の問題: Raycast の衝突相手の `gameObject.layer` をレイヤーマスクと比較していた。プレイヤーはレイヤー7で、マスクは `1 << 7` となるため一致しなかった。
- 根拠: `Assets/Scripts/Enemy/EnemyCore.cs` のレイヤー保持、`EnemyAttack.cs` と `EnemyMove.cs` の Raycast 判定、`Assets/Prefab/Player.prefab` の `m_Layer: 7`。
- 完了条件: レイヤー番号とマスクの用途・名前を分け、敵の射撃と停止判定がプレイヤーの視認時に成立する。
- 確認: 遮蔽物がない場合は敵が射撃範囲で停止して発砲し、遮蔽物がある場合は発砲しない。

### [ ] D-03 経験値とレベルを同期的に確定する

- 実施状況: 撃破時に経験値とレベルを確定し、バー演出と分離した。最終レベル後の経験値は蓄積せず、バーを満タンに保つ。コード確認済み、再生確認待ち。
- 追加確認（2026-10-01）: A-01 改修前の Unity 2022.3.62f2 バッチPlayで、`InGame.unity` の必要経験値を用いて `GainExperience()` を同一Play中に連続呼び出した。レベル31、強化選択31回、バー満タン、再抽選トークン2件、例外なしを確認した。敵の撃破操作と獲得演出の目視確認は含まないため、チェックは保留する。
- 変更前の問題: 経験値の消費とレベル加算を DOTween の完了コールバックで行い、その前に非同期待機していた。短時間に複数の敵を倒すと処理が重なり、同じ経験値を重複評価し得た。必要経験値リストの末尾も無条件に参照していた。
- 根拠: `Assets/Scripts/LevelUpSystemManager.cs` の `GainExpAsync()` と、`InGame.unity` の `_requireExpList`。
- 完了条件: 経験値・レベル・強化選択回数を一つの処理で確定し、バーの演出は確定済み状態を表示する。リスト末尾に達した場合の動作を定義する。
- 確認: 複数の敵を同時に倒しても獲得経験値とレベルが一度ずつ反映される。最大設定レベル到達後も例外が出ない。

## 優先度: 中

### [ ] D-04 強化適用の責務を射撃処理から分離する

- 実施状況: `PlayerCore.ApplyPowerUp()` が体力と移動速度を変更し、射撃関連だけを `PlayerAttack` へ渡す。コード確認済み、再生確認待ち。
- 変更前の問題: `PlayerAttack.ApplyPowerUp()` が射撃に加えて最大体力と移動速度も変更していた。能力値を増やすたびに射撃クラスの変更が必要だった。
- 根拠: `Assets/Scripts/Player/PlayerAttack.cs` の `ApplyPowerUp()`、`Assets/Scripts/Progression/Configuration/UpgradeParametersConfiguration.cs`。
- 完了条件: 強化適用の窓口で各能力値を担当コンポーネントへ振り分ける。現在の強化内容と選択時の動作を維持する。
- 確認: 体力、移動、攻撃、装弾数の強化をそれぞれ選び、表示と実際の値が一致する。

### [ ] D-05 シーン内の必須参照を明示する

- 改修前の追加証拠（2026-10-03）: A-06完了時のUnity 2022.3.62f2バッチPlayログでは `InGame.unity` の開始、プレイヤー・敵の初期化、強化、Play停止・再開が通過し、必須参照取得に関する例外は記録されなかった。A-07の比較前提として使用する。射撃音の聴取とGUI操作は含まない。
- 実施状況: `SceneReferenceResolverInfrastructure.RequireUnique<T>()` で初期化時に対象が1件であることを検証するよう変更。プレイヤーの射撃 UI は同一オブジェクトの攻撃コンポーネントを参照し、効果音出力は弾プールから弾へ渡す。`InGame.unity` でプレイヤー、カメラ、レベル管理、弾プール、効果音参照が各1件であること、敵弾はプレイヤー弾の Prefab Variant として `BulletShotBehaviour` を継承すること、`.meta` GUID の一意性を静的に確認済み。再生確認待ち。
- 変更前の問題: 複数のスクリプトが `FindAnyObjectByType`、`Camera.main`、`GameObject.Find("SE")` に依存していた。対象の改名・未配置・複数配置時に、意図した参照先を保証できなかった。
- 根拠: `Assets/Scripts/BulletShotBehaviour.cs`、`Player/PlayerAttack.cs`、`Enemy/EnemyCore.cs`、`CursorPointer.cs` など。
- 完了条件: 必須の参照を Inspector または初期化処理で明示し、不足時は対象が分かるエラーを出す。取得方法を変更する際は、プール生成時の弾にも参照を渡す。
- 確認: `InGame.unity` で全参照が解決し、射撃音、敵 AI、照準、強化 UI が動作する。

### [ ] D-06 体力 UI の初期値と表示順を修正する

- 実施状況: イベント購読直後に現在値を描画し、数値を `現在体力/最大体力` の順に変更した。コード確認済み、再生確認待ち。
- 変更前の問題: `PlayerUIViewer.Start()` は体力変更イベントを購読するだけで現在値を即時描画していなかった。`PlayerCore.Start()` との順序により初期表示が古くなった。数値は `最大体力/現在体力` の順で出力されていた。
- 根拠: `Assets/Scripts/Player/PlayerUIViewer.cs` と `PlayerCore.cs` の `Start()`。
- 完了条件: 購読直後に現在値を描画し、数値の表示順を決めて一貫させる。
- 確認: 開始直後、被弾後、最大体力強化後のバーと数値が一致する。

### [ ] D-07 プレイヤー Prefab の最大体力のシリアライズを揃える

- 実施状況: `Player.prefab` の基本値を `_maxHealth: 100` に修正し、`InGame.unity` の個別上書き `_maxHealth: 50` を維持した。Unity YAML 確認済み、再生確認待ち。
- 変更前の問題: `PlayerCore` の現行フィールド名は `_maxHealth` だが、`Player.prefab` には旧名 `MaxHealth: 100` が残っていた。`InGame.unity` の個別オーバーライド `_maxHealth: 50` に依存しており、Prefab を別シーンに置くと最大体力が既定値0になる可能性があった。
- 根拠: `Assets/Scripts/Player/PlayerCore.cs`、`Assets/Prefab/Player.prefab`、`Assets/Scenes/InGame.unity` のプレイヤー Prefab オーバーライド。
- 完了条件: Prefab 自体に有効な `_maxHealth` を設定し、シーン側との差を意図した設定として整理する。既存の `.meta` GUID と参照を維持する。
- 確認: Prefab 単体と `InGame.unity` の双方で開始体力が意図した値となる。

### [ ] D-08 弾数 UI の初期値を攻撃コンポーネントから描画する

- 実施状況: UI が弾数変更イベントを購読した直後に `RemainBulletCount` を描画するよう修正。コード確認済み、再生確認待ち。
- 変更前の問題: `PlayerAttack.Start()` は残弾数を設定して変更イベントを通知していたが、`PlayerUIViewer.Start()` の購読が後になると通知を受け取れなかった。`InGame.unity` では弾数表示の初期文字列と最大装弾数がともに5なので目立たず、最大装弾数だけ変更すると射撃まで古い値が表示され得た。
- 根拠: `Assets/Scripts/Player/PlayerAttack.cs` の `Start()` と `RemainBulletCount`、`PlayerUIViewer.cs` の `Start()`、`Assets/Scenes/InGame.unity` の弾数 UI 初期文字列。
- 完了条件: UI が購読直後に攻撃コンポーネントの現在残弾数を描画し、`Start()` の実行順やシーンに保存された文字列に依存しない。
- 確認: 最大装弾数を初期文字列と異なる値にして開始し、射撃前、射撃後、再装填後の表示が実際の残弾数と一致する。

### [ ] D-09 攻撃処理のキャンセル用オブジェクトを有効期間に合わせて管理する

- 実施状況: キャンセル用オブジェクトを有効化時に生成し、無効化時は null を許容して破棄する。初期化が完了していない間は `Update()` を進めず、中断した発射間隔・再装填の待機は再有効化後にやり直す。コード確認済み、再生確認待ち。
- 追加確認（2026-10-01）: A-04 改修前の Unity 2022.3.62f2 バッチPlayで、`InGame.unity` のプレイヤーと敵を各1発ずつ発射し、発射間隔後に残弾が維持されること、弾切れ後の再装填待機中に無効化・再有効化して満タンへ戻り再射撃できることを確認した。`Start()` 前の無効化も両コンポーネントで実行した。必須参照欠落時の元の例外とGUI操作は確認に含まないため、チェックは保留する。弾の命中エフェクトに関する `MissingReferenceException` はこの改修前ログに存在し、攻撃状態の確認自体は通過した。
- 変更前の問題: `PlayerAttack` と `EnemyAttack` は `Start()` の必須参照取得後に `_cts` を生成する一方、`OnDisable()` では無条件に `Cancel()` と `Dispose()` を呼んでいた。参照取得失敗後や `Start()` 前の無効化では `NullReferenceException` が重なり得た。一度無効化したコンポーネントを再有効化しても、破棄済み `_cts` の `Token` を射撃時に取得していた。
- 根拠: `Assets/Scripts/Player/PlayerAttack.cs` と `Assets/Scripts/Enemy/EnemyAttack.cs` の `Start()`、`Update()`、`OnDisable()`。D-05 で追加した `RequireUnique<T>()` は参照が不足または重複すると例外を送出する。
- 完了条件: 初期化に失敗した場合も無効化処理が安全に終了し、再有効化後の射撃で有効なキャンセル用オブジェクトを使用する。参照不足の元の例外が確認できる。
- 確認: 必須参照を欠いた状態での無効化、正常に初期化した攻撃コンポーネントの無効化・再有効化・射撃をそれぞれ確認する。

### [x] D-10 Play モード切り替え時の弾プール破棄エラーを修正する

- 実施状況: `OnDisposePoolObject()` が破棄済みコンポーネントを受け取った場合は `gameObject` にアクセスせず終了するよう修正。Unity 2022.3.62f2 のバッチモードで確認済み。
- 検証結果（2026-10-01）: 一時チェックアウトに同じシーン・Editor 設定を用意し、元のコードでは破棄済み弾を渡す確認で `MissingReferenceException` を再現した。修正後は同じ確認が通過し、Play モードの開始・停止・再開始を2回実施してプール由来の例外は0件。各 Play モードでプレイヤー弾・敵弾の取得、発射時初期化、返却を確認した。GUI の手動操作はこの検証に含めていない。
- 観測事実: 2026-10-01、Unity 2022.3.62f2 の Editor で Play モードを繰り返した際、`Editor.log` に `MissingReferenceException` が1件記録された。スタックトレースは `BulletPoolInfrastructure.OnDisposePoolObject()` の `parameter.gameObject` を指し、Unity の `ObjectPool<T>.Clear()` と `PoolManager.Reset()` から呼ばれている。ログには Domain Reload と Scene Reload が無効である旨も記録されている。
- 原因候補: プールのリセット時、既に破棄された `BulletShotBehaviour` を取り出して `gameObject` にアクセスしている。破棄順序とプール内の参照の寿命を確認する。
- 根拠: `Assets/Scripts/Combat/Infrastructure/BulletPoolInfrastructure.cs` の `OnDisposePoolObject()` と、上記 Unity Editor の実行ログ。
- 完了条件: 弾の破棄とプールのクリアが、参照先の破棄順序に依存せず安全に終わる。
- 確認: 同じ Editor 設定で Play モードの開始・停止・再開始を繰り返し、例外が出ず、再開始後もプレイヤー弾と敵弾が使える。
