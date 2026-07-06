import apiresponseArticle from './articles/api-response-filter-blog.md?raw'
import datetimeConverterArticle from './articles/datetime-json-converter-blog.md?raw'
import serilogArticle from './articles/serilog-blog.md?raw'
import identityArticle from './articles/identity-blog.md?raw'
import fluentvalidationArticle from './articles/fluentvalidation-blog.md?raw'
import dddArticle from './articles/ddd-blog.md?raw'
import exceptionHandlerArticle from './articles/global-exception-handler-blog.md?raw'
import efcoreArticle from './articles/efcore-blog.md?raw'
import vueRouterArticle from './articles/vue-router-blog.md?raw'
import vueReactivityArticle from './articles/vue-reactivity-blog.md?raw'

export const posts = [
  {
    id: 1,
    title: 'ASP.NET Core 统一响应体设计：ApiResponseFilter 实践',
    date: '2026-07-03',
    tags: ['ASP.NET Core', 'C#', '后端'],
    excerpt: '统一响应体（Unified API Response）是前后端分离架构下的标配设计模式。本文通过一个 IResultFilter 的实现，讲解如何用过滤器自动包装 API 响应，让所有接口输出统一格式，前端用同一套逻辑处理所有请求结果。',
    content: apiresponseArticle
  },
  {
    id: 2,
    title: 'ASP.NET Core 时间格式处理：自定义 DateTimeJsonConverter 实践',
    date: '2026-07-03',
    tags: ['ASP.NET Core', 'C#', '后端', 'JSON'],
    excerpt: '每当前端说"时间格式能统一一下吗"，就该处理 DateTime 序列化了。本文通过 DateTimeJsonConverter 和 NullableDateTimeJsonConverter 两个自定义转换器，解决 System.Text.Json 时间格式不一致、null 变默认值等问题。',
    content: datetimeConverterArticle
  },
  {
    id: 3,
    title: 'ASP.NET Core 结构化日志：Serilog 集成实践',
    date: '2026-07-03',
    tags: ['ASP.NET Core', 'C#', '后端', 'Serilog'],
    excerpt: '日志是生产环境最重要的监控手段。本文结合项目中的真实配置，讲解 Serilog 在 ASP.NET Core 微服务架构中的集成方式——集中配置、一行注册、异步文件写入、结构化占位符，让日志可以搜索、过滤、聚合。',
    content: serilogArticle
  },
  {
    id: 4,
    title: 'ASP.NET Core 身份认证实战：JWT + Identity 从零搭建',
    date: '2026-07-03',
    tags: ['ASP.NET Core', 'C#', '后端', 'JWT', '认证'],
    excerpt: '几乎所有 Web 应用都需要回答三个问题：你是谁？你能做什么？怎么证明是你？本文从数据模型到 Token 签发，从认证验证到授权拦截，完整拆解 ASP.NET Core Identity + JWT 的身份认证实现。',
    content: identityArticle
  },
  {
    id: 5,
    title: 'ASP.NET Core 请求验证：FluentValidation 从入门到项目实践',
    date: '2026-07-04',
    tags: ['ASP.NET Core', 'C#', '后端', 'FluentValidation'],
    excerpt: '写 Web API 时最烦的事情之一就是参数验证——散落的 if/else、重复的规则、被淹没的业务逻辑。本文从项目的真实代码出发，讲解 FluentValidation 的声明式验证、自定义扩展、异步数据库校验以及手动调用模式。',
    content: fluentvalidationArticle
  },
  {
    id: 6,
    title: '领域驱动设计实战：.NET 项目中的 DDD 实践',
    date: '2026-07-04',
    tags: ['ASP.NET Core', 'C#', '后端', 'DDD'],
    excerpt: 'DDD 不是框架也不是工具，而是一套设计思想——让软件的复杂性与业务领域的复杂性相匹配。本文基于项目中的真实代码，讲解充血模型、工厂方法、Builder 模式、仓储抽象、领域服务等 DDD 战术模式的落地实践。',
    content: dddArticle
  },
  {
    id: 7,
    title: 'ASP.NET Core 全局异常处理：从混乱到优雅',
    date: '2026-07-04',
    tags: ['ASP.NET Core', 'C#', '后端', '中间件'],
    excerpt: '生产环境中一个未处理异常可能导致黄页错误或连接断开。本文基于项目中的 GlobalExceptionHandler 中间件，讲解如何用不到 80 行代码统一捕获异常、分类映射为 HTTP 状态码、记录日志并返回友好的 JSON 响应。',
    content: exceptionHandlerArticle
  },
  {
    id: 8,
    title: 'ASP.NET Core 数据持久化：Entity Framework Core 项目实践',
    date: '2026-07-04',
    tags: ['ASP.NET Core', 'C#', '后端', 'EF Core', '数据库'],
    excerpt: '数据持久化是后端系统最基础也最关键的一环。本文基于项目中的三个独立 DbContext，讲解 EF Core 的 Fluent API 配置、软删除全局过滤器、DateTime 精度统一、迁移管理以及 SQL Server 生产部署实践。',
    content: efcoreArticle
  },
  {
    id: 9,
    title: 'Vue 3 前端路由：Vue Router 从入门到项目实践',
    date: '2026-07-04',
    tags: ['Vue 3', '前端', 'Vue Router', 'SPA'],
    excerpt: '前端路由是 SPA 的核心：URL 变化时不刷新页面，只切换组件。本文基于项目中的 20 条路由配置，讲解 createWebHistory 模式、路由分组、动态参数 :id、beforeEach 导航守卫、RouterLink/RouterView 的完整实践。',
    content: vueRouterArticle
  },
  {
    id: 10,
    title: 'Vue 3 响应式原理与实践：ref、reactive、computed 从入门到精通',
    date: '2026-07-04',
    tags: ['Vue 3', '前端', '响应式', 'Composition API'],
    excerpt: 'Vue 3 最核心的特性就是响应式系统——数据变了页面自动更新。本文从 Proxy 原理到 ref/reactive/computed/watch 的实际用法，结合项目中的 18 处 ref、15 处 reactive、3 处 computed 的真实代码，全面解析 Vue 3 响应式。',
    content: vueReactivityArticle
  }
]
