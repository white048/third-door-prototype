using System.Collections.Generic;
using UnityEngine;

public enum Language
{
    Chinese = 0,
    English = 1
}

// Static localization table. All player-visible text routes through Get(key).
// Language is chosen once on the opening Language Picker screen and persisted in PlayerPrefs.
public static class LocalizationManager
{
    private const string PrefKey = "ThirdDoor_Language";
    private static Language _language = Language.Chinese;
    private static bool _initialized;

    public static event System.Action OnLanguageChanged;

    public static Language Current
    {
        get { EnsureInitialized(); return _language; }
        set
        {
            bool changed = !_initialized || _language != value;
            _language = value;
            if(!EndingArchive.IsValidationRun){PlayerPrefs.SetInt(PrefKey, (int)value);PlayerPrefs.Save();}
            _initialized = true;
            if (changed && OnLanguageChanged != null)
            {
                OnLanguageChanged.Invoke();
            }
        }
    }

    public static bool HasChosen()
    {
        return PlayerPrefs.HasKey(PrefKey);
    }

    public static void ToggleLanguage()
    {
        Current=Current==Language.Chinese?Language.English:Language.Chinese;
        NarrativeFontWarmup.Warm();
    }

    public static void ResetChoice()
    {
        if(!EndingArchive.IsValidationRun)PlayerPrefs.DeleteKey(PrefKey);
        _initialized = false;
    }

    private static void EnsureInitialized()
    {
        if (_initialized) return;
        if (!EndingArchive.IsValidationRun&&PlayerPrefs.HasKey(PrefKey))
        {
            _language = (Language)PlayerPrefs.GetInt(PrefKey);
        }
        _initialized = true;
    }

    public static string Get(string key)
    {
        Dictionary<string, string> table = Current == Language.English ? EN : CN;
        if (table.TryGetValue(key, out string value)) return value;
        return "[" + key + "]";
    }

    private static readonly Dictionary<string, string> CN = new Dictionary<string, string>
    {
        // intro
        { "intro_line_1", "她请你带一句话。" },
        { "intro_line_2", "他想让你替他挡一下。" },
        { "intro_line_3", "两个人都在等你答复。" },
        { "intro_title",  "《第三扇门》" },
        { "intro_skip_hint", "空格 / Enter 进入展厅" },

        // language picker
        { "lang_title", "选择语言   ·   Choose Language" },
        { "lang_cn",    "中文" },
        { "lang_en",    "English" },

        // status
        { "status_initial",           "沿红线走向 01，听她的声音。" },
        { "status_third_door_tried",  "出口还未打开。先听 01，再听 02。" },
        { "status_red_door_pending",  "走向 01，听她的声音。" },
        { "status_blue_door_pending", "走向 02，听他的声音。" },
        { "status_final_available",   "走向 03。决定你在关系中的位置。" },

        // speakers
        { "speaker_red",   "01 / 牵挂" },
        { "speaker_blue",  "02 / 距离" },
        { "speaker_third", "03 / 边界" },

        // red door
        { "red_line",   "“告诉他，他不能总是把我关在门外。”" },
        { "red_opt_a",  "我会把话带给他。" },
        { "red_opt_b",  "你究竟想让他听见什么？" },
        { "red_opt_c",  "你自己跟他说。" },
        { "red_fb_a",   "你答应替她传话，得再去找他。" },
        { "red_fb_b",   "“我就是怕，他以后不再需要我了。”" },
        { "red_fb_c",   "她没再说话。你还没弄清她在担心什么。" },
        { "red_already", "这一边先说到这里。去 02 听听。" },

        // blue door
        { "blue_line",   "“让她别再替我决定一切。我需要喘口气。”" },
        { "blue_opt_a",  "我会替你挡住她。" },
        { "blue_opt_b",  "你是想她离开，还是想要空间？" },
        { "blue_opt_c",  "你自己跟她说。" },
        { "blue_fb_a",   "你答应替他应付她，接下来还得由你去说。" },
        { "blue_fb_b",   "“我想自己做决定，也想跟她来往。”\n这些话，得让他自己说。" },
        { "blue_fb_c_with",    "他终于抬起头。\n“好，这次我自己说。”" },
        { "blue_fb_c_without", "他没答应。你还没弄清他想要什么。" },
        { "blue_red_not_done", "先听 01 的声音。再回到这里。" },
        { "blue_already",      "两边都听过了。去 03 决定接下来怎么办。" },

        // third door
        { "third_line",   "他们都在等你。\n你还要继续传话吗？" },
        { "third_opt_a",  "我会继续替她和他传话。" },
        { "third_opt_b",  "让他们直接说。我退出。" },
        { "third_locked", "门没有回应。\n先听完 01 和 02，再决定怎样离开。" },

        // endings
        { "ending_good",   "结局 01 / 退出三角\n你不再代传，让他们自己谈。\n你也可以先走了。" },
        { "ending_bad",    "结局 02 / 再次循环\n他们还在等你转告。\n谁也没把话直接说给对方听。" },
        { "ending_restart", "R 重新体验   /   你可以继续探索" },

        // UI
        { "prompt_press_e", "[ E ]  倾听" },
        { "help_hint",      "WASD 行走 / 鼠标观察 / E 倾听 / F 传话 / 1–3 回应 / Q 卡牌 / Tab 手记 / V 长卷 / ESC 暂停 / R 重玩 / F2 语言" },
        { "scene_not_ready","场景还没有准备好。请退出 Play Mode 后重新进入。" },
    };

    private static readonly Dictionary<string, string> EN = new Dictionary<string, string>
    {
        // intro
        { "intro_line_1", "She asks you to carry a message." },
        { "intro_line_2", "He asks you to keep her at a distance." },
        { "intro_line_3", "They're both waiting for your answer." },
        { "intro_title",  "The Third Door" },
        { "intro_skip_hint", "SPACE / ENTER TO STEP INSIDE" },

        // language picker
        { "lang_title", "选择语言   ·   Choose Language" },
        { "lang_cn",    "中文" },
        { "lang_en",    "English" },

        // status
        { "status_initial",           "Follow the red line to 01. Listen to her." },
        { "status_third_door_tried",  "The exit is closed. Listen to 01, then 02." },
        { "status_red_door_pending",  "Approach 01. Listen to her." },
        { "status_blue_door_pending", "Approach 02. Listen to him." },
        { "status_final_available",   "Approach 03. Choose where you stand." },

        // speakers
        { "speaker_red",   "01 / THE WEIGHT" },
        { "speaker_blue",  "02 / THE DISTANCE" },
        { "speaker_third", "03 / THE BOUNDARY" },

        // red door
        { "red_line",   "Tell him he can't keep shutting me out." },
        { "red_opt_a",  "I'll pass it on to him." },
        { "red_opt_b",  "What do you actually want him to hear?" },
        { "red_opt_c",  "Tell him yourself." },
        { "red_fb_a",   "You agree to pass it on. You'll have to go and find him again." },
        { "red_fb_b",   "I'm afraid he won't need me anymore." },
        { "red_fb_c",   "She falls silent. You still don't know what's worrying her." },
        { "red_already", "That's all from her for now. Go and hear 02." },

        // blue door
        { "blue_line",   "Tell her to stop deciding everything for me. I need to breathe." },
        { "blue_opt_a",  "I'll keep her away from you." },
        { "blue_opt_b",  "Do you want her gone, or do you want space?" },
        { "blue_opt_c",  "Tell her yourself." },
        { "blue_fb_a",   "You agree to deal with her for him. You're the one who has to go and talk." },
        { "blue_fb_b",   "I want to make my own choices and stay in touch with her.\nHe needs to say that himself." },
        { "blue_fb_c_with",    "He looks up.\nAll right. This time, I'll say it myself." },
        { "blue_fb_c_without", "He hasn't agreed. You still don't know what he wants." },
        { "blue_red_not_done", "Listen to 01 first. Then return here." },
        { "blue_already",      "You've heard both sides. Go to 03 and choose what to do next." },

        // third door
        { "third_line",   "They're both waiting for you.\nWill you keep passing messages?" },
        { "third_opt_a",  "I'll keep carrying their words." },
        { "third_opt_b",  "Let them speak directly. I step out." },
        { "third_locked", "The door is silent.\nListen to 01 and 02 before deciding how to leave." },

        // endings
        { "ending_good",   "ENDING 01 / RELEASE\nYou stop passing messages and leave them to talk.\nYou can head out too." },
        { "ending_bad",    "ENDING 02 / THE LOOP\nThey're still waiting for you to pass things on.\nNeither has spoken directly to the other." },
        { "ending_restart", "R TO REPLAY   /   KEEP EXPLORING" },

        // UI
        { "prompt_press_e", "[ E ]  LISTEN" },
        { "help_hint",      "WASD MOVE / MOUSE LOOK / E LISTEN / F MESSAGE / 1–3 REPLY / Q CARDS / TAB NOTES / V STRIP / ESC PAUSE / R REPLAY / F2 LANG" },
        { "scene_not_ready","Scene isn't ready. Please exit Play Mode and re-enter." },
    };
}
