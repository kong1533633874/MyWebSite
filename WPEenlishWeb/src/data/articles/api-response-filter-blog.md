# ASP.NET Core 统一响应体设计：ApiResponseFilter 实践

## 一、前言

在实际的项目开发中，前后端分离已经是标配架构。但你是否遇到过这样的困扰：

- 有的接口返回 `{ "data": {...}, "success": true }`，有的直接返回 `{ "id": 1 }`，前端每次都要处理不同的格式？
- 成功时返回 JSON 对象，失败时却返回 HTML 错误页面或纯文本 `500 Internal Server Error`？
- 每个控制器里都手动写 `return Ok(new ApiResponse { ... })`，重复劳动且容易遗漏？

**统一响应体（Unified API Response）** 就是为了解决这些问题而生的设计模式。它定义了一个标准化的接口响应格式，让前端可以用同一套逻辑处理所有请求结果。

## 二、统一响应体解决的核心问题

| 问题 | 后果 | 统一响应体的方案 |
|------|------|------------------|
| 响应格式不统一 | 前端需要适配 N 种返回格式 | 所有接口输出相同结构的 `ApiResponse` |
| 错误信息散落各处 | 前端难以统一展示错误 | 格式化的 Message + 状态码 |
| 接口文档需要逐个标注 | 维护成本高 | 自动化的统一包装，文档只需关注 Data |
| 过滤器/中间件难以统一处理 | 重复代码 | 一次性注册，全局生效 |

## 三、核心设计：ApiResponse 模型

要统一响应，首先要定义一个标准的数据结构。通常包含以下几个字段：

### 标准响应模型

```csharp
public class ApiResponse<T>
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// HTTP 状态码
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// 提示消息（成功时可为空，失败时为错误描述）
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// 实际业务数据（成功时填充，失败时为 null）
    /// </summary>
    public T? Data { get; set; }
}
```

### 设计原则

- **Success** 是布尔值，前端可以 `if (res.success) { ... }` 直接判断，无需解析状态码。
- **StatusCode** 保留 HTTP 语义，方便调试和网关代理。
- **Message** 在失败时携带人类可读的错误信息，成功时可为空。
- **Data** 在成功时携带真正的业务数据，失败时为 `null`——避免前端误用错误状态下的数据。

## 四、自动包装的实现原理

手动在每个 Action 里包装 `ApiResponse` 太繁琐了。ASP.NET Core 提供了 **过滤器（Filter）** 机制，我们可以用 `IResultFilter` 在 MVC 输出结果的**最后一刻**自动包装。

### 为什么选择 IResultFilter？

ASP.NET Core 的请求管道中有几个切入点：

```
请求 → Middleware → 授权 → Action → ActionFilter → 执行 Action → ResultFilter → 输出响应
                                                          ↑
                                              我们在 ResultFilter 的 
                                              OnResultExecuting 中拦截
```

- **Middleware（中间件）**：太底层，需要手动处理 `HttpContext`，无法轻松获取 Action 返回的具体对象。
- **ActionFilter**：在 Action 执行前后触发，但此时结果尚未序列化，如果我们在这里替换结果，后面的 ResultFilter 还能干预。
- **IResultFilter**：在 Action 执行完毕、结果即将写入响应时触发。这是包装的**最佳时机**——我们知道最终的状态码和返回对象，而且只需处理 `ObjectResult` 和 `StatusCodeResult` 两种类型。

### 代码实现详解

#### 1. 定义状态码的默认错误消息

```csharp
private static readonly Dictionary<int, string> DefaultMessages = new()
{
    { 400, "请求参数无效" },
    { 401, "未授权，请先登录" },
    { 403, "无权限访问该资源" },
    { 404, "请求的资源不存在" },
    { 405, "不支持的请求方法" },
    { 415, "不支持的媒体类型" },
    { 500, "服务器内部错误" }
};
```

> 标准化常用的 HTTP 错误码对应的中文提示。这样即使你的代码里只写了 `return StatusCode(403)`，没有提供消息，前端也能拿到友好的描述。

#### 2. 实现 IResultFilter

核心方法是 `OnResultExecuting(ResultExecutingContext context)`，它发生在结果被写入响应流之前。

##### 第一步：跳过不需要包装的结果类型

```csharp
if (context.Result is not ObjectResult and not StatusCodeResult)
    return;
```

`FileResult`、`RedirectResult`、`ChallengeResult` 等特殊结果不需要包装，直接放行。

##### 第二步：处理 ObjectResult（最常见的返回类型）

```csharp
case ObjectResult objResult:
    statusCode = objResult.StatusCode ?? 200;

    // 已经是包装过的，跳过（避免重复包装）
    if (objResult.Value is ApiResponse)
        return;

    if (statusCode >= 200 && statusCode < 300)
    {
        data = objResult.Value;  // 成功时，原始数据放到 Data 字段
    }
    else
    {
        // 失败时，尝试从 ProblemDetails 提取友好信息
        message = objResult.Value switch
        {
            ValidationProblemDetails vp => string.Join("; ",
                vp.Errors.SelectMany(kv =>
                {
                    var field = kv.Key.TrimStart('$', '.');
                    return kv.Value.Select(e => $"{field}: {e}");
                })),
            ProblemDetails problem => problem.Title ?? problem.Detail,
            _ => objResult.Value?.ToString()
        };
    }
    break;
```

这个分支做了几件重要的事：

1. **防重复包装**：如果值已经是 `ApiResponse` 类型，直接跳过。
2. **成功/失败分流**：2xx 状态码认为成功，提取数据；否则认为失败，提取错误消息。
3. **智能错误提取**：
   - `ValidationProblemDetails`：模型验证失败的场景，自动拼接所有字段的错误信息，格式如 `"email: 邮箱格式不正确; password: 密码不能为空"`。
   - 普通 `ProblemDetails`：取 `Title` 或 `Detail`。
   - 其他类型：直接调用 `ToString()`。

##### 第三步：处理 StatusCodeResult

```csharp
case StatusCodeResult statusResult:
    statusCode = statusResult.StatusCode;
    break;
```

`StatusCodeResult`（如 `return StatusCode(403)`）没有 body，只需要记录状态码，稍后填充默认消息。

##### 第四步：填充默认消息并包装

```csharp
// 失败且没有提供消息时，从默认字典获取
if (!(statusCode >= 200 && statusCode < 300) && string.IsNullOrEmpty(message))
{
    DefaultMessages.TryGetValue(statusCode, out message);
}

var wrapped = new ApiResponse<object?>
{
    Success = statusCode >= 200 && statusCode < 300,
    StatusCode = statusCode,
    Message = message,
    Data = statusCode >= 200 && statusCode < 300 ? data : null
};

context.Result = new ObjectResult(wrapped) { StatusCode = statusCode };
```

最后一步：用 `ObjectResult` 替换原结果，这样 ASP.NET Core 的 `System.Text.Json` 序列化器会将其自动序列化为 JSON 响应。

## 五、完整代码

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Commons.Filters
{
    public class ApiResponseFilter : IResultFilter
    {
        private static readonly Dictionary<int, string> DefaultMessages = new()
        {
            { 400, "请求参数无效" },
            { 401, "未授权，请先登录" },
            { 403, "无权限访问该资源" },
            { 404, "请求的资源不存在" },
            { 405, "不支持的请求方法" },
            { 415, "不支持的媒体类型" },
            { 500, "服务器内部错误" }
        };

        public void OnResultExecuting(ResultExecutingContext context)
        {
            if (context.Result is not ObjectResult and not StatusCodeResult)
                return;

            int statusCode;
            object? data = null;
            string? message = null;

            switch (context.Result)
            {
                case ObjectResult objResult:
                    statusCode = objResult.StatusCode ?? 200;
                    if (objResult.Value is ApiResponse)
                        return;
                    if (statusCode >= 200 && statusCode < 300)
                    {
                        data = objResult.Value;
                    }
                    else
                    {
                        message = objResult.Value switch
                        {
                            ValidationProblemDetails vp => string.Join("; ",
                                vp.Errors.SelectMany(kv =>
                                {
                                    var field = kv.Key.TrimStart('$', '.');
                                    return kv.Value.Select(e => $"{field}: {e}");
                                })),
                            ProblemDetails problem => problem.Title ?? problem.Detail,
                            _ => objResult.Value?.ToString()
                        };
                    }
                    break;

                case StatusCodeResult statusResult:
                    statusCode = statusResult.StatusCode;
                    break;

                default:
                    return;
            }

            if (!(statusCode >= 200 && statusCode < 300) && string.IsNullOrEmpty(message))
            {
                DefaultMessages.TryGetValue(statusCode, out message);
            }

            var wrapped = new ApiResponse<object?>
            {
                Success = statusCode >= 200 && statusCode < 300,
                StatusCode = statusCode,
                Message = message,
                Data = statusCode >= 200 && statusCode < 300 ? data : null
            };

            context.Result = new ObjectResult(wrapped) { StatusCode = statusCode };
        }

        public void OnResultExecuted(ResultExecutedContext context) { }
    }
}
```

## 六、注册与使用

在 `Program.cs` 中全局注册：

```csharp
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiResponseFilter>();
});
```

注册后：

- `return Ok(user)` → `{ "success": true, "statusCode": 200, "data": { ... }, "message": null }`
- `return BadRequest("邮箱已注册")` → `{ "success": false, "statusCode": 400, "data": null, "message": "邮箱已注册" }`
- `return StatusCode(403)` → `{ "success": false, "statusCode": 403, "data": null, "message": "无权限访问该资源" }`
- 模型验证失败自动 → `{ "success": false, "statusCode": 400, "data": null, "message": "email: 邮箱格式不正确; password: 密码不能为空" }`

## 七、进阶优化方向

1. **对端点选择性启用**：如果需要某些控制器/方法不自动包装，可以定义 `[SkipApiResponse]` 特性，在过滤器中检查。

2. **支持泛型接口文档**：结合 Swashbuckle/Swagger，通过 `IOperationFilter` 让 Swagger 文档正确显示 `ApiResponse<T>` 的返回结构，而不是 `ObjectResult` 的裸类型。

3. **与 FluentValidation 集成**：如果使用 FluentValidation，可在过滤器或中间件中将验证失败的结果转换为统一的 `ApiResponse` 格式。

4. **日志增强**：在 `OnResultExecuted` 中记录请求路径、状态码和执行耗时，用于监控。

## 八、总结

统一响应体不仅仅是一个"格式规范"，它更是一层**防御性设计**：

- **对前端**：消除了对多种响应格式的适配代码，错误展示逻辑统一。
- **对后端**：开发者只需关注业务逻辑，返回原始数据即可，包装由过滤器自动完成。
- **对运维**：标准化的格式便于 API 网关解析、日志采集和监控告警。

`IResultFilter` 在 ASP.NET Core 的请求管道中恰到好处——它足够晚，拿到了完整的执行结果；又足够早，能在序列化之前替换输出。加上 `ProblemDetails` 的自动解析和默认错误消息兜底，整套方案实现了轻量而健壮的统一响应机制。

如果你项目里还没有统一的响应格式，这个方案开箱即用，值得一试。
