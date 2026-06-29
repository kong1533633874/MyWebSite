<template>
  <div class="page-container">
    <div class="page-header">
      <h2 class="page-title">添加分类</h2>
    </div>
    <div class="card">
      <el-form :model="form" :rules="rules" ref="formRef" label-width="80px" style="max-width: 500px;">
        <el-form-item label="标题" prop="name">
          <el-input v-model="form.name" placeholder="请输入分类标题" />
        </el-form-item>
        <el-form-item label="封面URL" prop="path">
          <el-input v-model="form.path" placeholder="请输入图片路径" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="add">提交</el-button>
          <el-button @click="back">返回</el-button>
        </el-form-item>
      </el-form>
    </div>
  </div>
</template>

<script>
import request from '@/utils/request';
import {useRouter} from 'vue-router'
import { reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';
import ConfirmBox from '@/utils/ConfirmBox';
export default{
	setup(){
    const form = reactive({
      name:'',
      path:''
    })
		const router = useRouter()
    const formRef = ref()

    const rules = {
    name:[{required:true, message:"必须填写标题", trigger:"blur"}]
    }

		async function add(){
        const confirm =  await ConfirmBox("确定要提交吗")
        if(!confirm) return

        await formRef.value.validate(async(valid) => {
          if(valid){
            try{
                const response = await request.post("/admin/category/Add",{
                  title:form.name,
                  coverUrl:form.path
                })
                if(response.status === 200){
                  form.name =''
                  form.path =''
                  ElMessage.success("添加成功")
                  back()
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
		return {form,rules,add,back,formRef}
	}
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
