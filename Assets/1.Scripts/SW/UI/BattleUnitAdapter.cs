using UnityEngine;
using KD;

namespace HornDancheong.Seongwoo.UI
{
    /// <summary>
    /// KD.BattleUnit 모델을 ICharacterBattleInfo 인터페이스 형식으로 변환하여
    /// 이니셔티브 UI에서 즉시 사용할 수 있도록 매핑해주는 어댑터 클래스입니다.
    /// </summary>
    public class BattleUnitAdapter : ICharacterBattleInfo
    {
        private readonly BattleUnit _unit;
        private readonly bool _isPC;

        public string Id => _unit.Data.unitId;
        public string CharacterName => _unit.Data.unitName;
        
        // 만약 Resources 폴더 경로가 필요하다면 사용 가능하나, 기본적으로 UnitData에 할당된 icon Sprite를 사용하도록 연동합니다.
        public string PortraitPath => string.Empty;
        public Sprite PortraitSprite => _unit.Data.icon;
        
        public float CurrentHp => _unit.CurrentHP;
        public float MaxHp => _unit.Stats.maxHP;
        public bool IsPC => _isPC;
        public int Initiative => _unit.Stats.initiative;

        public BattleUnitAdapter(BattleUnit unit, bool isPC)
        {
            _unit = unit;
            _isPC = isPC;
        }
    }
}
