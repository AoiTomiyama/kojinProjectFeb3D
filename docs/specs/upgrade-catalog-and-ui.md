# 強化項目の一覧と動的ボタンUI

タグ: `現行仕様` `強化` `ScriptableObject` `候補抽選` `動的UI`

## 設定の正本と追加手順

`Assets/GameData/Progression/InGameUpgrades.asset` が強化項目の一覧。各項目は `UpgradeDefinitionConfiguration` アセットで、正の一意な整数ID、タイトル、表示列、効果値、候補の有効設定・最低レベルを持つ。実行中の取得回数、選択状態、残高は保存しない。

1. `Create > Game > Upgrade Definition` で定義を作る。既存と重複しない正のIDを設定する。使用済みIDは並び替えで変更しない。
2. タイトル、表示列、`_effect`（加算・乗算）、`_available`、`_minimumLevel` を設定する。新規定義の乗数は1、加算値は0で始まる。
3. `InGameUpgrades` のDefinitionsへ追加する。専用Inspectorでは一覧と各定義を同じ画面で編集できる。実行中の一覧編集は対象外で、次回起動時に反映する。
4. シーンへのボタン個別配置は不要。`LevelUpViewPresentation` が既存 `UpgradeButton.prefab` から項目数分を生成する。

初期能力値・武器値は既存のPlayerInitialStats/WeaponDefinitionを継続使用し、強化一覧へ複製しない。`UpgradeParametersConfiguration` は効果値の値型として継続使用する。効果は定義だけに保存し、ボタンPrefabとシーンには保存しない。

## 移行した11項目

| ID | タイトル | 既存の主要効果 |
| --- | --- | --- |
| 1 | Bounce | 反射+3、弾速×0.7 |
| 2 | Fast Shot | 弾速×1.6、発射間隔×1.3 |
| 3 | Tactical | 再装填時間×0.75 |
| 4 | Charge Shot | ダメージ×1.5、発射間隔×1.8 |
| 5 | Machine Gun | ダメージ×0.5、装弾数+10、発射間隔×0.7 |
| 6 | Glass Shot | ダメージ×2.5、最大体力×0.5 |
| 7 | More HP | 最大体力×1.3 |
| 8 | Quick Shot | 弾速+1.3、ダメージ×0.9、発射間隔×0.9 |
| 9 | Guadian | 最大体力×2、ダメージ×0.5 |
| 10 | Forge | ダメージ×2、装弾数-3 |
| 11 | Spread Bullet | 同時発射数+2、装弾数+2、ダメージ×0.7 |

効果値と表示文言を移行し、倍率・加算や綴りの調整はしていない。例: Quick Shotの弾速は表示+30%に対して実装が+1.3、Fast Shotの発射間隔は×1.3。Tacticalには元Prefabの未調整表示が残っていた。表示と効果の整合性調整は別の仕様変更として扱う。Machine Gun、Quick Shot、Guadian、Spread Bulletのシーン側の追加列も定義へ移した。

## 候補抽選と選択

- 管理側はAwakeで一覧配列を複製し、欠落参照・0以下のID・重複IDを拒否する。IDはボタン順やシーン階層から算出しない。
- 抽選時に有効かつ現在レベルが最低レベル以上の定義からIDを集め、既存の `UpgradeCandidateSelectionDomain` で重複のない3件を選ぶ。既存11項目は全て有効・最低レベル0で、取得回数の上限を設けない。
- 再抽選は従来どおりトークン3件。候補不足時は失敗させ、残高と表示中の候補を保持する。既存のエラー文言は維持する。
- ボタンは表示した定義のIDを管理側へ渡す。管理側は現在の選出・候補条件・残選択回数を検証する。成功時に選択回数を1消費し、適用前に候補を解除・非表示にして再入を防ぐ。定義の効果を実行時能力値に適用し、候補を再抽選する。
- 最大レベルで経験値を蓄積しない、未使用選択回数を保持する、撃破数とトークンが引き続き増える仕様は維持する。

## 表示・クリック接続と再生成

既存Prefabのルート寸法、画像、フォント、配置先を再利用する。既存 `Button.onClick → Upgrade()` を1回使い、Bindは通知先を置き換える。AddListenerを繰り返さない。元の各シーンボタンのクリック音はViewの共通AudioSource/AudioClipへ移した。

基本2列に加えてAdditionalColumnsを必要数だけ生成する。追加列もPrefab内の既存列を複製する。表示テキストは効果値の自動計算ではなく、定義に保存した文言を使用する。

`LevelUpCoordinatorGameplay.RefreshUpgradeButtons()` は起動時に複製した定義からUIを再生成し、現在の候補IDを表示し直す。レベル・残高・選択回数・メニュー開閉状態を変えず、再抽選もしない。古いボタンは接続解除・非表示・親から分離して破棄し、破棄待ちでもクリックできない。管理GameObjectの無効化・再有効化でも状態を再初期化しない。経験値バーのTween中断は確定値へ戻す既存処理を使う。

## 関連コードと検証

- [定義](../../Assets/Scripts/Progression/Configuration/UpgradeDefinitionConfiguration.cs)、[一覧](../../Assets/Scripts/Progression/Configuration/UpgradeCatalogConfiguration.cs)、[一覧Inspector](../../Assets/Editor/UpgradeCatalogEditorInfrastructure.cs)
- [管理](../../Assets/Scripts/Progression/Gameplay/LevelUpCoordinatorGameplay.cs)、[View](../../Assets/Scripts/Progression/Presentation/LevelUpViewPresentation.cs)、[ボタン](../../Assets/Scripts/Progression/Presentation/UpgradeButtonPresentation.cs)
- [回帰テスト](../../Assets/Editor/UpgradeCatalogTests.cs)、[作業・検証記録](../tasks/upgrade-catalog-ui.md)
