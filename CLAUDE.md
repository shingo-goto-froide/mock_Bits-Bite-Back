# CLAUDE.md - Claudeとの作業ガイド（共通テンプレート）

> このファイルはどのUnityプロジェクトでも使い回せる共通ガイドです。
> CLIおよびVSCode拡張ではセッション開始時に自動で読み込まれます。
> `.claude/rules/` にある詳細ルールもCLI起動時に自動ロードされる。
> Claude Desktopでは、セッション開始時にこのファイルを手動で読み込ませること（UnityMCPとの接続・プレイテスト用）。

---

## .claude/ 構成

```
.claude/
├── settings.json          ← 権限・Hooks設定
├── rules/                 ← Claudeへの指示（起動時に自動ロード）
│   ├── 01-rules.md        ← ✅ 行動ルール（必ず守ること・禁止事項）
│   ├── 02-workflow.md     ← フェーズ詳細・コマンド・エージェント一覧
│   ├── 03-docs.md         ← Docs構成・更新ルール・Gitコミット
│   ├── 05-templates.md    ← 各種テンプレート
│   ├── 06-repo-ops.md     ← テンプレートリポジトリ運用・gen-scripts並列グループ
│   └── 07-troubleshooting.md ← セッションプロンプト集
├── agents/                ← 専門家エージェント（独立コンテキスト）
│   ├── spec-writer.md     ← 仕様書の対話的作成・更新
│   ├── scene-builder.md   ← シーン構築・変更
│   ├── unity-debugger.md  ← バグ調査・修正
│   └── doc-updater.md     ← ドキュメント差分確認・更新
├── commands/              ← 人が起動するワークフロー（/コマンド名）
│   ├── new-spec.md        ← 企画フェーズ起点
│   ├── gen-design.md      ← 設計フェーズ起点
│   ├── gen-scripts.md     ← スクリプト一括生成
│   ├── gen-scene.md       ← シーン構築
│   ├── playtest.md        ← 面白さの検証
│   ├── check-diff.md      ← 差分確認
│   ├── debug.md           ← バグ調査・修正
│   └── update-docs.md     ← Docs更新
└── skills/                ← 将来: commands が複雑化したら移行を検討
    └── （現在は commands で対応）
```

---

## モック制作フロー

```
企画 → 設計 → コアループ実装 → プレイテスト → 残り実装 → テスト
              ↑_______________|                  |
              |（面白くない・仕様変更）           |（バグ・仕様変更）
              ↑___________________________________|
```

### ドキュメント依存関係
```
仕様書 → 設計書 → スクリプト → シーン構成書 → シーン構築
```

> プレイテストで「面白くない」と判断したらコアループ設計からやり直す。残り実装に進まない。

---

## クイックリファレンス

### コマンド一覧（人が起動）

| コマンド | フェーズ | 内容 |
|---|---|---|
| `/new-spec` | 企画 | 仕様書を対話形式で作成・PROJECT.md自動生成 |
| `/gen-design` | 設計 | 設計書を自動生成 |
| `/gen-scripts [core\|full]` | 実装 | スクリプト生成（core: コアループのみ、full: 残り、引数なし: 全部） |
| `/gen-scene [core\|full]` | 実装 | シーン構築（core: 最小限、full: 残り、引数なし: 全部） |
| `/playtest` | 実装・テスト | 面白さの検証・問題分類 |
| `/check-diff` | テスト | 実装とドキュメントの差分確認 |
| `/debug` | テスト | バグ調査・修正 |
| `/update-docs` | 共通 | Docs一式を更新 |

### エージェント一覧（専門家に直接依頼 or コマンドから委譲）

| エージェント | 役割 |
|---|---|
| `@spec-writer` | 仕様書の対話的作成・更新 |
| `@scene-builder` | シーン構築・変更 |
| `@unity-debugger` | バグ調査・修正 |
| `@doc-updater` | ドキュメント差分確認・更新 |

---

## ツールの使い分け

```
企画         → Claude Code CLI（/new-spec）
設計         → Claude Code CLI（/gen-design）
実装（前半） → Claude Code CLI（/gen-scripts core → /gen-scene core）
プレイテスト → Claude Desktop（UnityMCP）+ Claude Code CLI（/playtest）
実装（後半） → Claude Code CLI（/gen-scripts full → /gen-scene full）
テスト       → Claude Desktop（UnityMCP）+ Claude Code CLI
```

> **Claude Desktop** は UnityMCP との接続専用。
> **Claude Code CLI** はファイル生成・Git操作・コマンド実行専用。
