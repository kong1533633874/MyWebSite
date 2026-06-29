<template>
    <div class="uploader">
        <label class="upload-area">
            <input ref="inputRef" type="file" accept="audio/*" class="upload-input" @change="selectFile"/>
            <span v-if="!selectedFile" class="upload-text">选择音频文件</span>
            <span v-else class="upload-text">{{ selectedFile.name }}</span>
        </label>
        <el-button type="primary" size="small" @click="upload" :disabled="!selectedFile">上传</el-button>
        <span v-show="showProgress" class="progress-text">{{ percent }}%</span>
    </div>
</template>

<script>
import { ref } from 'vue';
import request from '@/utils/request';
import { ElMessage } from 'element-plus';

export default{
    emits:["custom-event","duration-event"],
    setup(props,{emit}){
        const percent = ref(0)
        const inputRef = ref()
        const audioUrl = ref()
        const showProgress = ref(false)
        const selectedFile = ref(null);

        function selectFile(e){
            const file = e.target.files[0]

            if(e.target.files.length <= 0){
                ElMessage.warning("请选择音频文件");
                return;
            }

            if(!file){
                ElMessage.warning("请选择音频文件");
                return;
            }

            if(!file.type.startsWith('audio/'))
            {
                ElMessage.warning('请选择音频文件');
                inputRef.value.value = '';
                selectedFile.value = null;
                return;
            };

            selectedFile.value = file

            // 计算音频时长
            const audio = new Audio()
            audio.src = URL.createObjectURL(file)
            audio.addEventListener('loadedmetadata', () => {
                const duration = Math.round(audio.duration * 100) / 100
                emit('duration-event', duration)
                URL.revokeObjectURL(audio.src)
            })
        }

        function clear(){
            if(inputRef.value){
                inputRef.value.value = ''
            }
            selectedFile.value = null
            percent.value = 0
            showProgress.value = false
        }

        async function upload(){
            if (!selectedFile.value) {
                ElMessage.warning('请先选择音频文件');
                return;
            }

            try{
                const formdata = new FormData()
                formdata.append("file",selectedFile.value)

                const response = await request.post("/file/Upload",formdata,{
                    onUploadProgress: (progressEvent) => {
                        percent.value = Math.round((progressEvent.loaded * 100) / progressEvent.total);
                        showProgress.value = true
                    }
                })
                if(response.status == 200){
                    audioUrl.value = response.data.data
                    emit('custom-event', audioUrl.value);
                }
            }catch(error){
                console.log("出现异常: " , error)
                ElMessage.error("出现异常错误,请稍后重试")
            }
        }

        return {showProgress, percent, inputRef, selectedFile, selectFile, upload, clear}
    }
}
</script>

<style scoped>
.uploader {
  display: flex;
  align-items: center;
  gap: 10px;
}

.upload-area {
  display: inline-flex;
  align-items: center;
  padding: 0 14px;
  height: 32px;
  background: var(--bg-hover);
  border: 1px solid var(--border-color);
  border-radius: var(--radius-sm);
  cursor: pointer;
  transition: var(--transition-base);
}

.upload-area:hover {
  border-color: var(--primary-light);
  background: var(--primary-bg);
}

.upload-input {
  display: none;
}

.upload-text {
  font-size: 13px;
  color: var(--text-secondary);
  max-width: 200px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.progress-text {
  font-size: 13px;
  color: var(--primary);
  font-weight: 500;
}
</style>
