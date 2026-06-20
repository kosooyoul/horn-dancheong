using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KD;

namespace HornDancheong.Seongwoo.UI
{
    /// <summary>
    /// Image_CharacterClass 오브젝트에 장착되어 UnitData의 직군(Role)에 따라
    /// 해당하는 아이콘 이미지, 색상, 그리고 텍스트(Text_CharacterClass)를 설정하는 UI 컴포넌트입니다.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class CharacterClassUI : MonoBehaviour
    {
        [System.Serializable]
        public struct RoleUIConfig
        {
            [Tooltip("직군에 표시할 아이콘 이미지")]
            public Sprite icon;
            [Tooltip("인스펙터에서 설정 가능한 아이콘 색상")]
            public Color color;
            [Tooltip("Text_CharacterClass에 표시할 직군 명칭")]
            public string labelText;
        }

        [Header("UI Elements Reference")]
        [SerializeField] private TMP_Text classText;
        [SerializeField] private TMP_Text characterNameText;

        [Header("Role Configurations")]
        [SerializeField] private RoleUIConfig dealerConfig = new RoleUIConfig { color = Color.red, labelText = "집행" };
        [SerializeField] private RoleUIConfig healerConfig = new RoleUIConfig { color = Color.green, labelText = "의관" };
        [SerializeField] private RoleUIConfig tankerConfig = new RoleUIConfig { color = Color.blue, labelText = "금군" };
        [SerializeField] private RoleUIConfig supporterConfig = new RoleUIConfig { color = Color.yellow, labelText = "보조" };

        [Header("Testing")]
        [SerializeField] private UnitData testUnitData;
        [SerializeField] private UnitRole testRole;

        private Image _image;

        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        private Image GetImage()
        {
            if (_image == null)
            {
                _image = GetComponent<Image>();
            }
            return _image;
        }

        /// <summary>
        /// UnitData를 전달받아 직군 UI(아이콘, 색상, 텍스트)를 갱신합니다.
        /// </summary>
        /// <param name="unitData">갱신할 유닛 데이터</param>
        public void Initialize(UnitData unitData)
        {
            if (unitData == null)
            {
                ClearUI();
                return;
            }
            ApplyRoleSettings(unitData.role);

            if (characterNameText != null)
            {
                characterNameText.text = unitData.unitName;
            }
        }

        /// <summary>
        /// 직군(UnitRole)에 따라 UI를 직접 갱신합니다.
        /// </summary>
        /// <param name="role">갱신할 직군</param>
        public void ApplyRoleSettings(UnitRole role)
        {
            Image image = GetImage();
            RoleUIConfig config = GetConfig(role);

            if (image != null)
            {
                image.sprite = config.icon;
                image.color = config.color;
                // 아이콘 스프라이트가 존재할 때만 활성화
                image.enabled = (config.icon != null);
            }

            if (classText != null)
            {
                classText.text = config.labelText;
            }
        }

        /// <summary>
        /// UI 표시 정보를 초기화합니다.
        /// </summary>
        public void ClearUI()
        {
            Image image = GetImage();
            if (image != null)
            {
                image.sprite = null;
                image.enabled = false;
            }

            if (classText != null)
            {
                classText.text = string.Empty;
            }

            if (characterNameText != null)
            {
                characterNameText.text = string.Empty;
            }
        }

        private RoleUIConfig GetConfig(UnitRole role)
        {
            switch (role)
            {
                case UnitRole.Dealer: return dealerConfig;
                case UnitRole.Healer: return healerConfig;
                case UnitRole.Tanker: return tankerConfig;
                case UnitRole.Supporter: return supporterConfig;
                default: return default;
            }
        }

        #region Context Menu & Editor Testing
        [ContextMenu("Test with UnitData")]
        public void TestWithUnitData()
        {
            if (testUnitData != null)
            {
                Initialize(testUnitData);
                Debug.Log($"[CharacterClassUI] {testUnitData.unitName} (직군: {testUnitData.role}) 데이터로 테스트 적용 완료.");
            }
            else
            {
                Debug.LogWarning("[CharacterClassUI] 테스트용 UnitData가 할당되지 않았습니다.");
            }
        }

        [ContextMenu("Test with Selected Role")]
        public void TestWithSelectedRole()
        {
            ApplyRoleSettings(testRole);
            Debug.Log($"[CharacterClassUI] 선택한 직군 '{testRole}'로 테스트 적용 완료.");
        }

        [ContextMenu("Clear UI")]
        public void ClearUIContextMenu()
        {
            testUnitData = null;
            ClearUI();
            Debug.Log("[CharacterClassUI] 테스트 상태를 지우고 UI를 초기화했습니다.");
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 에디터 상에서 실시간 편집 반영 (testUnitData가 할당 해제되면 UI도 지움)
            if (testUnitData != null)
            {
                ApplyRoleSettings(testUnitData.role);
                if (characterNameText != null)
                {
                    characterNameText.text = testUnitData.unitName;
                }
            }
            else
            {
                ClearUI();
            }
        }
#endif
        #endregion
    }
}
