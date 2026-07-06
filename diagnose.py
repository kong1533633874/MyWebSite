"""
诊断脚本 - 先看看 ESL Lab 页面里到底有什么链接
"""
import requests
from bs4 import BeautifulSoup
from urllib.parse import urljoin

HEADERS = {"User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"}

url = "https://www.esl-lab.com/easy/"
r = requests.get(url, timeout=30, headers=HEADERS)
r.encoding = 'utf-8'

print(f"HTTP {r.status_code}, 页面大小: {len(r.text)} 字节")
print()

soup = BeautifulSoup(r.text, 'lxml')

# 找出所有链接
count = 0
for a in soup.find_all('a', href=True):
    href = a['href']
    text = a.get_text(strip=True)
    if text and len(text) > 3:
        count += 1
        if count <= 30:
            print(f"{href[:60]:60s} | {text[:40]}")

print(f"\n总共找到 {count} 个带文本的链接")
