using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KD
{
    // 스킬 사용 흐름 조율자
    // TacticalBattleManager → StartUseSkill() 호출
    //   → SimpleSkillFxPlayer.PlaySkillFx() 코루틴 실행
    //   → onImpact 콜백에서 SkillExecutor로 데미지/효과 적용
    //   → onComplete 콜백으로 TacticalBattleManager에 완료 알림
    //
    // IsRunning이 true인 동안 TacticalBattleManager는 새 입력을 차단해야 한다.
    public class SkillActionRunner : MonoBehaviour
    {
        [SerializeField] private SimpleSkillFxPlayer fxPlayer;

        public bool IsRunning { get; private set; }

        // TacticalBattleManager에서 호출 — 코루틴을 내부에서 시작하므로 void 반환
        public void StartUseSkill(
            BattleUnit       caster,
            List<BattleUnit> targets,
            List<Vector2Int> targetTiles,
            SkillData        skill,
            Action           onComplete)
        {
            if (IsRunning)
            {
                Debug.LogWarning("[SkillActionRunner] 이미 실행 중입니다.");
                return;
            }
            StartCoroutine(UseSkillCoroutine(caster, targets, targetTiles, skill, onComplete));
        }

        private IEnumerator UseSkillCoroutine(
            BattleUnit       caster,
            List<BattleUnit> targets,
            List<Vector2Int> targetTiles,
            SkillData        skill,
            Action           onComplete)
        {
            IsRunning = true;

            Action onImpact = () => ApplySkillEffect(caster, targets, skill);

            yield return fxPlayer.PlaySkillFx(caster, targets, targetTiles, skill, onImpact);

            IsRunning = false;
            onComplete?.Invoke();
        }

        private static void ApplySkillEffect(
            BattleUnit       caster,
            List<BattleUnit> targets,
            SkillData        skill)
        {
            if (targets == null || targets.Count == 0) return;

            if (targets.Count == 1)
                SkillExecutor.Execute(caster, targets[0], skill);
            else
                SkillExecutor.ExecuteArea(caster, targets, skill);
        }
    }
}
