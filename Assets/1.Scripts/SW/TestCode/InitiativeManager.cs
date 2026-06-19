using UnityEngine;

namespace HornDancheong.Seongwoo.UI
{
    public class InitiativeManager : MonoBehaviour
    {
        [SerializeField] private GameObject _characterPanelPrefab;
        private GameObject[] _characterPanels;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            // 임시로 10개의 캐릭터 패널을 생성하도록 설정합니다.
            // 추후 인터페이스 통신 등으로 동작하도록 수정 예정입니다.
            CreateCharacterPanels(10);
        }

        /// <summary>
        /// 지정된 개수만큼 캐릭터 패널을 동적으로 생성하고, 이 스크립트가 붙어 있는 오브젝트의 자식으로 배치합니다.
        /// </summary>
        /// <param name="count">생성할 패널 개수</param>
        public void CreateCharacterPanels(int count)
        {
            if (_characterPanelPrefab == null)
            {
                Debug.LogWarning("Character Panel Prefab이 할당되지 않았습니다.");
                return;
            }

            _characterPanels = new GameObject[count];

            for (int i = 0; i < count; i++)
            {
                // 두 번째 인자로 transform을 전달하여 현재 오브젝트의 자식으로 생성되도록 합니다.
                _characterPanels[i] = Instantiate(_characterPanelPrefab, transform);
            }
        }
    }
}