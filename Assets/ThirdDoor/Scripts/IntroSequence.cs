using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class IntroSequence : MonoBehaviour
{
    public static bool HasPlayed { get; private set; }
    public static bool IsActive { get; private set; }
    [SerializeField] private CanvasGroup blackGroup;
    [SerializeField] private ExhibitionText introText; private ExhibitionText skip;
    [SerializeField] private PlayerController player;
    private GameObject poster;
    private GameObject artwork;
    private ExhibitionText pickerTitle, caseBrief, homeLabel, officeLabel, continueLabel;
    private Button homeButton, officeButton, continueButton;
    private bool started,skipRequested;
    public static void ResetForLanguageChange(){HasPlayed=false;}
    public static void PrepareReload(bool skipIntro){HasPlayed=skipIntro;IsActive=false;}

    public void Configure(CanvasGroup overlay,ExhibitionText text,ExhibitionText hint,PlayerController controller,GameObject picker,ExhibitionText pickerTitle,Button chinese,Button english)
    {
        blackGroup=overlay;introText=text;skip=hint;player=controller;poster=picker;this.pickerTitle=pickerTitle;
        IsActive=!HasPlayed;
        RelationshipRun restored=CaseSession.TakePendingRestore();
        if(restored!=null){IsActive=true;player.SetControlsEnabled(false);poster.SetActive(false);StartCoroutine(Restore(restored));return;}
        if(HasPlayed){Finish();return;}
        player.SetControlsEnabled(false);
        chinese.onClick.AddListener(()=>Begin(Language.Chinese));
        english.onClick.AddListener(()=>Begin(Language.English));
        if(CaseSession.ConsumeAutoBegin())StartCoroutine(AutoBegin());
    }
    public void ConfigureCases(Button home,Button office,ExhibitionText brief,Button resume)
    {
        homeButton=home;officeButton=office;caseBrief=brief;continueButton=resume;
        homeLabel=home.GetComponentInChildren<ExhibitionText>();officeLabel=office.GetComponentInChildren<ExhibitionText>();
        continueLabel=resume.GetComponentInChildren<ExhibitionText>();
        home.onClick.AddListener(()=>SelectCase(CaseId.Home));
        office.onClick.AddListener(()=>SelectCase(CaseId.Office));
        resume.onClick.AddListener(()=>{if(!CaseSession.ContinueSaved())RefreshPicker();});
        RefreshPicker();
    }
    private void OnEnable(){LocalizationManager.OnLanguageChanged+=RefreshPicker;}
    public void ConfigureArtwork(GameObject plate){artwork=plate;artwork.SetActive(false);}
    private void OnDisable(){LocalizationManager.OnLanguageChanged-=RefreshPicker;}
    public void SelectCase(CaseId selected)
    {
        if(started)return;
        CaseSession.Select(selected);RefreshPicker();
    }
    private void RefreshPicker()
    {
        bool cn=LocalizationManager.Current==Language.Chinese;
        if(pickerTitle!=null)pickerTitle.text=CaseInfo.Title(CaseSession.Current);
        if(caseBrief!=null)caseBrief.text=CaseInfo.Brief(CaseSession.Current);
        if(homeLabel!=null)homeLabel.text=(CaseSession.Current==CaseId.Home?"●   ":"○   ")+CaseInfo.Title(CaseId.Home);
        if(officeLabel!=null)officeLabel.text=(CaseSession.Current==CaseId.Office?"●   ":"○   ")+CaseInfo.Title(CaseId.Office);
        if(homeButton!=null)homeButton.interactable=!started;
        if(officeButton!=null)officeButton.interactable=!started;
        if(continueButton!=null){continueButton.gameObject.SetActive(CheckpointStore.HasCheckpoint);continueButton.interactable=!started;}
        if(continueLabel!=null)continueLabel.text=cn?"继续上次 / CONTINUE":"CONTINUE SAVED JOURNEY";
    }
    public void Begin(Language language)
    {
        if(started)return;
        started=true;
        LocalizationManager.Current=language;
        NarrativeFontWarmup.Warm();
        poster.SetActive(false);
        if(artwork!=null)artwork.SetActive(true);
        skip.text=LocalizationManager.Get("intro_skip_hint");
        StartCoroutine(Reveal());
    }
    private void Update()
    {
        if(NarrativeNotebook.Open||ExhibitionHUD.Paused)return;
        if(!IsActive)return;
        if(!started)
        {
            if(Input.GetKeyDown(KeyCode.Alpha1))Begin(Language.Chinese);
            else if(Input.GetKeyDown(KeyCode.Alpha2))Begin(Language.English);
            else if(Input.GetKeyDown(KeyCode.Return))Begin(LocalizationManager.Current);
            else if(Input.GetKeyDown(KeyCode.LeftArrow))SelectCase(CaseId.Home);
            else if(Input.GetKeyDown(KeyCode.RightArrow))SelectCase(CaseId.Office);
        }
        else if(Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.Return))skipRequested=true;
    }
    private IEnumerator Reveal()
    {
        // Give the language selection key one frame to clear before accepting a skip.
        yield return null;
        GameStateManager.Instance.BeginCase(CaseSession.Current);
        introText.text=CaseInfo.Intro(CaseSession.Current);
        float elapsed=0;
        while(elapsed<4.5f&&!skipRequested){elapsed+=Time.unscaledDeltaTime;yield return null;}
        elapsed=0;
        while(elapsed<.85f)
        {
            elapsed+=Time.unscaledDeltaTime;
            blackGroup.alpha=1-Mathf.SmoothStep(0,1,elapsed/.85f);
            yield return null;
        }
        HasPlayed=true;
        Finish();
    }
    private IEnumerator AutoBegin(){yield return null;Begin(LocalizationManager.Current);}
    private IEnumerator Restore(RelationshipRun saved)
    {
        yield return null;
        GameStateManager.Instance.RestoreRun(saved);
        HasPlayed=true;Finish();
    }
    private void Finish()
    {
        IsActive=false;
        blackGroup.alpha=0;blackGroup.blocksRaycasts=false;
        gameObject.SetActive(false);
        if(player!=null)player.SetControlsEnabled(!NarrativeNotebook.Open&&!ExhibitionHUD.Paused);
    }
}
