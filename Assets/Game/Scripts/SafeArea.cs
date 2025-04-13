using UnityEngine;

namespace Game.Scripts
{
    [ExecuteInEditMode]
    public class SafeArea : MonoBehaviour
    {
        private RectTransform _panel;
        private Rect _lastSafeArea;

        [SerializeField] private bool _x = true;
        [SerializeField] private bool _y = true;
        [SerializeField] private bool _left;
        [SerializeField] private bool _right;
        [SerializeField] private bool _bottom;
        [SerializeField] private bool _top;

        private void Awake()
        {
            _panel = GetComponent<RectTransform>();

            if (_panel == null)
            {
                Debug.LogError("Cannot apply safe area - no RectTransform found on " + name);
            }
        }

        private void Update()
        {
            var safeArea = GetSafeArea();

#if !UNITY_EDITOR
			if (safeArea != _lastSafeArea)
#endif
            ApplySafeArea(safeArea);
        }

        private Rect GetSafeArea()
        {
            return
#if UNITY_EDITOR
                Screen.safeArea;
#else
			Screen.safeArea;
#endif
        }

        private void ApplySafeArea(Rect rect)
        {
            var anchorMinBase = Vector2.zero;
            var anchorMaxBase = Vector2.one;

            if (!_x)
            {
                rect.x = 0;
                rect.width = Screen.width;
            }

            if (!_y)
            {
                rect.y = 0;
                rect.height = Screen.height;
            }

            var anchorMin = rect.position;
            var anchorMax = rect.position + rect.size;

            anchorMin.x = _left ? anchorMin.x / Screen.width : anchorMinBase.x;
            anchorMax.x = _right ? anchorMax.x / Screen.width : anchorMaxBase.x;

            anchorMin.y = _bottom ? anchorMin.y / Screen.height : anchorMinBase.y;
            anchorMax.y = _top ? anchorMax.y / Screen.height : anchorMaxBase.y;

            _panel.anchorMin = anchorMin;
            _panel.anchorMax = anchorMax;
        }
    }
}