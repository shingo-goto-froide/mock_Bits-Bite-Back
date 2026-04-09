# PROJECT.md - プロジェクト固有情報
> このファイルはこのプロジェクト専用の情報です。
> CLAUDE.md を読んだ後に読み込んでください。

---

## プロジェクト概要

- **ゲームタイトル**：Bits Bite Back
- **ジャンル**：ローグライト × 隊列オートバトル
- **プレイ形式**：1人プレイ（PvE）
- **想定プレイ時間**：1プレイ15〜30分（モック）

---

## プロジェクトの目的

- 他の企画者が立てた企画のモック制作を担当
- 企画者に実際に操作してもらい、ゲームの面白さを検証する
- 特に「隊列シナジーがハマる快感」が成立するかを確認する
- 想定工数：3人日

---

## モックのスコープ

### 含むもの
- 隊列オートバトル（射程・行動順・特殊効果・シナジー）
- 基本練成・分解
- 隊列編成UI
- 魔物13体（企画書の編成例3パターンが再現可能）
- 敵との連戦（バトル→報酬のシンプルな進行）

### 含まないもの（後から追加可能）
- 探索フェーズ（ダンジョン移動・イベント・お宝）
- 魂システム（パラメータ・特性カスタマイズ）
- 素材による見た目変化

---

## ドキュメント一覧

| ファイル | 役割 | 最終更新 |
|---|---|---|
| CLAUDE.md | Claudeとの作業フロー・共通ルール | 2026-04-08 |
| PROJECT.md | プロジェクト固有情報・注意事項・変更履歴 | 2026-04-08 |
| 仕様書_v1.0.md | ゲームのルール・バトル・練成システム | 2026-04-08 |

---

## プロジェクト固有の注意事項

### Unity共通（変更不要）
- 「TLS Allocator ALLOC_TEMP_TLS has unfreed allocations」はUnity内部の警告。自作コードとは無関係。
- InputSystem を使う場合は StandaloneInputModule ではなく InputSystemUIInputModule を使うこと。
- SampleScene は使用しない。
- UIテキストは TextMeshPro（TMP）を使用。レガシー `Text` は使わないこと。
- 日本語フォント: `Assets/Fonts/NotoSansJP-Regular SDF.asset`（Padding:10%, AutoSizing, SDFAA, Fast）
- TMP Settings のデフォルトフォントは NotoSansJP-Regular SDF に設定済み。

### プロジェクト固有（設計フェーズ完了後に追記）
- 探索フェーズを後から差し込める設計にすること
- 2枠ユニット（オーク）の隊列処理に注意
- 元企画はSteamリリース想定（UE or Unity）だが、モックはUnityで制作

---

## データ管理・調整ガイド

### Wave編成の変更
- Wave SOは `Assets/Resources/EnemyWaves/` に配置。起動時に自動ロードされ `waveNumber` 順にソートされる
- Wave数の変更: SOファイルを追加・削除するだけ（コード変更不要）
- 新規作成: `Assets > Create > BitsBiteBack > EnemyWave` → `Resources/EnemyWaves/` に配置
- 固定編成: `Use Random Formation` OFF → `Enemies` に手動設定
- ランダム編成: `Use Random Formation` ON → `Random Pool`（空なら全魔物）、敵数、レベルを設定

### 魔物データの変更
- 各魔物SOは `Assets/Resources/Monsters/{MonsterType}.asset`
- HP・ATK・速度・射程・枠数・攻撃タイプ・貫通・レシピをインスペクタから編集可能
- 新規魔物: `Assets > Create > BitsBiteBack > MonsterData`（Enumへの追加も必要）

### バランス調整
- `Assets/Resources/GameBalance.asset` で一元管理
- 毒ダメージ、毒付与確率、ピヨリ確率、魔法バリア減衰率、僧侶回復率、分解返却率、報酬素材数、初期素材などを調整可能

### デバッグ機能（PrepareScene右上）
- `素材MAX`: 全素材を99個に
- `全魔物入手`: 全13体を1体ずつ追加（連打で複数体）
- `全回復`: 所持魔物のHPを全回復
- `魔物クリア`: 所持魔物を全削除
- `Wave▲/▼`: 任意のWaveにジャンプ

### 魔物画像（スプライトシート）
- `Assets/Resources/MonsterSpriteSheet.png` に7列×2行で配置
- MonsterType enum順（左上から右へ）で自動分割
- 個別の切り出しオフセットは `MonsterSpriteLoader.GetCropOffset()` で調整
- 向き情報は `MonsterSpriteLoader.IsLeftFacing()` で管理（6番目以降が左向き）

---

## 既知のバグ・TODO

### バグ（未解決）
- なし

### TODO（未実装）
- 探索フェーズの追加（面白さ確認後）
- 魂システムの追加（面白さ確認後）
- 素材による見た目変化（演出強化時）

---

## 変更履歴

| 日付 | 対象 | 内容 |
|---|---|---|
| 2026-04-08 | 仕様書・PROJECT.md | 初版作成 |
| 2026-04-08 | PrepareScene | タブ式UI実装（3カラム→タブ切替）、カード構造統一 |
| 2026-04-08 | BattleScene | 味方右・敵左配置、前衛対峙レイアウト、報酬グリッド化 |
| 2026-04-08 | 全シーン | TMP化・NotoSansJP導入・UI設計プロセス追加 |
