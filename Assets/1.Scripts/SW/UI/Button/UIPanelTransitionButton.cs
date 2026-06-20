using UnityEngine;

namespace HornDancheong.Seongwoo.UI
{
    /// <summary>
    /// UIManager와 연동하여 버튼 클릭 시 특정 UI 패널을 닫고 다른 UI 패널을 여는 역할을 수행하는 컴포넌트입니다.
    /// </summary>
    public class UIPanelTransitionButton : ButtonBase
    {
        [Header("Panel Transition Settings")]
        [SerializeField] private UIPanelType _panelToClose = UIPanelType.None;
        [SerializeField] private UIPanelType _panelToOpen = UIPanelType.None;

        [Header("Dialogue Option (Only for Panel_Dialogue)")]
        [SerializeField] private int _dialogueIndex = 1;

        [Header("EscMenu Option (Only for Panel_EscMenu)")]
        [SerializeField] private bool _isOpenedFromMainMenu = false;

        protected override void Function()
        {
            if (UIManager.Instance == null)
            {
                Debug.LogWarning("[UIPanelTransitionButton] UIManager.Instance가 존재하지 않아 패널 전환을 수행할 수 없습니다.");
                return;
            }

            // 1. 패널 닫기
            if (_panelToClose != UIPanelType.None)
            {
                UIManager.Instance.HidePanel(_panelToClose);
            }

            // 2. 패널 열기
            if (_panelToOpen != UIPanelType.None)
            {
                if (_panelToOpen == UIPanelType.Panel_Dialogue)
                {
                    UIManager.Instance.ShowPanel(_panelToOpen, _dialogueIndex);
                }
                else if (_panelToOpen == UIPanelType.Panel_EscMenu)
                {
                    UIManager.Instance.ShowPanel(_panelToOpen, _isOpenedFromMainMenu);
                }
                else
                {
                    UIManager.Instance.ShowPanel(_panelToOpen);
                }
            }
        }
    }
}
