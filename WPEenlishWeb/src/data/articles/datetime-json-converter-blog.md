# ASP.NET Core 时间格式处理：自定义 DateTimeJsonConverter 实践

## 一、前言

如果你用 ASP.NET Core Web API 返回过包含 `DateTime` 字段的数据，一定遇到过这个问题：

```
"createTime": "2026-07-03T14:30:00"
```

有时候它带时区：

```
"createTime": "2026-07-03T14:30:00+08:00"
```

有时候又变成这样：

```
"createTime": "2026-07-03T14:30:00Z"
```

当前端告诉你："时间格式咱能统一一下吗？" 的时候，你就知道该处理 DateTime 序列化了。

更麻烦的是，当数据库里某个字段为 `null` 时：

```json
{
  "updateTime": "0001-01-01T00:00:00"
}
```

前端看到公元 1 年 1 月 1 日——这显然是 `null` 日期被默认值填充了。

本文将用两个自定义 `JsonConverter` 优雅地解决以上所有问题。

## 二、System.Text.Json 的默认行为

ASP.NET Core 3.0+ 默认使用 `System.Text.Json` 而非 Newtonsoft.Json。它对 DateTime 的默认序列化规则是 **ISO 8601 标准格式**：

```csharp
// 默认输出
2026-07-03T14:30:00.0000000Z   // DateTimeKind.Utc
2026-07-03T22:30:00.0000000+08:00  // DateTimeKind.Local
2026-07-03T14:30:00.0000000     // DateTimeKind.Unspecified（无时区信息）
```

问题在哪？

| 问题 | 表现 |
|------|------|
| 格式不统一 | UTC 带 `Z`，Local 带 `+08:00`，前端难以统一解析 |
| null 变默认值 | `DateTime?` 为 null 时输出 `"0001-01-01"` |
| 精度不可控 | 默认保留 7 位小数毫秒，很多场景不需要 |
| 解析容错差 | 前端传 `"2026-07-03"`（日期无时间）可能解析失败 |

## 三、核心设计：DateTimeJsonConverter

### 3.1 自定义格式化输出

```csharp
public class DateTimeJsonConverter : JsonConverter<DateTime>
{
    private readonly string _format;

    public DateTimeJsonConverter() : this("yyyy-MM-ddTHH:mm:ssZ")
    {
    }

    public DateTimeJsonConverter(string format)
    {
        _format = format;
    }
    // ...
}
```

**关键设计点：**

- **默认格式** `yyyy-MM-ddTHH:mm:ssZ` 是 ISO 8601 的紧凑形式：`2026-07-03T14:30:00Z`
- **构造注入格式**：允许不同的微服务使用不同的格式。比如某些场景需要毫秒精度时可以传入 `yyyy-MM-ddTHH:mm:ss.fffZ`
- **只控制写入**：读取时尽可能兼容多种输入格式（见下文）

### 3.2 智能读取：兼容各种输入

```csharp
public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert,
    JsonSerializerOptions options)
{
    string? str = reader.GetString();
    if (string.IsNullOrEmpty(str))
    {
        return default(DateTime);
    }

    if (DateTime.TryParse(str, CultureInfo.InvariantCulture,
        DateTimeStyles.RoundtripKind, out var dt))
    {
        return dt;
    }

    return reader.GetDateTime();
}
```

读取逻辑是三层容错：

```
输入字符串
    ↓
①  null 或空 → 返回 default(DateTime)
    ↓
②  TryParse 成功 → 返回解析结果（RoundtripKind 保留时区信息）
    ↓
③  TryParse 失败 → 调用 reader.GetDateTime() 抛出标准异常
```

**为什么用 `RoundtripKind`？**

`DateTimeStyles.RoundtripKind` 能正确识别三种情况：

| 输入 | Kind 结果 |
|------|-----------|
| `"2026-07-03T14:30:00Z"` | `Utc` |
| `"2026-07-03T14:30:00+08:00"` | `Local`（带偏移） |
| `"2026-07-03T14:30:00"` | `Unspecified` |

这样不管前端传来什么格式，都能正确保留时区语义。

### 3.3 写入：按指定格式输出

```csharp
public override void Write(Utf8JsonWriter writer, DateTime value,
    JsonSerializerOptions options)
{
    writer.WriteStringValue(value.ToString(_format));
}
```

写入极其简洁——直接按构造函数指定的格式输出。默认输出如：

```json
"2026-07-03T14:30:00Z"
```

**重要注意事项**：`value.ToString("yyyy-MM-ddTHH:mm:ssZ")` 中的 `Z` 是**字面量字符**，不是格式说明符，它会被原样输出。如果要输出带偏移的格式，可以用 `yyyy-MM-ddTHH:mm:ssK`，其中的 `K` 才是时区说明符。

> 这实际上是这个转换器的一个小"陷阱"：`Z` 作为字面量总是出现在末尾，无论 DateTime 的 Kind 是什么。如果你的 DateTime 是 `Local` 类型，实际输出的时间可能和预期有时差，但末尾仍然写着 `Z`。使用时需确保写入的 DateTime 都是 UTC 时间。

## 四、NullableDateTimeJsonConverter：优雅处理 null

```csharp
public class NullableDateTimeJsonConverter : JsonConverter<DateTime?>
{
    private readonly DateTimeJsonConverter _inner;

    public NullableDateTimeJsonConverter() : this("yyyy-MM-ddTHH:mm:ssZ") { }

    public NullableDateTimeJsonConverter(string format) =>
        _inner = new DateTimeJsonConverter(format);

    public override DateTime? Read(ref Utf8JsonReader reader,
        Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        return _inner.Read(ref reader, typeof(DateTime), options);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value,
        JsonSerializerOptions options)
    {
        if (value == null) { writer.WriteNullValue(); return; }
        _inner.Write(writer, value.Value, options);
    }
}
```

### 设计亮点：组合而非继承

`NullableDateTimeJsonConverter` **没有继承** `DateTimeJsonConverter`，而是通过**组合**持有它的实例：

```
NullableDateTimeJsonConverter
  └─ 持有 DateTimeJsonConverter _inner
       ├─ null 检查 → 写 null / 返回 null
       └─ 非 null  → 委托给 _inner
```

这样做的理由：

1. `JsonConverter<T>` 的泛型参数不同（`DateTime?` vs `DateTime`），无法直接复用泛型继承
2. 单一职责：`NullableDateTimeJsonConverter` 只关心 null 判断，实际的读写逻辑委托给 `_inner`
3. 格式统一：构造函数接受相同的 `format` 参数，保证 nullable 和非 nullable 字段输出格式一致

### 效果对比

**没有 NullableDateTimeJsonConverter 时：**

```csharp
class Article
{
    public DateTime CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }  // 数据库中为 null
}
```

输出：

```json
{
  "createTime": "2026-07-03T14:30:00Z",
  "updateTime": "0001-01-01T00:00:00"   // null 变成了默认值！
}
```

**使用后：**

```json
{
  "createTime": "2026-07-03T14:30:00Z",
  "updateTime": null                     // 正确输出 null
}
```

## 五、完整代码

### DateTimeJsonConverter.cs

```csharp
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Commons.JsonConverters
{
    public class DateTimeJsonConverter : JsonConverter<DateTime>
    {
        private readonly string _format;

        public DateTimeJsonConverter() : this("yyyy-MM-ddTHH:mm:ssZ")
        {
        }

        public DateTimeJsonConverter(string format)
        {
            _format = format;
        }

        public override DateTime Read(ref Utf8JsonReader reader,
            Type typeToConvert, JsonSerializerOptions options)
        {
            string? str = reader.GetString();
            if (string.IsNullOrEmpty(str))
                return default;

            if (DateTime.TryParse(str, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var dt))
                return dt;

            return reader.GetDateTime();
        }

        public override void Write(Utf8JsonWriter writer,
            DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(_format));
        }
    }
}
```

### NullableDateTimeJsonConverter.cs

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Commons.JsonConverters
{
    public class NullableDateTimeJsonConverter : JsonConverter<DateTime?>
    {
        private readonly DateTimeJsonConverter _inner;

        public NullableDateTimeJsonConverter() : this("yyyy-MM-ddTHH:mm:ssZ") { }

        public NullableDateTimeJsonConverter(string format) =>
            _inner = new DateTimeJsonConverter(format);

        public override DateTime? Read(ref Utf8JsonReader reader,
            Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            return _inner.Read(ref reader, typeof(DateTime), options);
        }

        public override void Write(Utf8JsonWriter writer,
            DateTime? value, JsonSerializerOptions options)
        {
            if (value == null) { writer.WriteNullValue(); return; }
            _inner.Write(writer, value.Value, options);
        }
    }
}
```

## 六、注册到 ASP.NET Core

### 全局注册（推荐）

在 `Program.cs` 中为所有控制器启用：

```csharp
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new DateTimeJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableDateTimeJsonConverter());
    });
```

### 指定自定义格式

如果某个微服务需要毫秒精度：

```csharp
options.JsonSerializerOptions.Converters.Add(
    new DateTimeJsonConverter("yyyy-MM-ddTHH:mm:ss.fffZ"));
options.JsonSerializerOptions.Converters.Add(
    new NullableDateTimeJsonConverter("yyyy-MM-ddTHH:mm:ss.fffZ"));
```

### 效果一览

注册后，所有 DateTime 字段的输出变成统一的格式：

| 场景 | 之前 | 之后 |
|------|------|------|
| 正常时间 | `"2026-07-03T14:30:00.0000000Z"` | `"2026-07-03T14:30:00Z"` |
| 带时区时间 | `"2026-07-03T22:30:00.0000000+08:00"` | `"2026-07-03T14:30:00Z"` |
| 可空字段为 null | `"0001-01-01T00:00:00"` | `null` |

## 七、最佳实践与避坑指南

### 7.1 始终使用 UTC 时间

自定义转换器只是解决了**格式化输出**的问题，但没有解决**时区**的问题。最佳实践是：

```csharp
// 在应用层统一转换为 UTC
public class ArticleService
{
    public async Task CreateAsync(CreateArticleDto dto)
    {
        var article = new Article
        {
            CreateTime = DateTime.UtcNow,  // 统一使用 UtcNow
            // ...
        };
    }
}
```

配合转换器的 `Z` 后缀，前端收到的时间统一为 UTC + 时区标记，前端可以自动转换到本地时间：

```javascript
// 前端：自动将 UTC 时间转为本地时间
new Date("2026-07-03T14:30:00Z").toLocaleString()
```

### 7.2 格式字符串中的 Z 是字面量

这是一个容易踩的坑：

```csharp
// 格式 "yyyy-MM-ddTHH:mm:ssZ"
// 其中 Z 是字面量字符，不是格式说明符
// 输出始终是 "2026-07-03T14:30:00Z"
// 无论 DateTime.Kind 是 Utc、Local 还是 Unspecified

// 如果要根据时区动态输出，应使用 K 说明符：
// "yyyy-MM-ddTHH:mm:ssK"
// Utc → "2026-07-03T14:30:00Z"
// Local(+08:00) → "2026-07-03T22:30:00+08:00"
```

所以这个转换器隐含的前提是：**你写入的 DateTime 都是 Utc 类型**。如果不确定，可以用带 `K` 的格式让框架自动处理。

### 7.3 读取时不要丢失时区信息

`Read` 方法用 `DateTimeStyles.RoundtripKind` 保留时区信息，但返回的仍是 `DateTime` 结构——它只区分 `Utc`/`Local`/`Unspecified`，不保留具体的偏移量。

如果应用涉及跨时区计算，推荐改用 `DateTimeOffset`：

```csharp
// DateTimeOffset 天然保留时区偏移
public DateTimeOffset CreateTime { get; set; }
// 序列化结果: "2026-07-03T14:30:00+08:00"
```

### 7.4 两个转换器必须成对注册

```csharp
// ✅ 正确：两个一起注册
options.JsonSerializerOptions.Converters.Add(new DateTimeJsonConverter());
options.JsonSerializerOptions.Converters.Add(new NullableDateTimeJsonConverter());

// ❌ 错误：只注册非 nullable 的
// 这样 DateTime? 字段仍然使用默认序列化，null 变成 "0001-01-01"
options.JsonSerializerOptions.Converters.Add(new DateTimeJsonConverter());
```

## 八、总结

| 主题 | 要点 |
|------|------|
| **为什么要做** | 统一时间格式，避免前端适配多种格式 |
| **DateTimeJsonConverter** | 自定义格式输出 + 智能输入解析（三层容错） |
| **NullableDateTimeJsonConverter** | 组合委托模式，null 输出为 `null` 而非默认值 |
| **注册方式** | `AddJsonOptions` 全局注册，成对添加 |
| **最佳实践** | 业务层统一用 UTC，前端负责本地转换 |

这两个转换器加在一起不到 70 行代码，却解决了日常开发中最烦人的时间格式不一致问题。如果你的项目还在为"前端说时间格式不统一"而烦恼，不妨引入这两个转换器——开箱即用，零侵入。
