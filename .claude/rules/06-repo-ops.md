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
    ├── rules/                       ← Claudeへの指示（起動時に自動ロード）
    │   ├── 01-rules.md
    │   ├── 02-workflow.md
    │   ├── 03-docs.md
    │   ├── 05-templates.md
    │   ├── 06-repo-ops.md
    │   └── 07-troubleshooting.md
    ├── agents/                      ← 専門家エージェント（そのまま使える）
    │   ├── spec-writer.md
    │   ├── scene-builder.md
    │   ├── unity-debugger.md
    │   └── doc-updater.md
    ├── commands/
    │   ├── new-spec.md              ← 設計に依存しない（そのまま使える）
    │   ├── gen-design.md            ← 設計に依存しない（そのまま使える）
    │   ├── gen-scripts.md           ← ⚠️ 設計書完成後に並列グループを更新
    │   ├── gen-scene.md             ← 設計に依存しない（そのまま使える）
    │   ├── playtest.md              ← 設計に依存しない（そのまま使える）
    │   ├── check-diff.md            ← 設計に依存しない（そのまま使える）
    │   ├── update-docs.md           ← 設計に依存しない（そのまま使える）
    │   └── debug.md                 ← 設計に依存しない（そのまま使える）
    └── skills/                      ← 将来: gen-scripts 複雑化時に移行を検討
        └── （現在は commands で対応）
```

> `PROJECT.md` はプロジェクト固有情報のため、テンプレートには含めない。

---

## gen-scripts.md の更新ルール

`/gen-scripts` の並列グループは**設計書の依存関係ツリーを読んで更新する**。
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

---

## skills/ への移行を検討するタイミング

現在は `.claude/commands/` で十分だが、以下の条件を満たしたら `.claude/skills/` への移行を検討する：

| コマンド | 移行条件 |
|---|---|
| `/gen-scripts` | 依存関係テンプレートや生成規約をファイルとして同梱したくなったとき |
| その他 | 手順が長くなりサポートファイル（scripts/, templates/）が必要になったとき |
