<template>
  <div class="blog-list">
    <h1>博客</h1>
    <el-row :gutter="20">
      <el-col :span="8" v-for="post in currentPagePosts" :key="post.id">
        <el-card shadow="hover" @click="goToPost(post.id)" style="cursor: pointer; margin-bottom: 20px;">
          <template #header>
            <div class="card-header">
              <span>{{ post.title }}</span>
              <el-tag
                v-for="tag in post.tags"
                :key="tag"
                size="small"
                style="margin-left: 6px;"
              >{{ tag }}</el-tag>
            </div>
          </template>
          <p>{{ post.excerpt }}</p>
          <div class="post-date">{{ post.date }}</div>
        </el-card>
      </el-col>
    </el-row>
    <el-pagination
      v-model:current-page="currentPage"
      :page-size="pageSize"
      :total="posts.length"
      layout="prev, pager, next"
      background
      style="margin-top: 30px; justify-content: center;"
    />
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { posts } from '@/data/post'

const router = useRouter()
const currentPage = ref(1)
const pageSize = ref(6)

const currentPagePosts = computed(() => {
  const start = (currentPage.value - 1) * pageSize.value
  return posts.slice(start, start + pageSize.value)
})

const goToPost = (id) => {
  router.push(`/blogList/postDetail/${id}`)
}
</script>

<style scoped>
.card-header {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
}
.post-date {
  margin-top: 12px;
  color: #999;
  font-size: 13px;
}
</style>