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
        public UnitRole    role;
        public UnitAttribute attribute;

        [Header("무기")]
        [Tooltip("무기 종류에 따라 장착 가능한 교체 스킬이 결정됨")]
        public WeaponType weaponType;

        [Header("이동 방식")]
        [Tooltip("이동 형태를 결정. 이동 거리는 agility 스탯으로 계산됨")]
        public MovementType movementType = MovementType.Cardinal;
        [Tooltip("이동 1회 소모 AP (거리 무관, 이동을 선택하면 이 값이 차감됨)")]
        public int moveAPCost = 30;

        [Header("기본 스탯")]
        public UnitBaseStats baseStats;

        [Header("고유 스킬 (교체 불가)")]
        public SkillData uniqueSkill1;
        public SkillData uniqueSkill2;
        public SkillData uniqueSkill3;

        // 교체 가능 스킬 후보 목록은 SkillDatabase에서 weaponType으로 필터링

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(unitId))
                Debug.LogWarning($"[{name}] unitId가 비어 있습니다.", this);

            if (string.IsNullOrWhiteSpace(unitName))
                Debug.LogWarning($"[{name}] unitName이 비어 있습니다.", this);

            if (prefab == null)
                Debug.LogWarning($"[{name}] prefab이 없습니다.", this);

            if (uniqueSkill1 == null || uniqueSkill2 == null || uniqueSkill3 == null)
                Debug.LogWarning($"[{name}] 고유 스킬이 설정되지 않았습니다.", this);
        }
#endif
    }
}
