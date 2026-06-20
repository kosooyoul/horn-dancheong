using System.Collections.Generic;
using UnityEngine;

namespace KD
{
    // 전투 시작 시 OwnedUnit → BattleUnit 변환 담당
    // 전투 담당자는 이 클래스를 통해 플레이어 BattleUnit 목록을 받아 전투에 투입
    public class BattleSetup
    {
        // partyUnits      : PlayerRosterManager.SelectedParty
        // startPositions  : 전투 맵에서 플레이어 시작 타일 목록 (순서 일치)
        public List<BattleUnit> CreatePlayerBattleUnits(
            IReadOnlyList<OwnedUnit> partyUnits,
            List<Vector2Int> startPositions)
        {
            var result = new List<BattleUnit>();

            for (int i = 0; i < partyUnits.Count; i++)
            {
                OwnedUnit owned = partyUnits[i];

                if (owned?.unitData == null)
                {
                    Debug.LogWarning($"[BattleSetup] partyUnits[{i}]의 unitData가 없습니다. 건너뜁니다.");
                    continue;
                }

                Vector2Int startPos = i < startPositions.Count
                    ? startPositions[i]
                    : Vector2Int.zero;

                var battleUnit = new BattleUnit(
                    data:         owned.unitData,
                    teamId:       0,
                    startTilePos: startPos,
                    optionalSkill: owned.equippedOptionalSkill
                );

                result.Add(battleUnit);
            }

            return result;
        }

        // 배치 페이즈 확정 결과(DeploymentPlacement 목록)로 플레이어 BattleUnit 생성
        public List<BattleUnit> CreatePlayerBattleUnits(IReadOnlyList<DeploymentPlacement> placements)
        {
            var result = new List<BattleUnit>();

            foreach (DeploymentPlacement placement in placements)
            {
                if (placement?.ownedUnit?.unitData == null)
                {
                    Debug.LogWarning("[BattleSetup] placement에 유효한 unitData가 없습니다. 건너뜁니다.");
                    continue;
                }

                var battleUnit = new BattleUnit(
                    data:          placement.ownedUnit.unitData,
                    teamId:        0,
                    startTilePos:  placement.tilePos,
                    optionalSkill: placement.ownedUnit.equippedOptionalSkill
                );

                result.Add(battleUnit);
            }

            return result;
        }

        // 적 유닛 1체 생성 (스모크 테스트 등 단일 적이 필요할 때)
        public BattleUnit CreateEnemyBattleUnit(UnitData data, Vector2Int startPos)
        {
            if (data == null)
            {
                Debug.LogWarning("[BattleSetup] CreateEnemyBattleUnit: data가 null입니다.");
                return null;
            }
            return new BattleUnit(data, teamId: 1, startTilePos: startPos);
        }

        // 적 유닛 생성 (UnitData를 직접 받는 단순 버전)
        public List<BattleUnit> CreateEnemyBattleUnits(
            List<UnitData> enemyDataList,
            List<Vector2Int> startPositions)
        {
            var result = new List<BattleUnit>();

            for (int i = 0; i < enemyDataList.Count; i++)
            {
                UnitData data = enemyDataList[i];
                if (data == null) continue;

                Vector2Int startPos = i < startPositions.Count
                    ? startPositions[i]
                    : Vector2Int.zero;

                result.Add(new BattleUnit(data, teamId: 1, startTilePos: startPos));
            }

            return result;
        }
    }
}
