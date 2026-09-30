# プレイヤー能力値と強化の現行仕様

タグ: `現行仕様` `プレイヤー` `体力` `弾数` `強化`

## 体力

- `Assets/Scripts/Player/PlayerCore.cs` の `MaxHealth` を別の値に設定すると、現在体力も新しい最大体力に合わせ、`OnHealthChanged` を通知する。同じ値を設定した場合は更新しない。
- `Assets/Scripts/Player/PlayerAttack.cs` の `ApplyPowerUp()` は、最大体力の加算、乗算の順に適用する。最大体力が変わるたびに現在体力が新しい最大値になる。
- `Assets/Scripts/Player/PlayerUIViewer.cs` は `OnHealthChanged` を受けて体力バーと数値を更新する。初期表示の更新順と数値の並びに関する改善は `docs/tasks/program-design-review.md` の D-06 に記録している。

## InGame シーンの弾数と強化

- `Assets/Scenes/InGame.unity` のプレイヤーは最大装弾数を5発に上書きし、弾数 UI の初期文字列も `5` に設定している。射撃開始時には `PlayerAttack.Start()` が弾数を最大値に設定する。
- 強化候補「最大体力 +100%／ダメージ -50%」のパラメータは `MaxHealthMultiply = 2`、`DamageMultiply = 0.5`。`PlayerAttack.ApplyPowerUp()` の計算により最大体力が2倍、弾のダメージが半分になり、最大体力の変更時に現在体力も更新される。

## 強化候補

- `LevelUpSystemManager` は非表示の強化パネル内にあるボタンも抽選対象として取得する。候補が3件未満ならエラーを出して抽選を中止し、再抽選トークンを消費しない。候補は重複なしで3件を選ぶ。

## 敵の射線

- 敵の範囲判定ではプレイヤーのレイヤーマスクを使い、Raycast の衝突相手との比較ではレイヤー番号を使う。参照コードは `Assets/Scripts/Enemy/EnemyCore.cs`、`EnemyAttack.cs`、`EnemyMove.cs`。

## 確認状況

- 2026-09-30: コード、Prefab、Unity YAML の静的照合で確認。Unity Editor での再生確認は未実施。
