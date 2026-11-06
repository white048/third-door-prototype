"""Build an offline art handoff: original images, prompts, placement sketch and SVG layouts.
Generated raster assets are copied unchanged. Pillow draws only the requested wireframe.
"""
from pathlib import Path
import base64
import hashlib
import html
import json
import shutil
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Documentation' / 'ArtDirection'
IMAGES = OUT / 'images'
LAYOUTS = OUT / 'layouts'
IMAGES.mkdir(parents=True, exist_ok=True)
LAYOUTS.mkdir(parents=True, exist_ok=True)
SOURCE = ROOT / 'Assets' / 'Resources' / 'Design' / 'Illustrations'
DATE = '2026-10-29'
COPY_REVIEW_DATE = '2026-11-04'
STYLE = ('Refined constructivist editorial photomontage for The Third Door, a game about care, '
         'consent, carrying words and a personal way out. Warm ivory #E8E2CF, carbon #141514, '
         'vermilion #C51B11 only; tiny muted brass accent only for a house key. Hand-cut heavy '
         'paper, anonymous photocopied hands, fine halftone, dry lithographic ink, subtle paper '
         'fibers, one decisive diagonal and restrained red disk. Flat full-bleed printed artwork, '
         'quiet negative space, clear thumbnail read. No faces, readable text, letters, numbers, '
         'logos, watermarks, political insignia, flags, fists, chains, coercion or decorative clutter. '
         'Keep all typography in the editable game or layout layer, outside the generated image. ')
ASSETS = []

def add(code, name, group, priority, size, use, requirement, prompt='', status='待制作', filename=''):
    item = dict(id=code, name=name, group=group, priority=priority, size=size, use=use,
                requirement=requirement, prompt=prompt, status=status, filename=filename)
    ASSETS.append(item)
    return item

add('A01', '三扇门主视觉母版', '主画', 'P0', '现有 1672×940；建议重绘母版 ≥2560×1440',
    '商店构图、宣传页、未来主视觉', '左侧约 42% 留给标题；三扇门、信封与手集中在右侧。现有母版宽 1672，3840 宽图仍需另做。',
    STYLE + 'Landscape 16:9. Leave the left 42 percent mostly quiet ivory for a separately typeset title. '
    'On the right, three upright rectilinear paper door apertures, one red, one black, one untouched ivory; '
    'two cropped paper hands gently offer a closed ivory envelope with a tiny brass key. A loose red thread '
    'connects the first two doors, stopping short of the third. Large cropped red disk upper right. '
    'Keep focal objects inside the right half with margin for recomposition.',
    '已生成 / 构图母版', 'KeyArtMaster-v1.png')
add('A02', '家庭案例：钥匙与红线', '主画', 'P0', '现有 1254×1254；后续建议 2048×2048 PNG',
    '选案页、开场、展厅与图像手记', '钥匙象征联系；红线松弛，出口保持空白。双手不抓扯人物。',
    STYLE + 'Square companion print. A central ivory envelope of torn heavyweight paper, small worn brass '
    'house key tied with red cotton thread. Two opposing cropped anonymous photocopied hands enter from '
    'the left and right edges; neither restrains the other. Thread forms a loose open triangle and ends '
    'beside an unprinted door-shaped gap at lower right. Cropped red disk upper right, strong black diagonal. '
    'Broad breathing space. Care remains without carrying every message.', '已接入游戏', 'CaseHome-v1.png')
add('A03', '工作案例：留白事由', '主画', 'P0', '现有 1254×1254；后续建议 2048×2048 PNG',
    '选案页、开场、展厅与图像手记', '两条公开工作线；下方矩形事由栏必须完全空白。黑色信封保持封闭。',
    STYLE + 'Square companion print matching A02. A large ivory work-plan sheet with a small black binder '
    'clip. Exactly two thin horizontal task rules, below them one clear EMPTY rectangular field. No writing '
    'or checkmarks. A closed carbon envelope lies beside the blank field, never opened. Red cotton thread '
    'connects only the two task rules to a cropped hand, bypassing the private blank. A second cropped hand '
    'holds the page lightly. Black diagonal, cropped red disk and clean ivory doorway gap.',
    '已接入游戏', 'CaseOffice-v1.png')

atlas_request = json.loads((ROOT/'Documentation/ReferenceStudy-v7/GeneratedAssetPrompt.json').read_text(encoding='utf-8'))
add('A04', '传话摄影物件：九格透明图集', '主画', 'P0', '已交付 1254×1254 RGBA；3×3，每格 418×418',
    '本局拼贴长卷、交付校样与实体纸卷',
    '原图保留透明通道，按 UV 取格。九件物体不跨格；不把文字画进图。照片质感、日常物件和大块留白用于近距离阅读。',
    atlas_request['request']['prompt'], '已接入游戏 / 第七版', 'ObjectCutouts-v7.png')

cards = [
    ('C01', '停顿', 'A single loose red thread rests beside an untouched ivory envelope. One hand is open and still; generous empty space. No clock, timer or urgency.'),
    ('C02', '原话', 'Two matching torn ivory paper fragments, one held and one offered, connected by a red thread without erasing or covering their surfaces. The original meaning is kept. No letters or quotation text.'),
    ('C03', '界限', 'A red thread gently ends at the edge of a clean ivory doorway gap. An open hand remains outside the gap. The boundary is permeable and calm, never a prison wall.'),
    ('C04', '追问', 'A small closed envelope offered toward two nested arcs cut from ivory paper; one open hand waits. Curiosity and attention without exposing contents. No realistic ear, eye or face.'),
    ('C05', '翻译', 'Two angular paper fragments carefully unfolded into a single open ivory form. Red thread is rerouted without being cut. Preserve the source rather than conceal it. No written message.'),
    ('C06', '代偿', 'Two cropped hands support a visibly heavy stack of closed envelopes, with several taut red threads converging on the hands. Responsibility accumulates; no suffering face or coercive imagery.'),
    ('C07', '重述', 'One red thread makes a single deliberate loop beside an ivory envelope, then continues forward. A small displaced paper fragment remains visible as a trace. No clock, rewind-button icon or erased history.'),
]
for code, name, subject in cards:
    add(code, name + '工具插画', '卡牌', 'P1', '建议 1024×1024 PNG，缩小验收到 128×128',
        '下一轮扩大卡牌详情后接入；当前小按钮继续保留文字',
        '每张一件主物；主体控制在中间 70%。先附 A02/A03 作风格参考，再单张生成。不要把插画挤进现有按钮。',
        STYLE + 'Square single-object tool-card illustration. ' + subject)
for code, name, subject in [
    ('E01', '出口', 'An untouched ivory door-shaped gap stays open beside a set-down envelope and loose red thread. Quiet room for the self, not a golden victory portal.'),
    ('E02', '在场', 'Two separate simple chairs share one work surface, with two distinct loose red threads. A closed envelope rests between them, and a third position remains available. Consent without merging identities.'),
    ('E03', '承担', 'Several closed envelopes accumulate on a narrow paper shelf, with converging red threads. A clean ivory gap remains visible at the edge. Dependency and weight, no moral judgment or villain.'),
]:
    add(code, '结局意象：' + name, '后续', 'P2', '建议 2048×1152，16:9 PNG', '未来结局回看页，不覆盖当前可探索的三维后果',
        '三类意象按内容使用，不作好坏排名。等结局回看页需要配图时再制作，先共用三类意象。', STYLE + 'Landscape 16:9. ' + subject)
add('T01', '低对比纸张材质', '后续', 'P2', '建议 2048×2048，可平铺 PNG', '仅在程序化纸纹仍显单薄时补充',
    '亮度起伏极低，边缘无接缝，无污渍折痕。文字区域必须可读。现有代码纸纹优先保留。',
    'Seamless uncoated warm ivory paper, near-uniform #E8E2CF, extremely subtle short fibers and tiny natural grain. '
    'Flat lighting, no shadows, no folds, stains, torn edges, text or vignette. Intended beneath readable UI typography.')

steam = [
    ('S01', '商店头图', '920×430', 'Header.svg'),
    ('S02', '小胶囊', '462×174', 'Small.svg'),
    ('S03', '主胶囊', '1232×706', 'Main.svg'),
    ('S04', '竖胶囊', '748×896', 'Vertical.svg'),
    ('S05', '库封面', '600×900', 'LibraryCapsule.svg'),
]
for code, name, size, layout in steam:
    add(code, name, 'Steam', 'P0', size + '，最终 PNG/JPG', '正式商店图形资产',
        '提供可编辑 SVG 构图稿。插画与标题分别排版；上传前按模板导出栅格并缩小检查。仅保留游戏标题和画面。',
        STYLE + 'Recompose the three-door master for a ' + size + ' capsule. Keep title area separate and empty. '
        'Readable envelope, thread and third opening at thumbnail scale. Do not add badges, review scores, awards, genre labels or promotional copy.',
        '已有 SVG 构图稿 / 待导出', 'layouts/' + layout)
add('S06', '库横幅', 'Steam', 'P0', '3840×1240 PNG', 'Library Hero',
    '这张不放游戏 logo。当前母版宽 1672，需按 3840×1240 重新绘制或扩图，并检查细节。',
    STYLE + 'Ultrawide library hero artwork, 3840x1240 final. Broad ivory breathing space, three paper door '
    'openings with anonymous hands, closed envelope and loose red thread. Make a genuine wide composition, '
    'not a stretched square. No logo or text. Keep focal objects readable within the central safe region.')
add('S07', '透明标题 Logo', 'Steam', 'P0', '1280 宽和/或 720 高 PNG', 'Library Logo 与商店标题层',
    '用现有 Anton 字体排版，不让生成模型画字。附字体与 OFL 许可，以及可编辑 SVG；正式上传导出透明 PNG。',
    '不需要生图。用 THE THIRD DOOR 精确排字，保留三行或横排两个版本，缩小后检查字形与字距。',
    '已有字体与 SVG / 待导出', 'layouts/Logo.svg')
add('S08', '真实玩法截图', 'Steam', 'P0', '至少 1920×1080，16:9，至少 5 张', '商店截图',
    '从实际播放器采集：空间、选择与代价、门前交付、逐句回看、结局后空间。生成插画不是玩法截图。',
    '不需要生图。以实际游戏窗口在 1920×1080 或更高分辨率采集；不后加广告文字、评分或奖项。', '实机采集中')

native_frames = []
frame_specs = [
    ('v5-art-home-store-cn', '02-arrival.png', '01-Exhibition-zh.png', '展厅与三扇门'),
    ('v5-art-home-store-cn', '04-choice-0.png', '02-Choice-zh.png', '回应与实际资源预览'),
    ('v5-art-home-store-cn', '06-notebook.png', '03-Notebook-zh.png', '当下手记与回应记录'),
    ('v5-art-home-store-cn', '06b-sentence-trace.png', '04-Trace-zh.png', '逐句回看原话与回应'),
    ('v5-art-office-store-en', '13-office-public-request-preview.png', '05-Delivery-en.png', '公开请求与私下需要分开交付'),
    ('v5-art-office-store-en', '08-ending.png', '06-Ending-en.png', '有限工作协议与结局选择'),
    ('v7-ribbon-home-store-cn', '00-physical-strip.png', '07-PhysicalStrip-zh.png', '展厅中的实体纸卷'),
    ('v7-ribbon-home-store-cn', '18-growing-strip-response.png', '08-CollageStrip-zh.png', '从本局回应生长的拼贴长卷'),
    ('v7-ribbon-home-store-cn', '16-private-delivery-proof.png', '09-DeliveryProof-zh.png', '交付前读完整一句与许可'),
    ('v7-ribbon-office-agreement-store-en', '19-ending-strip-last-page.png', '10-CollageEnding-en.png', '有限协议的长卷收尾'),
]
qa = ROOT / 'Builds/Constructivist/QA'
assembly = ROOT / 'Builds/Constructivist/TheThirdDoor_Data/Managed/Assembly-CSharp.dll'
ready = assembly.exists()
for scenario in {x[0] for x in frame_specs}:
    report = qa / scenario / 'checks.txt'
    ready = ready and report.exists() and report.stat().st_mtime >= assembly.stat().st_mtime
    if report.exists():
        ready = ready and all(line.startswith('PASS') for line in report.read_text(encoding='utf-8-sig').splitlines())
ready = ready and all((qa / s / f).exists() for s, f, _, _ in frame_specs)
if ready:
    shot_dir = OUT / 'screenshots'
    shot_dir.mkdir(exist_ok=True)
    for scenario, source_name, target_name, caption in frame_specs:
        source_frame = qa / scenario / source_name
        with Image.open(source_frame) as frame:
            if frame.size != (1920, 1080):
                raise ValueError('Screenshot is not native 1920x1080: ' + str(source_frame))
        shutil.copy2(source_frame, shot_dir / target_name)
        native_frames.append(dict(file='screenshots/' + target_name, caption=caption, pixels=[1920,1080],
                                  source='Actual Unity player capture, copied unchanged',
                                  sha256=hashlib.sha256(source_frame.read_bytes()).hexdigest()))
    ASSETS[-1]['status'] = '已采集 10 张真实玩法画面'

for item in ASSETS:
    if item['filename'].endswith('.png'):
        src = SOURCE / item['filename']
        shutil.copy2(src, IMAGES / item['filename'])
        with Image.open(src) as image:
            item['actualPixels'] = list(image.size)
        item['sha256'] = hashlib.sha256(src.read_bytes()).hexdigest()
        item['generation'] = 'Built-in image_gen; copied unchanged'
        item['generatedOn'] = '2026-11-04' if item['id']=='A04' else DATE
shutil.copy2(ROOT / 'Assets/Resources/Design/Display.ttf', OUT / 'Display.ttf')
shutil.copy2(ROOT / 'Assets/Resources/Design/Display-LICENSE.txt', OUT / 'FontLicense.txt')

# Editable vector layouts use the existing project font, never AI-generated text.
font_data = base64.b64encode((OUT / 'Display.ttf').read_bytes()).decode()
master_data = 'data:image/png;base64,' + base64.b64encode((IMAGES / 'KeyArtMaster-v1.png').read_bytes()).decode()
home_data = 'data:image/png;base64,' + base64.b64encode((IMAGES / 'CaseHome-v1.png').read_bytes()).decode()
font_css = "@font-face{font-family:DoorTitle;src:url(data:font/ttf;base64," + font_data + ") format('truetype')}"
def svg_layout(filename, width, height, small=False, vertical=False, logo=False):
    if logo:
        body = '<text x="24" y="156" fill="#141514" font-family="DoorTitle" font-size="160">THE THIRD DOOR</text>'
    elif small:
        body = ('<rect width="100%" height="100%" fill="#E8E2CF"/><rect x="0" width="12" height="100%" fill="#C51B11"/>'
                '<text x="30" y="77" fill="#141514" font-family="DoorTitle" font-size="72">THE THIRD</text>'
                '<text x="30" y="151" fill="#141514" font-family="DoorTitle" font-size="72">DOOR</text>'
                '<path d="M360 22h70v130h-70z" fill="#C51B11"/><path d="M378 42h33v110h-33z" fill="#E8E2CF"/>')
    elif vertical:
        art_h = int(height * .69)
        fs = int(width * .16)
        body = (f'<rect width="100%" height="100%" fill="#E8E2CF"/><image href="{home_data}" x="0" y="0" width="{width}" height="{art_h}" preserveAspectRatio="xMidYMid meet"/>'
                f'<text x="{int(width*.065)}" y="{int(height*.82)}" fill="#141514" font-family="DoorTitle" font-size="{fs}">THE THIRD</text>'
                f'<text x="{int(width*.065)}" y="{int(height*.96)}" fill="#141514" font-family="DoorTitle" font-size="{fs}">DOOR</text>')
    else:
        fs = int(height * .265)
        body = (f'<image href="{master_data}" width="{width}" height="{height}" preserveAspectRatio="xMidYMid slice"/>'
                f'<g fill="#141514" font-family="DoorTitle" font-size="{fs}"><text x="{int(width*.04)}" y="{int(height*.31)}">THE</text>'
                f'<text x="{int(width*.04)}" y="{int(height*.60)}">THIRD</text><text x="{int(width*.04)}" y="{int(height*.89)}">DOOR</text></g>')
    svg = (f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">'
           f'<title>The Third Door editable composition</title><style>{font_css}</style>{body}</svg>')
    (LAYOUTS / filename).write_text(svg, encoding='utf-8')
svg_layout('Header.svg', 920, 430)
svg_layout('Main.svg', 1232, 706)
svg_layout('Small.svg', 462, 174, small=True)
svg_layout('Vertical.svg', 748, 896, vertical=True)
svg_layout('LibraryCapsule.svg', 600, 900, vertical=True)
svg_layout('Logo.svg', 1280, 210, logo=True)

# Requested simple placement diagram; code-drawn wireframe, not a game screenshot.
paper, ink, red, muted = '#E8E2CF', '#141514', '#C51B11', '#78786B'
canvas = Image.new('RGB', (1920, 1240), paper)
draw = ImageDraw.Draw(canvas)
cn_path = 'C:/Windows/Fonts/msyh.ttc'
def font(size):
    return ImageFont.truetype(cn_path, size)
def txt(x, y, value, size=24, color=ink):
    draw.text((x, y), value, font=font(size), fill=color)
def box(rect, fill=None, line=ink, width=3):
    draw.rectangle(rect, fill=fill, outline=line, width=width)
txt(68, 42, 'THE THIRD DOOR / 美术放置示意', 38)
txt(68, 103, '拼贴放在下图标出的位置；文字、编号、状态和按钮由界面另排。这是一张位置示意图。', 22, muted)
panels = [(68,180,922,640),(998,180,1852,640),(68,704,922,1166),(998,704,1852,1166)]
for p in panels: box(p)
txt(92, 200, '01 / 标题与选案', 27, red)
txt(1022, 200, '02 / 案例开场', 27, red)
txt(92, 724, '03 / 展厅中的实体拼贴', 27, red)
txt(1022, 724, '04 / 本局长卷与交付校样', 27, red)
txt(103, 271, 'THE\nTHIRD\nDOOR', 54)
box((480,268,850,575), fill='#D4CFBC');txt(522, 360, 'A02 / A03', 36);txt(522,414,'随选案切换',24)
box((105,527,259,563),fill=ink);box((278,527,432,563),fill=red);txt(107,579,'文字和按钮保持独立',20,muted)
box((1035,274,1324,563),fill='#D4CFBC');txt(1073,375,'A02 / A03',30)
txt(1390,322,'案例引言',34);txt(1390,390,'三行以内的短段落\n四周保留空白',22);txt(1390,529,'空格 / 进入展厅',20,muted)
box((140,806,250,982),fill=red);txt(161,861,'01',36,paper);box((430,785,540,982),fill=ink);txt(451,850,'03',36,paper)
box((702,806,812,982),fill=ink);txt(723,861,'02',36,paper)
draw.line([(195,1016),(485,1090),(757,1016)],fill=red,width=4)
box((738,1018,863,1104),fill='#D4CFBC');txt(750,1042,'图像手记',21)
box((550,1018,673,1104),fill='#D4CFBC');txt(565,1042,'纸卷 E',21)
txt(105,1120,'两座可读展台；模型与阅读分别处理',21,muted)
box((1035,802,1405,1098),fill='#F8F7F0');box((1440,802,1813,1098),fill='#F8F7F0')
txt(1055,818,'V / 拼贴长卷',25,red);txt(1460,818,'交付校样',25,red)
box((1060,890,1160,986),fill='#D4CFBC');txt(1064,920,'A04',23)
txt(1190,882,'原话\n\n你的回应\n\n实际反馈',20)
txt(1055,1043,'滚轮 / 轮次跳转',20,muted)
txt(1460,884,'来源 → 收信人\n完整的一句\n许可与代价',22)
box((1462,1021,1608,1060),fill=ink);box((1625,1021,1785,1060),fill=red)
txt(1475,1070,'改选 / 确认送出',18,muted)
txt(1035,1121,'A04 按 UV 取格；文字与状态保持独立',20,muted)
canvas.save(OUT / 'ArtPlacement.png')

SOURCES = [
    ('Steam 图形资产尺寸', 'https://partner.steamgames.com/doc/store/assets'),
    ('Steam 商店图与截图要求', 'https://partner.steamgames.com/doc/store/assets/standard'),
    ('Steam 图形资产规则', 'https://partner.steamgames.com/doc/store/assets/rules'),
]
manifest = dict(project='The Third Door', version='7.0', date=DATE, copyReviewed=COPY_REVIEW_DATE,
                runtimeAssemblySHA256=hashlib.sha256(assembly.read_bytes()).hexdigest() if ready else None,
                method='Built-in image_gen; no API-key workflow',
                palette=[paper,ink,red], note='Each asset lists its current pixels and proposed production size separately. Prompts describe the next image to make.',
                assets=ASSETS, nativeScreenshots=native_frames, sources=[dict(title=t,url=u) for t,u in SOURCES])
(OUT / 'AssetManifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
plain = ['第三扇门 / 美术制作单与提示词', '初版原图：'+DATE+'；第七版图集与制作单：'+COPY_REVIEW_DATE, '',
         '主旨：替人传话时，谁来承担后续？画面围绕信封、红线和仍可离开的第三扇门展开。',
         '生成方式：内置 image_gen。四张源图未做像素编辑，原尺寸与 SHA-256 见 AssetManifest.json。',
         'A02/A03 用于案例，A04 用于长卷、交付校样和实体纸卷。A01 用于宣传构图。',
         'SVG 提供可编辑排版，上传前需按规格导出 PNG/JPG。',
         '空间海报参考 A02/A03；近距离摄影剪贴参考 A04。文字和状态由界面另排；玩法截图保留实机原样。', '']
for item in ASSETS:
    plain += [f"{item['id']} / {item['name']} / {item['priority']} / {item['status']}",
              '尺寸：'+item['size'], '用途：'+item['use'], '要求：'+item['requirement'],
              '提示词：'+item['prompt'], '']
plain += ['验收：缩小到 128px 时能认出主物；采用纸白、炭黑、朱红，钥匙可用少量暗黄铜色。',
          '检查双手形状、事由栏是否空白、黑信封是否封闭。实际玩法截图与生成宣传图分别保存。',
          'Steam 规格核对日期 '+DATE+'，要求见下方官方链接。当前页面更新日期 '+COPY_REVIEW_DATE+'。', '']
plain += [t+'：'+u for t,u in SOURCES]
(OUT / '美术制作单与提示词.txt').write_text('\n'.join(plain), encoding='utf-8')
(OUT / 'OpenArtBrief.cmd').write_text('@echo off\nstart "" "%~dp0index.html"\n', encoding='ascii')

cards_html = []
for item in ASSETS:
    picture = f'<img src="images/{item["filename"]}" alt="{html.escape(item["name"])}">' if item['filename'].endswith('.png') else ''
    link = f'<a class="file" href="{item["filename"]}">打开可编辑 SVG ↗</a>' if item['filename'].endswith('.svg') else ''
    cards_html.append(f'''<article data-group="{item['group']}" data-priority="{item['priority']}">
      <div class="index">{item['id']} <span>{item['priority']} · {item['status']}</span></div>{picture}
      <h3>{item['name']}</h3><div class="size">{item['size']}</div><p>{item['use']}</p><p>{item['requirement']}</p>{link}
      <details><summary>要求与可复制提示词</summary><textarea readonly aria-label="{item['id']}提示词">{html.escape(item['prompt'])}</textarea><button class="copy">复制提示词</button></details></article>''')
html_doc = '''<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>第三扇门 / 美术制作单</title><style>
*{box-sizing:border-box}body{margin:0;background:#E8E2CF;color:#141514;font-family:'Microsoft YaHei',sans-serif;line-height:1.65}
main{max-width:1460px;margin:auto;padding:40px 48px 80px}header{border-top:4px solid;padding-top:22px;display:grid;grid-template-columns:1.05fr 1fr;gap:54px;align-items:center}
.eyebrow{font-size:13px;letter-spacing:.16em}h1{font:900 clamp(55px,7vw,102px)/.95 Impact,'Arial Narrow',sans-serif;margin:30px 0}h1 small{display:block;font:500 22px/1.6 'Microsoft YaHei',sans-serif;margin-top:28px}
.lead{max-width:650px;font-size:18px}.hero{width:100%;border:10px solid #141514}.stamp{display:inline-block;background:#C51B11;color:#E8E2CF;padding:6px 14px;font-size:13px}
nav{position:sticky;top:0;background:#E8E2CFef;backdrop-filter:blur(9px);padding:18px 0;border-top:2px solid;border-bottom:1px solid;z-index:2;display:flex;gap:9px;flex-wrap:wrap;margin-top:48px}
button,a.button{font:inherit;font-size:14px;border:1px solid #141514;background:none;color:#141514;padding:8px 17px;cursor:pointer;text-decoration:none}button[aria-pressed=true],button:hover,a.button:hover{background:#141514;color:#E8E2CF}
.note{border-left:5px solid #C51B11;padding:10px 22px;margin:30px 0;background:#d9d3bf}.grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:24px;margin-top:30px}
article{border-top:4px solid #141514;background:#f1ecdc;padding:20px;break-inside:avoid}article[hidden]{display:none}.index{font-weight:bold;color:#C51B11;display:flex;justify-content:space-between;font-size:15px}.index span{font-size:11px;color:#666658;font-weight:normal;text-align:right}
article img{width:100%;margin-top:18px;display:block}h3{font-size:21px;margin:18px 0 5px}.size{font-size:12px;color:#6a695e}article p{font-size:14px}details{border-top:1px solid #bcb6a4;padding-top:14px}summary{font-size:13px;cursor:pointer}textarea{width:100%;height:240px;margin:15px 0 10px;border:1px solid #b0aa98;padding:14px;background:#fffdf4;font:12px/1.7 monospace;resize:vertical}.copy{font-size:12px;padding:7px 12px}.file{display:inline-block;margin:0 0 20px;color:#9f2017}
.diagram{width:100%;margin:38px 0 16px;border:1px solid}.footer{margin-top:45px;border-top:3px solid;padding-top:25px;font-size:13px}.footer a{color:inherit}.layouts{display:grid;grid-template-columns:2fr 1fr;gap:22px;margin:25px 0}.layouts img{display:block;width:100%;border:1px solid #88826e}.muted{color:#6a695e}h2{margin:52px 0 12px;font-size:30px}
@media(max-width:900px){main{padding:24px}header{grid-template-columns:1fr}.grid{grid-template-columns:repeat(2,minmax(0,1fr))}.layouts{grid-template-columns:1fr}}@media(max-width:580px){.grid{grid-template-columns:1fr}h1{font-size:64px}}@media print{nav,.copy{display:none}details{display:block}main{padding:0}.grid{grid-template-columns:repeat(2,1fr)}article{page-break-inside:avoid}textarea{height:auto}header{display:block}.hero{max-width:700px}}
</style><main><header><div><div class="eyebrow">ART DIRECTION / THE THIRD DOOR / VOL. 07</div><h1>WORDS.<br>WEIGHT.<br>A WAY OUT.<small>美术制作单 · 要求 · 提示词 · 放置示意</small></h1><p class="lead">信封里装着要传的话，红线连着还没做完的事。第七版增加摄影物件和本局拼贴长卷，让原话、回应和交付沿着纸带展开。</p><span class="stamp">4 张 image_gen 原图 / 新增九格摄影物件</span></div><img class="hero" src="images/ObjectCutouts-v7.png" alt="第七版九格透明摄影物件图集"></header>
<nav aria-label="筛选素材"><button aria-pressed="true" data-filter="全部">全部</button><button aria-pressed="false" data-filter="主画">主画</button><button aria-pressed="false" data-filter="卡牌">卡牌</button><button aria-pressed="false" data-filter="Steam">Steam</button><button aria-pressed="false" data-filter="后续">后续</button><a class="button" href="美术制作单与提示词.txt">纯文字单</a><a class="button" href="AssetManifest.json">资产记录</a><a class="button" href="ArtPlacement.png">放置示意图</a></nav>
<div class="note">先完成 P0。P1 的七张工具图等卡牌详情有足够位置后再接入；P2 按需制作。每项分别标明现有原图尺寸和后续建议尺寸。</div>
<section class="grid">''' + ''.join(cards_html) + '''</section><h2>放置示意</h2><img class="diagram" src="ArtPlacement.png" alt="标题、开场、实体纸卷、拼贴长卷与交付校样的放置示意"><p class="muted">示意图说明插画与摄影物件放在哪里。实机画面展示在下方，包含展厅、回应、交付校样和长卷。</p>
<h2>可编辑的商店构图稿</h2><p>插画和标题分层。附 Anton 字体及 OFL 许可，SVG 可继续编辑；正式提交前，按当前 Steam 模板导出 PNG/JPG 并检查缩略图。Library Hero 另需 3840×1240 的原生宽图。</p><div class="layouts"><a href="layouts/Header.svg"><img src="layouts/Header.svg" alt="头图排版稿"></a><a href="layouts/Small.svg"><img src="layouts/Small.svg" alt="小胶囊排版稿"></a></div>
<footer class="footer"><p>原图由内置 image_gen 生成，PNG 像素保持原样。两张案例拼贴用于选案、开场和展台，新增九格图集用于长卷与交付校样。长卷从本局记录排版，阅读不消耗资源或回应。参考作品留在研究目录，包内仅含新生成的素材。页面可离线使用，更新于 2026-11-04。</p><p>规格核对：''' + ' · '.join(f'<a href="{u}">{t}</a>' for t,u in SOURCES) + '''（2026-10-29）。商店图只放标题与画面；截图采集真实玩法。</p></footer></main>
<script>document.querySelectorAll('[data-filter]').forEach(b=>b.addEventListener('click',()=>{document.querySelectorAll('[data-filter]').forEach(x=>x.setAttribute('aria-pressed',String(x===b)));document.querySelectorAll('article').forEach(a=>a.hidden=b.dataset.filter!=='全部'&&a.dataset.group!==b.dataset.filter)}));document.querySelectorAll('.copy').forEach(b=>b.addEventListener('click',async()=>{const t=b.previousElementSibling;t.focus();t.select();try{if(navigator.clipboard)await navigator.clipboard.writeText(t.value);else document.execCommand('copy');b.textContent='已复制'}catch(e){b.textContent='文字已选中，按 Ctrl+C'}setTimeout(()=>b.textContent='复制提示词',2000)}));</script></html>'''
(OUT / 'index.html').write_text(html_doc, encoding='utf-8')
if native_frames:
    native_html = '<h2>十张实机截图 / 1920×1080</h2><p>从通过本版流程检查的播放器采集，保留原始画面。包含实体纸卷、拼贴长卷和交付校样；点击可打开完整尺寸。</p><div class="grid">'
    for shot in native_frames:
        native_html += f'<article><a href="{shot["file"]}"><img src="{shot["file"]}" alt="{shot["caption"]}" loading="lazy"></a><p>{shot["caption"]}</p></article>'
    native_html += '</div>'
    html_doc = html_doc.replace('<footer class="footer">', native_html + '<footer class="footer">')
    (OUT / 'index.html').write_text(html_doc, encoding='utf-8')
print(f'ART BRIEF / {len(ASSETS)} entries / 4 unchanged source images / 6 editable SVG layouts / {len(native_frames)} gameplay frames / {OUT}')
