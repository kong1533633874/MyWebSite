<template>
  <div class="page-container">
    <div class="page-header">
      <h2 class="page-title">添加专辑</h2>
    </div>
    <div class="card">
      <el-form :model="form" ref="formRef" :rules="rules" label-width="80px" style="max-width: 500px;">
        <el-form-item label="标题" prop="name">
          <el-input v-model="form.name" placeholder="请输入专辑标题"/>
        </el-form-item>
        <el-form-item label="分类ID" prop="cId">
          <el-input v-model="form.cId" disabled />
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
      cId:''
    })
		const router = useRouter()
    const formRef = ref()

    form.cId = router.currentRoute.value.query.categoryId

    const rules = {
      name:[{required:true,message:"必须填写标题",trigger:"blur"}]
    }

		async function add(){
      const confirm = await ConfirmBox("确定添加吗")
      if(!confirm) return

			try{
				const response = await request.post("/admin/album/Add",{
					title:form.name,
					categoryId:form.cId
				})
				if(response.status === 200){
					form.name = ''
					ElMessage.success("添加成功")
				}
			}catch(error){
        ElMessage.error("添加失败,请检查网络连接")
			}
		}

		function back(){
			router.back()
		}
		return {form,formRef,rules,add,back}
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
