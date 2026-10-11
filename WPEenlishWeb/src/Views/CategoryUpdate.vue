<template>
  <div class="page-container">
    <div class="page-header">
      <h2 class="page-title">编辑分类</h2>
    </div>
    <div class="card">
      <el-form :model="form" ref="formRef" :rules="rules" label-width="80px" style="max-width: 500px;">
        <el-form-item label="标题" prop="title">
          <el-input v-model="form.title" placeholder="请输入分类标题" />
        </el-form-item>
        <el-form-item label="封面" prop="coverUrl">
          <div class="upload-row">
            <el-input v-model="form.coverUrl" disabled placeholder="不上传则保留原封面" />
            <ImageUploader @custom-event="handleCoverUrl" />
          </div>
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="update">提交</el-button>
          <el-button @click="back">返回</el-button>
        </el-form-item>
      </el-form>
    </div>
  </div>
</template>

<script >
import { reactive, ref } from 'vue';
import { useRouter } from 'vue-router';
import request from '@/utils/request';
import { ElMessage } from 'element-plus';
import ConfirmBox from '@/utils/ConfirmBox';
import ImageUploader from '@/components/ImageUploader.vue';

export default{
  components:{ImageUploader},
    setup(){
      const router = useRouter()
      const form = reactive({
        title:'',
        coverUrl:''
      })
      const formRef = ref()

      form.title = router.currentRoute.value.query.title
      form.coverUrl = router.currentRoute.value.query.coverUrl || ''

      const rules = {
        title:[{required:true,message:"必须填写标题",trigger:"blur"}]
      }

      function handleCoverUrl(url){
        form.coverUrl = url
      }

      async function update(){
        const confirm = await ConfirmBox("确定要更新吗")
        if(!confirm) return

        await formRef.value.validate(async(valid)=>{
          if(valid){
            try{
              const response = await request.put("/admin/category/Update",{
                id:router.currentRoute.value.query.id,
                title:form.title,
                coverUrl:form.coverUrl || null
              })
              if(response.status === 200){
                ElMessage.success("更新成功")
                back()
              }
            }catch(error){
                ElMessage.error("更新失败")
            }
          }
          else{
            ElMessage.error("表单验证失败")
          }
        })

      }

      function back(){
        router.back()
      }

      return {form,rules,back,update,formRef,handleCoverUrl}
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

.upload-row {
  display: flex;
  align-items: center;
  gap: 12px;
  width: 100%;
}
</style>
