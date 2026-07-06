# 领域驱动设计实战：.NET 项目中的 DDD 实践

## 一、前言

当你接手一个有一定规模的后端项目时，可能会遇到这样的问题：

- 业务逻辑散落在 Controller 和 Service 中，改一个需求要翻 5 个文件
- 实体类只有 get/set 属性，没有任何行为——"贫血模型"
- 数据库表结构直接暴露给上层，换个数据库要改一堆代码
- 团队对"用户"的理解不一致：订单系统叫他 `Buyer`，物流系统叫他 `Receiver`，CRM 系统叫他 `Customer`

**领域驱动设计（Domain-Driven Design，DDD）** 就是为解决这些问题而生。它不是一套框架或工具，而是一套**设计思想**——让软件的复杂性与业务领域的复杂性相匹配。

## 二、DDD 的核心概念

### 分层架构

DDD 经典的四层架构：

```
┌─────────────────────────────────────┐
│         Presentation（Controller）    │  → 接收请求，返回响应
├─────────────────────────────────────┤
│          Application（应用服务）       │  → 编排用例，事务管理
├─────────────────────────────────────┤
│           Domain（领域层）             │  → 核心业务逻辑 ← 重点
├─────────────────────────────────────┤
│        Infrastructure（基础设施）      │  → EF Core、文件存储、第三方 API
└─────────────────────────────────────┘
```

**核心原则**：Domain 层是圆心，不依赖任何其他层；Infrastructure 依赖 Domain（依赖倒置）。

### 战术模式

| 模式 | 作用 |
|------|------|
| **Entity（实体）** | 有唯一标识、有生命周期、有行为 |
| **Value Object（值对象）** | 无标识、不可变、描述性 |
| **Aggregate（聚合）** | 实体和值对象的边界，保证一致性 |
| **Aggregate Root（聚合根）** | 聚合的入口，外部只能通过聚合根操作聚合 |
| **Repository（仓储）** | 从基础设施视角看，像是"内存中的集合" |
| **Domain Service（领域服务）** | 不属于任何实体/值对象的业务逻辑 |
| **Domain Event（领域事件）** | 领域内发生的重要事情，其他部分可订阅 |

## 三、项目中的 DDD 落地实践

### 整体架构

```
WPEnglish/
├── DomainCommons/                  ← 共享内核（DDD 基础抽象）
├── IdentityServer.Domain/          ← 身份认证上下文
├── IdentityServer.Infrastructure/  ← 身份认证持久化
├── Listening.Domain/               ← 听力内容上下文（最典型的 DDD 实践）
├── Listening.Infrastructure/       ← 听力内容持久化
├── FileService.Domain/             ← 文件存储上下文
├── FileService.Infrastructure/     ← 文件存储持久化
└── 各 WebApi 项目                  ← 表现层（Controller）
```

项目按**业务边界**拆分为多个 Bounded Context（限界上下文），每个上下文有自己的 Domain 和 Infrastructure。

### 3.1 充血模型：让实体有行为

贫血模型 vs 充血模型是 DDD 中最基本的分水岭。

**❌ 贫血模型（Anti-pattern）：**

```csharp
public class Category
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public int SequenceNumber { get; set; }
    public bool IsDeleted { get; set; }
}
```

谁都可以 `category.Title = "新标题"`，没有任何约束。

**✅ 充血模型（项目中的做法）：**

```csharp
public class Category
{
    public Guid Id { get; private set; }
    public int SequenceNumber { get; private set; }
    public string Title { get; private set; }
    public Uri CoverUrl { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime CreateTime { get; private set; }
    public DateTime? DeletionTime { get; private set; }
    public DateTime? LastModificationTime { get; private set; }

    // ① 静态工厂方法
    public static Category Create(int sequenceNumber, string title, Uri coverUrl)
    {
        return new Category
        {
            Id = Guid.NewGuid(),
            SequenceNumber = sequenceNumber,
            Title = title,
            CoverUrl = coverUrl,
            CreateTime = DateTime.UtcNow,
        };
    }

    // ② 行为方法（有业务语义，而不是 setter）
    public Category ChangeTitle(string title)
    {
        this.Title = title;
        return this;
    }

    public Category ChangeCoverUrl(Uri url)
    {
        this.CoverUrl = url;
        return this;
    }

    // ③ 软删除
    public void SoftDelete()
    {
        this.IsDeleted = true;
        this.DeletionTime = DateTime.UtcNow;
    }

    // ④ 通知修改
    public void NotifyModified()
    {
        this.LastModificationTime = DateTime.UtcNow;
    }
}
```

**关键区别：**

| 贫血模型 | 充血模型 |
|---------|---------|
| `private set` 都没有，属性完全公开 | `private set` 封装，只能通过方法修改 |
| 行为写在外部 Service 中 | 行为写在实体自身 |
| `category.Title = "xxx"` | `category.ChangeTitle("xxx")` |
| 无法保证业务约束 | 方法内部可做校验 |
| 可以随意 new() | 只能用工厂方法 `Create()` |

### 3.2 静态工厂方法 vs 直接 new

```csharp
// ❌ 外部随意 new，容易遗漏字段
var category = new Category
{
    Id = Guid.NewGuid(),
    Title = title
    // 忘了设 CreateTime、SequenceNumber
};

// ✅ 工厂方法封装创建逻辑，保证完整性
var category = Category.Create(seq + 1, title, url);
```

工厂方法的优势：
- **强制完整**：构造函数保证所有必要字段被赋值
- **封装创建逻辑**：`Id = Guid.NewGuid()`、`CreateTime = DateTime.UtcNow` 等基础设施由实体自己处理
- **统一入口**：修改创建逻辑只需改一处

### 3.3 Builder 模式：解决复杂构造

当实体有大量可选/必选字段时，构造函数参数列表会变得很长。`Episode` 实体用 Builder 模式解决：

```csharp
var episode = new Episode.Builder()
    .Id(id)
    .Title(title)
    .AlbumId(albumId)
    .AudioUrl(audioUrl)
    .DurationInSecond(duration)
    .SubtitleType(subtitleType)
    .Subtitle(subtitle)
    .SequenceNumber(seq)
    .Build();
```

Builder 在 `Build()` 方法中进行**完整性校验**：

```csharp
public Episode Build()
{
    if (id == Guid.Empty)
        throw new ArgumentOutOfRangeException(nameof(id));
    if (title == null)
        throw new ArgumentNullException(nameof(title));
    if (audioUrl == null)
        throw new ArgumentNullException(nameof(audioUrl));
    if (durationInSecond <= 0)
        throw new ArgumentOutOfRangeException(nameof(durationInSecond));
    // ...

    return new Episode { /* 赋值 */ };
}
```

这样 "先设置参数，最后一次性构建" 的流程，既避免了长参数列表，又保证了对象创建时的完整性。

### 3.4 仓储模式：抽象持久化

仓储（Repository）在 DDD 中的定义是："从上层看，像一个内存中的集合；从下层看，封装了数据库访问"。

```csharp
// Domain 层定义接口（依赖倒置）
public interface IListeningRepository
{
    // Category
    Task<Category?> GetCategoryByIdAsync(Guid id);
    Task<Category[]> GetAllCategoriesAsync();
    Task<int> GetMaxSequenceNumberAsync();

    // Album
    Task<Album?> GetAlbumByIdAsync(Guid id);
    Task<Album[]> GetAllAlbumsByCategoryIdAsync(Guid categoryId);
    Task<int> GetMaxSequenceNumberOfAlbumAsync(Guid categoryId);

    // Episode
    Task<Episode?> GetEpisodeByIdAsync(Guid id);
    Task<Episode[]> GetAllEpisodesByAlbumIdAsync(Guid albumId);
    Task<int> GetMaxSequenceNumberOfEpisodeAsync(Guid albumId);
}
```

```csharp
// Infrastructure 层实现
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

    public async Task<Category[]> GetAllCategoriesAsync()
    {
        return await _dbContext.categories.ToArrayAsync();
    }

    public async Task<int> GetMaxSequenceNumberAsync()
    {
        return await _dbContext.categories.MaxAsync(c => (int?)c.SequenceNumber) ?? 0;
    }
    // ...
}
```

**仓储模式带来了什么：**

| 好处 | 说明 |
|------|------|
| **解耦** | Domain 层完全不知道 EF Core、Dapper 还是 MongoDB |
| **可测试** | 可以 Mock 仓储接口，单元测试领域逻辑 |
| **领域友好的接口** | `GetMaxSequenceNumberOfAlbumAsync` 比 `db.Albums.Where(x => ...).MaxAsync(...)` 语义更清晰 |
| **变更隔离** | 换数据库只需重写仓储实现，Domain 层不动 |

### 3.5 领域服务：编排复杂的业务逻辑

当业务逻辑涉及多个实体或需要调用仓储时，不属于任何一个实体，就放在领域服务中。

```csharp
public class ListeningService
{
    private readonly IListeningRepository repository;
    private readonly ILogger<ListeningService> logger;

    public ListeningService(IListeningRepository repository, ILogger<ListeningService> logger)
    {
        this.repository = repository;
        this.logger = logger;
    }

    public async Task<DomainResult<Category>> AddCategoryAsync(string title, Uri url)
    {
        int seq = await repository.GetMaxSequenceNumberAsync();
        var category = Category.Create(seq + 1, title, url);
        return DomainResult<Category>.Ok(category);
    }

    public async Task<DomainResult<Category>> DeleteCategoryAsync(Guid id)
    {
        var category = await repository.GetCategoryByIdAsync(id);
        if (category == null)
        {
            return DomainResult<Category>.Fail($"未找到Id:{id}的Category");
        }
        category.SoftDelete();
        return DomainResult<Category>.Ok(category);
    }

    public async Task<DomainResult<Category>> UpdateCategoryAsync(Guid id, string title, Uri coverUrl)
    {
        var category = await repository.GetCategoryByIdAsync(id);
        if (category == null)
        {
            return DomainResult<Category>.Fail($"未找到Id:{id}的Category");
        }
        category.ChangeTitle(title).ChangeCoverUrl(coverUrl).NotifyModified();
        return DomainResult<Category>.Ok(category);
    }
}
```

**领域服务的设计原则：**

1. **只做协调**：真正的状态变更调用实体的行为方法完成
2. **只依赖抽象**：只依赖仓储接口，不依赖 DbContext
3. **返回领域结果**：用 `DomainResult<T>` 返回操作结果，而不是直接抛出异常
4. **不处理事务**：领域服务不调用 `SaveChangesAsync`，事务由上层控制

### 3.6 DomainResult：领域操作的结果封装

```csharp
public class DomainResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public int StatusCode { get; set; }
    public string? Message { get; set; }

    public static DomainResult<T> Ok(T data) =>
        new() { Success = true, Data = data, StatusCode = 200 };

    public static DomainResult<T> Fail(string message, int statusCode = 400) =>
        new() { Success = false, Message = message, StatusCode = statusCode };
}
```

**为什么需要 DomainResult？**

```csharp
// 不用 DomainResult：返回 null 或抛出异常
public async Task<Category?> DeleteCategoryAsync(Guid id)
{
    var category = await repository.GetCategoryByIdAsync(id);
    if (category == null) return null;  // 调用方要判断 null
    category.SoftDelete();
    return category;
}

// 用 DomainResult：显式表达成功/失败
public async Task<DomainResult<Category>> DeleteCategoryAsync(Guid id)
{
    var category = await repository.GetCategoryByIdAsync(id);
    if (category == null)
        return DomainResult<Category>.Fail($"未找到Id:{id}的Category");
    category.SoftDelete();
    return DomainResult<Category>.Ok(category);
}
```

`DomainResult` 让领域方法的返回值**自文档化**——返回值类型已经告诉你"这个操作可能失败"。

### 3.7 软删除：DDD 中的横切关注点

项目中的实体都实现了软删除，这是一个典型的 DDD 横切关注点：

```csharp
public interface ISoftDelete
{
    bool IsDeleted { get; }
    void SoftDelete();
}
```

实体实现：

```csharp
public void SoftDelete()
{
    this.IsDeleted = true;
    this.DeletionTime = DateTime.UtcNow;
}
```

基础设施层配合：

```csharp
// EF Core 全局过滤器：查询自动排除已删除
builder.Entity<Category>().HasQueryFilter(s => !s.IsDeleted);
```

这样软删除的规则在基础设施层统一实施，领域层只需要关心业务含义——`category.SoftDelete()` 表达的是"这个分类被删除了"，而不是"把 IsDeleted 字段设为 true"。

## 四、对比：贫血模型 vs 充血模型

| 场景 | 贫血模型（传统三层架构） | 充血模型（DDD） |
|------|------------------------|----------------|
| 创建一个分类 | Service 中 new Category()，手动设值 | `Category.Create(seq, title, url)` |
| 修改标题 | `category.Title = newTitle` | `category.ChangeTitle(newTitle)` |
| 删除分类 | `category.IsDeleted = true` | `category.SoftDelete()` |
| 查询分类 | 直接操作 DbContext | 通过 Repository 接口 |
| 返回值 | null / 抛出异常 | `DomainResult<T>` |

*贫血模型的问题是：实体成了"数据容器"，逻辑散落在各个 Service 中，最终 Service 越来越胖，变成"上帝类"。*

## 五、项目中的 DDD 实践总结

### 项目中做到的

| DDD 模式 | 实现情况 | 举例 |
|---------|---------|------|
| **充血模型** | ✅ | `Category`、`Album`、`Episode` 都包含行为 |
| **静态工厂方法** | ✅ | `Category.Create()`、`Album.Create()`、`Episode.Create()` |
| **Builder 模式** | ✅ | `Episode.Builder` 处理复杂构造 |
| **仓储抽象** | ✅ | `IListeningRepository`、`IIdentityRepository`、`IFSRepository` |
| **领域服务** | ✅ | `ListeningService`、`IdService`、`FSDomainService` |
| **Bounded Context** | ✅ | `Listening.Domain`、`IdentityServer.Domain`、`FileService.Domain` |
| **领域结果** | ✅ | `DomainResult<T>` 统一操作结果 |
| **软删除** | ✅ | `ISoftDelete` 接口 + 全局过滤 |
| **依赖倒置** | ✅ | Domain 层只依赖接口，Infrastructure 实现接口 |
| **封装** | ✅ | `private set` + 行为方法 |

### 项目中未涉及但可以进一步实践的

| 模式 | 说明 |
|------|------|
| **Value Object（值对象）** | 如 `Uri`、`TimeSpan` 可以用值对象包装，但当前直接用原始类型 |
| **Aggregate Root（聚合根）** | 当前没有显式定义聚合根，`Category`→`Album`→`Episode` 的边界可以进一步明确 |
| **Domain Event（领域事件）** | 跨聚合通信（如"专辑发布后通知用户"）可以用领域事件解耦 |
| **Specification（规格模式）** | 复杂查询条件可以封装为规格对象 |
| **Unit of Work（工作单元）** | 当前 SaveChanges 由 Controller 调用，可以统一管理 |

## 六、DDD 的正确心态

> DDD 是一种工具，不是目的。

项目中的 DDD 实践很好地体现了**"恰到好处的设计"**：

- 实体有行为，但没有引入 AggregateRoot 基类——够用
- 仓储有抽象，但没有为每个实体写复杂的 Specification——够用
- 领域服务有业务逻辑，但没有引入 Event Bus 和消息队列——够用

**DDD 的黄金法则是**：在能带来最大价值的边界上应用，在复杂度不够的地方保持简单。一个 5 个字段的分类管理界面不需要 Aggregate Root + Domain Event，但一个有复杂业务规则的订单系统需要。

## 七、总结

| 主题 | 要点 |
|------|------|
| **什么是 DDD** | 让软件模型与业务领域模型相匹配的设计思想 |
| **充血模型** | 实体有行为，而不是仅含属性的数据容器 |
| **工厂方法** | `Category.Create()` 封装创建逻辑，保证完整性 |
| **Builder 模式** | 解决复杂实体的构造问题，Build() 时做完整性校验 |
| **仓储模式** | Domain 层定义接口，Infrastructure 层实现，解耦持久化 |
| **领域服务** | 编排跨实体业务逻辑，不直接依赖基础设施 |
| **DomainResult** | 显式表达操作成功/失败，替代 null 和异常 |
| **软删除** | 横切关注点，实体行为 + 全局过滤器配合 |
| **合理取舍** | 在需要的地方用 DDD，不必为了 DDD 而 DDD |
