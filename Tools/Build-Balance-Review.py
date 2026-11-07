"""Build a standalone, local balance browser from replayed model-search certificates."""
import csv
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / "Documentation" / "BalanceStudy"
data = {}
for edition, directory in (("v9", "V9-baseline"), ("v10", "V10-tuned")):
    with (DEST / directory / "hands.csv").open(encoding="utf-8-sig", newline="") as source:
        rows = list(csv.DictReader(source))
    assert len(rows) == 882 and len({(r["fixture"], r["mask"]) for r in rows}) == 882
    for row in rows:
        row["mask"] = int(row["mask"])
        row["found"] = row["found"] == "true"
        for field in ("cost", "breath", "load", "peakLoad", "doorLegs", "packets", "cardsUsed"):
            row[field] = float(row[field]) if row[field] else None
    data[edition] = rows

payload = json.dumps(data, ensure_ascii=False, separators=(",", ":"))
summaries = {}
for edition, rows in data.items():
    fixture_summaries = {}
    intersection = None
    for fixture in dict.fromkeys(r["fixture"] for r in rows):
        candidates = [r for r in rows if r["fixture"] == fixture and r["found"]]
        best = min(r["cost"] for r in candidates)
        tied = {r["mask"] for r in candidates if abs(r["cost"] - best) < .001}
        intersection = tied if intersection is None else intersection & tied
        fixture_summaries[fixture] = {"found": len(candidates), "inputs": 126, "bestObservedCost": best, "tiedBestMasks": sorted(tied), "presets": [r for r in candidates if r["mask"] in (211, 94, 482)]}
    summaries[edition] = {"fixtures": fixture_summaries, "bestMasksAcrossEveryFixture": sorted(intersection), "inputs": len(rows)}
configuration = (DEST / "V10-tuned" / "summary.txt").read_text(encoding="utf-8-sig")
width = int(re.search(r"Width=(\d+)", configuration)[1])
summaries["method"] = {"beamWidth": width, "maximumDepth": 10, "totalInputs": 1764, "playerWinRates": False, "globalOptimalityProven": False, "sharedCertificatesReplayed": True, "finalGoalChecksRealAgreement": True}
(DEST / "summary.json").write_text(json.dumps(summaries, ensure_ascii=False, indent=2), encoding="utf-8")
(DEST / "comparison.json").write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding="utf-8")
template = r'''<!doctype html>
<html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>第三扇门 / 牌组对照</title>
<style>
:root{--paper:#ebe5d4;--ink:#181b18;--red:#ba2c1b;--line:#bbb5a6;--muted:#626357}*{box-sizing:border-box}body{margin:0;background:var(--paper);color:var(--ink);font:16px/1.55 system-ui,'Microsoft YaHei',sans-serif}main{max-width:1440px;margin:auto;padding:36px}header{border-top:8px solid var(--red);padding:28px 0 16px;border-bottom:2px solid var(--ink)}.kicker{letter-spacing:.13em;font-size:12px}h1{font-size:clamp(30px,4vw,56px);margin:12px 0}h2{font-size:22px;margin:24px 0 10px}p{max-width:1050px}.aside{color:var(--muted);font-size:14px}.stats{display:flex;gap:12px;margin:22px 0}.stat{flex:1;border:1px solid var(--line);padding:14px}.stat strong{display:block;font-size:30px}.toolbar{display:flex;gap:14px;flex-wrap:wrap;padding:18px 0;border-block:1px solid var(--ink)}label{font-size:14px}select,button{font:inherit;background:#f9f5e9;border:1px solid var(--ink);padding:8px;color:var(--ink)}button{cursor:pointer}button.active{background:var(--red);color:white}.cards{display:flex;gap:8px;flex-wrap:wrap;margin:14px 0}.cards button{min-width:74px;padding:7px 12px}.preset-grid{display:grid;grid-template-columns:repeat(3,1fr);gap:14px}.preset{border:1px solid var(--line);padding:17px;background:#f2eddf}.preset strong{font-size:20px}.meter{background:#d2cbbc;height:10px;margin:10px 0}.meter span{display:block;background:var(--red);height:100%}.mono{font:13px/1.6 ui-monospace,Consolas,monospace}.scroll{overflow:auto}table{border-collapse:collapse;width:100%;font-size:14px}th{text-align:left;background:var(--ink);color:var(--paper);font-weight:500;white-space:nowrap}td,th{padding:10px;border-bottom:1px solid var(--line)}tr[data-found="false"]{color:#79786e}tbody tr{cursor:pointer}tbody tr:hover{background:#ded8c8}.tag{display:inline-block;border:1px solid var(--line);padding:2px 5px;margin:2px;font-size:12px}.named{color:var(--red);font-weight:600}#route{border-left:6px solid var(--red);padding:18px;background:#f8f4e8;white-space:pre-wrap;min-height:110px}footer{margin:30px 0;border-top:1px solid var(--ink);padding-top:18px}.legend{display:flex;gap:24px;flex-wrap:wrap}.more{font-size:13px;color:var(--muted)}@media(max-width:760px){main{padding:20px}.preset-grid{grid-template-columns:1fr}.stats{flex-wrap:wrap}.stat{min-width:42%}td,th{padding:8px}}
</style><main>
<header><div class="kicker">THE THIRD DOOR / BALANCE STUDY 010</div><h1>五张牌，换一种处理办法。</h1><p>全部 126 种五张牌组合，在 7 个固定局面中搜索准确传话路线。V9 与 V10 各 882 个输入；使用实际游戏模型结算。点选一副手牌，可查看找到的行动顺序。</p><p class="aside">这份报告没有真人试玩数据。“未找到”只表示宽度 96、深度 10 的搜索没有给出路线，不是不可通关证明。结果包含显式修改余力、承重、信任与接话次数的压力局面，不能解读成玩家胜率。</p></header>
<div class="stats"><div class="stat"><strong>126</strong>五张牌组合</div><div class="stat"><strong>7</strong>局面与压力条件</div><div class="stat"><strong>1,764</strong>版本 × 牌组 × 局面</div><div class="stat"><strong>逐条重放</strong>共享路线均经模型验证</div></div>
<div class="toolbar"><label>局面 <select id="fixture"></select></label><label>规则 <select id="edition"><option value="v10">V10 调整后</option><option value="v9">V9 保留规则</option></select></label><label>排序 <select id="sort"><option value="cost">综合代价 ↑</option><option value="breath">结束余力 ↓</option><option value="load">结束承重 ↑</option><option value="peakLoad">最高承重 ↑</option><option value="doorLegs">门间移动 ↑</option><option value="packets">接话次数 ↑</option></select></label></div>
<h2>三套预设在这个局面中的表现</h2><div id="presets" class="preset-grid"></div>
<h2>筛选手牌</h2><p class="aside">点选卡牌，只显示包含它们的手牌。数值来自“综合代价”选出的那条路线；切换排序不会重新寻找另一种最优策略。</p><div id="filters" class="cards"></div>
<div class="legend"><span id="count"></span><span class="more">综合代价越低，表示这组固定权重下的候选路线更省事。</span></div><div class="scroll"><table><thead><tr><th>手牌</th><th>代价</th><th>余力</th><th>承重 / 峰值</th><th>移动</th><th>接话</th><th>V9 → V10 代价</th></tr></thead><tbody id="rows"></tbody></table></div>
<h2 id="route-title">行动顺序</h2><div id="route">请选择一行或一套预设。</div>
<footer><p>综合代价 = 1.6 × 门/长椅位置变化 + 0.7 × 接话次数 + 1.5 ×（8 − 结束余力）+ 0.65 × 结束承重 + 0.2 × 峰值承重。基本核对、交接和再次确认需要到实际角色所在门前。每个局面从固定回应后的 02 门前开始，不计此前的往返。</p><p>搜索只考虑准确公开片段、空格工具动作与基本行动。代加保证和隐私路线另由规则测试覆盖。目标为本场可收场；最后一场必须实际满足协议结局。已找到的路线会在所有包含所用卡的手牌上重新执行，以减少搜索顺序造成的预设偏差。有限搜索仍不能证明全局最优。</p><p>V9 数据来自当前模型中保留的旧规则路径。CSV、搜索配置和每条路线在相邻目录中；本报告可离线打开。</p><p class="aside">后续应测试：玩家是否察觉窗口与余力的区别；代办是否真的改变计划；没有选到某张牌时，能否主动使用基本动作或提前调整说话次序。</p></footer>
</main><script>
const data=__PAYLOAD__;
const titles={'first-plan':'01 / 初次安排','guarded-recipient':'双方有防备，来源已核对','heavy-load':'承重 6 / 余力 4','new-edition':'02 / 新版通知','low-breath-edition':'新版通知 / 余力 2','two-attempts-left':'新版通知 / 已用两次接话','rushed-confirmation':'03 / 赶时间，等待再次确认'};
const names=['原话','停顿','追问','翻译','界限','代办','核对','回执','交接'];
const ids=['Quote','Pause','Listen','Reframe','Boundary','TakeOver','Check','Receipt','Handoff'];
const presets=[{mask:211,name:'留证',text:'核对 / 原话 / 回执'},{mask:94,name:'周旋',text:'停顿 / 翻译 / 窗口'},{mask:482,name:'承接',text:'代办 / 交接 / 回执'}];
let filterMask=0;const el=id=>document.getElementById(id);function append(parent,tag,text,cls){let node=document.createElement(tag);node.textContent=text;if(cls)node.className=cls;parent.append(node);return node}function number(n){return n==null?'—':Number.isInteger(n)?n:n.toFixed(2)}
for(const [value,text] of Object.entries(titles)){let option=append(el('fixture'),'option',text);option.value=value}
ids.forEach((id,i)=>{let button=append(el('filters'),'button',names[i]);button.type='button';button.onclick=()=>{filterMask^=1<<i;button.classList.toggle('active',!!(filterMask&(1<<i)));render()}});
function showRoute(row){el('route-title').textContent=titles[row.fixture]+' / '+row.cards.split('+').map(id=>names[ids.indexOf(id)]).join('、');el('route').textContent=row.found?row.trace.split('; ').filter(Boolean).map((s,i)=>(i+1)+'. '+s).join('\n'):'在当前搜索预算内没有找到路线。可以改变前面的回应、余力安排或手牌；这条记录不能证明无解。'}
function current(edition){return data[edition].filter(r=>r.fixture===el('fixture').value)}
function render(){let edition=el('edition').value;let rows=current(edition);let oldMap=new Map(current('v9').map(r=>[r.mask,r]));let newMap=new Map(current('v10').map(r=>[r.mask,r]));let maxCost=Math.max(1,...rows.filter(r=>r.found).map(r=>r.cost));el('presets').replaceChildren();presets.forEach(p=>{let row=rows.find(r=>r.mask===p.mask);let card=append(el('presets'),'div','', 'preset');append(card,'strong',p.name);append(card,'div',p.text,'aside');let bar=append(card,'div','','meter');let fill=append(bar,'span','');fill.style.width=row.found?Math.min(100,row.cost/maxCost*100)+'%':'0';append(card,'div',row.found?'代价 '+number(row.cost)+' / 余力 '+row.breath+' / 承重 '+row.load:'搜索预算内未找到','mono');let button=append(card,'button','查看行动顺序');button.type='button';button.onclick=()=>showRoute(row)});
rows=rows.filter(r=>(r.mask&filterMask)===filterMask);let metric=el('sort').value;rows.sort((a,b)=>{if(a.found!==b.found)return a.found?-1:1;if(!a.found)return a.mask-b.mask;let d=(a[metric]-b[metric])*(metric==='breath'?-1:1);return d||a.cost-b.cost||a.mask-b.mask});el('count').textContent='显示 '+rows.length+' 副 / 找到路线 '+rows.filter(r=>r.found).length+' 副';el('rows').replaceChildren();rows.forEach(row=>{let tr=append(el('rows'),'tr','');tr.dataset.found=row.found;let cards=append(tr,'td','');let preset=presets.find(p=>p.mask===row.mask);if(preset)append(cards,'div',preset.name+' / 预设','named');row.cards.split('+').forEach(id=>append(cards,'span',names[ids.indexOf(id)],'tag'));append(tr,'td',row.found?number(row.cost):'未找到');append(tr,'td',number(row.breath));append(tr,'td',number(row.load)+' / '+number(row.peakLoad));append(tr,'td',number(row.doorLegs));append(tr,'td',number(row.packets));let old=oldMap.get(row.mask),updated=newMap.get(row.mask);append(tr,'td',(old.found?number(old.cost):'—')+' → '+(updated.found?number(updated.cost):'—'));tr.onclick=()=>showRoute(row)});}
['fixture','edition','sort'].forEach(id=>el(id).onchange=render);render();
</script></html>'''
(DEST / "index.html").write_text(template.replace("宽度 96", f"宽度 {width}").replace("__PAYLOAD__", payload), encoding="utf-8")
print(f"BALANCE REVIEW / {len(data['v9']) + len(data['v10'])} inputs / {DEST / 'index.html'}")
