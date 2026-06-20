using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace HornDancheong.Seongwoo.UI
{
    /// <summary>
    /// 클릭 시 RectTransform이 지정한 크기 및 피벗으로 Easing 애니메이션을 거쳐 확장되는 버튼 컴포넌트입니다.
    /// 확장된 상태에서 버튼 외부의 영역을 클릭하면 원래 크기와 피벗으로 복원됩니다.
    /// Stretch 앵커 및 LayoutGroup 환경도 완벽히 지원하며 상세 디버그 로그 기능을 제공합니다.
    /// </summary>
    public class EasingExpandableButton : ButtonBase, IPointerClickHandler
    {
        public enum EasingType
        {
            Linear,
            EaseInQuad,
            EaseOutQuad,
            EaseInOutQuad,
            EaseInCubic,
            EaseOutCubic,
            EaseInOutCubic,
            EaseOutBack,
            EaseOutBounce,
            CustomCurve
        }

        [Header("Target RectTransform")]
        [Tooltip("크기가 변경될 대상 RectTransform입니다. 비워둘 경우 이 버튼의 RectTransform이 대상이 됩니다.")]
        [SerializeField] private RectTransform targetRectTransform;

        [Header("Width Settings")]
        [SerializeField] private bool expandWidth = true;
        [SerializeField] private float targetWidth = 300f;

        [Header("Height Settings")]
        [SerializeField] private bool expandHeight = true;
        [SerializeField] private float targetHeight = 100f;

        [Header("Pivot Settings")]
        [SerializeField] private bool changePivot = false;
        [SerializeField] private Vector2 targetPivot = new Vector2(0.5f, 0.5f);

        [Header("Easing Settings")]
        [SerializeField] private EasingType easingType = EasingType.EaseOutQuad;
        [SerializeField] private AnimationCurve customCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private float duration = 0.3f;

        [Header("Events")]
        public UnityEngine.Events.UnityEvent onExpandStart;
        public UnityEngine.Events.UnityEvent onExpandComplete;
        public UnityEngine.Events.UnityEvent onCollapseStart;
        public UnityEngine.Events.UnityEvent onCollapseComplete;

        [Header("Debug Settings")]
        [SerializeField] private bool showDebugLogs = true;

        private static EasingExpandableButton currentlyExpandedButton;

        private float originalWidth;
        private float originalHeight;
        private Vector2 originalPivot;
        private float originalPrefWidth = -1f;
        private float originalPrefHeight = -1f;

        private bool isExpanded = false;
        private bool isInitialized = false;
        private int expandFrameCount = -1;
        private LayoutElement targetLayoutElement;
        private Coroutine activeTransition;

        private void Log(string message)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[EasingExpandableButton] {message}", this);
            }
        }

        protected override void Awake()
        {
            Log("Awake() called.");
            // base.Awake()를 호출하지 않음으로써 ButtonBase의 onClick.AddListener 바인딩을 우회합니다.
            // 대신 IPointerClickHandler 인터페이스를 사용하여 클릭 이벤트를 직접 가로챕니다.
            // 이렇게 하면 다른 매니저 클래스에서 버튼의 onClick.RemoveAllListeners()를 호출해도 정상 작동합니다.
            Initialize();
        }

        private void Start()
        {
            Log("Start() called.");
            Initialize();
        }

        private void OnDisable()
        {
            // 이 버튼이 파괴되거나 꺼질 때 현재 등록된 전역 참조를 해제하여 메모리 누수를 방지합니다.
            if (currentlyExpandedButton == this)
            {
                currentlyExpandedButton = null;
                Log("OnDisable() - Cleared 전역 현재 확장 버튼 참조.");
            }
        }

        /// <summary>
        /// 초기 픽셀 크기 및 피벗, 레이아웃 설정을 안전하게 캐싱합니다.
        /// </summary>
        private void Initialize()
        {
            if (isInitialized) return;

            if (targetRectTransform == null)
            {
                targetRectTransform = GetComponent<RectTransform>();
                Log($"targetRectTransform was null, assigned self: {targetRectTransform.name}");
            }

            if (targetRectTransform != null)
            {
                originalWidth = targetRectTransform.rect.width;
                originalHeight = targetRectTransform.rect.height;
                originalPivot = targetRectTransform.pivot;

                Log($"Initialize() - originalWidth: {originalWidth}, originalHeight: {originalHeight}, originalPivot: {originalPivot}");

                // 부모에 LayoutGroup이 있는 경우 레이아웃 컨트롤을 위해 LayoutElement 확인 및 동적 추가
                targetLayoutElement = targetRectTransform.GetComponent<LayoutElement>();
                if (targetLayoutElement == null && targetRectTransform.parent != null)
                {
                    var parentLayout = targetRectTransform.parent.GetComponent<LayoutGroup>();
                    if (parentLayout != null)
                    {
                        targetLayoutElement = targetRectTransform.gameObject.AddComponent<LayoutElement>();
                        Log($"LayoutGroup detected on parent. Dynamically added LayoutElement to {targetRectTransform.name}.");
                    }
                }

                if (targetLayoutElement != null)
                {
                    originalPrefWidth = targetLayoutElement.preferredWidth;
                    originalPrefHeight = targetLayoutElement.preferredHeight;
                    Log($"LayoutElement detected. originalPrefWidth: {originalPrefWidth}, originalPrefHeight: {originalPrefHeight}");
                }

                // 크기가 아직 계산되지 않은 경우(0 이하인 경우) 초기화 완료 처리를 보류하여 다음 기회에 재시도
                if (originalWidth > 0f && originalHeight > 0f)
                {
                    isInitialized = true;
                    Log("Initialization completed successfully.");
                }
                else
                {
                    Log("Initialization deferred: dimensions are 0 or less (waiting for layout pass).");
                }
            }
            else
            {
                Debug.LogError($"[{nameof(EasingExpandableButton)}] RectTransform을 찾을 수 없습니다.");
            }
        }

        protected override void Function()
        {
            // 사용하지 않음 (IPointerClickHandler를 통해 OnPointerClick로 처리)
        }

        /// <summary>
        /// 마우스 클릭/터치 입력이 이 오브젝트에서 감지되었을 때 호출됩니다.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            Log($"OnPointerClick() [Pointer Click Event] called. isExpanded: {isExpanded}");
            if (!isExpanded)
            {
                Expand();
            }
        }

        private void Update()
        {
            if (isExpanded)
            {
                bool clickDetected = false;
                Vector2 pointerPosition = Vector2.zero;

                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    clickDetected = true;
                    pointerPosition = Mouse.current.position.ReadValue();
                }
                else if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0 && Touchscreen.current.touches[0].press.wasPressedThisFrame)
                {
                    clickDetected = true;
                    pointerPosition = Touchscreen.current.touches[0].position.ReadValue();
                }

                if (clickDetected)
                {
                    Log($"Input click detected while expanded. Frame: {Time.frameCount}, ExpandFrame: {expandFrameCount}");
                    
                    // 동일 프레임 클릭 감지 방지 (확장 버튼 클릭 시 바로 닫히는 현상 제거)
                    if (Time.frameCount <= expandFrameCount)
                    {
                        Log("Ignoring click: click occurred in the same frame as Expand.");
                        return;
                    }

                    bool overButton = IsPointerOverButton(pointerPosition);
                    Log($"IsPointerOverButton check result: {overButton}");
                    if (!overButton)
                    {
                        Log("Pointer is outside. Triggering Collapse().");
                        Collapse();
                    }
                }
            }
        }

        /// <summary>
        /// 지정한 대상 크기 및 피벗으로 확장을 시작합니다.
        /// </summary>
        public void Expand()
        {
            Initialize(); // 레이아웃 프레임 지연 등으로 미처 초기화가 안 되었다면 즉시 초기화 시도
            if (targetRectTransform == null)
            {
                Log("Expand() aborted: targetRectTransform is null.");
                return;
            }

            // 이미 확장되어 있는 다른 버튼이 존재한다면, 해당 버튼을 먼저 닫습니다.
            if (currentlyExpandedButton != null && currentlyExpandedButton != this)
            {
                Log($"[Expand] Another button '{currentlyExpandedButton.name}' is currently expanded. Collapsing it first.");
                currentlyExpandedButton.Collapse();
            }

            currentlyExpandedButton = this;
            isExpanded = true;
            expandFrameCount = Time.frameCount;
            onExpandStart?.Invoke();

            Vector2 destSize = new Vector2(
                expandWidth ? targetWidth : originalWidth,
                expandHeight ? targetHeight : originalHeight
            );
            Vector2 destPivot = changePivot ? targetPivot : originalPivot;

            Log($"Expand() - Target Size: {destSize}, Target Pivot: {destPivot}, Frame: {expandFrameCount}");

            if (activeTransition != null)
            {
                Log("Active transition found, stopping previous coroutine.");
                StopCoroutine(activeTransition);
            }
            activeTransition = StartCoroutine(TransitionRoutine(destSize, destPivot, onExpandComplete));
        }

        /// <summary>
        /// 원래 크기 및 피벗으로 축소를 시작합니다.
        /// </summary>
        public void Collapse()
        {
            if (targetRectTransform == null)
            {
                Log("Collapse() aborted: targetRectTransform is null.");
                return;
            }

            if (currentlyExpandedButton == this)
            {
                currentlyExpandedButton = null;
            }

            isExpanded = false;
            onCollapseStart?.Invoke();

            Vector2 destSize = new Vector2(originalWidth, originalHeight);
            Vector2 destPivot = originalPivot;

            Log($"Collapse() - Target Size: {destSize}, Target Pivot: {destPivot}");

            if (activeTransition != null)
            {
                Log("Active transition found, stopping previous coroutine.");
                StopCoroutine(activeTransition);
            }
            activeTransition = StartCoroutine(TransitionRoutine(destSize, destPivot, onCollapseComplete));
        }

        /// <summary>
        /// 크기와 피벗을 부드럽게 변경하는 코루틴입니다.
        /// </summary>
        private IEnumerator TransitionRoutine(Vector2 targetSize, Vector2 targetPivot, UnityEngine.Events.UnityEvent completionEvent)
        {
            Vector2 startSize = new Vector2(targetRectTransform.rect.width, targetRectTransform.rect.height);
            Vector2 startPivot = targetRectTransform.pivot;
            Vector3 startLocalPos = targetRectTransform.localPosition;

            Log($"Transition START. From Size: {startSize}, Pivot: {startPivot} -> To Size: {targetSize}, Pivot: {targetPivot}");

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float easedProgress = EvaluateEasing(progress);

                Vector2 currentSize = Vector2.Lerp(startSize, targetSize, easedProgress);
                Vector2 currentPivot = Vector2.Lerp(startPivot, targetPivot, easedProgress);

                SetPivotAndSize(targetRectTransform, currentPivot, currentSize, startPivot, targetPivot, startSize, startLocalPos);

                yield return null;
            }

            // 정확한 최종값 설정
            SetPivotAndSize(targetRectTransform, targetPivot, targetSize, startPivot, targetPivot, startSize, startLocalPos);
            
            // 축소 완료 시 LayoutElement 값을 원래 속성으로 복구
            if (!isExpanded && targetLayoutElement != null)
            {
                targetLayoutElement.preferredWidth = originalPrefWidth;
                targetLayoutElement.preferredHeight = originalPrefHeight;
                Log("LayoutElement parameters restored to original settings.");
            }

            Log("Transition COMPLETE.");
            activeTransition = null;
            completionEvent?.Invoke();
        }

        /// <summary>
        /// RectTransform의 pivot과 size를 변경하면서, 
        /// 해당 UI 요소가 화면 상에서 불필요하게 튀거나 시프팅되지 않도록 위치를 조정해줍니다.
        /// </summary>
        private void SetPivotAndSize(
            RectTransform rect, 
            Vector2 currentPivot, 
            Vector2 currentSize, 
            Vector2 startPivot, 
            Vector2 targetPivot, 
            Vector2 startSize, 
            Vector3 startLocalPos)
        {
            Vector3 scale = rect.localScale;
            
            Vector3 targetPivotStartOffset = new Vector3(
                (targetPivot.x - startPivot.x) * startSize.x * scale.x,
                (targetPivot.y - startPivot.y) * startSize.y * scale.y,
                0f
            );
            Vector3 targetPivotStartPos = startLocalPos + targetPivotStartOffset;

            rect.pivot = currentPivot;
            
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentSize.x);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, currentSize.y);

            if (targetLayoutElement != null)
            {
                if (expandWidth) targetLayoutElement.preferredWidth = currentSize.x;
                if (expandHeight) targetLayoutElement.preferredHeight = currentSize.y;

                // Force parent layout group rebuild to prevent alignment breaking
                if (rect.parent != null)
                {
                    LayoutRebuilder.MarkLayoutForRebuild(rect.parent as RectTransform);
                }
            }

            // Only adjust localPosition when NOT managed by a LayoutGroup
            if (targetLayoutElement == null)
            {
                Vector3 targetPivotCurrentOffset = new Vector3(
                    (targetPivot.x - currentPivot.x) * currentSize.x * scale.x,
                    (targetPivot.y - currentPivot.y) * currentSize.y * scale.y,
                    0f
                );

                rect.localPosition = targetPivotStartPos - targetPivotCurrentOffset;
            }
        }

        /// <summary>
        /// 포인터(마우스/터치)가 현재 버튼 영역 혹은 해당 자식 오브젝트 위에 있는지 여부를 반환합니다.
        /// 또한, 다른 EasingExpandableButton 위에 클릭이 일어났다면
        /// 각 버튼의 OnPointerClick 이벤트가 수축과 확장을 직접 동시에 수행할 수 있도록 true를 반환하여
        /// Update()에 의한 조기 수축(레이아웃 이동으로 인한 클릭 씹힘)을 방어합니다.
        /// </summary>
        private bool IsPointerOverButton(Vector2 pointerPosition)
        {
            if (EventSystem.current == null) return false;

            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = pointerPosition;

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            if (showDebugLogs)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine($"[IsPointerOverButton] Raycast Results at position {eventData.position}:");
                foreach (var result in results)
                {
                    sb.AppendLine($"- Hit: {result.gameObject.name} (Parent: {result.gameObject.transform.parent?.name})");
                }
                Log(sb.ToString());
            }

            foreach (var result in results)
            {
                // 1. 자기 자신 또는 대상 트랜스폼 영역 위에 있는지 검사
                if (result.gameObject == gameObject || result.gameObject.transform.IsChildOf(transform))
                {
                    return true;
                }

                if (targetRectTransform != null && (result.gameObject == targetRectTransform.gameObject || result.gameObject.transform.IsChildOf(targetRectTransform)))
                {
                    return true;
                }

                // 2. 다른 EasingExpandableButton 위를 누른 경우, 그 버튼이 클릭 이벤트를 받아가도록
                // 이 버튼의 Update에 의한 조기 수축을 막습니다. (클릭 씹힘 완벽 방어)
                var otherButton = result.gameObject.GetComponentInParent<EasingExpandableButton>();
                if (otherButton != null)
                {
                    Log($"Click landed on another ExpandableButton: '{otherButton.name}'. Preventing early collapse from Update().");
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Easing 그래프 계산기
        /// </summary>
        private float EvaluateEasing(float t)
        {
            switch (easingType)
            {
                case EasingType.Linear:
                    return t;
                case EasingType.EaseInQuad:
                    return t * t;
                case EasingType.EaseOutQuad:
                    return t * (2f - t);
                case EasingType.EaseInOutQuad:
                    return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
                case EasingType.EaseInCubic:
                    return t * t * t;
                case EasingType.EaseOutCubic:
                    return 1f - Mathf.Pow(1f - t, 3f);
                case EasingType.EaseInOutCubic:
                    return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
                case EasingType.EaseOutBack:
                    const float c1 = 1.70158f;
                    const float c3 = c1 + 1f;
                    return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
                case EasingType.EaseOutBounce:
                    return EaseOutBounce(t);
                case EasingType.CustomCurve:
                    return customCurve.Evaluate(t);
                default:
                    return t;
            }
        }

        private float EaseOutBounce(float x)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (x < 1f / d1)
            {
                return n1 * x * x;
            }
            else if (x < 2f / d1)
            {
                return n1 * (x -= 1.5f / d1) * x + 0.75f;
            }
            else if (x < 2.5f / d1)
            {
                return n1 * (x -= 2.25f / d1) * x + 0.9375f;
            }
            else
            {
                return n1 * (x -= 2.625f / d1) * x + 0.984375f;
            }
        }
    }
}

