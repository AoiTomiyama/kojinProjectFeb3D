# 設計図の役割配色と責務分類の検証

タグ: `検証証拠` `Mermaid` `役割配色` `責務分類` `再開情報`

- 確認日: 2026-10-12。対象文書HEAD: `c90020c55d42df9c3d4cae71abee223d75e6b2cd`。
- 対象: [設計図索引](../specs/architecture-diagrams.md)の全11図。共通CLI 1.1.0で原本と説明JSONを更新した。
- 分類: 125 leafノードをfoundation 16件、middle 62件、surface 29件、external 18件へ分類。新しいフロー囲み14件は役割指定の対象に含めず、説明と根拠を付けた。validateのnode_count=139はleaf 125件と囲み14件の合計である。
- グルーピング: 4フロー図にsubgraph 14件、3クラス図にnamespace 12件を追加。4シーケンス図は役割指定だけとし、既存の分岐・並行・注記・処理順を保持した。既存TB/LRの配置方向も保持した。

## 分類の根拠

各leafノードの既存説明とreferencesの実装・配線を根拠に分類した。[凡例](../specs/architecture-diagrams.md#責務分類と配色の凡例)は全図で共通である。

- 純粋C#の進行・抽選・残弾・拡散ルールと、自作のCore参照基底・抽象弾基底はfoundation。例えば[PlayerComponentBaseGameplay](../../Assets/Scripts/Player/Gameplay/PlayerComponentBaseGameplay.cs)は同じObjectのCoreをキャッシュする共通処理、[PooledAttackBaseGameplay](../../Assets/Scripts/Combat/Gameplay/PooledAttackBaseGameplay.cs)は発射値・返却Actionと初期化フックを定義する。
- Core・移動・攻撃・Pool・発射・状態表示はmiddle。表示は既存状態を読む具体実装であり、イベントを受けるという理由だけで接続契約に分類していない。
- [IDamageableDomain](../../Assets/Scripts/Combat/Contracts/IDamageableDomain.cs)、共有設定や値型、[SceneReferenceResolverInfrastructure](../../Assets/Scripts/Scene/Infrastructure/SceneReferenceResolverInfrastructure.cs)、[UpgradeButtonPresentation](../../Assets/Scripts/Progression/Presentation/UpgradeButtonPresentation.cs)の選択ID接続はsurface。これらは呼出しや値の受渡しを担い、個体状態の確定は実装側が行う。シーケンス図の「IDamageableの対象Core」は契約そのものではなく被弾を処理する具体実装なのでmiddle。
- Unity基底・入力/物理・Random・シーン配置・外部ライブラリ・Editor検証・ユーザーはexternal。UnityのMonoBehaviourは自作基底とは分けた。シーンUIのButton.onClickは既存操作の接続入口としてsurfaceに分類した。

囲みは説明用であり、C#の名前空間や新しい管理クラスを表さない。色HEXや配置用の矢印は追加していない。既存概要・ノード説明・referencesは変更せず、新しいフロー囲みの説明だけを追加した。

## 実行と結果

[共通契約](../../../../.codex/session-routing/architecture-docs-tool.md)と[更新手順](../specs/architecture-document-workflow.md)に従い、doctor → 最新inventory → apply preview → apply --write → validate → sync previewを実行した。

- doctor: tool_version=1.1.0、ok=true、未決transactionとrenderer整合エラーなし。
- apply preview: 役割・囲みを含む全11図のMermaid解析/描画と説明の網羅を確認してから書込。囲み説明を図ごとに具体化したプレビューを最終版として適用した。
- validate（allow-incompleteなし、1回）: ok=true、errors=[]、missing=[]、diagram_count=11、node_count=139。
- sync preview: changed=[]。
- 更新後inventory: 元の125ノードのID・ラベル・型が一致。元の全Mermaid文を囲みと字下げを除いて照合し、型付き関係・メンバー・注記の欠落/追加なし。シーケンス原文は完全一致。
- 保全: 既存概要・全125ノードの説明/根拠/source、過去SVG、着手前のV-02関連10ファイルのSHA256と既存staged差分が一致。今回の変更は設計文書だけである。

CLIは既存Mermaidとブラウザーで図を検証する。今回の結果はゲームのテスト・ビルドやV-02描画比較の再実行を意味しない。既存SVGは過去版として保持し、役割配色はハブのビューアがpresentation.rolesから表示する。

## 再開情報

全125ノードの分類と根拠、囲み一覧、検証結果、文書コミットSHAは `C:/Users/Taomo/.codex/session-routing/requests/architecture-presentation-survivor-result.json` に保存する。残作業なし。今後は最新inventoryのbase_revisionと現行実装を照合し、役割・Mermaid・説明を同じpatchで更新する。
