---
name: GHSWEB项目记忆恢复
description: 当用户说"重新读取代码/review项目/恢复项目记忆"等关键词时，读取知识库中的 GHS.Web 项目记忆页恢复上下文
scope: local
---

当用户说出以下任一关键词或语义等价表达时：「重新读取代码」「重读项目」「review 项目」「回顾项目」「恢复项目记忆」「读取 GHSWEB/GHS.Web 项目记忆」「捡起 GHS 项目」——

立即读取知识库中的 GHS.Web 主记忆页，用它恢复对话上下文（项目架构、数据模型关联链、核心机制、当前需求进度）：

用 PowerShell 7 绝对路径读取（中文路径必须用 -LiteralPath + -Encoding UTF8）：
```
"C:\Program Files\PowerShell\7\pwsh.exe" -NoProfile -Command "Get-Content -LiteralPath 'C:\Users\uif45761\OneDrive - Aumovio SE\知识库\项目\GHS.Web-项目记忆.md' -Encoding UTF8 -Raw"
```

读取后：
1. 简要复述项目当前状态与「下料功能」进度（已完成项 / 待做项），锚定上下文。
2. 若记忆页与工作区实际代码可能不一致，优先以工作区源码为准，并提示需要 review 的文件。
3. 源码工作区在本机（GHS.Web），`E:\Apps\GHS.Web` 仅为服务器部署地址，勿混淆。
