# kojinProjectFeb3D 作業ガイド

タグ: `作業方針` `Unity` `設計レビュー` `再開情報`

## 共通規則との関係

このローカル環境では `~/.codex/AGENTS.md` の対話・記録・作業効率・Git運用規則を適用する。報告は `~/.codex/knowledge/policies/review-verification-reporting.md`、Unityの参照・GUID維持と生成物保護は `~/.codex/knowledge/policies/unity-project-workflow.md` を正本とし、作業に該当する規則だけ参照する。別環境でこのガイドを再利用するときは共通規則も併せて提供する。固有条件は以下に残す。

## 新担当の入口

現行のゲームは、プレイヤーの移動・射撃、敵の追跡・射撃、撃破による経験値と強化選択を接続している。引継ぎでは次の順に正本を読み、対象コードと作業ツリー差分を照合する。

| 確認する内容 | 正本 |
| --- | --- |
| 目的・責務・設計判断 | [目標アーキテクチャ](docs/knowledge/target-architecture.md)、[現行設計と図式](docs/specs/architecture-diagrams.md) |
| 能力値・死亡・強化の動作 | [能力値と強化](docs/specs/player-stats-and-upgrades.md)、[強化一覧とUI](docs/specs/upgrade-catalog-and-ui.md) |
| 採用済み作業・状態・固定uid | [Chronosタスクリスト](docs/tasks/task-list.md) |
| 状態の作成根拠と採用判断 | [知識索引とタスク再構成](docs/knowledge/README.md) |
| タスク・設計図の更新 | [共通CLI更新手順](docs/specs/architecture-document-workflow.md) |

タスクの追加提案はユーザーが採用を決める。採用された判断を既存の仕様・設計文書へ記録し、対応するタスクを共通CLIで更新する。独立した担当業務の会話は共通AGENTS.mdの「担当セッションとプロジェクト知識」に従う。

## 環境と実装状態

- Unity の指定バージョンは `ProjectSettings/ProjectVersion.txt` にある `2022.3.62f2`。
- 環境設定の確認（2026-10-06）: manifestの直接依存45件とロックファイルが一致し、更新された9パッケージの導入済み実体も解決バージョンと一致した。同じEditor・Packages設定での最新のテスト・ビルド結果は `docs/tasks/headless-verification.md` を参照する。
- ゲームの C# コードは `Assets/Scripts/`。ビルド設定で有効なシーンは `Assets/Scenes/InGame.unity` の1件。
- `Packages/manifest.json` は UniTask と lilToon を Git URL から取得する。
- プレイヤーの移動・射撃、敵の追跡・射撃、弾のプール、経験値・強化 UI が実装されている。動作の保証はコードとシーンの静的調査だけではできない。
- 設計レビューの旧D-01〜D-11は修正・自動検証済み。旧IDはChronos原本のuidとして保持している。検証証拠は [既存検証記録](docs/tasks/headless-verification.md) と [死亡保証の検証記録](docs/tasks/death-once-verification.md) を参照する。
- 旧A-01〜A-09と旧R-01/R-03は完了。既存改善項目は2026-10-12のユーザー判断で実施対象となり、旧A-10〜A-13は未着手である。採用と実装の進捗は [タスク原本](docs/tasks/task-list.md) と [判断の根拠](docs/knowledge/README.md) で読み分ける。

- 強化項目はSOの一覧から既存Prefabを使って動的生成する。追加手順は [現行仕様](docs/specs/upgrade-catalog-and-ui.md)、実装・検証結果は [旧R-03の記録](docs/tasks/upgrade-catalog-ui.md) を参照する。
- VFX-01（旧uid: V-02）は2026-10-14のユーザー判断で廃止し、作業差分を取り消してタスク原本から削除した。[廃止判断と処理根拠](docs/knowledge/README.md#vfx-01の廃止判断と差分処理)を参照する。

## 作業上の注意

- Git 管理下のテキストは UTF-8 で保存する。C# も Shift_JIS / CP932 へ戻さず、既存の改行を維持する。新規ファイルは `.editorconfig` に従う。
- ゲーム用スクリプトは `Assets/Scripts/<機能>/<レイヤー>/` に置き、型名・ファイル名の末尾に `Domain`、`Gameplay`、`Presentation`、`Configuration`、`Infrastructure` を付ける。設定アセットは `Assets/GameData/`、Editorテストは `Assets/Editor/` に置く。契約の配置と例外、改名一覧は `docs/specs/script-layout-and-naming.md` を参照する。
- 不具合候補はコード上の根拠と Unity Editor で確認した事実を区別する。指定バージョンの Editor で再生できない場合はその旨を報告する。
- GUIなしで確認できる項目は既存のUnity Test Runnerテストを使って自走する。新しい重要な条件には `Assets/Editor/` の回帰テストを追加し、使い捨ての検証スクリプトを増やさない。Editorプロセスの終了コード、結果XML、例外ログを確認する。Windowsビルドは `ProjectAssetReferenceTests.BuildWindowsPlayer` を使用する。

## 知識の記録

共通知識の参照・昇格は `~/.codex/AGENTS.md` の「二層集合知」「共通規則の適用と昇格」に従う。プロジェクト固有の仕様は `docs/specs/`、採用した設計判断は `docs/knowledge/`、タスク状態と検証証拠は `docs/tasks/` に記録する。
