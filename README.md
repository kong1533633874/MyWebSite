# WPEnglish — 英语听力学习平台

一个基于 **Vue 3 + .NET 8** 的前后端分离英语听力学习平台，涵盖分类浏览、音频播放、LRC/SRT 字幕同步、后台内容管理、用户认证授权等完整功能。

> 🌐 在线体验：[https://www.zxhyq.site/](https://www.zxhyq.site/)

---

## 🚀 技术栈

### 前端

| 技术 | 说明 |
|------|------|
| **Vue 3** (Composition API) | 渐进式前端框架 |
| **Vite 8** | 极速构建与热更新 |
| **Element Plus** | 企业级 UI 组件库 |
| **Vue Router 5** | 路由管理 + 鉴权守卫 |
| **Axios** | HTTP 客户端，自动携带 JWT Token |
| **marked** | Markdown 内容渲染 |

### 后端

| 技术 | 说明 |
|------|------|
| **.NET 8** | ASP.NET Core Web API |
| **Entity Framework Core** | ORM 数据持久化（SQL Server） |
| **ASP.NET Core Identity** | 用户与角色管理 |
| **JWT**（HMAC-SHA256） | 自定义 Token 签发与验证 |
| **FluentValidation** | 声明式参数验证 |
| **Serilog** | 结构化日志（异步文件 + 控制台） |
| **Swagger / OpenAPI** | API 文档自动生成 |

### 架构设计

- **DDD 分层架构** — Domain（实体、领域服务）/ Infrastructure（EF Core 仓储）/ WebApi（控制器、中间件）/ Commons（通用基类、工具库）
- **CQRS 风格** — 查询与命令分离，公共 API 走缓存，管理 API 走数据库
- **微服务拆分** — 主业务 API、管理后台 API、身份认证服务、文件服务四个独立项目
- **全局异常处理** — `GlobalExceptionHandler` 中间件统一捕获
- **API 统一响应** — `ApiResponseFilter` 自动包装为 `{ success, data, message, statusCode }` 格式

---

## ✨ 功能特性

### 🎧 英语听力学习

- **音频播放** — 支持播放 / 暂停 / 进度拖拽
- **LRC / SRT 字幕同步** — 播放时字幕实时高亮滚动
- **字幕切换** — 一键显示 / 隐藏字幕
- **点击收藏** — 点击当前字幕可收藏句子
- **字幕回顾** — 收藏列表支持点击跳转到对应时间点
- **🤖 AI 生成字幕** — 集成腾讯云语音识别，上传音频自动生成 SRT/LRC 字幕

### 🌐 公开浏览

- **分类 → 专辑 → 剧集** 三级导航浏览
- **音频播放页** — 从剧集列表直接进入播放
- **技术博客** — 10 篇 .NET / Vue 技术文章（Markdown 渲染）
- **资源导航** — 精选开发资源分类展示与搜索

### 📚 内容管理（管理员）

- **分类管理** — 增删改查 + 拖拽排序
- **专辑管理** — 按分类归属 + 排序
- **剧集管理** — 音频上传 + 字幕编辑 + 排序 + 可见性控制
- **文件上传** — 独立文件服务支持音频、字幕文件

### 👥 用户管理

- 后台用户增删改查
- JWT 登录认证 + 角色授权

---

## 🏗️ 项目结构

```
WPEnglishWebSite/
├── WPEenlishWeb/                   # Vue 3 前端
│   └── src/
│       ├── Views/                  # 管理后台页面
│       ├── SightseerViews/         # 游客浏览页面
│       ├── components/             # 公共组件（Uploader）
│       ├── data/                   # 静态数据（文章、资源）
│       ├── route/                  # 路由配置 + 鉴权守卫
│       └── utils/                  # Axios 封装、工具函数
├── WPEnglish/                      # .NET 后端解决方案
│   ├── WPEnglish/                  # 主业务 API（公开查询）
│   ├── Listening.Admin.WebApi/     # 管理后台 API
│   ├── IdentitServer.WebApi/       # 身份认证服务
│   ├── FileService/                # 文件上传下载服务
│   ├── Listening.Domain/           # 领域实体与接口
│   ├── Listening.Infrastructure/   # EF Core 仓储实现
│   ├── IdentityServer.Domain/      # 认证领域模型
│   ├── IdentityServer.Infrastructure/ # 认证数据层
│   ├── FileService.Domain/         # 文件服务领域层
│   ├── FileService.Infrastructure/ # 文件服务数据层
│   ├── JWT/                        # 独立 JWT 鉴权库
│   ├── HashHelper (Commons)/       # 通用工具（异常处理、过滤、JSON 转换）
│   ├── CommonInitializer/          # Serilog、CORS 等共享配置
│   └── DomainCommons/              # 领域通用基类（软删除、统一返回）
└── README.md
```

---

## 🧩 关键设计

| 设计模式 | 实现 | 说明 |
|---------|------|------|
| 领域驱动设计 | Domain / Infrastructure / WebApi 分层 | 业务逻辑与基础设施解耦 |
| 仓储模式 | `IListeningRepository` → `ListeningRepository` | 抽象数据访问层 |
| 工厂模式 | `SubtitleParserFactory` → `LrcParser` / `SrtParser` | 支持双格式字幕解析 |
| Builder 模式 | `Episode.Builder` | 领域对象构建与校验 |
| 过滤器 | `ApiResponseFilter` | 统一 API 响应格式 |
| 中间件 | `GlobalExceptionHandler` | 全局异常统一处理 |
| 策略模式 | `IStorageClient` → `SMBStorageClient` | 文件存储可切换 |

---

## 🔐 认证流程

```
用户登录 → IdentitServer.WebApi 验证用户名密码
         → JWT 模块签发 Token（HMAC-SHA256）
         → 前端存储到 localStorage
         → Axios 拦截器自动携带 Authorization Header
         → 后端 JWT Bearer 中间件验证
         → Vue Router 守卫检测 Token 控制页面访问
```

---

## 🤝 贡献与许可

MIT License — 欢迎 Issue 和 Pull Request。
