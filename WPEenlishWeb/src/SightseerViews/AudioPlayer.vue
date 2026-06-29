<template>
  <div class="player-page">
    <div class="player-card">
      <h2 class="episode-title">{{ state.episode.title }}</h2>
      <audio @timeupdate="showSubtitle" ref="audioRef" :src="state.episode.audioUrl" controls autoplay="true" class="audio-player" />
      
       <!-- 字幕区，包含切换按钮 -->
      <div class="subtitle-wrapper" ref="subtitleRef">
        <!-- 右上角切换按钮 -->
        <el-button
          class="subtitle-toggle-btn"
          :icon="subtitleVisible ? View : Hide"
          circle
          size="small"
          @click="toggleSubtitle"
          :title="subtitleVisible ? '隐藏字幕' : '显示字幕'"
        />

        <div v-if="subtitleVisible" class="subtitle-display" @click="addSubtitle">
          <div v-if="state.currentSentence?.value" class="current-sentence">
            {{ state.currentSentence.value }}
          </div>
          <div v-else class="sentence-placeholder">点击可收藏当前字幕</div>
        </div>
        <div v-else class="subtitle-hidden-placeholder">
        </div>
      </div>

      <!--收藏-->
      <div v-if="state.selectedSentences.length > 0" class="collected-section">
        <div class="collected-header">
          <span>已收藏</span>
          <button class="clear-btn" @click="clearSubtitle">清空</button>
        </div>
        <div class="collected-list">
          <div v-for="(item, index) in state.selectedSentences" :key="index"
            @click="jumpTo(item)" class="collected-item">
            {{ item.value }}
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import request from '@/utils/request';
import { ElMessage } from 'element-plus';
import { onMounted, reactive, ref } from 'vue';
import { useRouter } from 'vue-router';
import { View, Hide } from '@element-plus/icons-vue'

const router = useRouter()
const episodeId = router.currentRoute.value.query.episodeId
const subtitleVisible = ref(false)

const state = reactive({
    episode:{},
    currentSentence:null,
    selectedSentences:[]
})
const audioRef = ref()

onMounted(async()=>{
  try{
    const response = await request.get(`/main/episode/FindById?id=${episodeId}`)
    if(response.data.data){
        state.episode = response.data.data
    }
    else{
      state.episode = []
    }
  }catch(error){
      ElMessage.error("获取片段时失败,请稍后重试")
  }
})

function toggleSubtitle() {
  subtitleVisible.value = !subtitleVisible.value
}

function showSubtitle(){
  if (!state.episode.sentences || !audioRef.value) return
  const currentTime = audioRef.value.currentTime
  for (const s of state.episode.sentences) {
    if (s.startTime < currentTime && currentTime < s.endTime) {
        state.currentSentence = s
        break
    }
  }
}

function addSubtitle(){
  if (!state.currentSentence) return
  const exists = state.selectedSentences.some(item => item.startTime === state.currentSentence.startTime)
  if (!exists) {
      state.selectedSentences.push({ ...state.currentSentence })
  }
}

function jumpTo(item){
  if (audioRef.value) {
      audioRef.value.currentTime = item.startTime
  }
}

function clearSubtitle(){
    state.selectedSentences = []
}
</script>

<style scoped>
.player-page {
  max-width: 700px;
  margin: 0 auto;
}

.player-card {
  background: var(--bg-card);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-card);
  border: 1px solid rgba(0, 0, 0, 0.04);
  padding: 36px;
}

.episode-title {
  font-size: 22px;
  font-weight: 600;
  color: var(--text-primary);
  margin-bottom: 24px;
  letter-spacing: -0.3px;
}

.audio-player {
  width: 100%;
  margin-bottom: 24px;
  border-radius: var(--radius-sm);
}

.subtitle-display {
  padding: 20px 24px;
  background: linear-gradient(135deg, var(--primary-bg) 0%, #f0f0ff 100%);
  border-radius: var(--radius-md);
  cursor: pointer;
  margin-bottom: 24px;
  transition: var(--transition-base);
  border: 1px solid rgba(99, 102, 241, 0.06);
}

.subtitle-display:hover {
  background: var(--primary-bg-hover);
  border-color: rgba(99, 102, 241, 0.12);
}

.current-sentence {
  font-size: 16px;
  color: var(--primary);
  font-weight: 500;
  line-height: 1.7;
}

.sentence-placeholder {
  font-size: 14px;
  color: var(--text-muted);
}

.collected-section {
  border-top: 1px solid var(--border-color);
  padding-top: 20px;
}

.collected-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 14px;
  font-size: 14px;
  font-weight: 500;
  color: var(--text-secondary);
}

.clear-btn {
  background: none;
  border: none;
  color: var(--text-muted);
  cursor: pointer;
  font-size: 13px;
  transition: var(--transition-base);
}

.clear-btn:hover {
  color: var(--primary);
}

.collected-list {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.collected-item {
  padding: 8px 16px;
  background: var(--bg-hover);
  border-radius: var(--radius-sm);
  font-size: 13px;
  color: var(--text-primary);
  cursor: pointer;
  transition: var(--transition-base);
  border: 1px solid transparent;
}

.collected-item:hover {
  background: var(--primary-bg);
  color: var(--primary);
  border-color: rgba(99, 102, 241, 0.1);
}
</style>
