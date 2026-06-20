using UnityEngine;
using TMPro;

namespace KD
{
    /// <summary>
    /// 배치 현황(배치 수 / 최대 수)을 표시하는 UI.
    /// KDBattleTurnController.OnDeploymentChanged 이벤트를 구독한다.
    ///
    /// Inspector:
    ///   statusText     - TextMeshProUGUI  예) "2 / 4"
    ///   turnController - KDBattleTurnController
    ///   battleManager  - TacticalBattleManager
    /// </summary>
    public class DeploymentStatusUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI        statusText;
        [SerializeField] private KDBattleTurnController turnController;
        [SerializeField] private TacticalBattleManager  battleManager;

        private void Start()
        {
            if (turnController != null)
                turnController.OnDeploymentChanged += RefreshDisplay;

            RefreshDisplay();
        }

        private void OnDestroy()
        {
            if (turnController != null)
                turnController.OnDeploymentChanged -= RefreshDisplay;
        }

        private void RefreshDisplay()
        {
            if (statusText == null || battleManager == null) return;

            int placed = battleManager.CurrentPlacements.Count;
            int max    = battleManager.MaxDeployCount;

            statusText.text = max > 0 ? $"{placed} / {max}" : $"{placed}";
        }
    }
}
