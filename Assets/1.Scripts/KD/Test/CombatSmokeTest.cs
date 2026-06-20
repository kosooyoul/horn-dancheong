using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KD
{
    // 스모크 테스트 — 실제 타일 오브젝트 없이 좌표만으로 핵심 흐름 검증
    //
    // 키보드 입력:
    //   1 : 마우스 위쪽 방향으로 스킬 범위 계산
    //   2 : 마우스 오른쪽 방향으로 스킬 범위 계산
    //   3 : 현재 범위 안에 적이 있으면 스킬 실행
    //   4 : 이동 가능한 첫 번째 타일로 이동
    //   5 : 대기 (AP 회복)
    public class CombatSmokeTest : MonoBehaviour
    {
        [Header("Test Data")]
        [SerializeField] private UnitData playerData;
        [SerializeField] private UnitData enemyData;
        [SerializeField] private SkillData testSkill;

        [Header("Test Positions")]
        [SerializeField] private Vector2Int playerStart = new Vector2Int(3, 3);
        [SerializeField] private Vector2Int enemyStart  = new Vector2Int(3, 5);
        [SerializeField] private Vector2Int mouseTile   = new Vector2Int(3, 6);

        [Header("Grid Setting")]
        [SerializeField] private int width  = 7;
        [SerializeField] private int height = 7;
        [SerializeField] private List<Vector2Int> wallTiles = new List<Vector2Int>();

        private BattleUnit player;
        private BattleUnit enemy;
        private CombatGridQuery query;

        private List<Vector2Int> currentSkillTiles  = new List<Vector2Int>();
        private List<MoveOption> currentMoveOptions = new List<MoveOption>();

        private void Start()
        {
            // BattleUnit 생성 — teamId: 0=플레이어, 1=적
            player = new BattleUnit(playerData, teamId: 0, playerStart);
            enemy  = new BattleUnit(enemyData,  teamId: 1, enemyStart);

            // AP는 전투 시작 시에만 리셋 (차례마다 리셋하면 Wait 의미 없어짐)
            player.ResetAP();
            enemy.ResetAP();

            // 이동 블로킹(벽+유닛)과 스킬 Ray 블로킹(벽만) 분리
            query = new CombatGridQuery(
                isValidTile:          IsValidTile,
                isBlockedForMove:     IsBlockedForMove,
                isBlockedForSkillRay: IsBlockedForSkillRay
            );

            Debug.Log("=== Combat Smoke Test Started ===");
            Debug.Log($"Player AP: {player.CurrentAP}");
            Debug.Log($"Enemy HP:  {enemy.CurrentHP}");

            TestSkillRange();
            TestMoveOptions();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.digit1Key.wasPressedThisFrame)
            {
                mouseTile = new Vector2Int(3, 6); // 위쪽
                TestSkillRange();
            }
            if (kb.digit2Key.wasPressedThisFrame)
            {
                mouseTile = new Vector2Int(6, 3); // 오른쪽
                TestSkillRange();
            }
            if (kb.digit3Key.wasPressedThisFrame) TestUseSkill();
            if (kb.digit4Key.wasPressedThisFrame) TestMove();
            if (kb.digit5Key.wasPressedThisFrame) TestWait();
        }

        // ── 테스트 함수 ──────────────────────────────────────────────────

        private void TestSkillRange()
{
    Debug.Log($"Skill Null? {testSkill == null}");

    if (testSkill != null)
    {
        Debug.Log($"Skill: {testSkill.skillName}");
        Debug.Log($"Pattern Null? {testSkill.targetPattern == null}");

        if (testSkill.targetPattern != null)
        {
            Debug.Log($"Pattern: {testSkill.targetPattern.patternName}");
            Debug.Log($"Fixed Count: {testSkill.targetPattern.fixedCells.Count}");
            Debug.Log($"Ray Count: {testSkill.targetPattern.rays.Count}");
            Debug.Log($"DirectionMode: {testSkill.targetPattern.directionMode}");
            Debug.Log($"DirectionSet: {testSkill.targetPattern.directionSet}");
        }
    }
    if (testSkill != null && testSkill.targetPattern != null)
{
    for (int i = 0; i < testSkill.targetPattern.rays.Count; i++)
    {
        PatternRay ray = testSkill.targetPattern.rays[i];

        Debug.Log(
            $"Ray {i}: " +
            $"start={ray.startLocalOffset}, " +
            $"dir={ray.localDirection}, " +
            $"min={ray.minDistance}, " +
            $"max={ray.maxDistance}, " +
            $"stop={ray.stopOnBlocked}"
        );
    }
}

    currentSkillTiles = query.GetSkillRange(
        caster: player,
        skill: testSkill,
        mouseTile: mouseTile
    );

    Debug.Log($"[Skill Range] MouseTile={mouseTile}");
    Debug.Log($"Skill Tile Count: {currentSkillTiles.Count}");

    foreach (Vector2Int tile in currentSkillTiles)
    {
        Debug.Log($"Skill Tile: {tile}");
    }

    bool enemyInRange = currentSkillTiles.Contains(enemy.CurrentTilePos);
    Debug.Log($"Enemy Pos: {enemy.CurrentTilePos}");
    Debug.Log($"Enemy In Range: {enemyInRange}");
}

        private void TestUseSkill()
        {
            if (!currentSkillTiles.Contains(enemy.CurrentTilePos))
            {
                Debug.LogWarning("[Skill] 적이 현재 스킬 범위 안에 없습니다. 먼저 1번으로 범위를 계산하세요.");
                return;
            }

            bool success = SkillExecutor.Execute(player, enemy, testSkill);

            Debug.Log($"[Skill] Execute: {success}");
            Debug.Log($"  Player AP: {player.CurrentAP}");
            Debug.Log($"  Enemy HP:  {enemy.CurrentHP}");
        }

        private void TestMoveOptions()
        {
            currentMoveOptions = query.GetMoveOptions(player);

            Debug.Log("[Move Options]");
            foreach (MoveOption opt in currentMoveOptions)
                Debug.Log($"  {opt.tilePos}  dist={opt.distance}  apCost={opt.apCost}");
        }

        private void TestMove()
        {
            if (currentMoveOptions == null || currentMoveOptions.Count == 0)
            {
                Debug.LogWarning("[Move] 이동 가능한 타일이 없습니다.");
                return;
            }

            MoveOption move = currentMoveOptions[0];

            if (!player.TrySpendAP(move.apCost))
                return;

            player.MoveTo(move.tilePos);

            Debug.Log($"[Move] → {move.tilePos}");
            Debug.Log($"  Player AP:  {player.CurrentAP}");
            Debug.Log($"  Player Pos: {player.CurrentTilePos}");

            TestMoveOptions(); // 이동 후 새 범위 재계산
        }

        private void TestWait()
        {
            player.Wait();
            Debug.Log($"[Wait] Player AP: {player.CurrentAP}/{player.MaxAP}");
        }

        // ── 그리드 판단 함수 ─────────────────────────────────────────────

        private bool IsValidTile(Vector2Int pos)
            => pos.x >= 0 && pos.y >= 0 && pos.x < width && pos.y < height;

        // 이동 블로킹: 벽 + 유닛 점유 타일 모두 막음
        private bool IsBlockedForMove(Vector2Int pos)
        {
            if (wallTiles.Contains(pos))             return true;
            if (enemy  != null && enemy.CurrentTilePos  == pos) return true;
            if (player != null && player.CurrentTilePos == pos) return true;
            return false;
        }

        // 스킬 Ray 블로킹: 벽만 막음 (유닛이 있어도 Ray 통과, 적 타일도 범위에 포함)
        private bool IsBlockedForSkillRay(Vector2Int pos)
            => wallTiles.Contains(pos);
    }
}
