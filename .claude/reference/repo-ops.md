# テンプレートリポジトリ運用

> 新規プロジェクト開始時はこの手順に従う。
> CLAUDE.md・`.claude/` 一式をテンプレートとして使い回す。

## テンプレートに含まれるもの

```
{テンプレートリポジトリ}/
├── CLAUDE.md                        ← 共通フロー・インデックス
├── README.md                        ← 人間向け操作手順・環境構築
└── .claude/
    ├── settings.json                ← Hooks設定（そのまま使える）
    ├── rules/                       ← 行動ルール（起動時に自動ロード）
    │   ├── 01-rules.md
    │   ├── 02-workflow.md
    │   └── 03-docs.md
    ├── skills/                      ← 対話型ワークフロー + テンプレート同梱
    │   ├── new-spec/                ← 企画フェーズ + 仕様書・PROJECT.mdテンプレート
    │   ├── gen-design/              ← 設計フェーズ + 設計書テンプレート
    │   ├── playtest/                ← 面白さの検証
    │   ├── check-diff/              ← 差分確認
    │   └── debug/                   ← バグ調査・修正
    ├── agents/                      ← 投げっぱなし型サブエージェント
    │   ├── gen-scripts.md           ← スクリプト一括生成
    │   ├── gen-scene.md             ← シーン構築
    │   └── update-docs.md           ← ドキュメント更新
    └── reference/                   ← 自動ロードしないリファレンス
        ├── repo-ops.md              ← テンプレートリポジトリ運用（このファイル）
        ├── session-prompts.md       ← セッションプロンプト集
        ├── scene-template.md        ← シーン構成書テンプレート
        └── fix-log-template.md      ← 修正記録テンプレート
```

> `PROJECT.md` はプロジェクト固有情報のため、テンプレートには含めない。

---

## gen-scripts の並列グループ更新ルール

gen-scripts エージェントの並列グループは**設計書の依存関係ツリーを読んで更新する**。
並列化の判断基準は「互いのクラスを using / 参照していないか」。

**基本的な並列化パターン（Unityプロジェクト共通）：**

```
ステップ1（並列）：Enum・ScriptableObject・静的クラス
  → 誰にも依存しないため必ず最初・全グループ並列OK

ステップ2（並列）：純粋C#クラス（MonoBehaviour非依存）
  → Enumにのみ依存するため、ステップ1完了後に並列実行

ステップ3（並列）：Core MonoBehaviour（GameManager等）
  → 純粋C#クラスを参照するため、ステップ2完了後

ステップ4（並列）：UI・AI・その他MonoBehaviour
  → Coreを参照するため、ステップ3完了後
  → UIとAIは互いに独立していれば並列OK
```

> 設計書の依存関係が上記パターンと異なる場合は、
> 「設計書を読んで並列グループを提案して」と依頼すればよい。
