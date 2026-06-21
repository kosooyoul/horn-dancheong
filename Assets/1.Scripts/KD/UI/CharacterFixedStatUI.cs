using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KD;
using System.Collections;

namespace KD
{
    [RequireComponent(typeof(CanvasGroup))]
    public class CharacterFixedStatUI : MonoBehaviour
    {
        [Header("Target Character Setup")]
        [SerializeField] private string targetCharacterName;

        [Header("UI References")]
        [SerializeField] private Slider hpSlider;
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private Slider spSlider;
        [SerializeField] private TMP_Text spText;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Image characterPortrait;
        [SerializeField] private Material grayscaleMaterial;

        [Header("Hover Area (Optional)")]
        [Tooltip("지정하지 않으면 이 컴포넌트가 달린 RectTransform 영역을 사용합니다.")]
        [SerializeField] private RectTransform customHoverArea;

        [Header("Fade Settings")]
        [SerializeField] private float hoverAlpha = 0.2f;
        [SerializeField] private float normalAlpha = 1.0f;
        [SerializeField] private float fadeSpeed = 5.0f;
        [SerializeField] private float hoverCheckInterval = 0.05f; // 호버 검사 주기 (초)

        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private RectTransform activeHoverArea;

        // 캐싱된 이전 상태값 (값 변화가 없을 때 UI 갱신 방지)
        private int lastHP = -1;
        private int lastSP = -1;
        private bool isInitialized = false;
        private float targetAlpha = 1.0f;

        private TacticalBattleManager battleManager;
        private Coroutine hoverCheckCoroutine;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            rectTransform = GetComponent<RectTransform>();

            // customHoverArea가 지정되지 않았다면 자기 자신을 타겟으로 설정
            activeHoverArea = (customHoverArea != null) ? customHoverArea : rectTransform;
        }

        private void Start()
        {
            // 씬 내의 TacticalBattleManager 탐색 및 턴 이벤트 바인딩
            battleManager = FindObjectOfType<TacticalBattleManager>();
            if (battleManager != null)
            {
                battleManager.OnTurnUpdated += RefreshStatInfo;
            }

            if (nameText != null)
            {
                nameText.text = targetCharacterName;
            }

            // 초기 수동 갱신 시도
            RefreshStatInfo();

            // 호버 체크 코루틴 시작
            hoverCheckCoroutine = StartCoroutine(CoHoverCheck());
        }

        private void OnDestroy()
        {
            if (battleManager != null)
            {
                battleManager.OnTurnUpdated -= RefreshStatInfo;
            }

            if (hoverCheckCoroutine != null)
            {
                StopCoroutine(hoverCheckCoroutine);
            }
        }

        private void Update()
        {
            // 페이드는 자연스러운 애니메이션 연출을 위해 매 프레임 부드럽게 보간
            if (canvasGroup != null && !Mathf.Approximately(canvasGroup.alpha, targetAlpha))
            {
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
            }
        }

        /// <summary>
        /// 턴이 변경될 때 호출되며, 캐릭터 정보를 매니저로부터 조회하여 값이 달라졌을 경우에만 UI를 갱신합니다.
        /// </summary>
        public void RefreshStatInfo()
        {
            if (battleManager == null) return;

            // 전투 참여 유닛 중 이름으로 타겟 검색
            BattleUnit targetUnit = battleManager.GetUnitByName(targetCharacterName);
            if (targetUnit == null)
            {
                // 전투에 참여 중이지 않거나 아직 스폰되지 않은 경우 UI 비활성화 또는 대기
                return;
            }

            int currentHP = targetUnit.CurrentHP;
            int currentSP = targetUnit.CurrentSP;
            int maxHP = targetUnit.Stats.maxHP;
            int maxSP = targetUnit.Stats.maxSP;

            // 값 변화가 없다면 UI 갱신을 생략 (성능 최적화)
            if (isInitialized && lastHP == currentHP && lastSP == currentSP)
            {
                return;
            }

            // 값 갱신
            lastHP = currentHP;
            lastSP = currentSP;
            isInitialized = true;

            // UI 표현 업데이트
            if (hpSlider != null)
            {
                hpSlider.maxValue = maxHP;
                hpSlider.value = currentHP;
            }
            if (hpText != null)
            {
                hpText.text = $"{currentHP} / {maxHP}";
            }

            if (spSlider != null)
            {
                spSlider.maxValue = maxSP;
                spSlider.value = currentSP;
            }
            if (spText != null)
            {
                spText.text = $"{currentSP} / {maxSP}";
            }

            // 초상화 상태 업데이트 (체력 0 이하 사망 시 흑백 처리)
            if (characterPortrait != null)
            {
                if (currentHP <= 0)
                {
                    if (grayscaleMaterial != null)
                    {
                        characterPortrait.material = grayscaleMaterial;
                    }
                    else
                    {
                        // 흑백 마테리얼이 지정되지 않았을 때의 색감 폴백 처리 (기본 컬러를 어둡게 처리하여 흑백 효과 모사)
                        characterPortrait.color = new Color(0.25f, 0.25f, 0.25f, 1.0f);
                    }
                }
                else
                {
                    characterPortrait.material = null;
                    characterPortrait.color = Color.white;
                }
            }
        }

        /// <summary>
        /// 호버 검사 주기에 따라 수동으로 마우스 좌표와 RectTransform의 포함 여부를 연산합니다.
        /// </summary>
        private IEnumerator CoHoverCheck()
        {
            var wait = new WaitForSeconds(hoverCheckInterval);
            while (true)
            {
                if (activeHoverArea != null)
                {
                    // New Input System을 이용해 현재 마우스 좌표 획득
                    Vector2 mousePosition = Vector2.zero;
                    if (UnityEngine.InputSystem.Mouse.current != null)
                    {
                        mousePosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                    }
                    else
                    {
                        mousePosition = Input.mousePosition;
                    }

                    // UI가 Screen Space - Overlay인 것으로 가정하여 camera에 null 전달
                    bool isHovered = RectTransformUtility.RectangleContainsScreenPoint(activeHoverArea, mousePosition, null);
                    targetAlpha = isHovered ? hoverAlpha : normalAlpha;
                }
                yield return wait;
            }
        }
    }
}
