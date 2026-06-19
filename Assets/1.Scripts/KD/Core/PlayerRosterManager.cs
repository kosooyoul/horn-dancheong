using System.Collections.Generic;
using UnityEngine;

namespace KD
{
    // 플레이어 보유 유닛 목록과 전투 파티 관리
    // 게임 시작 시 씬에 하나만 존재
    public class PlayerRosterManager : MonoBehaviour
    {
        [Header("현재 보유 유닛")]
        [SerializeField] private List<OwnedUnit> ownedUnits = new List<OwnedUnit>();

        [Header("전투에 참가할 파티")]
        [SerializeField] private List<OwnedUnit> selectedParty = new List<OwnedUnit>();

        public IReadOnlyList<OwnedUnit> OwnedUnits    => ownedUnits;
        public IReadOnlyList<OwnedUnit> SelectedParty => selectedParty;

        // 새 유닛 획득 (보상, 가챠, 이벤트 등)
        public OwnedUnit AddUnit(UnitData unitData)
        {
            if (unitData == null) return null;

            OwnedUnit newUnit = new OwnedUnit(unitData);
            ownedUnits.Add(newUnit);
            return newUnit;
        }

        // 전투 파티에 유닛 추가
        public bool SelectPartyUnit(OwnedUnit unit)
        {
            if (unit == null)                    return false;
            if (!ownedUnits.Contains(unit))      return false;
            if (selectedParty.Contains(unit))    return false;

            selectedParty.Add(unit);
            return true;
        }

        public bool DeselectPartyUnit(OwnedUnit unit)
        {
            return selectedParty.Remove(unit);
        }

        public void ClearParty()
        {
            selectedParty.Clear();
        }
    }
}
