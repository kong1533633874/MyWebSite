"""
English Audio Downloader — 英语音频下载器
使用已验证的 ESL Lab 文章链接，下载 MP3 + 生成 LRC 字幕
"""

import requests, re, os, json, uuid, hashlib, time, sys
from datetime import datetime, timezone
from urllib.parse import urljoin
from xml.etree import ElementTree

# ═══ 配置 ═══
MAX_MB = 5 * 1024        # 5GB 限制
MP3_DIR = "mp3"           # 音频存放目录
LRC_DIR = "lrc"           # 字幕存放目录
OUTPUT_JSON = "data.json" # 中间数据（供 data_importer.py 导入）
REQUEST_DELAY = 2.0

HEADERS = {"User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"}

session = requests.Session()
session.headers.update(HEADERS)
total_bytes = 0
total_files = 0

TED_LIMIT_PER_TALK = 5 * 1024 * 1024  # TED 每个音频最大 5MB（有些演讲太长）

# ═══ 工具 ═══

def sz(n):
    """bytes → 可读大小"""
    for u in ['B', 'KB', 'MB', 'GB']:
        if n < 1024: return f"{n:.1f}{u}"
        n /= 1024
    return f"{n:.1f}TB"

def get(url, timeout=30, referer=None):
    time.sleep(REQUEST_DELAY)
    try:
        headers = {}
        if referer:
            headers['Referer'] = referer
            headers['Origin'] = 'https://www.esl-lab.com'
        r = session.get(url, timeout=timeout, headers=headers)
        if r.status_code in (200, 206):
            return r
    except:
        pass
    return None

def safe_filename(name):
    """安全文件名"""
    return re.sub(r'[<>:"/\\|?*]', '_', name)[:100]

# ═══ 源1: ESL Lab — 已验证可用的文章链接 ═══

def crawl_esl_lab():
    """ESL Lab 文章页面 → 提取 MP3 URL → 下载"""
    global total_bytes, total_files
    
    # 已验证可用的 ESL Lab 文章链接（从诊断脚本获得）
    verified_urls = [
        # Easy 级别
        "https://www.esl-lab.com/easy/introductions-names/",
        "https://www.esl-lab.com/easy/making-online-friends/",
        "https://www.esl-lab.com/easy/meeting-coworkers/",
        "https://www.esl-lab.com/easy/meeting-new-neighbors/",
        "https://www.esl-lab.com/easy/personal-profile/",
        "https://www.esl-lab.com/easy/school-schedule/",
        "https://www.esl-lab.com/easy/family-recreation/",
        "https://www.esl-lab.com/easy/school-activities/",
        "https://www.esl-lab.com/easy/daily-schedule/",
        "https://www.esl-lab.com/easy/evening-routine/",
        "https://www.esl-lab.com/easy/morning-routine/",
        "https://www.esl-lab.com/easy/household-chores/",
        "https://www.esl-lab.com/easy/family-activities/",
        "https://www.esl-lab.com/easy/family-relationships/",
        "https://www.esl-lab.com/easy/answering-machine/",
        "https://www.esl-lab.com/easy/college-graduation/",
        "https://www.esl-lab.com/easy/bookstore-shopping/",
        "https://www.esl-lab.com/easy/clothing-styles/",
        "https://www.esl-lab.com/easy/office-supplies/",
        "https://www.esl-lab.com/easy/shopping-centers/",
        "https://www.esl-lab.com/easy/spending-money/",
        "https://www.esl-lab.com/easy/gourmet-cooking/",
        "https://www.esl-lab.com/easy/pie-restaurant/",
        "https://www.esl-lab.com/easy/restaurant-order/",
        "https://www.esl-lab.com/easy/apartments-for-rent/",
        "https://www.esl-lab.com/easy/hotel-reservations/",
        "https://www.esl-lab.com/easy/hotel-room-service/",
        "https://www.esl-lab.com/easy/immigration-customs/",
        "https://www.esl-lab.com/easy/sightseeing-tours/",
        "https://www.esl-lab.com/easy/tokyo-travel-guide/",
        "https://www.esl-lab.com/easy/drugs-medication/",
        "https://www.esl-lab.com/easy/health-insurance/",
        "https://www.esl-lab.com/easy/physical-therapy/",
        "https://www.esl-lab.com/easy/christmas-gifts/",
        "https://www.esl-lab.com/easy/happy-birthday/",
        "https://www.esl-lab.com/easy/happy-new-year/",
        "https://www.esl-lab.com/easy/holiday-traditions/",
        "https://www.esl-lab.com/easy/party-invitations/",
        "https://www.esl-lab.com/easy/casino-gambling/",
        "https://www.esl-lab.com/easy/dvd-movie-rentals/",
        "https://www.esl-lab.com/easy/missing-children/",
        # 还有几个需要补充的
        "https://www.esl-lab.com/easy/health-club/",
        "https://www.esl-lab.com/easy/first-date/",
        "https://www.esl-lab.com/easy/weekend-plans/",
        "https://www.esl-lab.com/easy/train-tickets/",
        "https://www.esl-lab.com/easy/snack-time/",
        "https://www.esl-lab.com/easy/new-friends/",
        "https://www.esl-lab.com/easy/coffee-shop/",
    ]
    
    # 去重
    verified_urls = list(dict.fromkeys(verified_urls))
    
    print(f"\n📡 ESL Lab — {len(verified_urls)} 个已验证链接")
    results = []
    
    for i, page_url in enumerate(verified_urls):
        if total_bytes >= MAX_MB * 1024 * 1024:
            print("  ⛔ 已达 5GB 上限")
            break
            
        print(f"  [{i+1}/{len(verified_urls)}] ", end="")
        
        resp = get(page_url)
        if not resp:
            print("❌ 页面不可达")
            continue
        
        html = resp.text
        
        # 从页面文本提取 MP3 URL（ESL Lab 会在页面中明文显示 MP3 地址）
        m = re.search(r'(https?://[^\s<>"\']+\.mp3[^\s<>"\']*)', html)
        if not m:
            print("❌ 找不到 MP3")
            continue
        
        mp3_url = m.group(1).rstrip("'\" ")
        print(f"MP3: {mp3_url.split('/')[-1]} ", end="")
        
        # 提取页面标题
        title_m = re.search(r'<title>(.+?)</title>', html, re.I)
        title = title_m.group(1) if title_m else "ESL Lab"
        title = title.replace(" - Randall's ESL Cyber Listening Lab", "").strip()
        title = title.replace("General Listening Quiz", "").strip()
        title = title.replace('\u201c', '').replace('\u201d', '').strip()
        if not title:
            title = f"ESL Lab #{i+1}"
        
        # 提取文本内容
        text = ""
        text_m = re.search(r'<div[^>]*class="[^"]*entry-content[^"]*"[^>]*>(.+?)</div>', html, re.S | re.I)
        if not text_m:
            text_m = re.search(r'<main[^>]*>(.+?)</main>', html, re.S | re.I)
        if text_m:
            # 去掉 HTML 标签
            text = re.sub(r'<[^>]+>', ' ', text_m.group(1))
            text = re.sub(r'\s+', ' ', text).strip()
        
        if not text or len(text) < 80:
            # 从 body 提取
            body = re.search(r'<body[^>]*>(.+?)</body>', html, re.S | re.I)
            if body:
                clean = re.sub(r'<script[^>]*>.*?</script>', '', body.group(1), flags=re.S|re.I)
                clean = re.sub(r'<style[^>]*>.*?</style>', '', clean, flags=re.S|re.I)
                clean = re.sub(r'<[^>]+>', ' ', clean)
                clean = re.sub(r'\s+', ' ', clean).strip()
                if len(clean) > 200:
                    text = clean
        
        # 生成 LRC
        filename = safe_filename(title)
        lrc_path = os.path.join(LRC_DIR, f"{filename}.lrc")
        os.makedirs(LRC_DIR, exist_ok=True)
        
        # 生成简单 LRC 格式（每行一个时间戳 + 文本行）
        lines = [l.strip() for l in text.split('\n') if l.strip()]
        lrc_lines = []
        t = 0
        for line in lines:
            # 每行分配约 5 秒
            mm = t // 60
            ss = t % 60
            lrc_lines.append(f"[{mm:02d}:{ss:02d}.00]{line}")
            t += 5
        
        with open(lrc_path, 'w', encoding='utf-8') as f:
            f.write('\n'.join(lrc_lines))
        
        # 下载 MP3
        mp3_name = safe_filename(mp3_url.split('/')[-1])
        if not mp3_name.endswith('.mp3'):
            mp3_name += '.mp3'
        mp3_path = os.path.join(MP3_DIR, mp3_name)
        os.makedirs(MP3_DIR, exist_ok=True)
        
        if not os.path.exists(mp3_path):
            dl = get(mp3_url, referer=page_url)
            if dl:
                size = len(dl.content)
                if total_bytes + size > MAX_MB * 1024 * 1024:
                    print(f"⛔ 超预算 ({sz(total_bytes + size)})，存URL")
                    mp3_path = mp3_url  # 不下载，存 URL
                else:
                    with open(mp3_path, 'wb') as f:
                        f.write(dl.content)
                    total_bytes += size
                    total_files += 1
                    print(f"✅ {sz(size)} | 累计 {sz(total_bytes)}")
            else:
                print(f"⚠️ 下载失败，存 URL")
                mp3_path = mp3_url
        else:
            size = os.path.getsize(mp3_path)
            total_bytes += size
            print(f"✅ 已存在 {sz(size)} | 累计 {sz(total_bytes)}")
        
        results.append({
            "category": "ESL Lab 分级听力",
            "album": "ESL Lab - 日常英语",
            "title": title,
            "audio_url": mp3_path,
            "lrc_file": lrc_path,
            "subtitle": text,
        })
    
    return results


# ═══ 源2: TED Talks RSS（演讲英语，适合托福/学术） ═══

def crawl_ted():
    global total_bytes, total_files
    
    print(f"\n📡 TED Talks...")
    results = []
    
    rss = get("https://www.ted.com/talks/rss", timeout=30)
    if not rss:
        print("  ❌ TED RSS 不可达")
        return results
    
    try:
        root = ElementTree.fromstring(rss.content)
    except:
        print("  ❌ 解析失败")
        return results
    
    items = root.findall('.//item')
    if not items:
        items = root.findall('.//{http://www.w3.org/2005/Atom}entry')
    
    print(f"  RSS 中找到 {len(items)} 条")
    
    for i, item in enumerate(items[:20]):  # 只取前 20 个 TED 演讲
        if total_bytes >= MAX_MB * 1024 * 1024:
            break
            
        title_el = item.find('title') if item.find('title') is not None else item.find('{http://www.w3.org/2005/Atom}title')
        link_el = item.find('link') if item.find('link') is not None else item.find('{http://www.w3.org/2005/Atom}link')
        
        if title_el is None:
            continue
        
        talk_url = ""
        if hasattr(link_el, 'get') and link_el.get('href'):
            talk_url = link_el.get('href')
        elif link_el is not None and link_el.text:
            talk_url = link_el.text
        
        title = title_el.text.strip() if title_el.text else f"TED #{i+1}"
        
        # 尝试获取 transcript
        slug = re.search(r'/talks/([^/?#]+)', talk_url)
        transcript = ""
        if slug:
            trans_url = f"https://www.ted.com/talks/{slug.group(1)}/transcript.json"
            tr = get(trans_url)
            if tr:
                try:
                    data = tr.json()
                    if isinstance(data, list):
                        texts = [p['text'] for p in data if isinstance(p, dict) and 'text' in p]
                        transcript = ' '.join(texts)
                except:
                    pass
        
        if not transcript or len(transcript) < 80:
            print(f"  [{i+1}] ❌ 无字幕: {title[:40]}")
            continue
        
        # 生成 LRC
        filename = safe_filename(title)
        lrc_path = os.path.join(LRC_DIR, f"{filename}.lrc")
        os.makedirs(LRC_DIR, exist_ok=True)
        
        words = transcript.split()
        lrc_lines = []
        t = 0
        chunk = []
        for w in words:
            chunk.append(w)
            if len(chunk) >= 8:
                mm, ss = t // 60, t % 60
                lrc_lines.append(f"[{mm:02d}:{ss:02d}.00]{' '.join(chunk)}")
                chunk = []
                t += 3
        if chunk:
            mm, ss = t // 60, t % 60
            lrc_lines.append(f"[{mm:02d}:{ss:02d}.00]{' '.join(chunk)}")
        
        with open(lrc_path, 'w', encoding='utf-8') as f:
            f.write('\n'.join(lrc_lines))
        
        # TED 音频（存页面 URL，因为 TED 视频没有直接 MP3 下载）
        print(f"  [{i+1}] ✅ {title[:40]}... | 字幕 {len(transcript)} 字")
        
        results.append({
            "category": "TED 演讲",
            "album": "TED 学术英语",
            "title": title,
            "audio_url": talk_url,  # TED 无直接 MP3
            "lrc_file": lrc_path,
            "subtitle": transcript,
        })
    
    return results


# ═══ 生成 SQL ═══

def generate_output(all_data):
    """生成 SQL 和汇总信息"""
    lines = [
        f"-- English Audio Data Import",
        f"-- 生成时间: {datetime.now(timezone.utc).strftime('%Y-%m-%d %H:%M:%S')}",
        f"-- 音频文件: {total_files} 个, 共 {sz(total_bytes)}",
        "",
        "USE [WPEnglish]; GO",
        "BEGIN TRANSACTION; GO",
        "DECLARE @Now datetime2 = GETUTCDATE();",
        "",
    ]
    
    from collections import defaultdict
    cat_groups = defaultdict(list)
    for d in all_data:
        cat_groups[d['category']].append(d)
    
    cat_guids = {c: str(uuid.uuid4()).upper() for c in cat_groups}
    
    # 分类
    for seq, (cat, _) in enumerate(cat_groups.items(), 1):
        lines.append(
            f"INSERT INTO [T_Categories] ([Id],[SequenceNumber],[Title],[CoverUrl],[IsDeleted],[CreateTime]) "
            f"VALUES (N'{cat_guids[cat]}',{seq},N'{cat}',NULL,0,@Now); GO"
        )
    
    # 专辑 + 单集
    ep_total = 0
    for cat, items in cat_groups.items():
        album_id = str(uuid.uuid4()).upper()
        album_name = items[0].get('album', f'{cat} 精选')
        lines.append(
            f"INSERT INTO [T_Albums] ([Id],[Title],[IsVisible],[SequenceNumber],[CategoryId],[CreatedTime],[IsDeleted]) "
            f"VALUES (N'{album_id}',N'{album_name}',1,1,N'{cat_guids[cat]}',@Now,0); GO"
        )
        
        for seq, d in enumerate(items, 1):
            ep_id = str(uuid.uuid4()).upper()
            ep_total += 1
            audio = d['audio_url'].replace("'", "''")
            sub = d['subtitle'].replace("'", "''")[:8000]  # 限制长度
            
            lines.append(
                f"INSERT INTO [T_Episodes] ([Id],[Title],[AlbumId],[SequenceNumber],[IsVisible],[AudioUrl],"
                f"[DurationInSecond],[Subtitle],[SubtitleType],[CreateTime],[IsDeleted]) "
                f"VALUES (N'{ep_id}',N'{d['title'].replace(chr(39), chr(39)+chr(39))}',N'{album_id}',{seq},1,"
                f"N'{audio}',NULL,N'{sub}',N'text',@Now,0); GO"
            )
    
    lines.append("COMMIT; GO")
    lines.append("SELECT 'T_Categories' [Table],COUNT(*) [Rows] FROM [T_Categories]")
    lines.append("UNION ALL SELECT 'T_Albums',COUNT(*) FROM [T_Albums]")
    lines.append("UNION ALL SELECT 'T_Episodes',COUNT(*) FROM [T_Episodes]; GO")
    
    sql = '\n'.join(lines)
    with open("english_data.sql", 'w', encoding='utf-8') as f:
        f.write(sql)
    
    print(f"\n✅ SQL 已生成: english_data.sql ({os.path.getsize('english_data.sql')/1024:.1f}KB)")
    print(f"   共计 {ep_total} 条 Episode")


# ═══ 主入口 ═══

def main():
    print("=" * 60)
    print("English Audio Downloader — 英语音频下载器")
    print(f"预算: {MAX_MB}MB | 数据源: ESL Lab + TED Talks")
    print("=" * 60)
    
    all_data = []
    
    # ESL Lab（日常/四六级英语）
    all_data.extend(crawl_esl_lab())
    
    # TED Talks（学术/托福英语）
    all_data.extend(crawl_ted())
    
    # 汇总
    print(f"\n{'='*60}")
    print(f"下载完成: {total_files} 个音频, {sz(total_bytes)}")
    print(f"数据条数: {len(all_data)}")
    
    if not all_data:
        print("❌ 没采集到数据")
        return
    
    # 统计
    from collections import Counter
    cats = Counter(d['category'] for d in all_data)
    for cat, n in cats.items():
        print(f"  {cat}: {n} 条")
    
    # 生成 SQL
    generate_output(all_data)
    
    print()
    print("上传到服务器：")
    print(f"  scp -r {MP3_DIR}/ your-user@your-server:/var/www/audio/")
    print(f"  scp -r {LRC_DIR}/ your-user@your-server:/var/www/subtitles/")
    print(f"  scp english_data.sql your-user@your-server:/tmp/")
    print(f"  sqlcmd -S 127.0.0.1 -U sa -P '密码' -d WPEnglish -i /tmp/english_data.sql -C")


if __name__ == '__main__':
    main()
