using System.Collections.Generic;

namespace HornDancheong.Seongwoo.UI
{
    public interface IInitiativeUI
    {
        /// <summary>
        /// 전투 시작 시 초기 UI 리스트 생성
        /// </summary>
        void InitializeBattleUI(IEnumerable<ICharacterBattleInfo> characters);

        /// <summary>
        /// 전투 시작 시 초기 UI 리스트 생성 (정렬 여부 선택 가능)
        /// </summary>
        void InitializeBattleUI(IEnumerable<ICharacterBattleInfo> characters, bool sortByInitiative);

        /// <summary>
        /// 특정 캐릭터의 체력이 변했을 때 실시간 갱신
        /// </summary>
        void UpdateCharacterHp(string id, float currentHp, float maxHp);

        /// <summary>
        /// 캐릭터가 사망했을 때 UI 패널을 리스트에서 제거
        /// </summary>
        void RemoveCharacter(string id);

        /// <summary>
        /// 캐릭터를 이니셔티브 트랙에 신규 추가(부활 등)하고 우선권 순위에 맞게 정렬 삽입합니다.
        /// </summary>
        void AddCharacter(ICharacterBattleInfo character);

        /// <summary>
        /// 턴 교체 시 맨 위에 있는 패널을 맨 아래로 순환시킴
        /// </summary>
        void NextTurn();

        /// <summary>
        /// 틱 게이지 등 동적으로 변한 턴 순서에 맞춰 UI 패널들의 위치를 갱신합니다.
        /// </summary>
        void UpdateTurnOrder(IEnumerable<ICharacterBattleInfo> characters);

        /// <summary>
        /// 현재 이니셔티브 트랙 UI 애니메이션이 진행 중인지 여부를 반환합니다.
        /// </summary>
        bool IsAnimating { get; }
    }
}

