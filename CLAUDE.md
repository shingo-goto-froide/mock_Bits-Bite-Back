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
├── agents/                ← 必要になったら追加（初期は空）
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

---

## ツールの使い分け

```
企画           → /new-spec
設計           → /gen-design
スクリプト生成 → /gen-scripts core / full
シーン構築     → /gen-scene core / full（UnityMCP必須）
プレイテスト   → /playtest（UnityMCP推奨）
テスト         → /debug, /check-diff（UnityMCP推奨）
```

> **コード生成・ファイル操作・Git** → MCP不要（CLIの標準機能で十分）
> **シーン構築・UI配置・動作確認** → UnityMCP必須（コードのみだとレイアウトが崩れる）
> MCP サーバーは CLI / VS Code / Desktop どこからでも使用可能。
