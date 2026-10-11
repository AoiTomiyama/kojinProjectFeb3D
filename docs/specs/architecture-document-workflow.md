# 設計図とノード説明の共通CLI更新手順

タグ: `現行運用` `Mermaid` `説明JSON` `更新保護` `再開情報`

確認日: 2026-10-11。登録対象は[設計図索引](architecture-diagrams.md)の11図。原本は各Markdown内の固定図ID付きMermaidと[architecture-descriptions.json](architecture-descriptions.json)。図の概要は対象・流れ・境界、各ノードは役割・扱う状態/データ・関連要素と根拠を記述する。

CLIとビューアは独立ハブ所有で、契約の正本は[architecture-docs-tool.md](../../../../.codex/session-routing/architecture-docs-tool.md)（ローカル共通環境内の参照）。本プロジェクト担当はCLIを改変せず、設計Markdown・説明JSON・関連記録だけを更新する。CLI不具合はハブへaskする。既存のゲームコード・設定・アセット・無関係差分を保持し、説明作業でUnity起動やSVG生成を行わない。

## 更新順

以下はPowerShellから実行する。patchとinventoryは自担当の一時ファイルを使い、別担当やハブ所有ファイルへ書き込まない。

```powershell
python C:/Users/Taomo/.codex/session-routing/architecture_docs.py doctor --project survivor
python C:/Users/Taomo/.codex/session-routing/architecture_docs.py sync --project survivor --write
python C:/Users/Taomo/.codex/session-routing/architecture_docs.py inventory --project survivor --output "$env:TEMP/survivor-architecture-inventory.json"
# inventoryのbase_revisionと全図/ノードIDを使い、共通契約に従った説明patchを用意する
python C:/Users/Taomo/.codex/session-routing/architecture_docs.py apply --project survivor --patch "$env:TEMP/survivor-description-patch.json" --write
python C:/Users/Taomo/.codex/session-routing/architecture_docs.py validate --project survivor
python C:/Users/Taomo/.codex/session-routing/architecture_docs.py sync --project survivor
```

doctorの未決journalやrenderer整合エラーを解決してから書込する。syncは既存Mermaidに固定IDを付け説明の空欄を追加し、既存IDと説明を保持する。ノードIDはinventoryのMermaid解析結果を使い、独自パーサーで推定しない。applyはbase_revision不一致時に停止するので、更新後に古いpatchを強制適用しない。

## 完了条件と内容確認

全図validateをallow-incompleteなしで実行し、ok=true・errors=[]・missing=[]を確認する。sync再実行のpreviewでchanged=[]となることも確認する。ノードの根拠はリポジトリ内相対pathを1件以上付け、ファイル存在に加え本文とコード/配線の責務・状態・参照を照合する。外部API・集約ノード・操作主体には実装の利用箇所や仕様/依存定義を使い、実装されていない設計理由を付けない。

図を変更する場合はMermaidと説明を同じpatchに入れ、削除ノードはremove_nodesへ明記する。原本・説明・無関係差分の保全を確認し、関連記録と共に文書目的だけを独立コミットする。SVGは過去の閲覧資料として保持し、本手順の検証結果をゲーム動作や描画比較の再検証として報告しない。
