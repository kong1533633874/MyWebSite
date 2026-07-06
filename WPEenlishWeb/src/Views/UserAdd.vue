<template>
  <div class="page-container">
    <div class="page-header">
      <h2 class="page-title">注册用户</h2>
    </div>
    <div class="card">
      <el-form ref="formRef" :rules="rules" :model="form" label-width="80px" style="max-width: 500px;">
        <el-form-item label="用户名" prop="userName">
          <el-input v-model="form.userName" placeholder="请输入用户名" />
        </el-form-item>
        <el-form-item label="密码" prop="password">
          <el-input v-model="form.password" show-password placeholder="请输入密码" />
        </el-form-item>
        <el-form-item label="确认密码" prop="passwordConfirm">
          <el-input v-model="form.passwordConfirm" show-password placeholder="请再次输入密码" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="add">创建</el-button>
          <el-button @click="back">返回</el-button>
        </el-form-item>
      </el-form>
    </div>
  </div>
</template>

<script setup lang="js">
import request from '@/utils/request';
import { useRouter } from 'vue-router'
import { reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';
import ConfirmBox from '@/utils/ConfirmBox';

const form = reactive({
    userName:'',
    password:'',
    passwordConfirm:''
})
const router = useRouter()
const formRef = ref(null)

const validatePasswordConfirm = (rule, value, callback)=>{
    if(value === ''){
        callback(new Error("请再次输入密码"))
    }
    else if(value != form.password){
        callback(new Error("两次输入的密码不一致"))
    }
    else{
        callback()
    }
}

const validateUserPasswordLength = (rule, value, callback)=>{
  if(value.length < 6 || value.length > 20){
    callback(new Error("密码请输入6位至20位"))
  }
  else{
    callback()
  }
}

const validateUserNameLength = (rule, value, callback)=>{
  if(value.length < 2 || value.length > 20){
    callback(new Error("用户名请输入2位至20位"))
  }
  else{
    callback()
  }
}

const rules = {
    userName:[{required:true, message:"请输入用户名", trigger:"blur"},
      {validator:validateUserNameLength,trigger:'blur'}],
    password:[{required:true, message:'请输入密码', trigger:"blur"},
      {validator:validateUserPasswordLength, trigger: 'blur'}],
    passwordConfirm:[{required:true, message:'请确认密码', trigger:"blur"},
      {validator:validatePasswordConfirm, trigger: 'blur'},
      {validator:validateUserPasswordLength, trigger: 'blur'}
    ]
}

async function add(){
    const confirm = await ConfirmBox("确定添加吗")
    if(!confirm) return
    await formRef.value.validate(async(valid) => {
        if(valid){
            try{
                const response = await request.post("/identity/Add",{
                    userName:form.userName,
                    password:form.password
                })
                if(response.status === 200){
                    ElMessage.success("添加成功")
                    router.back()
                }
            }catch(error){
                ElMessage.error("添加失败,请检查网络连接")
            }
        }
        else{
            ElMessage.error("表单验证失败")
            return false
        }
    })
}

function back(){
    router.back()
}
</script>

<style scoped>
.page-container {
  max-width: 960px;
  margin: 0 auto;
}

.page-header {
  margin-bottom: 24px;
}

.page-title {
  font-size: 22px;
  font-weight: 600;
  color: var(--text-primary);
  letter-spacing: -0.3px;
}

.card {
  background: var(--bg-card);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-card);
  border: 1px solid rgba(0, 0, 0, 0.04);
  padding: 28px;
}
</style>
