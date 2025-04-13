using CustomAssets.ProceduralUIImage.Scripts.Modifiers;
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
public class WindowRadiusController : MonoBehaviour
{
    [Header("Settings")]
    public float fullscreenRadius = 0f;
    public float normalRadius = 12f;
    public float animationDuration = 0.3f;

    private RectTransform rectTransform;
    private OnlyOneEdgeModifier radiusModifier;
    private Canvas rootCanvas;
    private float? originalRadius;
    private bool isAnimating;
    private Vector3 lastScale;
    private Vector2 lastAnchoredPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        radiusModifier = GetComponentInChildren<OnlyOneEdgeModifier>(true);
        rootCanvas = GetComponentInParent<Canvas>();
        if (radiusModifier != null) originalRadius = radiusModifier.Radius;
        
        lastScale = transform.localScale;
        lastAnchoredPosition = rectTransform.anchoredPosition;
    }

    private void OnEnable()
    {
        UpdateRadius(true);
        // Начинаем следить за изменениями
        DG.Tweening.DOVirtual.DelayedCall(0.1f, () => UpdateRadius(), true);
    }

    private void Update()
    {
        // Проверяем изменения масштаба и позиции
        if (transform.localScale != lastScale || 
            rectTransform.anchoredPosition != lastAnchoredPosition)
        {
            lastScale = transform.localScale;
            lastAnchoredPosition = rectTransform.anchoredPosition;
            UpdateRadius();
        }
    }

    private void UpdateRadius(bool immediate = false)
    {
        if (radiusModifier == null || rootCanvas == null) return;

        bool shouldBeFullscreen = CheckFullscreenStatus();
        float targetRadius = shouldBeFullscreen ? fullscreenRadius : normalRadius;

        if (immediate)
        {
            radiusModifier.Radius = targetRadius;
            return;
        }

        if (!isAnimating && Mathf.Abs(radiusModifier.Radius - targetRadius) > 0.1f)
        {
            isAnimating = true;
            DOTween.To(
                () => radiusModifier.Radius,
                x => radiusModifier.Radius = x,
                targetRadius,
                animationDuration
            ).SetEase(Ease.OutCubic)
            .OnComplete(() => isAnimating = false);
        }
    }

    private bool CheckFullscreenStatus()
    {
        // 1. Проверяем масштаб
        bool isNormalScale = transform.localScale.x >= 0.99f && transform.localScale.x <= 1.01f;
        
        // 2. Проверяем позицию (должна быть центрирована)
        bool isCentered = 
            Mathf.Abs(rectTransform.anchoredPosition.x) < 2f && 
            Mathf.Abs(rectTransform.anchoredPosition.y) < 2f;
        
        // 3. Проверяем, есть ли другие активные окна поверх этого
        bool hasWindowsAbove = false;
        var windows = rootCanvas.GetComponentsInChildren<WindowRadiusController>(true);
        
        foreach (var window in windows)
        {
            if (window != this && window.gameObject.activeSelf && 
                window.transform.GetSiblingIndex() > transform.GetSiblingIndex())
            {
                hasWindowsAbove = true;
                break;
            }
        }

        // Полноэкранный режим только если:
        // - масштаб 1:1
        // - позиция центрирована
        // - нет окон поверх этого
        return isNormalScale && isCentered && !hasWindowsAbove;
    }

    private void OnDisable()
    {
        if (radiusModifier != null && originalRadius.HasValue)
        {
            radiusModifier.Radius = originalRadius.Value;
        }
    }
}