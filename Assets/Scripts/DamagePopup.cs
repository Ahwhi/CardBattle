using DG.Tweening;
using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI label;

    private CanvasGroup _canvasGroup;

    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (label == null)
            label = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void Setup(int amount, bool isHeal)
    {
        string text = amount.ToString();
        Color color = isHeal ? new Color(0.25f, 0.95f, 0.25f) : Color.red;
        PlayPopup(text, color);
    }

    public void Setup(string text, Color color)
    {
        PlayPopup(text, color);
    }

    private void PlayPopup(string text, Color color)
    {
        if (label == null)
        {
            Debug.LogWarning("[DamagePopup] label is null. Assign TextMeshProUGUI in the prefab.");
            Destroy(gameObject, 1f);
            return;
        }

        label.text = text;
        label.color = color;

        _canvasGroup.alpha = 1f;
        transform.localScale = Vector3.zero;

        Vector3 originLocal = transform.localPosition;

        DOTween.Sequence()
            // 1. 크게 팡 등장
            .Append(transform.DOScale(2.0f, 0.13f).SetEase(Ease.OutBack))
            // 2. 살짝 올라가며 적정 크기로 정착
            .Append(transform.DOScale(1.2f, 0.15f).SetEase(Ease.InOutQuad))
            .Join(transform.DOLocalMoveY(originLocal.y + 55f, 0.15f).SetEase(Ease.OutQuad))
            // 3. 홀드 잠깐
            .AppendInterval(0.12f)
            // 4. 더 올라가며 줄어들고 페이드아웃
            .Append(transform.DOLocalMoveY(originLocal.y + 140f, 0.45f).SetEase(Ease.InQuad))
            .Join(transform.DOScale(0.3f, 0.45f).SetEase(Ease.InQuad))
            .Join(_canvasGroup.DOFade(0f, 0.4f).SetEase(Ease.InQuad))
            .OnComplete(() => Destroy(gameObject));
    }
}
