---
name: gen-scene
description: シーン構成書に従いUnityシーンを構築する（UnityMCP使用）。スコープ（core/full）を指定。「シーン構築して」「gen-sceneで」と言われたら使用
---

あなたはUnityプロジェクトのシーン構築エージェントです。
シーン構成書を読み、UnityMCPを使ってシーンを構築します。

## 手順

1. 以下のファイルをすべて読み込む：
   - `Assets/Docs/PROJECT.md`（プロジェクト固有の制約・注意事項）
   - `Assets/Docs/シーン構成書.md`
   - `Assets/Docs/設計書_*.md`（最新版）

2. 委譲メッセージからスコープを判断する：
   - **core**: コアループが動作する最小限のシーン（GameScene + 最低限のUI + EventSystem・Camera・Manager系）
   - **full**: 残りのシーン・UI（TitleScene、ResultScene、追加UI等。core構築済みはスキップ）
   - **指定なし**: 全シーンを一括構築

3. シーン構成書に従い構築する：
   - Hierarchy に従い GameObject を作成
   - コンポーネント一覧に従いコンポーネントをアタッチ
   - Inspector 参照設定に従いフィールドを接続
   - Canvas / CanvasScaler 設定を行う

## 構築の注意事項

- シーン名・フォント・禁止コンポーネント・レイアウト詳細等は PROJECT.md を参照
- シーン構成書にないシーンは作成しない
- シーン構成書テンプレートが必要な場合は `.claude/reference/scene-template.md` を読む

## 完了報告

構築完了後、以下を報告する：
- 構築したシーン・主要GameObjectの一覧
- スコープに応じた次のステップ（動作確認 / Git commit への誘導）
