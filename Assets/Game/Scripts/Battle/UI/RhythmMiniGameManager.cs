using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class RhythmMiniGameManager : MonoBehaviour
{
    private enum RhythmDirection
    {
        Up = 0,
        Down = 1,
        Left = 2,
        Right = 3
    }

    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Arrow Sequence")]
    [SerializeField] private Transform arrowItemContainer;
    [SerializeField] private RhythmArrowItemUI arrowItemPrefab;
    [SerializeField] private Sprite[] directionSprites = new Sprite[4];
    [SerializeField] private Color pendingColor = Color.white;
    [SerializeField] private Color successColor = Color.green;

    [Header("Timing Bar")]
    [SerializeField] private RectTransform timingBar;
    [SerializeField] private RectTransform marker;
    [SerializeField] private RectTransform perfectZone;

    [Header("Result")]
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private string perfectLabel = "Perfect";
    [SerializeField] private string greatLabel = "Great";
    [SerializeField] private string missLabel = "Miss";
    [SerializeField] private float resultDisplayDelay = 0.35f;

    private readonly List<RhythmDirection> sequence = new List<RhythmDirection>();
    private readonly List<RhythmArrowItemUI> spawnedItems = new List<RhythmArrowItemUI>();
    private UniTaskCompletionSource<AuditionMiniGameResult> completionSource;
    private CancellationTokenRegistration cancellationRegistration;
    private CancellationToken activeCancellationToken;
    private SkillData activeSkill;
    private float timer;
    private int currentInputIndex;
    private bool sequenceCompleted;
    private bool roundActive;

    private void Awake()
    {
        SetVisible(false);
    }

    private void OnDestroy()
    {
        cancellationRegistration.Dispose();
        ClearSpawnedItems();
    }

    private void Update()
    {
        if (!roundActive || activeSkill == null)
            return;

        timer += Time.deltaTime;
        float normalizedTime = GetNormalizedTime(activeSkill);
        UpdateMarker(normalizedTime);

        if (timer >= Mathf.Max(0.1f, activeSkill.auditionRoundDuration))
        {
            CompleteRound(BuildResult(activeSkill, AuditionResultType.Miss));
            return;
        }

        HandleArrowInput();
        HandleSpaceInput(normalizedTime);
    }

    public UniTask<AuditionMiniGameResult> PlayAsync(SkillData skill, CancellationToken cancellationToken)
    {
        if (skill == null)
            return UniTask.FromResult(AuditionMiniGameResult.DefaultMiss);

        CancelCurrentRound();

        activeSkill = skill;
        activeCancellationToken = cancellationToken;
        completionSource = new UniTaskCompletionSource<AuditionMiniGameResult>();
        cancellationRegistration = cancellationToken.Register(CancelCurrentRound);

        StartRound(skill);
        return completionSource.Task;
    }

    private void StartRound(SkillData skill)
    {
        timer = 0f;
        currentInputIndex = 0;
        sequenceCompleted = false;
        roundActive = true;

        GenerateSequence(skill);
        RebuildArrowItems();
        UpdateSequenceVisual();
        UpdateMarker(0f);
        UpdatePerfectZone(skill);
        SetResultText(string.Empty);
        SetVisible(true);
    }

    private void GenerateSequence(SkillData skill)
    {
        sequence.Clear();
        int minLength = Mathf.Max(1, skill.auditionSequenceLengthMin);
        int maxLength = Mathf.Max(minLength, skill.auditionSequenceLengthMax);
        int count = UnityEngine.Random.Range(minLength, maxLength + 1);

        for (int i = 0; i < count; i++)
            sequence.Add((RhythmDirection)UnityEngine.Random.Range(0, 4));
    }

    private void RebuildArrowItems()
    {
        ClearSpawnedItems();

        if (arrowItemContainer == null || arrowItemPrefab == null)
            return;

        for (int i = 0; i < sequence.Count; i++)
        {
            RhythmArrowItemUI item = Instantiate(arrowItemPrefab, arrowItemContainer);
            item.gameObject.SetActive(true);
            spawnedItems.Add(item);
        }
    }

    private void ClearSpawnedItems()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            RhythmArrowItemUI item = spawnedItems[i];
            if (item != null)
                Destroy(item.gameObject);
        }

        spawnedItems.Clear();
    }

    private void HandleArrowInput()
    {
        if (sequenceCompleted)
            return;

        if (!TryGetPressedDirection(out RhythmDirection pressedDirection))
            return;

        RhythmDirection expectedDirection = sequence[currentInputIndex];
        if (pressedDirection == expectedDirection)
        {
            currentInputIndex++;
            sequenceCompleted = currentInputIndex >= sequence.Count;
            UpdateSequenceVisual();
            return;
        }

        currentInputIndex = 0;
        sequenceCompleted = false;
        UpdateSequenceVisual();
    }

    private void HandleSpaceInput(float normalizedTime)
    {
        if (!Input.GetKeyDown(KeyCode.Space))
            return;

        if (!sequenceCompleted)
        {
            CompleteRound(BuildResult(activeSkill, AuditionResultType.Miss));
            return;
        }

        bool isPerfect = normalizedTime >= activeSkill.auditionPerfectZoneStart && normalizedTime <= activeSkill.auditionPerfectZoneEnd;
        CompleteRound(BuildResult(activeSkill, isPerfect ? AuditionResultType.Perfect : AuditionResultType.Great));
    }

    private void CompleteRound(AuditionMiniGameResult result)
    {
        if (!roundActive)
            return;

        CompleteRoundAsync(result).Forget();
    }

    private async UniTaskVoid CompleteRoundAsync(AuditionMiniGameResult result)
    {
        roundActive = false;
        SetResultText(GetResultLabel(result.ResultType));

        if (resultDisplayDelay > 0f)
            await UniTask.Delay(TimeSpan.FromSeconds(resultDisplayDelay), cancellationToken: activeCancellationToken);

        Finish(result);
    }

    private void Finish(AuditionMiniGameResult result)
    {
        cancellationRegistration.Dispose();
        SetVisible(false);
        ClearSpawnedItems();
        activeSkill = null;
        timer = 0f;
        currentInputIndex = 0;
        sequenceCompleted = false;
        roundActive = false;

        var source = completionSource;
        completionSource = null;
        source?.TrySetResult(result);
    }

    private void CancelCurrentRound()
    {
        if (completionSource == null)
            return;

        Finish(BuildResult(activeSkill, AuditionResultType.Miss));
    }

    private AuditionMiniGameResult BuildResult(SkillData skill, AuditionResultType resultType)
    {
        float multiplier = 1f;
        switch (resultType)
        {
            case AuditionResultType.Perfect:
                multiplier = skill != null ? skill.auditionPerfectMultiplier : 3f;
                break;
            case AuditionResultType.Great:
                multiplier = skill != null ? skill.auditionGreatMultiplier : 2f;
                break;
            case AuditionResultType.Miss:
            default:
                multiplier = skill != null ? skill.auditionMissMultiplier : 1f;
                break;
        }

        return new AuditionMiniGameResult(resultType, multiplier, currentInputIndex, sequence.Count);
    }

    private float GetNormalizedTime(SkillData skill)
    {
        float duration = skill != null ? Mathf.Max(0.1f, skill.auditionRoundDuration) : 5f;
        return Mathf.Clamp01(timer / duration);
    }

    private void UpdateMarker(float normalizedTime)
    {
        if (marker == null || timingBar == null)
            return;

        float width = timingBar.rect.width;
        Vector2 anchoredPosition = marker.anchoredPosition;
        anchoredPosition.x = normalizedTime * width;
        marker.anchoredPosition = anchoredPosition;
    }

    private void UpdatePerfectZone(SkillData skill)
    {
        if (perfectZone == null || timingBar == null || skill == null)
            return;

        float width = timingBar.rect.width;
        float start = Mathf.Clamp01(skill.auditionPerfectZoneStart);
        float end = Mathf.Clamp(skill.auditionPerfectZoneEnd, start, 1f);
        float zoneWidth = Mathf.Max(0f, (end - start) * width);

        perfectZone.anchorMin = new Vector2(0f, perfectZone.anchorMin.y);
        perfectZone.anchorMax = new Vector2(0f, perfectZone.anchorMax.y);
        perfectZone.pivot = new Vector2(0f, perfectZone.pivot.y);
        perfectZone.sizeDelta = new Vector2(zoneWidth, perfectZone.sizeDelta.y);
        perfectZone.anchoredPosition = new Vector2(start * width, perfectZone.anchoredPosition.y);
    }

    private void UpdateSequenceVisual()
    {
        if (spawnedItems.Count == 0)
            return;

        for (int i = 0; i < spawnedItems.Count; i++)
        {
            RhythmArrowItemUI item = spawnedItems[i];
            if (item == null)
                continue;

            int spriteIndex = (int)sequence[i];
            Sprite sprite = directionSprites != null && spriteIndex >= 0 && spriteIndex < directionSprites.Length
                ? directionSprites[spriteIndex]
                : null;
            Color color = i < currentInputIndex ? successColor : pendingColor;
            item.Setup(sprite, color);
        }
    }

    private static bool TryGetPressedDirection(out RhythmDirection direction)
    {
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            direction = RhythmDirection.Up;
            return true;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            direction = RhythmDirection.Down;
            return true;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            direction = RhythmDirection.Left;
            return true;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            direction = RhythmDirection.Right;
            return true;
        }

        direction = default;
        return false;
    }

    private void SetVisible(bool visible)
    {
        if (root != null)
            root.SetActive(visible);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = visible;
            canvasGroup.interactable = visible;
        }

        if (!visible)
            SetResultText(string.Empty);
    }

    private void SetResultText(string value)
    {
        if (resultText != null)
            resultText.text = value ?? string.Empty;
    }

    private string GetResultLabel(AuditionResultType resultType)
    {
        switch (resultType)
        {
            case AuditionResultType.Perfect:
                return perfectLabel;
            case AuditionResultType.Great:
                return greatLabel;
            case AuditionResultType.Miss:
            default:
                return missLabel;
        }
    }
}
