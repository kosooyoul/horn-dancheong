using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HornDancheong.Seongwoo.UI
{
    /// <summary>
    /// 버튼 클릭 시 지정한 방향과 거리로 easing 애니메이션을 통해 이동하는 컴포넌트입니다.
    /// 버튼 자신(Self) 및 여러 개의 별도 UI 요소(RectTransform)를 개별 거리를 지정하여 동시에 움직일 수 있도록 지원합니다.
    /// </summary>
    public class EasingMoveButton : ButtonBase
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

        public enum MoveDirection
        {
            Up,
            Down,
            Left,
            Right,
            Custom
        }

        public enum MovementMode
        {
            OneWay,     // 원래 위치에서 목표 위치로 1회 이동
            Toggle,     // 클릭할 때마다 원래 위치 <-> 목표 위치 왕복
            Relative    // 클릭할 때마다 현재 위치 기준 상대값만큼 누적 이동
        }

        [System.Serializable]
        public class TargetMoveInfo
        {
            [Tooltip("이동시킬 대상 RectTransform입니다.")]
            public RectTransform targetRect;

            [Tooltip("이동할 거리(px)입니다. (useSelfSizeAsDistance가 활성화된 경우 무시됩니다)")]
            public float distance = 100f;

            [Tooltip("대상 자신의 크기(가로/세로)를 기반으로 이동 거리를 결정할지 여부입니다.")]
            public bool useSelfSizeAsDistance = false;

            [System.NonSerialized]
            public Vector3 originalPosition;
            
            [System.NonSerialized]
            public bool isInitialized = false;
        }

        [Header("Self (Button) Settings")]
        [Tooltip("버튼(자기 자신)을 이동시킬지 여부입니다.")]
        [SerializeField] private bool moveSelf = true;

        [Tooltip("버튼(자기 자신)이 이동할 거리(px)입니다. (useSelfSizeAsDistance가 활성화된 경우 무시됩니다)")]
        [SerializeField] private float selfDistance = 50f;

        [Tooltip("버튼 자신의 크기(가로/세로)를 기반으로 이동 거리를 결정할지 여부입니다.")]
        [SerializeField] private bool useSelfSizeAsDistance = false;

        [Header("Target List Settings")]
        [Tooltip("버튼 클릭 시 함께 이동시킬 대상 RectTransform과 거리 설정 목록입니다.")]
        [SerializeField] private TargetMoveInfo[] targetMoveList;

        [Header("Common Movement Settings")]
        [Tooltip("이동할 방향을 설정합니다.")]
        [SerializeField] private MoveDirection direction = MoveDirection.Up;

        [Tooltip("방향이 Custom인 경우 사용할 사용자 정의 방향 벡터입니다.")]
        [SerializeField] private Vector2 customDirection = Vector2.up;

        [Tooltip("이동 애니메이션 지속 시간(초)입니다.")]
        [SerializeField] private float duration = 0.3f;

        [Tooltip("이동 모드 설정입니다.")]
        [SerializeField] private MovementMode movementMode = MovementMode.Toggle;

        [Tooltip("RectTransform의 anchoredPosition 대신 localPosition을 사용할지 여부입니다.")]
        [SerializeField] private bool useLocalPosition = false;

        [Header("Text Settings")]
        [Tooltip("상태 변화에 따라 텍스트 내용을 변경할 TextMeshProUGUI 컴포넌트입니다.")]
        [SerializeField] private TextMeshProUGUI targetText;

        [Tooltip("기본 상태(시작 위치)일 때 표시할 텍스트입니다. 비워두면 기존 컴포넌트에 적힌 텍스트를 기본값으로 지정합니다.")]
        [SerializeField] private string startStateText;

        [Tooltip("이동 상태(목표 위치)일 때 표시할 텍스트입니다.")]
        [SerializeField] private string targetStateText;

        [Tooltip("텍스트 변경 시 페이드 아웃 -> 텍스트 교체 -> 페이드 인 연출을 적용할지 여부입니다.")]
        [SerializeField] private bool fadeTextTransition = true;

        [Header("Easing Settings")]
        [SerializeField] private EasingType easingType = EasingType.EaseOutQuad;
        [SerializeField] private AnimationCurve customCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Events")]
        public UnityEngine.Events.UnityEvent onMoveStart;
        public UnityEngine.Events.UnityEvent onMoveComplete;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;

        private RectTransform _selfRectTransform;
        private Vector3 _originalSelfPosition;
        private Color _originalTextColor;
        private bool _isInitialized = false;
        private bool _isAtTarget = false;
        private Coroutine _activeTransition;

        private struct MoveElement
        {
            public RectTransform rect;
            public Vector3 startPos;
            public Vector3 destPos;
        }

        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// 초기 위치 및 텍스트 정보를 기록하고 초기화 단계를 거칩니다.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            // 1. 본인 RectTransform 설정
            _selfRectTransform = GetComponent<RectTransform>();
            if (_selfRectTransform != null)
            {
                _originalSelfPosition = GetPosition(_selfRectTransform);
            }
            else
            {
                Debug.LogError($"[{nameof(EasingMoveButton)}] 본인의 RectTransform을 찾을 수 없습니다.", this);
                return;
            }

            // 2. 타겟 리스트 초기화
            if (targetMoveList != null)
            {
                foreach (var info in targetMoveList)
                {
                    if (info != null && info.targetRect != null)
                    {
                        info.originalPosition = GetPosition(info.targetRect);
                        info.isInitialized = true;
                    }
                }
            }

            // 3. 텍스트 설정 캐싱 및 초기 상태 지정
            if (targetText != null)
            {
                _originalTextColor = targetText.color;

                if (string.IsNullOrEmpty(startStateText))
                {
                    startStateText = targetText.text;
                }

                targetText.text = _isAtTarget ? targetStateText : startStateText;
            }

            _isInitialized = true;
            Log($"Initialized. Self Original: {_originalSelfPosition}, Target list count: {(targetMoveList != null ? targetMoveList.Length : 0)}");
        }

        protected override void Function()
        {
            TriggerMove();
        }

        /// <summary>
        /// 이동 애니메이션을 시작합니다.
        /// </summary>
        public void TriggerMove()
        {
            Initialize();
            if (_selfRectTransform == null) return;

            int elementCount = 0;
            if (moveSelf) elementCount++;
            if (targetMoveList != null)
            {
                foreach (var info in targetMoveList)
                {
                    if (info != null && info.targetRect != null && info.isInitialized)
                    {
                        // 자기 자신과 동일한 요소가 중복 추가되는 것 방지
                        if (info.targetRect == _selfRectTransform) continue;
                        elementCount++;
                    }
                }
            }

            MoveElement[] elements = new MoveElement[elementCount];
            int idx = 0;

            Vector3 directionVec = (Vector3)GetDirectionVector().normalized;
            bool isHeadingToTarget = !_isAtTarget;

            // 1. 버튼 자신 요소 추가
            if (moveSelf)
            {
                float selfDist = GetCalculatedDistance(_selfRectTransform, selfDistance, useSelfSizeAsDistance);
                Vector3 selfOffset = directionVec * selfDist;
                Vector3 selfStart = GetPosition(_selfRectTransform);
                Vector3 selfDest = selfStart;

                if (movementMode == MovementMode.OneWay)
                {
                    selfDest = _originalSelfPosition + selfOffset;
                }
                else if (movementMode == MovementMode.Toggle)
                {
                    selfDest = !_isAtTarget ? _originalSelfPosition + selfOffset : _originalSelfPosition;
                }
                else if (movementMode == MovementMode.Relative)
                {
                    selfDest = selfStart + selfOffset;
                }

                elements[idx++] = new MoveElement
                {
                    rect = _selfRectTransform,
                    startPos = selfStart,
                    destPos = selfDest
                };
            }

            // 2. 타겟 리스트 요소 추가
            if (targetMoveList != null)
            {
                foreach (var info in targetMoveList)
                {
                    if (info != null && info.targetRect != null && info.isInitialized)
                    {
                        if (info.targetRect == _selfRectTransform) continue;

                        float dist = GetCalculatedDistance(info.targetRect, info.distance, info.useSelfSizeAsDistance);
                        Vector3 offset = directionVec * dist;
                        Vector3 start = GetPosition(info.targetRect);
                        Vector3 dest = start;

                        if (movementMode == MovementMode.OneWay)
                        {
                            dest = info.originalPosition + offset;
                        }
                        else if (movementMode == MovementMode.Toggle)
                        {
                            dest = !_isAtTarget ? info.originalPosition + offset : info.originalPosition;
                        }
                        else if (movementMode == MovementMode.Relative)
                        {
                            dest = start + offset;
                        }

                        elements[idx++] = new MoveElement
                        {
                            rect = info.targetRect,
                            startPos = start,
                            destPos = dest
                        };
                    }
                }
            }

            // 상태 변경 적용
            if (movementMode == MovementMode.OneWay)
            {
                _isAtTarget = true;
            }
            else if (movementMode == MovementMode.Toggle)
            {
                _isAtTarget = !_isAtTarget;
            }
            else if (movementMode == MovementMode.Relative)
            {
                _isAtTarget = !_isAtTarget; // relative 모드에서도 텍스트 연출 전환을 위해 상태 교체 지원
            }

            Log($"TriggerMove. Mode: {movementMode}, Total elements moving: {elements.Length}");

            if (_activeTransition != null)
            {
                StopCoroutine(_activeTransition);
            }
            _activeTransition = StartCoroutine(MoveRoutine(elements, isHeadingToTarget));
        }

        /// <summary>
        /// 모든 요소들을 원래 위치 및 기본 텍스트 상태로 애니메이션 없이 즉시 초기화합니다.
        /// </summary>
        public void ResetPosition()
        {
            Initialize();
            if (_activeTransition != null)
            {
                StopCoroutine(_activeTransition);
                _activeTransition = null;
            }

            if (moveSelf && _selfRectTransform != null)
            {
                SetPosition(_selfRectTransform, _originalSelfPosition);
            }

            if (targetMoveList != null)
            {
                foreach (var info in targetMoveList)
                {
                    if (info != null && info.targetRect != null && info.isInitialized)
                    {
                        SetPosition(info.targetRect, info.originalPosition);
                    }
                }
            }

            _isAtTarget = false;

            if (targetText != null)
            {
                targetText.text = startStateText;
                targetText.color = _originalTextColor;
            }

            Log("Reset position and text to original.");
        }

        private IEnumerator MoveRoutine(MoveElement[] elements, bool isHeadingToTarget)
        {
            onMoveStart?.Invoke();
            float elapsed = 0f;
            bool textChanged = false;
            string newText = isHeadingToTarget ? targetStateText : startStateText;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float easedProgress = EvaluateEasing(progress);

                // 1. 모든 요소 보간 위치 적용
                for (int i = 0; i < elements.Length; i++)
                {
                    if (elements[i].rect != null)
                    {
                        SetPosition(elements[i].rect, Vector3.Lerp(elements[i].startPos, elements[i].destPos, easedProgress));
                    }
                }

                // 2. 텍스트 연출 적용
                if (targetText != null)
                {
                    if (fadeTextTransition)
                    {
                        float alphaProgress;
                        if (progress < 0.5f)
                        {
                            // 전반부: 페이드 아웃 (알파 1 -> 0)
                            alphaProgress = 1f - (progress / 0.5f);
                        }
                        else
                        {
                            // 후반부: 페이드 인 (알파 0 -> 1)
                            if (!textChanged)
                            {
                                targetText.text = newText;
                                textChanged = true;
                            }
                            alphaProgress = (progress - 0.5f) / 0.5f;
                        }

                        Color c = _originalTextColor;
                        c.a = _originalTextColor.a * alphaProgress;
                        targetText.color = c;
                    }
                    else
                    {
                        // 페이드 연출이 비활성화된 경우 이동 중간 시점에 즉시 교체
                        if (progress >= 0.5f && !textChanged)
                        {
                            targetText.text = newText;
                            textChanged = true;
                        }
                    }
                }

                yield return null;
            }

            // 정확한 최종값 보정
            for (int i = 0; i < elements.Length; i++)
            {
                if (elements[i].rect != null)
                {
                    SetPosition(elements[i].rect, elements[i].destPos);
                }
            }

            if (targetText != null)
            {
                targetText.text = newText;
                if (fadeTextTransition)
                {
                    targetText.color = _originalTextColor;
                }
            }

            _activeTransition = null;
            onMoveComplete?.Invoke();
            Log($"Move Complete. Active elements count: {elements.Length}");
        }

        private Vector3 GetPosition(RectTransform rect)
        {
            if (useLocalPosition)
            {
                return rect.localPosition;
            }
            else
            {
                return rect.anchoredPosition3D;
            }
        }

        private void SetPosition(RectTransform rect, Vector3 pos)
        {
            if (useLocalPosition)
            {
                rect.localPosition = pos;
            }
            else
            {
                rect.anchoredPosition3D = pos;
            }
        }

        private float GetCalculatedDistance(RectTransform rect, float defaultDistance, bool useSelfSize)
        {
            if (!useSelfSize) return defaultDistance;
            if (rect == null) return 0f;

            switch (direction)
            {
                case MoveDirection.Up:
                case MoveDirection.Down:
                    return rect.rect.height;
                case MoveDirection.Left:
                case MoveDirection.Right:
                    return rect.rect.width;
                case MoveDirection.Custom:
                    Vector2 dirNorm = customDirection.normalized;
                    return Mathf.Abs(dirNorm.x) * rect.rect.width + Mathf.Abs(dirNorm.y) * rect.rect.height;
                default:
                    return 0f;
            }
        }

        private Vector2 GetDirectionVector()
        {
            switch (direction)
            {
                case MoveDirection.Up: return Vector2.up;
                case MoveDirection.Down: return Vector2.down;
                case MoveDirection.Left: return Vector2.left;
                case MoveDirection.Right: return Vector2.right;
                case MoveDirection.Custom: return customDirection;
                default: return Vector2.zero;
            }
        }

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

        private void Log(string message)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[EasingMoveButton] {message}", this);
            }
        }
    }
}
