---
name: UI実装ルールを作成
description: UI調整で同じ指摘が繰り返されるため .claude/rules/04-ui.md を作成した。VLG内ボタン問題・サイズ目安・フォント制限等
type: feedback
---

UI実装時は `.claude/rules/04-ui.md` を参照すること。
**Why:** ボタンサイズが小さい、VLG内でサイズが効かない、グリッドが左寄り、フォントにない記号を使う等の指摘が毎回発生していた。
**How to apply:** コードでUIを動的生成する際、特にボタン・ダイアログ・グリッドを作るときに04-ui.mdのルールに従う。
