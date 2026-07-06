# ASP.NET Core 请求验证：FluentValidation 从入门到项目实践

## 一、前言

写 Web API 时，最烦的事情之一就是参数验证。

```csharp
public async Task<IActionResult> CreateUser(string userName, string password)
{
    if (string.IsNullOrEmpty(userName))
        return BadRequest("用户名不能为空");
    if (userName.Length < 2)
        return BadRequest("用户名至少2个字符");
    if (userName.Length > 20)
        return BadRequest("用户名不能超过20个字符");
    if (string.IsNullOrEmpty(password))
        return BadRequest("密码不能为空");
    if (password.Length < 6)
        return BadRequest("密码至少6个字符");
    // ... 业务逻辑
}
```

这样的代码存在几个问题：

- **散落各处**：验证逻辑分散在每个 Action 中，很难统一管理
- **重复劳动**：相似的验证规则在多个接口中重复书写
- **可读性差**：真正的业务逻辑被验证代码淹没
- **难以测试**：验证逻辑和业务逻辑耦合在一起

**FluentValidation** 是 .NET 最流行的验证库，它用声明式规则将验证逻辑从业务代码中分离出来，让代码既清晰又易于复用。

## 二、FluentValidation 的核心思想

FluentValidation 采用"规则链"的模式，每个验证规则像搭积木一样串联起来：

```csharp
public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("姓名不能为空")
            .Length(2, 20).WithMessage("姓名长度需在2-20个字符之间");

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Age)
            .InclusiveBetween(0, 150);
    }
}
```

**对比传统方式：**

| 方面 | 手写 if/else | FluentValidation |
|------|-------------|------------------|
| 代码位置 | 散落在 Controller 中 | 集中在 Validator 类中 |
| 可读性 | 需要阅读 if 条件 | 链式调用一目了然 |
| 复用性 | 复制粘贴 | 继承/组合/扩展方法 |
| 异步验证 | 手动 Task | `MustAsync` 原生支持 |
| 错误收集 | 遇到第一个错误就返回 | 收集全部错误一并返回 |
| 测试性 | 需启动 Web 项目 | 单元测试 Validator 即可 |

## 三、项目中的 FluentValidation 架构

本项目在两个微服务中使用 FluentValidation，并抽离了**共享的扩展验证器**：

```
WPEnglish/
├── HashHelper/Commons.csproj           ← 共享扩展验证器
│   └── Validators/
│       ├── EnumerableValidators.cs     ← 集合去重、不包含
│       └── UriValidators.cs            ← Uri 非空、长度
│
├── IdentitServer.WebApi                ← 身份认证
│   └── Controllers/IdentityRequest/
│       ├── AddUserRequest.cs           + Validator（内联）
│       ├── LoginRequest.cs             + Validator（内联）
│       └── UpdateUserRequest.cs        + Validator（内联）
│
└── Listening.Admin.WebApi              ← 管理后台
    ├── AlbumController/Request/        ← 专辑 CRUD × 3
    ├── CategoryController/Request/     ← 分类 CRUD × 3
    └── EpisodeController/Request/      ← 音频 CRUD × 3
```

### 设计模式：DTO + Validator 同一个文件

每个请求 DTO 和它的 Validator 写在**同一个文件**中：

```csharp
// ===== AlbumAddRequest.cs =====

// ① DTO：接收前端参数
public record AlbumAddRequest
{
    public string Title { get; set; }
    public Guid CategoryId { get; set; }
}

// ② Validator：验证规则
public class AlbumAddRequrestValidator : AbstractValidator<AlbumAddRequest>
{
    public AlbumAddRequrestValidator(IdDbContext dbContext)
    {
        RuleFor(x => x.Title).NotEmpty();

        RuleFor(x => x.CategoryId)
            .NotEmpty()
            .MustAsync(async (id, cancellation) =>
                await dbContext.Set<Category>().AnyAsync(c => c.Id == id, cancellation))
            .WithMessage("分类不存在");
    }
}
```

DTO 和 Validator 放在一起的好处：
- **高内聚**：看到 DTO 就知道它的验证规则
- **一个文件搞定**：增删改查时只需操作一个文件
- **VS 中容易定位**：文件名就是 Request 名

## 四、FlutterValidation 中特殊验证场景

### 4.1 数据库唯一性验证

在用 MustAsync 异步请求数据的同时，需要验证提交的数据是否唯一：

```csharp
RuleFor(x => x.CategoryId)
    .NotEmpty()
    .MustAsync(async (id, cancellation) =>
        await dbContext.Set<Category>().AnyAsync(c => c.Id == id, cancellation))
    .WithMessage("分类不存在");
```

这个验证器会向数据库查询传入的 `CategoryId` 是否存在，若不存在则返回验证失败。

MustAsync 返回 `true` 表示验证通过，返回 `false` 表示验证失败。

### 4.2 自定义集合验证器

在 `HashHelper/Commons.csproj` 中，项目定义了针对集合类型 `IEnumerable<T>` 的扩展验证器：

```csharp
namespace FluentValidation
{
    public static class EnumerableValidators
    {
        // 验证集合中不包含重复项
        public static IRuleBuilderOptions<T, IEnumerable<TItem>> NotDuplicated<T, TItem>(
            this IRuleBuilder<T, IEnumerable<TItem>> ruleBuilder)
        {
            return ruleBuilder.Must(s => s == null || s.Distinct().Count() == s.Count());
        }

        // 验证集合中不包含某个特定值
        public static IRuleBuilderOptions<T, IEnumerable<TItem>> NotContains<T, TItem>(
            this IRuleBuilder<T, IEnumerable<TItem>> ruleBuilder, TItem comparedValue)
        {
            return ruleBuilder.Must(s => s == null || !s.Contains(comparedValue));
        }
    }
}
```

**使用示例：**

```csharp
public class AlbumSortRequestValidator : AbstractValidator<AlbumSortRequest>
{
    public AlbumSortRequestValidator()
    {
        RuleFor(x => x.SortedAlbumIds)
            .NotNull()
            .NotEmpty()
            .NotDuplicated()              // ← 自定义扩展：无重复 ID
            .NotContains(Guid.Empty);     // ← 自定义扩展：不含空 GUID
    }
}
```

`NotDuplicated()` 和 `NotContains()` 这两个方法被放在 `FluentValidation` 命名空间中，这样在使用时无需额外 `using` 语句，所有 AbstractValidator 子类自动可见——这是 .NET 扩展方法的一个实用技巧。

### 4.3 自定义 URI 验证器

由于项目中有些业务字段是 `Uri` 类型（如音频文件地址、封面图地址），普通的 `NotEmpty()` 不能直接用于 Uri，因此封装了专用验证器：

```csharp
namespace FluentValidation
{
    public static class UriValidators
    {
        // Uri 不能为空
        public static IRuleBuilderOptions<T, Uri> NotEmptyUri<T>(
            this IRuleBuilder<T, Uri> ruleBuilder)
        {
            return ruleBuilder.Must(p => p == null || !string.IsNullOrWhiteSpace(p.OriginalString))
                .WithMessage("The Uri must not be null nor empty.");
        }

        // Uri 字符串长度限制
        public static IRuleBuilderOptions<T, Uri> Length<T>(
            this IRuleBuilder<T, Uri> ruleBuilder, int min, int max)
        {
            return ruleBuilder.Must(p => string.IsNullOrWhiteSpace(p.OriginalString)
                || (p.OriginalString.Length >= min && p.OriginalString.Length <= max))
                .WithMessage($"The length of Uri must not be between {min} and {max}.");
        }
    }
}
```

**设计细节**：`Length` 方法中**为空时跳过检查**，因为可能有字段允许为 null，只在不为空时限制长度。职责分离——`NotEmptyUri` 负责非空检查，`Length` 只负责长度检查，可以独立组合使用。

```csharp
// 音频地址：必填，长度 1-1000
RuleFor(x => x.AudioUrl)
    .NotEmptyUri()
    .Length(1, 1000);
```

## 五、验证器的注册

在 `Program.cs` 中使用 `AddValidatorsFromAssemblyContaining<T>()` 批量注册：

```csharp
// IdentitServer.WebApi/Program.cs
builder.Services.AddValidatorsFromAssemblyContaining<AdduserRequestValidator>();

// Listening.Admin.WebApi/Program.cs
builder.Services.AddValidatorsFromAssemblyContaining<CategoryAddRequestValidator>();
```

这行代码的作用：

1. 扫描 `AdduserRequestValidator`（或 `CategoryAddRequestValidator`）所在的程序集
2. 找到所有继承 `AbstractValidator<T>` 的类
3. 将它们注册为 `IValidator<T>` 的 Scoped 服务

```csharp
// 注册后，DI 容器中就有了这些映射：
// IValidator<AddUserRequest>      → AdduserRequestValidator
// IValidator<LoginRequest>        → LoginRequestValidator
// IValidator<AlbumAddRequest>     → AlbumAddRequrestValidator
// IValidator<EpisodeAddRequest>   → EpisodeAddRequestValidator
// ... 共 12 个 Validator
```

## 六、手动调用验证

项目没有使用 ASP.NET Core 的自动验证机制，而是**在 Controller 中手动调用**：

```csharp
[ApiController]
[Route("api/[controller]")]
public class AlbumController : ControllerBase
{
    private readonly IValidator<AlbumAddRequest> _addValidator;

    public AlbumController(IValidator<AlbumAddRequest> addValidator /* ... */)
    {
        _addValidator = addValidator;
    }

    [HttpPost("Add")]
    public async Task<ActionResult> Add(AlbumAddRequest request)
    {
        // 手动验证
        var result = await _addValidator.ValidateAsync(request);
        if (!result.IsValid)
        {
            var errors = string.Join("; ", result.Errors.Select(s => s.ErrorMessage));
            return BadRequest(errors);
        }

        // ... 业务逻辑
    }
}
```

### 为什么选择手动调用而不是自动验证？

ASP.NET Core 的 `[ApiController]` 特性内置了自动模型验证——如果验证失败，自动返回 400 响应。但项目选择了手动调用，原因可能包括：

| 自动验证 | 手动调用 |
|---------|---------|
| 框架自动拦截，不可见 | 验证逻辑显式可见 |
| 返回 `ValidationProblemDetails` 格式 | 可以自定义错误格式 |
| 难以注入依赖（如 DbContext） | 构造器注入，天然支持 |
| 测试时需要模拟 HTTP 上下文 | 可以直接 `new Validator().Validate()` |

手动调用的最大优势是**灵活**——你可以决定验证失败的返回格式，也可以让验证和业务逻辑更紧密地配合。

### 错误信息汇总

```csharp
var errors = string.Join("; ", result.Errors.Select(s => s.ErrorMessage));
return BadRequest(errors);
```

当请求有多个字段验证失败时，FluentValidation 会收集**所有**错误（而不是"遇到第一个就返回"），然后拼接成一个字符串返回。经过 `ApiResponseFilter` 包装后，输出如下：

```json
{
  "success": false,
  "statusCode": 400,
  "message": "Title is required; CategoryId must not be empty",
  "data": null
}
```

这样前端可以一次性知道所有字段的问题。

## 七、完整示例：EpisodeAddRequest

这是一个实际业务中比较完整的验证器示例：

```csharp
public record EpisodeAddRequest
{
    public string Title { get; set; }
    public Guid AlbumId { get; set; }
    public Uri AudioUrl { get; set; }
    public double DurationInSecond { get; set; }
    public string SubtitleType { get; set; }
    public string Subtitle { get; set; }
}

public class EpisodeAddRequestValidator : AbstractValidator<EpisodeAddRequest>
{
    public EpisodeAddRequestValidator(IdDbContext dbContext)
    {
        // 标题：1-200 字符
        RuleFor(x => x.Title).Length(1, 200);

        // 专辑 ID：必须存在
        RuleFor(x => x.AlbumId)
            .NotEmpty()
            .MustAsync(async (id, cancellation) =>
                await dbContext.Set<Album>().AnyAsync(a => a.Id == id, cancellation))
            .WithMessage("专辑不存在");

        // 音频地址：自定义 Uri 验证器
        RuleFor(x => x.AudioUrl)
            .NotEmptyUri()
            .Length(1, 1000);

        // 时长：必须大于 0
        RuleFor(x => x.DurationInSecond).GreaterThan(0);

        // 字幕类型：1-10 字符
        RuleFor(x => x.SubtitleType).Length(1, 10);

        // 字幕内容
        RuleFor(x => x.Subtitle).NotEmpty();
    }
}
```

这个验证器展示了 FluentValidation 的几种能力：

| 能力 | 示例 |
|------|------|
| 简单长度校验 | `.Length(1, 200)` |
| 非空校验 | `.NotEmpty()` |
| 数值范围校验 | `.GreaterThan(0)` |
| 异步数据库校验 | `.MustAsync(...)` |
| 自定义扩展方法 | `.NotEmptyUri()` |
| 自定义错误消息 | `.WithMessage("专辑不存在")` |

## 八、FluentValidation 常见规则速查

### 8.1 字符串

```csharp
RuleFor(x => x.Name)
    .NotEmpty()                          // 不能为空（会 trim 后检查）
    .NotNull()                           // 不能为 null
    .Length(2, 20)                       // 长度 2-20
    .Matches(@"^[a-zA-Z]+$")             // 正则匹配
    .EmailAddress()                      // 邮箱格式
    .MaximumLength(100)                  // 最大长度
    .MinimumLength(6);                   // 最小长度
```

### 8.2 数值

```csharp
RuleFor(x => x.Age)
    .GreaterThan(0)                      // > 0
    .GreaterThanOrEqualTo(18)            // >= 18
    .LessThan(150)                       // < 150
    .InclusiveBetween(0, 100)            // 0-100 闭区间
    .ExclusiveBetween(0, 100);           // 0-100 开区间
```

### 8.3 集合

```csharp
RuleFor(x => x.Tags)
    .NotEmpty()                          // 不能为空集合
    .Must(tags => tags.Distinct().Count() == tags.Count())  // 无重复
    .ForEach(tag => tag.Length(1, 20));  // 每个元素分别校验
```

### 8.4 条件验证

```csharp
RuleFor(x => x.Email)
    .NotEmpty()
    .When(x => x.IsEmailRequired);       // 条件满足时才验证

RuleFor(x => x.PhoneNumber)
    .NotEmpty()
    .Unless(x => x.HasEmail);            // 条件不满足时才验证
```

## 九、NuGet 包

```xml
<ItemGroup>
  <!-- 核心库 -->
  <PackageReference Include="FluentValidation" Version="12.1.1" />
  
  <!-- DI 集成（批量注册） -->
  <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="12.1.1" />
</ItemGroup>
```

| 包 | 作用 |
|----|------|
| **FluentValidation** | 核心库：AbstractValidator、RuleFor、MustAsync 等 |
| **FluentValidation.DependencyInjectionExtensions** | `AddValidatorsFromAssemblyContaining<T>()` 扩展方法 |

## 十、最佳实践与避坑

### 10.1 验证器命名规范

```csharp
// ✅ 推荐：DTO 名 + Validator
public class AlbumAddRequestValidator : AbstractValidator<AlbumAddRequest>

// ❌ 不推荐：含义模糊
public class RequestValidator : AbstractValidator<AlbumAddRequest>
```

### 10.2 一个验证器只验证一个模型

每个 `AbstractValidator<T>` 只对应一个 DTO，不要在一个验证器中验证多个模型。

### 10.3 异步验证要注意性能

```csharp
// ✅ 推荐：异步数据库验证
RuleFor(x => x.CategoryId)
    .MustAsync(async (id, ct) => await dbContext.Categories.AnyAsync(c => c.Id == id, ct));

// ❌ 不推荐：同步验证器中使用 Task.Result
RuleFor(x => x.CategoryId)
    .Must(id => dbContext.Categories.Any(c => c.Id == id));  // 阻塞线程
```

`MustAsync` 在内部会使用异步委托，不会阻塞当前线程。而用 `Must` + `Task.Result` 可能导致死锁。

### 10.4 RuleFor 链的顺序影响错误提示

FluentValidation 会**按 RuleFor 的顺序**收集错误。所以应该把最重要的错误放在前面，这样拼接错误时会先提示关键信息：

```csharp
// 用户登录时，"用户名不能为空" 比 "密码不能为空" 优先级高
RuleFor(x => x.UserName).NotEmpty();
RuleFor(x => x.Password).NotEmpty();
```

### 10.5 使用命名空间技巧简化调用

项目中将自定义扩展方法放在 `FluentValidation` 命名空间下：

```csharp
// 文件：HashHelper/Validators/EnumerableValidators.cs
namespace FluentValidation  // ← 注意是 FluentValidation 命名空间
{
    public static class EnumerableValidators
    {
        // ...
    }
}
```

由于 `AbstractValidator<T>` 在 `FluentValidation` 命名空间中，所有验证器类自动 `using FluentValidation`，因此自定义扩展方法无需额外 using 即可使用。

## 十一、总结

| 主题 | 要点 |
|------|------|
| **为什么用 FluentValidation** | 声明式规则链、集中管理、高可读性、易于测试 |
| **架构设计** | 共享扩展（集合/Uri 验证）+ 各微服务独立 Validator |
| **DTO + Validator 同文件** | 高内聚，一个文件管理请求模型及其规则 |
| **手动调用** | 灵活控制错误格式，方便与 ApiResponseFilter 集成 |
| **异步验证** | MustAsync 原生支持数据库校验，不阻塞线程 |
| **规则收集** | 一次验证收集全部错误，前端一次性展示 |

FluentValidation 带来的最大价值不是"少写了 if/else"，而是**让验证逻辑变得可读、可测、可维护**。当你的项目有几十个接口、上百个验证规则时，一个集中的验证体系远比散落的 if 判断更可靠。
