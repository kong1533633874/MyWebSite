<template>
  <div class="login-wrapper">
    <div class="login-card">
      <h2 class="login-title">WPEnglish</h2>
      <p class="login-subtitle">英语听力学习平台</p>
      <el-form :model="form" label-width="0" class="login-form">
        <el-form-item>
          <el-input v-model="form.userName" placeholder="用户名" size="large" />
        </el-form-item>
        <el-form-item>
          <el-input v-model="form.password" type="password" placeholder="密码" show-password size="large" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" size="large" @click="Login()" class="login-btn">登录</el-button>
        </el-form-item>
      </el-form>
    </div>
  </div>
</template>

<script setup>
import { reactive } from 'vue'
import { useRouter } from 'vue-router';
import request from '@/utils/request';
import { inject } from 'vue';
import { ElMessage } from 'element-plus';

const form = reactive({
	userName: '',
	password: ''
})
const router = useRouter()
const setLoginStatus = inject("setLoginStatus")

const Login = async () => {
	try{
		const response = await request.post("/identity/Login",form)
		if(response.status == 200){
			localStorage.setItem("token",response.data.data)
      setLoginStatus()
			ElMessage.success("登录成功")
			router.push("/categoryList")
		}
	}catch(error){
		console.log("登录异常",error)
    ElMessage.error("登录异常")
	}
}

const Add = async () => {
	router.push("/userAdd")
}

</script>

<style scoped>
.login-wrapper {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: calc(100vh - var(--nav-height) - 64px);
  background:
    radial-gradient(ellipse at 20% 50%, rgba(99,102,241,0.06) 0%, transparent 50%),
    radial-gradient(ellipse at 80% 20%, rgba(129,140,248,0.05) 0%, transparent 50%);
}

.login-card {
  width: 400px;
  padding: 48px 40px;
  background: var(--bg-card);
  border-radius: var(--radius-xl);
  box-shadow: var(--shadow-lg);
  border: 1px solid rgba(0, 0, 0, 0.03);
}

.login-title {
  font-size: 26px;
  font-weight: 700;
  color: var(--primary);
  text-align: center;
  margin-bottom: 4px;
  letter-spacing: -0.5px;
}

.login-subtitle {
  font-size: 14px;
  color: var(--text-muted);
  text-align: center;
  margin-bottom: 36px;
}

.login-form :deep(.el-input__wrapper) {
  border-radius: var(--radius-sm);
  box-shadow: 0 0 0 1px var(--border-color) inset;
  transition: var(--transition-base);
}

.login-form :deep(.el-input__wrapper:hover) {
  box-shadow: 0 0 0 1px var(--primary-light) inset;
}

.login-form :deep(.el-input__wrapper.is-focus) {
  box-shadow: 0 0 0 1px var(--primary) inset;
}

.login-btn {
  width: 100%;
  border-radius: var(--radius-sm);
  height: 42px;
  font-weight: 500;
  font-size: 15px;
  letter-spacing: 0.3px;
}

.login-footer {
  text-align: center;
  font-size: 13px;
  color: var(--text-muted);
  margin-top: 4px;
}
</style>
