using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HornDancheong.Seongwoo.UI
{
    public class InitiativeManager : MonoBehaviour, IInitiativeUI
    {
        [Header("Prefab References")]
        [SerializeField] private GameObject _characterPanelPrefab;

        [Header("Manual Layout Settings")]
        [SerializeField] private float _panelHeight = 90f;        // 개별 패널의 세로 높이
        [SerializeField] private float _spacing = 10f;            // 패널 간의 수직 간격
        [SerializeField] private float _animationDuration = 0.35f; // 애니메이션 재생 시간
        [SerializeField] private float _slideDistance = 200f;     // 턴 종료 시 왼쪽으로 튕겨나가는 거리

        // 실시간 생성된 UI 패널들을 고유 ID를 기반으로 추적하고 관리하기 위한 딕셔너리
        private readonly Dictionary<string, CharacterPanelUI> _activePanels = new Dictionary<string, CharacterPanelUI>();
        
        // 정렬 순서를 유지하여 absolute 좌표 이동을 관리하는 내부 리스트
        private readonly List<CharacterPanelUI> _panelsList = new List<CharacterPanelUI>();

        private RectTransform _rectTransform;
        private Coroutine _activeAnimationCoroutine;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        /// <summary>
        /// 인덱스별 목표 Y 좌표를 계산합니다. (0번 유닛은 100% 스케일, 1번 이후는 85% 스케일 반영)
        /// </summary>
        private float GetTargetY(int index)
        {
            if (index <= 0) return 0f;
            
            // 0번 패널은 100% 크기이므로 _panelHeight를 그대로 사용하고,
            // 1번 이후 패널들은 85% 축소되므로 (_panelHeight * 0.85f) 크기와 spacing을 누적하여 Y 좌표를 구합니다.
            return -_panelHeight - _spacing - (index - 1) * (_panelHeight * 0.85f + _spacing);
        }

        /// <summary>
        /// 인덱스별 목표 스케일 값을 반환합니다. (맨 위 0번만 100%, 나머지는 85%)
        /// </summary>
        private float GetTargetScale(int index)
        {
            return (index == 0) ? 1.0f : 0.85f;
        }

        /// <summary>
        /// 전투 시작 시 캐릭터 리스트를 받아 우선권(Initiative) 높은 순으로 정렬하여 UI 패널들을 수동 레이아웃 좌표에 배치합니다.
        /// </summary>
        public void InitializeBattleUI(IEnumerable<ICharacterBattleInfo> characters)
        {
            InitializeBattleUI(characters, sortByInitiative: true);
        }

        /// <summary>
        /// 전투 시작 시 캐릭터 리스트를 받아 UI 패널들을 수동 레이아웃 좌표에 배치합니다. (정렬 여부 선택 가능)
        /// </summary>
        public void InitializeBattleUI(IEnumerable<ICharacterBattleInfo> characters, bool sortByInitiative)
        {
            // 애니메이션 초기화
            if (_activeAnimationCoroutine != null)
            {
                StopCoroutine(_activeAnimationCoroutine);
                _activeAnimationCoroutine = null;
            }

            // 기존 패널들 정리
            ClearPanels();

            if (_characterPanelPrefab == null)
            {
                Debug.LogWarning("[InitiativeManager] Character Panel Prefab이 할당되지 않았습니다.");
                return;
            }

            // 정렬 여부에 따라 캐릭터 리스트 구성
            var sortedCharacters = sortByInitiative
                ? characters.Where(c => c != null).OrderByDescending(c => c.Initiative).ToList()
                : characters.Where(c => c != null).ToList();

            for (int i = 0; i < sortedCharacters.Count; i++)
            {
                var charInfo = sortedCharacters[i];

                GameObject panelObj = Instantiate(_characterPanelPrefab, transform);
                CharacterPanelUI panelUI = panelObj.GetComponent<CharacterPanelUI>();

                if (panelUI != null)
                {
                    panelUI.Initialize(charInfo);
                    
                    // 수동 레이아웃 좌표 초기화 설정
                    RectTransform rect = panelUI.GetComponent<RectTransform>();
                    SetupRectTransform(rect);
                    
                    // 정렬 순서 Y 좌표 계산 및 스케일 지정 배치
                    rect.anchoredPosition = new Vector2(0f, GetTargetY(i));
                    panelUI.SetScale(GetTargetScale(i));

                    _activePanels[charInfo.Id] = panelUI;
                    _panelsList.Add(panelUI);
                }
                else
                {
                    Debug.LogWarning("[InitiativeManager] 생성된 프리팹에 CharacterPanelUI 컴포넌트가 없습니다.");
                    Destroy(panelObj);
                }
            }

            UpdateContainerSize();
        }

        /// <summary>
        /// 틱 게이지 등 동적으로 변한 턴 순서에 맞춰 UI 패널들의 위치를 갱신합니다.
        /// </summary>
        public void UpdateTurnOrder(IEnumerable<ICharacterBattleInfo> characters)
        {
            if (_activeAnimationCoroutine != null)
            {
                StopCoroutine(_activeAnimationCoroutine);
                ForceApplyTargets();
            }

            var newCharacters = characters.Where(c => c != null).ToList();
            var newIds = new HashSet<string>(newCharacters.Select(c => c.Id));

            // 1. 더이상 존재하지 않는 캐릭터 UI 제거
            var idsToRemove = new List<string>();
            foreach (var id in _activePanels.Keys)
            {
                if (!newIds.Contains(id))
                {
                    idsToRemove.Add(id);
                }
            }
            foreach (var id in idsToRemove)
            {
                if (_activePanels.TryGetValue(id, out var panel))
                {
                    _panelsList.Remove(panel);
                    _activePanels.Remove(id);
                    Destroy(panel.gameObject);
                }
            }

            // 2. 신규 캐릭터 UI 생성 및 기존 캐릭터 UI 순서 재정렬
            var reorderedPanels = new List<CharacterPanelUI>();
            foreach (var charInfo in newCharacters)
            {
                if (_activePanels.TryGetValue(charInfo.Id, out var panel))
                {
                    panel.Initialize(charInfo);
                    reorderedPanels.Add(panel);
                }
                else
                {
                    if (_characterPanelPrefab != null)
                    {
                        GameObject panelObj = Instantiate(_characterPanelPrefab, transform);
                        CharacterPanelUI panelUI = panelObj.GetComponent<CharacterPanelUI>();
                        if (panelUI != null)
                        {
                            panelUI.Initialize(charInfo);
                            SetupRectTransform(panelUI.GetComponent<RectTransform>());
                            panelUI.SetAlpha(0f); // 페이드인 대기
                            _activePanels[charInfo.Id] = panelUI;
                            reorderedPanels.Add(panelUI);
                        }
                    }
                }
            }

            _panelsList.Clear();
            _panelsList.AddRange(reorderedPanels);

            // 3. 변경된 순서에 맞게 슬라이딩 애니메이션 실행
            _activeAnimationCoroutine = StartCoroutine(AnimateTransitionCoroutine());
        }

        private IEnumerator AnimateTransitionCoroutine()
        {
            int count = _panelsList.Count;
            List<Vector2> startPositions = new List<Vector2>();
            List<Vector2> targetPositions = new List<Vector2>();
            List<float> startScales = new List<float>();
            List<float> targetScales = new List<float>();
            List<float> startAlphas = new List<float>();
            List<float> targetAlphas = new List<float>();

            for (int i = 0; i < count; i++)
            {
                var panel = _panelsList[i];
                var rect = panel.GetComponent<RectTransform>();
                startPositions.Add(rect.anchoredPosition);
                targetPositions.Add(new Vector2(0f, GetTargetY(i)));
                
                startScales.Add(panel.transform.localScale.x);
                targetScales.Add(GetTargetScale(i));

                CanvasGroup cg = panel.GetComponent<CanvasGroup>() ?? panel.gameObject.AddComponent<CanvasGroup>();
                startAlphas.Add(cg.alpha);
                targetAlphas.Add(1f);
            }

            float elapsed = 0f;
            while (elapsed < _animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _animationDuration);
                float tEase = Mathf.SmoothStep(0f, 1f, t);

                for (int i = 0; i < count; i++)
                {
                    var panel = _panelsList[i];
                    if (panel != null)
                    {
                        var rect = panel.GetComponent<RectTransform>();
                        rect.anchoredPosition = Vector2.Lerp(startPositions[i], targetPositions[i], tEase);
                        panel.SetScale(Mathf.Lerp(startScales[i], targetScales[i], tEase));
                        panel.SetAlpha(Mathf.Lerp(startAlphas[i], targetAlphas[i], tEase));
                    }
                }
                yield return null;
            }

            ForceApplyTargets();
            UpdateContainerSize();
            _activeAnimationCoroutine = null;
        }

        /// <summary>
        /// 특정 캐릭터의 체력 바와 텍스트를 실시간으로 갱신합니다.
        /// </summary>
        public void UpdateCharacterHp(string id, float currentHp, float maxHp)
        {
            if (_activePanels.TryGetValue(id, out var panelUI))
            {
                panelUI.UpdateHp(currentHp, maxHp);
            }
        }

        /// <summary>
        /// 캐릭터가 사망하면, UI에서 페이드아웃 효과와 함께 패널을 서서히 지우고 아래의 패널들을 위로 당겨 정렬시킵니다.
        /// </summary>
        public void RemoveCharacter(string id)
        {
            if (!_activePanels.ContainsKey(id)) return;

            // 실행 중인 애니메이션이 있으면 즉시 종료하고 최종 강제 정렬
            if (_activeAnimationCoroutine != null)
            {
                StopCoroutine(_activeAnimationCoroutine);
                ForceApplyTargets(); // 중요: 중단 즉시 이전 상태의 알파 및 위치 최종 강제 적용
            }

            _activeAnimationCoroutine = StartCoroutine(RemoveCharacterCoroutine(id));
        }

        private IEnumerator RemoveCharacterCoroutine(string id)
        {
            if (_activePanels.TryGetValue(id, out var panelUI))
            {
                _activePanels.Remove(id);
                _panelsList.Remove(panelUI);

                RectTransform targetRect = panelUI.GetComponent<RectTransform>();
                Vector2 startPos = targetRect.anchoredPosition;
                Vector2 endPos = startPos + Vector2.left * _slideDistance;
                float startScale = targetRect.localScale.x;

                // 나머지 패널들이 올라올 시작 좌표/크기와 목표 좌표/크기 수집
                List<Vector2> startPositions = new List<Vector2>();
                List<Vector2> targetPositions = new List<Vector2>();
                List<float> startScales = new List<float>();
                List<float> targetScales = new List<float>();

                for (int i = 0; i < _panelsList.Count; i++)
                {
                    startPositions.Add(_panelsList[i].GetComponent<RectTransform>().anchoredPosition);
                    targetPositions.Add(new Vector2(0f, GetTargetY(i)));
                    startScales.Add(_panelsList[i].transform.localScale.x);
                    targetScales.Add(GetTargetScale(i));
                }

                float elapsed = 0f;
                while (elapsed < _animationDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / _animationDuration);
                    float tEase = Mathf.SmoothStep(0f, 1f, t);

                    // 타겟 캐릭터 페이드아웃 및 튕겨나가기
                    if (panelUI != null)
                    {
                        targetRect.anchoredPosition = Vector2.Lerp(startPos, endPos, tEase);
                        panelUI.SetScale(Mathf.Lerp(startScale, 0.85f, tEase));
                        panelUI.SetAlpha(1f - t);
                    }

                    // 나머지 살아있는 패널들 끌어올리기 및 크기 변환 (0번 자리에 서는 녀석은 100%로 커짐)
                    for (int i = 0; i < _panelsList.Count; i++)
                    {
                        if (_panelsList[i] != null)
                        {
                            _panelsList[i].GetComponent<RectTransform>().anchoredPosition = Vector2.Lerp(startPositions[i], targetPositions[i], tEase);
                            _panelsList[i].SetScale(Mathf.Lerp(startScales[i], targetScales[i], tEase));
                        }
                    }
                    yield return null;
                }

                if (panelUI != null)
                {
                    Destroy(panelUI.gameObject);
                }

                // 타겟 정합성 강제 맞춤
                ForceApplyTargets();
                UpdateContainerSize();
            }
            _activeAnimationCoroutine = null;
        }

        /// <summary>
        /// 캐릭터가 부활하면, 알맞은 이니셔티브 삽입 위치에 패널을 생성(Fade-In + Slide-Up)하고 하위 유닛들을 아래로 밀어냅니다.
        /// </summary>
        public void AddCharacter(ICharacterBattleInfo character)
        {
            if (character == null) return;

            // 실행 중인 애니메이션이 있으면 즉시 종료하고 최종 강제 정렬
            if (_activeAnimationCoroutine != null)
            {
                StopCoroutine(_activeAnimationCoroutine);
                ForceApplyTargets(); // 중요: 중단 즉시 이전 상태의 알파 및 위치 최종 강제 적용
            }

            // 이미 동일한 ID의 패널이 존재한다면 갱신만 처리하고 종료
            if (_activePanels.TryGetValue(character.Id, out var existingPanel))
            {
                existingPanel.Initialize(character);
                ForceApplyTargets();
                return;
            }

            if (_characterPanelPrefab == null)
            {
                Debug.LogWarning("[InitiativeManager] Character Panel Prefab이 할당되지 않았습니다.");
                return;
            }

            // 정렬 삽입 인덱스 계산 (내림차순 기준)
            int insertIndex = _panelsList.Count;
            for (int i = 0; i < _panelsList.Count; i++)
            {
                if (character.Initiative > _panelsList[i].Initiative)
                {
                    insertIndex = i;
                    break;
                }
            }

            GameObject panelObj = Instantiate(_characterPanelPrefab, transform);
            CharacterPanelUI newPanelUI = panelObj.GetComponent<CharacterPanelUI>();

            if (newPanelUI != null)
            {
                newPanelUI.Initialize(character);
                RectTransform rect = newPanelUI.GetComponent<RectTransform>();
                SetupRectTransform(rect);

                _activePanels[character.Id] = newPanelUI;
                _panelsList.Insert(insertIndex, newPanelUI);

                _activeAnimationCoroutine = StartCoroutine(AddCharacterCoroutine(newPanelUI, insertIndex));
            }
            else
            {
                Debug.LogWarning("[InitiativeManager] 생성된 프리팹에 CharacterPanelUI 컴포넌트가 없습니다.");
                Destroy(panelObj);
            }
        }

        private IEnumerator AddCharacterCoroutine(CharacterPanelUI newPanelUI, int insertIndex)
        {
            RectTransform newRect = newPanelUI.GetComponent<RectTransform>();
            float targetY = GetTargetY(insertIndex);
            float targetScale = GetTargetScale(insertIndex);
            
            // 초기 위치는 타겟 위치보다 50px 아래에 투명하게 설정
            newRect.anchoredPosition = new Vector2(0f, targetY - 50f);
            newPanelUI.SetScale(targetScale);
            newPanelUI.SetAlpha(0f);

            // 다른 패널들의 이동할 위치와 스케일 수집 (이로 인해 0번에서 1번으로 밀리는 패널은 축소 처리됨)
            List<Vector2> startPositions = new List<Vector2>();
            List<Vector2> targetPositions = new List<Vector2>();
            List<float> startScales = new List<float>();
            List<float> targetScales = new List<float>();

            for (int i = 0; i < _panelsList.Count; i++)
            {
                startPositions.Add(_panelsList[i].GetComponent<RectTransform>().anchoredPosition);
                targetPositions.Add(new Vector2(0f, GetTargetY(i)));
                
                // 새로 추가된 패널 제외하고 이전 프레임의 크기를 시작점으로 지정
                if (_panelsList[i] == newPanelUI)
                {
                    startScales.Add(targetScale);
                }
                else
                {
                    startScales.Add(_panelsList[i].transform.localScale.x);
                }
                targetScales.Add(GetTargetScale(i));
            }

            float elapsed = 0f;
            while (elapsed < _animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _animationDuration);
                float tEase = Mathf.SmoothStep(0f, 1f, t);

                // 부활 패널 서서히 페이드인 및 올라옴
                if (newPanelUI != null)
                {
                    newRect.anchoredPosition = Vector2.Lerp(new Vector2(0f, targetY - 50f), new Vector2(0f, targetY), tEase);
                    newPanelUI.SetAlpha(t);
                }

                // 다른 패널들 밀려나며 위치 및 스케일 조정 (0번에서 밀려나는 패널은 축소)
                for (int i = 0; i < _panelsList.Count; i++)
                {
                    if (_panelsList[i] != null && _panelsList[i] != newPanelUI)
                    {
                        _panelsList[i].GetComponent<RectTransform>().anchoredPosition = Vector2.Lerp(startPositions[i], targetPositions[i], tEase);
                        _panelsList[i].SetScale(Mathf.Lerp(startScales[i], targetScales[i], tEase));
                    }
                }
                yield return null;
            }

            ForceApplyTargets();
            UpdateContainerSize();
            _activeAnimationCoroutine = null;
        }

        /// <summary>
        /// 캐릭터 턴이 교체될 때 호출되어 맨 위 패널은 축소되며 맨 뒤로 가고, 새로 맨 위가 된 1번 패널은 100%로 커집니다.
        /// </summary>
        public void NextTurn()
        {
            if (_panelsList.Count <= 1) return;

            // 실행 중인 애니메이션이 있으면 즉시 종료하고 최종 강제 정렬
            if (_activeAnimationCoroutine != null)
            {
                StopCoroutine(_activeAnimationCoroutine);
                ForceApplyTargets(); // 중요: 중단 즉시 이전 상태의 알파 및 위치 최종 강제 적용
            }

            _activeAnimationCoroutine = StartCoroutine(NextTurnCoroutine());
        }

        private IEnumerator NextTurnCoroutine()
        {
            CharacterPanelUI activePanel = _panelsList[0];
            _panelsList.RemoveAt(0);
            _panelsList.Add(activePanel);

            RectTransform activeRect = activePanel.GetComponent<RectTransform>();
            Vector2 activeStartPos = activeRect.anchoredPosition;
            Vector2 activeEndPos = activeStartPos + Vector2.left * _slideDistance;

            // 나머지 위로 올라갈 패널들의 시작/목표 좌표 및 스케일 수집 (맨 뒤로 간 activePanel 제외)
            List<Vector2> startPositions = new List<Vector2>();
            List<Vector2> targetPositions = new List<Vector2>();
            List<float> startScales = new List<float>();
            List<float> targetScales = new List<float>();

            for (int i = 0; i < _panelsList.Count - 1; i++)
            {
                startPositions.Add(_panelsList[i].GetComponent<RectTransform>().anchoredPosition);
                targetPositions.Add(new Vector2(0f, GetTargetY(i)));
                
                startScales.Add(_panelsList[i].transform.localScale.x);
                targetScales.Add(GetTargetScale(i)); // 이 과정에서 index 0이 되는 녀석은 100% 타겟이 잡힘
            }

            // 1단계: 맨 위 패널은 왼쪽으로 날아가며 Fade-out 및 스케일 축소, 
            // 뒤이어 맨 위(0번)로 올라가는 유닛은 85% -> 100%로 확대되며 슬라이드 업
            float elapsed = 0f;
            while (elapsed < _animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _animationDuration);
                float tEase = Mathf.SmoothStep(0f, 1f, t);

                if (activePanel != null)
                {
                    activeRect.anchoredPosition = Vector2.Lerp(activeStartPos, activeEndPos, tEase);
                    activePanel.SetScale(Mathf.Lerp(1.0f, 0.85f, tEase));
                    activePanel.SetAlpha(1f - t);
                }

                for (int i = 0; i < _panelsList.Count - 1; i++)
                {
                    if (_panelsList[i] != null)
                    {
                        _panelsList[i].GetComponent<RectTransform>().anchoredPosition = Vector2.Lerp(startPositions[i], targetPositions[i], tEase);
                        _panelsList[i].SetScale(Mathf.Lerp(startScales[i], targetScales[i], tEase));
                    }
                }
                yield return null;
            }

            // 2단계: activePanel의 위치를 맨 아래 위치에서 50px 더 밑으로 보낸 뒤 알파 0%로 세팅
            float lastTargetY = GetTargetY(_panelsList.Count - 1);
            Vector2 lastStartPos = new Vector2(0f, lastTargetY - 50f);
            Vector2 lastEndPos = new Vector2(0f, lastTargetY);

            if (activePanel != null)
            {
                activeRect.anchoredPosition = lastStartPos;
                activePanel.SetScale(0.85f);
                activePanel.SetAlpha(0f);
            }

            // 3단계: 맨 아래로 들어오는 유닛의 슬라이드 업 및 페이드인 처리
            elapsed = 0f;
            while (elapsed < _animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _animationDuration);
                float tEase = Mathf.SmoothStep(0f, 1f, t);

                if (activePanel != null)
                {
                    activeRect.anchoredPosition = Vector2.Lerp(lastStartPos, lastEndPos, tEase);
                    activePanel.SetAlpha(t);
                }
                yield return null;
            }

            ForceApplyTargets();
            _activeAnimationCoroutine = null;
        }

        /// <summary>
        /// 패널의 RectTransform의 Anchor, Pivot 속성을 수동 좌표 정렬에 맞춰 변경합니다. (0, 1) 상단 왼쪽 정렬 기준
        /// </summary>
        private void SetupRectTransform(RectTransform rect)
        {
            if (rect == null) return;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
        }

        /// <summary>
        /// 활성화된 유닛 수와 유닛들의 동적 축소 비율을 고려하여 부모 스크롤 콘텐츠 컨테이너 영역의 높이를 동적으로 갱신합니다.
        /// </summary>
        private void UpdateContainerSize()
        {
            if (_rectTransform == null) return;

            int activeCount = _panelsList.Count;
            if (activeCount <= 0)
            {
                _rectTransform.sizeDelta = new Vector2(_rectTransform.sizeDelta.x, 0f);
                return;
            }

            // 마지막 유닛의 Y 좌표에서 유닛의 높이(마지막 유닛은 85% 크기이므로 _panelHeight * 0.85f) 및 여백을 반영하여 총 높이 계산
            float lastUnitHeight = (activeCount == 1) ? _panelHeight : (_panelHeight * 0.85f);
            float totalHeight = -GetTargetY(activeCount - 1) + lastUnitHeight + _spacing;
            
            _rectTransform.sizeDelta = new Vector2(_rectTransform.sizeDelta.x, totalHeight);
        }

        /// <summary>
        /// 모든 유닛들의 렉트 트랜스폼 좌표, 스케일 및 투명도를 타겟 값에 정확히 물리적으로 매칭시킵니다.
        /// </summary>
        private void ForceApplyTargets()
        {
            for (int i = 0; i < _panelsList.Count; i++)
            {
                if (_panelsList[i] != null)
                {
                    _panelsList[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, GetTargetY(i));
                    _panelsList[i].SetScale(GetTargetScale(i));
                    _panelsList[i].SetAlpha(1f);
                }
            }
        }

        /// <summary>
        /// 파괴 연출 및 리스트 청소
        /// </summary>
        private void ClearPanels()
        {
            foreach (var panel in _panelsList)
            {
                if (panel != null && panel.gameObject != null)
                {
                    Destroy(panel.gameObject);
                }
            }
            _activePanels.Clear();
            _panelsList.Clear();

            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }
    }
}