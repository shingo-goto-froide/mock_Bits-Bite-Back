# CLAUDE.md - Claudeとの作業ガイド（共通テンプレート）

> このファイルはどのUnityプロジェクトでも使い回せる共通ガイドです。
> CLIおよびVSCode拡張ではセッション開始時に自動で読み込まれます。
> `.claude/rules/` にある行動ルールもCLI起動時に自動ロードされる。
> Claude Desktopでは、セッション開始時にこのファイルを手動で読み込ませること（UnityMCPとの接続・プレイテスト用）。

---

## .claude/ 構成

```
.claude/
├── settings.json          ← 権限・Hooks設定
├── rules/                 ← 行動ルール（起動時に自動ロード）
│   ├── 01-rules.md        ← 必ず守ること・禁止事項
│   ├── 02-workflow.md     ← フェーズ概要・ツール使い分け
│   └── 03-docs.md         ← Docs構成・更新ルール・Gitコミット
├── skills/                ← 対話型ワークフロー（/スキル名 で起動）
│   ├── new-spec/          ← 企画フェーズ + 仕様書・PROJECT.mdテンプレート
│   ├── gen-design/        ← 設計フェーズ + 設計書テンプレート
│   ├── playtest/          ← 面白さの検証
│   ├── check-diff/        ← 差分確認
│   └── debug/             ← バグ調査・修正
├── agents/                ← 投げっぱなし型サブエージェント（別コンテキストで実行）
│   ├── gen-scripts.md     ← スクリプト一括生成
│   ├── gen-scene.md       ← シーン構築
│   └── update-docs.md     ← ドキュメント更新
└── reference/             ← 自動ロードしないリファレンス
    ├── repo-ops.md        ← テンプレートリポジトリ運用
    ├── session-prompts.md ← セッションプロンプト集
    ├── scene-template.md  ← シーン構成書テンプレート
    └── fix-log-template.md ← 修正記録テンプレート
```

---

## モック制作フロー

```
企画 → 設計 → コアループ実装 → プレイテスト →┬→ 残り実装 → テスト
              ↑_______________|               │
              |（面白くない）                   ├→ 仕様更新 → 設計更新 → 残り実装
              ↑_______________________________|
                            （仕様追加・変更あり）
```

### ドキュメント依存関係
```
仕様書 → 設計書 → スクリプト → シーン構成書 → シーン構築
```

### プレイテスト後の判断
- **面白い＋仕様変更なし** → そのまま残り実装へ
- **面白い＋仕様追加/変更あり** → 仕様書更新 → `/gen-design` で設計更新 → 残り実装
- **面白くない** → 設計フェーズからやり直す。残り実装に進まない

---

## クイックリファレンス

### skills（対話型 — /スキル名 で起動）

| スキル | フェーズ | 内容 |
|---|---|---|
| `/new-spec` | 企画 | 仕様書を対話形式で作成・PROJECT.md自動生成 |
| `/gen-design` | 設計 | 設計書を自動生成（ゲームデザイン + UI + 技術設計） |
| `/playtest` | 実装・テスト | 面白さの検証・問題分類 |
| `/check-diff` | テスト | 実装とドキュメントの差分確認 |
| `/debug` | テスト | バグ調査・修正 |

### agents（投げっぱなし型 — 自然言語 or @mention で起動）

| エージェント | フェーズ | 起動例 |
|---|---|---|
| gen-scripts | 実装 | 「gen-scriptsでcoreスクリプト生成して」 |
| gen-scene | 実装 | 「gen-sceneでcoreシーン構築して」 |
| update-docs | 共通 | 「update-docsでドキュメント更新して」 |

> agents は別コンテキストで実行されるため、メインの会話を圧迫しない。

---

## ツールの使い分け

```
企画           → /new-spec（skill）
設計           → /gen-design（skill）
スクリプト生成 → gen-scripts エージェント
シーン構築     → gen-scene エージェント（UnityMCP必須）
プレイテスト   → /playtest（skill・UnityMCP推奨）
テスト         → /debug, /check-diff（skill・UnityMCP推奨）
ドキュメント更新 → update-docs エージェント
```

> **コード生成・ファイル操作・Git** → MCP不要（CLIの標準機能で十分）
> **シーン構築・UI配置・動作確認** → UnityMCP必須（コードのみだとレイアウトが崩れる）
> MCP サーバーは CLI / VS Code / Desktop どこからでも使用可能。
