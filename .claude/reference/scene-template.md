# [ゲームタイトル] シーン構成書 v1.0

## シーン一覧

| シーン名 | 役割 | Build Index | パス |
|---|---|---|---|
| TitleScene | タイトル・モード選択 | 0 | Assets/Scenes/TitleScene.unity |
| GameScene  | ゲーム本体          | 1 | Assets/Scenes/GameScene.unity  |

---

## [シーン名]

### Hierarchy

```
SceneName
├── EventSystem
├── [MainCanvas]
│   ├── Background
│   └── [PanelA]
├── [Manager]
└── Main Camera
```

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
