using System.Collections.Generic;

namespace HornDancheong.Seongwoo.UI
{
    /// <summary>
    /// 전투 UI 조작과 실제 전투 로직(컨트롤러)을 연결하는 인터페이스입니다.
    /// </summary>
    public interface ICombatInteractionController
    {
        /// <summary>
        /// 현재 이동 모드가 활성화되어 있는지 여부
        /// </summary>
        bool IsMoveModeActive { get; }

        /// <summary>
        /// 이동 모드 상태를 설정합니다.
        /// </summary>
        void SetMoveMode(bool active);

        /// <summary>
        /// 지정한 스킬 ID의 스킬을 실행합니다.
        /// </summary>
        void ExecuteSkill(int skillId);

        /// <summary>
        /// 대기(턴 종료)를 실행합니다.
        /// </summary>
        void ExecuteWait();
    }
}
