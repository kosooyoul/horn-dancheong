using UnityEngine;

namespace KD
{
    // 유닛의 고정 설계도 — Inspector에서 설정, 전투 중 절대 수정 금지
    // 새 유닛 추가: Create > KD > Unit Data
    [CreateAssetMenu(fileName = "UnitData_", menuName = "KD/Unit Data")]
    public class UnitData : ScriptableObject
    {
        [Header("기본 정보")]
        public string unitId;
        public string unitName;
        [TextArea] public string description;
        public Sprite icon;
        public GameObject prefab;

        [Header("직군 & 속성")]
        public UnitRole role;
        public UnitAttribute attribute;

        [Header("이동 방식")]
        [Tooltip("이동 형태를 결정. 이동 거리는 agility 스탯으로 계산됨")]
        public MovementType movementType = MovementType.Cardinal;

        [Header("기본 스탯")]
        public UnitBaseStats baseStats;

        [Header("고유 스킬 (교체 불가)")]
        public SkillData uniqueSkill1;
        public SkillData uniqueSkill2;

        [Header("교체 가능 스킬 후보 목록")]
        [Tooltip("직군에 맞는 스킬만 넣을 것. 전투 시작 전 1개를 선택해 장착")]
        public SkillData[] availableOptionalSkills;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(unitId))
                Debug.LogWarning($"[{name}] unitId가 비어 있습니다.", this);

            if (string.IsNullOrWhiteSpace(unitName))
                Debug.LogWarning($"[{name}] unitName이 비어 있습니다.", this);

            if (prefab == null)
                Debug.LogWarning($"[{name}] prefab이 없습니다.", this);

            if (uniqueSkill1 == null || uniqueSkill2 == null)
                Debug.LogWarning($"[{name}] 고유 스킬이 설정되지 않았습니다.", this);
        }
#endif
    }
}
