# プレイヤー能力値と強化の現行仕様

タグ: `現行仕様` `プレイヤー` `体力` `弾数` `強化`

## 体力

- `Assets/Scripts/Player/PlayerCore.cs` の `MaxHealth` を別の値に設定すると、現在体力も新しい最大体力に合わせ、`OnHealthChanged` を通知する。同じ値を設定した場合は更新しない。
- `Assets/Scripts/Player/PlayerCore.cs` の `ApplyPowerUp()` は、最大体力の加算、乗算の順に適用し、移動速度と射撃能力の変更を担当コンポーネントへ振り分ける。最大体力が変わると現在体力が新しい最大値になる。
- `Assets/Scripts/Player/PlayerUIViewer.cs` はイベント購読直後と体力変更時に、体力バーと `現在体力/最大体力` の数値を更新する。`PlayerCore.Start()` と UI の `Start()` の順序に依存しない。
- `Assets/Prefab/Player.prefab` の基本最大体力は100。`InGame.unity` の配置個体は `_maxHealth: 50` に上書きされる。

## InGame シーンの弾数と強化

- `Assets/Scenes/InGame.unity` のプレイヤーは最大装弾数を5発に上書きしている。`PlayerAttack.Start()` は残弾数を最大値に設定し、`PlayerUIViewer.Start()` は通知を購読した直後に現在の残弾数を描画する。開始順やシーンに保存された表示文字列に依存しない。
- 強化候補「最大体力 +100%／ダメージ -50%」のパラメータは `MaxHealthMultiply = 2`、`DamageMultiply = 0.5`。`PlayerCore.ApplyPowerUp()` が最大体力を2倍にし、`PlayerAttack.ApplyPowerUp()` が弾のダメージを半分にする。最大体力の変更時に現在体力も更新される。

## 強化候補と経験値

- `LevelUpSystemManager` は非表示の強化パネル内にあるボタンも抽選対象として取得する。候補が3件未満ならエラーを出して抽選を中止し、再抽選トークンを消費しない。
- 候補は重複なしで3件を選ぶ。経験値、レベル、強化選択回数は敵撃破時に確定し、経験値バーの演出は確定後の値を表示する。
- `InGame.unity` の必要経験値リストの最終レベルに到達したら、追加の経験値は蓄積せずバーを満タンに固定する。撃破数と再抽選トークンは引き続き増える。

## 敵の射線

- 敵の範囲判定ではプレイヤーのレイヤーマスクを使い、Raycast の衝突相手との比較ではレイヤー番号を使う。参照コードは `Assets/Scripts/Enemy/EnemyCore.cs`、`EnemyAttack.cs`、`EnemyMove.cs`。

## シーン内の必須参照

- `SceneReferenceResolver.RequireUnique<T>()` は、プレイヤー、カメラ、レベル管理、弾プールのように `InGame.unity` に一つ必要なコンポーネントを初期化時に取得する。0件または複数件なら、要求元と種類を含む例外を出す。非アクティブな GameObject は検索対象に含めない。
- `PlayerUIViewer` は同じプレイヤーの `PlayerCore.Attack` を使う。弾の効果音は `BulletObjectPoolManager` に設定された `AudioSource` をプール生成時に渡し、GameObject 名に依存しない。
- 現行の `InGame.unity` はプレイヤー、カメラ、レベル管理、弾プールを各1件置き、弾プールの `_soundEffects` に `SE` の AudioSource を設定する。

## 確認状況

- 2026-09-30: コード、Prefab、Unity YAML の静的照合で確認。上記のシーン参照は各1件で、アセットの `.meta` GUID に重複はなかった。Unity Editor での再生確認は未実施。`dotnet build Assembly-CSharp.csproj --no-restore` は、指定バージョンの Unity Source Generator がないため失敗した。
