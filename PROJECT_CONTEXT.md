# 📘 GHS.Web 项目上下文（新对话快速恢复用）

> 💡 **一句话摘要**：GHS.Web = Blazor Server(.NET 9)+PostgreSQL 的产线上料/下料对接系统。按产线·项目扫描匹配设备，经 TCP 发 `LOAD_MATERIAL` / `UNLOAD_MATERIAL`（含 STX/ETX、收 ACK），并写历史。下料功能与远程 XML 读取均已打通。

> 📌 **新对话开场白**：「先读 `PROJECT_CONTEXT.md`，再看 `git log -10`，告诉我当前进度和下一步，确认后继续。」

---

## 🧭 1. 基本信息
| 项 | 值 |
|----|----|
| 项目名 | GHS.Web（GHP HandShake Web，产线与设备上/下料对接） |
| 技术栈 | **Blazor Server** + **.NET 9** + **MudBlazor** + **PostgreSQL(Npgsql)** + **SignalR** |
| 源码工作区（本机） | `C:\Users\uif45761\Desktop\IT Programe\GHPHandshakeDemo\GHS.Web` |
| 解决方案 | `GHS.Web.sln` / `GHS.Web.csproj`（net9.0） |
| 仓库 / 分支 | `github.com/hoangthoi43q4-bit/GHS-`，分支 **web-build** |
| DB 连接串 | `Host=localhost;Database=ghsweb;Username=ghs_app;Password=ghs_app_2024`（appsettings.json） |
| 建表方式 | `Program.cs` 启动时 `db.Database.EnsureCreated()`，**无 EF 迁移**；已存在表的新字段手动 `ALTER` |

> ⚠️ **源码位置澄清**：`E:\Apps\GHS.Web` 只是**服务器部署地址**，不是源码。任何改动须在本机源码目录自己编译 + 重定向验证。

---

## 🗄️ 2. 数据模型关联链
```
选 Line(Name) → Project(LineId==line.Id)
   → MaterialTypeEntity(ProjectId==project.Id)
       → MaterialSubTypeEntity(MaterialTypeId==type.Id)
           → EquipmentId(int)==Equipment.Id  → 站位设备(ServerIp:ServerPort + BufferXmlPath)
```
| 实体 / 表 | 关键字段 |
|------|------|
| `Line` / `lines` | Id, Name(唯一) |
| `Project` / `projects` | Id, Name, **LineId**(FK)，(LineId,Name) 唯一 |
| `Equipment` / `equipments` | Id, EquipmentId(站位名,唯一), ServerIp, ServerPort(默认2001), **BufferXmlPath**(可空), IsActive |
| `MaterialTypeEntity` / `material_types` | Id, **ProjectId**(FK), TypeName(=Pos), SortOrder |
| `MaterialSubTypeEntity` / `material_sub_types` | Id, **MaterialTypeId**(FK), SubTypeName(料号), **EquipmentId**(int) |
| `UploadHistoryEntry` / `upload_history` | 时间/线体/项目/Pos/料号/EquipmentId/扫码内容/命令/响应/IsAckSuccess |

> ⚠️ 表名全小写（`ToTable("equipments")`）；新增字段用 `ALTER TABLE equipments ADD COLUMN IF NOT EXISTS "BufferXmlPath" text;`（引号大小写敏感）。

---

## 📂 3. 关键文件
| 层 | 文件 | 说明 |
|----|------|------|
| 入口 | `Program.cs` | DI、`EnsureCreated()`、`UseWindowsService()`、MapBlazorHub、`/loghub` |
| 主页 | `Components/Pages/Send.razor`（`@page "/"`、`/send`） | **发送主页 + 查看当前料 + 下料按钮**（非独立页） |
| 页面 | `Equipments.razor` / `Materials.razor` / `History.razor` / `Statistics.razor` | 设备(含BufferXmlPath)/物料/历史/统计 |
| 服务 | `Services/TcpConnectionPool.cs` | **TCP 发送核心**（Singleton + HostedService，连接池） |
| 服务 | `Services/DeviceService.cs` 等 | 设备/物料/历史/日志 CRUD |
| Core | `Services/Core/StringProcessor.cs` | 扫码解析 + `GenerateLoadCommand` / `GenerateUnloadCommand` |
| Core | `Services/Core/BufferMaterialService.cs` | 读设备当前料 XML（远程 SMB） |
| Hub | `Hubs/LogHub.cs` | 实时日志推送 |

---

## ⚙️ 4. 核心机制
- **TCP 发送**：`TcpPool.SendCommandAsync(id,name,ip,port,cmd)` → 自动包 `STX/ETX`（`\x02{cmd}\x03`），判 `ACK`。
- **上料指令**：`LOAD_MATERIAL,{站位},10,{yyyyMMddHHmmssfff},<LoadMaterial mat_pos_1=".." mat_uid_1=".." tokens="3" />`
- **下料指令**：`UNLOAD_MATERIAL,{EquipmentId},10,{yyyyMMddHHmmssfff},<UnLoadMaterial mat_pos_1="{Position}" mat_uid_1="{XML原始Id}" tokens="3" />`（`10` 固定、`tokens=3` 固定、uid 取 XML 原始 Id）。
- **当前料读取**：`BufferMaterialService` 读设备 `BufferXmlPath` 指向的远程 `LoadMaterialBufferPreserve.xml`。

---

## 📊 5. 当前进度
| # | 项 | 状态 |
|---|-----|------|
| ✅ | 上料功能 / 错误 ACK / upload_history 全量记录 / NUL 字节清洗 | 完成 |
| ✅ | 下料功能（GenerateUnloadCommand + 单条/全部下料按钮） | 代码完成，待现场 ACK 验证 |
| ✅ | 远程 XML 读取（SMB：`.\admin` 账号跑服务 + cmdkey 存凭据） | 已打通 |
| ✅ | GHP 侧对接（SingleOrDefault→FirstOrDefault，同一 RawId 多装配位） | 已解决 |
| ✅ | 刷新后电路崩溃（ProtectedSessionStorage → localStorage） | 已解决 |
| 🟡 | UNLOAD_MATERIAL 现场连测验证设备 ACK | 待验证（SMB+GHP 条件已具备） |
| 🟡 | LOAD_MATERIAL 现场回归（同 RawId 多装配位 + ACK） | 待 GHP 环境回归 |
| 🟡 | `GHS.Web.styles.css 404`（scoped CSS bundle 缺失） | 次要，不影响功能 |
| 📘 | Blazor 托管模型 `_Host.cshtml → App.razor` 迁移 | 仅学习研究，暂不迁移；见 知识库\学习\Blazor托管模型-_Host迁移到App.razor.md |

---

## 🕙 6. Git 历史（最近 15 条，分支 web-build）
```
de8710b 2026-10-10 feat(sound): 增强失败提示音并补全失败场景覆盖
51035c5 2026-09-29 fix(Send): 改用 localStorage 修复刷新后电路崩溃 (CryptographicException)
b87ed48 2026-09-10 fix(Send): 修复刷新后线体/项目下拉回退到第一条(CAP101)
d939b4a 2026-09-03 增加站位 LoadMaterialBufferPreserve.xml 查询和发送 unload material 消息
1ac2b91 2026-09-03 POS匹配逻辑修改：根据项目匹配，而不是遍历所有项目命中第一个 POS
0803585 2026-08-28 bug fix: error ack
12dbb8a 2026-08-27 Merge branch 'web-build'
f509cc2 2026-08-27 modify upload_history：记录全部上传历史(no material / no info / no equipment)
f9dc95a 2026-08-27 add git ignore for memory md
3479c45 2026-08-27 modify upload_history（同上）
cfe3811 2026-08-24 fix: 清除 NUL 字节并保护 DB 保存，避免 circuit 崩溃 (PostgreSQL 22021)
40ee41b 2026-08-21 UI: change project size，add success/error sounds
40ddf06 2026-08-21 style: 物料状态/日志面板高度随视口自适应
fbba655 2026-07-24 增加运维手册
e802b17 2026-07-24 build: 添加 Windows Service 支持
```
> 🔄 刷新本节：`git log -15 --pretty=format:"%h %ad %s" --date=short`

---

## 🚀 7. 部署运维
- Windows 服务 **GHSWeb**，目录 **`E:\Apps\GHS.Web`**，端口 **80**。
- 发布：`dotnet publish GHS.Web\GHS.Web.csproj -c Release -o <目录>`。
- 更新：`sc.exe stop GHSWeb` → 覆盖文件 → `sc.exe start GHSWeb`。
- SMB 读远程 XML：服务以 `.\admin` 运行 + `cmdkey` 存目标共享凭据。

---

## 🔑 8. 速记卡
- 读当前料 = `Send.razor`「查看当前料」按钮，非独立页。
- 关联链：Line→Project(LineId)→material_types(ProjectId)→material_sub_types(MaterialTypeId)→Equipment(id)。
- 下料头 `UNLOAD_MATERIAL`、`10` 固定、`tokens=3` 固定、uid=XML 原始 Id。
- 表名全小写、无 EF 迁移、新字段手动 ALTER。
- 任何改动自己编译 + 重定向验证；`E:\Apps\GHS.Web` 是部署地址非源码。
- 完整主记忆：`C:\Users\uif45761\OneDrive - Aumovio SE\知识库\项目\GHS.Web-项目记忆.md`
