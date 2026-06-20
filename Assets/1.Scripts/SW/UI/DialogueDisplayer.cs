using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HornDancheong.Seongwoo.UI
{
    /// <summary>
    /// DialogueTable ScriptableObject 데이터를 파싱하여 대화창 UI(이름, 대사, 포트레이트)를 갱신하고
    /// 대화 진행 흐름을 제어하는 클래스입니다.
    /// </summary>
    public class DialogueDisplayer : MonoBehaviour
    {
        public static DialogueDisplayer Instance { get; private set; }

        [Header("Data Source")]
        [SerializeField] private DialogueTable dialogueTable;

        [Header("UI Panel Reference")]
        [SerializeField] private GameObject panelDialogue;

        [Header("Background Image")]
        [SerializeField] private Image backgroundImage;

        [Header("Portrait Images")]
        [SerializeField] private Image portraitLeft;
        [SerializeField] private Image portraitCenter;
        [SerializeField] private Image portraitRight;

        [Header("Text UI Elements")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text dialogueText;

        // 대화 시작 인덱스를 외부에서 받기 위한 static 이벤트
        public static event Action<int> OnDialogueStartRequested;

        private int _currentDialogueIndex;
        private DialogueTableItem _currentItem;
        private bool _isDialogueActive;

        public bool IsDialogueActive => _isDialogueActive;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogWarning($"[DialogueDisplayer] 이미 인스턴스가 존재하므로 해당 오브젝트를 파괴합니다: {gameObject.name}");
                Destroy(gameObject);
                return;
            }
        }

        private void OnEnable()
        {
            OnDialogueStartRequested += StartDialogue;
        }

        private void OnDisable()
        {
            OnDialogueStartRequested -= StartDialogue;
        }

        /// <summary>
        /// 외부 이벤트를 통해 대화를 시작 요청하기 위한 정적 헬퍼 메소드입니다.
        /// </summary>
        public static void RequestDialogue(int startIndex)
        {
            OnDialogueStartRequested?.Invoke(startIndex);
        }

        /// <summary>
        /// 지정한 인덱스부터 대화를 시작합니다.
        /// </summary>
        public void StartDialogue(int startIndex)
        {
            if (dialogueTable == null)
            {
                Debug.LogError("[DialogueDisplayer] DialogueTable ScriptableObject가 지정되지 않았습니다.");
                return;
            }

            _isDialogueActive = true;

            if (panelDialogue != null && !panelDialogue.activeSelf)
            {
                panelDialogue.SetActive(true);
            }

            ShowDialogueIndex(startIndex);
        }

        /// <summary>
        /// 특정 대화 인덱스의 데이터를 UI에 표시합니다.
        /// </summary>
        private void ShowDialogueIndex(int index)
        {
            _currentDialogueIndex = index;
            _currentItem = GetDialogueItem(index);

            if (_currentItem == null)
            {
                Debug.LogWarning($"[DialogueDisplayer] DialogueIndex '{index}'에 해당하는 대화 데이터를 찾을 수 없어 대화를 종료합니다.");
                EndDialogue();
                return;
            }

            // 1. 텍스트 설정
            if (nameText != null) nameText.text = _currentItem.CharacterName;
            if (dialogueText != null) dialogueText.text = _currentItem.DialogueText;

            // 2. 포트레이트 설정
            UpdatePortrait(portraitLeft, _currentItem.LeftPortrait);
            UpdatePortrait(portraitCenter, _currentItem.CenterPortrait);
            UpdatePortrait(portraitRight, _currentItem.RightPortrait);

            // 3. 백그라운드 이미지 설정
            UpdateBackground(_currentItem.BackgroundImage);
        }

        /// <summary>
        /// 포트레이트 이미지 컴포넌트에 스프라이트를 할당하고 활성화/비활성화 상태를 제어합니다.
        /// </summary>
        private void UpdatePortrait(Image portraitImage, string portraitFilename)
        {
            if (portraitImage == null) return;

            if (string.IsNullOrEmpty(portraitFilename))
            {
                portraitImage.gameObject.SetActive(false);
                return;
            }

            Sprite sprite = LoadPortraitSprite(portraitFilename);
            if (sprite != null)
            {
                if (portraitImage.gameObject.activeSelf && portraitImage.sprite == sprite)
                {
                    return;
                }
                portraitImage.sprite = sprite;
                portraitImage.gameObject.SetActive(true);
            }
            else
            {
                portraitImage.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Assets/Art/Portrait/Resources/Portrait/ 폴더 등 리소스 폴더로부터 스프라이트를 동적 로드합니다.
        /// 파일 확장자가 포함된 경우 제거하여 로드합니다.
        /// </summary>
        private Sprite LoadPortraitSprite(string filename)
        {
            if (string.IsNullOrEmpty(filename)) return null;

            string resourceName = filename;
            int dotIndex = resourceName.LastIndexOf('.');
            if (dotIndex > 0)
            {
                resourceName = resourceName.Substring(0, dotIndex);
            }

            // 1. 단일 스프라이트 로드 시도 (Portrait/ 폴더)
            Sprite sprite = Resources.Load<Sprite>("Portrait/" + resourceName);
            
            // 2. 만약 Multiple Sprite (스프라이트 시트)로 설정된 경우 Resources.LoadAll을 사용해야 함
            if (sprite == null)
            {
                Sprite[] sprites = Resources.LoadAll<Sprite>("Portrait/" + resourceName);
                if (sprites != null && sprites.Length > 0)
                {
                    sprite = sprites[0];
                }
            }

            // 3. Fallback: Resources 폴더 최상위 단일 스프라이트 로드 시도
            if (sprite == null)
            {
                sprite = Resources.Load<Sprite>(resourceName);
            }

            // 4. Fallback: Resources 폴더 최상위 Multiple Sprite 로드 시도
            if (sprite == null)
            {
                Sprite[] sprites = Resources.LoadAll<Sprite>(resourceName);
                if (sprites != null && sprites.Length > 0)
                {
                    sprite = sprites[0];
                }
            }

            if (sprite == null)
            {
                Debug.LogWarning($"[DialogueDisplayer] 포트레이트 이미지를 로드하지 못했습니다: {filename} (검색 경로: 'Portrait/{resourceName}' 및 '{resourceName}')");
            }

            return sprite;
        }

        /// <summary>
        /// 백그라운드 이미지 컴포넌트에 스프라이트를 할당하고 활성화 상태를 제어합니다.
        /// </summary>
        private void UpdateBackground(string backgroundFilename)
        {
            if (backgroundImage == null) return;

            if (string.IsNullOrEmpty(backgroundFilename))
            {
                backgroundImage.gameObject.SetActive(false);
                return;
            }

            Sprite sprite = LoadBackgroundSprite(backgroundFilename);
            if (sprite != null)
            {
                if (backgroundImage.gameObject.activeSelf && backgroundImage.sprite == sprite)
                {
                    return;
                }
                backgroundImage.sprite = sprite;
                backgroundImage.gameObject.SetActive(true);
            }
            else
            {
                backgroundImage.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Assets/Resources/Background/ 폴더 등 리소스 폴더로부터 백그라운드 스프라이트를 동적 로드합니다.
        /// 파일 확장자가 포함된 경우 제거하여 로드합니다.
        /// </summary>
        private Sprite LoadBackgroundSprite(string filename)
        {
            if (string.IsNullOrEmpty(filename)) return null;

            string resourceName = filename;
            int dotIndex = resourceName.LastIndexOf('.');
            if (dotIndex > 0)
            {
                resourceName = resourceName.Substring(0, dotIndex);
            }

            // 1. 단일 스프라이트 로드 시도 (Background/ 폴더)
            Sprite sprite = Resources.Load<Sprite>("Background/" + resourceName);
            
            // 2. 만약 Multiple Sprite (스프라이트 시트)로 설정된 경우 Resources.LoadAll을 사용해야 함
            if (sprite == null)
            {
                Sprite[] sprites = Resources.LoadAll<Sprite>("Background/" + resourceName);
                if (sprites != null && sprites.Length > 0)
                {
                    sprite = sprites[0];
                }
            }

            // 3. Fallback: typo가 있는 폴더 "Backgroud/" 로드 시도
            if (sprite == null)
            {
                sprite = Resources.Load<Sprite>("Backgroud/" + resourceName);
            }
            if (sprite == null)
            {
                Sprite[] sprites = Resources.LoadAll<Sprite>("Backgroud/" + resourceName);
                if (sprites != null && sprites.Length > 0)
                {
                    sprite = sprites[0];
                }
            }

            // 4. Fallback: Resources 폴더 최상위 단일 스프라이트 로드 시도
            if (sprite == null)
            {
                sprite = Resources.Load<Sprite>(resourceName);
            }

            // 5. Fallback: Resources 폴더 최상위 Multiple Sprite 로드 시도
            if (sprite == null)
            {
                Sprite[] sprites = Resources.LoadAll<Sprite>(resourceName);
                if (sprites != null && sprites.Length > 0)
                {
                    sprite = sprites[0];
                }
            }

            if (sprite == null)
            {
                Debug.LogWarning($"[DialogueDisplayer] 백그라운드 이미지를 로드하지 못했습니다: {filename} (검색 경로: 'Background/{resourceName}', 'Backgroud/{resourceName}' 및 '{resourceName}')");
            }

            return sprite;
        }

        /// <summary>
        /// DialogueTable의 Sheet1 리스트에서 DialogueIndex가 일치하는 아이템을 찾습니다.
        /// </summary>
        private DialogueTableItem GetDialogueItem(int index)
        {
            if (dialogueTable == null || dialogueTable.Sheet1 == null) return null;
            return dialogueTable.Sheet1.Find(item => item.DialogueIndex == index);
        }

        /// <summary>
        /// FrontPanel이 클릭되었을 때 호출되는 스크립트 진행 메소드입니다.
        /// </summary>
        public void OnFrontPanelClicked()
        {
            if (!_isDialogueActive || _currentItem == null) return;

            // 1. 대화 종료 조건 확인 (NextUI가 빈칸이 아니면 대화의 끝으로 간주하고 해당 UI를 활성화하며 종료)
            if (!string.IsNullOrEmpty(_currentItem.NextUI))
            {
                // 다음 UI 활성화 처리 (UIManager 연동)
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowPanel(_currentItem.NextUI);
                }
                else
                {
                    Debug.LogWarning($"[DialogueDisplayer] UIManager.Instance가 없어서 NextUI '{_currentItem.NextUI}'를 켤 수 없습니다.");
                }

                // 대화 패널 비활성화 및 상태 초기화
                EndDialogue();
            }
            else
            {
                // 다음 대화 인덱스로 진행
                ShowDialogueIndex(_currentDialogueIndex + 1);
            }
        }

        /// <summary>
        /// 대화를 종료하고 대화 UI 패널을 비활성화합니다.
        /// </summary>
        public void EndDialogue()
        {
            _isDialogueActive = false;
            _currentItem = null;

            if (panelDialogue != null)
            {
                panelDialogue.SetActive(false);
            }

            if (backgroundImage != null)
            {
                backgroundImage.gameObject.SetActive(false);
            }
        }
    }
}
