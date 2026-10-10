# 設計図SVGの索引と生成条件

タグ: `図式` `閲覧用SVG` `検証証拠` `再開情報`

確認日: 2026-10-11。対象コード: `e87e95adc3de00abba3ce10599032ec9e722c277`。原本はMarkdown内のMermaidブロック。SVGは閲覧用で、実装根拠・矢印凡例・対象差分は[設計図索引](../architecture-diagrams.md)を参照する。

| SVG | Mermaid原本（文書内の掲載順） |
| --- | --- |
| [overview](overview.svg) | [architecture-diagrams.md](../architecture-diagrams.md) |
| [wiring](wiring.svg) | [architecture-diagrams.md](../architecture-diagrams.md) |
| [classes-actors](classes-actors.svg) | [architecture-classes.md](../architecture-classes.md) |
| [classes-combat](classes-combat.svg) | [architecture-classes.md](../architecture-classes.md) |
| [classes-progression](classes-progression.svg) | [architecture-classes.md](../architecture-classes.md) |
| [correlation-combat](correlation-combat.svg) | [architecture-correlations.md](../architecture-correlations.md) |
| [correlation-progression](correlation-progression.svg) | [architecture-correlations.md](../architecture-correlations.md) |
| [sequence-startup](sequence-startup.svg) | [architecture-sequences.md](../architecture-sequences.md) |
| [sequence-fire](sequence-fire.svg) | [architecture-sequences.md](../architecture-sequences.md) |
| [sequence-death](sequence-death.svg) | [architecture-sequences.md](../architecture-sequences.md) |
| [sequence-upgrade](sequence-upgrade.svg) | [architecture-sequences.md](../architecture-sequences.md) |

## 構文・閲覧の確認

- 既存Orca配布物に同梱された `mermaid.core-INM3DhGR.js` と依存chunkを一時ディレクトリで使用。既存ChromeのheadlessとNodeからMermaid `parse`/`render` を実行した。新規package導入、GUI操作、Unity起動は行っていない。
- 11図のparse/renderが成功。生成SVGはXMLとして解析し、viewBoxと参照リンクを確認した。各図をheadlessでPNG化して日本語文字・凡例/矢印ラベル・枠/注記を閲覧確認した。初回後の変更箇所と閲覧ラベル調整は対応SVGへ再反映した。
- flowchartは白いラベル背景、線形の矢印、既存環境の日本語フォント（Yu Gothic/Meiryo）を使用。外部フォントやネットワーク資源は使わない。MermaidのHTMLラベルはDOMのXMLSerializerで単独SVGとして正規化した。
- game実行の新しい検証証拠ではない。ランタイム検証の正本は既存タスク記録。V-02の未追跡Editorテスト参照は現在の作業ツリー上の存在を表す。

## 更新時

既存のMermaid対応ビューア/rendererで原本を編集・解析する。SVG生成環境がない場合も原本は通常のテキストとして読める。生成器やバージョンを替える場合は全図を再生成し、日本語ラベルと注記を閲覧確認する。SVGだけを手編集せず、コード・原本・対応表・SVGを同じ目的の変更単位で更新する。
