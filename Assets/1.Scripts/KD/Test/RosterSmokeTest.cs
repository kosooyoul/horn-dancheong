using System.Collections.Generic;
using UnityEngine;

namespace KD
{
    // 로스터 → 전투 변환 흐름 스모크 테스트
    //
    // 확인 항목:
    //   1. 유닛이 보유 목록에 들어가는가?
    //   2. 무기 타입에 맞는 교체 스킬 후보가 나오는가?
    //   3. 교체 스킬 장착이 되는가?
    //   4. 파티에 추가되는가?
    //   5. BattleUnit으로 변환 시 값이 정확한가?
    public class RosterSmokeTest : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerRosterManager rosterManager;
        [SerializeField] private SkillDatabase       skillDatabase;

        [Header("Test Data")]
        [SerializeField] private UnitData          testUnitData;
        [SerializeField] private List<Vector2Int>  testStartPositions = new List<Vector2Int>
        {
            new Vector2Int(1, 1)
        };

        private void Start()
        {
            Debug.Log("=== Roster Smoke Test Started ===");

            // 1. 유닛 획득 → 보유 목록 추가
            OwnedUnit owned = rosterManager.AddUnit(testUnitData);
            Debug.Log($"[1] 보유 유닛 추가: {owned.unitData.unitName} / 무기: {owned.unitData.weaponType}");
            Debug.Log($"    보유 총 수: {rosterManager.OwnedUnits.Count}");

            // 2. 무기 타입 기반 교체 스킬 후보 조회
            List<SkillData> choices = skillDatabase.GetSkillsFor(owned);
            Debug.Log($"[2] 장착 가능 스킬 수: {choices.Count}");
            foreach (SkillData skill in choices)
                Debug.Log($"    후보: {skill.skillName} (apCost={skill.apCost})");

            // 3. 교체 스킬 장착 (후보가 있을 때만)
            if (choices.Count > 0)
            {
                bool equipped = owned.EquipOptionalSkill(choices[0], skillDatabase);
                Debug.Log($"[3] 장착 결과: {equipped} / 장착된 스킬: {owned.equippedOptionalSkill?.skillName}");
            }
            else
            {
                Debug.LogWarning("[3] 장착 가능한 스킬이 없습니다. SkillDatabase를 확인하세요.");
            }

            // 4. 파티에 추가
            bool selected = rosterManager.SelectPartyUnit(owned);
            Debug.Log($"[4] 파티 추가 결과: {selected} / 파티 수: {rosterManager.SelectedParty.Count}");

            // 5. BattleUnit 변환
            var battleSetup = new BattleSetup();
            List<BattleUnit> battleUnits = battleSetup.CreatePlayerBattleUnits(
                rosterManager.SelectedParty, testStartPositions);

            Debug.Log($"[5] 변환된 BattleUnit 수: {battleUnits.Count}");
            foreach (BattleUnit unit in battleUnits)
            {
                Debug.Log($"    이름:     {unit.Data.unitName}");
                Debug.Log($"    팀:       {unit.TeamId}");
                Debug.Log($"    HP:       {unit.CurrentHP}/{unit.Stats.maxHP}");
                Debug.Log($"    AP:       {unit.CurrentAP}/{unit.MaxAP}");
                Debug.Log($"    위치:     {unit.CurrentTilePos}");
                Debug.Log($"    교체스킬: {unit.EquippedOptionalSkill?.skillName ?? "없음"}");
            }

            Debug.Log("=== Roster Smoke Test Done ===");
        }
    }
}
