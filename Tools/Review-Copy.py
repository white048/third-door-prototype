"""Inventory owned copy and verify an editorial pass did not alter executable C# tokens."""
from pathlib import Path
import hashlib
import json
import re
import sys

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Documentation/Editorial'
TOKEN=re.compile(r'//[^\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'')
def structure(text):
    def replace(match):
        token=match.group()
        if token.startswith('//') or token.startswith('/*'): return ''
        return 'STRING' if token.startswith(('"','@"')) else token
    return hashlib.sha256(re.sub(r'\s+','',TOKEN.sub(replace,text)).encode()).hexdigest()
def corpus():
    files={}
    for file in sorted((ROOT/'Assets/ThirdDoor/Scripts').rglob('*.cs')):
        text=file.read_text(encoding='utf-8-sig')
        entries=[]
        for match in TOKEN.finditer(text):
            raw=match.group()
            if raw.startswith('"'):
                try:value=json.loads(raw)
                except ValueError:continue
                entries.append(dict(line=text.count('\n',0,match.start())+1,value=value))
        files[file.relative_to(ROOT).as_posix()]=dict(codeHash=structure(text),strings=entries)
    return files
OUT.mkdir(parents=True,exist_ok=True)
baseline=OUT/'Before-v6.json'
if sys.argv[1:] == ['snapshot']:
    if baseline.exists():raise SystemExit('Baseline already exists; refusing to replace the review record.')
    baseline.write_text(json.dumps(corpus(),ensure_ascii=False,indent=2),encoding='utf-8')
    print('COPY BASELINE / saved without changing game code')
else:
    old=json.loads(baseline.read_text(encoding='utf-8'))
    now=corpus();changed=[];bad=[]
    for path,item in old.items():
        if path not in now or item['codeHash']!=now[path]['codeHash']:bad.append(path)
        if path in now:
            a=[x['value'] for x in item['strings']];b=[x['value'] for x in now[path]['strings']]
            if len(a)!=len(b):bad.append(path+' / string count changed')
            for i,(before,after) in enumerate(zip(a,b)):
                if before!=after:changed.append(dict(file=path,line=now[path]['strings'][i]['line'],before=before,after=after))
    report=dict(changedLiteralOccurrences=len(changed),changedFiles=len({x['file'] for x in changed}),
                executableTokensUnchanged=not bad,violations=bad,changes=changed)
    (OUT/'CopyReview-v6.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    print(f'COPY REVIEW / {len(changed)} string occurrences / {report["changedFiles"]} files / executable tokens unchanged={not bad}')
    if bad:raise SystemExit('\n'.join(bad))
