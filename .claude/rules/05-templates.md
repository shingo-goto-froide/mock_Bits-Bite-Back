# 各種テンプレート

## 仕様書テンプレート

```markdown
# [ゲームタイトル] 仕様書 v1.0

## ゲーム概要
- ジャンル：
- プレイ人数：
- 対戦形式：（ローカル / AI / オンライン 等）
- 想定プレイ時間：

---

## プレイヤー
- 人数：
- 初期ステータス：（HP・所持数上限 等）
- 勝利条件：
- 敗北条件：

---

## ゲーム要素

| 名前 | 種類 | 効果・説明 |
|---|---|---|
|  |  |  |

---

## フェーズ構成（1ターン）

フェーズ1：〇〇
フェーズ2：〇〇
フェーズ3：〇〇
次のターンへ

---

## 画面構成

| シーン名 | 役割 |
|---|---|
| TitleScene | タイトル・モード選択 |
| GameScene | ゲーム本体 |

---

## パラメータ一覧

| パラメータ名 | 値 | 説明 |
|---|---|---|
|  |  |  |

---

## 未決事項・注意事項
```

---

## 設計書テンプレート

```markdown
# [ゲームタイトル] 設計書 v1.0

## スクリプト一覧・依存関係

ManagerClass（MonoBehaviour・Singleton）
├── SubManagerA
├── SubManagerB
└── DataClass × N
    └── ChildClass

---

## Enum・静的クラス・ScriptableObject

### ScriptableObject
> アセットパス: Assets/ScriptableObjects/Xxx.asset

### Enum

### 静的クラス

---

## クラス詳細

### [ClassName]（pure C# class / MonoBehaviour・Singleton）

| 変数 / イベント | 型 | 説明 |
|---|---|---|
|  |  |  |

| メソッド | 説明 |
|---|---|
|  |  |

---

## フォルダ構成

Assets/
├── Scripts/
│   ├── Core/
│   ├── [カテゴリ]/
│   └── UI/
├── Docs/
├── Prefabs/
├── Scenes/
└── ScriptableObjects/
```

---

## シーン構成書テンプレート

```markdown
# [ゲームタイトル] シーン構成書 v1.0

## シーン一覧

| シーン名 | 役割 | Build Index | パス |
|---|---|---|---|
| TitleScene | タイトル・モード選択 | 0 | Assets/Scenes/TitleScene.unity |
| GameScene  | ゲーム本体          | 1 | Assets/Scenes/GameScene.unity  |

---

## [シーン名]

### Hierarchy

SceneName
├── EventSystem
├── [MainCanvas]
│   ├── Background
│   └── [PanelA]
├── [Manager]
└── Main Camera

### コンポーネント一覧

| GameObject | コンポーネント | 備考 |
|---|---|---|
| EventSystem | EventSystem, InputSystemUIInputModule | StandaloneInputModuleは使わない |
| [MainCanvas] | Canvas, CanvasScaler, GraphicRaycaster | |
| [Manager] | [ScriptName] | |

### Inspector参照設定

| コンポーネント | フィールド | 参照先 |
|---|---|---|
|  |  |  |
```

---

## PROJECT.md テンプレート

```markdown
# PROJECT.md - プロジェクト固有情報
> このファイルはこのプロジェクト専用の情報です。
> CLAUDE.md を読んだ後に読み込んでください。

---

## プロジェクト概要

- **ゲームタイトル**：
- **ジャンル**：
- **プレイ形式**：
- **想定プレイ時間**：

---

## ドキュメント一覧

| ファイル | 役割 | 最終更新 |
|---|---|---|
| CLAUDE.md | Claudeとの作業フロー・共通ルール | YYYY-MM-DD |
| PROJECT.md | プロジェクト固有情報・注意事項・変更履歴 | YYYY-MM-DD |
| 仕様書_v1.0.md | ゲームのルール・フェーズ・バランス数値 | YYYY-MM-DD |

---

## プロジェクト固有の注意事項

### Unity共通（変更不要）
- 「TLS Allocator ALLOC_TEMP_TLS has unfreed allocations」はUnity内部の警告。自作コードとは無関係。
- InputSystem を使う場合は StandaloneInputModule ではなく InputSystemUIInputModule を使うこと。
- SampleScene は使用しない。

### プロジェクト固有（設計フェーズ完了後に追記）

---

## 既知のバグ・TODO

### バグ（未解決）
- なし

### TODO（未実装）

---

## 変更履歴

| 日付 | 対象 | 内容 |
|---|---|---|
| YYYY-MM-DD | 仕様書・PROJECT.md | 初版作成 |
```

---

## 修正記録テンプレート

```markdown
## 修正記録

| 日付 | 種別 | 内容 | 対応 |
|---|---|---|---|
| YYYY-MM-DD | 軽微バグ / 仕様変更 |  | CLI修正 / 設計フェーズ戻り |

## 未解決の技術課題
```
