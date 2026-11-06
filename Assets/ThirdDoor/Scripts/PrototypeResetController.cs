using UnityEngine;
using UnityEngine.SceneManagement;

public class PrototypeResetController : MonoBehaviour
{
    private void Update()
    {
        if (IntroSequence.IsActive || ExhibitionHUD.Paused || NarrativeNotebook.Open) return;
        if (Input.GetKeyDown(KeyCode.R))
        {
            ReloadScene();
        }
        else if (Input.GetKeyDown(KeyCode.F2))
        {
            LocalizationManager.ToggleLanguage();
        }
    }

    private static void ReloadScene()
    {
        CaseSession.StartFresh(GameStateManager.Instance.Run.Case);
    }
}
