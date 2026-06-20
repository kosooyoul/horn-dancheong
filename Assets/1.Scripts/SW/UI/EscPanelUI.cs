using UnityEngine;

namespace HornDancheong.Seongwoo.UI
{
    /// <summary>
    /// ESCMenu UI 패널을 제어하는 클래스입니다.
    /// 진입 경로(예: 메인화면 여부)에 따라 특정 버튼들을 동적으로 비활성화(숨김) 처리합니다.
    /// </summary>
    public class EscPanelUI : MonoBehaviour
    {
        [Header("Main Menu Settings")]
        [Tooltip("메인화면에서 ESC 메뉴를 열었을 때 비활성화(숨김) 처리할 버튼들의 배열입니다.")]
        [SerializeField] private GameObject[] buttonsToHideOnMainMenuOpen;

        private void OnEnable()
        {
            if (UIManager.Instance == null)
            {
                Debug.LogWarning("[EscPanelUI] UIManager.Instance가 존재하지 않아 진입 경로를 확인할 수 없습니다.");
                return;
            }

            bool isFromMainMenu = UIManager.Instance.IsEscMenuOpenedFromMainMenu;
            
            if (buttonsToHideOnMainMenuOpen != null)
            {
                foreach (var button in buttonsToHideOnMainMenuOpen)
                {
                    if (button != null)
                    {
                        // 메인 화면에서 열린 경우 버튼을 비활성화(SetActive(false)), 그렇지 않으면 활성화
                        button.SetActive(!isFromMainMenu);
                    }
                }
            }
        }
    }
}
