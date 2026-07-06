# ASP.NET Core 全局异常处理：从混乱到优雅

## 一、前言

在生产环境中，一个未处理异常会导致什么？

```
HTTP/1.1 500 Internal Server Error
content-type: text/html

<!DOCTYPE html>
<html>
<head><title>500 Internal Server Error</title></head>
<body>... 一大坨黄色的错误页面 ...</body>
</html>
```

更糟的是，有时候连 500 都不是——直接连接断开，前端收到 `ERR_CONNECTION_RESET`，用户看到的是白屏。

写 Web API 时，异常处理是一个**贯穿整个系统的横切关注点**——每个接口都可能抛异常，不可能在每个 Action 里写 try-catch。需要一个**统一的地方**来：

1. 捕获所有未处理异常
2. 记录日志（包含堆栈信息）
3. 返回对前端友好的 JSON 响应

这就是 **GlobalExceptionHandler（全局异常处理中间件）** 做的事。

## 二、为什么需要全局异常处理

### 没有全局异常处理时

```csharp
[HttpGet("{id}")]
public async Task<ActionResult<Category>> GetById(Guid id)
{
    var category = await _repository.GetCategoryByIdAsync(id);
    if (category == null)
    {
        return NotFound("分类不存在");
    }
    return Ok(category);
}

[HttpPost]
public async Task<ActionResult> Create(Category request)
{
    try
    {
        var category = Category.Create(request.Title, request.CoverUrl);
        // ...
    }
    catch (Exception ex)
    {
        // 每个 Action 都写一遍？
        _logger.LogError(ex, "创建分类失败");
        return StatusCode(500, "服务器内部错误");
    }
}
```

**问题：**

| 问题 | 后果 |
|------|------|
| 每个 Action 重复 try-catch | 代码膨胀，真正的业务逻辑被淹没 |
| 异常类型处理不一致 | 有的返回 400，有的返回 500，前端难以统一处理 |
| 日志格式不统一 | 有的记了日志，有的没记，排查问题时遗漏 |
| 开发环境泄露堆栈 | 返回了 `System.NullReferenceException: Object reference...` 给前端 |

### 有全局异常处理后

```csharp
[HttpGet("{id}")]
public async Task<ActionResult<Category>> GetById(Guid id)
{
    var category = await _repository.GetCategoryByIdAsync(id);
    
    // 直接抛异常，由全局处理器统一处理
    if (category == null)
        throw new NotFoundException("分类不存在");

    return Ok(category);
}

[HttpPost]
public async Task<ActionResult> Create(Category request)
{
    // 业务逻辑中出现问题就抛异常
    // 不需要 try-catch，不需要手动 return 500
    var category = Category.Create(request.Title, request.CoverUrl);
    // ...
}
```

Controller 只需要关注业务流程，出了异常"甩出去"就行——全局中间件统一接盘。

## 三、实现原理

### 3.1 中间件管道

ASP.NET Core 的请求处理是一个管道（Pipeline）：中间件按照注册顺序层层嵌套。

```
请求进入
    │
    ▼
┌─────────────────────────────────────────┐
│  app.UseCors()                          │
├─────────────────────────────────────────┤
│  app.UseGlobalExceptionHandler()   ← 在这里包裹所有后续中间件  │
├─────────────────────────────────────────┤
│  app.UseAuthentication()                │
├─────────────────────────────────────────┤
│  app.UseAuthorization()                 │
├─────────────────────────────────────────┤
│  app.MapControllers()                   │
│    └─ Controller → Action               │
└─────────────────────────────────────────┘
    │
    ▼
响应返回
```

全局异常处理中间件必须**注册在所有中间件的最外层**（除了 CORS），这样才能捕获管道中任何环节抛出的异常——包括认证失败、授权失败、模型绑定错误、Action 执行异常等。

### 3.2 中间件的标准结构

```csharp
public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next.Invoke(httpContext);  // 调用下一个中间件
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(httpContext, ex);  // 捕获所有异常
        }
    }
}
```

核心逻辑只有两步：

1. `await _next.Invoke(httpContext)` — 把请求交给管道中的下一个中间件
2. `catch (Exception ex)` — 如果后续任何环节抛出异常，在这里统一拦截

### 3.3 智能异常分类

```csharp
private async Task HandleExceptionAsync(HttpContext httpContext, Exception exception)
{
    _logger.LogError(exception, "发生未处理异常:{Message}", exception.Message);

    var (statusCode, message) = exception switch
    {
        NotFoundException ex => (HttpStatusCode.NotFound, ex.Message),
        BusinessException ex => (HttpStatusCode.BadRequest, ex.Message),
        UnauthorizedAccessException ex => (HttpStatusCode.Unauthorized, ex.Message),
        ArgumentException ex => (HttpStatusCode.BadRequest, ex.Message),
        _ => (HttpStatusCode.InternalServerError, "服务器内部错误，请稍后重试")
    };

    var response = new
    {
        success = false,
        message = message,
        statusCode = (int)statusCode,
        timestamp = DateTime.UtcNow
    };

    httpContext.Response.StatusCode = (int)statusCode;
    httpContext.Response.ContentType = "application/json";
    await httpContext.Response.WriteAsync(
        JsonSerializer.Serialize(response, _jsonOptions));
}
```

**异常 → HTTP 状态码映射规则：**

| 异常类型 | HTTP 状态码 | 含义 |
|---------|------------|------|
| `NotFoundException` | **404 Not Found** | 请求的资源不存在 |
| `BusinessException` | **400 Bad Request** | 业务规则校验不通过 |
| `UnauthorizedAccessException` | **401 Unauthorized** | 无权限访问 |
| `ArgumentException` | **400 Bad Request** | 参数错误 |
| **其他所有异常** | **500 Internal Server Error** | 服务器内部错误，不泄露具体信息 |

这种 switch 表达式（C# 8.0+ 的模式匹配）比传统的 if-else 或 switch-case 更简洁，且编译器会检查是否覆盖了所有情况。

### 3.4 异常类型的设计

为了实现良好的异常分类，项目定义了自定义异常类：

```csharp
// 资源不存在
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string message, Exception inner) : base(message, inner) { }
}

// 业务规则冲突
public class BusinessException : Exception
{
    public int Code { get; set; }

    public BusinessException(string message, int code = 400) : base(message)
    {
        Code = code;
    }
}
```

**使用示例：**

```csharp
// 查找不存在的资源
public async Task<Category> GetCategoryByIdAsync(Guid id)
{
    var category = await _repository.GetCategoryByIdAsync(id);
    if (category == null)
        throw new NotFoundException($"未找到Id:{id}的Category");
    return category;
}

// 业务规则不满足
public void ValidateDuration(double durationInSecond)
{
    if (durationInSecond <= 0)
        throw new BusinessException("时长必须大于0");
}
```

## 四、响应格式

异常处理中间件输出的 JSON 格式：

```json
{
  "success": false,
  "message": "未找到Id:xxx的Category",
  "statusCode": 404,
  "timestamp": "2026-07-04T02:30:00Z"
}
```

这与其他接口的 `ApiResponse` 格式保持一致——前端可以用同一套 `if (!res.success)` 逻辑处理所有错误响应。

**与 ApiResponseFilter 的关系：**

```
Controller 中抛出异常
    ↓
GlobalExceptionHandler 捕获
    ↓
返回 JSON 响应 (success=false, message, statusCode)
    ↓
ApiResponseFilter 不会拦截这个 JSON —— 因为已经是 ObjectResult 
```

但注意：GlobalExceptionHandler 在中间件管道的最外层，它返回的 JSON 响应**不经过 ApiResponseFilter**（Filter 只作用于 Controller 的 ActionResult）。所以响应格式是独立的。

## 五、注册方式

```csharp
public static class GlobalExceptionHandlerExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandler>();
    }
}
```

在 `Program.cs` 中注册：

```csharp
var app = builder.Build();

app.UseCors();                        // CORS 通常在异常处理器之前
app.UseGlobalExceptionHandler();      // ← 全局异常处理在管道最外层
app.UseAuthentication();              // 认证
app.UseAuthorization();               // 授权
app.MapControllers();                 // 路由

app.Run();
```

**关键：`UseGlobalExceptionHandler()` 必须在所有可能抛出异常的中间件之前注册**，这样才能捕获它们。

## 六、完整代码

```csharp
using Commons.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace Commons.Middlewares
{
    public class GlobalExceptionHandler
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public GlobalExceptionHandler(RequestDelegate next, 
            ILogger<GlobalExceptionHandler> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next.Invoke(httpContext);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private async Task HandleExceptionAsync(
            HttpContext httpContext, Exception exception)
        {
            _logger.LogError(exception, "发生未处理异常:{Message}", 
                exception.Message);

            var (statusCode, message) = exception switch
            {
                NotFoundException ex => (HttpStatusCode.NotFound, ex.Message),
                BusinessException ex => (HttpStatusCode.BadRequest, ex.Message),
                UnauthorizedAccessException ex => (HttpStatusCode.Unauthorized, ex.Message),
                ArgumentException ex => (HttpStatusCode.BadRequest, ex.Message),
                _ => (HttpStatusCode.InternalServerError, "服务器内部错误，请稍后重试")
            };

            var response = new
            {
                success = false,
                message = message,
                statusCode = (int)statusCode,
                timestamp = DateTime.UtcNow
            };

            httpContext.Response.StatusCode = (int)statusCode;
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsync(
                JsonSerializer.Serialize(response, _jsonOptions));
        }
    }

    public static class GlobalExceptionHandlerExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(
            this IApplicationBuilder app)
        {
            return app.UseMiddleware<GlobalExceptionHandler>();
        }
    }
}
```

## 七、进阶优化方向

### 7.1 开发环境返回详细错误

生产环境不应该泄露堆栈信息，但开发环境需要详细错误来调试。可以注入 `IWebHostEnvironment` 判断：

```csharp
public class GlobalExceptionHandler
{
    private readonly IWebHostEnvironment _env;
    // ...

    private async Task HandleExceptionAsync(HttpContext httpContext, Exception exception)
    {
        _logger.LogError(exception, "发生未处理异常");

        var message = exception switch
        {
            NotFoundException ex => ex.Message,
            BusinessException ex => ex.Message,
            _ => _env.IsDevelopment() 
                ? exception.ToString()           // 开发环境：详细堆栈
                : "服务器内部错误，请稍后重试"       // 生产环境：模糊信息
        };
        // ...
    }
}
```

### 7.2 支持 FluentValidation 异常

项目中使用了手动调用验证器，但如果使用 `ValidateAndThrow()` 模式，FluentValidation 会抛出 `ValidationException`。可以在 switch 中增加分支：

```csharp
ValidationException ex => (HttpStatusCode.BadRequest,
    string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)))
```

### 7.3 集成请求上下文

与 Serilog 的 `LogContext` 配合，在日志中附加请求信息：

```csharp
using (LogContext.PushProperty("RequestPath", httpContext.Request.Path))
using (LogContext.PushProperty("RequestMethod", httpContext.Request.Method))
using (LogContext.PushProperty("User", httpContext.User?.Identity?.Name))
{
    _logger.LogError(exception, "发生未处理异常");
}
```

### 7.4 记录请求体

对于 500 错误，有时需要知道请求体是什么。可以读取请求体并附加到日志中（注意：请求体只能读取一次，需要启用缓冲）：

```csharp
httpContext.Request.EnableBuffering();
var body = await new StreamReader(httpContext.Request.Body).ReadToEndAsync();
httpContext.Request.Body.Position = 0;
_logger.LogError(exception, "请求体: {Body}", body);
```

## 八、总结

| 主题 | 要点 |
|------|------|
| **作用** | 统一捕获所有未处理异常，避免泄漏堆栈信息 |
| **实现方式** | ASP.NET Core 中间件，try-catch 包裹整个管道 |
| **注册位置** | 管道最外层（除 CORS 外），确保能捕获所有后续中间件 |
| **异常分类** | switch 模式匹配，按异常类型映射为不同的 HTTP 状态码 |
| **响应格式** | `{ success, message, statusCode, timestamp }` JSON |
| **自定义异常** | `NotFoundException`（404）、`BusinessException`（400）等 |
| **日志记录** | 自动记录 Error 级别日志，包含异常堆栈 |

整个 `GlobalExceptionHandler` 加上扩展方法不到 80 行代码，却解决了 Web API 开发中最常见也最头痛的问题——**异常处理分散、格式不统一、信息泄漏**。它和 `ApiResponseFilter` 一起构成了项目的"防御性基础设施"：一个管业务返回的正常流程，一个管异常流程，双剑合璧。
