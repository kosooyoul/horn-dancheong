using UnityEngine;
using TMPro;

namespace KD
{
    /// <summary>
    /// 현재 배치 선택 중인 유닛 이름을 표시하는 UI.
    /// KDBattleTurnController.OnDeployUnitSelected 이벤트를 구독한다.
    ///
    /// Inspector:
    ///   unitNameText   - TextMeshProUGUI
    ///   turnController - KDBattleTurnController
    ///   emptyText      - 아무것도 선택 안 됐을 때 표시할 문자열 (예: "유닛을 선택하세요")
    /// </summary>
    public class DeploymentSelectedUnitUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI     unitNameText;
        [SerializeField] private KDBattleTurnController turnController;
        [SerializeField] private string              emptyText = "유닛을 선택하세요";

        private void Start()
        {
            if (turnController != null)
                turnController.OnDeployUnitSelected += RefreshDisplay;

            RefreshDisplay(turnController?.SelectedDeployUnit);
        }

        private void OnDestroy()
        {
            if (turnController != null)
                turnController.OnDeployUnitSelected -= RefreshDisplay;
        }

        private void RefreshDisplay(OwnedUnit unit)
        {
            if (unitNameText == null) return;
            unitNameText.text = unit?.unitData.unitName ?? emptyText;
        }
    }
}
