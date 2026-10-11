# Mermaid概要・ノード説明の日本語点検

タグ: `日本語校閲` `yomiyasu` `設計文書` `検証証拠` `再開情報`

- 確認日: 2026-10-11
- 対象HEAD: `9191a92a88966e3c9284777059e97ac1a20273c3`。既存V-02差分は保持し、今回の推敲では変更していない。
- 対象: [設計図索引](../specs/architecture-diagrams.md)に登録された11図の概要と125ノード説明。全136フィールドを独立した説明として点検した。
- 原本: [architecture-descriptions.json](../specs/architecture-descriptions.json)。概要11件、ノード説明15件を修正し、自然な110ノード説明は保持した。

## 推敲の根拠と判断

`yomiyasu`のSKILL.md、references/gemini-syntax.md、references/domains/tech.mdを全文確認した。使用したSKILL.mdのSHA256は `ad44dde5255970ca705fd1821c810920b08781731005f1152f75f7b1514fdb29`。
図を「読む」と指示する概要を、図が示す対象・流れ・境界の説明へ改めた。名詞の連続をほどき、死亡確定と通知の順序、同じシーンの参照を一意取得する条件、再抽選成功時のトークン消費を保った。
根拠は [SceneReferenceResolverInfrastructure](../../Assets/Scripts/Scene/Infrastructure/SceneReferenceResolverInfrastructure.cs)、[EnemyCoreGameplay](../../Assets/Scripts/Enemy/Gameplay/EnemyCoreGameplay.cs)、[UpgradeCandidateSelectionDomain](../../Assets/Scripts/Progression/Domain/UpgradeCandidateSelectionDomain.cs) と、原本の各referencesである。

lintはscore=90、info候補5件。共有定義と個体状態、interfaceと実装Core、操作と管理側の検証経路を区別する否定4件と、その密度候補1件は意味を担うため保持した。diff（stance=説明）の条件・接続語・新旧語・文数の候補は対応する文章とコードで確認した。条件の追加ではなく、既存の抽選成功条件を述語で明示したもの。常体と説明の機能を維持し、技術名UnityEvent.OnDiedを保持する調整を1回行った。lint/diffは各2回、全図validateは1回実行した。

## 検証・保全と再開

[共通CLI契約](C:/Users/Taomo/.codex/session-routing/architecture-docs-tool.md)に従いdoctor、inventory、apply --write、validate、sync previewを実行。validateは11図・125ノード、ok=true、errors=[]、missing=[]。sync previewはchanged=[]。
説明以外のJSONフィールド、図原文・ID・source・references、設計Markdownと既存SVG、着手前10ファイルの内容と既存staged差分を保全した。製品コード・アセット設定の変更やUnity起動は行っていない。
変更前後の全文とスキル候補、代表3件の比較、独立コミットSHAは `C:/Users/Taomo/.codex/session-routing/requests/yomiyasu-survivor-reply.json` から辿れる。残作業なし。次回は [設計文書更新手順](../specs/architecture-document-workflow.md)に従い現行コードと原本を照合する。
