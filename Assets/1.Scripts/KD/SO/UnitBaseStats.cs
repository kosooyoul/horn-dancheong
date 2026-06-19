using System;
using UnityEngine;

namespace KD
{
    // 유닛의 4개 원시 스탯 — Inspector에서 설정
    // StatCalculator가 이 값을 읽어 UnitDerivedStats를 계산
    // struct: ScriptableObject 원본 데이터가 런타임에 실수로 수정되는 것을 방지
    [Serializable]
    public struct UnitBaseStats
    {
        [Range(1, 20)] [Tooltip("행동 순서(initiative), 이동 거리(moveRange), 회피율에 영향")]
        public int agility;     // 민첩

        [Range(1, 20)] [Tooltip("스킬 데미지 및 회복량(skillPower)에 영향")]
        public int spirit;      // 영력

        [Range(1, 20)] [Tooltip("최대 체력(maxHP) 및 받는 피해 감소(defense)에 영향")]
        public int guard;       // 방어 (파생 스탯 defense와 구분하기 위해 guard 사용)

        [Range(1, 20)] [Tooltip("치명타 확률(critChance) 및 회피 확률(evasionChance)에 영향")]
        public int luck;        // 운
    }
}
