# 第三扇门 — 音效寻声清单

把下面 6 个 wav 文件按指定文件名放到 `Assets/Resources/Audio/` 下，运行时 AudioManager 会自动加载并播放。文件**缺失不会报错**（程序静默跳过该音效），所以可以分批补齐。

所有推荐均来自 [freesound.org](https://freesound.org)，CC0 许可可商用。

---

## 1. `letter_shatter.wav` — 字母逸散

**用途**：玩家走近门时 MOM / KID 三个字母被触发飞散瞬间播放
**性格**：轻盈，水晶 / 玻璃 / 风一闪而过
**时长**：0.8–1.5 秒
**音量建议**：放进游戏后听感约对话音量的 60%

**Freesound 搜索关键词**：
- `glass break short`
- `crystal shatter quick`
- `magic poof short`
- `paper rip whoosh`

**挑选标准**：开头要锐利、尾巴要短、不要有人声 / 旁白 / 音乐。

---

## 2. `locked_thud.wav` — 第三扇门未达成时按 E

**用途**："还不是时候"反馈瞬间的物理冲击音
**性格**：低频、沉重、单次，像被推不动的厚门 / 闷雷
**时长**：0.4–0.8 秒
**音量建议**：游戏内偏大，让人意识到"撞上了"

**Freesound 搜索关键词**：
- `heavy door thud`
- `deep wood knock single`
- `stone hit low`
- `door push fail`

**挑选标准**：低频成分要够，不要有金属共鸣（这是木 / 石的门感）。

---

## 3. `door_open_success.wav` — 好结局三门齐升

**用途**：玩家选 B + directConnection ≥ 2 触发好结局，三扇门同时上抬时播放
**性格**：低频鸣响渐起 + 透气感 + 微 swell（不要太"庆祝"，是"被允许通过"的克制喜悦）
**时长**：2.5–4 秒
**音量建议**：偏大但不刺，让结局有重量

**Freesound 搜索关键词**：
- `ambient riser short`
- `cinematic reveal soft`
- `door reveal swell`
- `bright drone uplifter`

**挑选标准**：不要"魔法升级"那种欢乐音色；要更沉静、空旷、有空间感。

---

## 4. `door_close_bad.wav` — 坏结局门关 + 天花板压

**用途**：坏结局触发瞬间播放
**性格**：低沉关门 + 压抑落定感
**时长**：1.5–3 秒
**音量建议**：偏大

**Freesound 搜索关键词**：
- `heavy door close slam`
- `metal door close deep`
- `crypt door shut`
- `low impact final`

**挑选标准**：尾音要有"留下沉默"的感觉，不要纯粹清脆的"啪"。

---

## 5. `choice_click.wav` — 对话选项点击

**用途**：玩家选红/蓝/第三扇门任一选项时的标点音
**性格**：极短、克制、单一木质或敲击感
**时长**：0.1–0.3 秒
**音量建议**：很小，70% 选项音量

**Freesound 搜索关键词**：
- `ui click soft wood`
- `subtle tap`
- `wood knock short single`
- `pencil tick`

**挑选标准**：不要科技感"哔"音；要木 / 纸 / 石的物质感。

---

## 6. `ambient_drone.wav` — 中庭环境底噪（循环）

**用途**：开场后常驻 loop，好结局停止
**性格**：极低频 drone，几乎听不见但能"感觉到"空间存在
**时长**：10–30 秒可循环（首尾能无缝衔接的）
**音量建议**：30%（游戏内非常背景）

**Freesound 搜索关键词**：
- `room tone drone loop`
- `dark ambient hum loopable`
- `subbass drone seamless`
- `empty room atmosphere loop`

**挑选标准**：必须是 "seamless loop" 或文件描述里提到循环。听 10 秒以上确认没有突变 / 起伏。

---

## 命名 & 放置规则

下载完成后：
1. 把每个文件**重命名为上述精确名字**（小写下划线，保留原后缀）
2. 拖到 Unity 的 `Assets/Resources/Audio/` 文件夹下
3. Unity 自动 import（默认设置即可，不需要 3D Spatialize）
4. 直接进 Play 测试

**支持的格式**：`.wav` / `.aiff` / `.aif` / `.mp3` / `.ogg` 任意一种都行。Resources.Load 看的是文件名，不看扩展名。比如下载到的是 `88809__lsprice__gb13.aiff`，重命名为 `letter_shatter.aiff` 即可。

如果某个音效暂时没找到，那个音效就静默，不影响其他音效播放。
