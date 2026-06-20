using UnityEngine;
using UnityEngine.UI;
using HornDancheong.Audio;

namespace HornDancheong.Seongwoo.UI
{
    /// <summary>
    /// EscMenu UI 패널 내부의 Master, BGM, SFX 볼륨 슬라이더 입력을 받아
    /// SoundManager의 볼륨 시스템에 연동시켜주는 UI 컨트롤러 클래스입니다.
    /// 디버그용 사운드 재생 테스트 기능도 포함되어 있습니다.
    /// </summary>
    public class EscMenuVolumeController : MonoBehaviour
    {
        [Header("Volume Sliders")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider sfxSlider;

        [Header("Debug Playback Settings")]
        [SerializeField] private BGMType testBgmType = BGMType.None;
        [SerializeField] private SFXType testSfxType = SFXType.None;

        private void Start()
        {
            InitializeSliders();
        }

        private void OnEnable()
        {
            // 패널이 다시 활성화될 때 볼륨 값을 동기화합니다.
            UpdateSliderValues();
        }

        private void InitializeSliders()
        {
            if (SoundManager.Instance == null)
            {
                Debug.LogWarning("[EscMenuVolumeController] SoundManager.Instance를 찾을 수 없어 볼륨 설정을 연동할 수 없습니다.");
                return;
            }

            UpdateSliderValues();

            // 슬라이더 변경 이벤트 리스너 등록
            if (masterSlider != null)
            {
                masterSlider.onValueChanged.RemoveAllListeners();
                masterSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }

            if (bgmSlider != null)
            {
                bgmSlider.onValueChanged.RemoveAllListeners();
                bgmSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
            }

            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.RemoveAllListeners();
                sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            }
        }

        private void UpdateSliderValues()
        {
            if (SoundManager.Instance == null) return;

            // 현재 사운드 매니저 값으로 슬라이더의 위치를 초기화
            if (masterSlider != null) masterSlider.value = SoundManager.Instance.MasterVolume;
            if (bgmSlider != null) bgmSlider.value = SoundManager.Instance.BgmVolume;
            if (sfxSlider != null) sfxSlider.value = SoundManager.Instance.SfxVolume;
        }

        private void OnMasterVolumeChanged(float value)
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.SetMasterVolume(value);
            }
        }

        private void OnBgmVolumeChanged(float value)
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.SetBGMVolume(value);
            }
        }

        private void OnSfxVolumeChanged(float value)
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.SetSFXVolume(value);
            }
        }

        private void OnDestroy()
        {
            // 리스너 제거로 메모리 누수 방지
            if (masterSlider != null) masterSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            if (bgmSlider != null) bgmSlider.onValueChanged.RemoveListener(OnBgmVolumeChanged);
            if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
        }

        #region Debug Test Play API

        public void DebugPlayBGM()
        {
            if (SoundManager.Instance != null)
            {
                Debug.Log($"[EscMenuVolumeController] 디버그 BGM 재생: {testBgmType}");
                SoundManager.Instance.PlayBGM(testBgmType, true, 1.0f, 0.5f);
            }
        }

        public void DebugStopBGM()
        {
            if (SoundManager.Instance != null)
            {
                Debug.Log("[EscMenuVolumeController] 디버그 BGM 정지");
                SoundManager.Instance.StopBGM(0.5f);
            }
        }

        public void DebugPlaySFX()
        {
            if (SoundManager.Instance != null)
            {
                Debug.Log($"[EscMenuVolumeController] 디버그 SFX 재생: {testSfxType}");
                SoundManager.Instance.PlaySFX(testSfxType, 1.0f, 1.0f);
            }
        }

        #endregion
    }
}
