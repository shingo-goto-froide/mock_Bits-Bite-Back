# Unity モック制作フロー

> このリポジトリは Claude Code CLI + Claude Desktop（UnityMCP）を使ったUnityゲームのモック制作テンプレートです。
> このREADMEは**人間向けの操作手順**です。Claudeへの指示は `CLAUDE.md` と `.claude/` を参照してください。

---

## 必要な環境

| ツール | 確認コマンド | 用途 |
|---|---|---|
| Unity Hub + Unity Editor | Unity Hubから起動 | ゲーム開発 |
| Claude Desktop | タスクトレイに表示されているか確認 | UnityMCPとの接続・プレイテスト |
| Claude Code CLI | `claude --version` | 設計・実装フェーズのCLI操作 |
| uv | `uvx --version` | git MCPの実行に必要 |
| git MCP | `claude mcp list` に `git` が表示されるか | Claude CodeからGitを操作 |

### 初回セットアップ

**1. Claude Desktop**
https://claude.ai/download からインストール。

**2. UnityMCP**
Unity Package Manager → Add package from git URL：
```
https://github.com/justinpbarnett/unity-mcp.git
```
インストール後にUnityエディタ上でセットアップ画面が表示される。指示に従い `claude_desktop_config.json` にMCPサーバーを追加する。
接続確認：Claude Desktopで「Unity MCPに繋がってる？」と聞いてエディタ状態が返ればOK。

**3. Claude Code CLI**
```powershell
npm install -g @anthropic-ai/claude-code
```
Node.js が必要。未インストールの場合は https://nodejs.org/ からインストール。

**4. uv（git MCP の前提条件）**
```powershell
winget install astral-sh.uv
```
インストール後ターミナルを再起動して `uvx --version` で確認。

**5. git MCP**
`~/.claude.json` の `projects` キーの直前に以下を追加：
```json
"mcpServers": {
  "git": {
    "command": "cmd",
    "args": ["/c", "uvx", "mcp-server-git"]
  }
},
```
確認：`claude mcp list` に `git` が表示されればOK。

### セットアップ完了チェックリスト
- [ ] Claude Desktop が起動する
- [ ] Unity + UnityMCP が接続できる
- [ ] `claude --version` が表示される
- [ ] `uvx --version` が表示される
- [ ] `claude mcp list` に `git` が表示される

---

## テンプレートから新規プロジェクトを始める

このリポジトリをテンプレートとして使い、新しいUnityプロジェクトを作成できます。

### テンプレートファイル（そのまま使い回せる）

| ファイル | 説明 |
|---|---|
| `CLAUDE.md` | Claudeとの作業フロー・共通ルール |
| `README.md` | 本ファイル（操作手順） |
| `.claude/settings.json` | 権限・Hooks設定 |
| `.claude/rules/` 全ファイル | Claudeへの指示（行動ルール・テンプレート等） |
| `.claude/agents/` 全ファイル | 専門家エージェント定義 |
| `.claude/commands/` の大半 | スラッシュコマンド |

### プロジェクトごとに生成されるファイル（テンプレートに含まない）

| ファイル | 生成タイミング |
|---|---|
| `PROJECT.md` | 企画フェーズ（`/new-spec` で自動生成） |
| `Assets/Docs/仕様書_v*.md` | 企画フェーズ |
| `Assets/Docs/設計書_v*.md` | 設計フェーズ |
| `Assets/Docs/シーン構成書.md` | 実装フェーズ |
| `Assets/Docs/修正記録.md` | テストフェーズ |
| `Assets/Scripts/` | 実装フェーズ |
| `Assets/Scenes/` | 実装フェーズ |

### 新規プロジェクトの手順

1. **Unityで新規プロジェクトを作成する**
2. **テンプレートファイルをコピーする**
   - `CLAUDE.md`、`README.md`、`.claude/` フォルダ一式をプロジェクトルートに配置
3. **環境セットアップ**（初回のみ。上記「必要な環境」を参照）
4. **Claude Code CLI を起動して `/new-spec` から開始する**

> ⚠️ `.claude/commands/gen-scripts.md` の並列グループは設計フェーズ完了後に更新が必要です（唯一の手作業）。

---

## モック制作フロー

### ① 企画フェーズ
**ツール：** Claude Code CLI

```powershell
cd {プロジェクトルート}
claude          # Claude Code を起動
/new-spec       # 企画開始
```

1. `/new-spec` を打つ
2. Claudeとゲームデザインについて対話する（1問ずつ答えるだけ）
3. 「これでいく」と伝える → `Assets/Docs/仕様書_v1.0.md` と `PROJECT.md` が自動生成される
4. 内容を確認して「コミットして」と依頼

---

### ② 設計フェーズ
**ツール：** Claude Code CLI

```
/gen-design     # 設計書を自動生成
```

1. `/gen-design` を打つ → `Assets/Docs/設計書.md` が自動生成される
2. 設計書を読んで内容を確認する
3. **⚠️ `/gen-scripts` の並列グループを更新する**（唯一の手作業）
   - 「設計書を読んで並列グループを提案して」と依頼するとClaudeが案を出してくれる
4. 「コミットして」と依頼

---

### ③ 実装フェーズ前半（コアループ）
**ツール：** Claude Code CLI

```
/gen-scripts    # スクリプト一括生成
/gen-scene      # シーン構築
```

1. `/gen-scripts` を打つ → `Assets/Scripts/` にスクリプトが生成される
2. `/gen-scene` を打つ → シーンが構築される
3. **Unity エディタで再生ボタンを押して動作確認する**
4. 「コミットして」と依頼 → プレイテストへ

---

### ④ プレイテスト ⚠️ 最重要
**ツール：** Claude Desktop（UnityMCP）+ Claude Code CLI

```
/playtest       # 面白さを検証
```

1. **Claude Desktop を開いた状態で Unity を再生して実際に遊ぶ**
2. `/playtest` を打つ → Claudeと一緒に面白さを検証する
3. 判断する：
   - ✅ **面白い** → 実装フェーズ後半へ進む
   - 🔄 **面白くない・仕様を変えたい** → ② 設計フェーズへ戻る

> ここで戻ることを恐れない。戻るのがこのフローの設計意図。

---

### ⑤ 実装フェーズ後半（残り実装）
**ツール：** Claude Code CLI

```
/gen-scripts    # 残りのスクリプト生成
/gen-scene      # シーン更新
```

1. `/gen-scripts` → `/gen-scene` を再度実行
2. 最低限のUI・フィードバックも追加する（演出は最小限でOK）
3. 全体を確認して「コミットして」と依頼

---

### ⑥ テストフェーズ
**ツール：** Claude Desktop（UnityMCP）+ Claude Code CLI

**Unity で遊びながら気になった点をClaudeに伝えるだけ。**

| 状況 | 操作 |
|---|---|
| バグが出た | 「このエラーを直して」と伝える → `/debug` |
| ドキュメントと実装がずれてそう | `/check-diff` |
| 仕様書・設計書を更新したい | `/update-docs` |
| 面白さを確認したい | `/playtest` |
| バグ修正が終わった | 「コミットして」と依頼 |

---

## エージェントを直接呼ぶ場合

コマンドを使わず専門家に直接依頼することもできます：

```
@spec-writer    仕様書を一緒に作りたい・修正したい
@scene-builder  シーンを構築・変更したい
@unity-debugger このバグを調査してほしい
@doc-updater    ドキュメントと実装の差分を確認してほしい
```

---

## .claude/ 構成（参考）

```
.claude/
├── rules/       ← Claudeへの指示（自動ロード）
├── agents/      ← 専門家エージェント
├── commands/    ← スラッシュコマンド（/gen-scripts 等）
└── skills/      ← 将来用（現在は commands で対応）
```

---

## ドキュメント一覧

| ファイル | 内容 |
|---|---|
| `README.md` | **本ファイル**（人間向け操作手順） |
| `CLAUDE.md` | Claude向けインデックス |
| `Assets/Docs/仕様書.md` | ゲームの仕様 |
| `Assets/Docs/設計書.md` | クラス設計・依存関係 |
| `Assets/Docs/シーン構成書.md` | シーン構成 |
| `Assets/Docs/修正記録.md` | バグ修正の記録 |
| `Assets/Docs/PROJECT.md` | プロジェクト固有情報 |

---

## トラブル対処

| 問題 | 対処 |
|---|---|
| UnityMCP が繋がらない | Unity エディタが起動しているか確認 → Claude Desktop を再起動 |
| git MCP が動かない | `claude mcp list` で `git` が表示されるか確認 |
| Hooks エラーが出る | 「Continue without these settings」で起動 → 「settings.jsonを修正して」と依頼 |

セッション開始時のプロンプト例は `.claude/rules/07-troubleshooting.md` を参照。
