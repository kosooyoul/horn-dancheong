using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace KD
{
    /// <summary>
    /// 배치 로스터 패널의 유닛 슬롯 하나.
    /// 클릭 → 유닛 선택. 드래그 앤 드롭 → 타일에 직접 배치.
    ///
    /// Prefab 구성:
    ///   Root (Button + DeploymentUnitSlotUI)
    ///     ├─ UnitNameText (TextMeshProUGUI)
    ///     ├─ SelectedFrame (Image, 기본 비활성)
    ///     └─ GhostIcon (Image, 드래그 중 마우스를 따라다님 / Prefab 루트가 없으면 생략 가능)
    ///
    /// Inspector:
    ///   unitNameText   - 유닛 이름 텍스트
    ///   selectedFrame  - 선택 표시 프레임 GameObject
    ///   dragGhostIcon  - 드래그 고스트 (선택 사항, 없으면 동작 생략)
    /// </summary>
    public class DeploymentUnitSlotUI : MonoBehaviour,
        IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private TextMeshProUGUI unitNameText;
        [SerializeField] private GameObject      selectedFrame;
        [SerializeField] private RectTransform   dragGhostIcon;

        private OwnedUnit               boundUnit;
        private KDBattleTurnController  turnController;

        private Canvas rootCanvas;

        // ── 바인딩 ────────────────────────────────────────────────────────

        public void Bind(OwnedUnit unit, KDBattleTurnController controller)
        {
            boundUnit      = unit;
            turnController = controller;

            if (unitNameText != null)
                unitNameText.text = unit?.unitData.unitName ?? "—";

            SetSelected(false);

            if (dragGhostIcon != null)
                dragGhostIcon.gameObject.SetActive(false);

            gameObject.SetActive(unit != null);
        }

        public void SetSelected(bool selected)
        {
            if (selectedFrame != null)
                selectedFrame.SetActive(selected);
        }

        // ── 클릭 → 유닛 선택 ─────────────────────────────────────────────

        public void OnPointerClick(PointerEventData eventData)
        {
            if (boundUnit == null || turnController == null) return;
            turnController.SelectDeployUnit(boundUnit);
        }

        // ── 드래그 앤 드롭 ────────────────────────────────────────────────

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (boundUnit == null || turnController == null) return;

            turnController.SelectDeployUnit(boundUnit);

            if (dragGhostIcon != null)
            {
                rootCanvas = GetComponentInParent<Canvas>();
                dragGhostIcon.gameObject.SetActive(true);
                MoveGhostToPointer(eventData.position);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            MoveGhostToPointer(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (dragGhostIcon != null)
                dragGhostIcon.gameObject.SetActive(false);

            if (turnController != null)
                turnController.TryDeploySelectedUnitAtScreenPosition(eventData.position);
        }

        private void MoveGhostToPointer(Vector2 screenPos)
        {
            if (dragGhostIcon == null) return;

            if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rootCanvas.transform as RectTransform,
                    screenPos,
                    rootCanvas.worldCamera,
                    out Vector2 localPoint);
                dragGhostIcon.anchoredPosition = localPoint;
            }
            else
            {
                dragGhostIcon.position = screenPos;
            }
        }
    }
}
