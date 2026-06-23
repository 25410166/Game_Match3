using DG.Tweening;
using UnityEngine;

public class FloatingAnimation : MonoBehaviour
{
    [Header("Floating Range (Local Y)")]
    [SerializeField] private float yDown = -0.15f;
    [SerializeField] private float yUp = 0.15f;

    [Header("Speed")]
    [SerializeField] private float speed = 0.5f;

    private Tween floatTween;
    private Vector3 startLocalPos;

    private void OnEnable()
    {
        startLocalPos = transform.localPosition;
        PlayFloating();
    }

    private void OnDisable()
    {
        if (floatTween != null && floatTween.IsActive())
            floatTween.Kill();
    }

    private void PlayFloating()
    {
        if (speed <= 0f) speed = 0.1f;

        float fromY = startLocalPos.y + yDown;
        float toY = startLocalPos.y + yUp;
        float distance = Mathf.Abs(toY - fromY);
        float duration = distance / speed;
        if (duration <= 0f) duration = 0.1f;

        transform.localPosition = new Vector3(startLocalPos.x, fromY, startLocalPos.z);

        floatTween = transform
            .DOLocalMoveY(toY, duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }
}
