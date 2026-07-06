"""
英语数据导入脚本：JSON/CSV → SQL Server INSERT
================================================
读取你手动整理的英语音频/字幕数据，自动生成 INSERT SQL。

使用方法：
    pip install pandas
    python data_importer.py

输入格式支持：
    1. JSON 文件 (data.json)    ← 推荐，结构清晰
    2. CSV 文件  (data.csv)

输出：import_data.sql（可直接在 sqlcmd 中执行）
"""

import json
import csv
import uuid
import os
import sys
from datetime import datetime, timezone
from collections import defaultdict

# ============================================================
# 配置
# ============================================================

INPUT_FILE = "data.json"       # 输入文件（JSON 或 CSV）
OUTPUT_FILE = "import_data.sql"

# ============================================================
# 实用函数
# ============================================================

def sql_str(value):
    """转义并包装为 N'...' 或 NULL"""
    if value is None or (isinstance(value, str) and value.strip() == ""):
        return "NULL"
    escaped = str(value).replace("'", "''")
    return f"N'{escaped}'"


def sql_guid():
    return str(uuid.uuid4()).upper()


def now_sql():
    return f"'{datetime.now(timezone.utc).strftime('%Y-%m-%d %H:%M:%S')}'"


# ============================================================
# 读取输入数据
# ============================================================

def load_data(filepath: str) -> list[dict]:
    """自动识别 JSON 或 CSV 并读取"""
    if not os.path.exists(filepath):
        print(f"❌ 文件不存在：{filepath}")
        print(f"   请先创建数据文件（参考下方格式）")
        sys.exit(1)

    ext = os.path.splitext(filepath)[1].lower()

    if ext == ".json":
        with open(filepath, "r", encoding="utf-8") as f:
            data = json.load(f)
        if isinstance(data, dict) and "episodes" in data:
            data = data["episodes"]
        if not isinstance(data, list):
            raise ValueError("JSON 必须是数组或包含 'episodes' 数组的对象")
        print(f"📖 从 JSON 读取到 {len(data)} 条记录")
        return data

    elif ext == ".csv":
        with open(filepath, "r", encoding="utf-8-sig") as f:
            reader = csv.DictReader(f)
            data = list(reader)
        print(f"📖 从 CSV 读取到 {len(data)} 条记录")
        return data

    else:
        raise ValueError(f"不支持的文件格式：{ext}，请使用 .json 或 .csv")


def validate_record(record: dict, idx: int) -> list[str]:
    """验证单条数据，返回错误列表"""
    errors = []
    if not record.get("title"):
        errors.append(f"[{idx}] 缺少 title（标题）")
    if not record.get("subtitle"):
        errors.append(f"[{idx}] 缺少 subtitle（字幕文本）")
    # audio_url 可选：有则必须有值
    # category 可选：默认 "General"
    # album 可选：默认按 category 分组
    return errors


# ============================================================
# 字段映射（兼容中英文/简写）
# ============================================================

FIELD_ALIASES = {
    "category": ["category", "分类", "类别", "cat"],
    "album": ["album", "专辑", "series", "系列", "集"],
    "title": ["title", "标题", "name", "名称", "episode", "集标题"],
    "audio_url": ["audio_url", "audiourl", "audio", "mp3", "mp3_url", "音频链接", "音频地址"],
    "subtitle": ["subtitle", "字幕", "transcript", "文本", "content", "内容", "正文"],
    "subtitle_type": ["subtitle_type", "subtitletype", "字幕类型", "type"],
    "duration_in_second": ["duration_in_second", "duration", "durationinsecond", "时长", "时长秒"],
    "cover_url": ["cover_url", "coverurl", "cover", "封面", "封面链接"],
}


def normalize_record(record: dict) -> dict:
    """将各种字段名映射为标准字段名"""
    normalized = {}
    reverse_map = {}
    for std_name, aliases in FIELD_ALIASES.items():
        for alias in aliases:
            reverse_map[alias] = std_name

    for key, value in record.items():
        k = key.strip().lower()
        if k in reverse_map:
            normalized[reverse_map[k]] = value
        else:
            normalized[k] = value

    # 默认值
    normalized.setdefault("category", "General")
    normalized.setdefault("album", None)
    normalized.setdefault("subtitle_type", "text")
    normalized.setdefault("duration_in_second", None)
    normalized.setdefault("cover_url", None)
    normalized.setdefault("audio_url", None)

    # 类型转换
    if normalized.get("duration_in_second"):
        try:
            normalized["duration_in_second"] = float(normalized["duration_in_second"])
        except (ValueError, TypeError):
            normalized["duration_in_second"] = None

    return normalized


# ============================================================
# 生成 SQL
# ============================================================

def generate_sql(records: list[dict]) -> str:
    """将记录列表转为 INSERT SQL"""

    lines = [
        "-- ============================================================",
        "-- 英语学习数据导入脚本",
        f"-- 生成时间：{datetime.now(timezone.utc).strftime('%Y-%m-%d %H:%M:%S')} UTC",
        f"-- 记录数：{len(records)}",
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

    now = now_sql()

    # ---- 第1步：收集所有分类 ----
    # 按 category 分组
    cat_groups = defaultdict(list)
    for r in records:
        cat_groups[r["category"]].append(r)

    # 为每个分类分配 GUID
    cat_guids = {}
    for cat_name in cat_groups:
        cat_guids[cat_name] = sql_guid()

    # ---- 第2步：插入分类 ----
    lines.append("-- ======================================")
    lines.append("-- 1. 插入分类 (T_Categories)")
    lines.append("-- ======================================")
    lines.append("")

    for seq, (cat_name, _) in enumerate(cat_groups.items(), 1):
        cat_id = cat_guids[cat_name]

        # 提取该分类下第一条记录的封面图（如果有）
        cover = None
        for r in cat_groups[cat_name]:
            if r.get("cover_url"):
                cover = r["cover_url"]
                break

        lines.append(f"-- 分类：{cat_name}")
        lines.append(
            f"INSERT INTO [T_Categories] ([Id], [SequenceNumber], [Title], [CoverUrl], "
            f"[IsDeleted], [CreateTime])"
        )
        lines.append(
            f"VALUES ({sql_str(cat_id)}, {seq}, {sql_str(cat_name)}, "
            f"{sql_str(cover)}, 0, @Now);"
        )
        lines.append("GO")
        lines.append("")

    # ---- 第3步：插入专辑 ----
    # 如果记录指定了 album，按 (category, album) 分组
    # 否则按 category 分组，整个分类作为一个专辑
    album_groups = defaultdict(list)
    for r in records:
        album_key = r["album"] if r.get("album") else r["category"]
        album_groups[(r["category"], album_key)].append(r)

    album_guids = {}

    lines.append("-- ======================================")
    lines.append("-- 2. 插入专辑 (T_Albums)")
    lines.append("-- ======================================")
    lines.append("")

    for seq, ((cat_name, album_name), items) in enumerate(album_groups.items(), 1):
        album_id = sql_guid()
        album_guids[(cat_name, album_name)] = album_id
        cat_id = cat_guids[cat_name]

        lines.append(f"-- 专辑：{album_name}")
        lines.append(
            f"INSERT INTO [T_Albums] ([Id], [Title], [IsVisible], [SequenceNumber], "
            f"[CategoryId], [CreatedTime], [IsDeleted])"
        )
        lines.append(
            f"VALUES ({sql_str(album_id)}, {sql_str(album_name)}, 1, {seq}, "
            f"{sql_str(cat_id)}, @Now, 0);"
        )
        lines.append("GO")
        lines.append("")

    # ---- 第4步：插入单集 ----
    lines.append("-- ======================================")
    lines.append("-- 3. 插入单集 (T_Episodes)")
    lines.append("-- ======================================")
    lines.append("")

    ep_seq_counter = defaultdict(int)

    for r in records:
        album_key = r["album"] if r.get("album") else r["category"]
        album_id = album_guids[(r["category"], album_key)]
        ep_seq_counter[(r["category"], album_key)] += 1
        ep_seq = ep_seq_counter[(r["category"], album_key)]

        ep_id = sql_guid()
        duration = r["duration_in_second"]
        duration_sql = str(duration) if duration is not None else "NULL"

        lines.append(f"-- 单集：{r['title']}")
        lines.append(
            f"INSERT INTO [T_Episodes] ([Id], [Title], [AlbumId], [SequenceNumber], "
            f"[IsVisible], [AudioUrl], [DurationInSecond], [Subtitle], [SubtitleType], "
            f"[CreateTime], [IsDeleted])"
        )
        lines.append(
            f"VALUES ({sql_str(ep_id)}, {sql_str(r['title'])}, "
            f"{sql_str(album_id)}, {ep_seq}, 1, "
            f"{sql_str(r.get('audio_url'))}, {duration_sql}, "
            f"{sql_str(r['subtitle'])}, {sql_str(r.get('subtitle_type', 'text'))}, "
            f"@Now, 0);"
        )
        lines.append("GO")
        lines.append("")

    # ---- 提交 ----
    lines.append("COMMIT;")
    lines.append("GO")
    lines.append("")
    lines.append("-- ============================================================")
    lines.append("-- 导入完成！验证数据量")
    lines.append("-- ============================================================")
    lines.append("SELECT 'T_Categories' AS [Table], COUNT(*) AS [Rows] FROM [T_Categories]")
    lines.append("UNION ALL")
    lines.append("SELECT 'T_Albums', COUNT(*) FROM [T_Albums]")
    lines.append("UNION ALL")
    lines.append("SELECT 'T_Episodes', COUNT(*) FROM [T_Episodes]")
    lines.append("ORDER BY [Table];")
    lines.append("GO")

    return "\n".join(lines)


# ============================================================
# 生成示例数据文件
# ============================================================

def generate_template():
    """生成一个示例 JSON 模板文件"""
    template = [
        {
            "category": "雅思听力",
            "album": "Cambridge IELTS 18 - Test 1",
            "title": "Section 1 - 租房咨询",
            "audio_url": "https://example.com/ielts18_test1_section1.mp3",
            "subtitle": "Woman: Hello, I'm calling about the apartment you advertised...\nMan: Oh yes, the two-bedroom apartment...",
            "subtitle_type": "text",
            "duration_in_second": 420,
            "cover_url": "https://example.com/ielts_cover.jpg"
        },
        {
            "category": "雅思听力",
            "album": "Cambridge IELTS 18 - Test 1",
            "title": "Section 2 - 博物馆导览",
            "audio_url": "https://example.com/ielts18_test1_section2.mp3",
            "subtitle": "Welcome to the City Museum. Let me begin by telling you about our opening hours...",
            "subtitle_type": "text",
            "duration_in_second": 410
        },
        {
            "category": "四六级听力",
            "album": "2024年6月 四级真题",
            "title": "短篇新闻 - 第1篇",
            "audio_url": "https://example.com/cet4_202406_news1.mp3",
            "subtitle": "A severe storm has hit the coastal area of southern China...",
            "subtitle_type": "text",
            "duration_in_second": 180
        },
        {
            "category": "托福听力",
            "album": "TOEFL iBT 样题 - 讲座",
            "title": "Biology Lecture - Photosynthesis",
            "audio_url": "https://example.com/toefl_biology_photosynthesis.mp3",
            "subtitle": "Today we're going to continue our discussion of plant biology by looking at photosynthesis...",
            "subtitle_type": "text",
            "duration_in_second": 300
        }
    ]

    with open("data_template.json", "w", encoding="utf-8") as f:
        json.dump(template, f, ensure_ascii=False, indent=2)
    print(f"📝 示例模板已生成：data_template.json")


# ============================================================
# 主入口
# ============================================================

def main():
    print("=" * 60)
    print("📚 英语学习数据导入工具")
    print("=" * 60)
    print()

    # 如果没有输入文件，生成模板并退出
    if not os.path.exists(INPUT_FILE):
        print(f"⚠️  未找到数据文件 {INPUT_FILE}")
        generate_template()
        print()
        print("请按 data_template.json 的格式整理你的数据，然后重新运行：")
        print(f"  python {os.path.basename(__file__)}")
        print()
        print("或者用 CSV 格式（字段名随意，脚本会自动识别）：")
        print("  category, album, title, audio_url, subtitle, duration_in_second")
        print("  雅思听力, Cambridge 18-T1, Section 1, https://..., 字幕内容..., 420")
        return

    # 读取数据
    records = load_data(INPUT_FILE)
    if not records:
        print("❌ 数据为空")
        return

    # 标准化字段
    print("🔄 标准化字段...")
    normalized = [normalize_record(r) for r in records]

    # 验证
    print("✅ 验证数据...")
    all_errors = []
    for idx, r in enumerate(normalized):
        errors = validate_record(r, idx)
        all_errors.extend(errors)

    if all_errors:
        print(f"❌ 发现 {len(all_errors)} 个错误：")
        for e in all_errors:
            print(f"   {e}")
        return

    # 统计
    categories = set(r["category"] for r in normalized)
    albums = set(
        (r["category"], r["album"] if r.get("album") else r["category"])
        for r in normalized
    )

    # 生成 SQL
    print("🛠️  生成 SQL...")
    sql = generate_sql(normalized)

    with open(OUTPUT_FILE, "w", encoding="utf-8") as f:
        f.write(sql)

    print()
    print("=" * 60)
    print("✅ 完成！")
    print(f"   - 输入：{INPUT_FILE} ({len(normalized)} 条记录)")
    print(f"   - 输出：{OUTPUT_FILE}")
    print(f"   - 分类：{len(categories)} 个")
    print(f"   - 专辑：{len(albums)} 个")
    print(f"   - 单集：{len(normalized)} 集")
    print()
    print("📋 导入到服务器：")
    print(f"   scp {OUTPUT_FILE} your-user@your-server:/tmp/")
    print(f"   /opt/mssql-tools18/bin/sqlcmd -S 127.0.0.1 -U sa -P '128639xuexi3.6' \\")
    print(f"     -d WPEnglish -i /tmp/{OUTPUT_FILE} -C")
    print("=" * 60)


if __name__ == "__main__":
    main()
