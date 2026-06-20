using UnityEngine;

namespace HornDancheong.Seongwoo.UI
{
    /// <summary>
    /// 전투 조작 UI에 필요한 확장된 캐릭터 정보 인터페이스입니다.
    /// </summary>
    public interface ICombatCharacterInfo : ICharacterBattleInfo
    {
        /// <summary>
        /// 현재 기력(SP)
        /// </summary>
        float CurrentSp { get; }

        /// <summary>
        /// 최대 기력(SP)
        /// </summary>
        float MaxSp { get; }

        /// <summary>
        /// 클래스(직업) 이름
        /// </summary>
        string ClassName { get; }

        /// <summary>
        /// 클래스(직업) 아이콘 이미지
        /// </summary>
        Sprite ClassIcon { get; }

        /// <summary>
        /// 속성 이름
        /// </summary>
        string AttributeName { get; }

        /// <summary>
        /// 속성 아이콘 이미지
        /// </summary>
        Sprite AttributeIcon { get; }

        /// <summary>
        /// 활성화된 버프 및 디버프 아이콘 목록
        /// </summary>
        Sprite[] ActiveBuffDebuffs { get; }
    }
}
