using System.Collections;
using UnityEngine;
using KD;

namespace HornDancheong.Seongwoo.UI
{
    /// <summary>
    /// 플레이어 턴이 활성화되면 Panel_CombatInteraction을 원위치로 EaseIn(슬라이드 인) 시키고,
    /// 플레이어 턴이 비활성화되면 한쪽 방향으로 지정된 거리만큼 EaseOut(슬라이드 아웃) 시키는 컴포넌트입니다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class CombatInteractionPanelTransition : MonoBehaviour
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

        [Header("Battle Manager Settings")]
        [Tooltip("연동할 TacticalBattleManager 입니다. 비워둘 경우 씬에서 자동으로 검색합니다.")]
        [SerializeField] private TacticalBattleManager battleManager;

        [Tooltip("연동할 SimpleBattleManager 입니다. TacticalBattleManager가 없고 이 필드가 비워져 있다면 씬에서 자동으로 검색합니다.")]
        [SerializeField] private SimpleBattleManager simpleBattleManager;

        [Tooltip("씬에서 배틀 매니저를 자동으로 검색할지 여부입니다.")]
        [SerializeField] private bool autoDetectManagers = true;

        [Tooltip("턴 상태를 감지하기 위한 Update 주기입니다. (초 단위, 0이면 매 프레임)")]
        [SerializeField] private float checkInterval = 0f;

        [Header("Movement Settings")]
        [Tooltip("플레이어 턴이 아닐 때 이동할 방향을 설정합니다.")]
        [SerializeField] private MoveDirection direction = MoveDirection.Down;

        [Tooltip("방향이 Custom인 경우 사용할 사용자 정의 방향 벡터입니다.")]
        [SerializeField] private Vector2 customDirection = Vector2.down;

        [Tooltip("이동할 거리(px)입니다. (useSelfSizeAsDistance가 활성화된 경우 무시됩니다)")]
        [SerializeField] private float distance = 150f;

        [Tooltip("자기 자신의 크기(가로/세로)를 기반으로 이동 거리를 결정할지 여부입니다.")]
        [SerializeField] private bool useSelfSizeAsDistance = true;

        [Tooltip("RectTransform의 anchoredPosition 대신 localPosition을 사용할지 여부입니다.")]
        [SerializeField] private bool useLocalPosition = false;

        [Tooltip("시작할 때 (플레이어 턴이 아니라면) 애니메이션 없이 즉시 비활성 위치로 보낼지 여부입니다.")]
        [SerializeField] private bool instantInitAtStart = true;

        [Header("Animation Settings")]
        [Tooltip("이동 애니메이션 지속 시간(초)입니다.")]
        [SerializeField] private float duration = 0.35f;

        [Tooltip("플레이어 턴 활성화 시(원위치로 들어올 때) 사용할 Easing 함수 타입입니다.")]
        [SerializeField] private EasingType easeInType = EasingType.EaseOutCubic;

        [Tooltip("플레이어 턴 비활성화 시(화면 밖으로 나갈 때) 사용할 Easing 함수 타입입니다.")]
        [SerializeField] private EasingType easeOutType = EasingType.EaseInCubic;

        [SerializeField] private AnimationCurve customCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Events")]
        public UnityEngine.Events.UnityEvent onTransitionStart;
        public UnityEngine.Events.UnityEvent onTransitionComplete;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;

        private RectTransform _rectTransform;
        private Vector3 _originalPosition;
        private Vector3 _inactivePosition;
        
        private bool _isInitialized = false;
        private bool _isPlayerTurnActive = false;

        /// <summary>
        /// 현재 플레이어 턴이 활성화된 상태인지 여부를 반환합니다.
        /// </summary>
        public bool IsPlayerTurnActive => _isPlayerTurnActive;
        private Coroutine _activeTransition;
        private float _nextCheckTime = 0f;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            Initialize();
        }

        private void Start()
        {
            Initialize();
            
            // 시작할 때의 상태 결정
            bool isPlayerTurn = CheckIsPlayerTurn();
            _isPlayerTurnActive = isPlayerTurn;
            
            if (instantInitAtStart)
            {
                SetPosition(_rectTransform, isPlayerTurn ? _originalPosition : _inactivePosition);
                Log($"Start initialization: IsPlayerTurn={isPlayerTurn}. Position forced instantly.");
            }
            else
            {
                // 애니메이션과 함께 초기 방향으로 이동
                TriggerTransition(isPlayerTurn, force: true);
            }
        }

        private void Update()
        {
            if (Time.time >= _nextCheckTime)
            {
                _nextCheckTime = Time.time + checkInterval;
                
                bool currentTurnState = CheckIsPlayerTurn();
                if (currentTurnState != _isPlayerTurnActive)
                {
                    Log($"Turn state changed: {_isPlayerTurnActive} -> {currentTurnState}. Triggering transition.");
                    _isPlayerTurnActive = currentTurnState;
                    TriggerTransition(_isPlayerTurnActive);
                }
            }
        }

        public void Initialize()
        {
            if (_isInitialized) return;

            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }

            _originalPosition = GetPosition(_rectTransform);
            
            // 배틀 매니저 검색
            if (autoDetectManagers)
            {
                if (battleManager == null)
                {
                    battleManager = FindObjectOfType<TacticalBattleManager>();
                }
                if (battleManager == null && simpleBattleManager == null)
                {
                    simpleBattleManager = FindObjectOfType<SimpleBattleManager>();
                }
            }

            // 비활성(목표) 위치 계산
            CalculateInactivePosition();

            _isInitialized = true;
            Log($"Initialized. Original Position: {_originalPosition}, Inactive Position: {_inactivePosition}");
        }

        /// <summary>
        /// 인스펙터나 외부에서 거리/방향 설정을 바꿨을 때 강제로 비활성 위치를 갱신할 수 있도록 제공합니다.
        /// </summary>
        public void CalculateInactivePosition()
        {
            if (_rectTransform == null) return;

            Vector3 directionVec = (Vector3)GetDirectionVector().normalized;
            float calcDist = GetCalculatedDistance(_rectTransform, distance, useSelfSizeAsDistance);
            Vector3 offset = directionVec * calcDist;
            _inactivePosition = _originalPosition + offset;
        }

        private bool CheckIsPlayerTurn()
        {
            if (autoDetectManagers)
            {
                if (battleManager == null)
                {
                    battleManager = FindObjectOfType<TacticalBattleManager>();
                }
                if (battleManager == null && simpleBattleManager == null)
                {
                    simpleBattleManager = FindObjectOfType<SimpleBattleManager>();
                }
            }

            if (battleManager != null)
            {
                return battleManager.CurrentPhase == BattlePhase.PlayerPhase &&
                       battleManager.SelectedUnit != null &&
                       battleManager.CurrentActionMode == BattleActionMode.None;
            }
            
            if (simpleBattleManager != null)
            {
                return simpleBattleManager.CurrentPhase == BattlePhase.PlayerPhase &&
                       simpleBattleManager.SelectedUnit != null &&
                       simpleBattleManager.CurrentActionMode == BattleActionMode.None;
            }

            return false;
        }

        /// <summary>
        /// 외부에서 수동으로 플레이어 턴 활성화 상태를 주입하여 트랜지션을 작동시킵니다. (테스트용)
        /// </summary>
        public void SetPlayerTurnActive(bool active)
        {
            Initialize();
            if (_isPlayerTurnActive != active)
            {
                _isPlayerTurnActive = active;
                TriggerTransition(active);
            }
        }

        private void TriggerTransition(bool isPlayerTurn, bool force = false)
        {
            if (!_isInitialized) Initialize();

            Vector3 startPos = GetPosition(_rectTransform);
            Vector3 destPos = isPlayerTurn ? _originalPosition : _inactivePosition;

            if (_activeTransition != null)
            {
                StopCoroutine(_activeTransition);
            }
            _activeTransition = StartCoroutine(TransitionRoutine(startPos, destPos, isPlayerTurn));
        }

        private IEnumerator TransitionRoutine(Vector3 startPos, Vector3 destPos, bool isEntering)
        {
            onTransitionStart?.Invoke();
            float elapsed = 0f;
            EasingType currentEasing = isEntering ? easeInType : easeOutType;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float easedProgress = EvaluateEasing(progress, currentEasing);

                SetPosition(_rectTransform, Vector3.Lerp(startPos, destPos, easedProgress));
                yield return null;
            }

            SetPosition(_rectTransform, destPos);
            _activeTransition = null;
            onTransitionComplete?.Invoke();
            Log($"Transition complete: IsPlayerTurnActive={_isPlayerTurnActive}, Current Position={GetPosition(_rectTransform)}");
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

        private float EvaluateEasing(float t, EasingType type)
        {
            switch (type)
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
                Debug.Log($"[CombatInteractionPanelTransition] {message}", this);
            }
        }
    }
}
