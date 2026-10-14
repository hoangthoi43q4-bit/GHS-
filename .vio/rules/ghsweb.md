---
name: GHSWEB项目记忆恢复
description: 恢复/保存 GHS.Web 项目进度的双文件机制：恢复时读 PROJECT_CONTEXT.md + 项目记忆库；保存时简要进度写入 PROJECT_CONTEXT.md（含 git log），完整内容写入记忆库。
scope: local
---

当用户说出以下任一关键词或语义等价表达时：「重新读取代码」「重读项目」「review 项目」「回顾项目」「恢复项目」「恢复项目记忆」「读取 GHSWEB/GHS.Web 项目记忆」「捡起 GHS 项目」「继续项目」——触发【恢复流程】。

当用户说出「保存项目」「保存进度」「同步项目」「更新项目记忆」或在完成阶段性开发后要求记录时——触发【保存流程】。

---

## 两个关键文件
| 文件 | 角色 | 内容粒度 |
|------|------|----------|
| **工作区根目录 `PROJECT_CONTEXT.md`** | 快速恢复入口 / 进度看板 | **简要**：目标、架构速览、当前进度表、下一步、**最近 git log** |
| **`C:\Users\uif45761\OneDrive - Aumovio SE\知识库\项目\GHS.Web-项目记忆.md`** | 完整记忆库 | **完整**：全部架构细节、数据模型关联链、核心机制、历史决策、坑 |

源码工作区在本机（GHS.Web / 当前工作区）；`E:\Apps\GHS.Web` 仅为服务器部署地址，勿混淆。

---

## 【恢复流程】
1. 读取工作区 `PROJECT_CONTEXT.md`（可直接 read_file）。
2. 读取完整记忆库（中文路径用 PowerShell 7 绝对路径 + -LiteralPath + -Encoding UTF8）：
   ```
   "C:\Program Files\PowerShell\7\pwsh.exe" -NoProfile -Command "Get-Content -LiteralPath 'C:\Users\uif45761\OneDrive - Aumovio SE\知识库\项目\GHS.Web-项目记忆.md' -Encoding UTF8 -Raw"
   ```
3. 简要复述项目当前状态与进度（已完成 / 进行中 / 待做），锚定上下文。
4. 若记忆与工作区实际代码可能不一致，优先以工作区源码为准，并提示需 review 的文件。

## 【保存流程】
每次保存项目进度时，**双写**：

1. **简要内容 → 工作区 `PROJECT_CONTEXT.md`**（帮助同步项目进度）：
   - 更新「当前进度表」「下一步」等快照区块。
   - 保持精简，只放看板级信息，不堆砌细节。
2. **刷新 git log → `PROJECT_CONTEXT.md`**：取最近 15 条写入其 Git 历史区块：
   ```
   git log -15 --pretty=format:"%h %ad %s" --date=short
   ```
3. **完整内容 → 记忆库 `GHS.Web-项目记忆.md`**：
   - 写入全部新增的架构/机制/决策/坑等细节，保留原有信息（追加/更新而非覆盖）。
   - 中文路径按「知识库自动保存」规则的可靠写法（英文临时文件 + 终端 move，或 pwsh 绝对路径 Set-Content -Encoding UTF8）。
4. 保存后一句话告知用户：两个文件各更新了什么、git log 已刷新。

