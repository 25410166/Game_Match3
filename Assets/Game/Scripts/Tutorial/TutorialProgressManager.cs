using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private bool tutorial2Triggered;
    [SerializeField] private bool tutorial2Completed;
    [SerializeField] private bool tutorial2Skipped;
    [SerializeField] private bool tutorial3Triggered;
    [SerializeField] private bool tutorial3Completed;
    [SerializeField] private bool tutorial3Skipped;

    private TutorialId activeTutorial = TutorialId.None;
    private TutorialStepId activeStep = TutorialStepId.None;
    private readonly List<TutorialFocusTarget> focusedTargets = new List<TutorialFocusTarget>();
    private readonly List<Items> highlightedTutorialItems = new List<Items>();
    private bool isSceneHookRegistered;
    private Coroutine pendingStepCoroutine;
    private Coroutine pendingBattleStartCoroutine;
    private Coroutine boardStepDisplayCoroutine;
    private Coroutine pendingHomeTutorial3Coroutine;

    private Board battleBoard;
    private BattleGemInfoPopupUI battleGemInfoPopup;
    private HomeElementPopupUI battleElementPopup;
    private Items tutorial2FirstItem;
    private Items tutorial2SecondItem;
    private bool waitingForTutorial2PlayerTurn;

    private LevelRewardPopupUI homeLevelRewardPopup;
    private ChoosePet homeChoosePet;
    private GemUpdate homeGemUpdate;

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

        UnsubscribeBattleObjects();
        UnsubscribeHomeObjects();
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
        ClearItemHighlights();
        HidePopup();
        ScheduleStep(TutorialStepId.HomeFirstMap, GetHomePopupDelay());
    }

    public void NotifyFirstMapClicked()
    {
        if (activeTutorial != TutorialId.Tutorial1 || activeStep != TutorialStepId.HomeFirstMap)
            return;

        CancelPendingStep();
        ClearFocus();
        ClearItemHighlights();
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

    public void NotifyBattleBoardItemSelected(Items item)
    {
        if (item == null || activeTutorial != TutorialId.Tutorial2)
            return;

        if (activeStep == TutorialStepId.BattleBoardFirstGem && item == tutorial2FirstItem)
            ShowStep(TutorialStepId.BattleBoardSecondGem);
    }

    public void NotifyBattleTurnChanged(GameManager.Turn turn)
    {
        if (activeTutorial != TutorialId.Tutorial2)
            return;

        if (waitingForTutorial2PlayerTurn && turn == GameManager.Turn.Player)
        {
            waitingForTutorial2PlayerTurn = false;
            ShowStep(TutorialStepId.BattleGemInfoOpen);
        }
    }

    public bool IsTutorialStepActive(TutorialStepId stepId)
    {
        return activeStep == stepId;
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
        CancelBoardStepDisplayRoutine();
        CancelBattleStartRoutine();
        CancelHomeTutorial3Routine();
        ClearFocus();
        ClearItemHighlights();
        HidePopup();
        UnsubscribeBattleObjects();
        UnsubscribeHomeObjects();
        waitingForTutorial2PlayerTurn = false;
        tutorial2FirstItem = null;
        tutorial2SecondItem = null;
        SyncFromPlayerData();

        if (scene.name == "SceneHome")
        {
            TryStartTutorial1();
            TryStartTutorial3();
        }
        else if (scene.name == "SceneBattle")
        {
            TryStartTutorial2();
        }
    }

    private void TryStartTutorial1()
    {
        if (!tutorial1Triggered || tutorial1Completed)
            return;

        activeTutorial = TutorialId.Tutorial1;
        ShowStep(TutorialStepId.HomeAdventureButton);
    }

    private void TryStartTutorial2()
    {
        if (!tutorial1Completed || tutorial2Completed)
            return;

        tutorial2Triggered = true;
        SaveToPlayerData();
        CancelBattleStartRoutine();
        pendingBattleStartCoroutine = StartCoroutine(CoTryStartTutorial2());
    }

    private IEnumerator CoTryStartTutorial2()
    {
        for (int i = 0; i < 90; i++)
        {
            ResolveBattleObjects();

            if (battleBoard != null)
            {
                SubscribeBattleObjects();
                if (RefreshTutorial2SwapTargets())
                {
                    pendingBattleStartCoroutine = null;
                    activeTutorial = TutorialId.Tutorial2;
                    ShowStep(TutorialStepId.BattleBoardFirstGem);
                    yield break;
                }
            }

            yield return null;
        }

        pendingBattleStartCoroutine = null;
    }

    private void TryStartTutorial3()
    {
        if (!tutorial2Completed || tutorial3Completed)
            return;

        if (!CanTriggerTutorial3())
            return;

        tutorial3Triggered = true;
        SaveToPlayerData();
        CancelHomeTutorial3Routine();
        pendingHomeTutorial3Coroutine = StartCoroutine(CoTryStartTutorial3());
    }

    private bool CanTriggerTutorial3()
    {
        if (PlayerManager.Instance == null || PlayerManager.Instance.Data == null)
            return false;

        if (PlayerManager.Instance.Data.level < 2)
            return false;

        return PlayerManager.Instance.GetMapWinCount("101") > 0;
    }

    private IEnumerator CoTryStartTutorial3()
    {
        for (int i = 0; i < 180; i++)
        {
            ResolveHomeObjects();
            SubscribeHomeObjects();

            if (homeLevelRewardPopup != null)
            {
                if (homeLevelRewardPopup.IsOpen)
                {
                    StartTutorial3FromRewardPopup();
                    yield break;
                }

                homeLevelRewardPopup.TryShowPendingReward();
            }

            yield return null;
        }

        pendingHomeTutorial3Coroutine = null;
    }

    private void StartTutorial3FromRewardPopup()
    {
        pendingHomeTutorial3Coroutine = null;

        if (activeTutorial == TutorialId.Tutorial3 && activeStep == TutorialStepId.HomeLevelRewardContinue)
            return;

        activeTutorial = TutorialId.Tutorial3;
        ShowStep(TutorialStepId.HomeLevelRewardContinue);
    }

    private void ResolveBattleObjects()
    {
        battleBoard = Board.Instance != null ? Board.Instance : FindObjectOfType<Board>(true);
        if (battleGemInfoPopup == null)
            battleGemInfoPopup = FindObjectOfType<BattleGemInfoPopupUI>(true);
        if (battleElementPopup == null)
            battleElementPopup = FindObjectOfType<HomeElementPopupUI>(true);
    }

    private void ResolveHomeObjects()
    {
        if (homeLevelRewardPopup == null)
            homeLevelRewardPopup = FindObjectOfType<LevelRewardPopupUI>(true);
        if (homeChoosePet == null)
            homeChoosePet = FindObjectOfType<ChoosePet>(true);
        homeGemUpdate = homeChoosePet != null ? homeChoosePet.GemUpdateUI : FindObjectOfType<GemUpdate>(true);
    }

    private bool RefreshTutorial2SwapTargets()
    {
        if (battleBoard == null)
            return false;

        bool success = battleBoard.TryGetSuggestedSwap(out tutorial2FirstItem, out tutorial2SecondItem)
            && tutorial2FirstItem != null
            && tutorial2SecondItem != null;

        if (success)
            Debug.Log($"[Tutorial] Tutorial2 swap target -> first=({tutorial2FirstItem.column},{tutorial2FirstItem.row}) second=({tutorial2SecondItem.column},{tutorial2SecondItem.row})");

        return success;
    }

    private void SubscribeBattleObjects()
    {
        if (battleBoard != null)
        {
            battleBoard.OnBoardInitialized -= HandleBoardInitialized;
            battleBoard.OnBoardInitialized += HandleBoardInitialized;
            battleBoard.OnSuccessfulPlayerSwapResolved -= HandleSuccessfulPlayerSwapResolved;
            battleBoard.OnSuccessfulPlayerSwapResolved += HandleSuccessfulPlayerSwapResolved;
        }

        if (battleGemInfoPopup != null)
        {
            battleGemInfoPopup.OnOpened -= HandleBattleGemInfoOpened;
            battleGemInfoPopup.OnOpened += HandleBattleGemInfoOpened;
            battleGemInfoPopup.OnClosed -= HandleBattleGemInfoClosed;
            battleGemInfoPopup.OnClosed += HandleBattleGemInfoClosed;
        }

        if (battleElementPopup != null)
        {
            battleElementPopup.OnOpened -= HandleBattleElementOpened;
            battleElementPopup.OnOpened += HandleBattleElementOpened;
            battleElementPopup.OnClosed -= HandleBattleElementClosed;
            battleElementPopup.OnClosed += HandleBattleElementClosed;
        }
    }

    private void UnsubscribeBattleObjects()
    {
        if (battleBoard != null)
        {
            battleBoard.OnBoardInitialized -= HandleBoardInitialized;
            battleBoard.OnSuccessfulPlayerSwapResolved -= HandleSuccessfulPlayerSwapResolved;
        }
        if (battleGemInfoPopup != null)
        {
            battleGemInfoPopup.OnOpened -= HandleBattleGemInfoOpened;
            battleGemInfoPopup.OnClosed -= HandleBattleGemInfoClosed;
        }
        if (battleElementPopup != null)
        {
            battleElementPopup.OnOpened -= HandleBattleElementOpened;
            battleElementPopup.OnClosed -= HandleBattleElementClosed;
        }
        battleBoard = null;
        battleGemInfoPopup = null;
        battleElementPopup = null;
    }

    private void SubscribeHomeObjects()
    {
        if (homeLevelRewardPopup != null)
        {
            homeLevelRewardPopup.OnOpened -= HandleHomeLevelRewardOpened;
            homeLevelRewardPopup.OnOpened += HandleHomeLevelRewardOpened;
            homeLevelRewardPopup.OnRewardClaimed -= HandleHomeLevelRewardClaimed;
            homeLevelRewardPopup.OnRewardClaimed += HandleHomeLevelRewardClaimed;
        }
        if (homeChoosePet != null)
        {
            if (homeChoosePet.TrainingButton != null)
            {
                homeChoosePet.TrainingButton.onClick.RemoveListener(HandleTrainingButtonClicked);
                homeChoosePet.TrainingButton.onClick.AddListener(HandleTrainingButtonClicked);
            }
            if (homeChoosePet.UpgradeButton != null)
            {
                homeChoosePet.UpgradeButton.onClick.RemoveListener(HandleTrainingUpgradeButtonClicked);
                homeChoosePet.UpgradeButton.onClick.AddListener(HandleTrainingUpgradeButtonClicked);
            }
        }
        if (homeGemUpdate != null)
        {
            homeGemUpdate.OnPopupOpened -= HandleGemPopupOpened;
            homeGemUpdate.OnPopupOpened += HandleGemPopupOpened;
            homeGemUpdate.OnGemSelected -= HandleGemSelected;
            homeGemUpdate.OnGemSelected += HandleGemSelected;
        }
    }

    private void UnsubscribeHomeObjects()
    {
        if (homeLevelRewardPopup != null)
        {
            homeLevelRewardPopup.OnOpened -= HandleHomeLevelRewardOpened;
            homeLevelRewardPopup.OnRewardClaimed -= HandleHomeLevelRewardClaimed;
        }
        if (homeChoosePet != null)
        {
            if (homeChoosePet.TrainingButton != null)
                homeChoosePet.TrainingButton.onClick.RemoveListener(HandleTrainingButtonClicked);
            if (homeChoosePet.UpgradeButton != null)
                homeChoosePet.UpgradeButton.onClick.RemoveListener(HandleTrainingUpgradeButtonClicked);
        }
        if (homeGemUpdate != null)
        {
            homeGemUpdate.OnPopupOpened -= HandleGemPopupOpened;
            homeGemUpdate.OnGemSelected -= HandleGemSelected;
        }
        homeLevelRewardPopup = null;
        homeChoosePet = null;
        homeGemUpdate = null;
    }

    private void HandleBoardInitialized()
    {
        if (SceneManager.GetActiveScene().name != "SceneBattle")
            return;

        if (!tutorial1Completed || tutorial2Completed)
            return;

        TryStartTutorial2();
    }

    private void HandleSuccessfulPlayerSwapResolved()
    {
        if (activeTutorial != TutorialId.Tutorial2 || activeStep != TutorialStepId.BattleBoardSecondGem)
            return;

        Debug.Log("[Tutorial] Step T5 completed -> wait player turn for T6");
        CancelBoardStepDisplayRoutine();
        ClearFocus();
        ClearItemHighlights();
        HidePopup();
        waitingForTutorial2PlayerTurn = true;
        if (GameManager.Instance != null)
            GameManager.Instance.ResumeTurnTimerAfterTutorial();
    }

    private void HandleBattleGemInfoOpened()
    {
        if (activeTutorial == TutorialId.Tutorial2 && activeStep == TutorialStepId.BattleGemInfoOpen)
            ShowStep(TutorialStepId.BattleGemInfoClose);
    }

    private void HandleBattleGemInfoClosed()
    {
        if (activeTutorial == TutorialId.Tutorial2 && activeStep == TutorialStepId.BattleGemInfoClose)
            ShowStep(TutorialStepId.BattleElementOpen);
    }

    private void HandleBattleElementOpened()
    {
        if (activeTutorial == TutorialId.Tutorial2 && activeStep == TutorialStepId.BattleElementOpen)
            ShowStep(TutorialStepId.BattleElementClose);
    }

    private void HandleBattleElementClosed()
    {
        if (activeTutorial == TutorialId.Tutorial2 && activeStep == TutorialStepId.BattleElementClose)
            CompleteTutorial2();
    }

    private void HandleHomeLevelRewardOpened()
    {
        if (tutorial3Completed || !CanTriggerTutorial3())
            return;

        StartTutorial3FromRewardPopup();
    }

    private void HandleHomeLevelRewardClaimed()
    {
        if (activeTutorial == TutorialId.Tutorial3 && activeStep == TutorialStepId.HomeLevelRewardContinue)
            ShowStep(TutorialStepId.HomeTrainingButton);
    }

    private void HandleTrainingButtonClicked()
    {
        if (activeTutorial == TutorialId.Tutorial3 && activeStep == TutorialStepId.HomeTrainingButton)
            ScheduleStep(TutorialStepId.HomeTrainingGemSlot, 0.15f);
    }

    private void HandleGemPopupOpened()
    {
        if (activeTutorial == TutorialId.Tutorial3 && activeStep == TutorialStepId.HomeTrainingGemSlot)
            ShowStep(TutorialStepId.HomeTrainingFirstPopupGem);
    }

    private void HandleGemSelected()
    {
        if (activeTutorial == TutorialId.Tutorial3 && activeStep == TutorialStepId.HomeTrainingFirstPopupGem)
            ShowStep(TutorialStepId.HomeTrainingUpgradePet);
    }

    private void HandleTrainingUpgradeButtonClicked()
    {
        if (activeTutorial != TutorialId.Tutorial3 || activeStep != TutorialStepId.HomeTrainingUpgradePet)
            return;

        if (PlayerManager.Instance != null && PlayerManager.Instance.Data != null)
        {
            PlayerManager.Instance.Data.tutorial3FirstUpgradeGuaranteedUsed = true;
            PlayerManager.Instance.SaveData();
        }

        CompleteTutorial3();
    }

    private void ShowStep(TutorialStepId stepId)
    {
        CancelPendingStep();
        CancelBoardStepDisplayRoutine();
        ClearFocus();
        ClearItemHighlights();
        HidePopup();

        activeStep = stepId;

        if (activeTutorial == TutorialId.Tutorial2 && GameManager.Instance != null)
            GameManager.Instance.PauseTurnTimerForTutorial();

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
            case TutorialStepId.BattleBoardFirstGem:
                Debug.Log("[Tutorial] Step T4 -> First Gem");
                if (RefreshTutorial2SwapTargets())
                    ShowBoardTutorialStep("T4", tutorial2FirstItem, tutorial2SecondItem);
                break;
            case TutorialStepId.BattleBoardSecondGem:
                Debug.Log("[Tutorial] Step T5 -> Second Gem");
                if (tutorial2SecondItem != null)
                    ShowBoardTutorialStep("T5", tutorial2SecondItem, tutorial2FirstItem);
                break;
            case TutorialStepId.BattleGemInfoOpen:
                Debug.Log("[Tutorial] Step T6 -> Gem Info Open");
                ResolveBattleObjects();
                if (battleGemInfoPopup != null)
                    FocusAndShow(battleGemInfoPopup.OpenButton, "T6", TutorialPopupView.LayoutMode.LeftArrowRightText);
                break;
            case TutorialStepId.BattleGemInfoClose:
                Debug.Log("[Tutorial] Step T7 -> Gem Info Close");
                ResolveBattleObjects();
                if (battleGemInfoPopup != null)
                    FocusAndShow(battleGemInfoPopup.CloseButton, "T7");
                break;
            case TutorialStepId.BattleElementOpen:
                Debug.Log("[Tutorial] Step T8 -> Element Open");
                ResolveBattleObjects();
                if (battleElementPopup != null)
                    FocusAndShow(battleElementPopup.OpenButton, "T8", TutorialPopupView.LayoutMode.LeftArrowRightText);
                break;
            case TutorialStepId.BattleElementClose:
                Debug.Log("[Tutorial] Step T9 -> Element Close");
                ResolveBattleObjects();
                if (battleElementPopup != null)
                    FocusAndShow(battleElementPopup.CloseButton, "T9");
                break;
            case TutorialStepId.HomeLevelRewardContinue:
                Debug.Log("[Tutorial] Step T10 -> Level Reward Continue");
                ResolveHomeObjects();
                if (homeLevelRewardPopup != null)
                    FocusAndShow(homeLevelRewardPopup.ContinueButton, "T10");
                break;
            case TutorialStepId.HomeTrainingButton:
                Debug.Log("[Tutorial] Step T11 -> Training Button");
                ResolveHomeObjects();
                if (homeChoosePet != null)
                    FocusAndShow(homeChoosePet.TrainingButton, "T11");
                break;
            case TutorialStepId.HomeTrainingGemSlot:
                Debug.Log("[Tutorial] Step T12 -> Training Gem Slot");
                ResolveHomeObjects();
                if (homeGemUpdate != null)
                    FocusAndShow(homeGemUpdate.FirstSlotButton, "T12");
                break;
            case TutorialStepId.HomeTrainingFirstPopupGem:
                Debug.Log("[Tutorial] Step T13 -> First Popup Gem");
                ResolveHomeObjects();
                if (homeGemUpdate != null && homeGemUpdate.FirstPopupGemButton != null)
                    FocusAndShow(homeGemUpdate.FirstPopupGemButton, "T13");
                break;
            case TutorialStepId.HomeTrainingUpgradePet:
                Debug.Log("[Tutorial] Step T14 -> Upgrade Pet");
                ResolveHomeObjects();
                if (homeGemUpdate != null && PlayerManager.Instance != null && PlayerManager.Instance.Data != null && !PlayerManager.Instance.Data.tutorial3FirstUpgradeGuaranteedUsed)
                    homeGemUpdate.ForceFirstTutorialUpgradeSuccess = true;
                if (homeChoosePet != null)
                    FocusAndShow(homeChoosePet.UpgradeButton, "T14");
                break;
        }
    }

    private void FocusAndShow(Button button, string messageKey, TutorialPopupView.LayoutMode layoutMode = TutorialPopupView.LayoutMode.Above)
    {
        if (button == null)
            return;

        FocusAndShow(button.gameObject, messageKey, layoutMode);
    }

    private void FocusAndShow(Items item, string messageKey, TutorialPopupView.LayoutMode layoutMode = TutorialPopupView.LayoutMode.Above)
    {
        if (item == null)
            return;

        item.ShowTutorialHighlight();
        if (!highlightedTutorialItems.Contains(item))
            highlightedTutorialItems.Add(item);

        FocusAndShow(item.gameObject, messageKey, layoutMode);
    }

    private void ShowBoardTutorialStep(string messageKey, Items arrowItem, Items extraHighlightItem)
    {
        CancelBoardStepDisplayRoutine();
        boardStepDisplayCoroutine = StartCoroutine(CoShowBoardTutorialStep(messageKey, arrowItem, extraHighlightItem));
    }

    private IEnumerator CoShowBoardTutorialStep(string messageKey, Items arrowItem, Items extraHighlightItem)
    {
        yield return new WaitForEndOfFrame();
        yield return null;
        boardStepDisplayCoroutine = null;

        if (arrowItem != null)
            FocusAndShow(arrowItem, messageKey);

        if (extraHighlightItem != null)
        {
            extraHighlightItem.ShowTutorialHighlight();
            if (!highlightedTutorialItems.Contains(extraHighlightItem))
                highlightedTutorialItems.Add(extraHighlightItem);
        }
    }

    private void FocusAndShow(GameObject targetObject, string messageKey, TutorialPopupView.LayoutMode layoutMode = TutorialPopupView.LayoutMode.Above)
    {
        if (targetObject == null)
            return;

        TutorialFocusTarget focus = targetObject.GetComponent<TutorialFocusTarget>();
        if (focus == null)
            focus = targetObject.AddComponent<TutorialFocusTarget>();

        focus.ApplyFocus();
        focusedTargets.Add(focus);

        if (focus.RectTransform != null && TutorialPopupManager.Instance != null)
            TutorialPopupManager.Instance.ShowStep(messageKey, focus.RectTransform, SkipActiveTutorial, layoutMode);
    }

    private void SkipActiveTutorial()
    {
        if (activeTutorial == TutorialId.Tutorial1)
        {
            tutorial1Skipped = true;
            CompleteTutorial1();
            return;
        }
        if (activeTutorial == TutorialId.Tutorial2)
        {
            tutorial2Skipped = true;
            CompleteTutorial2();
            return;
        }
        if (activeTutorial == TutorialId.Tutorial3)
        {
            tutorial3Skipped = true;
            CompleteTutorial3();
        }
    }

    private void CompleteTutorial1()
    {
        tutorial1Completed = true;
        ResetActiveTutorialState();
        SaveToPlayerData();
    }

    private void CompleteTutorial2()
    {
        tutorial2Completed = true;
        waitingForTutorial2PlayerTurn = false;
        if (GameManager.Instance != null)
            GameManager.Instance.ResumeTurnTimerAfterTutorial();
        ResetActiveTutorialState();
        SaveToPlayerData();
    }

    private void CompleteTutorial3()
    {
        tutorial3Completed = true;
        ResetActiveTutorialState();
        SaveToPlayerData();
    }

    private void ResetActiveTutorialState()
    {
        activeTutorial = TutorialId.None;
        activeStep = TutorialStepId.None;
        CancelPendingStep();
        CancelBoardStepDisplayRoutine();
        ClearFocus();
        ClearItemHighlights();
        HidePopup();
    }

    private void HidePopup()
    {
        if (TutorialPopupManager.Instance != null)
            TutorialPopupManager.Instance.Hide();
    }

    private void ClearFocus()
    {
        if (focusedTargets.Count == 0)
            return;

        for (int i = 0; i < focusedTargets.Count; i++)
        {
            if (focusedTargets[i] != null)
                focusedTargets[i].ClearFocus();
        }

        focusedTargets.Clear();
    }

    private void ClearItemHighlights()
    {
        if (highlightedTutorialItems.Count == 0)
            return;

        for (int i = 0; i < highlightedTutorialItems.Count; i++)
        {
            if (highlightedTutorialItems[i] != null)
                highlightedTutorialItems[i].HideTutorialHighlight();
        }

        highlightedTutorialItems.Clear();
    }

    private void SyncFromPlayerData()
    {
        if (PlayerManager.Instance == null || PlayerManager.Instance.Data == null)
            return;

        tutorial1Triggered = PlayerManager.Instance.Data.tutorial1Triggered;
        tutorial1Completed = PlayerManager.Instance.Data.tutorial1Completed;
        tutorial1Skipped = PlayerManager.Instance.Data.tutorial1Skipped;
        tutorial2Triggered = PlayerManager.Instance.Data.tutorial2Triggered;
        tutorial2Completed = PlayerManager.Instance.Data.tutorial2Completed;
        tutorial2Skipped = PlayerManager.Instance.Data.tutorial2Skipped;
        tutorial3Triggered = PlayerManager.Instance.Data.tutorial3Triggered;
        tutorial3Completed = PlayerManager.Instance.Data.tutorial3Completed;
        tutorial3Skipped = PlayerManager.Instance.Data.tutorial3Skipped;
    }

    private void SaveToPlayerData()
    {
        if (PlayerManager.Instance == null || PlayerManager.Instance.Data == null)
            return;

        PlayerManager.Instance.Data.tutorial1Triggered = tutorial1Triggered;
        PlayerManager.Instance.Data.tutorial1Completed = tutorial1Completed;
        PlayerManager.Instance.Data.tutorial1Skipped = tutorial1Skipped;
        PlayerManager.Instance.Data.tutorial2Triggered = tutorial2Triggered;
        PlayerManager.Instance.Data.tutorial2Completed = tutorial2Completed;
        PlayerManager.Instance.Data.tutorial2Skipped = tutorial2Skipped;
        PlayerManager.Instance.Data.tutorial3Triggered = tutorial3Triggered;
        PlayerManager.Instance.Data.tutorial3Completed = tutorial3Completed;
        PlayerManager.Instance.Data.tutorial3Skipped = tutorial3Skipped;
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

    private void CancelBoardStepDisplayRoutine()
    {
        if (boardStepDisplayCoroutine == null)
            return;

        StopCoroutine(boardStepDisplayCoroutine);
        boardStepDisplayCoroutine = null;
    }

    private void CancelBattleStartRoutine()
    {
        if (pendingBattleStartCoroutine == null)
            return;

        StopCoroutine(pendingBattleStartCoroutine);
        pendingBattleStartCoroutine = null;
    }

    private void CancelHomeTutorial3Routine()
    {
        if (pendingHomeTutorial3Coroutine == null)
            return;

        StopCoroutine(pendingHomeTutorial3Coroutine);
        pendingHomeTutorial3Coroutine = null;
    }

    private float GetHomePopupDelay()
    {
        MapPopupUI mapPopup = FindObjectOfType<MapPopupUI>(true);
        if (mapPopup == null)
            return 0.3f;

        return mapPopup.PopupSlideDuration + 0.05f;
    }
}
