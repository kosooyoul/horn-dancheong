using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HornDancheong.Seongwoo.UI
{
    public class CharacterPanelUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image portraitImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Slider hpSlider;
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private Image panelBackgroundImage;

        [Header("PC / NPC Background Colors")]
        [SerializeField] private Color pcColor = new Color(0.2f, 0.4f, 0.8f, 1f);   // 플레이어 캐릭터 패널 색상 (푸른색 계열)
        [SerializeField] private Color npcColor = new Color(0.8f, 0.2f, 0.2f, 1f);  // 적 캐릭터 패널 색상 (붉은색 계열)

        public string CharacterId { get; private set; }
        public int Initiative { get; private set; }

        /// <summary>
        /// 캐릭터 정보를 바탕으로 패널의 초기 정보를 설정합니다.
        /// </summary>
        public void Initialize(ICharacterBattleInfo data)
        {
            CharacterId = data.Id;
            Initiative = data.Initiative;
            
            if (nameText != null)
            {
                nameText.text = data.CharacterName;
            }

            // 초상화 로드 및 할당
            UpdatePortrait(data);

            // 체력 정보 설정
            UpdateHp(data.CurrentHp, data.MaxHp);

            // PC 혹은 NPC 여부에 따른 패널의 배경 색상 변경
            if (panelBackgroundImage != null)
            {
                panelBackgroundImage.color = data.IsPC ? pcColor : npcColor;
            }
        }

        /// <summary>
        /// 초상화 이미지를 설정합니다. Sprite가 제공되면 직접 사용하고, 없으면 경로로 로드합니다.
        /// </summary>
        private void UpdatePortrait(ICharacterBattleInfo data)
        {
            if (portraitImage == null) return;

            if (data.PortraitSprite != null)
            {
                portraitImage.sprite = data.PortraitSprite;
                portraitImage.gameObject.SetActive(true);
            }
            else if (!string.IsNullOrEmpty(data.PortraitPath))
            {
                Sprite loadedSprite = Resources.Load<Sprite>(data.PortraitPath);
                if (loadedSprite != null)
                {
                    portraitImage.sprite = loadedSprite;
                    portraitImage.gameObject.SetActive(true);
                }
                else
                {
                    portraitImage.gameObject.SetActive(false);
                }
            }
            else
            {
                portraitImage.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 캐릭터의 체력 바와 텍스트를 갱신합니다.
        /// </summary>
        public void UpdateHp(float currentHp, float maxHp)
        {
            maxHp = Mathf.Max(1f, maxHp);
            currentHp = Mathf.Clamp(currentHp, 0f, maxHp);

            if (hpSlider != null)
            {
                hpSlider.maxValue = maxHp;
                hpSlider.value = currentHp;
            }

            if (hpText != null)
            {
                hpText.text = $"{currentHp} / {maxHp}";
            }
        }
    }
}
