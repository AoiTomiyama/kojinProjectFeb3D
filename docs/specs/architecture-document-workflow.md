# 設計図とタスクリストの共通CLI更新手順

タグ: `現行運用` `Metis` `Chronos` `説明JSON` `更新保護` `再開情報`

確認日: 2026-10-14。設計図はMetis、タスクはChronosの共通CLIで更新する。設計図の登録対象は[図式索引](architecture-diagrams.md)の11図で、原本は固定図ID付きMermaidと[architecture-descriptions.json](architecture-descriptions.json)。タスク原本は[task-list.md](../tasks/task-list.md)の1ファイルである。

共通工具とビューアはAtlas開発担当が所有する。入口はローカルAtlasの[README](../../../../.codex/atlas/README.md)、契約は[Metis更新仕様](../../../../.codex/atlas/docs/metis/metis-update-tool.md)と[Chronos更新仕様](../../../../.codex/atlas/docs/chronos/chronos-update-tool.md)、カテゴリIDは[共通契約](../../../../.codex/session-routing/docs/routing/chronos-category-ids.md)を参照する。工具の質問・結果はハブから指定された返信JSONに保存し、担当ターミナルで要点を返す。

## Metisの更新順

以下はPowerShellから実行する。patchとinventoryは自担当の一時ファイルを使う。図の概要には対象・流れ・境界、各ノードには役割・状態・関連と根拠を記述する。

```powershell
python -X utf8 C:/Users/Taomo/.codex/atlas/metis.py doctor --project survivor
python -X utf8 C:/Users/Taomo/.codex/atlas/metis.py inventory --project survivor --output "$env:TEMP/survivor-architecture-inventory.json"
# inventoryのbase_revisionと全図/ノードIDを使い、共通契約に従った説明patchを用意する
python -X utf8 C:/Users/Taomo/.codex/atlas/metis.py apply --project survivor --patch "$env:TEMP/survivor-description-patch.json"
python -X utf8 C:/Users/Taomo/.codex/atlas/metis.py apply --project survivor --patch "$env:TEMP/survivor-description-patch.json" --write
python -X utf8 C:/Users/Taomo/.codex/atlas/metis.py validate --project survivor
python -X utf8 C:/Users/Taomo/.codex/atlas/metis.py sync --project survivor
```

doctorの未決journalやrenderer整合エラーを解決してから書込する。ノードIDはinventoryのMermaid解析結果を使う。新しい登録図のIDや説明の雛形を作る場合はsyncをpreviewしてから--writeする。applyはbase_revisionの一致を要求するため、競合時は最新inventoryからpatchを作り直す。

## Metisの完了条件と内容確認

全図validateをallow-incompleteなしで実行し、ok=true・errors=[]・missing=[]を確認する。sync再実行のpreviewでchanged=[]となることも確認する。ノードの根拠はリポジトリ内相対pathを1件以上付け、ファイル存在に加え本文とコード/配線の責務・状態・参照を照合する。外部API・集約ノード・操作主体には実装の利用箇所や仕様/依存定義を使い、実装されていない設計理由を付けない。

図を変更する場合はMermaidと説明を同じpatchに入れ、削除ノードはremove_nodesへ明記する。原本・説明・無関係差分の保全を確認し、関連記録と共に文書目的だけを独立コミットする。SVGは過去の閲覧資料として保持し、本手順の検証結果をゲーム動作や描画比較の再検証として報告しない。

役割分類は各図のpresentation.rolesで全leafノードへ指定する。foundation/middle/surface/externalの意味は[凡例](architecture-diagrams.md#責務分類と配色の凡例)と共通契約に従い、名前や矢印だけで判断せずコードと説明を確認する。presentationは全置換なので、変更時もその図の全rolesを含める。flowchartのsubgraphには説明と根拠を追加し、グループ自体をrolesへ含めない。classDiagramのnamespaceは説明用の分類であり、架空クラスを追加しない。leafのID・ラベル・関係・メンバー・注記と既存の説明・根拠を保持し、配置用の矢印は追加しない。

## Chronosの作成根拠と更新順

新担当は[知識索引](../knowledge/README.md)から目的・設計・現行仕様・採用判断・検証証拠を読む。コードと作業ツリーを照合し、既存タスクとの重複・部分対応を確認する。新しい提案の採用はユーザーが決める。採用された項目は、根拠を既存の正本へ記録してからタスク原本へ反映する。

現在の原本はChronos v2。YAMLはid・uid・category・title・status・updated_at、本文は問題・解決策・結論を各1〜2文にまとめる。uidは旧D/A/R/Vの固定ID、表示idはカテゴリ英字3文字と01〜99の優先順である。categoryは原本の日本語名を使い、updated_atは共通工具が設定する。表示ID変更後も更新JSONのtasksキーには固定uidを使う。

```powershell
python -X utf8 C:/Users/Taomo/.codex/atlas/chronos.py inventory --project survivor > "$env:TEMP/survivor-task-inventory.json"
# 最新base_revisionと固定uidを使ってschema_version=2の更新JSONを用意する
python -X utf8 C:/Users/Taomo/.codex/atlas/chronos.py apply --project survivor --request "$env:TEMP/survivor-task-update.json"
python -X utf8 C:/Users/Taomo/.codex/atlas/chronos.py apply --project survivor --request "$env:TEMP/survivor-task-update.json" --write
python -X utf8 C:/Users/Taomo/.codex/atlas/chronos.py validate --project survivor
```

指定したtitle・status・sectionsを更新し、省略した項目を保持する。新規項目はユーザーの採用判断と根拠に基づき、固定uid・表示id・日本語category・title・status・3節を揃える。再採番する場合は共通契約に従い、対応表・本文参照・uidの保持を確認する。

VFX-01（uid: V-02）はユーザーが破棄を決めた停止項目である。工具の進捗値に破棄専用の値がないため、blockedと結論で決定を記録する。既存差分の処理は後続指示で扱う。SET-01のblockedは参照仕様待ちであり、両者の判断理由は各タスクの結論を読む。

更新後は件数・ID/uid・状態・本文・日時・参照を確認し、変更した判断と根拠の対応を照合する。採用状態と実装完了を読み分け、対象の文書だけを独立コミットする。日本語はyomiyasuで推敲し、prose-gateのerrorを修正して反映する。warningは文脈で判断する。
