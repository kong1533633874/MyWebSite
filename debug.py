"""
调试版 - 一步步打印提取过程
"""
import requests
from bs4 import BeautifulSoup
from urllib.parse import urljoin

HEADERS = {"User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"}

url = "https://www.esl-lab.com/easy/"
print(f"1. 请求 {url}")
r = requests.get(url, timeout=30, headers=HEADERS)
r.encoding = 'utf-8'
print(f"   状态码: {r.status_code}, 大小: {len(r.text)} 字节\n")

soup = BeautifulSoup(r.text, 'lxml')

print("2. 查找所有 a 标签...")
all_a = soup.find_all('a', href=True)
print(f"   共 {len(all_a)} 个带 href 的 a 标签\n")

print("3. 筛选文章链接（/easy/xxx/ 格式且不是导航）...")
skip = ['/easy/', '/intermediate/', '/difficult/', '/basic-english/',
        '/academic-english/', '/interviews/', '/grammar/', '/stories/',
        '/games/', '/idioms/', '/english-culture-videos/', '/vocabulary-lessons/',
        '/esl-vocabulary-quizzes/', '/broadcasts/', '/randall-davis/',
        '/our-team/', '/faqs/', '/copyright_terms/', '/blog/',
        '/speaking-events/', '/first-time-users/', '/audio-help/',
        '/study-guide/', '/esl-study-handouts/']

articles = []
for a in all_a:
    href = a['href']
    text = a.get_text(strip=True)
    
    cond1 = href.startswith('/')
    cond2 = href.count('/') == 3
    cond3 = href.endswith('/')
    cond4 = href not in skip
    cond5 = text and len(text) > 3
    
    if cond1 and cond2 and cond3 and cond4 and cond5:
        articles.append((href, text, cond1, cond2, cond3, cond4, cond5))

print(f"   符合条件的: {len(articles)} 个\n")

if articles:
    print("前 10 个匹配到的链接:")
    for href, text, c1, c2, c3, c4, c5 in articles[:10]:
        print(f"  {href:45s} | {text[:40]}")
else:
    print("没有匹配到任何链接！检查条件...")
    print("\n显示 href 符合 /easy/xxx/ 格式的所有链接:")
    for a in all_a:
        href = a['href']
        text = a.get_text(strip=True)
        if href.startswith('/') and href.count('/') == 3 and href.endswith('/') and len(href) > 10:
            cond4 = href not in skip
            cond5 = text and len(text) > 3
            print(f"  {href:45s} | text={text[:30]:30s} | skip={'跳过' if not cond4 else '通过'} | text_len={'过短' if not cond5 else '通过'}")
