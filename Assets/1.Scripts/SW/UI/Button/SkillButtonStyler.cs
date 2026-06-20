using UnityEngine;
using UnityEngine.UI;
using KD;

namespace HornDancheong.Seongwoo.UI
{
    /// <summary>
    /// 캐릭터의 직군(UnitRole)에 맞춰 버튼의 배경 이미지(Sprite)를 동적으로 변경하는 컴포넌트입니다.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class SkillButtonStyler : MonoBehaviour
    {
        [Header("Role Sprites")]
        [SerializeField] private Sprite dealerSprite;     // 집행 (Dealer) - Red 계열
        [SerializeField] private Sprite tankerSprite;     // 금군 (Tanker) - Yellow 계열
        [SerializeField] private Sprite healerSprite;     // 의관 (Healer) - Green 계열
        [SerializeField] private Sprite supporterSprite;  // 보조 (Supporter)

        private Image _image;

        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        /// <summary>
        /// 지정한 직군(UnitRole)에 해당하는 스프라이트로 버튼의 배경 이미지를 갱신합니다.
        /// </summary>
        public void ApplyStyle(UnitRole role)
        {
            if (_image == null)
            {
                _image = GetComponent<Image>();
            }

            if (_image == null) return;

            Sprite targetSprite = null;
            switch (role)
            {
                case UnitRole.Dealer:
                    targetSprite = dealerSprite;
                    break;
                case UnitRole.Tanker:
                    targetSprite = tankerSprite;
                    break;
                case UnitRole.Healer:
                    targetSprite = healerSprite;
                    break;
                case UnitRole.Supporter:
                    targetSprite = supporterSprite;
                    break;
            }

            if (targetSprite != null)
            {
                _image.sprite = targetSprite;

                // Button 컴포넌트의 targetGraphic 설정 동기화
                Button button = GetComponent<Button>();
                if (button != null)
                {
                    button.targetGraphic = _image;
                }
            }
        }
    }
}
