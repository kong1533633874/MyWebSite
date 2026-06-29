<template>
  <div class="episode-page">
    <h2 class="page-title">选择片段</h2>
    <div class="episode-list">
      <RouterLink v-for="episode in episodes" :key="episode.id"
        :to="{path:'/audio',query:{episodeId:episode.id}}" class="episode-item">
        <span class="episode-title">{{ episode.title }}</span>
        <span class="episode-arrow">→</span>
      </RouterLink>
    </div>
    <div v-if="episodes && episodes.length === 0" class="empty-tip">暂无片段</div>
  </div>
</template>

<script setup>
import request from '@/utils/request';
import { ElMessage } from 'element-plus';
import { onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';

const episodes = ref(null)
const router = useRouter()
const alubmId = router.currentRoute.value.query.alubmId

onMounted(async() =>{
    try{
      const response = await request.get(`/main/episode/FindAllByAlbumId?id=${alubmId}`)
      if(response.data.data){
        episodes.value = response.data.data
      }
      else{
        episodes.value = []
      }
    }catch(error){
        ElMessage.error("获取片段列表失败,请检查网络连接")
    }
})
</script>

<style scoped>
.episode-page {
  max-width: 960px;
  margin: 0 auto;
}

.page-title {
  font-size: 22px;
  font-weight: 600;
  color: var(--text-primary);
  margin-bottom: 24px;
  letter-spacing: -0.3px;
}

.episode-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.episode-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 24px;
  background: var(--bg-card);
  border-radius: var(--radius-md);
  box-shadow: var(--shadow-card);
  border: 1px solid rgba(0, 0, 0, 0.04);
  text-decoration: none;
  color: var(--text-primary);
  transition: var(--transition-base);
}

.episode-item:hover {
  background: var(--primary-bg);
  border-color: rgba(99, 102, 241, 0.12);
  transform: translateX(4px);
}

.episode-title {
  font-size: 15px;
  font-weight: 500;
}

.episode-arrow {
  color: var(--text-muted);
  font-size: 16px;
  transition: var(--transition-base);
}

.episode-item:hover .episode-arrow {
  color: var(--primary);
  transform: translateX(2px);
}

.empty-tip {
  text-align: center;
  color: var(--text-muted);
  padding: 48px 0;
  font-size: 14px;
}
</style>
