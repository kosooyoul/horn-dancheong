using UnityEngine;
using UnityEngine.EventSystems;

namespace HornDancheong.Seongwoo.UI
{
    /// <summary>
    /// FrontPanel UI 오브젝트에 부착되어 클릭 입력을 감지하고 DialogueDisplayer에 신호를 보내는 컴포넌트입니다.
    /// IPointerClickHandler 인터페이스를 통해 UI 터치/클릭 이벤트를 직접 받아옵니다.
    /// </summary>
    public class DialogueFrontPanel : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private DialogueDisplayer dialogueDisplayer;

        private void Awake()
        {
            // 수동 할당되지 않은 경우 자동으로 DialogueDisplayer 컴포넌트를 탐색해 설정합니다.
            if (dialogueDisplayer == null)
            {
                dialogueDisplayer = FindObjectOfType<DialogueDisplayer>();
            }
        }

        /// <summary>
        /// 클릭 또는 탭을 감지했을 때 호출됩니다.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (dialogueDisplayer != null)
            {
                dialogueDisplayer.OnFrontPanelClicked();
            }
            else
            {
                Debug.LogWarning("[DialogueFrontPanel] 연결된 DialogueDisplayer를 찾을 수 없습니다.");
            }
        }
    }
}
