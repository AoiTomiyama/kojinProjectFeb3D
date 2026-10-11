# 全Mermaid図の概要・ノード説明の整備

タグ: `検証証拠` `設計文書` `説明JSON` `再開情報`

確認日: 2026-10-11。対象文書HEADはa07af514e59cd045c37b968922c6301181c87309、自作ランタイムはe87e95aのR-01完了コードから変更なし。対象は[図式索引](../specs/architecture-diagrams.md)の登録11図・125ノード。

## 成果と確認

共通CLIのdoctor→sync --write→inventory→apply --write→validateを実施した。doctorは未決journalとrenderer整合エラーなし。syncが既存図に固定IDを付け、空の説明雛形を作成した。applyで11概要・125説明と各ノードの実在する根拠相対pathを適用した。説明は103〜156字で、責務・状態/データ・関連要素を記載した。

完成時のvalidate（allow-incompleteなし）:

```json
{"ok": true, "errors": [], "missing": [], "diagram_count": 11, "node_count": 125}
```

sync再実行はmode=preview、changed=[]、coverageも上記と同じ成功結果。各referenceのリポジトリ内path・ファイル存在と、指定symbolの実装内存在を確認した。本文は現行クラス/契約/配線との照合で記載し、集約や外部ノードは対応する利用箇所・設定・仕様へ結び付けた。特に死亡通知前の確定と副作用順、個体Ammo/共有SO、表示購読の責務を区別した。

元の11 Mermaid本文はHEADから不変で、変更は固定IDコメントと説明JSON・関連運用文書だけ。SVGの差分なし。既存V-02 stagedシェーダー2件と未コミット設定/検証差分を保全し、今回の文書コミットには含めない。Unity起動・SVG生成・製品変更・pushは行わず、この確認をゲーム動作再検証とは扱わない。

## 説明例

死亡図Coreノード:

EnemyCoreとPlayerCoreの被弾処理をまとめた参加者である。致死ならHealth通知より先に死亡フラグを立て、ローカルbecameDeadでこの呼出しの副作用だけを発行する。敵は報酬・死亡通知・演出、プレイヤーは演出・死亡通知の順で最後に無効化する。

根拠は[EnemyCore](../../Assets/Scripts/Enemy/Gameplay/EnemyCoreGameplay.cs)と[PlayerCore](../../Assets/Scripts/Player/Gameplay/PlayerCoreGameplay.cs)。両実装のDamage・死亡ラッチ・通知順を照合した。

## 現在の更新と再開

[共通CLI更新手順](../specs/architecture-document-workflow.md)を追加し、図式索引と旧SVG生成記録の更新節からリンクした。既存SVG生成証拠は過去記録として保持する。次の設計変更ではinventoryの固定IDとbase_revisionを取得し、Mermaidと説明を共通patchで更新して全図validateとsync previewの無変更を確認する。本依頼の残作業なし。
