<template>
  <div class="page-container">
    <div class="page-header">
      <h2 class="page-title">添加片段</h2>
    </div>
    <div class="card">
      <el-form :model="form" ref="formRef" :rules="rules" label-width="80px" style="max-width: 700px;">
        <el-form-item label="标题" prop="title">
          <el-input v-model="form.title" placeholder="请输入片段标题" />
        </el-form-item>
        <el-form-item label="音频" prop="audioUrl">
          <div class="upload-row">
            <el-input v-model="form.audioUrl" disabled placeholder="上传后自动填充" style="flex:1;" />
            <Uploader ref="uploaderRef" @custom-event="handleChildData" @duration-event="handleDuration"/>
          </div>
        </el-form-item>
        <el-form-item label="时长(秒)" prop="durationInSecond">
          <el-input v-model="form.durationInSecond" disabled style="max-width:200px;" />
        </el-form-item>
        <el-form-item label="字幕类型" prop="subtitleType">
          <div class="subtitle-type-row">
            <el-select v-model="form.subtitleType" placeholder="选择字幕类型" style="max-width:200px;">
              <el-option label="LRC" value="lrc" />
              <el-option label="SRT" value="srt" />
            </el-select>
            <el-button
              type="success"
              :icon="AISubtitleIcon"
              :loading="aiLoading"
              :disabled="!form.audioUrl || !form.subtitleType"
              @click="generateSubtitle"
            >
              {{ aiLoading ? 'AI 生成中...' : 'AI 生成字幕' }}
            </el-button>
          </div>
        </el-form-item>
        <el-form-item label="字幕内容" prop="subtitle">
          <el-input v-model="form.subtitle" type="textarea" :rows="8" />
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
import Uploader from '../components/Uploader.vue'
import { ElMessage } from 'element-plus';
import { MagicStick } from '@element-plus/icons-vue'
import ConfirmBox from '@/utils/ConfirmBox.js';

export default{
  components:{Uploader},
	setup(){
		const form = reactive({
            albumId:'',
            title:'',
            audioUrl:'',
            durationInSecond:0,
            subtitleType:'',
            subtitle:''
        })
		const router = useRouter()
		const uploaderRef = ref()
    const formRef = ref()
    const aiLoading = ref(false)
    const AISubtitleIcon = MagicStick

    form.albumId = router.currentRoute.value.query.albumId

    function handleChildData(data){
        form.audioUrl = data
    }

    function handleDuration(duration){
        form.durationInSecond = duration
    }

    const rules = {
      title:[{required:true,message:"必须填写标题",trigger:"blur"}],
      subtitleType:[{required:true,message:"必须选择字幕类型",trigger:"blur"}]
    }

    async function generateSubtitle(){
      if (!uploaderRef.value?.selectedFile) {
        ElMessage.warning("请先上传音频文件")
        return
      }
      if (!form.subtitleType) {
        ElMessage.warning("请先选择字幕类型")
        return
      }

      aiLoading.value = true
      try {
        const formData = new FormData()
        formData.append("audioFile", uploaderRef.value.selectedFile)
        formData.append("subtitleType", form.subtitleType)

        const response = await request.post("/admin/episode/GenerateSubtitle", formData, {
          timeout: 600000
        })

        const data = response.data.data
        form.subtitle = data.subtitle
        ElMessage.success(`AI 字幕生成成功！共 ${data.segmentsCount || 0} 句`)
      } catch (error) {
        console.error("AI 生成字幕异常:", error)
        const errMsg = error.response?.data?.message
          || error.response?.data?.title
          || error.message
          || "未知错误"
        const statusCode = error.response?.status
          ? `(HTTP ${error.response.status})`
          : "(网络错误)"
        ElMessage.error(`AI 字幕生成失败 ${statusCode}: ${errMsg}`)
      } finally {
        aiLoading.value = false
      }
    }

		async function add(){
      const confirm = await ConfirmBox("确定添加吗")
      if(!confirm) return
      await formRef.value.validate(async(valid)=>{
        if(valid){
          try{
              const response = await request.post("/admin/episode/Add",form)
              if(response.status === 200){
                  ElMessage.success("添加成功")
                  form.title=''
                  form.audioUrl=''
                  form.durationInSecond=0
                  form.subtitleType=''
                  form.subtitle=''
                  uploaderRef.value.clear()
              }
          }catch(error){
            ElMessage.error("添加失败, 请检查网络连接")
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
		return {form,rules,formRef,add,back,handleChildData,handleDuration,uploaderRef,aiLoading,AISubtitleIcon,generateSubtitle}
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

.subtitle-type-row {
  display: flex;
  align-items: center;
  gap: 12px;
  width: 100%;
}
</style>
