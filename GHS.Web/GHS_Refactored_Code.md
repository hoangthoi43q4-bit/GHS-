# GHS.Web 技术文档

**项目名称**: GHS.Web（上料握手管理系统 / GHP Handshake Demo）
**架构**: 单项目 ASP.NET Core **Blazor Server**（旧式托管模型，`_Host` + `MapBlazorHub`）+ EF Core + PostgreSQL + SignalR + MudBlazor
**目标框架**: **net9.0**
**来源**: 由旧 WPF 桌面程序 `GHPHandShake` 重构为 Web 应用
**文档更新时间**: 2026-07-22（依据磁盘实际代码重写）

> ⚠️ 说明：本文档已按仓库中**实际代码**重写（含完整 Blazor UI 层）。前一版文档（Serilog / net8 / `MachineInfo` / `MaterialRoutingService` / `TcpSenderService` 等）与当前代码不符，已作废。

---

## 0. 系统概述

GHS.Web 是一个工业**物料上料管理系统**。操作流程：

1. 操作员在页面选择产线/项目并扫码（条码格式 `...@P{物料号}@V{版本}@3S{序列号}...`）。
2. 后端解析扫码字符串，匹配物料号 → 物料子类型 → 物料类型（Pos）→ 关联设备。
3. 生成 SECS/GEM 风格的 `LOAD_MATERIAL` 指令，用 `STX(0x02) ... ETX(0x03)` 包裹，通过 TCP 发送到目标设备。
4. 等待设备返回，响应中包含 `ACK` 视为成功，记录到上料历史。
5. 通信过程通过 SignalR（`LogHub`）实时推送到前端日志面板。

层级数据模型：**线体 Line → 项目 Project → 物料类型 MaterialType(Pos) → 物料子类型 MaterialSubType(物料号) → 关联设备 Equipment**。

---

## 1. 技术栈与依赖

| 分类 | 组件 | 版本 |
|------|------|------|
| 运行时 | .NET / ASP.NET Core | net9.0 |
| UI 框架 | Blazor Server（`AddServerSideBlazor`） | 内置 |
| UI 组件库 | MudBlazor | 8.6.0 |
| ORM | Microsoft.EntityFrameworkCore | 9.0.7 |
| ORM 工具 | Microsoft.EntityFrameworkCore.Design | 9.0.7 |
| 数据库驱动 | Npgsql.EntityFrameworkCore.PostgreSQL | 9.0.4 |
| 实时通信 | Microsoft.AspNetCore.SignalR.Client | 9.0.7 |
| Excel 导入 | ClosedXML | 0.105.0 |
| 日志 | 内置 `ILogger`（未使用 Serilog） | 内置 |

**实际 `GHS.Web.csproj`**：
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="9.0.7" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.7" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.7" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.4" />
    <PackageReference Include="MudBlazor" Version="8.6.0" />
    <PackageReference Include="ClosedXML" Version="0.105.0" />
  </ItemGroup>
</Project>
```

---

## 2. 项目结构（磁盘实际）

```
GHS.Web/
├── Program.cs                      ← 应用入口与 DI 注册
├── appsettings.json                ← 连接串 + TcpSettings + 日志级别
├── appsettings.Development.json    ← 开发环境（DetailedErrors）
├── Properties/launchSettings.json  ← http://localhost:5195
│
├── Data/
│   └── GHSDbContext.cs             ← EF Core DbContext（6 张表 + Fluent 配置）
│
├── Models/                         ← 数据库实体（POCO）
│   ├── Line.cs                     ← 线体
│   ├── Project.cs                  ← 项目（隶属线体）
│   ├── Equipment.cs                ← 设备
│   ├── MaterialTypeEntity.cs       ← 物料类型（Pos）
│   ├── MaterialSubTypeEntity.cs    ← 物料子类型（物料号）
│   └── UploadHistoryEntry.cs       ← 上料历史
│
├── Services/                       ← 业务服务层（Scoped，用 DbContextFactory）
│   ├── LineService.cs
│   ├── DeviceService.cs
│   ├── ProjectService.cs
│   ├── MaterialService.cs          ← 含 Excel 导入
│   ├── UploadHistoryService.cs
│   ├── StatisticsService.cs
│   ├── LogService.cs
│   ├── TcpConnectionPool.cs        ← Singleton + IHostedService（TCP 连接池）
│   └── Core/                       ← 无状态核心算法（Singleton）
│       ├── StringProcessor.cs      ← 扫码解析 + 指令生成
│       ├── MaterialClassifier.cs   ← 子类型匹配 → 类型
│       └── SelectMatPos.cs         ← 计算 mat_pos (1–19)
│
├── Hubs/
│   └── LogHub.cs                   ← SignalR Hub (/loghub)
│
├── App.razor                       ← Blazor 路由器（Router）
├── _Imports.razor                  ← 全局 using
├── Components/
│   ├── Layout/
│   │   ├── MainLayout.razor        ← MudBlazor 主布局（AppBar+Drawer）
│   │   └── NavMenu.razor           ← 侧边导航
│   ├── Pages/
│   │   ├── Send.razor              ← 发送物料主页 ("/" 与 "/send")
│   │   ├── Equipments.razor        ← 设备管理 ("/equipments")
│   │   ├── Materials.razor         ← 物料配置 ("/materials")
│   │   ├── History.razor           ← 上传历史 ("/history")
│   │   └── Statistics.razor        ← 统计图表 ("/statistics")
│   └── Shared/
│       └── ProjectMaterialStatusItem.cs  ← Send 页状态网格 ViewModel
├── Pages/
│   └── _Host.cshtml                ← Blazor Server 宿主页（加载 MudBlazor CSS/JS）
│
└── wwwroot/
    └── css/                        ← bootstrap + open-iconic + site.css
```

---

## 3. 应用入口 (Program.cs)

```csharp
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using GHS.Web.Data;
using GHS.Web.Services;
using GHS.Web.Services.Core;
using GHS.Web.Hubs;

var builder = WebApplication.CreateBuilder(args);

// UI
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();

// 数据库（使用 DbContextFactory，适配 Blazor Server 并发场景）
builder.Services.AddDbContextFactory<GHSDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 业务服务（Scoped）
builder.Services.AddSingleton<TcpConnectionPool>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<TcpConnectionPool>());
builder.Services.AddScoped<LineService>();
builder.Services.AddScoped<DeviceService>();
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<MaterialService>();
builder.Services.AddScoped<UploadHistoryService>();
builder.Services.AddScoped<StatisticsService>();
builder.Services.AddScoped<LogService>();

// 核心算法（Singleton，无状态）
builder.Services.AddSingleton<StringProcessor>();
builder.Services.AddSingleton<MaterialClassifier>();
builder.Services.AddSingleton<SelectMatPos>();

// SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// 启动时自动建库（注意：使用 EnsureCreated，非迁移）
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GHSDbContext>();
    db.Database.EnsureCreated();
}

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Error");

app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapHub<LogHub>("/loghub");
app.MapFallbackToPage("/_Host");

app.Run();
```

**要点**
- 使用 **`AddServerSideBlazor` + `_Host` fallback** 的旧式 Blazor Server 托管模型（非 .NET 8 的 `AddRazorComponents`）。
- 数据库用 **`AddDbContextFactory`**：所有服务通过 `IDbContextFactory<GHSDbContext>` 按需创建短生命周期 DbContext，规避 Blazor Server 中 Scoped DbContext 的并发问题。
- `TcpConnectionPool` 同时注册为 **Singleton** 和 **HostedService**（后台健康检查）。
- 建库用 **`EnsureCreated()`**，**没有 EF 迁移**——schema 变更需删库重建或手动处理。

---

## 4. 配置 (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=ghsweb;Username=ghs_app;Password=ghs_app_2024"
  },
  "TcpSettings": {
    "ConnectTimeoutMs": 5000,
    "ReadTimeoutMs": 5000,
    "HealthCheckIntervalSeconds": 30,
    "MaxRetries": 3
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

| 键 | 作用 |
|----|------|
| `ConnectionStrings:DefaultConnection` | PostgreSQL 连接串（库名 `ghsweb`） |
| `TcpSettings:ConnectTimeoutMs` / `ReadTimeoutMs` | TCP 连接/读取超时（默认 5s） |
| `TcpSettings:HealthCheckIntervalSeconds` | 连接池健康检查间隔（默认 30s） |
| `TcpSettings:MaxRetries` | 最大重试次数（配置项已定义） |

- 启动 URL：`http://localhost:5195`（见 `launchSettings.json`）。
- 开发环境启用 `DetailedErrors`。

---

## 5. 数据模型层 (Models/)

### 5.1 Line.cs — 线体
```csharp
public class Line
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

### 5.2 Project.cs — 项目（隶属线体）
```csharp
public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int LineId { get; set; }

    [NotMapped] public string LineName { get; set; } = string.Empty;  // 查询时填充

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

### 5.3 Equipment.cs — 设备
```csharp
public class Equipment
{
    public int Id { get; set; }
    public string EquipmentId { get; set; } = string.Empty;   // 设备唯一标识
    public string ServerIp { get; set; } = string.Empty;      // TCP 目标 IP
    public int ServerPort { get; set; } = 2001;               // TCP 目标端口
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

### 5.4 MaterialTypeEntity.cs — 物料类型 (Pos)
```csharp
public class MaterialTypeEntity
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string TypeName { get; set; } = string.Empty;   // Pos，通常为 "1".."19"
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [NotMapped] public string ProjectName { get; set; } = string.Empty;
}
```

### 5.5 MaterialSubTypeEntity.cs — 物料子类型 (物料号)
```csharp
public class MaterialSubTypeEntity
{
    public int Id { get; set; }
    public int MaterialTypeId { get; set; }
    public string SubTypeName { get; set; } = string.Empty;  // 物料号
    public int EquipmentId { get; set; }                     // 关联设备（Equipment.Id）
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [NotMapped] public string TypeName { get; set; } = string.Empty;
    [NotMapped] public string ProjectName { get; set; } = string.Empty;
    [NotMapped] public string AssociatedMachineName { get; set; } = string.Empty;
}
```

### 5.6 UploadHistoryEntry.cs — 上料历史
```csharp
public class UploadHistoryEntry
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? LineName { get; set; }
    public string? ProjectName { get; set; }
    public string? TypeName { get; set; }
    public string? SubTypeName { get; set; }
    public string? EquipmentId { get; set; }
    public string? ScanContent { get; set; }   // 扫码原文
    public string? CommandSent { get; set; }    // 发送的指令
    public string? Response { get; set; }       // 设备响应
    public bool IsAckSuccess { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

---

## 6. 数据访问层 (Data/GHSDbContext.cs)

6 个 `DbSet`：`Lines`、`Projects`、`Equipments`、`MaterialTypes`、`MaterialSubTypes`、`UploadHistory`。

**表映射与约束（Fluent API）**

| 实体 | 表名 | 关键约束/索引 |
|------|------|----------------|
| `Line` | `lines` | `Name` 必填(100)、唯一 |
| `Project` | `projects` | `Name` 必填(100)；FK→`Line`（级联删除）；`(LineId, Name)` 唯一 |
| `Equipment` | `equipments` | `EquipmentId` 必填(100)、唯一；`ServerIp` 必填(45) |
| `MaterialTypeEntity` | `material_types` | `TypeName` 必填(10)；FK→`Project`（级联）；`(ProjectId, TypeName)` 唯一 |
| `MaterialSubTypeEntity` | `material_sub_types` | `SubTypeName` 必填(200)；FK→`MaterialType`（级联）；FK→`Equipment`；`(MaterialTypeId, SubTypeName)` 唯一 |
| `UploadHistoryEntry` | `upload_history` | 索引：`LineName`、`ProjectName`、`Timestamp`、`SubTypeName` |

- 外键关系用 `HasOne<T>().WithMany().HasForeignKey(...)` 定义，未使用导航属性集合。
- `Line → Project → MaterialType → MaterialSubType` 为级联删除链。

---

## 7. 业务服务层 (Services/)

所有数据类服务均通过 `IDbContextFactory<GHSDbContext>` 创建 `await using` 短生命周期 DbContext。

### 7.1 LineService
线体 CRUD：`GetAllAsync` / `AddAsync(name)` / `DeleteAsync(id)`。

### 7.2 DeviceService
设备 CRUD：`GetAllAsync` / `GetByIdAsync` / `AddAsync(...)` / `UpdateAsync(...)` / `DeleteAsync`。

### 7.3 ProjectService
项目 CRUD：`GetAllAsync`（回填 `LineName`）/ `GetByLineAsync(lineId)` / `AddAsync(name, lineId)` / `DeleteAsync`。

### 7.4 MaterialService
物料类型/子类型管理 + **Excel 导入**（依赖 ClosedXML）。
- `GetMaterialTreeAsync()` → 返回内存匹配用的 `List<MaterialTypeInfo>`（含子类型、设备名）。
- `GetTypesByProjectAsync(projectId)` / `GetSubTypesWithInfoAsync(lineId, projectId)`。
- `AddTypeAsync` / `AddSubTypeAsync` / `DeleteTypeAsync` / `DeleteSubTypeAsync`。
- `ImportFromExcelAsync(stream)`：支持 **3 列或 5 列**格式
  `A:类型(Pos) B:物料号 C:设备 D:项目(可选) E:线体(可选)`，缺省项目/线体时用 `"Default"`；导入过程自动**查找或创建** Line / Project / Equipment / MaterialType，返回新增子类型数量。

### 7.5 UploadHistoryService
- `AddEntryAsync(entry)` 写历史。
- `GetGroupedHistoryAsync(projectFilter)` → 按 `(项目,类型,子类型)` 分组的 `UploadHistoryGroupItem`（含成功次数、最近成功扫码预览、最后时间）。
- `GetDetailHistoryAsync(project, type, subType)` → 明细 `UploadHistoryDetailItem`。
- `GetProjectNamesAsync()` → 历史中出现过的项目名去重。

### 7.6 StatisticsService
统计聚合：
- `GetSummaryAsync()` → `StatisticsSummary`（总量、成功数、成功率、今日量/今日成功）。
- `GetEquipmentStatsAsync()` → 按设备统计 `EquipmentStats`。
- `GetDailyStatsAsync(days=7)` → 近 N 天 `DailyStats`。

### 7.7 LogService
封装内置 `ILogger`，`Log(message)` 写 Information 级日志。

### 7.8 TcpConnectionPool（核心通信）
- **Singleton + IHostedService**；用 `ConcurrentDictionary<int, TcpClient>` 按 `equipmentId` 缓存连接。
- `SendCommandAsync(equipmentId, name, ip, port, command, ct)`：
  - 取/建连接 → 用 `STX(0x02)...ETX(0x03)` 包裹指令 → ASCII 发送 → 读响应。
  - 响应含 `ACK`（忽略大小写）视为成功；出错则移除该连接。
  - 全程通过 `IHubContext<LogHub>.SendAsync("ReceiveLog", ...)` 推送实时日志。
- `HealthCheckLoop`：按 `HealthCheckIntervalSeconds` 周期检查，剔除断开的连接。
- 返回 `SendResult { Response, IsAckSuccess }`。

---

## 8. 核心算法层 (Services/Core/)

无状态、注册为 Singleton。

### 8.1 StringProcessor — 扫码解析与指令生成
- `Process(input)`：按 `@` 分段，取 `P`（左补 0 到 18 位）、`V`（左补 0 到 10 位）、`3S`（序列号）字段，返回 `"{P}@{V}@{3S}"`。
- `FindMaterial(input)`：提取 `P` 段并去除 `-` 与空格，得到匹配用物料号。
- `GenerateLoadCommand(input, stationName, matPos, tokens=3)`：生成
  ```
  LOAD_MATERIAL,{stationName},10,{yyyyMMddHHmmssfff},
  <LoadMaterial mat_pos_1="{matPos}" mat_uid_1="{processed}" tokens="{tokens}" />
  ```

### 8.2 MaterialClassifier — 子类型匹配
- `GetTypeBySubTypeMatch(input, config)`：遍历 `List<MaterialTypeInfo>`，若 `input` 包含某子类型名，返回其 `TypeName`。
- 附带轻量 DTO：`MaterialTypeInfo` / `MaterialSubTypeInfo`（用于内存匹配，避免 EF 实体开销）。

### 8.3 SelectMatPos — 计算物料位置
- 依赖 `MaterialClassifier`；先清理 `-` 和空格。
- `GetMatPos(input, config)`：匹配到的类型名若在合法集合 `{"1".."19"}` 内则转为 int 返回，否则返回 0。
- 注释记录：修复了原 WPF 版 `MessageBox.Show` 后缺 `{}` 导致 `return 0` 恒执行的 bug。

---

## 8.5 Blazor UI 层（App / 宿主页 / 布局 / 页面）

**托管链路**：`_Host.cshtml`（`ServerPrerendered`，加载 `MudBlazor.min.css/js` 与自定义 `clickElement` / `clearActiveInput` JS）→ `App.razor`（`Router`，默认布局 `MainLayout`）→ 各页面。

**MainLayout.razor**：基于 MudBlazor，自定义主题（Primary `#FF4208`、品牌“Aumovio”），含 `MudAppBar` + `MudDrawer`（内嵌 `NavMenu`）+ `MudMainContent`；已配 `MudThemeProvider/Popover/Dialog/Snackbar` 四个 Provider。

**NavMenu.razor**：五个 `MudNavLink`——发送物料 `/send`、设备管理 `/equipments`、物料配置 `/materials`、上传历史 `/history`、统计图表 `/statistics`。

### 页面一览

| 页面 | 路由 | 职责 |
|------|------|------|
| `Send.razor` | `/`, `/send` | 核心主页：扫码发送 + 物料状态面板 + 实时日志 |
| `Equipments.razor` | `/equipments` | 设备 CRUD（站位/IP/端口/描述） |
| `Materials.razor` | `/materials` | 线体/项目/物料类型与子类型管理 + Excel 导入 |
| `History.razor` | `/history` | 上料历史——左侧分组摘要，右侧点选看明细 |
| `Statistics.razor` | `/statistics` | 总量/成功率/今日卡片 + 设备排行 + 近7天趋势 |

### 8.5.1 Send.razor（核心发送流程）
- 顶部：扫码输入框（回车触发）+ 线体下拉 + 项目下拉（值为 `"{Id}:{Name}"` 形式）+ “匹配设备”显示 + 发送按钮。
- 左下：`MudDataGrid` 物料状态面板（图标色 = 成功绿/失败红/等待黄/未传灰）；右下：实时通信日志（终端风格）。
- 通过 **SignalR `HubConnection`** 连 `/loghub` 接收 `ReceiveLog` 实时日志（上限 500 行），`IAsyncDisposable` 释放连接。
- **发送逻辑 `ProcessMessageAsync`**：FindMaterial 提物料号 → 在内存 `_materialConfig`（按选中项目 ProjectId 过滤）匹配子类型 → 找到关联设备 → `SelectMatPos.GetMatPos` 算 mat_pos → `StringProcessor.GenerateLoadCommand` 生成指令 → `TcpConnectionPool.SendCommandAsync` 发送 → 根据 ACK 更新状态并 `HistoryService.AddEntryAsync` 写历史。

### 8.5.2 Materials.razor
- 三栏：线体管理 / 项目管理（选所属线体）/ 添加物料类型+子类型（选项目/输 Pos/输物料号/选设备）。
- 底部物料列表：按线体+项目筛选，支持删除；`InputFile` + `clickElement` JS 实现**隐藏按钮触发 Excel 导入**（≤ 10MB），导入调 `MaterialService.ImportFromExcelAsync`，`ISnackbar` 提示结果。

### 8.5.3 其余页面
- **Equipments.razor**：编辑区 + `MudDataGrid` 列表，端口校验 1–65535，调 `DeviceService`。
- **History.razor**：项目筛选 + 左侧 `GetGroupedHistoryAsync` 摘要网格，选中行触发 `GetDetailHistoryAsync` 填右侧明细。
- **Statistics.razor**：4 张汇总卡 + 设备排行网格 + 近7天趋势网格，均来自 `StatisticsService`。

---

## 9. 已知风险与注意事项

1. **无 EF 迁移**：使用 `EnsureCreated()`，schema 变更后不会自动升级，生产升级需引入迁移。
2. **连接串明文密码**：`appsettings.json` 中含明文口令，建议改用 User Secrets / 环境变量。
3. **扫码发送依赖内存配置 `_materialConfig`**：Send 页在 `OnInitializedAsync` 一次性加载，后台改物料配置后需刷新页面才生效。
4. **`Serilog` 已废弃**：旧文档提到 Serilog，实际未引用，日志走内置 `ILogger` + SignalR 实时推送。

---

## 10. 与旧 WPF 版（GHPHandShake）的映射

| 旧 WPF | 现 Web |
|--------|--------|
| `config.json` AllMachines | `equipments` 表 / `DeviceService` |
| `MaterialConfig` / `MaterialType` | `material_types` + `material_sub_types` / `MaterialService` |
| `StringProcessor`（Views） | `Services/Core/StringProcessor` |
| `SelectMatPos` / `MaterialClassifier`（Services） | `Services/Core/*` |
| `MainWindow` `SendMessage()` | `TcpConnectionPool.SendCommandAsync` |
| `MaterialUploadHistory*` | `upload_history` / `UploadHistoryService` |
| WPF 窗口（Main/Settings/Material/History） | Blazor 页面（Send/Equipments/Materials/History/Statistics） |
