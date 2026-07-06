<template>
  <div class="page-container">
    <div class="page-header">
      <h2 class="page-title">修改密码</h2>
    </div>
    <div class="card">
      <el-form ref="formRef" :rules="rules" :model="form" label-width="80px" style="max-width: 500px;">
        <el-form-item label="旧密码" prop="oldPassword">
          <el-input v-model="form.oldPassword" show-password placeholder="请输入旧密码" />
        </el-form-item>
        <el-form-item label="新密码" prop="newPassword">
          <el-input v-model="form.newPassword" show-password placeholder="请输入新密码" />
        </el-form-item>
        <el-form-item label="确认密码" prop="passwordConfirm">
          <el-input v-model="form.passwordConfirm" show-password placeholder="请再次输入新密码" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="edit">提交</el-button>
          <el-button @click="back">返回</el-button>
        </el-form-item>
      </el-form>
    </div>
  </div>
</template>

<script setup lang="js">
import request from '@/utils/request';
import { useRouter } from 'vue-router'
import { reactive, ref, watch } from 'vue';
import { ElMessage } from 'element-plus';
import ConfirmBox from '@/utils/ConfirmBox';

const form = reactive({
    oldPassword:'',
    newPassword:'',
    passwordConfirm:''
})
const router = useRouter()
const formRef = ref(null)

const validatePasswordConfirm = (rule, value, callback)=>{
    if(value === ''){
        callback(new Error("请再次输入密码"))
    }
    else if(value != form.newPassword){
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

const rules = {
    oldPassword:[{required:true, message:"请输入旧密码", trigger:"blur"}],
    newPassword:[{required:true, message:'请输入新密码', trigger:"blur"},
      {validator:validateUserPasswordLength,trigger:'blur'}
    ],
    passwordConfirm:[{required:true, message:'请确认密码', trigger:"blur"},
        {validator:validatePasswordConfirm, trigger: 'blur'},
        {validator:validateUserPasswordLength,trigger:'blur'}
    ]
}

watch(() => form.newPassword, () => {
  formRef.value?.validateField('passwordConfirm')
})

async function edit(){
    await formRef.value.validate(async(valid) => {
        if(valid){
            const confirm = await ConfirmBox("确定更改吗")
            if(!confirm) return

            try{
                const response = await request.put("/identity/UpdatePassword",{
                    id:router.currentRoute.value.query.id,
                    oldPassword:form.oldPassword,
                    newPassword:form.newPassword
                })
                if(response.status === 200){
                    ElMessage.success("更改密码成功")
                    back()
                }
            }catch(error){
                ElMessage.error("更改密码失败,请检查网络连接")
            }
        }
        else{
            ElMessage.error("表单验证失败,请重新填写")
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
