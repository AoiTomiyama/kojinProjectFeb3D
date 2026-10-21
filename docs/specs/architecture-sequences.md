# 起動・射撃・死亡・強化のシーケンス

タグ: `現行仕様` `シーケンス` `初期化` `死亡再入` `Mermaid`

対象版は[索引](architecture-diagrams.md)。`->>`は同期呼出、`-->>`は返値/通知、`Note`は条件と寿命、`alt/opt/loop`は分岐/繰返し。異なるコンポーネントのStart順序を全順序として保証しない。図のStart処理は各コンポーネント内の順序を列挙したもの。コード入口は[対応表](architecture-script-catalog.md)。

## 起動：状態生成と配線

<!-- diagram-id: architecture-sequences-1 -->
```mermaid
sequenceDiagram
    participant U as Unity
    participant C as PlayerCore / Attack
    participant M as Coordinator
    participant S as 設定SO
    participant V as LevelUpView
    participant P as BulletPool
    participant E as EnemyCore / Move
    participant R as SceneResolver
    U->>C: Awake（Core死亡=false、設定複製、Attack残弾状態生成）
    C->>S: 初期能力値/武器値を読む
    U->>M: Awake
    M->>M: Progression/Selectionをnew
    M->>S: Catalog.CreateSnapshot（検証・配列copy）
    M->>V: 同一Object GetComponent
    Note over U,E: Sceneの有効コンポーネントはAwake/OnEnable後にStart<br/>異なるStart間の相対順は図で保証しない
    U->>P: Start
    P->>S: 弾カタログを読む
    P->>P: 種類別ObjectPool作成、生成・初期化、prewarmして返却
    U->>M: Start
    M->>R: RequireUnique PlayerCore
    R-->>M: 同一sceneのPlayer
    M->>V: Initialize → CreateButtons → 候補描画
    U->>C: 各Start
    C->>C: Core.Health=MaxHealth / Attack参照取得・残弾初期化
    U->>E: 各Start
    E->>R: PlayerとCoordinatorを一意探索
    E->>E: Health=Max、対象/mask設定、OnDeath接続
    E->>E: MoveがNavMeshAgentを有効化
    Note over U,E: NavMeshSurfaceのOnEnable登録後にAgentを有効化<br/>UIはStart初期化後に購読、初回値を描画
```

[起動SVG](diagrams/sequence-startup.svg)。SOは読取元で初期値を個体へ複製。SceneResolverの成功は同一シーンで必要コンポーネントが一意かつ活動中であることが条件。非活動Objectや未設定参照は診断対象。

## 射撃・命中・返却

<!-- diagram-id: architecture-sequences-2 -->
```mermaid
sequenceDiagram
    participant A as PlayerAttack / EnemyAttack
    participant W as 個体AmmoState
    participant F as FireSequence
    participant P as BulletPool
    participant B as BulletShot
    participant D as IDamageableの対象Core
    participant UI as PlayerStatusView
    A->>W: IsReady / RemainingAmmoを読む
    Note over A: Playerは入力とメニュー、Enemyは距離/射線を確認
    A->>F: Fire（武器定義の弾種、値、muzzle、向き、発数）
    F->>F: SpreadCalculator.GetShotCount/GetAngle
    loop 発射可能な弾数
      F->>P: Get(type)
      P->>B: SetActive(false)
      P-->>F: 弾参照
      F->>B: Parameter・position・forward設定
      F->>B: SetActive(true) → OnGetFromPool
      B->>B: 任意の音・CTS寿命待機・速度・命中回数初期化
      F->>W: TryConsumeShot
      opt Playerのみ
        F-->>UI: OnAmmoCountChanged（callback経由）
      end
    end
    par 攻撃側の非同期待機
      A->>W: StartWait（cooldown又はreload）
    opt Playerのみ
      A-->>UI: OnCoolDownBegin / OnReloadBegin
    end
    A->>A: UniTask.Delay（CTS取消可能）
    A->>W: 完了時CompleteWait
      Note over A: 無効化時Cancel、未完了なら再有効化で待機を再開始
    and 弾側の物理衝突（待機完了との前後は任意）
    B->>B: OnCollisionEnter、hit effect、hitCount++
    opt hitCountがRicochetCount超過
      B->>P: OnReturnToPool
      P->>B: Release → inactive → OnDisableで寿命Cancel
    end
    B->>D: TryGetComponent成功ならDamage(Parameter.Damage)
    B->>B: damage text生成
    Note over B,D: 現行実装は返却判定の後にDamageを呼ぶ<br/>命中なしでも寿命終了で返却
    end
```

[射撃SVG](diagrams/sequence-fire.svg)。待機種類を確定してUIへ通知した後、Delayをawaitする。プールの有効化が配置より先である現状を変更案に置き換えない。

## 死亡：通知中の再入を含む一度きり保証

<!-- diagram-id: architecture-sequences-3 -->
```mermaid
sequenceDiagram
    participant Caller as 弾/被弾呼出元
    participant Core as EnemyCore / PlayerCore
    participant H as 体力通知の購読者
    participant M as Coordinator
    participant D as OnDied購読者
    Caller->>Core: Damage(amount)
    alt 既に_isDead
      Core-->>Caller: 更新せずreturn
    else 生存
      Core->>Core: nextHealth算出、becameDeadをローカル保存
      opt 今回が致死
        Core->>Core: _isDead=true（通知より先）
      end
      Core->>Core: Health=nextHealth
      Core-->>H: OnHealthChanged
      opt 購読者がDamageへ再入
        H->>Core: Damage(extra)
        Note over Caller,H: 既に死亡なら即return<br/>非致死通知から致死再入なら<br/>内側の呼出しだけが死亡を発行
      end
      alt この呼出しのbecameDead=true
        alt Enemy
          Core-->>M: OnDeath(exp) → GainExperience
          opt 報酬通知から再入
            M->>Core: Damage(extra) → 死亡済みreturn
          end
          Core-->>D: OnDied
          opt 死亡通知から再入
            D->>Core: Damage(extra) → 死亡済みreturn
          end
          Core->>Core: death effect生成
        else Player
          Core->>Core: death effect生成
          Core-->>D: OnDied
          opt 死亡通知から再入
            D->>Core: Damage(extra) → 死亡済みreturn
          end
        end
        Core->>Core: SetActive(false)
      else この呼出しは非致死
        Core-->>Caller: 死亡副作用を追加発行しない
      end
    end
```

[死亡SVG](diagrams/sequence-death.svg)。敵の報酬通知からの再入も死亡済みreturnになる。通知からの再入は各通知の同期呼出スタック内で起きる。Health setter自体は死亡判定しない。復活/敵再利用のresetは未実装で、Awake以外で死亡を解除しない。必須effect不備などの例外に対するトランザクション/再試行保証ではない。[R-01証拠](../tasks/death-once-verification.md)参照。

## 強化選択：定義IDから個体値へ

<!-- diagram-id: architecture-sequences-4 -->
```mermaid
sequenceDiagram
    actor User as ユーザー
    participant B as UpgradeButton
    participant M as Coordinator
    participant E as ExperienceProgression
    participant V as LevelUpView
    participant P as PlayerCore
    participant A as Move / Attack
    participant S as CandidateSelection
    User->>B: Prefab Button.onClick → Upgrade
    B->>M: Bind済みAction(id) → SelectUpgrade
    M->>M: 選出ID・定義存在・IsEligible確認
    M->>E: TrySpendUpgradeChoice
    alt 有効で残回数あり
      E-->>M: true（残回数確定）
      M->>V: ShowUpgradeChoices
      M->>M: _selectedIdsからID除去
      M->>V: HideCandidate(id)
      M->>P: ApplyPowerUp(definition.Effect)
      P->>P: MaxHealth計算、変更時Healthを同値へ
      P->>A: Speed倍率 / 武器値・容量・待機時間の強化
      M->>M: Eligible IDを更新
      M->>S: TryDraw（ID配列とRandom.Range）
      S-->>M: 3件の重複なしID
      M->>V: ShowCandidates
    else 不正ID/条件/残回数不足
      M-->>B: 変更せず終了
    end
```

[強化SVG](diagrams/sequence-upgrade.svg)。候補解除は効果適用前なので、能力変更通知中に同一候補から再入しても二重適用しない。通常選択後の抽選はtoken不要、シーンTryReroll操作はtoken3消費を伴う別経路。ボタン生成/列配置はView/UpgradeButton、候補判定と能力計算はゲーム側。
