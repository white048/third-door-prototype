"""Review compatibility copy too, retaining its historical branches and lookup keys."""
from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[1]
changes=[
('他请你守一道门。','他想让你替他挡一下。'),
('而你，站在他们中间。','两个人都在等你答复。'),
('两人的距离，又落在你的肩上。','你又得去另一边传话。'),
('你接过她的话。\n两人的距离，又落在你的肩上。','你答应替她传话，得再去找他。'),
('“我只是怕，他不再需要我。”\n责备的背后，原来是害怕。','“我就是怕，他以后不再需要我了。”'),
('她沉默了。\n一句拒绝，还没有让她被听见。','她没再说话。你还没弄清她在担心什么。'),
('她的声音暂时安静了。去听 02 的声音。','这一边先说到这里。去 02 听听。'),
('你成了挡在他们中间的墙。\n保护，也可以成为另一种传话。','你答应替他应付她，接下来还得由你去说。'),
('“我需要自己的空间，不是失去她。”\n这句话，应该由他亲口说。','“我想自己做决定，也想跟她来往。”\n这些话，得让他自己说。'),
('他退回门后。\n有些话，还没有被理解。','他没答应。你还没弄清他想要什么。'),
('两种声音都留在这里了。现在去 03 作决定。','两边都听过了。去 03 决定接下来怎么办。'),
('他们都在等你。\n这一次，你要站在哪里？','他们都在等你。\n你还要继续传话吗？'),
('结局 01 / 退出三角\n你不再替他们说话。\n关系留给他们，路留给自己。','结局 01 / 退出三角\n你不再代传，让他们自己谈。\n你也可以先走了。'),
('结局 02 / 再次循环\n你仍站在两人的中间。\n有些话，还没有真正被听见。','结局 02 / 再次循环\n他们还在等你转告。\n谁也没把话直接说给对方听。'),
('He asks you to hold a boundary.','He asks you to keep her at a distance.'),
('And you stand between them.',"They're both waiting for your answer."),
('You take her words with you.\nThe distance between them becomes your burden.',"You agree to pass it on. You'll have to go and find him again."),
("I'm afraid he doesn't need me anymore.\nBeneath the accusation, a fear.","I'm afraid he won't need me anymore."),
('She falls silent.\nA refusal has not made her feel heard.',"She falls silent. You still don't know what's worrying her."),
('Her voice rests here. Listen to 02.',"That's all from her for now. Go and hear 02."),
('You become the wall between them.\nProtection can be another way of carrying words.',"You agree to deal with her for him. You're the one who has to go and talk."),
("I need my own space. I don't need to lose her.\nThese are words he could say himself.","I want to make my own choices and stay in touch with her.\nHe needs to say that himself."),
('He retreats behind the door.\nSomething remains unheard.',"He hasn't agreed. You still don't know what he wants."),
('Both voices rest here. Go to 03 and decide.',"You've heard both sides. Go to 03 and choose what to do next."),
("They're both waiting for you.\nThis time, where will you stand?","They're both waiting for you.\nWill you keep passing messages?"),
('ENDING 01 / RELEASE\nTheir words belong to them.\nThe way forward belongs to you.',"ENDING 01 / RELEASE\nYou stop passing messages and leave them to talk.\nYou can head out too."),
('ENDING 02 / THE LOOP\nYou still stand between them.\nSomething has yet to be heard.',"ENDING 02 / THE LOOP\nThey're still waiting for you to pass things on.\nNeither has spoken directly to the other."),
]
path=ROOT/'Assets/ThirdDoor/Scripts/LocalizationManager.cs'
source=path.read_text(encoding='utf-8-sig');applied=[]
for old,new in changes:
    quoted=json.dumps(old,ensure_ascii=False)
    # A substring within a larger sentence is not a string-literal target.
    if quoted not in source:continue
    source=source.replace(quoted,json.dumps(new,ensure_ascii=False));applied.append(dict(before=old,after=new))
path.write_text(source,encoding='utf-8')
(ROOT/'Documentation/Editorial/Decisions-Legacy-v6.json').write_text(json.dumps(applied,ensure_ascii=False,indent=2),encoding='utf-8')
print(f'COMPATIBILITY COPY / {len(applied)} reviewed edits; lookup keys retained')
