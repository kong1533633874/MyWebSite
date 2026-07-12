<template>
  <nav class="navbar">
    <div class="navbar-inner">
      <RouterLink to="/" class="navbar-brand">WPEnglish</RouterLink>
      <div class="navbar-links">
        <RouterLink to="/category" class="nav-link">浏览</RouterLink>
        <RouterLink to="/blogList" class="nav-link">个人博客</RouterLink>
        <RouterLink to="/resources" class="nav-link">资源分享</RouterLink>
        <RouterLink v-if="loginState.isLoggedIn" to="/categoryList" class="nav-link">管理</RouterLink>
        <RouterLink to="/userList" v-if="loginState.isLoggedIn" class="nav-link">用户管理</RouterLink>
        <RouterLink v-if="!loginState.isLoggedIn" to="/login" class="nav-link">登录</RouterLink>
        <el-button v-else link size="small" class="logout-btn" @click="logout">
          <el-icon><SwitchButton /></el-icon>
          登出
        </el-button>
      </div>
    </div>
  </nav>

  <main class="main-content">
    <RouterView />
  </main>

  <SiteFooter />
</template>

<script setup lang="js">
import { ElMessage, ElMessageBox } from 'element-plus';
import { SwitchButton } from '@element-plus/icons-vue'; // 可选图标
import { provide, reactive } from 'vue';
import { RouterLink, RouterView } from 'vue-router';
import { useRouter } from 'vue-router';
import ConfirmBox from './utils/ConfirmBox';
import SiteFooter from './components/SiteFooter.vue';

const router = useRouter()

const loginState  = reactive({
  isLoggedIn: !!localStorage.getItem('token')
})

function setLoginStatus(loginedIn = true){
  loginState.isLoggedIn = loginedIn
}

async function logout(){
  const confirm = await ConfirmBox("是否退出当前账号")
  if(!confirm) return
  
  if(localStorage.getItem("token")){
    localStorage.setItem("token",'')
    setLoginStatus(false)
    ElMessage.success("账号已登出")
    router.push("/login")
  }
}

provide("setLoginStatus",setLoginStatus)
</script>

<style scoped>
.navbar {
  height: var(--nav-height);
  background: rgba(255, 255, 255, 0.72);
  backdrop-filter: blur(16px);
  -webkit-backdrop-filter: blur(16px);
  border-bottom: 1px solid rgba(0, 0, 0, 0.05);
  position: sticky;
  top: 0;
  z-index: 100;
}

.navbar-inner {
  max-width: 1100px;
  margin: 0 auto;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 32px;
}

.navbar-brand {
  font-size: 19px;
  font-weight: 700;
  color: var(--primary);
  letter-spacing: -0.3px;
  text-decoration: none;
  transition: var(--transition-base);
}

.navbar-brand:hover {
  color: var(--primary-dark);
}

.navbar-links {
  display: flex;
  align-items: center;
  gap: 4px;
}

.nav-link {
  padding: 7px 18px;
  border-radius: var(--radius-sm);
  font-size: 14px;
  font-weight: 450;
  color: var(--text-secondary);
  transition: var(--transition-base);
  text-decoration: none;
}

.nav-link:hover {
  background: var(--primary-bg);
  color: var(--primary);
}

.nav-link.router-link-active {
  background: var(--primary-bg);
  color: var(--primary);
  font-weight: 500;
}

.logout-btn {
  padding: 7px 18px !important;
  border-radius: var(--radius-sm) !important;
  font-size: 14px !important;
  font-weight: 450 !important;
  color: var(--text-secondary) !important;
  transition: var(--transition-base) !important;
  text-decoration: none !important;
  display: inline-flex !important;
  align-items: center !important;
  gap: 5px;
}

.logout-btn:hover {
  background: var(--primary-bg) !important;
  color: var(--primary) !important;
}

.main-content {
  flex: 1;
  max-width: 1100px;
  width: 100%;
  margin: 0 auto;
  padding: 32px;
}
</style>