# ASP.NET Core 结构化日志：Serilog 集成实践

## 一、前言

日志是生产环境最重要的"监控手段"之一——没有日志，你就像在一间黑屋里修机器。

但现实往往是这样的：

```
2026-07-03 14:30:01 || 信息: 用户访问了首页
2026-07-03 14:30:02 || 信息: 数据库连接成功
2026-07-03 14:30:05 || 错误: 对象引用未设置到对象的实例
```

- 错误信息完全没有上下文，看不出来是哪个请求、哪个用户触发的
- 当日志量大时，文本文件检索困难
- 不同微服务的日志散落在不同的地方

**Serilog** 是 .NET 生态最流行的结构化日志库。它不只是"写日志"，而是把日志变成**结构化数据**，让你能搜索、过滤、分析。

## 二、什么是结构化日志？

传统日志是**文本**，结构化日志是**事件**。

```javascript
// 传统文本日志（人是阅读者）
"用户 1001 在 2026-07-03 14:30:00 下单成功"

// 结构化日志（机器也是阅读者）
{
  "@timestamp": "2026-07-03T14:30:00Z",
  "level": "Information",
  "userId": 1001,
  "action": "下单",
  "status": "成功",
  "elapsed_ms": 235
}
```

结构化日志的好处：

| 能力 | 文本日志 | 结构化日志 |
|------|----------|------------|
| 搜索 | grep "关键词" | 按字段精确查询 |
| 过滤 | 正则表达式 | `level=Error AND elapsed_ms>1000` |
| 聚合 | 几乎不可能 | SQL 般按 userId 统计 |
| 上下文 | 需要手动拼字符串 | 自动附加请求 ID、用户等 |

## 三、为什么选 Serilog

对于 ASP.NET Core 项目，Serilog 是事实标准：

- **与 `ILogger<T>` 无缝集成**：Serilog 接管 ASP.NET Core 内置的日志管道，所有 `_logger.LogInformation(...)` 自动变为结构化日志
- **丰富的 Sinks**：文件、控制台、Elasticsearch、Seq、数据库……几十种输出目标
- **丰富的 Enrichers**：自动附加线程 ID、请求 ID、机器名等上下文
- **配置灵活**：支持 appsettings.json、代码、环境变量多种配置源
- **异步写入**：避免日志 I/O 拖慢业务逻辑

## 四、项目中的 Serilog 集成架构

本项目采用**多微服务**架构，包含 4 个独立的 Web API 项目：

```
WPEnglish/
├── CommonInitializer/          ← 共享库：Serilog 配置集中在这里
│   └── SerilogConfiguration.cs
├── FileService/                ← 文件服务
├── IdentitServer.WebApi/       ← 身份认证服务
├── Listening.Admin.WebApi/     ← 管理后台 API
└── WPEnglish/                  ← 主 API（Listening.Main.WebApi）
```

设计原则：**配置集中，使用简化**。所有 Serilog 配置写在一个共享的扩展方法中，每个微服务只需一行代码调用。

### 4.1 共享配置：SerilogConfiguration.cs

```csharp
public static class SerilogConfiguration
{
    public static void AddSerilogConfiguration(this IHostBuilder host,
        IConfiguration configuration)
    {
        host.UseSerilog((context, config) =>
        {
            config.MinimumLevel.Information()
                  .Enrich.FromLogContext()
                  .WriteTo.Async(a => a.File(
                      configuration.GetSection("LoggerToFile").Value,
                      rollingInterval: RollingInterval.Day,
                      retainedFileCountLimit: 7,
                      fileSizeLimitBytes: 500 * 1024 * 1024,
                      rollOnFileSizeLimit: true,
                      shared: true,
                      outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                  ))
                  .WriteTo.Console();
        });
    }
}
```

**逐行拆解：**

#### `host.UseSerilog(...)`

这是 Serilog.AspNetCore 提供的扩展方法，它接管了 ASP.NET Core 的整个日志管道。调用后：
- `ILogger<T>` 的所有输出都走 Serilog
- ASP.NET Core 框架日志（Kestrel、MVC、EF Core）也走 Serilog

#### `MinimumLevel.Information()`

设置最低日志级别为 Information。级别由低到高：

```
Verbose → Debug → Information → Warning → Error → Fatal
```

这意味着 Debug 和 Verbose 级别的日志会被过滤掉。生产环境推荐 `Information`，开发时可临时降到 `Debug`。

> 注意：这里没有对框架日志做 Override 降级，所以 ASP.NET Core 自身的 Information 级别日志（如每个请求的 "HTTP GET /api/xxx responded 200"）也会写入文件。如果觉得太吵，可以增加：
> ```csharp
> .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
> .MinimumLevel.Override("System", LogEventLevel.Warning)
> ```

#### `.Enrich.FromLogContext()`

**这是结构化日志的核心。** 它允许你在日志写入时动态附加额外的属性：

```csharp
using (LogContext.PushProperty("UserId", userId))
using (LogContext.PushProperty("RequestId", requestId))
{
    _logger.LogWarning("用户 {UserId} 请求超时", userId);
    // 输出会自动包含 UserId 和 RequestId 属性
}
```

ASP.NET Core 的 `Serilog.AspNetCore` 还会自动附加 `RequestMethod`、`RequestPath`、`StatusCode` 等属性（如果用 UseSerilogRequestLogging）。

#### `.WriteTo.Async(a => a.File(...))`

**异步文件写入。** `Serilog.Sinks.Async` 包装了文件 sink，使得日志写入在后台线程执行，业务线程不会被 I/O 阻塞。

文件 sink 的参数：

| 参数 | 值 | 含义 |
|------|-----|------|
| path | `appsettings` 中 `LoggerToFile` 的值 | 日志文件路径模板，含 `{RollingInterval}` 占位符 |
| rollingInterval | `Day` | 每天一个日志文件 |
| retainedFileCountLimit | `7` | 保留最近 7 天日志，自动删除旧的 |
| fileSizeLimitBytes | `500 * 1024 * 1024` (500MB) | 单个文件达到 500MB 时滚动 |
| rollOnFileSizeLimit | `true` | 允许按文件大小滚动 |
| shared | `true` | 多进程共享写入（Kestrel 多进程场景） |

文件路径示例（以 FileService 为例）：

| 环境 | 路径 |
|------|------|
| 开发 | `f:/temp1/log/FileService-20260703.log` |
| 生产 | `/var/www/wpenglish/log/FileService-20260703.log` |

文件名中的日期由 `{RollingInterval.Day}` 自动替换，`-` 是路径模板中的字面量。

#### `outputTemplate`

```
{Timestamp:yyyy-MM-dd HH:mm:ss zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}
```

日志行格式：

```
2026-07-03 14:30:00 +08:00 [INF] 用户登录成功
2026-07-03 14:30:05 +08:00 [ERR] 数据库连接失败
System.Data.SqlClient.SqlException: ...
```

| 占位符 | 含义 |
|--------|------|
| `{Timestamp:yyyy-MM-dd HH:mm:ss zzz}` | 带时区偏移的时间戳 |
| `[{Level:u3}]` | 级别缩写（INF/DBG/WRN/ERR/FTL），大写 |
| `{Message:lj}` | 日志消息，JSON 编码的多行文本 |
| `{NewLine}` | 换行符 |
| `{Exception}` | 异常信息（含堆栈） |

### 4.2 微服务中的一行注册

每个 `Program.cs` 只需一行：

```csharp
builder.Host.AddSerilogConfiguration(builder.Configuration);
```

以 FileService 为例：

```csharp
var builder = WebApplication.CreateBuilder(args);

// ... 各种服务注册 ...

builder.Host.AddSerilogConfiguration(builder.Configuration);  // ← 这行

var app = builder.Build();
// ...
app.Run();
```

### 4.3 环境相关的配置

Serilog 不读 `appsettings.json` 的 `"Serilog"` 节，而是从 `IConfiguration` 中直接取 `LoggerToFile` 值。每个微服务在 `appsettings.Development.json` 和 `appsettings.Production.json` 中分别配置：

```json
// FileService/appsettings.Development.json
{
  "LoggerToFile": "f:/temp1/log/FileService-.log"
}
```

```json
// FileService/appsettings.Production.json
{
  "LoggerToFile": "/var/www/wpenglish/log/FileService-.log"
}
```

这样不同环境、不同微服务的日志文件自动隔离：

```
/var/www/wpenglish/log/
├── FileService-20260703.log
├── FileService-20260702.log
├── IdentityService-20260703.log
├── Listening.Admin-20260703.log
└── Listening.Main-20260703.log
```

## 五、NuGet 包依赖

```xml
<ItemGroup>
  <PackageReference Include="Serilog" Version="4.3.1" />
  <PackageReference Include="Serilog.AspNetCore" Version="10.0.0" />
  <PackageReference Include="Serilog.Sinks.Async" Version="2.1.0" />
  <PackageReference Include="Serilog.Sinks.Console" Version="6.1.1" />
  <PackageReference Include="Serilog.Sinks.File" Version="7.0.0" />
</ItemGroup>
```

| 包 | 作用 |
|----|------|
| **Serilog** | 核心库 |
| **Serilog.AspNetCore** | 与 ASP.NET Core 集成，接管 `ILogger<T>` 管道 |
| **Serilog.Sinks.Async** | 异步包装器，避免日志 I/O 阻塞业务线程 |
| **Serilog.Sinks.Console** | 控制台输出（开发时直接看、Docker 中由容器收集） |
| **Serilog.Sinks.File** | 滚动文件输出，支持按日期/大小滚动 |

## 六、使用示例

### 6.1 在业务代码中使用

```csharp
public class UserService
{
    private readonly ILogger<UserService> _logger;

    public UserService(ILogger<UserService> logger)
    {
        _logger = logger;
    }

    public async Task<UserDto> LoginAsync(string username, string password)
    {
        _logger.LogInformation("用户登录尝试: {Username}", username);

        try
        {
            var user = await _userRepo.GetByUsernameAsync(username);
            if (user == null)
            {
                _logger.LogWarning("登录失败: 用户 {Username} 不存在", username);
                throw new NotFoundException("用户不存在");
            }

            _logger.LogInformation("用户 {UserId} 登录成功", user.Id);
            return new UserDto(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户 {Username} 登录时发生异常", username);
            throw;
        }
    }
}
```

注意日志中使用的是**占位符** `{Username}` 而不是字符串拼接：

```csharp
// ❌ 不推荐：字符串拼接
_logger.LogWarning("登录失败: 用户 " + username + " 不存在");

// ✅ 推荐：结构化占位符
_logger.LogWarning("登录失败: 用户 {Username} 不存在", username);
```

两者的区别：

- 字符串拼接：即使在日志级别被过滤掉的情况下，拼接操作也会执行
- 结构化占位符：只有当日志级别满足条件时才执行格式化；且 `{Username}` 作为独立字段存储，可以查询

### 6.2 输出效果

```
2026-07-03 14:30:00 +08:00 [INF] 用户登录尝试: admin
2026-07-03 14:30:01 +08:00 [WRN] 登录失败: 用户 guest 不存在
2026-07-03 14:30:02 +08:00 [INF] 用户 1001 登录成功
2026-07-03 14:30:05 +08:00 [ERR] 用户 admin 登录时发生异常
System.Data.SqlClient.SqlException: 超时时间已到
   at UserService.LoginAsync() in ...
```

## 七、进阶优化方向

### 7.1 请求日志中间件

Serilog.AspNetCore 提供了 `UseSerilogRequestLogging()` 中间件，可以记录每个 HTTP 请求的详细信息：

```csharp
app.UseSerilogRequestLogging();  // 放在 app.UseRouting() 之后
```

效果：每个请求自动输出了一条包含方法、路径、状态码、耗时的结构化日志。

### 7.2 更多的 Enrichers

```csharp
config.Enrich.WithMachineName()       // 附加机器名
      .Enrich.WithEnvironmentName()   // 附加环境名 (Development/Production)
      .Enrich.WithThreadId()          // 附加线程 ID
```

### 7.3 集中式日志收集

生产环境中，文件日志只是过渡方案。推荐将日志发送到集中式平台：

```csharp
// Seq（本地搭建的日志服务器）
.WriteTo.Seq("http://localhost:5341")

// Elasticsearch（配合 Kibana 可视化）
.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://localhost:9200")))

// 阿里云日志服务 / 腾讯云 CLS 等云服务
```

### 7.4 按级别拆分文件

如果调试日志太多，可以分文件存储：

```csharp
// 错误日志单独存一份
.WriteTo.Logger(c => c
    .Filter.ByIncludingOnly(e => e.Level == LogEventLevel.Error)
    .WriteTo.File("logs/error-.log", rollingInterval: RollingInterval.Day))

// 所有日志
.WriteTo.File("logs/all-.log", rollingInterval: RollingInterval.Day)
```

## 八、总结

| 主题 | 要点 |
|------|------|
| **为什么用 Serilog** | 结构化日志，可搜索、可过滤、可聚合 |
| **架构设计** | 集中配置 + 一行注册，4 个微服务共享同一套配置 |
| **文件日志** | 异步写入、按天滚动、保留 7 天、500MB 上限、支持多进程共享 |
| **控制台输出** | 开发调试、Docker 容器日志收集 |
| **结构化占位符** | 用 `{UserId}` 而不是字符串拼接，让日志可查询 |
| **上下文丰富** | `FromLogContext()` 自动附加请求上下文 |

Serilog 的引入代码非常轻量——一个扩展方法 + 一行注册 + 一个配置文件键值——却为整个多微服务架构打下了坚实的日志基础。当线上出现问题时，你不会再对着满屏的文本日志发愁，而是可以用结构化的方式快速定位问题。
