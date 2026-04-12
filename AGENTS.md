# MoreEnchantStandalone 会话

## 会话说明
本项目主要添加更多额外的附魔于原版，并在游戏中有概率遇见。

## 会话规则
- 每次会话修改完成后，助手需执行一次 git 提交并更新`CHANGELOG.md`
- 每次完成**影响程序行为或资源**的改动后，助手须执行下方「发布强制命令」第1步，重新生成并打包文件，便于本地实测；纯文档或仅 `AGENTS.md` 等规则说明、且用户未要求打包时可跳过。
- 当用户说“发布”时，遵从上传发布强制命令中第3步的相关文件。
- 发布目录为：此项目的release文件夹

### 发布强制命令（必须按此执行，禁止自行改参数）

1. 生成dll和导出pck。

2. 打包为zip。

3. GitHub Release 上传：
- MoreEnchantStandalone-版本.zip
- [MoreEnchantStandalone.dll](release/MoreEnchantStandalone.dll)
- [MoreEnchantStandalone.json](release/MoreEnchantStandalone.json)
- [MoreEnchantStandalone.pck](release/MoreEnchantStandalone.pck)