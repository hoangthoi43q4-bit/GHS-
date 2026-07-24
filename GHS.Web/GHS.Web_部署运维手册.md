# GHS.Web 部署与运维手册

> 记录从 GitHub 自动发布到 Windows 服务部署的完整流程，方便日后复用与升级。

---

## 一、环境信息

| 项目 | 值 |
|------|-----|
| 应用 | GHS.Web（ASP.NET Core Blazor Server，net9.0） |
| 代码分支 | `web-build`（默认分支是 master，打 tag 时务必选 web-build） |
| 仓库可见性 | 公开 |
| 服务器 | Windows Server |
| 运行时 | 已安装 ASP.NET Core Runtime 9.0（精简版发布，服务器需此运行时） |
| 数据库 | PostgreSQL（连接串在 appsettings.json，首次启动自动建表 EnsureCreated） |
| 固定部署目录 | `E:\Apps\GHS.Web` |
| 服务名 | `GHSWeb` |
| 监听地址 | http://0.0.0.0:5000 |

---

## 二、GitHub 自动发布（CI/CD）

### 1. workflow 文件

位置：`.github/workflows/release.yml`（web-build 分支）

```yaml
name: Release GHS.Web

on:
  push:
    tags:
      - 'v*'

permissions:
  contents: write

jobs:
  build-and-release:
    runs-on: windows-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET 9
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Publish
        run: dotnet publish GHS.Web/GHS.Web.csproj -c Release -r win-x64 --self-contained false -o publish/GHS.Web

      - name: Zip
        shell: pwsh
        run: Compress-Archive -Path publish/GHS.Web/* -DestinationPath GHS.Web-${{ github.ref_name }}-win-x64.zip

      - name: Release
        uses: softprops/action-gh-release@v2
        with:
          name: GHS.Web ${{ github.ref_name }}
          files: GHS.Web-${{ github.ref_name }}-win-x64.zip
```

> 说明：`--self-contained false` = 精简版（框架依赖），服务器需装 Runtime 9.0。

### 2. 触发发布

在 GitHub 网页操作：Releases → Draft a new release
- Choose a tag：输入版本号如 `v1.0.1` → Create new tag on publish
- ⚠️ Target 下拉框改成 `web-build`（默认 master，必须改）
- 填标题 → Publish release

发布后到 Actions 看构建（等变绿 ✅），成功后 Releases 页出现 `GHS.Web-<版本>-win-x64.zip`。

---

## 三、代码要点：支持 Windows 服务

普通 ASP.NET Core 程序直接注册成服务会报 **错误 1053**（服务未及时响应）。解决方式：

### 1. Program.cs 加一行

```csharp
builder.Host.UseWindowsService();
```

（手动双击 exe 运行时该行无害，会自动跳过）

### 2. GHS.Web.csproj 加包引用

在 `<ItemGroup>` 内加：

```xml
<PackageReference Include="Microsoft.Extensions.Hosting.WindowsServices" Version="9.0.0" />
```

> 只有代码里的 UseWindowsService() 没用，必须配合此 NuGet 包，否则仍报 1053。

---

## 四、Windows 服务部署（首次）

以**管理员身份**打开 PowerShell 执行。

### 1. 创建服务

```powershell
sc.exe create "GHSWeb" binPath= "E:\Apps\GHS.Web\GHS.Web.exe" start= auto
```

> ⚠️ `binPath=` 和 `start=` 的等号后面必须有一个空格，路径要加双引号。
> 如果提示 "The specified service already exists"，改用 config 修改路径（见下）。

### 2. 修改服务路径（服务已存在时用）

```powershell
sc.exe config "GHSWeb" binPath= "E:\Apps\GHS.Web\GHS.Web.exe" start= auto
```

### 3. 确认服务配置

```powershell
sc.exe qc "GHSWeb"
```

检查 BINARY_PATH_NAME 是否为 `E:\Apps\GHS.Web\GHS.Web.exe`。

### 4. 设置环境变量（系统级，服务才读得到）

```powershell
setx ASPNETCORE_ENVIRONMENT "Production" /M
setx ASPNETCORE_URLS "http://0.0.0.0:5000" /M
```

### 5. 设置崩溃自动重启

```powershell
sc.exe failure "GHSWeb" reset= 86400 actions= restart/5000/restart/5000/restart/5000
```

> 崩溃后 5 秒重启，前三次失败都重启，每天重置失败计数。

### 6. 启动并验证

```powershell
sc.exe start "GHSWeb"
sc.exe query "GHSWeb"
```

看到 `STATE : 4 RUNNING` 即成功。浏览器访问 `http://localhost:5000` 或 `http://<服务器IP>:5000` 验证。

---

## 五、日后升级（固定目录，服务路径不变）

因为部署在固定目录 `E:\Apps\GHS.Web`，升级只需 3 步：

```powershell
# 1. 停服务
sc.exe stop "GHSWeb"

# 2. 把新版 zip 解压后的文件覆盖到 E:\Apps\GHS.Web
#    （web-build 打新 tag 如 v1.0.2 → Actions 构建 → Releases 下载新 zip）

# 3. 启动服务
sc.exe start "GHSWeb"
```

> 关键：不管下载的 zip 目录名带什么版本号，都覆盖到同一个固定目录，服务路径永不再改。

---

## 六、常见故障排查

| 现象 | 原因 | 处理 |
|------|------|------|
| 报错 1053，服务未及时响应 | 缺 Windows 服务支持（代码/包） | 加 `UseWindowsService()` + `Microsoft.Extensions.Hosting.WindowsServices` 包，重新发布 |
| The specified service already exists | 服务已存在 | 用 `sc.exe config` 改配置，不用重建 |
| 启动后状态变回 STOPPED（非 1053） | 多半数据库连不上 | 查 事件查看器 → Windows 日志 → 应用程序，看红色错误；检查 appsettings.json 连接串与 PostgreSQL 可达性 |
| 卡在 START_PENDING | 启动慢或卡住 | 多等 10 秒再查；仍不行查事件查看器 |
| 页面局域网打不开 | 端口未放行 / 监听地址 | 确认 ASPNETCORE_URLS 是 0.0.0.0:5000，并放行防火墙 5000 端口 |

### 防火墙放行命令（如需局域网访问）

```powershell
New-NetFirewallRule -DisplayName "GHS.Web 5000" -Direction Inbound -Protocol TCP -LocalPort 5000 -Action Allow
```

---

## 七、常用服务管理命令速查

```powershell
sc.exe start "GHSWeb"      # 启动
sc.exe stop "GHSWeb"       # 停止
sc.exe query "GHSWeb"      # 查看运行状态
sc.exe qc "GHSWeb"         # 查看配置（路径/启动类型）
sc.exe config "GHSWeb" start= auto      # 设为开机自启
sc.exe config "GHSWeb" start= demand    # 设为手动启动
sc.exe delete "GHSWeb"     # 删除服务（谨慎）
```

也可用 `services.msc` 图形界面管理 GHSWeb 服务。

---

_文档记录日期：2026-07-24_
