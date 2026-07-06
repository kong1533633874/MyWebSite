# ASP.NET Core 身份认证实战：JWT + Identity 从零搭建

## 一、前言

几乎所有 Web 应用都需要回答三个问题：

1. **你是谁？** — 认证（Authentication）
2. **你能做什么？** — 授权（Authorization）
3. **怎么证明是你？** — 令牌（Token）

在 ASP.NET Core 中，微软提供了 **ASP.NET Core Identity** 框架来管理用户/角色/密码，配合 **JWT（JSON Web Token）** 做无状态认证，是目前最主流的方案。

本文基于项目中的真实代码，逐步拆解 JWT + Identity 的完整实现。

## 二、整体架构

项目采用多微服务架构，身份认证作为一个独立的服务运行：

```
Client (浏览器/App)
    │
    ▼
┌─────────────────────────────────────────────┐
│          IdentitServer.WebApi               │
│  ┌───────────┐  ┌──────────────────────┐   │
│  │ Controller │  │   JWT Middleware     │   │
│  │ (登录/用户管理)│  │  (验证 token)      │   │
│  └─────┬─────┘  └──────────────────────┘   │
│        │                                     │
│  ┌─────▼─────────────────────────────────┐  │
│  │         IdentityServer.Domain         │  │
│  │    IdService (登录逻辑 + 发 Token)     │  │
│  └─────┬─────────────────────────────────┘  │
│        │                                     │
│  ┌─────▼─────────────────────────────────┐  │
│  │      IdentityServer.Infrastructure    │  │
│  │  IdDbContext + IdentityRepository     │  │
│  └─────┬─────────────────────────────────┘  │
│        │                                     │
│  ┌─────▼─────────────────────────────────┐  │
│  │          SQL Server                   │  │
│  │  T_Users / T_Roles / AspNetUserRoles  │  │
│  └───────────────────────────────────────┘  │
└─────────────────────────────────────────────┘
```

核心项目分层：

| 项目 | 职责 |
|------|------|
| **IdentitServer.WebApi** | Web API 入口：Controller、JWT 配置、中间件管道 |
| **IdentityServer.Domain** | 领域层：User/Role 实体、IdService 登录服务 |
| **IdentityServer.Infrastructure** | 持久化：IdDbContext、IdentityRepository |
| **JWT** | 共享库：TokenService 签发 Token、JWTOptions 配置模型 |

## 三、数据模型设计

### 3.1 User 实体

```csharp
public class User : IdentityUser<Guid>
{
    public DateTime CreationTime { get; init; }
    public DateTime? DeletionTime { get; private set; }
    public bool IsDeleted { get; private set; }

    public User(string userName) : base(userName)
    {
        Id = Guid.NewGuid();
        CreationTime = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        this.IsDeleted = true;
        this.DeletionTime = DateTime.UtcNow;
    }
}
```

**设计要点：**

- **继承 `IdentityUser<Guid>`**：ASP.NET Core Identity 已内置了 Id、UserName、PasswordHash、Email、PhoneNumber、LockoutEnd 等 20+ 字段，直接继承即可，无需重新造轮子
- **泛型参数 `Guid`**：默认 IdentityUser 的主键是 string，这里改为 Guid，避免字符串主键的性能问题
- **软删除**：不真正从数据库删除用户，而是标记 `IsDeleted = true`，保留数据用于审计
- **自动生成 Id**：构造函数中 `Id = Guid.NewGuid()`，避免忘记赋值

### 3.2 Role 实体

```csharp
public class Role : IdentityRole<Guid>
{
    public Role()
    {
        this.Id = Guid.NewGuid();
    }
}
```

同样继承 Identity 内置的 Role 基类，自动获得 Id、Name、NormalizedName 等字段。

### 3.3 数据库上下文

```csharp
public class IdDbContext : IdentityDbContext<User, Role, Guid>
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(this.GetType().Assembly);

        // 软删除全局过滤：查询时自动排除已删除用户
        builder.Entity<User>().HasQueryFilter(s => !s.IsDeleted);

        // 用户名唯一索引（仅对未删除的用户生效）
        builder.Entity<User>().HasIndex(u => u.NormalizedUserName)
            .HasFilter("[IsDeleted] = 0")
            .IsUnique();

        // 所有 DateTime 列精确到秒
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var prop in entityType.GetProperties()
                .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)))
            {
                prop.SetColumnType("datetime2(0)");
            }
        }
    }
}
```

**三个关键配置：**

1. **软删除全局过滤器**：`HasQueryFilter(s => !s.IsDeleted)` — 所有查询自动加 `WHERE IsDeleted = 0`，无需在每个查询中手动加条件
2. **软删除感知的唯一索引**：`HasFilter("[IsDeleted] = 0")` — 允许"已删除用户"和"新用户"有相同的用户名，不会冲突
3. **`datetime2(0)`**：默认的 `datetime2(7)` 保留 7 位小数毫秒，绝大多数场景不需要，改为 `datetime2(0)` 精确到秒即可

### 3.4 数据库表结构

继承 `IdentityDbContext<User, Role, Guid>` 后，EF Core 自动创建以下表：

| 表名 | 内容 |
|------|------|
| **T_Users** | 用户表（含 Identity 所有内置字段 + CreationTime、DeletionTime、IsDeleted） |
| **T_Roles** | 角色表 |
| **AspNetUserRoles** | 用户-角色关联表 |
| **AspNetUserClaims** | 用户声明（Claims）表 |
| **AspNetRoleClaims** | 角色声明表 |
| **AspNetUserLogins** | 外部登录关联表（如微信、Google） |
| **AspNetUserTokens** | 令牌表（密码重置令牌等） |

## 四、JWT 令牌服务

### 4.1 JWTOptions 配置模型

```csharp
public class JWTOptions
{
    public string Issuer { get; set; }        // 签发者
    public string Audience { get; set; }      // 接收者
    public string Key { get; set; }           // 签名密钥（敏感信息！）
    public int ExpireSeconds { get; set; }    // 过期时间（秒）
}
```

对应 `appsettings.json` 配置：

```json
{
  "JWT": {
    "Issuer": "my",
    "Audience": "my",
    "Key": "密钥",
    "ExpireSeconds": 86400
  }
}
```

### 4.2 TokenService：签发 JWT

```csharp
public class TokenService
{
    public static string BuildToken(IEnumerable<Claim> claims, JWTOptions options)
    {
        TimeSpan ExpiryDuration = TimeSpan.FromSeconds(options.ExpireSeconds);
        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(options.Key));
        var credentials = new SigningCredentials(
            securityKey, SecurityAlgorithms.HmacSha256Signature);
        var tokenDescriptor = new JwtSecurityToken(
            options.Issuer,
            options.Audience,
            claims,
            expires: DateTime.UtcNow.Add(ExpiryDuration),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
    }
}
```

**生成 Token 的过程中发生了什么：**

```
Claims（用户标识 + 角色）
    │
    ▼
JwtSecurityToken
  ├─ Issuer: "my"
  ├─ Audience: "my"
  ├─ Subject: Claims (NameIdentifier=用户ID, Role=admin)
  ├─ Expiration: UTC 当前时间 + 86400秒（24小时）
  └─ SigningCredentials: HMAC-SHA256(密钥)
    │
    ▼
JwtSecurityTokenHandler.WriteToken()
    │
    ▼
JWT 字符串：eyJhbGciOiJIUzI1NiIs...
```

JWT 的三个组成部分：

```
header.payload.signature

header:  {"alg":"HS256","typ":"JWT"}
payload: {"sub":"用户ID","role":"admin","exp":"...","iss":"my","aud":"my"}
signature: HMACSHA256(base64(header)+"."+base64(payload), 密钥)
```

## 五、JWT 认证配置

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        var jwtSetting = builder.Configuration.GetSection("JWT").Get<JWTOptions>();

        opt.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuer = true,               // 验证签发者
            ValidateAudience = true,             // 验证接收者
            ValidateLifetime = true,             // 验证过期时间
            ValidateIssuerSigningKey = true,     // 验证签名密钥
            ValidIssuer = jwtSetting.Issuer,
            ValidAudience = jwtSetting.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSetting.Key))
        };
    });
```

**四个验证开关全部开启，意味着：**

1. Token 必须是"我"签发的（Issuer 匹配）
2. Token 必须发给"我"的（Audience 匹配）
3. Token 没有过期（Lifetime 验证）
4. Token 没有被篡改（签名验证）

如果任何一个不通过，ASP.NET Core 自动返回 `401 Unauthorized`，无需手动写验证代码。

## 六、ASP.NET Core Identity 配置

```csharp
builder.Services.AddIdentityCore<User>(options =>
{
    // 简化密码策略（内部系统可适当放宽）
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;

    options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;
    options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
});

IdentityBuilder identityBuilder = new IdentityBuilder(typeof(User), typeof(Role), builder.Services);
identityBuilder.AddEntityFrameworkStores<IdDbContext>();
identityBuilder.AddDefaultTokenProviders();
identityBuilder.AddUserManager<IdUserManager>();
identityBuilder.AddRoleManager<RoleManager<Role>>();
```

**这里用了 `AddIdentityCore` 而不是 `AddIdentity`：**

| 方法 | 特点 |
|------|------|
| `AddIdentity` | 一体式注册，自动配置 Cookie + 默认 UI，适合 MVC 项目 |
| `AddIdentityCore` | 最小化注册，只注册核心服务，适合 Web API + JWT 场景 |

**密码策略**：内部管理后台，密码规则比较宽松——6 位以上即可，不要求大小写/数字/特殊字符。如果是面向用户的产品，建议收紧。

## 七、登录流程详解

### 7.1 控制器入口

```csharp
[HttpPost("Login")]
public async Task<ActionResult> Login(LoginRequest request)
{
    var (result, token) = await _idService.LoginByUserNameAndPwdAsync(
        request.UserName, request.Password);

    if (result.Succeeded)
        return Ok(token);

    if (result.IsLockedOut)
        return StatusCode(423, "账号已被锁定");

    return BadRequest("用户名或密码错误");
}
```

### 7.2 IdService：登录逻辑

```
LoginByUserNameAndPwdAsync(userName, password)
    │
    ├─ UserManager.FindByNameAsync(userName)
    │    └─ 用户不存在 → 返回 Failed（不提示"用户不存在"，防枚举攻击）
    │
    ├─ IdentityRepository.CheckForSignInAsync(user, password, lockoutOnFailure: true)
    │    ├─ UserManager.IsLockedOutAsync() → 已锁定 → 返回 LockedOut
    │    ├─ UserManager.CheckPasswordAsync() → 密码错误
    │    │    └─ UserManager.AccessFailedAsync() → 增加失败次数
    │    │         └─ 达到阈值 → 自动锁定账号
    │    └─ 密码正确 → UserManager.ResetAccessFailedCountAsync() → 成功
    │
    ├─ 登录成功 → BuildTokenAsync()
    │    ├─ UserManager.GetRolesAsync(user) → 获取用户角色
    │    ├─ 构建 Claims: [NameIdentifier=用户ID, Role=admin, ...]
    │    └─ TokenService.BuildToken(claims, jwtOptions) → 返回 JWT
    │
    └─ 返回 (SignInResult, JWT字符串)
```

**安全设计亮点：**

| 措施 | 实现 |
|------|------|
| 用户不存在时不提示 | 对前端返回统一的"用户名或密码错误" |
| 登录失败锁定 | `AccessFailedAsync()` 自动增加失败次数，达到 `MaxFailedAccessAttempts`（默认 5 次）后锁定 |
| 登录成功重置计数 | `ResetAccessFailedCountAsync()` 防止攻击者"试到锁定，等解锁再试" |
| Token 有时效 | 24 小时过期，过期后需重新登录 |

### 7.3 签发 Token 的 Claims

```csharp
private async Task<string> BuildToenkAsync(User user)
{
    var roles = await _userManager.GetRolesAsync(user);
    List<Claim> claims = new List<Claim>();
    claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
    foreach (var role in roles)
    {
        claims.Add(new Claim(ClaimTypes.Role, role));
    }
    return TokenService.BuildToken(claims, _jwtOptions.Value);
}
```

Token 中包含两类信息：
- **`ClaimTypes.NameIdentifier`**：用户 ID，后续接口可以用它识别当前用户
- **`ClaimTypes.Role`**：用户角色，用于 `[Authorize(Roles = "admin")]` 授权检查

## 八、授权：角色保护

```csharp
// 只有 admin 角色才能访问
[HttpGet("FindAllUsers")]
[Authorize(Roles = "admin")]
public async Task<ActionResult> FindAllUsers()
{
    // ...
}
```

ASP.NET Core 的授权中间件会自动解析 JWT 中的 `role` claim，与 `[Authorize(Roles = "admin")]` 进行匹配：

```
请求 → JWT 认证中间件
  ├─ 验证 Token 有效 → 构造 ClaimsPrincipal（含 role=admin）
  ├─ 进入 Controller 管道
  │    └─ [Authorize(Roles="admin")] 过滤器
  │         ├─ 用户有 admin 角色 → ✅ 通过
  │         └─ 用户没有 admin 角色 → ❌ 403 Forbidden
```

## 九、Admin 初始种子数据

```csharp
static async Task SeedAdminUser(IdUserManager userManager, RoleManager<Role> roleManager)
{
    var adminpassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");

    if (!await roleManager.RoleExistsAsync("admin"))
    {
        await roleManager.CreateAsync(new Role() { Name = "admin" });
    }

    if (await userManager.FindByNameAsync("admin") == null)
    {
        var adminUser = new User("admin");
        var result = await userManager.CreateAsync(adminUser, adminpassword);
        await userManager.AddToRoleAsync(adminUser, "admin");
    }
}
```

首次启动时自动创建 admin 角色和 admin 用户。密码通过环境变量 `ADMIN_PASSWORD` 传入，**不硬编码在代码或配置文件中**。

`launchSettings.json` 中配置：

```json
{
  "environmentVariables": {
    "ADMIN_PASSWORD": "密码"
  }
}
```

> ⚠️ 开发环境用简单密码方便调试，生产环境务必使用强密码。

## 十、完整的数据流

```
                 登录                          访问受保护接口
                  │                                │
                  ▼                                ▼
Client       POST /api/Identity/Login       GET /api/User/FindAllUsers
             {                                Authorization: Bearer eyJ...
               userName: "admin",
               password: "xxx"
             }
                  │                                │
                  ▼                                ▼
Controller   IdentityController              UserController
             .Login()                        .FindAllUsers()
                  │                                │
                  ▼                                ▼
Service      IdService                       [Authorize(Roles="admin")]
             .LoginByUserNameAndPwdAsync()   JWT 中间件解析 Token
                  │                          ├─ 验证签名 ✅
                  ▼                          ├─ 验证未过期 ✅
             UserManager                     ├─ 验证 Issuer/Audience ✅
             .FindByNameAsync()              └─ 提取 role claim → admin ✅
             .CheckPasswordAsync()                  │
                  │                                 ▼
                  ▼                           返回用户列表
             验证成功
                  │
                  ▼
             TokenService
             .BuildToken(claims, options)
                  │
                  ▼
             返回 JWT ←─────────────────── Client 保存 Token
                                           (localStorage/内存)
```

## 十一、最佳实践与避坑

### 11.1 密钥管理

```csharp
// ❌ 不要硬编码在代码里
private const string Key = "密钥";

// ✅ 推荐：使用配置 + 环境变量
// appsettings.Production.json 中不写 Key
// 通过环境变量或者密钥管理服务注入
```

JWT 密钥一旦泄露，任何人都可以伪造 Token。生产环境建议：
- 使用 256 位以上随机密钥
- 通过环境变量或 Azure Key Vault / 阿里云 KMS 管理
- 定期轮换密钥

### 11.2 Token 过期策略

当前设置为 24 小时（86400 秒）：

```
expires: DateTime.UtcNow.Add(ExpiryDuration)
```

- **短期 Token（15-30 分钟）**：更安全，但用户需要频繁重新登录
- **长期 Token（7-30 天）**：用户体验好，但泄露后风险窗口大
- **折中方案**：Access Token（短）+ Refresh Token（长），用刷新令牌延长会话

### 11.3 密码策略

当前配置较为宽松，适合内部管理系统。面向用户的系统建议：

```csharp
options.Password.RequiredLength = 8;
options.Password.RequireDigit = true;
options.Password.RequireLowercase = true;
options.Password.RequireUppercase = true;
options.Password.RequireNonAlphanumeric = true;
options.Password.RequiredUniqueChars = 4;   // 至少 4 个不同字符
```

### 11.4 中间件顺序很重要

```csharp
app.UseCors();                    // ① CORS 要放在最前面
app.UseGlobalExceptionHandler(); // ② 全局异常处理
app.UseAuthentication();         // ③ 认证
app.UseAuthorization();          // ④ 授权（必须在认证之后）
app.MapControllers();            // ⑤ 路由
```

顺序错了，认证/授权可能不生效。

## 十二、总结

| 主题 | 要点 |
|------|------|
| **认证方式** | JWT Bearer Token，无状态，适合分布式/微服务 |
| **用户管理** | ASP.NET Core Identity，继承 `IdentityUser<Guid>` |
| **软删除** | 逻辑删除用户，配合全局过滤器和过滤索引 |
| **密码保护** | `LockoutOnFailure` 自动锁定，防暴力破解 |
| **角色授权** | `[Authorize(Roles = "admin")]` 声明式控制 |
| **Token 签发** | HMAC-SHA256 签名，24 小时过期 |
| **种子数据** | 首次启动自动创建管理员 |

整个身份认证系统从数据模型 → Token 签发 → 认证验证 → 授权拦截，构成了一个完整的闭环。虽然项目名叫 IdentitServer，但并没有使用 IdentityServer4——而是用 ASP.NET Core Identity + JWT 实现了轻量级的自建认证体系，足够应对大多数内部系统和管理后台的场景。
