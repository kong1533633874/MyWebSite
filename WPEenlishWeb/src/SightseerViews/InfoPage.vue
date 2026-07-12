<template>
  <div class="info-page">
    <!-- 关于我们 -->
    <section id="about" class="section-block">
      <h2 class="section-title">📖 关于我们</h2>
      <el-card shadow="hover">
        <p class="section-desc">
          WPEnglish 是一个专注于英语听力学习的在线平台。我们提供丰富的英语听力资源，
          涵盖多种主题和难度层级，帮助学习者在真实的语境中提升听力理解能力。
        </p>
        <p class="section-desc">
          平台的核心功能包括音频播放与 LRC 歌词字幕同步、句子收藏与复习、
          以及分类清晰的学习内容管理，让英语学习更加系统高效。
        </p>
        <el-divider />
        <h3>✨ 平台特色</h3>
        <ul class="feature-list">
          <li><strong>🎵 音频 + 字幕同步</strong> — 支持 LRC/SRT 字幕格式，听清每一个单词</li>
          <li><strong>📚 分类学习</strong> — 按主题和难度分组，轻松找到适合的材料</li>
          <li><strong>💾 句子收藏</strong> — 收藏感兴趣的句子，随时回顾复习</li>
          <li><strong>📝 原创博客</strong> — 分享英语学习经验、技巧和资源推荐</li>
        </ul>
      </el-card>
    </section>

    <!-- 联系我们 -->
    <section id="contact" class="section-block">
      <h2 class="section-title">📬 联系我们</h2>
      <el-card shadow="hover">
        <p class="section-desc">如有任何问题或建议，欢迎通过以下邮箱与我们联系：</p>
        <div class="email-row">
          <el-icon :size="22" color="#6366f1"><Message /></el-icon>
          <a :href="'mailto:' + email" class="email-link">{{ email }}</a>
        </div>
      </el-card>
    </section>

    <!-- 免责声明 & 隐私政策（折叠面板） -->
    <section id="legal" class="section-block">
      <h2 class="section-title">⚖️ 法律说明</h2>

      <el-collapse v-model="activeLegal">
        <el-collapse-item title="免责声明" name="disclaimer">
          <div class="legal-content">
            <h4>1. 内容免责</h4>
            <p>本网站所提供的英语听力学习材料、文章及资源均来源于网络公开资源或用户投稿，仅供学习交流使用，不用于任何商业目的。</p>

            <h4>2. 版权声明</h4>
            <p>本网站原创内容的版权归 WPEnglish 所有。转载内容均会标注来源，如无意中侵犯了您的合法权益，请通过<a href="#contact">联系我们</a>告知，我们将在核实后尽快处理。</p>

            <h4>3. 外部链接</h4>
            <p>本网站可能包含指向第三方网站的链接。我们对第三方网站的内容、准确性或安全性不承担责任。</p>

            <h4>4. 使用风险</h4>
            <p>用户在使用本网站服务时，应自行承担相关风险。我们不对因使用本网站服务产生的任何直接或间接损失承担责任。</p>

            <h4>5. 用户行为</h4>
            <p>用户不得利用本网站从事任何违法违规活动。对于用户的违规行为，我们保留追究法律责任的权利。</p>

            <h4>6. 免责声明更新</h4>
            <p>我们保留随时修改本免责声明的权利。继续使用本网站即表示您接受更新后的条款。</p>

            <p class="legal-date">最后更新日期：2026 年 6 月</p>
          </div>
        </el-collapse-item>

        <el-collapse-item title="隐私政策" name="privacy">
          <div class="legal-content">
            <h4>1. 信息收集</h4>
            <p>当您使用本网站时，我们可能会收集账户信息（用户名、邮箱）、使用数据（浏览页面、收藏、学习进度）以及基本的设备匿名信息。</p>

            <h4>2. 信息使用</h4>
            <p>收集的信息用于提供和维护服务、改善用户体验、与您沟通以及保障账户安全。</p>

            <h4>3. 信息存储与安全</h4>
            <p>我们采取合理的技术措施保护您的个人信息安全。账户密码经过加密处理，我们无法获取您的原始密码。</p>

            <h4>4. 信息共享</h4>
            <p>我们不会将您的个人信息出售或交易给第三方，除非获得您的明确同意或法律要求。</p>

            <h4>5. Cookie 使用</h4>
            <p>本网站可能使用 Cookie 来记录登录状态和偏好设置。您可以在浏览器中选择禁用 Cookie。</p>

            <h4>6. 用户权利</h4>
            <p>您有权访问、修改或删除您的个人信息，以及要求删除您的账户及相关数据。</p>

            <h4>7. 政策更新</h4>
            <p>我们可能会不时更新本隐私政策。继续使用本网站即表示您同意更新后的政策。</p>

            <p class="legal-date">最后更新日期：2026 年 6 月</p>
          </div>
        </el-collapse-item>
      </el-collapse>
    </section>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { Message } from '@element-plus/icons-vue'

const email = '1533633874@qq.com'
const activeLegal = ref(['disclaimer'])
</script>

<style scoped>
.info-page {
  max-width: 860px;
  margin: 0 auto;
}

.section-block {
  margin-bottom: 40px;
}

.section-title {
  font-size: 22px;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0 0 16px;
  letter-spacing: -0.3px;
}

.section-desc {
  font-size: 14px;
  color: var(--text-secondary);
  line-height: 1.8;
  margin: 0 0 8px;
}

/* 联系我们 */
.email-row {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-top: 4px;
}

.email-link {
  font-size: 16px;
  font-weight: 500;
  color: var(--primary);
  text-decoration: none;
  transition: var(--transition-base);
}

.email-link:hover {
  color: var(--primary-dark);
  text-decoration: underline;
}

/* 平台特色 */
.feature-list {
  list-style: none;
  padding: 0;
  margin: 8px 0 0;
}

.feature-list li {
  font-size: 14px;
  color: var(--text-secondary);
  line-height: 1.8;
  padding: 6px 0;
}

.feature-list li strong {
  color: var(--text-primary);
}

/* 法律折叠面板 */
.legal-content {
  padding: 4px 0;
}

.legal-content h4 {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 16px 0 6px;
}

.legal-content h4:first-child {
  margin-top: 0;
}

.legal-content p {
  font-size: 13px;
  color: var(--text-secondary);
  line-height: 1.8;
  margin: 0 0 4px;
}

.legal-content a {
  color: var(--primary);
  text-decoration: none;
}

.legal-content a:hover {
  text-decoration: underline;
}

.legal-date {
  color: var(--text-muted) !important;
  font-size: 12px !important;
  margin-top: 20px !important;
}
</style>
