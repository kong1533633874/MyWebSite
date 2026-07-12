<template>
  <footer class="site-footer">
    <div class="footer-inner">
      <div class="footer-brand">
        <RouterLink to="/" class="footer-logo">WPEnglish</RouterLink>
        <p class="footer-desc">专注于英语听力学习，提升你的语言能力</p>
      </div>
      <div class="footer-links">
        <div class="link-group">
          <h4>关于</h4>
          <RouterLink to="/info#about" class="footer-link">关于我们</RouterLink>
          <RouterLink to="/info#contact" class="footer-link">联系我们</RouterLink>
        </div>
        <div class="link-group">
          <h4>支持</h4>
          <RouterLink to="/info#legal" class="footer-link">免责声明</RouterLink>
          <RouterLink to="/info#legal" class="footer-link">隐私政策</RouterLink>
        </div>
        <div class="link-group">
          <h4>内容</h4>
          <RouterLink to="/blogList" class="footer-link">个人博客</RouterLink>
          <RouterLink to="/resources" class="footer-link">资源分享</RouterLink>
        </div>
      </div>
    </div>
    <div class="footer-bottom">
      <p>&copy; {{ new Date().getFullYear() }} WPEnglish. All rights reserved.</p>
    </div>
  </footer>
</template>

<script setup>
import { RouterLink } from 'vue-router'
</script>

<style scoped>
.site-footer {
  margin-top: auto;
  background: var(--bg-card);
  border-top: 1px solid var(--border-color);
}

.footer-inner {
  max-width: 1100px;
  margin: 0 auto;
  padding: 48px 32px 32px;
  display: flex;
  justify-content: space-between;
  gap: 48px;
  flex-wrap: wrap;
}

.footer-brand {
  max-width: 260px;
}

.footer-logo {
  font-size: 20px;
  font-weight: 700;
  color: var(--primary);
  text-decoration: none;
  transition: var(--transition-base);
}

.footer-logo:hover {
  color: var(--primary-dark);
}

.footer-desc {
  margin-top: 10px;
  font-size: 13px;
  color: var(--text-muted);
  line-height: 1.6;
}

.footer-links {
  display: flex;
  gap: 48px;
  flex-wrap: wrap;
}

.link-group h4 {
  font-size: 13px;
  font-weight: 600;
  color: var(--text-primary);
  text-transform: uppercase;
  letter-spacing: 0.5px;
  margin-bottom: 14px;
}

.link-group {
  display: flex;
  flex-direction: column;
  gap: 10px;
  min-width: 100px;
}

.footer-link {
  font-size: 13px;
  color: var(--text-secondary);
  text-decoration: none;
  transition: var(--transition-base);
}

.footer-link:hover {
  color: var(--primary);
}

.footer-bottom {
  border-top: 1px solid var(--border-light);
  text-align: center;
  padding: 20px 32px;
}

.footer-bottom p {
  font-size: 12px;
  color: var(--text-muted);
  margin: 0;
}
</style>
