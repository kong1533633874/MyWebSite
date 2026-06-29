<template>
  <div class="resources">
    <div class="page-header">
      <h1>常用资源分享</h1>
      <p class="subtitle">收集前端、后端及其他实用开发资源</p>
      <el-input
        v-model="searchText"
        placeholder="搜索资源名称或描述..."
        prefix-icon="Search"
        clearable
        class="search-input"
      />
    </div>

    <div v-for="category in categories" :key="category.key" class="category-section">
      <div class="category-header">
        <span class="category-icon">{{ category.icon }}</span>
        <h2>{{ category.label }}</h2>
        <el-tag size="small" type="info" round>{{ filteredSites(category.key).length }}</el-tag>
      </div>
      <el-row :gutter="16">
        <el-col :xs="24" :sm="12" :md="8" v-for="site in filteredSites(category.key)" :key="site.id">
          <el-card shadow="hover" class="site-card" @click="openSite(site.url)">
            <div class="site-title">
              <img v-if="site.icon" :src="site.icon" width="20" height="20" class="site-icon" />
              <span>{{ site.title }}</span>
              <el-icon class="link-icon"><Link /></el-icon>
            </div>
            <p class="site-desc">{{ site.description }}</p>
            <div class="tags">
              <el-tag v-for="tag in site.tags" :key="tag" size="small" effect="plain" round>{{ tag }}</el-tag>
            </div>
          </el-card>
        </el-col>
      </el-row>
      <el-empty v-if="filteredSites(category.key).length === 0 && searchText" description="没有匹配的资源" :image-size="60" />
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { Link } from '@element-plus/icons-vue'
import { frontSites, backSites, elseSites } from '@/data/resources'

const searchText = ref('')

const categories = [
  { key: 'front', label: '前端', icon: '🎨' },
  { key: 'back', label: '后端', icon: '⚙️' },
  { key: 'other', label: '其他', icon: '📦' }
]

const siteMap = {
  front: frontSites,
  back: backSites,
  other: elseSites
}

const filteredSites = (key) => {
  const keyword = searchText.value.trim().toLowerCase()
  const sites = siteMap[key]
  if (!keyword) return sites
  return sites.filter(s =>
    s.title.toLowerCase().includes(keyword) ||
    s.description.toLowerCase().includes(keyword) ||
    s.tags.some(t => t.toLowerCase().includes(keyword))
  )
}

const openSite = (url) => {
  window.open(url, '_blank')
}
</script>

<style scoped>
.resources {
  max-width: 1200px;
  margin: 0 auto;
  padding: 20px;
}

.page-header {
  margin-bottom: 32px;
}

.page-header h1 {
  margin: 0 0 8px;
  font-size: 28px;
  color: #303133;
}

.subtitle {
  color: #909399;
  font-size: 14px;
  margin: 0 0 20px;
}

.search-input {
  max-width: 400px;
}

.category-section {
  margin-bottom: 36px;
}

.category-header {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 16px;
  padding-bottom: 12px;
  border-bottom: 2px solid #f0f0f0;
}

.category-header h2 {
  margin: 0;
  font-size: 20px;
  color: #303133;
}

.category-icon {
  font-size: 22px;
}

.site-card {
  cursor: pointer;
  margin-bottom: 16px;
  transition: transform 0.2s, box-shadow 0.2s;
  border-radius: 8px;
}

.site-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.1);
}

.site-title {
  display: flex;
  align-items: center;
  font-weight: 600;
  font-size: 15px;
  margin-bottom: 8px;
  color: #303133;
}

.site-icon {
  margin-right: 8px;
  border-radius: 4px;
}

.link-icon {
  margin-left: auto;
  color: #c0c4cc;
  font-size: 14px;
}

.site-card:hover .link-icon {
  color: #409eff;
}

.site-desc {
  color: #606266;
  font-size: 13px;
  line-height: 1.6;
  margin: 0 0 12px;
  min-height: 42px;
}

.tags .el-tag {
  margin-right: 4px;
  margin-bottom: 4px;
}
</style>
