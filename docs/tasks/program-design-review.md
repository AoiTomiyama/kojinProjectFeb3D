# プログラム設計レビューの改修タスク

タグ: `設計レビュー` `戦闘` `強化` `経験値` `Unity`

## 記録の前提

- 対象: `Assets/Scenes/InGame.unity` と `Assets/Scripts/` の現行コード。
- 根拠: 2026-09-30 時点のコード・Unity YAML の静的調査。Unity Editor での再生確認は未実施。
- 状態: 下記はすべて未着手。実装後に完了条件と再生確認の結果を記録してチェックする。
- 進め方: 上から順に対応する。各タスクで必要な範囲の修正と検証を行う。

## 優先度: 高

### [ ] D-01 強化候補ボタンの初期化を修正する

- 実施状況: 非アクティブな子ボタンも収集し、候補不足時はエラーを出す。重複しない3件の抽選に変更し、抽選失敗時はトークンを消費しない。コード確認済み、再生確認待ち。
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

- 問題: 経験値の消費とレベル加算を DOTween の完了コールバックで行い、その前に非同期待機する。短時間に複数の敵を倒すと処理が重なり、同じ経験値を重複評価し得る。必要経験値リストの末尾も無条件に参照する。
- 根拠: `Assets/Scripts/LevelUpSystemManager.cs` の `GainExpAsync()` と、`InGame.unity` の `_requireExpList`。
- 完了条件: 経験値・レベル・強化選択回数を一つの処理で確定し、バーの演出は確定済み状態を表示する。リスト末尾に達した場合の動作を定義する。
- 確認: 複数の敵を同時に倒しても獲得経験値とレベルが一度ずつ反映される。最大設定レベル到達後も例外が出ない。

## 優先度: 中

### [ ] D-04 強化適用の責務を射撃処理から分離する

- 問題: `PlayerAttack.ApplyPowerUp()` が射撃に加えて最大体力と移動速度も変更する。能力値を増やすたびに射撃クラスの変更が必要になる。
- 根拠: `Assets/Scripts/Player/PlayerAttack.cs` の `ApplyPowerUp()`、`Assets/Scripts/PowerUpParameter.cs`。
- 完了条件: 強化適用の窓口で各能力値を担当コンポーネントへ振り分ける。現在の強化内容と選択時の動作を維持する。
- 確認: 体力、移動、攻撃、装弾数の強化をそれぞれ選び、表示と実際の値が一致する。

### [ ] D-05 シーン内の必須参照を明示する

- 問題: 複数のスクリプトが `FindAnyObjectByType`、`Camera.main`、`GameObject.Find("SE")` に依存する。対象の改名・未配置・複数配置時に、意図した参照先を保証できない。
- 根拠: `Assets/Scripts/BulletShotBehaviour.cs`、`Player/PlayerAttack.cs`、`Enemy/EnemyCore.cs`、`CursorPointer.cs` など。
- 完了条件: 必須の参照を Inspector または初期化処理で明示し、不足時は対象が分かるエラーを出す。取得方法を変更する際は、プール生成時の弾にも参照を渡す。
- 確認: `InGame.unity` で全参照が解決し、射撃音、敵 AI、照準、強化 UI が動作する。

### [ ] D-06 体力 UI の初期値と表示順を修正する

- 問題: `PlayerUIViewer.Start()` は体力変更イベントを購読するだけで現在値を即時描画しない。`PlayerCore.Start()` との順序により初期表示が古くなる。数値は `最大体力/現在体力` の順で出力される。
- 根拠: `Assets/Scripts/Player/PlayerUIViewer.cs` と `PlayerCore.cs` の `Start()`。
- 完了条件: 購読直後に現在値を描画し、数値の表示順を決めて一貫させる。
- 確認: 開始直後、被弾後、最大体力強化後のバーと数値が一致する。

### [ ] D-07 プレイヤー Prefab の最大体力のシリアライズを揃える

- 問題: `PlayerCore` の現行フィールド名は `_maxHealth` だが、`Player.prefab` には旧名 `MaxHealth: 100` が残る。`InGame.unity` の個別オーバーライド `_maxHealth: 50` に依存しており、Prefab を別シーンに置くと最大体力が既定値0になる可能性がある。
- 根拠: `Assets/Scripts/Player/PlayerCore.cs`、`Assets/Prefab/Player.prefab`、`Assets/Scenes/InGame.unity` のプレイヤー Prefab オーバーライド。
- 完了条件: Prefab 自体に有効な `_maxHealth` を設定し、シーン側との差を意図した設定として整理する。既存の `.meta` GUID と参照を維持する。
- 確認: Prefab 単体と `InGame.unity` の双方で開始体力が意図した値となる。
