# 《第三扇门》 / The Third Door

第一人称符号化叙事原型 · Unity 2022.3 URP · 3-5 分钟体验两种结局

> 灵感取自家庭系统理论中的一个微小观察：两人系统不稳定，总会有第三个人被拉进来传话。
> 玩家在游戏中亲身成为这个"第三个支点"，决定是否走出去。

---

## 下载游玩

从 [Releases](../../releases) 下载最新版本的 `TheThirdDoor_Windows.zip`，解压后双击 `TheThirdDoor.exe` 即可运行。

**操作**：WASD 移动 · 鼠标视角 · `E` 交互 · `R` 重玩 · `ESC` 退出

---

## 设计文档

设计文档（`第三扇门_设计文档.docx`）和构建产物一并打包在每次 Release 中。

文档涵盖：立意动机、核心体验设计、玩法规则传达、系统模块分解、AI 协作模式。

---

## 项目特点

- **100% 程序化建场**：场景中没有手摆物件、没有 prefab、没有 ScriptableObject，所有几何/材质/UI/逻辑都由 `SceneBootstrapper.BuildScene()` 一次性构建
- **单变量驱动**：`directConnection`（一个 int）控制门的开合、天花板高度、引导文字、结局判定
- **三扇门即角色**：去掉了 NPC 模型，让门本身作为说话者，强化"位置感"
- **Edith Finch 风格字母逸散**：MOM / KID 标签悬浮在门前，玩家走近时物理碰撞触发飞散

---

## 本地编辑

需要 Unity 2022.3.62f3（理论上 2022.3 任意子版本兼容）。

1. clone 本仓库
2. Unity Hub → Add → 选择此目录
3. 打开 `Assets/Scenes/MainScene.unity`
4. 按 Play

打包：菜单 **Third Door → Build Windows EXE** 一键打包到 `Builds/Windows/`。

---

## AI 协作披露

本项目由人类设计师主导（视觉调性、文字内容、机制设计、迭代方向），代码实施由 [Claude Code](https://www.anthropic.com/claude-code)（Anthropic 出品的命令行 AI agent）配对完成。详见设计文档第六章。

---

## License

源代码 MIT。音效素材来自 [freesound.org](https://freesound.org) CC0 库。
