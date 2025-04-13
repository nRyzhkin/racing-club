using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class UIWindow : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Window Settings")]
    public bool isFullscreen = true;
    public float animationDuration = 0.35f;
    [Range(0.05f, 0.3f)] public float swipeCloseThreshold = 0.15f;
    public float windowScaleFactor = 0.96f;
    public float windowTopOffset = 36f;

    [Header("References")]
    public RectTransform dragHandle;
    public Image background;

    public CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector2 closedPosition;
    private bool isAnimating = false;
    private bool isDragging = false;
    private Vector3 originalScale = Vector3.one;
    private Vector2 originalSizeDelta;
    private Vector2 originalAnchoredPosition;
    private float dragOffsetY;
    private static int activeWindowCount = 0;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        originalScale = transform.localScale;
        originalSizeDelta = rectTransform.sizeDelta;
        originalAnchoredPosition = rectTransform.anchoredPosition;
        
        if (isFullscreen)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
        }
        
        closedPosition = new Vector2(0, -GetWindowHeight());
    }

    private float GetWindowHeight()
    {
        return rectTransform.rect.height * rectTransform.lossyScale.y;
    }

    private void OnEnable()
    {
        transform.SetAsLastSibling();
        activeWindowCount++;
        
        if (isFullscreen)
        {
            UpdateWindowStack();
        }
        
        StartOpenAnimation();
    }

    private void OnDisable()
    {
        activeWindowCount--;
        
        if (isFullscreen)
        {
            // Всегда обновляем стек при любом способе закрытия
            UpdateWindowStack();
        }
        
        ResetWindowState();
    }

    private void UpdateWindowStack()
    {
        var windows = transform.parent.GetComponentsInChildren<UIWindow>(true);
        UIWindow topWindow = null;
        UIWindow newTopWindow = null;
        int activeCount = 0;

        // Находим текущее верхнее окно и считаем активные окна
        foreach (var window in windows)
        {
            if (window.isFullscreen && window.gameObject.activeSelf)
            {
                activeCount++;
                topWindow = window;
            }
        }

        // Находим новое верхнее окно (если текущее закрывается)
        foreach (var window in windows)
        {
            if (window.isFullscreen && window.gameObject.activeSelf && window != topWindow)
            {
                if (newTopWindow == null || window.transform.GetSiblingIndex() > newTopWindow.transform.GetSiblingIndex())
                {
                    newTopWindow = window;
                }
            }
        }

        // Применяем правильные трансформации ко всем окнам
        foreach (var window in windows)
        {
            if (!window.isFullscreen) continue;

            if (window.gameObject.activeSelf)
            {
                bool isTopMost = window == topWindow;
                bool hasWindowsAbove = activeCount > 1;

                if (isTopMost)
                {
                    // Верхнее окно
                    if (hasWindowsAbove)
                    {
                        // С отступом сверху
                        window.rectTransform.DOSizeDelta(new Vector2(0, -windowTopOffset), animationDuration);
                        window.rectTransform.DOAnchorPos(new Vector2(0, -windowTopOffset/2), animationDuration);
                        window.transform.DOScale(originalScale, animationDuration);
                    }
                    else
                    {
                        // Полноэкранное
                        window.rectTransform.DOSizeDelta(Vector2.zero, animationDuration);
                        window.rectTransform.DOAnchorPos(Vector2.zero, animationDuration);
                        window.transform.DOScale(originalScale, animationDuration);
                    }
                }
                else
                {
                    // Не верхнее окно
                    if (hasWindowsAbove)
                    {
                        // Уменьшенное
                        window.transform.DOScale(originalScale * windowScaleFactor, animationDuration);
                        window.rectTransform.DOSizeDelta(Vector2.zero, animationDuration);
                        window.rectTransform.DOAnchorPos(Vector2.zero, animationDuration);
                    }
                    else
                    {
                        // Становится верхним - полноэкранное
                        window.transform.DOScale(originalScale, animationDuration);
                        window.rectTransform.DOSizeDelta(Vector2.zero, animationDuration);
                        window.rectTransform.DOAnchorPos(Vector2.zero, animationDuration);
                    }
                }
            }
        }
    }

    private void ResetWindowState()
    {
        rectTransform.sizeDelta = originalSizeDelta;
        rectTransform.anchoredPosition = originalAnchoredPosition;
        transform.localScale = originalScale;
        canvasGroup.alpha = 1;
        isAnimating = false;
        isDragging = false;
    }

    private void StartOpenAnimation()
    {
        if (isAnimating) return;
        isAnimating = true;
        
        rectTransform.anchoredPosition = closedPosition;
        canvasGroup.alpha = 0;
        
        DOTween.Sequence()
            .Append(rectTransform.DOAnchorPosY(activeWindowCount > 1 ? -windowTopOffset/2 : 0, animationDuration))
            .Join(canvasGroup.DOFade(1, animationDuration * 0.8f))
            .OnComplete(() => isAnimating = false);

        if (background != null)
        {
            background.color = new Color(0, 0, 0, 0);
            background.DOFade(0.7f, animationDuration);
        }
    }

    public void CloseWindow()
    {
        if (isAnimating || !gameObject.activeSelf) return;
        isAnimating = true;
        
        // Синхронная анимация закрытия
        var sequence = DOTween.Sequence();
        sequence.Join(rectTransform.DOAnchorPos(closedPosition, animationDuration).SetEase(Ease.InCubic));
        sequence.Join(canvasGroup.DOFade(0, animationDuration * 0.7f));
        
        sequence.OnComplete(() => {
            isAnimating = false;
            gameObject.SetActive(false);
            
            if (background != null)
            {
                background.DOFade(0, 0.1f);
            }
        });
    }

    #region Input Handlers
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isFullscreen || isAnimating || activeWindowCount <= 1) return;
        
        if (dragHandle != null && !RectTransformUtility.RectangleContainsScreenPoint(
            dragHandle, eventData.position, eventData.pressEventCamera))
        {
            return;
        }
        
        isDragging = true;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, 
            eventData.position, 
            eventData.pressEventCamera, 
            out Vector2 localPoint);
        
        dragOffsetY = rectTransform.anchoredPosition.y - localPoint.y;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || !isFullscreen || isAnimating) return;
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, 
            eventData.position, 
            eventData.pressEventCamera, 
            out Vector2 localPoint);
        
        float newY = localPoint.y + dragOffsetY;
        newY = Mathf.Min(newY, activeWindowCount > 1 ? -windowTopOffset/2 : 0);
        rectTransform.anchoredPosition = new Vector2(0, newY);
        
        // Синхронное масштабирование других окон
        UpdateWindowScalesDuringDrag(newY);
    }

    private void UpdateWindowScalesDuringDrag(float currentY)
    {
        var windows = transform.parent.GetComponentsInChildren<UIWindow>(true);
        UIWindow previousWindow = null;
        
        // Находим предыдущее окно
        foreach (var window in windows)
        {
            if (window != this && window.isFullscreen && window.gameObject.activeSelf)
            {
                if (previousWindow == null || window.transform.GetSiblingIndex() > previousWindow.transform.GetSiblingIndex())
                {
                    previousWindow = window;
                }
            }
        }

        if (previousWindow != null)
        {
            float progress = Mathf.InverseLerp(-windowTopOffset/2, closedPosition.y, currentY);
            float scale = Mathf.Lerp(windowScaleFactor, 1f, progress);
            previousWindow.transform.localScale = originalScale * scale;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging || !isFullscreen || isAnimating) return;
        
        isDragging = false;
        
        float threshold = Screen.height * swipeCloseThreshold;
        float currentOffset = -rectTransform.anchoredPosition.y;
        
        if (currentOffset > threshold)
        {
            CloseWindow();
        }
        else
        {
            rectTransform.DOAnchorPos(new Vector2(0, -windowTopOffset/2), animationDuration / 2);
            UpdateWindowStack();
        }
    }
    #endregion
}