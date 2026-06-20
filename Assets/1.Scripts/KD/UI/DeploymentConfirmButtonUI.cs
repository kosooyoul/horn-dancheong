using UnityEngine;
using UnityEngine.UI;

namespace KD
{
    /// <summary>
    /// 배치 확정 버튼.
    /// TacticalBattleManager.IsDeploymentReady에 따라 버튼 활성/비활성을 매 프레임 갱신한다.
    ///
    /// Inspector:
    ///   confirmButton  - 클릭할 Button 컴포넌트
    ///   battleManager  - TacticalBattleManager
    /// </summary>
    public class DeploymentConfirmButtonUI : MonoBehaviour
    {
        [SerializeField] private Button                 confirmButton;
        [SerializeField] private TacticalBattleManager  battleManager;

        private void Start()
        {
            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        private void Update()
        {
            if (confirmButton == null || battleManager == null) return;
            if (battleManager.CurrentPhase != BattlePhase.Deployment) return;

            confirmButton.interactable = battleManager.IsDeploymentReady;
        }

        private void OnConfirmClicked()
        {
            battleManager?.ConfirmDeployment();
        }
    }
}
