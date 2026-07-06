# WPEnglishWebSite - 英语学习网站

一个基于 **Vue 3 + .NET** 的英语听力学习平台，支持文章管理、分类管理、音频播放等功能，并配有数据采集工具自动抓取英语学习资源。

## 🚀 技术栈

| 层级 | 技术 |
|------|------|
| **前端** | Vue 3 + Vite 8 + Element Plus + Vue Router + Axios |
| **后端** | .NET (C#) - 多层架构 |
| **认证** | IdentityServer4 + JWT |
| **数据采集** | Python (Requests + BeautifulSoup + lxml) |
| **数据库** | SQL |

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
│       │   └── Login.vue       # 登录
│       └── data/
│           └── post.js         # API 接口
├── WPEnglish/                 # .NET 后端解决方案
│   ├── Listening.Admin.WebApi # 后台管理 API
│   ├── IdentitServer.WebApi   # 认证服务
│   ├── Listening.Domain       # 领域层
│   ├── IdentityServer.Domain  # 认证领域
│   ├── FileService            # 文件服务
│   ├── HashHelper             # 工具类
│   ├── JWT                    # JWT 认证
│   └── Infrastructure         # 基础设施层
├── *.py                       # Python 数据采集脚本
│   ├── voa_crawler.py         # VOA 英语爬虫
│   ├── english_collector.py   # 英语资源采集器
│   └── data_importer.py       # 数据导入工具
├── lrc/                       # LRC 歌词文件
├── mp3/                       # 音频文件
└── publish/                   # 发布产物
```

## ✨ 功能特性

- 🎧 **英语听力学习** - 支持音频播放与同步字幕
- 📚 **内容管理** - 专辑、分类、剧集 CRUD
- 👥 **用户管理** - 用户增删改查
- 🔐 **身份认证** - JWT + IdentityServer4
- 🤖 **数据采集** - 自动抓取 VOA、ESL 等英语学习资源
- 📝 **文章发布** - 支持 Markdown 内容

## 🛠️ 本地开发

### 前置要求

- Node.js >= 20
- .NET SDK
- Python 3

### 前端启动

```bash
cd WPEenlishWeb
npm install
npm run dev
```

### 后端启动

```bash
# 使用 Visual Studio 或 Rider 打开 WPEnglish/WPEnglish.sln
# 或使用 dotnet CLI
cd WPEnglish
dotnet restore
dotnet run
```

### 数据采集

```bash
# 采集 VOA 英语学习内容
python voa_crawler.py

# 采集英语资源
python english_collector.py
```

## 📄 许可证

MIT
