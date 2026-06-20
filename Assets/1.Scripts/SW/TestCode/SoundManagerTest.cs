using UnityEngine;
using HornDancheong.Audio;

namespace HornDancheong.Seongwoo.Test
{
    /// <summary>
    /// SoundManager의 기능을 검증하기 위한 테스트 컴포넌트입니다.
    /// 인스펙터 콘솔 창과 함께 작동하거나 키보드 입력을 통해 사운드 재생, BGM 페이드, SFX 풀 확장/재활용을 검증합니다.
    /// </summary>
    public class SoundManagerTest : MonoBehaviour
    {
        [Header("Test SFX Play Settings")]
        [SerializeField] private SFXType testSFXType = SFXType.None;
        [SerializeField] private float sfxVolume = 1f;
        [SerializeField] private float sfxPitch = 1f;

        [Header("Test BGM Play Settings")]
        [SerializeField] private BGMType testBGMType = BGMType.None;
        [SerializeField] private float bgmVolume = 1f;
        [SerializeField] private float fadeDuration = 1.0f;

        private void Update()
        {
            // 키보드 입력을 통한 빠른 테스트
            // 1: 지정된 BGM 재생 (페이드 효과 포함)
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                TestPlayBGM();
            }

            // 2: BGM 정지 (페이드아웃 포함)
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                TestStopBGM();
            }

            // 3: 지정된 SFX 재생
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                TestPlaySFX();
            }

            // 4: 동시에 12개의 SFX를 연속 재생하여 풀링(최대 10개) 및 가장 오래된 사운드 교체(Recycle) 로직 검증
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                TestSFXPooledRecycling();
            }
        }

        public void TestPlayBGM()
        {
            if (SoundManager.Instance != null)
            {
                Debug.Log($"[SoundManagerTest] BGM 재생 시도: {testBGMType} (페이드 시간: {fadeDuration}초)");
                SoundManager.Instance.PlayBGM(testBGMType, true, bgmVolume, fadeDuration);
            }
            else
            {
                Debug.LogError("[SoundManagerTest] 씬에 SoundManager 인스턴스가 존재하지 않습니다.");
            }
        }

        public void TestStopBGM()
        {
            if (SoundManager.Instance != null)
            {
                Debug.Log($"[SoundManagerTest] BGM 정지 시도 (페이드아웃 시간: {fadeDuration}초)");
                SoundManager.Instance.StopBGM(fadeDuration);
            }
            else
            {
                Debug.LogError("[SoundManagerTest] 씬에 SoundManager 인스턴스가 존재하지 않습니다.");
            }
        }

        public void TestPlaySFX()
        {
            if (SoundManager.Instance != null)
            {
                Debug.Log($"[SoundManagerTest] SFX 재생 시도: {testSFXType}");
                SoundManager.Instance.PlaySFX(testSFXType, sfxVolume, sfxPitch);
            }
            else
            {
                Debug.LogError("[SoundManagerTest] 씬에 SoundManager 인스턴스가 존재하지 않습니다.");
            }
        }

        public void TestSFXPooledRecycling()
        {
            if (SoundManager.Instance != null)
            {
                Debug.Log("[SoundManagerTest] 동시 SFX 12개 재생 시작 - 최대 풀 개수(10개) 및 가장 오래된 소스 재사용 검증");
                
                // 루프를 돌며 아주 미세하게 딜레이를 주거나(동일 프레임이어도 됨) 12번의 PlaySFX를 즉시 호출합니다.
                // 10개를 넘어서면 경고 로그가 뜨며 오래된 채널이 재활용되어야 합니다.
                for (int i = 1; i <= 12; i++)
                {
                    SoundManager.Instance.PlaySFX(testSFXType, sfxVolume, sfxPitch);
                }
            }
            else
            {
                Debug.LogError("[SoundManagerTest] 씬에 SoundManager 인스턴스가 존재하지 않습니다.");
            }
        }
    }
}
