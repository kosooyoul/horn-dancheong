using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HornDancheong.Seongwoo.UI
{
    public class InitiativeManager : MonoBehaviour, IInitiativeUI
    {
        [SerializeField] private GameObject _characterPanelPrefab;

        // 생성된 UI 패널들을 고유 ID를 기반으로 추적하고 관리하기 위한 딕셔너리
        private readonly Dictionary<string, CharacterPanelUI> _activePanels = new Dictionary<string, CharacterPanelUI>();

        /// <summary>
        /// 전투 시작 시 캐릭터 리스트를 받아 우선권(Initiative) 높은 순으로 정렬하여 UI 패널들을 생성하고 데이터를 주입합니다.
        /// </summary>
        public void InitializeBattleUI(IEnumerable<ICharacterBattleInfo> characters)
        {
            // 기존 패널들 정리
            ClearPanels();

            if (_characterPanelPrefab == null)
            {
                Debug.LogWarning("[InitiativeManager] Character Panel Prefab이 할당되지 않았습니다.");
                return;
            }

            // 우선권(Initiative) 값이 높은 순서(내림차순)로 정렬합니다.
            var sortedCharacters = characters
                .Where(c => c != null)
                .OrderByDescending(c => c.Initiative);

            foreach (var charInfo in sortedCharacters)
            {
                if (charInfo == null) continue;

                // 이 스크립트가 붙어 있는 레이아웃 그룹 등의 자식으로 프리팹 인스턴스화
                GameObject panelObj = Instantiate(_characterPanelPrefab, transform);
                CharacterPanelUI panelUI = panelObj.GetComponent<CharacterPanelUI>();

                if (panelUI != null)
                {
                    panelUI.Initialize(charInfo);
                    _activePanels[charInfo.Id] = panelUI;
                }
                else
                {
                    Debug.LogWarning("[InitiativeManager] 생성된 프리팹에 CharacterPanelUI 컴포넌트가 없습니다.");
                }
            }
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
        /// 적이 사망하거나 이탈할 때 해당 UI 패널을 파괴하고 관리 대상에서 제외합니다.
        /// </summary>
        public void RemoveCharacter(string id)
        {
            if (_activePanels.TryGetValue(id, out var panelUI))
            {
                _activePanels.Remove(id);
                if (panelUI != null && panelUI.gameObject != null)
                {
                    Destroy(panelUI.gameObject);
                }
            }
        }

        /// <summary>
        /// 캐릭터를 이니셔티브 트랙에 추가(부활 등)하고, 우선권 순위에 맞는 올바른 Sibling Index 위치에 정렬 삽입합니다.
        /// </summary>
        public void AddCharacter(ICharacterBattleInfo character)
        {
            if (character == null) return;

            // 이미 동일한 ID의 패널이 존재한다면 갱신만 처리하고 신규 추가는 생략
            if (_activePanels.TryGetValue(character.Id, out var existingPanel))
            {
                existingPanel.Initialize(character);
                return;
            }

            if (_characterPanelPrefab == null)
            {
                Debug.LogWarning("[InitiativeManager] Character Panel Prefab이 할당되지 않았습니다.");
                return;
            }

            // 신규 패널 생성
            GameObject panelObj = Instantiate(_characterPanelPrefab, transform);
            CharacterPanelUI newPanelUI = panelObj.GetComponent<CharacterPanelUI>();

            if (newPanelUI != null)
            {
                newPanelUI.Initialize(character);
                _activePanels[character.Id] = newPanelUI;

                // 정렬 배치 계산 (우선권 내림차순 기준)
                int targetSiblingIndex = transform.childCount - 1; // 기본은 맨 마지막 배치

                for (int i = 0; i < transform.childCount - 1; i++)
                {
                    Transform child = transform.GetChild(i);
                    CharacterPanelUI childUI = child.GetComponent<CharacterPanelUI>();

                    if (childUI != null)
                    {
                        // 새로 삽입될 캐릭터의 우선권이 탐색 중인 캐릭터의 우선권보다 높다면
                        // 그 위치에 삽입하고 탐색을 중지합니다.
                        if (character.Initiative > childUI.Initiative)
                        {
                            targetSiblingIndex = i;
                            break;
                        }
                    }
                }

                // 계산된 최적의 위치로 순서 지정
                newPanelUI.transform.SetSiblingIndex(targetSiblingIndex);
            }
            else
            {
                Debug.LogWarning("[InitiativeManager] 생성된 프리팹에 CharacterPanelUI 컴포넌트가 없습니다.");
                Destroy(panelObj);
            }
        }

        /// <summary>
        /// 캐릭터 턴이 교체될 때 호출되어, 맨 위에 있는 패널을 맨 아래 순서로 보냅니다.
        /// </summary>
        public void NextTurn()
        {
            if (transform.childCount > 1)
            {
                // UI Hierarchy 상의 첫 번째(맨 위) 자식 트랜스폼을 맨 뒤로 보냅니다.
                // Layout Group(예: Vertical Layout Group)을 사용 중이라면 자동으로 맨 아래로 이동하게 됩니다.
                Transform firstChild = transform.GetChild(0);
                firstChild.SetAsLastSibling();
            }
        }

        /// <summary>
        /// 관리 중인 모든 패널 오브젝트를 삭제하고 딕셔너리를 초기화합니다.
        /// </summary>
        private void ClearPanels()
        {
            foreach (var panel in _activePanels.Values)
            {
                if (panel != null && panel.gameObject != null)
                {
                    Destroy(panel.gameObject);
                }
            }
            _activePanels.Clear();

            // 딕셔너리 정합성 및 혹시 모를 잔여 오브젝트 파괴를 위한 2차 처리
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }
    }
}