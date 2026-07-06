"""
VOA Learning English 爬虫脚本
==============================
抓取 VOA Learning English 的音频 + 字幕数据，
生成 INSERT SQL 脚本导入 WPEnglish 数据库。

使用方法：
    pip install requests beautifulsoup4 lxml
    python voa_crawler.py

输出：voa_data.sql（可直接在 sqlcmd 中执行）
"""

import uuid
import requests
from datetime import datetime, timezone
from bs4 import BeautifulSoup
from urllib.parse import urljoin
import re
import time
import html

# ============================================================
# 配置
# ============================================================

# 请求头，模拟浏览器
HEADERS = {
    "User-Agent": (
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) "
        "AppleWebKit/537.36 (KHTML, like Gecko) "
        "Chrome/120.0.0.0 Safari/537.36"
    )
}

# VOA 分类定义（名称 → 列表页 URL）
CATEGORIES = {
    "As It Is": "https://learningenglish.voanews.com/z/4451",
    "Health & Lifestyle": "https://learningenglish.voanews.com/z/4453",
    "Science & Technology": "https://learningenglish.voanews.com/z/4454",
    "Education": "https://learningenglish.voanews.com/z/4452",
    "Arts & Culture": "https://learningenglish.voanews.com/z/4455",
    "Everyday Grammar": "https://learningenglish.voanews.com/z/4357",
    "News Words": "https://learningenglish.voanews.com/z/4730",
}

# 每类抓取最近多少篇文章
MAX_ARTICLES_PER_CATEGORY = 10

# 请求间隔（秒），别把 VOA 服务器打崩
REQUEST_DELAY = 1.5

# 输出文件
OUTPUT_FILE = "voa_data.sql"

# 生成的 Id 前缀（方便追踪哪些数据是爬虫导入的）
# 实际上我们会用 uuid.uuid4() 生成随机 GUID


# ============================================================
# 工具函数：生成 SQL 值
# ============================================================

def sql_str(value: str | None) -> str:
    """将字符串转成 SQL N'...' 格式，处理转义"""
    if value is None:
        return "NULL"
    # 转义单引号：两个单引号
    escaped = value.replace("'", "''")
    return f"N'{escaped}'"


def sql_guid() -> str:
    """生成 SQL Server uniqueidentifier 格式的 GUID"""
    return str(uuid.uuid4()).upper()


def sql_datetime() -> str:
    """生成当前 UTC 时间的 SQL datetime2 字符串"""
    return datetime.now(timezone.utc).strftime("'%Y-%m-%d %H:%M:%S'")


def extract_mp3_url(soup: BeautifulSoup, base_url: str) -> str | None:
    """从 VOA 文章页提取 MP3 音频 URL"""
    # 方式1: 查找 download-link 类的 a 标签
    download_link = soup.find("a", class_="download-link")
    if download_link and download_link.get("href"):
        return urljoin(base_url, download_link["href"])

    # 方式2: 查找 data-audiourl 属性
    audio_el = soup.find(attrs={"data-audiourl": True})
    if audio_el:
        return urljoin(base_url, audio_el["data-audiourl"])

    # 方式3: 查找 audio 标签内的 source
    audio_tag = soup.find("audio")
    if audio_tag:
        source = audio_tag.find("source")
        if source and source.get("src"):
            return urljoin(base_url, source["src"])

    # 方式4: 在 wsw__embedAudios 脚本或 data-audio 属性中找
    for script in soup.find_all("script"):
        if script.string and "mp3" in script.string.lower():
            match = re.search(r'(https?://[^"\']+\.mp3)', script.string)
            if match:
                return match.group(1)

    return None


def extract_transcript(soup: BeautifulSoup) -> str | None:
    """从 VOA 文章页提取字幕/全文文本"""
    # 方式1: 查找 class 包含 "transcript" 或 "article-content" 的区域
    for cls in ["transcript", "article-content", "content", "story-body"]:
        container = soup.find(class_=re.compile(cls, re.I))
        if container:
            # 移除内部脚本、样式
            for tag in container.find_all(["script", "style"]):
                tag.decompose()
            text = container.get_text(separator="\n").strip()
            if len(text) > 50:  # 至少要有实质性内容
                return text

    # 方式2: 查找 wsw__transcript 容器
    transcript_div = soup.find(id=re.compile(r"transcript", re.I))
    if transcript_div:
        for tag in transcript_div.find_all(["script", "style"]):
            tag.decompose()
        text = transcript_div.get_text(separator="\n").strip()
        if len(text) > 50:
            return text

    # 方式3: 从 article-text 中提取
    article_text = soup.find(class_="article-text")
    if article_text:
        for tag in article_text.find_all(["script", "style"]):
            tag.decompose()
        text = article_text.get_text(separator="\n").strip()
        if len(text) > 50:
            return text

    return None


def extract_duration(soup: BeautifulSoup) -> float | None:
    """从页面提取音频时长（秒），找不到则返回 None"""
    # 查找 class/span 包含 duration、time 字样的元素
    duration_el = soup.find(class_=re.compile(r"duration|time", re.I))
    if duration_el:
        text = duration_el.get_text(strip=True)
        # 匹配 mm:ss 或 hh:mm:ss 格式
        match = re.search(r'(?:(\d+):)?(\d+):(\d+)', text)
        if match:
            h = int(match.group(1)) if match.group(1) else 0
            m = int(match.group(2))
            s = int(match.group(3))
            return float(h * 3600 + m * 60 + s)
    return None


def extract_publish_date(soup: BeautifulSoup) -> str | None:
    """提取文章发布日期，返回 datetime2 字符串"""
    # 查找 time 标签
    time_tag = soup.find("time")
    if time_tag and time_tag.get("datetime"):
        try:
            dt = datetime.fromisoformat(time_tag["datetime"].replace("Z", "+00:00"))
            return f"'{dt.strftime('%Y-%m-%d %H:%M:%S')}'"
        except (ValueError, TypeError):
            pass

    # 查找 class 含 date 的元素
    date_el = soup.find(class_=re.compile(r"date|time", re.I))
    if date_el:
        date_text = date_el.get_text(strip=True)
        # 尝试解析 "DD Month YYYY" 格式
        match = re.search(r'(\d{1,2}\s+\w+\s+\d{4})', date_text)
        if match:
            try:
                dt = datetime.strptime(match.group(1), "%d %B %Y")
                return f"'{dt.strftime('%Y-%m-%d %H:%M:%S')}'"
            except ValueError:
                pass

    return None


# ============================================================
# 主爬虫逻辑
# ============================================================

def crawl_voa():
    """主函数：爬取 VOA 数据并生成 SQL 文件"""
    session = requests.Session()
    session.headers.update(HEADERS)

    # 收集到的数据
    articles_data = []  # list of dict: category, title, mp3_url, transcript, duration, publish_date, article_url

    print("=" * 60)
    print("VOA Learning English 爬虫")
    print("=" * 60)

    for cat_name, cat_url in CATEGORIES.items():
        print(f"\n📂 分类：{cat_name}")
        print(f"  列表页：{cat_url}")

        try:
            resp = session.get(cat_url, timeout=15)
            resp.encoding = "utf-8"
            if resp.status_code != 200:
                print(f"  ⚠️  HTTP {resp.status_code}，跳过")
                continue

            soup = BeautifulSoup(resp.text, "lxml")

            # 找到文章列表中的链接
            # VOA 列表页文章通常在 <a> 标签中，href 包含 /a/ 路径
            article_links = []
            for a_tag in soup.find_all("a", href=True):
                href = a_tag["href"]
                # 匹配 VOA 文章 URL 模式：/a/article-slug/id.html
                if re.search(r'/a/[^/]+/\d+\.html', href):
                    full_url = urljoin(cat_url, href)
                    if full_url not in [item["url"] for item in article_links]:
                        title_text = a_tag.get_text(strip=True)
                        if title_text and len(title_text) > 10:
                            article_links.append({"url": full_url, "title": title_text})

            # 去重后截取上限
            seen_urls = set()
            unique_links = []
            for link in article_links:
                if link["url"] not in seen_urls:
                    seen_urls.add(link["url"])
                    unique_links.append(link)

            links_to_fetch = unique_links[:MAX_ARTICLES_PER_CATEGORY]
            print(f"  找到 {len(unique_links)} 篇文章，抓取前 {len(links_to_fetch)} 篇")

            for idx, link in enumerate(links_to_fetch, 1):
                article_url = link["url"]
                print(f"    [{idx}/{len(links_to_fetch)}] {link['title'][:50]}...")

                try:
                    time.sleep(REQUEST_DELAY)  # 礼貌爬取
                    art_resp = session.get(article_url, timeout=15)
                    art_resp.encoding = "utf-8"
                    if art_resp.status_code != 200:
                        print(f"      ⚠️ HTTP {art_resp.status_code}，跳过")
                        continue

                    art_soup = BeautifulSoup(art_resp.text, "lxml")

                    # 提取数据
                    mp3_url = extract_mp3_url(art_soup, article_url)
                    transcript = extract_transcript(art_soup)
                    duration = extract_duration(art_soup)
                    pub_date = extract_publish_date(art_soup)

                    # 标题
                    title = link["title"]
                    # 如果页面有更完整的标题，用它
                    h1 = art_soup.find("h1")
                    if h1:
                        h1_text = h1.get_text(strip=True)
                        if len(h1_text) > len(title):
                            title = h1_text

                    if not mp3_url:
                        print(f"      ⚠️ 未找到 MP3 链接，跳过")
                        continue

                    if not transcript:
                        print(f"      ⚠️ 未找到字幕文本，跳过")
                        continue

                    articles_data.append({
                        "category": cat_name,
                        "title": html.unescape(title),
                        "mp3_url": mp3_url.strip(),
                        "transcript": transcript,
                        "duration": duration,
                        "publish_date": pub_date,
                        "article_url": article_url,
                    })

                    print(f"      ✅ {mp3_url.split('/')[-1][:40]} | 字幕 {len(transcript)} 字")

                except requests.RequestException as e:
                    print(f"      ❌ 请求失败：{e}")
                    continue

        except requests.RequestException as e:
            print(f"  ❌ 分类页请求失败：{e}")
            continue

    # ============================================================
    # 生成 SQL
    # ============================================================
    print(f"\n{'=' * 60}")
    print(f"共抓取 {len(articles_data)} 条有效数据，开始生成 SQL...")
    print(f"{'=' * 60}")

    sql_lines = [
        "-- ============================================================",
        "-- VOA Learning English 数据导入脚本",
        f"-- 生成时间：{datetime.now(timezone.utc).strftime('%Y-%m-%d %H:%M:%S')} UTC",
        f"-- 数据来源：VOA Learning English (https://learningenglish.voanews.com/)",
        "-- ============================================================",
        "",
        "USE WPEnglish;",
        "GO",
        "",
        "BEGIN TRANSACTION;",
        "GO",
        "",
        "DECLARE @Now datetime2 = GETUTCDATE();",
        "",
    ]

    # ---- 收集所有分类名（去重） ----
    categories = list(dict.fromkeys([d["category"] for d in articles_data]))

    # ---- 为每个分类生成 GUID（固定，后面专辑和文章引用它） ----
    cat_guids = {cat: sql_guid() for cat in categories}
    album_guids = {}  # (category, year_month) → guid

    seq_num = 0
    for cat_name in categories:
        seq_num += 1
        cat_id = cat_guids[cat_name]
        sql_lines.append(f"-- 分类：{cat_name}")
        sql_lines.append(
            f"INSERT INTO [T_Categories] ([Id], [SequenceNumber], [Title], [CoverUrl], "
            f"[IsDeleted], [CreateTime])"
        )
        sql_lines.append(
            f"VALUES ({sql_str(cat_id)}, {seq_num}, {sql_str(cat_name)}, "
            f"NULL, 0, @Now);"
        )
        sql_lines.append("GO")
        sql_lines.append("")

    # ---- 为每个分类下的文章分组到专辑（按月分专辑） ----
    # 按 (category, 年月) 分组
    from collections import defaultdict

    cat_month_groups = defaultdict(list)
    for d in articles_data:
        # 从发布日期提取年月
        ym = "unknown"
        if d["publish_date"]:
            try:
                dt_str = d["publish_date"].strip("'")
                dt = datetime.strptime(dt_str, "%Y-%m-%d %H:%M:%S")
                ym = dt.strftime("%Y-%m")
            except ValueError:
                ym = "unknown"
        cat_month_groups[(d["category"], ym)].append(d)

    album_seq = 0
    for (cat_name, ym), articles in cat_month_groups.items():
        album_seq += 1
        album_id = sql_guid()
        key = (cat_name, ym)
        album_guids[key] = album_id

        cat_id = cat_guids[cat_name]
        album_title = f"VOA {cat_name} - {ym}" if ym != "unknown" else f"VOA {cat_name}"

        sql_lines.append(f"-- 专辑：{album_title}")
        sql_lines.append(
            f"INSERT INTO [T_Albums] ([Id], [Title], [IsVisible], [SequenceNumber], "
            f"[CategoryId], [CreatedTime], [IsDeleted])"
        )
        sql_lines.append(
            f"VALUES ({sql_str(album_id)}, {sql_str(album_title)}, "
            f"1, {album_seq}, {sql_str(cat_id)}, @Now, 0);"
        )
        sql_lines.append("GO")
        sql_lines.append("")

        # ---- 插入该专辑下的所有文章 ----
        episode_seq = 0
        for d in articles:
            episode_seq += 1
            ep_id = sql_guid()

            duration_val = f"{d['duration']}" if d["duration"] else "NULL"
            pub_date_val = d["publish_date"] if d["publish_date"] else "@Now"

            sql_lines.append(f"-- 单集：{d['title']}")
            sql_lines.append(
                f"INSERT INTO [T_Episodes] ([Id], [Title], [AlbumId], [SequenceNumber], "
                f"[IsVisible], [AudioUrl], [DurationInSecond], [Subtitle], [SubtitleType], "
                f"[CreateTime], [IsDeleted])"
            )
            sql_lines.append(
                f"VALUES ({sql_str(ep_id)}, {sql_str(d['title'])}, "
                f"{sql_str(album_id)}, {episode_seq}, 1, "
                f"{sql_str(d['mp3_url'])}, {duration_val}, "
                f"{sql_str(d['transcript'])}, {sql_str('text')}, "
                f"{pub_date_val}, 0);"
            )
            sql_lines.append("GO")
            sql_lines.append("")

    # ---- 提交事务 ----
    sql_lines.append("COMMIT;")
    sql_lines.append("GO")
    sql_lines.append("")
    sql_lines.append("-- ============================================================")
    sql_lines.append("-- 导入完成！验证数据量")
    sql_lines.append("-- ============================================================")
    sql_lines.append("SELECT 'T_Categories' AS [Table], COUNT(*) AS [Rows] FROM [T_Categories]")
    sql_lines.append("UNION ALL")
    sql_lines.append("SELECT 'T_Albums', COUNT(*) FROM [T_Albums]")
    sql_lines.append("UNION ALL")
    sql_lines.append("SELECT 'T_Episodes', COUNT(*) FROM [T_Episodes]")
    sql_lines.append("ORDER BY [Table];")
    sql_lines.append("GO")

    # ---- 写入文件 ----
    sql_content = "\n".join(sql_lines)

    with open(OUTPUT_FILE, "w", encoding="utf-8") as f:
        f.write(sql_content)

    print(f"\n✅ SQL 文件已生成：{OUTPUT_FILE}")
    print(f"   - {len(categories)} 个分类")
    print(f"   - {len(album_guids)} 个专辑")
    print(f"   - {len(articles_data)} 条音频/字幕数据")
    print(f"\n📋 使用方式：")
    print(f"   /opt/mssql-tools18/bin/sqlcmd -S 127.0.0.1 -U sa -P '128639xuexi3.6' -d WPEnglish -i {OUTPUT_FILE} -C")


# ============================================================
# 入口
# ============================================================

if __name__ == "__main__":
    print("注意：请先确保已安装依赖包")
    print("  pip install requests beautifulsoup4 lxml")
    print()
    crawl_voa()
