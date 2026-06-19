using System.Collections.Generic;

namespace KD
{
    // 전투 시작·라운드 시작 시 행동 순서 결정
    // initiative(= agility*10 + luck) 높을수록 먼저 행동
    public static class TurnOrderManager
    {
        // allUnits: 플레이어 + 적 전체 목록
        // 사망한 유닛은 제외하고 initiative 내림차순 정렬
        public static List<BattleUnit> BuildTurnOrder(IReadOnlyList<BattleUnit> allUnits)
        {
            var order = new List<BattleUnit>();

            foreach (BattleUnit unit in allUnits)
            {
                if (unit != null && !unit.IsDead)
                    order.Add(unit);
            }

            // initiative 높을수록 먼저, 동률이면 플레이어(TeamId 0) 우선
            order.Sort((a, b) =>
            {
                int cmp = b.Stats.initiative.CompareTo(a.Stats.initiative);
                if (cmp != 0) return cmp;
                return a.TeamId.CompareTo(b.TeamId);
            });
            return order;
        }
    }
}
