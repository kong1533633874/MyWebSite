"""
诊断脚本2 - 找文章链接
"""
import requests
from bs4 import BeautifulSoup
from urllib.parse import urlparse

HEADERS = {"User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"}

url = "https://www.esl-lab.com/easy/"
r = requests.get(url, timeout=30, headers=HEADERS)
r.encoding = 'utf-8'
soup = BeautifulSoup(r.text, 'lxml')

# 找出所有链接，只看除了导航以外的
all_links = []
for a in soup.find_all('a', href=True):
    href = a['href']
    text = a.get_text(strip=True)
    if text and len(text) > 3:
        path = urlparse(href).path
        all_links.append((href, text, path))

# 按路径长度排序，文章通常有更深的路径
print("=== 路径较深的链接（可能是文章）===")
for href, text, path in all_links:
    if path.count('/') >= 3 and len(path) > 20:
        print(f"{path:50s} | {text[:50]}")

print(f"\n=== 共 {len(all_links)} 个链接 ===")

# 看看有没有藏在 h2/h3/h4 里面的链接
print("\n=== h2/h3/h4 内的链接 ===")
for h in ['h2','h3','h4']:
    for tag in soup.find_all(h):
        a = tag.find('a')
        if a and a.get('href'):
            print(f"{tag.name:5s} | {a['href'][:60]:60s} | {a.get_text(strip=True)[:50]}")
