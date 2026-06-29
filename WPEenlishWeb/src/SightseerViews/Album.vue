<template>
  <div class="album-page">
    <h2 class="page-title">选择专辑</h2>
    <div class="album-list">
      <RouterLink v-for="album in albums" :key="album.id"
        :to="{path:'/episode',query:{alubmId:album.id}}" class="album-item">
        <span class="album-title">{{ album.title }}</span>
        <span class="album-arrow">→</span>
      </RouterLink>
    </div>
    <div v-if="albums && albums.length === 0" class="empty-tip">暂无专辑</div>
  </div>
</template>

<script setup>
import request from '@/utils/request';
import { ElMessage } from 'element-plus';
import { onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';

const albums = ref(null)
const router = useRouter()
const categoryId = router.currentRoute.value.query.categoryId

onMounted(async()=>{
    try{
        const response = await request.get(`/main/album/FindAllByCategoryId?id=${categoryId}`)
        if(response.data.data){
            albums.value = response.data.data
        }
        else{
          albums.value = []
        }
    }catch(error){
        ElMessage.error("获取专辑时出现异常,请稍后重试")
    }
})
</script>

<style scoped>
.album-page {
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

.album-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.album-item {
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

.album-item:hover {
  background: var(--primary-bg);
  border-color: rgba(99, 102, 241, 0.12);
  transform: translateX(4px);
}

.album-title {
  font-size: 15px;
  font-weight: 500;
}

.album-arrow {
  color: var(--text-muted);
  font-size: 16px;
  transition: var(--transition-base);
}

.album-item:hover .album-arrow {
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
