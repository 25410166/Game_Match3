using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TutorialProgressManager : MonoBehaviour
{
    public static TutorialProgressManager Instance { get; private set; }

    [Header("Debug State")]
    [SerializeField] private bool tutorial1Triggered;
    [SerializeField] private bool tutorial1Completed;
    [SerializeField] private bool tutorial1Skipped;

    private TutorialId activeTutorial = TutorialId.None;
    private TutorialStepId activeStep = TutorialStepId.None;
    private TutorialFocusTarget focusedTarget;
    private bool isSceneHookRegistered;
    private Coroutine pendingStepCoroutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        GameObject go = new GameObject("TutorialProgressManager");
        Instance = go.AddComponent<TutorialProgressManager>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        RegisterSceneHook();
        SyncFromPlayerData();

        if (TutorialPopupManager.Instance == null)
            FindObjectOfType<TutorialPopupManager>(true);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    public void NotifyStarterPetConfirmed()
    {
        if (tutorial1Completed)
            return;

        tutorial1Triggered = true;
        SaveToPlayerData();

        if (SceneManager.GetActiveScene().name == "SceneHome")
            TryStartTutorial1();
    }

    public void NotifyAdventureButtonClicked()
    {
        if (activeTutorial != TutorialId.Tutorial1 || activeStep != TutorialStepId.HomeAdventureButton)
            return;

        ClearFocus();
        HidePopup();
        ScheduleStep(TutorialStepId.HomeFirstMap, GetHomePopupDelay());
    }

    public void NotifyFirstMapClicked()
    {
        if (activeTutorial != TutorialId.Tutorial1 || activeStep != TutorialStepId.HomeFirstMap)
            return;

        CancelPendingStep();
        ClearFocus();
        HidePopup();
        activeStep = TutorialStepId.HomePrebattleStart;
    }

    public void NotifyPrebattleOpened()
    {
        if (activeTutorial == TutorialId.Tutorial1 && activeStep == TutorialStepId.HomePrebattleStart)
            ShowStep(TutorialStepId.HomePrebattleStart);
    }

    public void NotifyPrebattleStartClicked()
    {
        if (activeTutorial != TutorialId.Tutorial1 || activeStep != TutorialStepId.HomePrebattleStart)
            return;

        CompleteTutorial1();
    }

    public bool IsTutorialStepActive(TutorialStepId stepId)
    {
        return activeTutorial == TutorialId.Tutorial1 && activeStep == stepId;
    }

    private void RegisterSceneHook()
    {
        if (isSceneHookRegistered)
            return;

        isSceneHookRegistered = true;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CancelPendingStep();
        ClearFocus();
        HidePopup();
        SyncFromPlayerData();

        if (scene.name == "SceneHome")
            TryStartTutorial1();
    }

    private void TryStartTutorial1()
    {
        if (!tutorial1Triggered || tutorial1Completed)
            return;

        activeTutorial = TutorialId.Tutorial1;
        ShowStep(TutorialStepId.HomeAdventureButton);
    }

    private void ShowStep(TutorialStepId stepId)
    {
        CancelPendingStep();
        ClearFocus();
        HidePopup();

        activeStep = stepId;

        switch (stepId)
        {
            case TutorialStepId.HomeAdventureButton:
                Debug.Log("[Tutorial] Step T1 -> Adventure");
                FocusAndShow(MapPopupUI.AdventureButtonStatic, "T1");
                break;
            case TutorialStepId.HomeFirstMap:
                Debug.Log("[Tutorial] Step T2 -> First Map");
                FocusAndShow(MapPopupUI.FirstMapButtonStatic, "T2");
                break;
            case TutorialStepId.HomePrebattleStart:
                Debug.Log("[Tutorial] Step T3 -> Prebattle Start");
                FocusAndShow(PrebattlePopupUI.StartButtonStatic, "T3");
                break;
        }
    }

    private void FocusAndShow(Button button, string message)
    {
        if (button == null)
            return;

        TutorialFocusTarget focus = button.GetComponent<TutorialFocusTarget>();
        if (focus == null)
            focus = button.gameObject.AddComponent<TutorialFocusTarget>();

        focus.ApplyFocus();
        focusedTarget = focus;

        if (focus.RectTransform != null && TutorialPopupManager.Instance != null)
            TutorialPopupManager.Instance.ShowStep(message, focus.RectTransform, SkipActiveTutorial);
    }

    private void SkipActiveTutorial()
    {
        if (activeTutorial != TutorialId.Tutorial1)
            return;

        tutorial1Skipped = true;
        CompleteTutorial1();
    }

    private void CompleteTutorial1()
    {
        tutorial1Completed = true;
        activeTutorial = TutorialId.None;
        activeStep = TutorialStepId.None;
        CancelPendingStep();
        ClearFocus();
        HidePopup();
        SaveToPlayerData();
    }

    private void HidePopup()
    {
        if (TutorialPopupManager.Instance != null)
            TutorialPopupManager.Instance.Hide();
    }

    private void ClearFocus()
    {
        if (focusedTarget == null)
            return;

        focusedTarget.ClearFocus();
        focusedTarget = null;
    }

    private void SyncFromPlayerData()
    {
        if (PlayerManager.Instance == null || PlayerManager.Instance.Data == null)
            return;

        tutorial1Triggered = PlayerManager.Instance.Data.tutorial1Triggered;
        tutorial1Completed = PlayerManager.Instance.Data.tutorial1Completed;
        tutorial1Skipped = PlayerManager.Instance.Data.tutorial1Skipped;
    }

    private void SaveToPlayerData()
    {
        if (PlayerManager.Instance == null || PlayerManager.Instance.Data == null)
            return;

        PlayerManager.Instance.Data.tutorial1Triggered = tutorial1Triggered;
        PlayerManager.Instance.Data.tutorial1Completed = tutorial1Completed;
        PlayerManager.Instance.Data.tutorial1Skipped = tutorial1Skipped;
        PlayerManager.Instance.SaveData();
    }

    private void ScheduleStep(TutorialStepId stepId, float delay)
    {
        CancelPendingStep();
        pendingStepCoroutine = StartCoroutine(CoShowStepDelayed(stepId, delay));
    }

    private IEnumerator CoShowStepDelayed(TutorialStepId stepId, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        pendingStepCoroutine = null;
        ShowStep(stepId);
    }

    private void CancelPendingStep()
    {
        if (pendingStepCoroutine == null)
            return;

        StopCoroutine(pendingStepCoroutine);
        pendingStepCoroutine = null;
    }

    private float GetHomePopupDelay()
    {
        MapPopupUI mapPopup = FindObjectOfType<MapPopupUI>(true);
        if (mapPopup == null)
            return 0.3f;

        return mapPopup.PopupSlideDuration + 0.05f;
    }

    private string GetLocalizedTutorialText(string key)
    {
        if (LocalizationManager.Instance != null && LocalizationManager.Instance.IsLoaded)
            return LocalizationManager.Instance.GetText(key, key);

        return key;
    }
}



