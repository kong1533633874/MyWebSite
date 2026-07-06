# Vue 3 前端路由：Vue Router 从入门到项目实践

## 一、前言

当你打开一个单页应用（SPA）时，看似在页面之间"跳转"，但浏览器并没有真正刷新——这一切都是**前端路由**在背后工作。

前端路由的核心就两件事：

1. **URL 变化时，不刷新页面，只切换组件**
2. **用户刷新或直接访问 URL 时，能正确渲染对应的视图**

Vue Router 是 Vue.js 官方的路由管理器，与 Vue 3 深度集成。本文基于项目中 20 条路由的真实配置，讲解 Vue Router 的核心用法。

## 二、两种路由模式

Vue Router 提供两种模式：

| 模式 | URL 示例 | 原理 | 需要服务端配置？ |
|------|---------|------|----------------|
| **Hash 模式** | `http://site.com/#/blogList` | URL 中 `#` 后面的变化不触发浏览器请求 | 否 |
| **History 模式** | `http://site.com/blogList` | 利用 HTML5 History API 改变 URL | 是 |

项目中使用的是 **History 模式**：

```javascript
import { createRouter, createWebHistory } from "vue-router";

const router = createRouter({
    history: createWebHistory(),    // ← History 模式
    routes: paths
});
```

**History 模式的优点**：URL 干净、美观，没有 `#` 符号。

**代价**：需要服务端配合。因为用户直接访问 `/blogList` 时，浏览器会向服务端请求这个路径，而服务端没有对应的文件，如果不做配置会返回 404。

Nginx 配置示例：

```nginx
location / {
    try_files $uri $uri/ /index.html;
}
```

这样所有路径都返回 `index.html`，让 Vue Router 接管路由解析。

## 三、路由配置

### 3.1 基本结构

项目中的路由配置采用"数组定义 + 导入路由"的方式：

```javascript
import { createRouter, createWebHistory } from "vue-router";

// 导入所有页面组件
import CategoryList from '../Views/CategoryList.vue'
import Login from '../Views/Login.vue'
import BlogList from "@/SightseerViews/ResourceSites/BlogList.vue"
// ...

// 路由数组
const paths = [
    // ... 所有路由定义
]

const router = createRouter({
    history: createWebHistory(),
    routes: paths
})

export default router
```

### 3.2 路由分组

项目的 20 条路由分为三个清晰的区域：

```javascript
const paths = [
    // ===== 重定向 =====
    { path: "/", redirect: "/category" },

    // ===== 登录页 =====
    { path: "/login", component: Login },

    // ===== 管理页面（需登录） =====
    { path: "/categoryList", component: CategoryList, meta: { requiresAuth: true } },
    { path: "/categoryAdd",  component: CategoryAdd,  meta: { requiresAuth: true } },
    { path: "/categoryUpdate", component: CategoryUpdate, meta: { requiresAuth: true } },
    // ... Album、Episode、User 的 CRUD 路由

    // ===== 游客页面（无需登录） =====
    { path: "/category", component: Category },
    { path: "/album", component: Album },
    { path: "/episode", component: Episode },
    { path: "/audio", component: AudioPlayer },

    // ===== 静态网站（博客、资源） =====
    { path: "/blogList", component: BlogList },
    { path: "/blogList/postDetail/:id", component: PostDetail },
    { path: "/resources", component: Resources },
]
```

| 分组 | 路径前缀 | 访问控制 | 说明 |
|------|---------|---------|------|
| **管理页面** | `/category*`, `/album*`, `/episode*`, `/user*` | 需登录 | 12 个 CRUD 页面 |
| **游客页面** | `/category`, `/album`, `/episode`, `/audio` | 公开 | 浏览和播放内容 |
| **静态网站** | `/blogList`, `/resources` | 公开 | 博客和技术文章 |

### 3.3 动态路由参数

在 20 条路由中，只有一条使用了动态参数：

```javascript
{
    path: "/blogList/postDetail/:id",
    component: PostDetail
}
```

这里的 `:id` 是**动态路径段**，可以匹配 `/blogList/postDetail/1`、`/blogList/postDetail/2` 等。

**在组件中获取参数：**

```javascript
import { useRoute } from 'vue-router'
import { computed } from 'vue'

const route = useRoute()
const post = computed(() => posts.find(p => p.id == route.params.id))
```

**在模板中导航：**

```html
<!-- BlogList.vue 中点击文章卡片时 -->
<div @click="$router.push(`/blogList/postDetail/${post.id}`)">
    <!-- 文章卡片内容 -->
</div>

<!-- PostDetail.vue 中的返回按钮 -->
<el-button @click="$router.back()">← 返回列表</el-button>
```

### 3.4 重定向

```javascript
{ path: "/", redirect: "/category" }
```

访问根路径 `/` 时自动跳转到 `/category`，让用户默认看到分类浏览页。

## 四、路由导航守卫

项目中只有一个 `beforeEach` 全局守卫：

```javascript
router.beforeEach((to, from) => {
    if (to.meta.requiresAuth) {
        const token = localStorage.getItem("token")
        if (!token) {
            return "/login"
        }
    }
})
```

**它的执行流程：**

```
用户访问 /categoryAdd
    ↓
URL 尚未变更
    ↓
router.beforeEach 触发
    ↓
to.meta.requiresAuth === true
    ↓
检查 localStorage 中是否有 token
    ├── 有 → 放行，渲染 CategoryAdd 组件
    └── 无 → redirect 到 /login，URL 变为 /login
```

### 4.1 meta 元信息

`meta` 是路由配置中的一个字段，用来附加自定义数据：

```javascript
{
    path: "/categoryList",
    component: CategoryList,
    meta: { requiresAuth: true }  // ← 自定义元信息
}
```

在导航守卫中通过 `to.meta.requiresAuth` 读取。除了 `requiresAuth`，`meta` 还可以存储：

```javascript
meta: {
    requiresAuth: true,
    title: "分类列表",
    transition: "slide-left",
    keepAlive: true
}
```

### 4.2 为什么用 localStorage 存 token？

```javascript
const token = localStorage.getItem("token")
```

项目将 JWT 令牌存储在 `localStorage` 中，这是 SPA 最常见的方式。登录成功后：

```javascript
// Login.vue（示意）
const res = await axios.post("/api/Identity/Login", { userName, password })
localStorage.setItem("token", res.data)   // 保存 token
```

登出时：

```javascript
localStorage.removeItem("token")
router.push("/login")
```

**关于 Token 存储的讨论：**

| 方案 | 优点 | 缺点 |
|------|------|------|
| `localStorage` | 简单，JS 直接访问 | XSS 攻击可窃取 |
| `sessionStorage` | 关闭标签页自动清除 | 多标签页不共享 |
| `httpOnly Cookie` | XSS 无法窃取 | 需要服务端配合，CSRF 防护 |

对于内容管理后台这种内部系统，`localStorage` 是合理的选择。

## 五、在 main.js 中注册

```javascript
import { createApp } from 'vue'
import App from './App.vue'
import ElementPlus from 'element-plus'
import 'element-plus/dist/index.css'
import router from './route'      // ← 导入路由配置

const app = createApp(App)
app.use(router)                   // ← 注册 Vue Router
app.use(ElementPlus)
app.mount('#app')
```

`app.use(router)` 做了三件事：

1. **注册全局组件**：`<RouterLink>` 和 `<RouterView>` 在所有组件中可用
2. **注入路由对象**：所有组件可以通过 `this.$router` / `this.$route` 访问（Options API），或 `useRouter()` / `useRoute()`（Composition API）
3. **启动路由**：根据当前 URL 渲染对应的组件

## 六、在 App.vue 中使用

### 6.1 RouterView

```html
<template>
    <main>
        <RouterView />     <!-- ← 路由组件在此渲染 -->
    </main>
</template>
```

`<RouterView>` 是一个动态组件，它根据当前路由的配置渲染对应的页面组件。比如：

| URL | RouterView 渲染的组件 |
|-----|---------------------|
| `/login` | `<Login />` |
| `/blogList` | `<BlogList />` |
| `/blogList/postDetail/3` | `<PostDetail />` |

### 6.2 RouterLink

```html
<!-- App.vue 导航栏中的链接 -->
<RouterLink to="/category">浏览</RouterLink>
<RouterLink to="/blogList">博客</RouterLink>
<RouterLink to="/resources">资源</RouterLink>
<RouterLink to="/login">登录</RouterLink>

<!-- 需要登录后才显示的链接 -->
<RouterLink v-if="loginState.isLoggedIn" to="/categoryList">分类管理</RouterLink>
<RouterLink v-if="loginState.isLoggedIn" to="/userList">用户管理</RouterLink>
```

`<RouterLink>` 渲染为 `<a>` 标签，但点击时不会触发页面刷新，而是由 Vue Router 拦截并切换组件。

## 七、前端路由的核心原理

Vue Router 的 History 模式背后是 **HTML5 History API**：

```javascript
// 改变 URL 但不刷新页面
history.pushState({}, '', '/blogList')       // 添加一条历史记录
history.replaceState({}, '', '/login')       // 替换当前记录
window.addEventListener('popstate', () => {  // 监听前进/后退
    // 根据当前 URL 渲染对应组件
})
```

当你调用 `router.push('/blogList')` 时，Vue Router 内部：

```
① router.push('/blogList')
    ↓
② history.pushState(null, '', '/blogList')   ← 浏览器 URL 变化，不刷新
    ↓
③ Vue Router 匹配 /blogList → BlogList 组件
    ↓
④ RouterView 切换到 BlogList 组件
    ↓
⑤ beforeEach 守卫执行（如果有）
    ↓
⑥ 页面显示 BlogList 的内容
```

## 八、项目的路由导航方式总结

| 方式 | 代码 | 场景 |
|------|------|------|
| **声明式导航** | `<RouterLink to="/blogList">` | 导航栏固定链接 |
| **编程式导航（路径）** | `$router.push('/login')` | 登录成功/登出后跳转 |
| **编程式导航（参数）** | `$router.push('/blogList/postDetail/' + id)` | 点击文章卡片进入详情 |
| **返回** | `$router.back()` | 详情页返回列表 |
| **重定向** | `redirect: '/category'` | 根路径跳转到默认页 |
| **守卫拦截** | `return '/login'` | 未登录时跳到登录页 |

## 九、进阶优化方向

### 9.1 路由懒加载

当项目变大时，一次性加载所有页面组件会影响首屏速度。可以用动态导入实现按需加载：

```javascript
// 当前方式：全部导入（eager loading）
import BlogList from "@/SightseerViews/ResourceSites/BlogList.vue"

// 优化方式：动态导入（lazy loading）
const BlogList = () => import("@/SightseerViews/ResourceSites/BlogList.vue")
```

项目目前是**全部静态导入**，20 个页面组件在首屏就被打包在一起。懒加载可以将每个页面单独打包，访问时才加载：

```javascript
const paths = [
    {
        path: "/blogList",
        component: () => import("@/SightseerViews/ResourceSites/BlogList.vue")
    },
    {
        path: "/blogList/postDetail/:id",
        component: () => import("@/SightseerViews/ResourceSites/PostDetail.vue")
    },
]
```

### 9.2 页面过渡动画

目前 `<RouterView>` 是直接切换的，没有过渡效果。可以添加：

```html
<template>
    <main>
        <RouterView v-slot="{ Component }">
            <Transition name="fade" mode="out-in">
                <component :is="Component" />
            </Transition>
        </RouterView>
    </main>
</template>

<style>
.fade-enter-active, .fade-leave-active {
    transition: opacity 0.3s ease;
}
.fade-enter-from, .fade-leave-to {
    opacity: 0;
}
</style>
```

### 9.3 路由元信息扩展

在 `meta` 中存储页面标题，通过 `afterEach` 统一更新 `document.title`：

```javascript
// 路由配置
{
    path: "/blogList",
    component: BlogList,
    meta: { title: "博客列表" }
}

// 全局后置守卫
router.afterEach((to) => {
    document.title = to.meta.title || "WPEnglish"
})
```

### 9.4 404 页面

如果用户访问了一个不存在的路径，可以添加一个 404 路由：

```javascript
{
    path: "/:pathMatch(.*)*",
    component: NotFound
}
```

使用带正则的 `:pathMatch(.*)*` 匹配所有未定义的路径，显示自定义 404 页面。

## 十、总结

| 主题 | 要点 |
|------|------|
| **路由模式** | `createWebHistory()` 干净 URL，需要 Nginx 配合 |
| **路由分组** | 管理页（需登录）/ 游客页 / 博客，用 `meta` 区分 |
| **动态参数** | `:id` 传递博客文章 ID，`useRoute().params` 读取 |
| **导航守卫** | `beforeEach` 检查 localstorage token，无 token 跳登录 |
| **导航方式** | `RouterLink` / `$router.push` / `$router.back` / `redirect` |
| **注册方式** | `app.use(router)` 注入全局组件和能力 |
| **RouterView** | 动态渲染当前路由对应的组件 |
| **进阶方向** | 懒加载、过渡动画、页面标题、404 路由 |

Vue Router 作为 SPA 的"骨架"，连接了 URL 和页面组件。项目中的配置虽然简单，但覆盖了前端路由的**核心场景**：公开页面、需要登录的管理页面、带参数的动态路由、全局守卫拦截。这种清晰的分类和简单的架构，恰恰是最容易维护的。
