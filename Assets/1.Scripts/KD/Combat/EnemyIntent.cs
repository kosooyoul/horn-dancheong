using System.Collections.Generic;
using UnityEngine;

namespace KD
{
    // 적이 이번 턴에 실행할 행동 — 예고 표시 후 실제 실행에 사용
    public class EnemyIntent
    {
        public BattleUnit        caster;
        public SkillData         skill;
        public List<Vector2Int>  warningTiles = new List<Vector2Int>();
    }
}
