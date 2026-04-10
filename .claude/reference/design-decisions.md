# .claude/ 設計判断の記録

> 共通テンプレートの構造変更とその理由を記録する。
> 「なぜこうなっているのか」を未来の自分やチームメンバーが理解できるようにする。

---

## v1.0 (2026-04-08) — 初版

### 構成
```
rules/    : 01-rules, 02-workflow, 03-docs, 05-templates, 06-repo-ops, 07-troubleshooting
commands/ : new-spec, gen-design, gen-scripts, gen-scene, playtest, check-diff, debug, update-docs
agents/   : 空
skills/   : 空（「将来 commands が複雑化したら移行を検討」）
```

### 当時の判断
- 全ワークフローを commands/ に配置。シンプルで一貫した `/コマンド名` 起動
- テンプレート（仕様書・設計書・シーン構成書・PROJECT.md・修正記録）は rules/05-templates.md に集約
- リポジトリ運用ガイド・セッションプロンプト集も rules/ に配置

### 問題点（v2.0 で解消）
- rules/ は毎セッション自動ロードされるため、テンプレート約280行が常にコンテキストを消費していた
- リファレンス系（repo-ops, troubleshooting）も毎回ロードされていたが、使われるのは稀
- commands/ はテンプレートを同梱できず、rules/05-templates.md を暗黙的に参照していた
- 設計書テンプレートに Part 1（ゲームデザイン設計）が欠落していた（gen-design コマンドには指示があるのにテンプレートにない）
- 仕様書テンプレートに「ゲームシステム」「モック制限事項」セクションがなく、実際の生成物と乖離
- PROJECT.md テンプレートに「目的」「スコープ」「データ管理ガイド」「次のアクション」がなかった

---

## v2.0 (2026-04-10) — skills / agents / reference 体制へ移行

### 構成
```
rules/     : 01-rules, 02-workflow, 03-docs（3ファイルのみ）
skills/    : new-spec, gen-design, playtest, check-diff, debug（対話型5つ）
agents/    : gen-scripts, gen-scene, update-docs（投げっぱなし型3つ）
reference/ : repo-ops, session-prompts, scene-template, fix-log-template, design-decisions
```

### 変更内容と理由

#### 1. commands/ → skills/ に全面移行
**理由:** Claude Code の公式ドキュメントで commands は skills に統合された。skills は commands の上位互換であり、テンプレートの同梱・自動トリガー設定・引数ヒントなどの追加機能がある。同名が存在する場合 skills が優先されるため、commands を残す意味がない。

#### 2. 投げっぱなし系3つ（gen-scripts, gen-scene, update-docs）を agents/ に分離
**理由:** これら3つは「ドキュメントを読んでファイルを書く」だけで、ユーザーとの対話が不要。agents/ に置くと別コンテキストウィンドウで実行されるため、メインの会話を圧迫しない。成果物がファイルへの書き込みなので、結果が要約されても実害がない。

**agents にしなかったもの:**
- check-diff: 成果物が差分レポート（テキスト）。要約されるとレポートの価値が下がる
- debug: 「さっき話したバグ」の会話文脈が必要な場面が多い。agents は会話文脈を直接受け取れない（Claudeが仲介する形になり一手間増える）

#### 3. rules/ を3ファイルに絞った（~650行 → ~160行、約75%削減）
**理由:** rules/ は毎セッション自動ロードされる。テンプレート（05-templates.md, ~280行）は企画・設計フェーズでしか使わないのに、デバッグだけのセッションでも毎回ロードされていた。リファレンス系（06, 07）も同様。行動ルール（01, 02, 03）だけを残すことで、コンテキストウィンドウの空きが増え、長い会話やコード生成で圧縮が起きにくくなる。

#### 4. reference/ を新設
**理由:** 自動ロード不要だが、特定の場面で参照したい情報の置き場所がなかった。rules/ に置くとコンテキストを浪費し、skills/ に置くと「ワークフローではないのにスキルとして認識される」問題がある。reference/ は自動ロードされず、Claudeが必要時にReadで読む。

#### 5. テンプレートの改善
**変更点:**
- 仕様書テンプレート: 「ゲームシステム」「モック制限事項」セクションを追加。実際の生成物（Bits Bite Back の仕様書）で必要だったセクションがテンプレートに欠落していた
- 設計書テンプレート: Part 1（ゲームデザイン設計 — 難易度曲線・フィードバックループ・バランス数値・リスクリワード・MVP）を追加。gen-design の指示にはあったがテンプレートに反映されていなかった
- PROJECT.md テンプレート: 「目的」「スコープ」「データ管理ガイド」「次のアクション」を追加。実運用で自然に追加されたセクションをテンプレート化
- シーン構成書テンプレート: Hierarchy をコードブロック化。実際の出力形式と合わせた

#### 6. check-diff と debug の自動トリガー設定
**理由:** この2つは `disable-model-invocation` を設定していない。Claudeがコードと仕様の乖離やエラーに気づいたとき、ユーザーが `/` で呼ばなくても自動で提案・実行できる。他のスキル（new-spec, gen-design, playtest 等）はユーザーの意思決定が必要なため `disable-model-invocation: true` で手動起動のみ。

### skills / agents / rules / reference の使い分け原則

| ディレクトリ | ロードタイミング | 用途 | 判断基準 |
|---|---|---|---|
| rules/ | 毎セッション自動 | 行動ルール | 常に守るべき制約だけ |
| skills/ | /スキル名 で起動（説明文のみ常時） | 対話型ワークフロー | ユーザーとの対話が必要なもの |
| agents/ | 自然言語 or @mention で起動（説明文のみ常時） | 投げっぱなし型タスク | 対話不要、成果物がファイル書き込み |
| reference/ | 必要時にReadで読む | リファレンス資料 | 常時ロード不要な情報 |

### agents に昇格する判断基準（将来）
check-diff や debug を agents に移す条件：
- モデルを変えたい（check-diff を haiku で高速化等）
- 永続メモリが欲しい（セッションをまたいで学習を蓄積）
- ツール制限を厳密にしたい（check-diff は Read/Grep のみ等）
- 会話文脈の喪失が実運用で問題にならないと判断できた場合
