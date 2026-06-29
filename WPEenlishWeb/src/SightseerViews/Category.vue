<template>
  <div class="category-page">
    <h2 class="page-title">选择分类</h2>
    <div class="category-grid">
      <div v-for="category in categories" :key="category.id" class="category-card">
        <RouterLink :to="{path:'/album',query:{categoryId:category.id}}" class="category-link">
          <img v-if="category.coverUrl" :src="category.coverUrl" alt="未找到图片" class="category-cover" referrerpolicy="no-referrer"/>
          <div v-else class="category-cover category-cover-placeholder">No Image</div>
          <div class="category-name">{{ category.title }}</div>
        </RouterLink>
      </div>
    </div>
  </div>
</template>

<script setup>
import request from '@/utils/request';
import { onMounted, ref } from 'vue';

const categories = ref([])

onMounted(async () =>{
    try{
      const response = await request.get("/main/category/FindAll")
      if(response.data.data){
        categories.value = response.data.data.filter(s=>!s.isDeleted)
      }
      else{
        categories.value= []
      }
    }catch(error){
        console.log("获取分类失败", error)
    }
})
</script>

<style scoped>
.category-page {
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

.category-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
  gap: 20px;
}

.category-card {
  background: var(--bg-card);
  border-radius: var(--radius-md);
  overflow: hidden;
  box-shadow: var(--shadow-card);
  border: 1px solid rgba(0, 0, 0, 0.04);
  transition: var(--transition-base);
}

.category-card:hover {
  box-shadow: var(--shadow-md);
  transform: translateY(-3px);
  border-color: rgba(99, 102, 241, 0.12);
}

.category-link {
  display: block;
  text-decoration: none;
  color: inherit;
}

.category-cover {
  width: 100%;
  height: 150px;
  object-fit: cover;
  display: block;
}

.category-cover-placeholder {
  background: linear-gradient(135deg, var(--primary-bg) 0%, #f0f0ff 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--text-muted);
  font-size: 13px;
}

.category-name {
  padding: 14px 18px;
  font-size: 15px;
  font-weight: 500;
  color: var(--text-primary);
}
</style>
