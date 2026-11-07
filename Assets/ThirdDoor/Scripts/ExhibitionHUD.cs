using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ExhibitionHUD : MonoBehaviour
{
    public static bool Paused { get; private set; }
    private PlayerController player;
    private ExhibitionText help,chapter,sensitivity,audioLabel,resumeLabel,restartLabel,quitLabel,languageLabel;
    private ExhibitionText navigationCaption,navigationHint,replayLabel,nextLabel,titleLabel,pauseTitleLabel;
    private GameObject endingCard,crosshair,objective,pause,caseNavigation;
    private bool restoreControls,endingCursor,endingRead=true;
    public void Configure(PlayerController p,ExhibitionText h,ExhibitionText c,GameObject ending,GameObject focus,GameObject intention,Font font)
    {player=p;help=h;chapter=c;endingCard=ending;crosshair=focus;objective=intention;Paused=false;}
    public void ConfigurePause(GameObject panel,Button resume,Button restart,Button quit,Button slower,Button faster,ExhibitionText sens,Button audio)
    {
        pause=panel;sensitivity=sens;audioLabel=audio.GetComponentInChildren<ExhibitionText>();
        resumeLabel=resume.GetComponentInChildren<ExhibitionText>();
        restartLabel=restart.GetComponentInChildren<ExhibitionText>();
        quitLabel=quit.GetComponentInChildren<ExhibitionText>();
        resume.onClick.AddListener(()=>SetPaused(false));
        restart.onClick.AddListener(Restart);
        quit.onClick.AddListener(Quit);
        slower.onClick.AddListener(()=>player.Sensitivity-=.25f);
        faster.onClick.AddListener(()=>player.Sensitivity+=.25f);
        audio.onClick.AddListener(()=>{AudioListener.volume=AudioListener.volume>0?0:1;});
        pause.SetActive(false);
        Refresh();
    }
    private void OnEnable(){LocalizationManager.OnLanguageChanged+=Refresh;}
    public void ConfigureLanguage(Button button)
    {languageLabel=button.GetComponentInChildren<ExhibitionText>();button.onClick.AddListener(LocalizationManager.ToggleLanguage);Refresh();}
    public void ConfigureSession(GameObject navigation,ExhibitionText caption,ExhibitionText hint,Button replay,Button next,Button title,Button pauseTitle)
    {
        caseNavigation=navigation;navigationCaption=caption;navigationHint=hint;
        replayLabel=replay.GetComponentInChildren<ExhibitionText>();nextLabel=next.GetComponentInChildren<ExhibitionText>();
        titleLabel=title.GetComponentInChildren<ExhibitionText>();pauseTitleLabel=pauseTitle.GetComponentInChildren<ExhibitionText>();
        replay.onClick.AddListener(()=>CaseSession.StartFresh(GameStateManager.Instance.Run.Case));
        next.onClick.AddListener(()=>CaseSession.StartFresh(GameStateManager.Instance.Run.Case==CaseId.Home?CaseId.Office:CaseId.Home,true));
        title.onClick.AddListener(CaseSession.ReturnToTitle);pauseTitle.onClick.AddListener(CaseSession.ReturnToTitle);
        Refresh();
    }
    private void OnDisable(){LocalizationManager.OnLanguageChanged-=Refresh;Time.timeScale=1;Paused=false;}
    private void Refresh()
    {
        bool cn=LocalizationManager.Current==Language.Chinese;
        if(help!=null)help.text=LocalizationManager.Get("help_hint");
        if(resumeLabel!=null)resumeLabel.text=cn?"继续   /   RESUME":"RESUME";
        if(restartLabel!=null)restartLabel.text=cn?"重新开始 / RESTART":"RESTART";
        if(quitLabel!=null)quitLabel.text=cn?"退出游戏 / QUIT":"QUIT";
        if(languageLabel!=null)languageLabel.text=cn?"F2 / 中文 → English":"F2 / ENGLISH → 中文";
        if(replayLabel!=null)replayLabel.text=cn?"再玩本案例 / REPLAY":"REPLAY THIS CASE";
        if(nextLabel!=null)nextLabel.text=cn?"另一个案例 / NEXT CASE":"PLAY THE OTHER CASE";
        if(titleLabel!=null)titleLabel.text=cn?"返回目录 / TITLE":"RETURN TO TITLE";
        if(pauseTitleLabel!=null)pauseTitleLabel.text=cn?"返回目录 / TITLE":"RETURN TO TITLE";
        if(navigationHint!=null)navigationHint.text=cn?"按住 Alt 点击菜单 · R 重玩 · N 切换 · Home 目录":"HOLD ALT TO CLICK · R REPLAY · N NEXT · HOME TITLE";
    }
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape)&&!IntroSequence.IsActive&&!NarrativeNotebook.Open&&NarrativeNotebook.EscapeFrame!=Time.frameCount)
        {
            var conversation=GetComponent<DialogueUI>();
            if(Paused||conversation==null||!conversation.CancelChoice())SetPaused(!Paused);
        }
        if(Paused&&Input.GetKeyDown(KeyCode.F2))LocalizationManager.ToggleLanguage();
        bool cn=LocalizationManager.Current==Language.Chinese;
        if(sensitivity!=null)sensitivity.text=(cn?"鼠标灵敏度":"MOUSE SENSITIVITY")+"   /   "+player.Sensitivity.ToString("0.00");
        if(audioLabel!=null)audioLabel.text=(cn?"声音":"AUDIO")+"   /   "+(AudioListener.volume>0?"ON":"OFF");
        var state=GameStateManager.Instance;
        if(state==null)return;
        bool ended=state.gameEnded;
        bool endingVisible=ended&&GetComponent<DialogueUI>().EndingVisible&&!GetComponent<DialogueUI>().IsDialogueOpen;
        if(endingVisible&&!Paused&&!NarrativeNotebook.Open&&!IntroSequence.IsActive&&DialogueUI.LastClosedFrame!=Time.frameCount)
        {
            if(Input.GetKeyDown(KeyCode.Space))ToggleEndingOverlay();
            bool freeCursor=Input.GetKey(KeyCode.LeftAlt)||Input.GetKey(KeyCode.RightAlt);
            if(freeCursor){endingCursor=true;if(player.ControlsEnabled)player.SetControlsEnabled(false);}
            else if(endingCursor){endingCursor=false;player.SetControlsEnabled(true);}
            if(Input.GetKeyDown(KeyCode.N)){CaseSession.StartFresh(state.Run.Case==CaseId.Home?CaseId.Office:CaseId.Home,true);return;}
            if(Input.GetKeyDown(KeyCode.Home)){CaseSession.ReturnToTitle();return;}
        }
        endingCard.SetActive(endingVisible&&endingRead&&!Paused&&!NarrativeNotebook.Open);
        if(caseNavigation!=null)caseNavigation.SetActive(endingVisible&&endingRead&&!Paused&&!NarrativeNotebook.Open&&!IntroSequence.IsActive);
        if(help!=null)help.text=endingVisible?NarrativeText.L("空格 收起/展开结局 / Alt 点击菜单 / Tab 回看 / R 重玩 / N 切换案例 / Esc 暂停","SPACE FOLD / UNFOLD ENDING / ALT CLICK MENU / TAB REVISIT / R REPLAY / N NEXT CASE / ESC PAUSE"):LocalizationManager.Get("help_hint");
        if(help!=null&&state.Run.DeckRules&&!endingVisible)help.text=NarrativeText.L("WASD 行走 / 鼠标观察 / E 接话与阅读 / F 组句出牌 / Tab 手牌 / V 长卷 / Esc 暂停 / F2 语言","WASD MOVE / MOUSE LOOK / E LISTEN OR READ / F COMPOSE / TAB HAND / V STRIP / ESC PAUSE / F2 LANGUAGE");
        if(navigationCaption!=null)navigationCaption.text=(cn?"案例完成 / ":"CASE COMPLETE / ")+CaseInfo.Title(state.Run.Case);
        objective.SetActive(!ended&&!IntroSequence.IsActive&&!WorkplanStation.Focused);
        crosshair.SetActive(player.ControlsEnabled&&!Paused&&!IntroSequence.IsActive);
        chapter.text=ended?"ENDING 0"+(int)state.Run.Ending+" / 07":"CASE 0"+((int)state.Run.Case+1)+" / ROUND 0"+(state.Run.Phase+1);
    }
    public void SetPaused(bool value)
    {
        if(pause==null||IntroSequence.IsActive)return;
        if(value){restoreControls=player.ControlsEnabled;player.SetControlsEnabled(false);}
        Paused=value;pause.SetActive(value);Time.timeScale=value?0:1;
        if(!value)player.SetControlsEnabled(restoreControls);
    }
    public void ToggleEndingOverlay(){if(GameStateManager.Instance!=null&&GameStateManager.Instance.Run.Ended)endingRead=!endingRead;}
    public void Restart()
    {
        Time.timeScale=1;Paused=false;
        CaseSession.StartFresh(GameStateManager.Instance.Run.Case);
    }
    private static void Quit()
    {
        Time.timeScale=1;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying=false;
#else
        Application.Quit();
#endif
    }
}

