using UnityEngine;

public class TutorialPopupManager : MonoBehaviour
{
    public static TutorialPopupManager Instance { get; private set; }

    [SerializeField] private TutorialPopupView currentView;

    public TutorialPopupView CurrentView => currentView;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        ResolveView();
    }

    private void OnEnable()
    {
        ResolveView();
    }

    public void RegisterView(TutorialPopupView view)
    {
        if (view == null)
            return;

        currentView = view;
    }

    public void ShowStep(string message, RectTransform target, System.Action onSkip, TutorialPopupView.LayoutMode layoutMode = TutorialPopupView.LayoutMode.Above)
    {
        ResolveView();
        if (currentView == null)
        {
            Debug.LogWarning("[TutorialPopupManager] No TutorialPopupView found in active scene.");
            return;
        }

        currentView.Show(message, target, onSkip, layoutMode);
    }

    public void Hide()
    {
        ResolveView();
        if (currentView == null)
            return;

        currentView.Hide();
    }

    private void ResolveView()
    {
        if (currentView != null)
            return;

        currentView = GetComponentInChildren<TutorialPopupView>(true);
        if (currentView == null)
            currentView = FindObjectOfType<TutorialPopupView>(true);
    }
}
