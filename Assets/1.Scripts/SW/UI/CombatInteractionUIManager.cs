using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HornDancheong.Seongwoo.UI
{
    public class CombatInteractionUIManager : MonoBehaviour
    {
        [Header("Character Info Elements")]
        [SerializeField] private Image portraitImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Image classImage;
        [SerializeField] private TMP_Text classText;
        [SerializeField] private Image attributeImage;
        [SerializeField] private TMP_Text attributeText;

        [Header("Stat Sliders")]
        [SerializeField] private Slider hpSlider;
        [SerializeField] private Slider spSlider;

        [Header("Buff/Debuff Panel")]
        [SerializeField] private RectTransform buffDebuffPanel;
        [SerializeField] private Image[] buffDebuffSlots;

        [Header("Sub Panels")]
        [SerializeField] private GameObject actionMenuPanel;
        [SerializeField] private GameObject skillMenuPanel;
        [SerializeField] private Transform skillListParent;

        [Header("Action Buttons")]
        [SerializeField] private Button moveButton;
        [SerializeField] private Button attackButton;
        [SerializeField] private Button pendingButton; // 대기 버튼

        [Header("Skill Prefab")]
        [SerializeField] private GameObject skillButtonPrefab; // 스킬 목록 버튼 프리팹

        [Header("Warning Text Settings")]
        [SerializeField] private TMP_Text warningText;

        // 현재 조작 중인 전투 컨트롤러 및 캐릭터 정보
        private ICombatInteractionController _controller;
        private ICharacterBattleInfo _currentCharacterInfo;

        // 동적으로 생성한 스킬 버튼들을 추적 및 정리하기 위한 리스트
        private readonly List<GameObject> _instantiatedSkillButtons = new List<GameObject>();

        // 버튼 원래 색상 및 텍스트 캐싱용 필드
        private Color _originalMoveButtonColor = Color.white;
        private Color _originalAttackButtonColor = Color.white;
        private Color _originalMoveTextColor = Color.white;
        private Color _originalAttackTextColor = Color.white;
        private TMP_Text _moveText;
        private TMP_Text _attackText;
        private bool _colorsCached = false;

        // 행동 가능 여부 상태
        private bool _canMove = true;
        private bool _canUseSkills = true;

        private void Awake()
        {
            // 기본 버튼 이벤트 리스너 등록
            if (moveButton != null) moveButton.onClick.AddListener(OnMoveButtonClicked);
            if (attackButton != null) attackButton.onClick.AddListener(OnAttackButtonClicked);
            if (pendingButton != null) pendingButton.onClick.AddListener(OnPendingButtonClicked);

            if (warningText != null)
            {
                warningText.gameObject.SetActive(false);
            }

            // 초기 상태는 액션 메뉴 보이기, 스킬 메뉴 숨기기
            ShowActionMenu();
        }

        /// <summary>
        /// 캐릭터 정보 및 컨트롤러를 바인딩하여 UI를 갱신합니다.
        /// </summary>
        public void Initialize(ICharacterBattleInfo characterInfo, ICombatInteractionController controller)
        {
            _currentCharacterInfo = characterInfo;
            _controller = controller;

            if (_currentCharacterInfo == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            CacheOriginalColors();
            ShowActionMenu();

            // 1. 캐릭터 기본 이름 및 초상화 갱신
            if (nameText != null) nameText.text = _currentCharacterInfo.CharacterName;
            UpdatePortrait(_currentCharacterInfo);

            // 2. 체력 바 갱신
            if (hpSlider != null)
            {
                hpSlider.maxValue = _currentCharacterInfo.MaxHp;
                hpSlider.value = _currentCharacterInfo.CurrentHp;
            }

            // 3. 확장 캐릭터 정보 (SP, 직업, 속성, 버프/디버프) 처리
            var extendedInfo = _currentCharacterInfo as ICombatCharacterInfo;
            if (extendedInfo != null)
            {
                // SP 바 갱신
                if (spSlider != null)
                {
                    spSlider.gameObject.SetActive(true);
                    spSlider.maxValue = extendedInfo.MaxSp;
                    spSlider.value = extendedInfo.CurrentSp;
                }

                // 직업 이름 및 아이콘 - CharacterClassUI가 있으면 활용
                var classUI = classImage != null ? classImage.GetComponent<CharacterClassUI>() : null;
                if (classUI != null && extendedInfo.UnitData != null)
                {
                    classUI.Initialize(extendedInfo.UnitData);
                }
                else
                {
                    if (classText != null) classText.text = extendedInfo.ClassName;
                    if (classImage != null)
                    {
                        classImage.sprite = extendedInfo.ClassIcon;
                        classImage.gameObject.SetActive(extendedInfo.ClassIcon != null);
                    }
                }

                // 속성 이름 및 아이콘
                if (attributeText != null) attributeText.text = extendedInfo.AttributeName;
                if (attributeImage != null)
                {
                    attributeImage.sprite = extendedInfo.AttributeIcon;
                    attributeImage.gameObject.SetActive(extendedInfo.AttributeIcon != null);
                }

                // 버프/디버프 슬롯 갱신
                UpdateBuffDebuffSlots(extendedInfo.ActiveBuffDebuffs);
            }
            else
            {
                // 확장 정보가 없는 일반 BattleUnitEntry 대응 (폴백)
                if (spSlider != null) spSlider.gameObject.SetActive(false);
                if (classText != null) classText.text = "클래스 정보 없음";
                if (classImage != null) classImage.gameObject.SetActive(false);
                if (attributeText != null) attributeText.text = "속성 정보 없음";
                if (attributeImage != null) attributeImage.gameObject.SetActive(false);

                UpdateBuffDebuffSlots(null);
            }
        }

        /// <summary>
        /// 초상화 스프라이트를 설정합니다. 없으면 스프라이트 영역을 비활성화합니다.
        /// </summary>
        private void UpdatePortrait(ICharacterBattleInfo data)
        {
            if (portraitImage == null) return;

            if (data.PortraitSprite != null)
            {
                portraitImage.sprite = data.PortraitSprite;
                portraitImage.gameObject.SetActive(true);
            }
            else if (!string.IsNullOrEmpty(data.PortraitPath))
            {
                Sprite loadedSprite = Resources.Load<Sprite>(data.PortraitPath);
                if (loadedSprite != null)
                {
                    portraitImage.sprite = loadedSprite;
                    portraitImage.gameObject.SetActive(true);
                }
                else
                {
                    portraitImage.gameObject.SetActive(false);
                }
            }
            else
            {
                portraitImage.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 버프/디버프 슬롯을 활성화하고 이미지를 매핑합니다.
        /// </summary>
        public void UpdateBuffDebuffSlots(Sprite[] buffDebuffs)
        {
            if (buffDebuffSlots == null || buffDebuffSlots.Length == 0) return;

            for (int i = 0; i < buffDebuffSlots.Length; i++)
            {
                if (buffDebuffs != null && i < buffDebuffs.Length && buffDebuffs[i] != null)
                {
                    buffDebuffSlots[i].sprite = buffDebuffs[i];
                    buffDebuffSlots[i].gameObject.SetActive(true);
                }
                else
                {
                    buffDebuffSlots[i].gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 액션 메뉴 활성화 상태로 전환합니다.
        /// </summary>
        public void ShowActionMenu()
        {
            if (actionMenuPanel != null) actionMenuPanel.SetActive(true);
            if (skillMenuPanel != null) skillMenuPanel.SetActive(false);

            // 경고창이 열려있다면 즉시 닫습니다.
            HideWarningImmediately();

            UpdateActionButtonsState();
        }

        /// <summary>
        /// 스킬 선택 메뉴 활성화 상태로 전환합니다.
        /// </summary>
        private void ShowSkillMenu()
        {
            if (actionMenuPanel != null) actionMenuPanel.SetActive(false);
            if (skillMenuPanel != null) skillMenuPanel.SetActive(true);

            PopulateSkillList();
        }

        /// <summary>
        /// 현재 캐릭터 정보에 따라 동적으로 스킬 버튼 리스트를 구성합니다.
        /// </summary>
        private void PopulateSkillList()
        {
            // 기존 생성된 스킬 버튼들 삭제
            foreach (var btn in _instantiatedSkillButtons)
            {
                if (btn != null) Destroy(btn);
            }
            _instantiatedSkillButtons.Clear();

            if (skillListParent == null || _currentCharacterInfo == null) return;

            var extendedInfo = _currentCharacterInfo as ICombatCharacterInfo;

            // 1. 실제 유닛의 스킬 목록이 제공된다면 이를 사용하여 생성
            var realSkills = extendedInfo?.Skills;
            if (realSkills != null && realSkills.Count > 0)
            {
                foreach (var skill in realSkills)
                {
                    if (skill == null) continue;

                    GameObject buttonObj;
                    if (skillButtonPrefab != null)
                    {
                        buttonObj = Instantiate(skillButtonPrefab, skillListParent);
                    }
                    else
                    {
                        buttonObj = CreateDefaultButtonObject(skill.skillName, skill.apCost);
                    }

                    _instantiatedSkillButtons.Add(buttonObj);

                    Button btn = buttonObj.GetComponent<Button>();
                    TMP_Text btnText = buttonObj.GetComponentInChildren<TMP_Text>();

                    if (btnText != null)
                    {
                        btnText.text = $"{skill.skillName} (AP: {skill.apCost})";
                    }

                    // 직군별 스킬 버튼 스타일 적용
                    var styler = buttonObj.GetComponent<SkillButtonStyler>();
                    if (styler != null && extendedInfo.UnitData != null)
                    {
                        styler.ApplyStyle(extendedInfo.UnitData.role);
                    }

                    string skillId = skill.skillId;
                    if (btn != null)
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => OnSkillSelected(skillId));
                    }
                }
                return; // 실제 스킬 목록 생성이 완료되었으므로 복귀
            }

            // 2. [Fallback] 캐릭터 이름/직업을 기반으로 스킬 세트 판별 (테스트 시연용)
            string nameOrClass = _currentCharacterInfo.CharacterName;
            if (extendedInfo != null && !string.IsNullOrEmpty(extendedInfo.ClassName))
            {
                nameOrClass = extendedInfo.ClassName;
            }

            List<(string id, string name, int cost)> skillsToSpawn = new List<(string, string, int)>();
            skillsToSpawn.Add(("99", "기본 공격", 0));

            if (nameOrClass.Contains("Warrior") || nameOrClass.Contains("전사"))
            {
                skillsToSpawn.Add(("3", "광역 폭발", 12));
            }
            else if (nameOrClass.Contains("Archer") || nameOrClass.Contains("궁수") || nameOrClass.Contains("마법사"))
            {
                skillsToSpawn.Add(("1", "파이어볼", 5));
            }
            else if (nameOrClass.Contains("Healer") || nameOrClass.Contains("힐러") || nameOrClass.Contains("지원"))
            {
                skillsToSpawn.Add(("2", "치유", 3));
            }
            else
            {
                skillsToSpawn.Add(("1", "파이어볼", 5));
                skillsToSpawn.Add(("2", "치유", 3));
                skillsToSpawn.Add(("3", "광역 폭발", 12));
            }

            // 가상 캐릭터의 역할 추정
            KD.UnitRole mockRole = KD.UnitRole.Dealer;
            if (nameOrClass.Contains("Warrior") || nameOrClass.Contains("전사")) mockRole = KD.UnitRole.Dealer;
            else if (nameOrClass.Contains("Archer") || nameOrClass.Contains("궁수") || nameOrClass.Contains("마법사")) mockRole = KD.UnitRole.Supporter;
            else if (nameOrClass.Contains("Healer") || nameOrClass.Contains("힐러") || nameOrClass.Contains("지원")) mockRole = KD.UnitRole.Healer;

            // 버튼 오브젝트 생성 및 이벤트 바인딩
            foreach (var skill in skillsToSpawn)
            {
                GameObject buttonObj;
                if (skillButtonPrefab != null)
                {
                    buttonObj = Instantiate(skillButtonPrefab, skillListParent);
                }
                else
                {
                    buttonObj = CreateDefaultButtonObject(skill.name, skill.cost);
                }

                _instantiatedSkillButtons.Add(buttonObj);

                Button btn = buttonObj.GetComponent<Button>();
                TMP_Text btnText = buttonObj.GetComponentInChildren<TMP_Text>();

                if (btnText != null)
                {
                    btnText.text = $"{skill.name} (MP: {skill.cost})";
                }

                // 직군별 스킬 버튼 스타일 적용
                var styler = buttonObj.GetComponent<SkillButtonStyler>();
                if (styler != null)
                {
                    styler.ApplyStyle(mockRole);
                }

                string skillId = skill.id;
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnSkillSelected(skillId));
                }
            }
        }

        /// <summary>
        /// 프리팹 미지정 시 생성할 임시 기본 버튼 오브젝트를 만듭니다.
        /// </summary>
        private GameObject CreateDefaultButtonObject(string skillName, int cost)
        {
            GameObject go = new GameObject($"Button_{skillName}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(skillListParent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(250f, 40f);

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);

            TextMeshProUGUI tmpText = textGo.GetComponent<TextMeshProUGUI>();
            tmpText.fontSize = 18;
            tmpText.color = Color.black;
            tmpText.alignment = TextAlignmentOptions.Center;

            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            return go;
        }

        // ── UI 클릭 핸들러 ──

        private void OnMoveButtonClicked()
        {
            if (!_canMove)
            {
                Debug.Log("[CombatInteractionUI] 이동 불가능한 상태입니다.");
                return;
            }

            Debug.Log("[CombatInteractionUI] 이동 버튼 클릭됨");
            if (_controller != null)
            {
                _controller.SetMoveMode(true);
            }
        }

        private void OnAttackButtonClicked()
        {
            // 공격 버튼을 누르면 언제나 스킬 메뉴로 진입 가능하게 함 (사거리/AP 제한 등은 실제 스킬 발동 시점에 판단)
            Debug.Log("[CombatInteractionUI] 공격 버튼 클릭됨 -> 스킬 선택 메뉴로 전환");
            ShowSkillMenu();
            if (_controller != null)
            {
                _controller.SetMoveMode(false); // 공격 상태 진입 시 이동 모드는 우선 Off
            }
        }

        private void CacheOriginalColors()
        {
            if (_colorsCached) return;

            if (moveButton != null)
            {
                _moveText = moveButton.GetComponentInChildren<TMP_Text>();
                var img = moveButton.GetComponent<Image>();
                if (img != null) _originalMoveButtonColor = img.color;
                if (_moveText != null) _originalMoveTextColor = _moveText.color;
            }
            if (attackButton != null)
            {
                _attackText = attackButton.GetComponentInChildren<TMP_Text>();
                var img = attackButton.GetComponent<Image>();
                if (img != null) _originalAttackButtonColor = img.color;
                if (_attackText != null) _originalAttackTextColor = _attackText.color;
            }

            _colorsCached = true;
        }

        private void UpdateActionButtonsState()
        {
            if (_currentCharacterInfo == null) return;

            // BattleUnitAdapter를 통해 실제 BattleUnit 참조 획득
            KD.BattleUnit unit = null;
            if (_currentCharacterInfo is BattleUnitAdapter adapter)
            {
                unit = adapter.Unit;
            }

            if (unit != null)
            {
                // 1. 이동 가능 여부 체크
                _canMove = unit.HasEnoughAP(unit.Data.moveAPCost);

                // 2. 스킬 사용 가능 여부 체크 (AP 및 사용가능 상태 확인)
                _canUseSkills = HasAnyUsableSkillWithEnoughAP(unit);
            }
            else
            {
                _canMove = true;
                _canUseSkills = true;
            }

            // 3. Move 버튼 비주얼 갱신 (흑백/회색조 처리)
            if (moveButton != null)
            {
                var img = moveButton.GetComponent<Image>();
                if (img != null)
                {
                    img.color = _canMove ? _originalMoveButtonColor : new Color(0.5f, 0.5f, 0.5f, 0.6f);
                }
                if (_moveText != null)
                {
                    _moveText.color = _canMove ? _originalMoveTextColor : new Color(0.7f, 0.7f, 0.7f, 1f);
                }
            }

            // 4. Attack 버튼 비주얼 갱신 (흑백/회색조 처리)
            if (attackButton != null)
            {
                var img = attackButton.GetComponent<Image>();
                if (img != null)
                {
                    img.color = _canUseSkills ? _originalAttackButtonColor : new Color(0.5f, 0.5f, 0.5f, 0.6f);
                }
                if (_attackText != null)
                {
                    _attackText.color = _canUseSkills ? _originalAttackTextColor : new Color(0.7f, 0.7f, 0.7f, 1f);
                }
            }
        }

        private bool HasAnyUsableSkillWithEnoughAP(KD.BattleUnit unit)
        {
            if (unit == null) return false;

            var usableSkills = unit.GetUsableSkills();
            foreach (var skill in usableSkills)
            {
                if (unit.HasEnoughAP(skill.apCost))
                    return true;
            }
            return false;
        }

        private Coroutine _warningCoroutine;

        /// <summary>
        /// 현재 경고 메시지가 화면에 출력 중인지 여부를 반환합니다.
        /// </summary>
        public bool IsWarningActive => _warningCoroutine != null;

        /// <summary>
        /// 화면 중앙의 경고 메시지 텍스트를 3초간 출력했다가 사라지게 만듭니다. (출력 완료 후 콜백 호출 지원)
        /// </summary>
        public void ShowWarningMessage(string message, System.Action callback = null)
        {
            if (warningText == null)
            {
                callback?.Invoke();
                return;
            }

            if (_warningCoroutine != null)
            {
                StopCoroutine(_warningCoroutine);
            }
            _warningCoroutine = StartCoroutine(WarningRoutine(message, callback));
        }

        private IEnumerator WarningRoutine(string message, System.Action callback)
        {
            warningText.text = message;
            warningText.gameObject.SetActive(true);
            yield return new WaitForSeconds(3f);
            warningText.gameObject.SetActive(false);
            _warningCoroutine = null;
            callback?.Invoke();
        }

        /// <summary>
        /// 활성화된 경고 메시지를 즉시 닫고 진행 중인 타이머 코루틴을 중단시킵니다.
        /// </summary>
        public void HideWarningImmediately()
        {
            if (warningText != null)
            {
                warningText.gameObject.SetActive(false);
            }
            if (_warningCoroutine != null)
            {
                StopCoroutine(_warningCoroutine);
                _warningCoroutine = null;
            }
        }

        private void OnPendingButtonClicked()
        {
            Debug.Log("[CombatInteractionUI] 대기 버튼 클릭됨");
            if (_controller != null)
            {
                _controller.ExecuteWait();
            }
        }


        private void OnSkillSelected(string skillId)
        {
            Debug.Log($"[CombatInteractionUI] 스킬 ID {skillId} 선택됨 -> 실행 요청");
            if (_controller != null)
            {
                _controller.ExecuteSkill(skillId);
            }
        }
    }
}
