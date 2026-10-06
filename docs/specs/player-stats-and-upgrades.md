# プレイヤー能力値と強化の現行仕様

タグ: `現行仕様` `プレイヤー` `体力` `弾数` `強化` `攻撃処理` `弾プール` `ScriptableObject` `初期設定`

## 体力

- `Assets/Scripts/Player/Gameplay/PlayerCoreGameplay.cs` の `MaxHealth` を別の値に設定すると、現在体力も新しい最大体力に合わせ、`OnHealthChanged` を通知する。同じ値を設定した場合は更新しない。
- `Assets/Scripts/Player/Gameplay/PlayerCoreGameplay.cs` の `ApplyPowerUp()` は、最大体力の加算、乗算の順に適用し、移動速度と射撃能力の変更を担当コンポーネントへ振り分ける。最大体力が変わると現在体力が新しい最大値になる。
- `Assets/Scripts/Player/Presentation/PlayerStatusViewPresentation.cs` は初回の購読、再有効化、体力変更時に、体力バーと `現在体力/最大体力` の数値を更新する。`PlayerCoreGameplay.Start()` と UI の `Start()` の順序に依存しない。
- `PlayerInitialStatsConfiguration` の `PlayerBaseStats` は最大体力100・移動速度1000を定義し、`Player.prefab` が参照する。`InGame.unity` の配置個体は最大体力50・移動速度1000の `InGamePlayerStats` を参照する。`PlayerCoreGameplay` と `PlayerMoveGameplay` は開始時に個体ごとの値へコピーし、強化後も設定アセットを変更しない。

## InGame シーンの弾数と強化

- `Player.prefab` は `PlayerWeapon`、`InGame.unity` の配置個体は最大装弾数5発・拡散角20度・弾の滞在時間15秒の `InGamePlayerWeapon` を参照する。`PlayerAttackGameplay` は `WeaponDefinitionConfiguration` の値を個体ごとの実行時値へコピーし、`PlayerStatusViewPresentation.Start()` は通知を購読した直後に現在の残弾数を描画する。開始順やシーンに保存された表示文字列に依存しない。
- `Enemy.prefab` は `EnemyWeapon`、派生Prefab `Enemy Type Beta.prefab` は `EnemyBetaWeapon` を参照する。`InGame.unity` の個別調整した派生敵は `InGameEnemyWeapon` を参照する。各 `EnemyAttackGameplay` は弾の値と残弾を個体ごとに保持する。いずれの設定アセットもプレイ中に書き換えない。
- 強化ボタンの `UpgradeParametersConfiguration` はPrefab・シーンに保存する値型の設定であり、選択時に値を渡す。強化による加算・乗算はプレイヤーの実行時値にだけ適用する。
- 強化候補「最大体力 +100%／ダメージ -50%」のパラメータは `MaxHealthMultiply = 2`、`DamageMultiply = 0.5`。`PlayerCoreGameplay.ApplyPowerUp()` が最大体力を2倍にし、`PlayerAttackGameplay.ApplyPowerUp()` が弾のダメージを半分にする。最大体力の変更時に現在体力も更新される。

## 強化候補と経験値

- `LevelUpViewPresentation` は非表示の強化パネル内にあるボタンも抽選対象として取得し、候補の表示と並び替えを担当する。`UpgradeCandidateSelectionDomain` はボタン配列の添字を候補IDとして受け取り、Unityに依存せず重複のない3件を抽選する。乱数は `LevelUpCoordinatorGameplay` から渡す。
- 候補が3件未満ならエラーを出して抽選を中止する。再抽選にはトークン3件が必要で、抽選が成功した後にだけ消費する。失敗時は表示中の候補とトークン残高を維持する。
- 経験値、レベル、強化選択回数は敵撃破時に確定し、経験値バーの演出は確定後の値を表示する。
- `ExperienceProgressionDomain` は `UnityEngine` に依存せず、必要経験値の設定値を起動時に複製して、現在経験値、レベル、未使用の強化選択回数を管理する。`LevelUpCoordinatorGameplay.GainExperience()` は獲得値を渡し、確定したレベル・進捗率・選択回数を `LevelUpViewPresentation` へ渡す。強化ボタンは `TrySpendUpgradeChoice()` が成功したときだけ強化を適用する。
- `InGame.unity` では `LevelUpCoordinatorGameplay` と `LevelUpViewPresentation` を同じGameObjectに置く。必要経験値の設定は管理側、経験値バー・文字・候補ボタンへの参照は表示側に保持する。表示側は起動時に現在値を描画し、経験値バーの演出が中断されたら確定済みの進捗率に合わせる。演出は経験値やレベルを変更しない。
- 必要経験値が空、または0以下を含む場合は進行状態の生成時に設定エラーとして例外を出す。負の獲得経験値も拒否する。通常の獲得と強化選択では、未使用回数を0未満にしない。
- `InGame.unity` の必要経験値リストの最終レベルに到達したら、追加の経験値は蓄積せずバーを満タンに固定する。撃破数と再抽選トークンは引き続き増える。

## 敵の射線

- 敵の範囲判定ではプレイヤーのレイヤーマスクを使い、Raycast の衝突相手との比較ではレイヤー番号を使う。参照コードは `Assets/Scripts/Enemy/Gameplay/EnemyCoreGameplay.cs`、`EnemyAttackGameplay.cs`、`EnemyMoveGameplay.cs`。

## 攻撃処理の有効期間

- `BulletSpreadCalculatorDomain` は要求された同時発射数と残弾から発射数を決め、各弾の左右の拡散角をUnityに依存せず算出する。残弾が不足しても要求された同時発射数の角度配置を維持し、先頭から残弾分だけ発射する。単発の角度は中央の0度。
- `BulletFireSequenceGameplay` はプレイヤーと敵に共通するUnity側の発射手順で、弾プールから取得し、パラメータ・発射口の位置・拡散角から求めた向きを設定した後に弾を初期化して残弾を消費する。プレイヤーの弾数通知は発射ごとに行い、敵にはUI通知を渡さない。弾の速度と効果音は弾の初期化時に設定・再生される。
- `WeaponAmmoStateDomain` はプレイヤーと敵に共通の純粋C#の実行時状態で、最大装弾数、残弾、射撃可能状態、発射間隔・再装填の区別と時間を保持する。発射ごとに1発を消費し、残弾が0になった射撃後は再装填待機、残弾があれば発射間隔の待機へ移る。再装填完了で残弾を最大値に戻す。最大装弾数の強化時も残弾を満タンにする。
- `PlayerAttackGameplay` と `EnemyAttackGameplay` はそれぞれ入力・射線判定と弾の生成を担当し、状態の判定を `WeaponAmmoStateDomain` に委ねる。非同期待機、キャンセル、プレイヤーの弾数UI通知はUnity側で行う。
- `PlayerAttackGameplay` と `EnemyAttackGameplay` は有効化時に非同期待機のキャンセル用オブジェクトを作り、無効化時に待機を中断して破棄する。初期化前に無効化されても安全に終了する。
- 発射間隔または再装填の待機中に無効化された場合、再有効化後にその待機を最初からやり直す。プレイヤーの射撃ボタン押下状態は無効化時に解除する。
- 必須参照の取得に失敗して初期化が完了しなかった攻撃コンポーネントは、後続の `Update()` で射撃処理を進めない。

## 体力・弾数UIの購読期間

- `PlayerStatusViewPresentation` と `EnemyHealthViewPresentation` は初回の `Start()` で必須参照を確定して購読する。最初の `OnEnable()` が通知元の `Awake()` より先になる場合を避け、初期体力・弾数は購読直後に現在値から表示する。
- 以後は `OnEnable()` で購読と再描画、`OnDisable()` で解除する。名前付きメソッドで登録・解除し、コンポーネントまたはGameObjectの無効化、再有効化、破棄を同じ期間で扱う。敵の体力バーも現在値を即時表示し、最大体力が0以下なら表示比率を0にする。
- プレイヤーUIは無効化時に再装填・発射間隔バーのTweenを停止する。再有効化時は `PlayerAttackGameplay` の読み取り専用の待機種類・開始時の時間・残り時間から進捗を復元し、再装填は線形、発射間隔は従来のイージングで残りの演出を行う。待機中でないバーは完了表示にする。
- UIだけを無効化しても攻撃側の待機は進む。攻撃側も無効化した場合の待機再開始は既存仕様を維持する。表示は待機の完了や残弾の確定を担当しない。

## シーン内の必須参照

- `SceneReferenceResolverInfrastructure.RequireUnique<T>()` は、プレイヤー、カメラ、レベル管理、弾プールのように要求元と同じシーンに一つ必要なコンポーネントを初期化時に取得する。別シーンと非アクティブなGameObjectは対象に含めず、0件・複数件では要求元と必要な型を含む例外を出す。同じGameObjectの部品は `GetComponent()` で取得し、必須部品とInspector参照は呼び出し側でnullチェックする。取得方法の一覧は [コンポーネントの必須参照と取得範囲](component-dependencies.md) を参照する。
- `PlayerStatusViewPresentation` は同じプレイヤーの `PlayerCoreGameplay.Attack` を使う。弾の効果音は `BulletPoolInfrastructure` に設定された `AudioSource` をプール生成時に渡し、GameObject 名に依存しない。
- 現行の `InGame.unity` はプレイヤー、カメラ、レベル管理、弾プールを各1件置き、弾プールの `_soundEffects` に `SE` の AudioSource を設定する。

## 弾プールの破棄

- `BulletPoolInfrastructure.OnDisposePoolObject()` は、Unity がプールをクリアする時点で弾のコンポーネントが既に破棄されていれば何もしない。生存する弾だけを破棄し、シーンとプールの破棄順序による例外を防ぐ。
- `BulletShotGameplay.OnDisable()` は待機を中断・破棄した後にキャンセル用オブジェクトの参照をnullにする。返却済みの弾を再取得し、次の発射初期化より前に破棄しても、破棄済みオブジェクトへ再びアクセスしない。2026-10-06に `BulletLifetimeTests.ReturnedBulletCanBeDestroyedBeforeNextShotInitialization` でプレイヤー弾と敵弾の両方を検証した。

## 確認状況

- 2026-10-06: 現行コード・シーン・設定でUnity Test Runnerの56件がすべて成功し、終了コード0。敵死亡から経験値への接続、実際の弾衝突、保存されたButton.onClickからの強化、初期表示・被弾・再装填、遮蔽判定、参照不足の診断までを自動確認した。具体的な範囲と再実行手順は [GUI操作なしでのゲーム機能検証](../tasks/headless-verification.md) を参照する。以下の旧検証記録は当時の範囲を示す。
- 2026-10-03: A-08 は Unity 2022.3.62f2 の Test Runner で `UiEventLifetimeTests.ViewersSubscribeOnlyWhileEnabledAndRestoreCurrentState` を実行し、1件成功・失敗0件、Editor終了コード0。Edit ModeテストからPlayへ移行し、実際の `InGame.unity` の初期表示、3回のUI無効化・再有効化と購読数、無効期間中の状態変更と再表示、GameObject全体の切り替え、再装填・発射間隔のTween停止と途中復元、通知元を残したUI破棄後の解除を確認した。結果XMLはローカル一時ディレクトリの `kojin-a08-tests-final.xml`、実行ログは `kojin-a08-tests-final.log`。コンパイルも成功した。テストは `Assets/Editor/UiEventLifetimeTests.cs` に残し、Test RunnerのEditModeで同じクラス名を指定して再実行できる。射撃・移動は止めてUIの寿命を検証しており、GUIの手動目視は含まない。

- 2026-09-30: コード、Prefab、Unity YAML の静的照合で確認。上記のシーン参照は各1件で、アセットの `.meta` GUID に重複はなかった。Unity Editor での再生確認は未実施。`dotnet build Assembly-CSharp.csproj --no-restore` は、指定バージョンの Unity Source Generator がないため失敗した。
- 2026-10-01: 弾プールの破棄処理は Unity 2022.3.62f2 のバッチモードで、破棄済み弾の確認と Play モード2回の往復を確認。両回でプレイヤー弾と敵弾の発射時初期化が通過した。
- 2026-10-01: A-01 の経験値計算は一時的な .NET コンソール検証で、連続獲得、複数レベル上昇、最大レベル、設定値の複製、不正な必要経験値、選択回数の消費を確認。Unity 2022.3.62f2 の一時チェックアウトで `InGame.unity` をPlayし、レベル31、獲得選択回数31、バー満タン、選択回数をすべて消費した後のUI表示0を確認した。敵を実際に倒すGUI操作は含まない。
- 2026-10-01: A-02 の抽選は一時的な .NET コンソール検証で、候補IDの一意性、候補不足、トークン不足、再抽選成功・失敗時の残高を確認。Unity 2022.3.62f2 の一時チェックアウトで `InGame.unity` をPlayし、初回・再抽選後・強化選択後に各3件が表示されることを確認した。候補を2件にした再抽選はエラーを記録し、表示中の候補とトークン3件を保持した。GUIの手動操作は含まない。
- 2026-10-01: A-03 は Unity 2022.3.62f2 の一時チェックアウトで改修前後を比較し、初期表示、強化メニュー開閉、途中経験値・レベル上昇・最大レベルの表示、演出中の連続更新、再抽選後の候補3件を確認した。改修後は表示コンポーネントだけを無効化した際、バーが確定済み進捗へ戻り、経験値状態が変わらないことも確認した。GUIの手動目視は含まない。
- 2026-10-01: A-04 は一時的な .NET コンソールで発射、弾切れ、発射間隔、再装填、待機の再開始、最大装弾数変更、個体間の状態独立を確認。Unity 2022.3.62f2 の一時チェックアウトで、改修前後の `InGame.unity` を同じバッチPlay手順で比較し、プレイヤーと敵の発射・再装填・無効化と再有効化後の射撃を確認した。両ログに同じ弾の命中エフェクトの `MissingReferenceException` があり、攻撃状態の確認は通過した。GUIの手動操作は含まない。
- 2026-10-02: A-05 は一時的な .NET コンソールで単発中央、3発の左右角度、残弾不足時の先頭2発、残弾0と不正な添字を確認。Unity 2022.3.62f2 の一時チェックアウトで `InGame.unity` をバッチPlayし、プレイヤー・敵それぞれ3発設定／残弾2発から2発が生成され、向きが22.5度と0度、残弾が0、初期化時の速度が向きと設定速度に一致することを確認した。GUIの手動操作は含まない。
- 2026-10-03: A-06 は Unity 2022.3.62f2 の一時チェックアウトで `InGame.unity` をバッチPlayし、プレイヤーの初期体力50・移動速度1000・最大装弾数5、通常敵・派生敵・個別調整した派生敵の設定参照を確認した。強化後の体力・弾威力・装弾数は実行時だけ変更され、設定アセットと同じ設定を参照する別個体は変わらなかった。Playを停止・再開した後は初期体力50・移動速度1000・最大装弾数5に戻った。GUIの手動操作は含まない。
- 2026-10-03: A-07の共通検証導入時は Unity 2022.3.62f2 のバッチ検証で、必須参照の未設定、ローカル部品・シーン内参照の欠落と重複の診断、加算ロードした別シーンと非アクティブなGameObjectの除外を確認した。プレイヤー、敵、派生敵、両弾Prefabの参照と欠落スクリプトの有無を検証し、`InGame.unity` のPlayでシーン参照、プレイヤーUIの参照、プレイヤー・敵の射撃が通過した。その後、ローカル部品・Inspector参照の共通ヘルパーを削除し、呼び出し側のGetComponentとnullチェックへ簡素化した。簡素化後は差分レビューとUnityコンパイルで確認し、Playの再検証は行っていない。GUI操作と射撃音の聴取は含まない。
