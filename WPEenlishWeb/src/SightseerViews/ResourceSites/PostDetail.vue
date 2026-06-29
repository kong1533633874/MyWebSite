<template>
  <div class="post-detail" v-if="post">
    <h1>{{ post.title }}</h1>
    <div class="meta">
      <span>{{ post.date }}</span>
      <el-tag v-for="tag in post.tags" :key="tag" size="small" style="margin-left: 8px;">{{ tag }}</el-tag>
    </div>
    <div class="content markdown-body" v-html="renderedContent"></div>
    <el-button @click="$router.back()" type="primary" plain>← 返回列表</el-button>
  </div>
  <div v-else>
    <el-empty description="文章不存在" />
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { posts } from '@/data/post'
import { marked } from 'marked'

const route = useRoute()
const post = computed(() => posts.find(p => p.id == route.params.id))
const renderedContent = computed(() => {
  if (!post.value) return ''
  return marked(post.value.content)
})
</script>

<style scoped>
.meta {
  margin: 16px 0 24px;
  color: #666;
}
.content {
  line-height: 1.8;
  font-size: 16px;
}
</style>

<style>
/* Markdown 渲染样式 */
.markdown-body h1 { font-size: 1.8em; margin: 0.8em 0 0.4em; border-bottom: 1px solid #eee; padding-bottom: 0.3em; }
.markdown-body h2 { font-size: 1.5em; margin: 0.8em 0 0.4em; border-bottom: 1px solid #eee; padding-bottom: 0.3em; }
.markdown-body h3 { font-size: 1.25em; margin: 0.8em 0 0.4em; }
.markdown-body p { margin: 0.8em 0; }
.markdown-body ul, .markdown-body ol { padding-left: 2em; margin: 0.8em 0; }
.markdown-body blockquote { border-left: 4px solid #ddd; padding: 0 1em; color: #666; margin: 0.8em 0; background: #f9f9f9; }
.markdown-body code { background: #f4f4f4; padding: 2px 6px; border-radius: 3px; font-size: 0.9em; }
.markdown-body pre { background: #f4f4f4; padding: 1em; border-radius: 5px; overflow-x: auto; }
.markdown-body pre code { background: none; padding: 0; }
.markdown-body img { max-width: 100%; }
.markdown-body a { color: #409eff; }
.markdown-body table { border-collapse: collapse; width: 100%; margin: 0.8em 0; }
.markdown-body th, .markdown-body td { border: 1px solid #ddd; padding: 8px 12px; }
.markdown-body th { background: #f4f4f4; }
</style>