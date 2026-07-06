# WPEnglishWebSite - 英语学习网站

一个基于 **Vue 3 + .NET** 的英语听力学习平台，支持音频播放、文章管理、分类管理、专辑管理、用户管理等功能。

## 🚀 技术栈

| 层级 | 技术 |
|------|------|
| **前端** | Vue 3 + Vite 8 + Element Plus + Vue Router + Axios |
| **后端** | .NET (C#) - 多层架构 (DDD) |
| **认证** | IdentityServer4 + JWT |
| **数据库** | SQL Server / MySQL |
| **日志** | Serilog |
| **API 文档** | RESTful + HTTP |

## 📁 项目结构

```
WPEnglishWebSite/
├── WPEenlishWeb/              # Vue 3 前端项目
│   └── src/
│       ├── Views/             # 页面组件
│       │   ├── AlbumList.vue  # 专辑列表
│       │   ├── AlbumAdd.vue   # 添加专辑
│       │   ├── AlbumUpdate.vue# 编辑专辑
│       │   ├── CategoryList.vue# 分类列表
│       │   ├── CategoryAdd.vue # 添加分类
│       │   ├── CategoryUpdate.vue# 编辑分类
│       │   ├── EpisodeList.vue # 剧集列表
│       │   ├── EpisodeAdd.vue  # 添加剧集
│       │   ├── EpisodeUpdate.vue# 编辑剧集
│       │   ├── UserList.vue    # 用户列表
│       │   ├── UserAdd.vue     # 添加用户
│       │   ├── UserUpdate.vue  # 编辑用户
│       │   └── Login.vue       # 登录页面
│       ├── data/
│       │   └── post.js         # Axios API 接口封装
│       └── utils/
│           └── time.js         # 时间工具函数
├── WPEnglish/                 # .NET 后端解决方案 (.sln)
│   ├── Listening.Admin.WebApi # 后台管理 API 控制器
│   ├── IdentitServer.WebApi   # 认证授权服务
│   ├── Listening.Domain       # 领域层（实体、服务接口）
│   ├── IdentityServer.Domain  # 认证领域模型
│   ├── FileService            # 文件上传下载服务
│   ├── FileService.Domain     # 文件服务领域层
│   ├── FileService.Infrastructure # 文件服务基础设施
│   ├── IdentityServer.Infrastructure # 认证基础设施
│   ├── Listening.Infrastructure    # 听力数据基础设施
│   ├── HashHelper             # 通用工具类（异常处理、JSON 转换等）
│   ├── JWT                    # JWT 令牌生成与验证
│   ├── DomainCommons          # 领域通用基类
│   └── Infrastructure         # 通用基础设施
├── lrc/                       # LRC 字幕/歌词文件
├── mp3/                       # 英语听力音频文件
└── publish/                   # 发布产物
```

## ✨ 功能特性

- 🎧 **英语听力学习** - 支持音频播放与 LRC 同步字幕
- 📚 **内容管理** - 专辑（Album）、分类（Category）、剧集（Episode）完整 CRUD
- 👥 **用户管理** - 后台用户增删改查
- 🔐 **身份认证** - JWT + IdentityServer4 授权
- 📝 **文章发布** - 支持 Markdown 格式内容

## 🛠️ 本地开发

### 前置要求

- Node.js >= 20
- .NET SDK 8.0+
- SQL Server（或 MySQL）

### 前端启动

```bash
cd WPEenlishWeb
npm install
npm run dev
```

前端默认运行在 `http://localhost:5173`。

### 后端启动

```bash
# 使用 Visual Studio 或 JetBrains Rider 打开 WPEnglish/WPEnglish.sln
# 或使用 dotnet CLI：
cd WPEnglish
dotnet restore
dotnet run --project Listening.Admin.WebApi
```

后端 API 默认运行在 `http://localhost:5000`。

### 数据库配置

在 `appsettings.json` 中配置数据库连接字符串：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=WPEnglish;Trusted_Connection=True;"
  }
}
```

首次运行前需执行 Entity Framework 迁移：

```bash
dotnet ef database update
```

## 🏗️ 架构说明

项目采用 **领域驱动设计（DDD）** 分层架构：

- **Domain 层** - 定义实体、值对象、领域服务接口，不依赖基础设施
- **Application 层** - 应用服务、DTO、接口实现
- **Infrastructure 层** - 数据持久化（EF Core）、仓储实现
- **WebApi 层** - 控制器、中间件、过滤器

### 关键设计

- ✅ **全局异常处理** - `GlobalExceptionHandler` 中间件统一处理异常
- ✅ **API 响应封装** - `ApiResponse` 统一返回格式
- ✅ **FluentValidation** - 请求参数验证
- ✅ **AutoMapper** - 实体与 DTO 映射
- ✅ **JWT 认证** - IdentityServer4 集成

## 🌐 部署

### 前端构建

```bash
cd WPEenlishWeb
npm run build
# 构建产物在 dist/ 目录
```

### 后端发布

```bash
cd WPEnglish
dotnet publish -c Release -o ../publish
```

可将前后端部署至 IIS、Nginx 或 Docker 容器。

## 🤝 贡献

欢迎提交 Issue 或 Pull Request！

1. Fork 本项目
2. 创建功能分支 (`git checkout -b feature/your-feature`)
3. 提交更改 (`git commit -m 'Add some feature'`)
4. 推送到分支 (`git push origin feature/your-feature`)
5. 创建 Pull Request

## 📄 许可证

MIT
