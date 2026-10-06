# kojinProjectFeb3D 作業ガイド

タグ: `作業方針` `Unity` `設計レビュー` `再開情報`

## 共通規則との関係

このローカル環境では `~/.codex/AGENTS.md` の対話・記録・作業効率・Git運用規則を適用する。報告は `~/.codex/knowledge/policies/review-verification-reporting.md`、Unityの参照・GUID維持と生成物保護は `~/.codex/knowledge/policies/unity-project-workflow.md` を正本とし、作業に該当する規則だけ参照する。別環境でこのガイドを再利用するときは共通規則も併せて提供する。固有条件は以下に残す。

## プロジェクトの現状

- Unity の指定バージョンは `ProjectSettings/ProjectVersion.txt` にある `2022.3.62f2`。
- 環境設定の確認（2026-10-06）: manifestの直接依存45件とロックファイルが一致し、更新された9パッケージの導入済み実体も解決バージョンと一致した。同じEditor・Packages設定での最新のテスト・ビルド結果は `docs/tasks/headless-verification.md` を参照する。
- ゲームの C# コードは `Assets/Scripts/`。ビルド設定で有効なシーンは `Assets/Scenes/InGame.unity` の1件。
- `Packages/manifest.json` は UniTask と lilToon を Git URL から取得する。
- プレイヤーの移動・射撃、敵の追跡・射撃、弾のプール、経験値・強化 UI が実装されている。動作の保証はコードとシーンの静的調査だけではできない。
- 設計レビューの D-01〜D-11 は修正・自動検証済み。2026-10-06に指定Unity 2022.3.62f2のTest Runnerで56件成功、終了コード0。実装タスクは `docs/tasks/program-design-review.md`、範囲・証拠・再実行手順・人による確認事項は `docs/tasks/headless-verification.md` を参照する。
- 拡張性向上の採用済みタスク A-01〜A-09 は完了し、結果は `docs/tasks/architecture-improvement.md` に記録している。現行の責務・依存方向と、強化・弾種・敵行動の追加時に変更・検証する箇所は `docs/specs/gameplay-architecture.md`、設計方針は `docs/knowledge/target-architecture.md` を参照する。純粋C#化の追加候補 A-10〜A-13 は採用未決定。

## 作業上の注意

- Git 管理下のテキストは UTF-8 で保存する。C# も Shift_JIS / CP932 へ戻さず、既存の改行を維持する。新規ファイルは `.editorconfig` に従う。
- ゲーム用スクリプトは `Assets/Scripts/<機能>/<レイヤー>/` に置き、型名・ファイル名の末尾に `Domain`、`Gameplay`、`Presentation`、`Configuration`、`Infrastructure` を付ける。設定アセットは `Assets/GameData/`、Editorテストは `Assets/Editor/` に置く。契約の配置と例外、改名一覧は `docs/specs/script-layout-and-naming.md` を参照する。
- 不具合候補はコード上の根拠と Unity Editor で確認した事実を区別する。指定バージョンの Editor で再生できない場合はその旨を報告する。
- GUIなしで確認できる項目は既存のUnity Test Runnerテストを使って自走する。新しい重要な条件には `Assets/Editor/` の回帰テストを追加し、使い捨ての検証スクリプトを増やさない。Editorプロセスの終了コード、結果XML、例外ログを確認する。Windowsビルドは `ProjectAssetReferenceTests.BuildWindowsPlayer` を使用する。

## 知識の記録

共通知識の参照・昇格は `~/.codex/AGENTS.md` の「二層集合知」「共通規則の適用と昇格」に従う。プロジェクト固有の仕様は `docs/specs/`、採用した設計判断は `docs/knowledge/`、タスク状態と検証証拠は `docs/tasks/` に記録する。
