using System;
using UnityEngine;

namespace KD
{
    // 배치 페이즈에서 확정된 유닛 1명의 배치 정보
    // DeploymentController가 수집 → BattleSetup이 BattleUnit으로 변환
    [Serializable]
    public class DeploymentPlacement
    {
        public OwnedUnit  ownedUnit;
        public Vector2Int tilePos;

        public DeploymentPlacement(OwnedUnit ownedUnit, Vector2Int tilePos)
        {
            this.ownedUnit = ownedUnit;
            this.tilePos   = tilePos;
        }
    }
}
