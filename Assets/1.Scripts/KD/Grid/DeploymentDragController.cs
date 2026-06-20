using UnityEngine;
using UnityEngine.InputSystem;

namespace KD
{
    // 배치 단계에서 고스트 프리팹을 드래그하여 다른 배치 가능 타일로 재배치.
    //
    // 흐름:
    //   마우스 다운 → 해당 타일에 고스트 있으면 드래그 시작
    //   드래그 중   → 고스트가 마우스 위치(그리드 스냅)를 따라 이동
    //   마우스 업   → 배치 가능 타일이면 TacticalBattleManager.TryRedeployGhost 호출,
    //                실패하면 원래 위치로 복귀
    public class DeploymentDragController : MonoBehaviour
    {
        [SerializeField] private TacticalBattleManager battleManager;
        [SerializeField] private GridManager           gridManager;
        [SerializeField] private Camera                battleCamera;

        private OwnedUnit  _dragging;
        private Vector2Int _originTile;

        private static readonly Plane GroundPlane = new Plane(Vector3.up, Vector3.zero);

        private void Awake()
        {
            if (battleCamera == null) battleCamera = Camera.main;
        }

        private void Update()
        {
            if (_dragging == null)
                HandlePickup();
            else
                HandleDrag();
        }

        private void HandlePickup()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;

            // UI 클릭 시 드래그 픽업 무시
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (!TryGetTileUnderMouse(out Vector2Int tile)) return;

            OwnedUnit unit = gridManager.GetGhostUnitAt(tile);
            if (unit == null) return;

            _dragging   = unit;
            _originTile = tile;
        }

        private void HandleDrag()
        {
            if (TryGetTileUnderMouse(out Vector2Int hovered))
                gridManager.SetUnitGhost(_dragging, hovered);

            if (Mouse.current == null || !Mouse.current.leftButton.wasReleasedThisFrame) return;

            bool placed = false;
            if (TryGetTileUnderMouse(out Vector2Int dropTile) && CanDrop(dropTile))
                placed = battleManager != null && battleManager.TryRedeployGhost(_dragging, dropTile);

            if (!placed)
                gridManager.SetUnitGhost(_dragging, _originTile);

            _dragging = null;
        }

        private bool CanDrop(Vector2Int tile)
        {
            // if (!gridManager.CurrentDeployableHighlights.Contains(tile))
            //     return false;

            OwnedUnit occupant = gridManager.GetGhostUnitAt(tile);
            return occupant == null || occupant == _dragging;
        }

        private bool TryGetTileUnderMouse(out Vector2Int tile)
        {
            Vector2 mousePos = Mouse.current?.position.ReadValue() ?? Vector2.zero;
            Ray ray = battleCamera.ScreenPointToRay(mousePos);
            if (GroundPlane.Raycast(ray, out float dist))
            {
                tile = gridManager.WorldToGrid(ray.GetPoint(dist));
                return true;
            }
            tile = default;
            return false;
        }
    }
}
