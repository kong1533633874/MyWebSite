# Vue 3 响应式原理与实践：ref、reactive、computed 从入门到精通

## 一、前言

Vue 3 最核心的特性就是**响应式系统**。简单说，就是"当数据变化时，页面自动更新"。

```html
<template>
  <p>{{ message }}</p>
  <button @click="message = '新消息'">更新</button>
</template>

<script setup>
import { ref } from 'vue'
const message = ref('你好，Vue 3！')
</script>
```

点击按钮后，`message` 变了，页面上的文字立刻更新——**不需要手动操作 DOM**。

这个机制是如何工作的？`ref` 和 `reactive` 有什么区别？`computed` 又是干什么的？本文结合项目中的真实代码，详细讲解 Vue 3 的响应式系统。

## 二、响应式系统的核心原理

### 2.1 Vue 2 的响应式：Object.defineProperty

Vue 2 通过 `Object.defineProperty` 拦截属性的 get/set 操作，实现响应式：

```javascript
// 简化版 Vue 2 响应式
Object.defineProperty(data, 'message', {
    get() {
        // 收集依赖（记录哪些地方用了这个数据）
        return value
    },
    set(newValue) {
        value = newValue
        // 通知更新（重新渲染）
    }
})
```

**局限性：**

| 问题 | 表现 |
|------|------|
| 无法检测新增属性 | `data.newProp = 'xxx'` 不是响应式的 |
| 无法检测数组变化 | `arr[0] = 'xxx'` 不是响应式的 |
| 需要递归遍历所有属性 | 性能开销大 |

### 2.2 Vue 3 的响应式：Proxy

Vue 3 改用 ES6 的 `Proxy` 代理整个对象：

```javascript
// 简化版 Vue 3 响应式
const proxy = new Proxy(data, {
    get(target, key) {
        // 收集依赖
        return target[key]
    },
    set(target, key, value) {
        target[key] = value
        // 通知更新
        return true
    }
})
```

**优势：**

| 能力 | 说明 |
|------|------|
| 动态增删属性 | `proxy.newProp = 'xxx'` 自动响应式 |
| 数组索引/长度变化 | `proxy.arr[0] = 'xxx'` 自动响应式 |
| 拦截操作更多 | 可拦截 `delete`、`in`、`has` 等 13 种操作 |
| 性能更好 | 不需递归遍历，访问到哪层才代理到哪层 |

## 三、ref —— 封装基本类型

### 3.1 基本用法

项目中，`ref` 被用在大量场景：

```javascript
// 基本类型
const isSorting = ref(false)       // 布尔值
const currentPage = ref(1)         // 数字
const searchText = ref('')         // 字符串

// 数组
const categories = ref([])         // 列表数据

// 模板引用
const formRef = ref()              // <el-form ref="formRef">
```

**为什么基本类型要用 ref 包裹？**

因为 JavaScript 的基本类型（string、number、boolean）是按值传递的，Proxy 只能代理对象，无法代理基本类型。`ref` 的本质是在内部创建一个 `{ value: ... }` 的结构，然后对这个对象使用 Proxy：

```javascript
// ref 的内部实现（简化）
function ref(value) {
    return reactive({ value: value })
}
```

所以在模板和 JS 中访问 ref 数据时，有个关键区别：

```html
<template>
  <!-- 模板中自动解包，不需要 .value -->
  <p>{{ currentPage }}</p>
</template>

<script setup>
import { ref } from 'vue'

const currentPage = ref(1)

// JS 中必须通过 .value 访问
console.log(currentPage.value)  // 1
currentPage.value++             // 修改
</script>
```

### 3.2 项目中的 ref 模式

项目中常见的列表数据模式：

```javascript
// Category.vue
const categories = ref([])

// 加载数据
const loadData = async () => {
    const res = await axios.get('/api/Category/FindAll')
    categories.value = res.data.data
}

// 模板中自动解包
// <div v-for="c in categories">{{ c.title }}</div>
```

## 四、reactive —— 封装对象

### 4.1 基本用法

`reactive` 接收一个对象，返回这个对象的响应式代理。项目中大多数表单和页面状态都用它：

```javascript
// 登录表单
const form = reactive({
    userName: '',
    password: ''
})

// 列表页状态
const state = reactive({
    tableData: []       // 表格数据
})

// 音频播放器状态
const state = reactive({
    episode: {},
    currentSentence: null,
    selectedSentences: []
})
```

### 4.2 项目中的登录状态管理

```javascript
// App.vue —— 应用的根组件
const loginState = reactive({
    isLoggedIn: !!localStorage.getItem('token')
})
```

这里有个巧妙的初始化方式：`!!localStorage.getItem('token')` 将 token 字符串转为布尔值——有 token 就是 `true`，没有就是 `false`。

### 4.3 ref vs reactive 怎么选？

| 场景 | 用 ref | 用 reactive |
|------|--------|------------|
| 单个基本类型 | ✅ `ref('你好')` | ❌ 不支持 |
| 普通对象 | ✅ `ref({ name: 'Vue' })` | ✅ `reactive({ name: 'Vue' })` |
| 表单数据 | 可以，但每个字段要 `.value` | ✅ 直接解构更自然 |
| 需要重新赋值 | ✅ `data.value = newVal` | ❌ 直接赋值会丢失响应式 |
| 模板引用 | ✅ `const divRef = ref()` | ❌ 不支持 |

**一个重要的区别：**

```javascript
// reactive：直接赋值会丢失响应式！
let state = reactive({ items: [] })
state = reactive({ items: [1, 2, 3] })  // ❌ 这是重新赋值，旧的响应式丢失了

// 正确方式：修改属性
state.items = [1, 2, 3]  // ✅

// ref：重新赋值就是修改 .value，不会有问题
const state = ref({ items: [] })
state.value = { items: [1, 2, 3] }  // ✅
```

所以项目中的模式是：**表单用 `reactive`**（字段多，不需要整体替换），**列表数据用 `ref([])`**（需要整体替换为新的数据）。

## 五、computed —— 计算属性

`computed` 根据已有数据**自动计算**出新值，并且有**缓存**——只有依赖的数据变了才会重新计算。

### 5.1 项目中的 computed 应用

**PostDetail.vue：从列表中查找当前文章**

```javascript
const post = computed(() => posts.find(p => p.id == route.params.id))
```

当 `route.params.id` 变化时（用户点击了另一篇文章），`post` 会自动重新计算。不需要在 `watch` 中手动监听 id 变化再更新数据。

**PostDetail.vue：将 Markdown 渲染为 HTML**

```javascript
const renderedContent = computed(() => {
    if (!post.value) return ''
    return marked(post.value.content)
})
```

这里 `computed` 做了两件事：
1. 自动跟随 `post` 的变化（post 变了说明换了文章）
2. 缓存渲染结果——如果 post 没变，不会重复调用 `marked()`

**BlogList.vue：分页截取**

```javascript
const currentPagePosts = computed(() => {
    const start = (currentPage.value - 1) * pageSize.value
    return posts.slice(start, start + pageSize.value)
})
```

当 `currentPage` 或 `pageSize` 变化时，自动计算当前页要显示的文章列表。

### 5.2 computed vs 方法

```html
<!-- 模板中 -->
<p>{{ getFullName() }}</p>     <!-- 方法：每次渲染都执行 -->
<p>{{ fullName }}</p>          <!-- computed：有缓存，依赖不变就不执行 -->
```

```javascript
// 方法：每次调用都会执行
function getFullName() {
    return firstName.value + ' ' + lastName.value
}

// computed：只有 firstName 或 lastName 变化时才重新计算
const fullName = computed(() => firstName.value + ' ' + lastName.value)
```

**项目中的选择：** 在两个细节页（PostDetail、BlogList）中使用了 `computed`，因为这些组件对性能更敏感——文章内容渲染和列表分页都是"频繁读取、少量变更"的场景，适合用计算属性缓存。

## 六、watch —— 侦听器

### 6.1 项目唯一的 watch

项目中只有一个 `watch`：

```javascript
// UserUpdate.vue
watch(() => form.newPassword, () => {
    formRef.value?.validateField('passwordConfirm')
})
```

当用户修改"新密码"字段时，自动重新验证"确认密码"字段——因为密码变了，之前验证通过的确认密码现在可能不匹配了。

### 6.2 watch 的三种写法

```javascript
// ① 侦听 ref
watch(count, (newVal, oldVal) => {
    console.log(`count 从 ${oldVal} 变为 ${newVal}`)
})

// ② 侦听 reactive 的属性（用 getter 函数）
watch(() => form.newPassword, (newVal) => {
    // 只有 form.newPassword 变化时触发
})

// ③ 侦听多个来源
watch([count, name], ([newCount, newName]) => {
    // 任意一个变化都触发
})
```

### 6.3 watch vs computed

| | computed | watch |
|--|---------|-------|
| 用途 | 派生新值 | 响应变化执行操作 |
| 返回值 | 有（自动计算的值） | 无（执行副作用） |
| 缓存 | 有 | 无 |
| 适合场景 | 数据转换、过滤、拼接 | 验证、同步、API 调用 |

## 七、provide / inject —— 跨组件状态共享

### 7.1 项目中的应用

项目没有使用 Pinia 或 Vuex，而是用 Vue 3 内置的 `provide` / `inject` 共享登录状态：

```javascript
// ===== App.vue（祖先组件） =====
const loginState = reactive({
    isLoggedIn: !!localStorage.getItem('token')
})

function setLoginStatus(loginedIn = true) {
    loginState.isLoggedIn = loginedIn
}

provide("setLoginStatus", setLoginStatus)

// ===== Login.vue（后代组件） =====
const setLoginStatus = inject("setLoginStatus")

// 登录成功时
localStorage.setItem("token", response.data.data)
setLoginStatus(true)  // ← 更新 App.vue 中的 loginState
```

### 7.2 provide / inject 的工作原理

```
App.vue           ← provide("setLoginStatus", fn)
  │
  ├─ RouterView
  │   │
  │   ├─ Login.vue    ← inject("setLoginStatus") ✅
  │   ├─ PostDetail.vue  ← inject("setLoginStatus") ✅（也能拿到）
  │   └─ ...
```

- `provide` 在祖先组件中提供数据
- `inject` 在后代组件中注入数据
- 无论嵌套多深，所有后代都能 `inject` 到祖先 `provide` 的内容

## 八、模板中的自动解包

Vue 3 的模板对响应式数据有"自动解包"机制：

```javascript
const count = ref(0)
const user = reactive({ name: 'Alice' })
```

```html
<template>
  <!-- ref 自动解包，不需要 .value -->
  <p>{{ count }}</p>

  <!-- reactive 也是直接访问属性 -->
  <p>{{ user.name }}</p>
</template>
```

但在 JS 中，必须通过 `.value` 访问 ref：

```javascript
// ❌ 错误
count + 1  // 结果是 "[object Object]1"

// ✅ 正确
count.value + 1
```

## 九、项目响应式数据统计

| API | 数量 | 主要用途 |
|-----|------|---------|
| `ref()` | 18 处 | 基本类型、列表数据、模板引用 |
| `reactive()` | 15 处 | 表单、状态对象、登录状态 |
| `computed()` | 3 处 | 文章查找、Markdown 渲染、分页 |
| `watch()` | 1 处 | 密码确认验证 |
| `provide` / `inject` | 2 处 | 跨组件登录状态 |
| `watchEffect` | 0 处 | — |
| `shallowRef` | 0 处 | — |
| `toRef` / `toRefs` | 0 处 | — |

**项目的选择很务实：**
- 表单用 `reactive`（字段多、不需要整体替换）
- 列表数据用 `ref([])`（需要整体替换为 API 返回数据）
- 派生数据用 `computed`（自动缓存，减少重复计算）
- 跨组件状态用 `provide` / `inject`（足够轻量，不需要引入 Pinia）

## 十、进阶话题

### 10.1 为什么没有用 Pinia？

项目中登录状态管理没有引入 Pinia 或 Vuex，而是用 `provide` / `inject` 配合 `localStorage`。这是因为：

1. 应用规模适中，全局状态很少（只有一个登录状态）
2. `provide` / `inject` 零依赖，足够满足需求
3. `localStorage` 持久化 token，刷新不丢失

如果项目规模扩大，全局状态增多，可以考虑引入 Pinia。

### 10.2 避免的常见陷阱

```javascript
// ① reactive 重新赋值会导致响应式丢失
let state = reactive({ count: 0 })
state = reactive({ count: 1 })  // ❌ 旧的响应式断开

// ② 解构 reactive 会丢失响应式
const state = reactive({ count: 0, name: 'Vue' })
const { count, name } = state   // ❌ count 和 name 不再是响应式的

// ③ ref 在模板外必须用 .value
const count = ref(0)
// JS 中
console.log(count.value)  // ✅
```

### 10.3 响应式调试

Vue 3 Devtools 可以直观地看到所有响应式数据，是排查响应式问题的最佳工具。

## 十一、总结

| 主题 | 要点 |
|------|------|
| **Vue 3 响应式原理** | 基于 Proxy，相比 Vue 2 的 Object.defineProperty 更强、更灵活 |
| **ref** | 用于基本类型，JS 中需 `.value`，模板中自动解包 |
| **reactive** | 用于对象，直接访问属性，但小心重新赋值和解构 |
| **computed** | 有缓存的派生数据，依赖不变就不重新计算 |
| **watch** | 响应变化执行副作用，适合验证、同步 |
| **provide / inject** | 跨层级组件通信，轻量级状态共享 |
| **选型原则** | 表单用 reactive、列表用 ref、派生用 computed、跨组件用 provide/inject |

Vue 3 的响应式系统让开发者从繁琐的 DOM 操作中解放出来——你只需要关心"数据是什么"，不需要关心"数据变了之后怎么更新页面"。
