# GHS / GHPHandShake — Web 化重构方案

> **源项目**：GHPHandShake (.NET Framework 4.8 WPF 桌面应用)
> **目标技术栈**：Blazor Server + PostgreSQL
> **目标形态**：局域网 Web 应用，配置与日志服务端存储，多用户同时使用
> **文档版本**：v1.1 | **日期**：2026-06-26

---

## 目录

1. [现状分析](#1-现状分析)
2. [重构目标](#2-重构目标)
3. [技术选型](#3-技术选型)
4. [架构设计](#4-架构设计)
5. [数据库设计](#5-数据库设计)
6. [后端 API 设计](#6-后端-api-设计)
7. [Blazor 前端设计](#7-blazor-前端设计)
8. [项目结构](#8-项目结构)
9. [迁移映射表](#9-迁移映射表)
10. [实施计划](#10-实施计划)
11. [风险与注意事项](#11-风险与注意事项)

---

## 1. 现状分析

### 1.1 现有系统核心功能

| 功能模块 | 描述 | 当前实现 |
|----------|------|----------|
| **TCP 设备通信** | 通过 TCP 向工业设备发送 `LOAD_MATERIAL` 指令 | `TcpClient` 异步通信，STX/ETX 帧格式 |
| **物料标签解析** | 解析 `P@V@3S` 格式的 SECS/GEM 风格报文 | `StringProcessor.Process()` / `FindMaterial()` |
| **物料路由匹配** | 根据物料号查找目标设备 IP/端口 | `MainWindow.FindRoutingForMaterial()` |
| **指令生成** | 生成 `LOAD_MATERIAL,...` 标准指令 | `StringProcessor.GenerateLoadCommand()` |
| **设备配置管理** | 多设备（IP/端口）增删改 | `SettingsWindow` + `MachineInfo` |
| **物料配置管理** | 项目→类型(Pos)→子类型(物料号)→设备 层级管理 | `MaterialSettingControl` + TreeView |
| **Excel 导入** | 从 Excel 批量导入物料配置 | ClosedXML 读取 `.xlsx` |
| **上传历史记录** | 每次上料操作的日志（时间/项目/物料/设备/ACK状态） | `MaterialUploadHistoryWindow` + JSON 文件 |
| **文件日志** | 每日文本日志 | `FileLogger` → `Logs/ghp_yyyyMMdd.log` |
| **配置持久化** | 本地 JSON 文件 | `config.json` + `materials.json` + `upload_history.json` |

### 1.2 当前架构痛点

1. **单机限制**：WPF 桌面应用，只能在一台电脑上运行
2. **数据孤岛**：配置和日志存储在本地 JSON 文件，无法共享
3. **无法多人协作**：多个工位无法同时查看/操作
4. **部署麻烦**：每台电脑需要单独安装 .NET Framework 4.8 运行时和配置
5. **历史数据查询弱**：JSON 文件存储，无 SQL 查询能力

---

## 2. 重构目标

### 2.1 核心目标

| 目标 | 描述 |
|------|------|
| 🌐 **Web 化** | 浏览器访问，无需安装客户端 |
| 🗄️ **服务端存储** | 配置、日志、历史数据全部存入 PostgreSQL |
| 🏢 **局域网多用户** | 支持多个工位同时使用，互不干扰 |
| — | ~~用户登录（暂不实现）~~ |
| 🏭 **线体配置** | 设备归属线体；操作时先选线体再选项目，自动筛选 |
| 📱 **响应式 UI** | 适配不同屏幕尺寸（工位机、平板、手机） |

### 2.2 保留不变的核心

- TCP 通信协议（STX/ETX 格式、LOAD_MATERIAL 指令格式）
- 物料标签解析逻辑（`StringProcessor`）
- 物料分类层级（项目 → 类型 → 物料号 → 设备）
- Excel 导入格式

---

## 3. 技术选型

| 层级 | 技术 | 说明 |
|------|------|------|
| **运行时** | .NET 9 | 最新 LTS，性能优异 |
| **Web 框架** | **Blazor Server** | 基于 SignalR 的实时 UI，C# 全栈，无需写 JS |
| **UI 组件库** | **MudBlazor** | Material Design 风格 Blazor 组件库，丰富且成熟 |
| **数据库** | **PostgreSQL 16** | 开源关系型数据库，JSONB 支持好 |
| **ORM** | **Entity Framework Core 9** | 官方 ORM，Code First 迁移 |
| **认证** | 无（暂不实现） | 局域网内直接访问，后续可按需添加 |
| **TCP 通信** | `System.Net.Sockets`（同现有） | 服务端后台服务统一管理 TCP 连接 |
| **Excel 导入** | **ClosedXML**（同现有） | .NET 生态最佳 Excel 库 |
| **日志** | **Serilog** + PostgreSQL Sink | 结构化日志，写入数据库 |
| **实时通信** | Blazor Server 内置 SignalR | 通信日志实时推送到浏览器 |

### 为什么选择 Blazor Server？

| 优势 | 说明 |
|------|------|
| ✅ **C# 全栈** | 前后端同语言，共享模型/服务代码 |
| ✅ **复用现有逻辑** | `StringProcessor`、`MaterialClassifier` 等几乎可直接迁移 |
| ✅ **实时 UI** | SignalR 天然支持，TCP 通信日志实时推送 |
| ✅ **局域网友好** | SignalR 长连接在局域网内延迟极低 |
| ✅ **无 API 层** | 组件直接调用服务，减少开发量 |
| ⚠️ **需保持连接** | 每个用户占用一个 SignalR 连接（局域网场景完全可接受） |

---

## 4. 架构设计

### 4.1 整体架构图

```
┌─────────────────────────────────────────────────────────────┐
│                        浏览器 (Browser)                      │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌───────────────┐  │
│  │ 主面板    │ │ 物料配置  │ │ 设备管理  │ │ 上传历史     │  │
│  │ (发送)   │ │ (CRUD)   │ │ (CRUD)   │ │ (查询+图表)   │  │
│  └──────────┘ └──────────┘ └──────────┘ └───────────────┘  │
└───────────────────────┬─────────────────────────────────────┘
                        │ SignalR (WebSocket)
┌───────────────────────┴─────────────────────────────────────┐
│                    ASP.NET Core 9 + Blazor Server            │
│  ┌──────────────────────────────────────────────────────┐   │
│  │                    Blazor Components                   │   │
│  │  MainLayout / SendPanel / MaterialConfig / DeviceMgr  │   │
│  │  HistoryDashboard                                       │   │
│  ├──────────────────────────────────────────────────────┤   │
│  │                    Application Services               │   │
│  │  MaterialService / DeviceService / TcpCommService     │   │
│  │  UploadHistoryService / LogService / LineBodyService   │   │
│  ├──────────────────────────────────────────────────────┤   │
│  │                    Domain Services (复用现有逻辑)      │   │
│  │  StringProcessor / MaterialClassifier / SelectMatPos  │   │
│  ├──────────────────────────────────────────────────────┤   │
│  │                    Data Access (EF Core)              │   │
│  │  GHSDbContext / Repositories                          │   │
│  └──────────────────────────────────────────────────────┘   │
│                            │                                 │
│                     ┌──────┴──────┐                          │
│                     │ PostgreSQL  │                          │
│                     └─────────────┘                          │
│                            │                                 │
│  ┌───────────────────────┴──────────────────────────────┐   │
│  │              TcpCommunicationService                  │   │
│  │  (后台常驻服务，管理到多台设备的 TCP 连接)             │   │
│  └───────────────────────┬──────────────────────────────┘   │
│                            │ TCP (STX/ETX 帧)                │
│  ┌───────────────────────┴──────────────────────────────┐   │
│  │         工业设备 (HGW101 / FAM101 / ...)              │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### 4.2 关键设计决策

#### TCP 连接管理 (核心改动)

**现状**：每次发送指令时新建 TCP 连接，发送完立即关闭。

**新方案**：使用**连接池**模式：
- 应用启动时，根据设备配置建立到各设备的 TCP 长连接
- 维护 `Dictionary<string, TcpClient>` 连接池
- 后台健康检查定时发送心跳/重连
- 发送指令时从池中获取连接（线程安全）

```csharp
// 连接池管理器
public class TcpConnectionPool : IHostedService
{
    private readonly ConcurrentDictionary<string, TcpClient> _clients = new();
    private readonly SemaphoreSlim _locks = new(1, 1);

    public async Task<string> SendCommandAsync(string equipmentId,
        string command, CancellationToken ct);

    // 健康检查（定时任务）
    private async Task HealthCheckLoop(CancellationToken ct);
}
```

#### 实时通信日志

利用 Blazor Server 的 SignalR 天然优势：
- 服务端调用 `IHubContext` 推送通信日志到前端
- 无需额外编写 WebSocket/轮询代码

---

## 5. 数据库设计

### 5.1 ER 图 (核心表)

```
┌──────────────────┐
│   LineBodies     │
│──────────────────│
│ Id (PK)          │
│ Name             │
│ Description      │
│ CreatedAt        │
└──────┬───────────┘
       │ 1:N (一个线体下有多个设备)
       ▼
┌──────────────────┐
│    Equipments    │          ← 设备即站位，直接归属线体
│──────────────────│
│ Id (PK)          │
│ EquipmentId     │
│ ServerIp        │
│ ServerPort      │
│ LineBodyId (FK) │────────── 设备归属线体
│ Description     │
│ IsActive        │
│ CreatedAt       │
└────────┬─────────┘
         │ 1:N
         ▼
┌──────────────────┐       ┌──────────────────┐
│ MaterialSubTypes │       │     Projects     │
│──────────────────│       │──────────────────│
│ Id (PK)          │       │ Id (PK)          │
│ MaterialTypeId   │──┐    │ Name             │
│ SubTypeName      │  │    │ CreatedAt        │
│ EquipmentId (FK) │  │    └──────┬───────────┘
│ CreatedAt        │  │           │ 1:N
└──────────────────┘  │           ▼
                      │    ┌──────────────────┐
                      │    │  MaterialTypes   │
                      ├───▶│──────────────────│
                      │    │ Id (PK)          │
                      │    │ ProjectId (FK)   │
                      │    │ TypeName         │
                      │    │ SortOrder        │
                      │    └──────────────────┘
                      │
筛选链路: LineBody → Equipment(LineBodyId) → MaterialSubType(EquipmentId)
                                              → MaterialType(Id)
                                              → Project(Id)

┌───────────────────────────────────────────┐
│           UploadHistory                   │
│───────────────────────────────────────────│
│ Id (PK)                                   │
│ Timestamp                                 │
│ LineBodyName                              │
│ ProjectName                               │
│ TypeName                                  │
│ SubTypeName                               │
│ EquipmentId                               │
│ ScanContent (TEXT)                        │
│ CommandSent (TEXT)                        │
│ Response (TEXT)                           │
│ IsAckSuccess (BOOLEAN)                    │
│ CreatedAt                                 │
└───────────────────────────────────────────┘

┌───────────────────────────────────────────┐
│           SystemLogs                      │
│───────────────────────────────────────────│
│ Id (PK)                                   │
│ Timestamp                                 │
│ Level                                     │
│ Message                                   │
│ Exception                                 │
│ Source                                    │
└───────────────────────────────────────────┘

```

### 5.2 PostgreSQL 建表 SQL

```sql
-- 线体表 (新增)
CREATE TABLE line_bodies (
    id          SERIAL PRIMARY KEY,
    name        VARCHAR(100) NOT NULL UNIQUE,
    description VARCHAR(500),
    created_at  TIMESTAMPTZ DEFAULT NOW()
);

-- 项目表
CREATE TABLE projects (
    id          SERIAL PRIMARY KEY,
    name        VARCHAR(100) NOT NULL UNIQUE,
    created_at  TIMESTAMPTZ DEFAULT NOW(),
    updated_at  TIMESTAMPTZ DEFAULT NOW()
);

-- 设备表 (新增 line_body_id，设备直接归属线体)
CREATE TABLE equipments (
    id           SERIAL PRIMARY KEY,
    equipment_id VARCHAR(100) NOT NULL UNIQUE,
    server_ip    VARCHAR(45)  NOT NULL,
    server_port  INT          NOT NULL CHECK (server_port BETWEEN 1 AND 65535),
    line_body_id INT          REFERENCES line_bodies(id) ON DELETE SET NULL,
    description  VARCHAR(500),
    is_active    BOOLEAN      DEFAULT TRUE,
    created_at   TIMESTAMPTZ  DEFAULT NOW(),
    updated_at   TIMESTAMPTZ  DEFAULT NOW()
);

-- 物料类型表
CREATE TABLE material_types (
    id         SERIAL PRIMARY KEY,
    project_id INT          NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
    type_name  VARCHAR(10)  NOT NULL,
    sort_order INT          DEFAULT 0,
    created_at TIMESTAMPTZ  DEFAULT NOW(),
    UNIQUE(project_id, type_name)
);

-- 物料子类型表
CREATE TABLE material_sub_types (
    id               SERIAL PRIMARY KEY,
    material_type_id INT          NOT NULL REFERENCES material_types(id) ON DELETE CASCADE,
    sub_type_name    VARCHAR(200) NOT NULL,
    equipment_id     INT          NOT NULL REFERENCES equipments(id),
    created_at       TIMESTAMPTZ  DEFAULT NOW(),
    UNIQUE(material_type_id, sub_type_name)
);

-- 上传历史表 (新增 line_body_name)
CREATE TABLE upload_history (
    id              BIGSERIAL PRIMARY KEY,
    timestamp       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    line_body_name  VARCHAR(100),
    project_name    VARCHAR(100),
    type_name       VARCHAR(10),
    sub_type_name   VARCHAR(200),
    equipment_id    VARCHAR(100),
    scan_content    TEXT,
    command_sent    TEXT,
    response        TEXT,
    is_ack_success  BOOLEAN      DEFAULT FALSE,
    created_at      TIMESTAMPTZ  DEFAULT NOW()
);

-- 索引
CREATE INDEX idx_equipments_line_body ON equipments(line_body_id);
CREATE INDEX idx_upload_history_line_body ON upload_history(line_body_name);
CREATE INDEX idx_upload_history_project ON upload_history(project_name);
CREATE INDEX idx_upload_history_timestamp ON upload_history(timestamp DESC);
CREATE INDEX idx_upload_history_subtype ON upload_history(sub_type_name);
CREATE INDEX idx_upload_history_success ON upload_history(is_ack_success);
```

---

## 6. 后端 API / 服务设计

### 6.1 服务层 (Blazor 组件直接注入调用)

由于 Blazor Server 模式，大部分操作无需 REST API，组件直接注入 Service。仅对**未来扩展**预留 REST API。

| 服务类 | 职责 | 对应旧代码 |
|--------|------|-----------|
| `TcpCommunicationService` | 管理设备 TCP 连接池，发送指令，接收响应 | `MainWindow.SendMessage()` |
| `MaterialService` | 物料 CRUD、Excel 导入导出 | `MaterialSettingControl` |
| `DeviceService` | 设备（MachineInfo）CRUD | `SettingsWindow` |
| `UploadHistoryService` | 上料历史记录查询、聚合统计 | `MaterialUploadHistoryService` |
| `ProjectService` | 项目管理 | `MaterialSettingControl` 中项目部分 |
| `LineBodyService` | 线体 CRUD，查询线体下的设备/项目 | 新增 |
| ~~`UserService`~~ | ~~用户管理、登录~~ | 暂不实现 |
| `LogService` | 系统日志查询 | `FileLogger` 替代 |

### 6.2 核心服务伪代码

#### TcpCommunicationService

```csharp
public class TcpCommunicationService : IHostedService, IDisposable
{
    private readonly ConcurrentDictionary<int, TcpClient> _pool = new();
    private readonly IHubContext<LogHub> _hubContext;
    private readonly IServiceScopeFactory _scopeFactory;

    // 发送指令到指定设备
    public async Task<SendResult> SendLoadMaterialAsync(
        string scanInput,      // 用户扫描的原始物料标签
        int equipmentId)       // 目标设备数据库 ID
    {
        // 1. 从数据库获取设备信息
        // 2. 调用 StringProcessor 解析物料标签
        // 3. 调用 MaterialClassifier 匹配物料位置
        // 4. 调用 GenerateLoadCommand 生成指令
        // 5. 从连接池获取/创建 TCP 连接
        // 6. 包装 STX/ETX 帧并发送
        // 7. 读取响应，判断 ACK
        // 8. 写入 upload_history 表
        // 9. 通过 SignalR 推送日志到前端
        return result;
    }

    // 后台健康检查
    private async Task HealthCheckAsync();
}
```

#### MaterialService

```csharp
public class MaterialService
{
    private readonly GHSDbContext _db;

    // 按项目分组获取完整物料树
    public async Task<List<ProjectNode>> GetMaterialTreeAsync();

    // 新增物料类型
    public async Task AddMaterialTypeAsync(int projectId, string typeName);

    // 新增物料子类型
    public async Task AddSubTypeAsync(int typeId, string subName, int equipmentId);

    // Excel 批量导入
    public async Task<ImportResult> ImportFromExcelAsync(Stream excelStream);

    // 删除物料
    public async Task DeleteSubTypeAsync(int subTypeId);
}
```

---

## 7. Blazor 前端设计

### 7.1 页面结构

```
/                           → 重定向到 /send

/send                       → 【主面板】物料发送 + 实时通信日志
/equipments                 → 【设备管理】设备列表 CRUD + 归属线体
/line-config                → 【线体配置】线体管理
/materials                  → 【物料配置】项目/类型/物料树管理 + Excel 导入
/history                    → 【上传历史】历史记录查询 + 线体/项目筛选
```

### 7.2 主面板 (/send) 布局设计

```
┌──────────────────────────────────────────────────────────────────┐
│  GHP HandShake Web                                                 │
├──────────────────────────────────────────────────────────────────┤
│  [导航栏]  发送 │ 设备管理 │ 线体配置 │ 物料配置 │ 历史          │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─ 操作区 ───────────────────────────────────────────────┐     │
│  │  ① 当前线体: [下拉选择 ▼]  (手动选择当前所在线体)     │     │
│  │  ② 项目号:   [下拉选择 ▼]  (根据线体自动筛选)         │     │
│  │  ③ 物料标签: [________________________] [发送 ↵]     │     │
│  │     设备:     [自动匹配显示]                           │     │
│  └────────────────────────────────────────────────────────┘     │
│                                                                  │
│  ┌─ 物料状态面板 ────────────────────────────────────────┐     │
│  │  Pos │ 物料号      │ 设备        │ 状态 ●  │         │
│  │  1   │ 1514380...  │ HGW_AG010   │ 上传成功 │         │
│  │  2   │ 1702568...  │ HGW_AG020   │ 未上传   │         │
│  │  ... │ ...         │ ...         │ ...      │         │
│  └────────────────────────────────────────────────────────┘     │
│                                                                  │
│  ┌─ 实时通信日志 ────────────────────────────────────────┐     │
│  │  [14:30:01] [线体: LINE-A] 接收到物料号: 151438020... │     │
│  │  [14:30:01] 匹配成功! 目标设备: HGW_AG010              │     │
│  │  [14:30:02] 发送 -> LOAD_MATERIAL,...                 │     │
│  │  [14:30:02] 响应: ACK                                 │     │
│  │  [14:30:02] [上料记录] 线体=LINE-A 设备=HGW_AG010 OK  │     │
│  └────────────────────────────────────────────────────────┘     │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### 7.3 用户操作流程

```
操作者到达工位
       │
       ▼
① 手动选择当前线体 (如 LINE-A)
       │
       ▼
② 项目号下拉框自动筛选 —— 只显示该线体下各设备所对应的项目号
       │
       ▼
③ 选择目标项目号
       │
       ▼
④ 扫描物料标签 → 系统自动匹配该项目的物料→设备
       │
       ▼
⑤ 点击发送 → TCP 指令发出 → 实时日志反馈 ACK 结果
```

> **设计要点**: 线体→项目号 的筛选逻辑：`LineBody → Equipments(WHERE line_body_id) → MaterialSubTypes(WHERE equipment_id) → MaterialTypes → Projects`，即线体下的设备 → 设备对应的物料子类型 → 物料类型 → 项目。

### 7.4 线体配置页 (/line-config) 布局设计

```
┌──────────────────────────────────────────────────────────┐
│  [导航栏]  发送 │ 设备管理 │ 线体配置 │ 物料配置 │ 历史  │
├──────────────────────────────────────────────────────────┤
│                                                          │
│  [+ 新增线体]                                            │
│                                                          │
│  ┌────────────────────────────────────────────────────┐  │
│  │  线体名称      │  描述         │  设备数  │  操作   │  │
│  ├────────────────────────────────────────────────────┤  │
│  │  LINE-A        │  一号产线     │  3       │ ✏ 🗑  │  │
│  │  LINE-B        │  二号产线     │  2       │ ✏ 🗑  │  │
│  │  LINE-C        │  三号产线     │  0       │ ✏ 🗑  │  │
│  └────────────────────────────────────────────────────┘  │
│                                                          │
│  ※ 设备归属线体在「设备管理」页面中设置                   │
└──────────────────────────────────────────────────────────┘
```

- 线体简单 CRUD，无子表
- 设备数列显示该线体下关联的设备数量（只读统计）
- 设备与线体的关联在 **设备管理页** 中通过下拉选择完成

### 7.5 MudBlazor 组件映射

| 旧 WPF 控件 | 新 MudBlazor 组件 |
|-------------|-------------------|
| `TextBox` | `<MudTextField>` |
| `Button` | `<MudButton>` |
| `ComboBox` | `<MudSelect>` |
| `DataGrid` | `<MudDataGrid>` |
| `TreeView` | `<MudTreeView>` |
| `ListBox` | `<MudList>` |
| `MessageBox` | `<MudDialog>` |
| `GroupBox` | `<MudPaper>` + `<MudText>` |

---

## 8. 项目结构

```
GHS.Web/                              ← 新 Blazor Server 项目
├── GHS.Web.csproj                    ← .NET 9 Blazor Server 项目
├── Program.cs                        ← 启动配置、DI 注册
├── appsettings.json                  ← 数据库连接串、TCP 超时配置
├── appsettings.Development.json
│
├── Data/                             ← 数据访问层
│   ├── GHSDbContext.cs               ← EF Core DbContext
│   └── Migrations/                   ← EF Core 迁移文件
│
├── Models/                           ← 数据模型 (Entity)
│   ├── LineBody.cs
│   ├── Project.cs
│   ├── Equipment.cs
│   ├── MaterialType.cs
│   ├── MaterialSubType.cs
│   ├── UploadHistoryEntry.cs
│   └── SystemLog.cs
│
├── Services/                         ← 业务服务层
│   ├── Core/                         ← 从旧项目复用的核心逻辑
│   │   ├── StringProcessor.cs        ← 直接迁移，几乎不改
│   │   ├── MaterialClassifier.cs     ← 直接迁移
│   │   └── SelectMatPos.cs           ← 直接迁移
│   ├── TcpCommunicationService.cs    ← TCP 连接池 + 通信
│   ├── MaterialService.cs            ← 物料 CRUD + Excel
│   ├── DeviceService.cs              ← 设备 CRUD
│   ├── LineBodyService.cs            ← 线体 CRUD + 查询
│   ├── UploadHistoryService.cs       ← 历史查询
│   ├── ProjectService.cs             ← 项目 CRUD
│   └── LogService.cs                 ← 日志管理
│
├── Hubs/                             ← SignalR Hub
│   └── LogHub.cs                     ← 实时通信日志推送
│
├── Components/                       ← Blazor 组件
│   ├── Layout/
│   │   ├── MainLayout.razor
│   │   └── NavMenu.razor
│   ├── Pages/
│   │   ├── Send.razor                ← 主面板（①选线体→②选项目→③扫物料发送）
│   │   ├── Equipments.razor          ← 设备管理（含归属线体下拉关联）
│   │   ├── LineConfig.razor          ← 线体配置（线体 CRUD）
│   │   ├── Materials.razor           ← 物料配置
│   │   └── History.razor             ← 上传历史
│   └── Shared/
│       ├── MaterialTreeView.razor    ← 物料树组件
│       ├── CommunicationLog.razor    ← 通信日志组件
│       └── StatusIndicator.razor     ← 状态指示灯
│
└── wwwroot/                          ← 静态资源
    ├── css/
    └── js/
```

---

## 9. 迁移映射表

### 9.1 核心逻辑迁移

| 旧代码路径 | 新代码路径 | 改动程度 |
|-----------|-----------|----------|
| `Services/StringProcessor.cs` | `Services/Core/StringProcessor.cs` | ✅ 几乎不变 |
| `Services/MaterialClassifier.cs` | `Services/Core/MaterialClassifier.cs` | ✅ 几乎不变 |
| `Services/SelectMatPos.cs` | `Services/Core/SelectMatPos.cs` | ✅ 几乎不变 |
| `Common/Constants.cs` | `Models/Constants.cs` | ✅ 不变 |
| `MainWindow.xaml.cs → SendMessage()` | `Components/Pages/Send.razor` | 🔄 重写为 Blazor 组件 |
| `MainWindow.xaml.cs → FindRoutingForMaterial()` | `Services/TcpCommunicationService.cs` | 🔄 SQL 查询替代内存查找 |
| `MaterialSettingControl.xaml.cs` | `Components/Pages/Materials.razor` + `MaterialService` | 🔄 CRUD 走数据库 |
| `SettingsWindow.xaml.cs` | `Components/Pages/Equipments.razor` + `DeviceService` | 🔄 CRUD 走数据库 |
| `MaterialUploadHistoryWindow.xaml.cs` | `Components/Pages/History.razor` + `UploadHistoryService` | 🔄 SQL 查询 |
| `FileLogger.cs` | `Services/LogService.cs` | 🔄 Serilog → PostgreSQL |
| `Config.cs` (JSON 读写) | 删除，全部走数据库 | ❌ 废弃 |
| `ViewModel/AboutViewModel.cs` | `Components/Pages/About.razor` | 🔄 静态页面 |
| 无 | `Models/LineBody.cs` | ✨ 新增 |
| 无 | `Services/LineBodyService.cs` | ✨ 新增 |
| 无 | `Components/Pages/LineConfig.razor` | ✨ 新增 |

### 9.2 数据迁移

| 旧存储 | 新存储 | 迁移方式 |
|--------|--------|----------|
| 无 | `line_bodies` 表 | ✨ 新系统手动配置 |
| `config.json` → `MachineInfo` 列表 | `equipments` 表（新增 `line_body_id` 字段） | 手动录入 + 关联线体 |
| `materials.json` → 物料树 | `projects` + `material_types` + `material_sub_types` 表 | Excel 重新导入 / 迁移脚本 |
| `upload_history.json` → 历史记录 | `upload_history` 表（新增 `line_body_name`） | 迁移脚本 |
| `Logs/*.log` → 文件日志 | `system_logs` 表 (Serilog) | 无需迁移，从新系统开始 |

---

## 10. 实施计划

### 第一阶段：基础搭建 (第 1-2 天)

```
□ 1.1 创建 .NET 9 Blazor Server 项目
□ 1.2 配置 PostgreSQL + EF Core
□ 1.3 创建数据库 Migration (所有表)
□ 1.4 配置 MudBlazor 组件库
□ 1.5 搭建 MainLayout + NavMenu 导航框架
```

### 第二阶段：核心服务迁移 (第 3-4 天)

```
□ 2.1 迁移 StringProcessor / MaterialClassifier / SelectMatPos (直接复用)
□ 2.2 实现 DeviceService (设备 CRUD + 线体下拉关联)
□ 2.3 实现 LineBodyService (线体 CRUD + 查询线体下设备/项目)
□ 2.4 实现 ProjectService (项目 CRUD)
□ 2.5 实现 MaterialService (物料 CRUD + Excel 导入)
□ 2.6 实现 TcpCommunicationService (TCP 连接池 + 通信)
□ 2.7 实现 UploadHistoryService (历史记录)
□ 2.8 实现 LogHub (SignalR 实时日志)
```

### 第三阶段：前端页面 (第 5-7 天)

```
□ 3.1 线体配置页 (LineConfig.razor) — 线体 CRUD
□ 3.2 设备管理页 (Equipments.razor) — DataGrid CRUD + 线体下拉关联
□ 3.3 主面板 (Send.razor) — ①选线体→②选项目(自动筛选)→③扫物料发送 + 实时日志
□ 3.4 物料配置页 (Materials.razor) — TreeView + Excel 导入
□ 3.5 上传历史页 (History.razor) — 线体/项目筛选 + 聚合查询 + 明细展开
```

### 第四阶段：测试与部署 (第 8-10 天)

```
□ 4.1 单元测试 (核心 Service 测试)
□ 4.2 集成测试 (TCP 通信模拟测试)
□ 4.3 局域网多用户并发测试
□ 4.4 配置 IIS / Docker 部署
□ 4.5 编写部署文档
□ 4.6 旧数据迁移验证
```

---

## 11. 风险与注意事项

### 11.1 关键风险

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| **TCP 连接池稳定性** | 通信中断 | 健康检查 + 自动重连 + 连接超时兜底 |
| **Blazor Server 断线** | 用户操作中断 | 自动重连机制 (Blazor 内置) |
| **数据库性能** | 历史数据量大 | 合理索引 + 分区表 + 定期归档 |
| **局域网并发** | SignalR 连接数过多 | 合理配置最大连接数，实测压力 |
| **旧数据迁移** | 格式不兼容 | 编写独立的迁移工具 |

### 11.2 关键注意点

1. **`SelectMatPos.cs` 第 29-31 行有 bug**：`MessageBox.Show` 后缺少大括号 `{}`，导致 `return 0` 总会执行。迁移时修复。
2. **`StringProcessor.Process()` 中空格/横线剔除逻辑被注释**，迁移时确认是否需要启用。
3. **Excel 导入格式**：新版本支持第 4 列（项目名），需更新导入模板 `BOM import.xlsx`。
4. **TCP ReadTimeout**：当前为 5000ms，建议做成可配置项 (`appsettings.json`)。
5. **设备密码/安全**：当前 TCP 协议无认证，后续可考虑加简单握手验证。

---

## 附录 A：NuGet 包清单

```xml
<!-- 新项目需要的包 -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.*" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.*" />
<PackageReference Include="MudBlazor" Version="7.*" />
<PackageReference Include="ClosedXML" Version="0.105.*" />
<PackageReference Include="Serilog.AspNetCore" Version="9.*" />
<PackageReference Include="Serilog.Sinks.PostgreSQL" Version="2.*" />
```

## 附录 B：appsettings.json 模板

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=GHSWeb;Username=ghs_user;Password=xxx"
  },
  "TcpSettings": {
    "ConnectTimeoutMs": 5000,
    "ReadTimeoutMs": 5000,
    "HealthCheckIntervalSeconds": 30,
    "MaxRetries": 3
  },
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      { "Name": "PostgreSQL", "Args": { "connectionString": "DefaultConnection" } }
    ]
  }
}
```

---

> **下一步**：请审核以上方案，确认无误后我将开始在 `web-build` 分支上进行重构实施。
