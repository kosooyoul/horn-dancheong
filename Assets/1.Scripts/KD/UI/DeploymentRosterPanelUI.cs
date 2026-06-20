using System.Collections.Generic;
using UnityEngine;

namespace KD
{
    /// <summary>
    /// 배치 단계 로스터 패널.
    /// 씬에 미리 배치된 4개의 DeploymentUnitSlotUI 슬롯에 보유 유닛을 채워 넣는다.
    /// 유닛 수가 슬롯보다 적으면 남은 슬롯은 빈 상태로 표시된다.
    ///
    /// Inspector:
    ///   slots           - 씬에 배치된 DeploymentUnitSlotUI 4개 (순서대로 드래그)
    ///   rosterManager   - PlayerRosterManager
    ///   turnController  - KDBattleTurnController
    /// </summary>
    public class DeploymentRosterPanelUI : MonoBehaviour
    {
        [SerializeField] private DeploymentUnitSlotUI[]  slots;
        [SerializeField] private PlayerRosterManager     rosterManager;
        [SerializeField] private KDBattleTurnController  turnController;

        private void Start()
        {
            RefreshSlots();

            if (turnController != null)
                turnController.OnDeployUnitSelected += OnDeployUnitSelected;
        }

        private void OnDestroy()
        {
            if (turnController != null)
                turnController.OnDeployUnitSelected -= OnDeployUnitSelected;
        }

        private void RefreshSlots()
        {
            if (slots == null) return;

            IReadOnlyList<OwnedUnit> source = GetSource();

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;

                OwnedUnit unit = (source != null && i < source.Count) ? source[i] : null;
                slots[i].Bind(unit, turnController);
            }
        }

        private void OnDeployUnitSelected(OwnedUnit selected)
        {
            if (slots == null) return;

            IReadOnlyList<OwnedUnit> source = GetSource();

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;

                bool isSelected = selected != null
                    && source != null
                    && i < source.Count
                    && source[i] == selected;

                slots[i].SetSelected(isSelected);
            }
        }

        private IReadOnlyList<OwnedUnit> GetSource()
        {
            if (rosterManager == null) return null;
            return rosterManager.SelectedParty.Count > 0
                ? rosterManager.SelectedParty
                : rosterManager.OwnedUnits;
        }
    }
}
