using System;
using System.Collections.Generic;
using UnityEngine;

namespace HornDancheong.Seongwoo.UI
{
    /// <summary>
    /// 단일 씬 환경에서 UI 패널들의 활성화/비활성화를 관리하는 중앙 싱글톤 매니저입니다.
    /// 에디터 인스펙터에서 직접 드래그 앤 드롭으로 패널을 등록하여 비활성화 상태의 패널도 안전하게 관리합니다.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [System.Serializable]
        public struct PanelEntry
        {
            public UIPanelType panelType;
            public GameObject panelGameObject;
        }

        public static UIManager Instance { get; private set; }

        [Header("UI Panels Registration")]
        [SerializeField] private List<PanelEntry> panelEntries = new List<PanelEntry>();

        private readonly Dictionary<UIPanelType, GameObject> _panelsDict = new Dictionary<UIPanelType, GameObject>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogWarning($"[UIManager] 이미 인스턴스가 존재하므로 해당 오브젝트를 파괴합니다: {gameObject.name}");
                Destroy(gameObject);
                return;
            }

            InitializePanels();
        }

        private void InitializePanels()
        {
            _panelsDict.Clear();
            foreach (var entry in panelEntries)
            {
                if (entry.panelType == UIPanelType.None) continue;

                if (entry.panelGameObject == null)
                {
                    Debug.LogWarning($"[UIManager] {entry.panelType}에 할당된 GameObject가 비어있습니다.");
                    continue;
                }

                if (_panelsDict.ContainsKey(entry.panelType))
                {
                    Debug.LogWarning($"[UIManager] 중복된 패널 타입 등록이 감지되었습니다: {entry.panelType}");
                    _panelsDict[entry.panelType] = entry.panelGameObject;
                }
                else
                {
                    _panelsDict.Add(entry.panelType, entry.panelGameObject);
                }
            }
        }

        /// <summary>
        /// 특정 타입의 UI 패널을 활성화합니다.
        /// </summary>
        public void ShowPanel(UIPanelType panelType)
        {
            if (panelType == UIPanelType.None) return;

            if (panelType == UIPanelType.Panel_Dialogue)
            {
                Debug.LogError("[UIManager] Dialogue 패널은 다이얼로그 인덱스(dialogueIndex) 없이 활성화할 수 없습니다. 패널 열기를 차단합니다.");
                return;
            }

            if (_panelsDict.TryGetValue(panelType, out var panelGo))
            {
                if (panelGo != null && !panelGo.activeSelf)
                {
                    panelGo.SetActive(true);
                }
            }
            else
            {
                Debug.LogWarning($"[UIManager] 등록되지 않은 패널을 표시하려 했습니다: {panelType}");
            }
        }

        /// <summary>
        /// 다이얼로그 인덱스를 지정하여 특정 타입의 UI 패널을 활성화합니다. (대화 패널 전용)
        /// </summary>
        public void ShowPanel(UIPanelType panelType, int dialogueIndex)
        {
            if (panelType == UIPanelType.None) return;

            if (_panelsDict.TryGetValue(panelType, out var panelGo))
            {
                if (panelGo != null)
                {
                    if (!panelGo.activeSelf)
                    {
                        panelGo.SetActive(true);
                    }

                    if (panelType == UIPanelType.Panel_Dialogue)
                    {
                        if (DialogueDisplayer.Instance != null)
                        {
                            DialogueDisplayer.Instance.StartDialogue(dialogueIndex);
                        }
                        else
                        {
                            var displayer = panelGo.GetComponent<DialogueDisplayer>();
                            if (displayer != null)
                            {
                                displayer.StartDialogue(dialogueIndex);
                            }
                            else
                            {
                                Debug.LogWarning("[UIManager] Panel_Dialogue GameObject에 DialogueDisplayer 컴포넌트가 존재하지 않습니다.");
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[UIManager] 등록되지 않은 패널을 표시하려 했습니다: {panelType}");
            }
        }

        /// <summary>
        /// 특정 타입의 UI 패널을 비활성화합니다.
        /// </summary>
        public void HidePanel(UIPanelType panelType)
        {
            if (panelType == UIPanelType.None) return;

            if (_panelsDict.TryGetValue(panelType, out var panelGo))
            {
                if (panelGo != null)
                {
                    panelGo.SetActive(false);
                }
            }
            else
            {
                Debug.LogWarning($"[UIManager] 등록되지 않은 패널을 숨기려 했습니다: {panelType}");
            }
        }

        /// <summary>
        /// 문자열 이름을 기반으로 패널을 활성화합니다. (엑셀 데이터 테이블 대응용)
        /// </summary>
        public void ShowPanel(string panelName)
        {
            if (string.IsNullOrEmpty(panelName)) return;

            if (Enum.TryParse<UIPanelType>(panelName, out var type))
            {
                ShowPanel(type);
            }
            else
            {
                Debug.LogWarning($"[UIManager] UIPanelType Enum으로 파싱할 수 없는 패널 이름입니다: {panelName}");
            }
        }

        /// <summary>
        /// 문자열 이름과 다이얼로그 인덱스를 지정하여 패널을 활성화합니다. (대화 패널 전용)
        /// </summary>
        public void ShowPanel(string panelName, int dialogueIndex)
        {
            if (string.IsNullOrEmpty(panelName)) return;

            if (Enum.TryParse<UIPanelType>(panelName, out var type))
            {
                ShowPanel(type, dialogueIndex);
            }
            else
            {
                Debug.LogWarning($"[UIManager] UIPanelType Enum으로 파싱할 수 없는 패널 이름입니다: {panelName}");
            }
        }

        /// <summary>
        /// 문자열 이름을 기반으로 패널을 비활성화합니다.
        /// </summary>
        public void HidePanel(string panelName)
        {
            if (string.IsNullOrEmpty(panelName)) return;

            if (Enum.TryParse<UIPanelType>(panelName, out var type))
            {
                HidePanel(type);
            }
            else
            {
                Debug.LogWarning($"[UIManager] UIPanelType Enum으로 파싱할 수 없는 패널 이름입니다: {panelName}");
            }
        }

        /// <summary>
        /// 특정 패널의 GameObject 참조를 반환합니다.
        /// </summary>
        public GameObject GetPanel(UIPanelType panelType)
        {
            if (_panelsDict.TryGetValue(panelType, out var panelGo))
            {
                return panelGo;
            }
            return null;
        }

        /// <summary>
        /// 문자열 이름으로 패널 GameObject 참조를 반환합니다.
        /// </summary>
        public GameObject GetPanel(string panelName)
        {
            if (string.IsNullOrEmpty(panelName)) return null;

            if (Enum.TryParse<UIPanelType>(panelName, out var type))
            {
                return GetPanel(type);
            }
            return null;
        }
    }
}
