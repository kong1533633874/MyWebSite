# ASP.NET Core 数据持久化：Entity Framework Core 项目实践

## 一、前言

在任何一个后端系统中，数据持久化都是最基础也最关键的一环。选择 ORM 框架时，大多数 .NET 团队会问三个问题：

1. **开发效率高不高？** —— 写 CRUD 要多少代码？
2. **性能行不行？** —— 会不会有 N+1 查询？
3. **能不能驾驭复杂查询？** —— 多表关联、分组聚合、事务？

**Entity Framework Core（EF Core）** 是微软官方的 ORM 框架，也是 .NET 生态的默认选择。它提供 LINQ 查询、自动迁移、延迟加载、全局过滤器等丰富功能。

本文基于项目中的真实代码，讲解 EF Core 在 .NET 8 微服务架构中的落地实践。

## 二、整体架构

项目采用"一个 Bounded Context 一个 DbContext"的模式：

```
WPEnglish/
├── IdentityServer.Infrastructure/     ← 身份认证上下文
│   ├── IdDbContext.cs                 ← IdentityDbContext<User, Role, Guid>
│   ├── config/                        ← UserConfig, RoleConfig
│   └── Migrations/                    ← 3 个迁移
│
├── Listening.Infrastructure/          ← 听力内容上下文
│   ├── ListengingDbContext.cs         ← DbContext（3 个 DbSet）
│   ├── Configs/                       ← CategoryConfig, AlbumConfig, EpisodeConfig
│   └── Migrations/                    ← 2 个迁移
│
├── FileService.Infrastructure/        ← 文件存储上下文
│   ├── FSDbContext.cs                 ← DbContext（1 个 DbSet）
│   ├── Configs/                       ← AudioFileConfig
│   └── Migrations/                    ← 3 个迁移
│
└── Infrastructure/                    ← 共享 EFCore 工具
    └── EFCore/EFCoreExtensions.cs     ← 软删除全局过滤器扩展（未使用）
```

每个 DbContext 对应**独立的数据库**（目前共用同一个 Catalog，但设计上是隔离的），各自维护自己的迁移历史。

## 三、DbContext 配置

### 3.1 基础 DbContext

以 `ListengingDbContext` 为例：

```csharp
public class ListengingDbContext : DbContext
{
    public DbSet<Category> categories { get; set; }
    public DbSet<Album> albums { get; set; }
    public DbSet<Episode> episodes { get; set; }

    public ListengingDbContext(DbContextOptions<ListengingDbContext> option) : base(option)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(this.GetType().Assembly);
        
        // 软删除全局过滤器
        modelBuilder.Entity<Category>().HasQueryFilter(s => !s.IsDeleted);
        modelBuilder.Entity<Album>().HasQueryFilter(s => !s.IsDeleted);
        modelBuilder.Entity<Episode>().HasQueryFilter(s => !s.IsDeleted);

        // 所有 DateTime 列精确到秒
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
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

`OnModelCreating` 中的三件事：

| 代码 | 作用 |
|------|------|
| `ApplyConfigurationsFromAssembly` | 自动扫描并应用程序集中的所有 `IEntityTypeConfiguration<T>` |
| `HasQueryFilter` | 软删除全局过滤器，所有查询自动加 `WHERE IsDeleted = 0` |
| `datetime2(0)` 循环 | 统一 DateTime 列的精度为秒级，省去每个属性单独配置 |

### 3.2 Identity 特殊 DbContext

```csharp
public class IdDbContext : IdentityDbContext<User, Role, Guid>
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(this.GetType().Assembly);

        // 软删除过滤
        builder.Entity<User>().HasQueryFilter(s => !s.IsDeleted);

        // 软删除感知的唯一索引
        builder.Entity<User>().HasIndex(u => u.NormalizedUserName)
            .HasFilter("[IsDeleted] = 0")
            .IsUnique();

        // datetime2(0) 循环
        // ...
    }
}
```

**继承 `IdentityDbContext<User, Role, Guid>` 后自动获得什么？**

```
AspNetUsers       → 用户表
AspNetRoles       → 角色表
AspNetUserRoles   → 用户-角色关联表
AspNetUserClaims  → 用户声明
AspNetRoleClaims  → 角色声明
AspNetUserLogins  → 外部登录
AspNetUserTokens  → 令牌
```

这 7 张表由 Identity 框架自动管理，无需手动定义 DbSet 和配置。

## 四、实体配置：Fluent API vs Data Annotation

EF Core 提供两种配置方式：

| 方式 | 优点 | 缺点 |
|------|------|------|
| **Data Annotation**（特性） | 写在实体类上，直观 | 污染领域模型，无法处理高级映射 |
| **Fluent API**（IEntityTypeConfiguration） | 配置与实体分离，功能强大 | 需要额外文件 |

项目采用了 **Fluent API** 方式，每个实体一个配置类：

```csharp
public class CategoryConfig : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("T_Categories");
        builder.HasKey(c => c.Id).IsClustered(false);  // 非聚集索引主键
        builder.Property(s => s.CoverUrl).IsRequired(false).IsUnicode();
        builder.Property(s => s.Title).IsRequired().HasMaxLength(200).IsUnicode();
    }
}
```

```csharp
public class AlbumConfig : IEntityTypeConfiguration<Album>
{
    public void Configure(EntityTypeBuilder<Album> builder)
    {
        builder.ToTable("T_Albums");
        builder.HasKey(x => x.Id).IsClustered();       // 聚集索引主键
        builder.Property(s => s.Title).IsRequired().IsUnicode();
        builder.HasIndex(x => new { x.CategoryId, x.IsDeleted });  // 复合索引
    }
}
```

```csharp
public class EpisodeConfig : IEntityTypeConfiguration<Episode>
{
    public void Configure(EntityTypeBuilder<Episode> builder)
    {
        builder.ToTable("T_Episodes");
        builder.HasKey(x => x.Id).IsClustered();
        builder.Property(s => s.AudioUrl).HasMaxLength(1000);
        builder.Property(s => s.Subtitle).HasColumnType("nvarchar(max)");
        builder.Property(s => s.SubtitleType).HasMaxLength(10)
            .IsUnicode(false);        // varchar，非 nvarchar
        builder.HasIndex(x => new { x.AlbumId, x.IsDeleted });
    }
}
```

### 配置要点总结

| 配置 | 说明 | 示例 |
|------|------|------|
| `ToTable("T_xxx")` | 指定表名，统一前缀 | `T_Categories`、`T_Albums` |
| `HasKey(x => x.Id).IsClustered(bool)` | 控制聚集索引 | Category 用非聚集（适合 Guid 主键） |
| `HasMaxLength(200)` | 限制字符串长度 | `Title` 最长 200 |
| `IsRequired()` | 非空约束 | `Title` 必填 |
| `IsUnicode()` / `IsUnicode(false)` | nvarchar vs varchar | `SubtitleType` 用 varchar（无中文） |
| `HasColumnType("nvarchar(max)")` | 直接指定 SQL 类型 | 大文本字段 |
| `HasIndex(...)` | 创建索引 | 复合索引 `(CategoryId, IsDeleted)` 加速按分类查询 |
| `IsClustered(false)` | 非聚集主键 | Guid 主键用非聚集可以减少页分裂 |

**为什么 Category 用非聚集主键，Album 和 Episode 用聚集？**

在 SQL Server 中，聚集索引决定了数据的物理存储顺序。Guid 主键是随机的，如果使用聚集索引，每次插入新行都可能导致页分裂。因此 Category 使用 `IsClustered(false)`，将 Guid 主键设为非聚集索引。而 Album 和 Episode 如果主键是递增的或对顺序不敏感，使用聚集索引更高效。

## 五、软删除的两种实现方式

项目中软删除的需求很明确：删除用户/分类/专辑/音频时，不真删数据，只标记 `IsDeleted = true`。

### 5.1 方式一：手动配置（项目中实际使用的）

在每个 DbContext 中逐实体添加：

```csharp
modelBuilder.Entity<Category>().HasQueryFilter(s => !s.IsDeleted);
modelBuilder.Entity<Album>().HasQueryFilter(s => !s.IsDeleted);
modelBuilder.Entity<Episode>().HasQueryFilter(s => !s.IsDeleted);
```

效果：所有 LINQ 查询自动附加 `WHERE IsDeleted = 0`：

```csharp
// 实际执行的 SQL
var categories = await _dbContext.categories.ToListAsync();
// SELECT * FROM T_Categories WHERE IsDeleted = 0
```

但如果想查询已删除的数据，需要用 `IgnoreQueryFilters()`：

```csharp
var deletedCategories = await _dbContext.categories
    .IgnoreQueryFilters()
    .Where(c => c.IsDeleted)
    .ToListAsync();
```

### 5.2 方式二：通用扩展（项目中存在但未使用）

项目中有一个 `EFCoreExtensions` 提供了自动应用软删除过滤器的通用方法：

```csharp
public static void EnableSoftDeletionGlobalFilter(this ModelBuilder modelBuilder)
{
    var entityTypesHasSoftDeletion = modelBuilder.Model.GetEntityTypes()
        .Where(e => e.ClrType.IsAssignableTo(typeof(ISoftDelete)));

    foreach (var entityType in entityTypesHasSoftDeletion)
    {
        var isDeletedProperty = entityType.FindProperty(nameof(ISoftDelete.IsDeleted));
        var parameter = Expression.Parameter(entityType.ClrType, "p");
        var filter = Expression.Lambda(
            Expression.Not(Expression.Property(parameter, isDeletedProperty.PropertyInfo)),
            parameter);
        entityType.SetQueryFilter(filter);
    }
}
```

如果启用这个扩展，只需一行：

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.EnableSoftDeletionGlobalFilter();  // 自动为所有 ISoftDelete 实体添加过滤器
    // ...
}
```

结合 `ApplyConfigurationsFromAssembly`，甚至可以实现"新增一个实体 → 实现 ISoftDelete → 自动全局过滤"，完全零配置。

但目前项目中选择了手动方式——虽然每个实体要写一行，但意图更明确，也更容易追踪。

### 5.3 软删除感知的唯一索引

用户名字段需要唯一，但软删除后允许同名用户注册。普通唯一索引会阻止这个操作。

解决方案是**过滤索引**：

```csharp
builder.Entity<User>().HasIndex(u => u.NormalizedUserName)
    .HasFilter("[IsDeleted] = 0")
    .IsUnique();
```

这个索引只对 `IsDeleted = 0` 的行强制唯一，已删除的用户名可以被重新使用。

## 六、数据库迁移

### 6.1 迁移生命周期

```
① 修改实体 / 配置类
        ↓
② dotnet ef migrations add <名称>
        ↓
③ 审查生成的 Migration 文件
        ↓
④ dotnet ef database update
        ↓
⑤ 部署到生产环境
```

### 6.2 项目中的迁移历史

每个 Infrastructure 项目的迁移独立维护：

**Listening（听力内容）—— 2 个迁移：**

| 迁移 | 内容 |
|------|------|
| `init` | 创建 T_Categories、T_Albums、T_Episodes 三张表，含索引 |
| `changeDatetime` | 将所有 DateTime 列改为 `datetime2(0)` |

**IdentityServer（身份认证）—— 3 个迁移：**

| 迁移 | 内容 |
|------|------|
| `init` | 创建 Identity 全套 7 张表，含软删除字段 |
| `changeDatetime` | DateTime 列改为 `datetime2(0)` |
| `changeUsernameIndex` | 重建用户名字段为过滤索引，支持软删除后重用用户名 |

**FileService（文件存储）—— 3 个迁移：**

| 迁移 | 内容 |
|------|------|
| `init` | 创建 AudioFiles 表，含文件名哈希索引 |
| `updateDatetime` | 空迁移（占位） |
| `changeDatetime` | DateTime 列改为 `datetime2(0)` |

有趣的是：三个 Bounded Context 都在差不多的时间加了 `changeDatetime` 迁移——因为开发中后期才意识到默认的 `datetime2(7)` 精度太高，统一改为秒级。

### 6.3 发布 SQL 脚本

生成环境不使用 `dotnet ef database update`，而是生成 SQL 脚本由 DBA 执行：

```
publish/
├── migration_fileservice.sql
├── migration_identity.sql
└── migration_listening.sql
```

这符合企业级部署规范：迁移脚本经过审查后再执行到生产数据库。

## 七、注册方式

```csharp
builder.Services.AddDbContext<ListengingDbContext>(opt =>
{
    string connStr = builder.Configuration.GetSection("connStr").Value;
    opt.UseSqlServer(connStr, option =>
    {
        option.EnableRetryOnFailure(maxRetryCount: 3);
    });
});
```

**配置要点：**

| 参数 | 值 | 作用 |
|------|-----|------|
| Connection String | `appsettings.*.json` 的 `connStr` 节 | 开发/生产环境不同的连接串 |
| `EnableRetryOnFailure(3)` | 最多重试 3 次 | 处理 Azure SQL 等云数据库的瞬时故障 |

**为什么不需要 UseLoggerFactory？**

EF Core 默认通过 `ILoggerFactory` 输出 SQL 日志，而项目中 Serilog 已接管了所有日志管道。所以 `_logger.LogInformation("SQL: ...")` 会自动流向 Serilog 的文件和控制台 sink，无需额外配置。

## 八、EF Core 在项目中的使用模式

### 8.1 仓储 + DbContext

项目在 Infrastructure 层实现了 Repository 接口，其内部使用 DbContext：

```csharp
public class ListeningRepository : IListeningRepository
{
    private readonly ListengingDbContext _dbContext;

    public ListeningRepository(ListengingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Category?> GetCategoryByIdAsync(Guid id)
    {
        return await _dbContext.categories.FindAsync(id);
    }

    public async Task<int> GetMaxSequenceNumberAsync()
    {
        return await _dbContext.categories.MaxAsync(c => (int?)c.SequenceNumber) ?? 0;
    }
}
```

### 8.2 生命周期管理

`AddDbContext` 默认注册为 **Scoped**（作用域模式）：

```
请求到达 → 创建 Controller（Scoped）
                 ↓
         注入 ListeningService（Scoped）
                 ↓
         注入 IListeningRepository（Scoped）
                 ↓
         注入 ListengingDbContext（Scoped）
                 ↓
         同一个请求内，所有操作共享同一个 DbContext 实例
请求结束 → 释放 DbContext
```

这意味着同一个 HTTP 请求内对实体的修改会自动跟踪，`SaveChangesAsync` 时一次性提交。

### 8.3 SaveChanges 的位置

有意思的是，项目中 **SaveChanges 不在仓储中调用**，而是在 Controller 中：

```csharp
// Controller
var category = await _service.AddCategoryAsync(request.Title, request.CoverUrl);
// category 是一个新创建的实体对象
await _dbContext.categories.AddAsync(category);
await _dbContext.SaveChangesAsync();
```

这里有一个混合模式：领域服务创建实体 → Controller 负责持久化。这样领域服务保持了对持久化的无感知，但 Controller 层承担了事务管理的职责。

## 九、NuGet 包

```xml
<!-- 基础包（每个 Infrastructure 项目） -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.26" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.26" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.26" />

<!-- 迁移设计时（仅 WebApi 项目需要） -->
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.27" />

<!-- Identity 扩展（仅 IdentityServer 需要） -->
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.0.27" />
```

| 包 | 作用 |
|----|------|
| **EntityFrameworkCore** | 核心 ORM 引擎 |
| **SqlServer** | SQL Server 数据库提供程序 |
| **Tools** | `dotnet ef` CLI 迁移命令 |
| **Design** | 迁移设计时工厂（`IDesignTimeDbContextFactory`） |
| **Identity.EntityFrameworkCore** | Identity 与 EF Core 的集成 |

## 十、最佳实践与避坑

### 10.1 Guid 主键的聚集索引选择

Guid 作为主键时，如果用聚集索引（默认就是聚集），每次插入都会导致索引页分裂，影响写性能。

```csharp
// ❌ 默认：Guid 主键 + 聚集索引 → 页分裂频繁
builder.HasKey(c => c.Id);  // 默认 IsClustered = true

// ✅ 推荐：Guid 主键 + 非聚集索引
builder.HasKey(c => c.Id).IsClustered(false);

// 再建一个业务相关的聚集索引（如自增的 CreateTime）
builder.HasIndex(c => c.CreateTime).IsClustered();
```

### 10.2 DateTime 精度

EF Core 默认用 `datetime2(7)`，保留 7 位小数毫秒。绝大多数业务场景只需要精确到秒。

```csharp
// 全局统一配置
foreach (var entityType in modelBuilder.Model.GetEntityTypes())
{
    foreach (var prop in entityType.GetProperties()
        .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)))
    {
        prop.SetColumnType("datetime2(0)");
    }
}
```

### 10.3 追踪 vs 非追踪

```csharp
// 读（不修改）：用 AsNoTracking 提高性能
var categories = await _dbContext.categories.AsNoTracking().ToListAsync();

// 写（需要更新）：用默认追踪，EF Core 自动检测变化
var category = await _dbContext.categories.FindAsync(id);
category.ChangeTitle(newTitle);
await _dbContext.SaveChangesAsync();  // 自动生成 UPDATE
```

`FindAsync` 返回的是追踪实体，修改属性后 `SaveChangesAsync` 会自动生成 UPDATE 语句。

### 10.4 迁移的命名规范

```csharp
// ✅ 有意义的名称
dotnet ef migrations add Init
dotnet ef migrations add ChangeDatetimeToSecondPrecision
dotnet ef migrations add AddSoftDeleteUniqueIndexToUserName

// ❌ 无意义的名称
dotnet ef migrations add Test1
dotnet ef migrations add Fix
```

有意义的迁移名称让团队成员不用查看代码就知道迁移改了什么。

## 十一、总结

| 主题 | 要点 |
|------|------|
| **架构模式** | 每个 Bounded Context 一个独立 DbContext，各自维护迁移 |
| **Fluent API** | 使用 `IEntityTypeConfiguration<T>` 分离配置与实体 |
| **软删除** | `HasQueryFilter` 全局过滤 + 过滤索引支持唯一约束 |
| **DateTime 精度** | 全局循环统一为 `datetime2(0)`，精确到秒 |
| **迁移管理** | 每个 Infrastructure 项目独立迁移，生产用 SQL 脚本部署 |
| **重试策略** | `EnableRetryOnFailure(3)` 处理云数据库瞬时故障 |
| **生命周期** | Scoped 模式，请求内共享 DbContext 实例 |

EF Core 在项目中的定位非常务实——没有使用高级特性（如拦截器、值转换器、 OwnsOne 等），而是把基础功能用扎实：良好的表映射、合理的索引、全局过滤器、干净的迁移。对于大多数 .NET 项目来说，这就足够支撑业务了。
