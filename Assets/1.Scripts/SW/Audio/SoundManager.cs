using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HornDancheong.Audio
{
    /// <summary>
    /// BGM 및 SFX 사운드 재생을 통합 관리하는 싱글톤 사운드 매니저입니다.
    /// SFX는 초기에 설정된 풀 크기만큼 생성하고 최대 동시 실행 개수(10개)까지 자동으로 확장합니다.
    /// 동시 실행 개수를 초과하여 재생을 요청하면 가장 오래된 SFX를 강제 중지하고 재사용합니다.
    /// BGM은 두 개의 소스를 교차 사용하여 페이드(Cross-Fade) 전환이 가능합니다.
    /// Master, BGM, SFX 볼륨을 실시간으로 곱하여 전체 오디오 출력을 제어합니다.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("SFX Pool Settings")]
        [SerializeField] private int initialPoolSize = 5;
        [SerializeField] private int maxPoolSize = 10;

        [Header("Volume Settings")]
        [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float bgmVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;

        [Header("Editor Sync Path Settings (Editor Only)")]
        [SerializeField] private string sfxFolderPath = "Assets/Resources/Audio/SFX";
        [SerializeField] private string bgmFolderPath = "Assets/Resources/Audio/BGM";

        [Header("Audio Clips (Synced by Editor Tool)")]
        [SerializeField] private List<AudioClip> sfxClips = new List<AudioClip>();
        [SerializeField] private List<AudioClip> bgmClips = new List<AudioClip>();

        // 런타임 딕셔너리 매핑
        private readonly Dictionary<SFXType, AudioClip> _sfxDict = new Dictionary<SFXType, AudioClip>();
        private readonly Dictionary<BGMType, AudioClip> _bgmDict = new Dictionary<BGMType, AudioClip>();

        // SFX 풀 관리
        private readonly List<AudioSource> _sfxPool = new List<AudioSource>();
        private readonly List<float> _sfxStartTimes = new List<float>();

        // BGM 채널 관리 (교차 페이딩용 2채널)
        private AudioSource _bgmSourceA;
        private AudioSource _bgmSourceB;
        private AudioSource _activeBgmSource;
        private AudioSource _inactiveBgmSource;
        private Coroutine _bgmFadeCoroutine;

        // 현재 활성화된 BGM의 고유 원래 볼륨 캐싱
        private float _activeBgmBaseVolume = 1f;

        // 볼륨 접근 프로퍼티
        public float MasterVolume => masterVolume;
        public float BgmVolume => bgmVolume;
        public float SfxVolume => sfxVolume;

#if UNITY_EDITOR
        public string SfxFolderPath { get => sfxFolderPath; set => sfxFolderPath = value; }
        public string BgmFolderPath { get => bgmFolderPath; set => bgmFolderPath = value; }
        public List<AudioClip> SfxClips => sfxClips;
        public List<AudioClip> BgmClips => bgmClips;
#endif

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogWarning($"[SoundManager] 이미 다른 인스턴스가 존재하므로 이 오브젝트를 파괴합니다: {gameObject.name}");
                Destroy(gameObject);
                return;
            }

            InitializeBGMSources();
            InitializeSFXPool();
            InitializeDictionaries();
        }

        /// <summary>
        /// BGM 재생용 오디오 소스를 초기화합니다.
        /// </summary>
        private void InitializeBGMSources()
        {
            if (_bgmSourceA == null)
            {
                GameObject go = new GameObject("BGM_Source_A");
                go.transform.SetParent(transform);
                _bgmSourceA = go.AddComponent<AudioSource>();
                _bgmSourceA.playOnAwake = false;
                _bgmSourceA.loop = true;
            }

            if (_bgmSourceB == null)
            {
                GameObject go = new GameObject("BGM_Source_B");
                go.transform.SetParent(transform);
                _bgmSourceB = go.AddComponent<AudioSource>();
                _bgmSourceB.playOnAwake = false;
                _bgmSourceB.loop = true;
            }

            _activeBgmSource = _bgmSourceA;
            _inactiveBgmSource = _bgmSourceB;
        }

        /// <summary>
        /// 지정된 크기만큼 SFX 오디오 소스 풀을 미리 생성합니다.
        /// </summary>
        private void InitializeSFXPool()
        {
            _sfxPool.Clear();
            _sfxStartTimes.Clear();

            for (int i = 0; i < initialPoolSize; i++)
            {
                AudioSource src = CreateNewSFXAudioSource();
                _sfxPool.Add(src);
                _sfxStartTimes.Add(0f);
            }
        }

        private AudioSource CreateNewSFXAudioSource()
        {
            GameObject go = new GameObject($"SFX_Source_{_sfxPool.Count}");
            go.transform.SetParent(transform);
            AudioSource src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            return src;
        }

        /// <summary>
        /// 동기화된 클립 목록을 기반으로 런타임 매핑 딕셔너리를 구축합니다.
        /// </summary>
        private void InitializeDictionaries()
        {
            _sfxDict.Clear();
            foreach (var clip in sfxClips)
            {
                if (clip == null) continue;
                string enumName = SanitizeName(clip.name);
                if (Enum.TryParse<SFXType>(enumName, out var type))
                {
                    if (!_sfxDict.ContainsKey(type))
                    {
                        _sfxDict.Add(type, clip);
                    }
                    else
                    {
                        Debug.LogWarning($"[SoundManager] 중복된 SFXType 등록 감지: {type} (파일명: {clip.name})");
                    }
                }
            }

            _bgmDict.Clear();
            foreach (var clip in bgmClips)
            {
                if (clip == null) continue;
                string enumName = SanitizeName(clip.name);
                if (Enum.TryParse<BGMType>(enumName, out var type))
                {
                    if (!_bgmDict.ContainsKey(type))
                    {
                        _bgmDict.Add(type, clip);
                    }
                    else
                    {
                        Debug.LogWarning($"[SoundManager] 중복된 BGMType 등록 감지: {type} (파일명: {clip.name})");
                    }
                }
            }
        }

        #region Volume Control API

        public void SetMasterVolume(float value)
        {
            masterVolume = Mathf.Clamp01(value);
            UpdateActiveBGMVolume();
        }

        public void SetBGMVolume(float value)
        {
            bgmVolume = Mathf.Clamp01(value);
            UpdateActiveBGMVolume();
        }

        public void SetSFXVolume(float value)
        {
            sfxVolume = Mathf.Clamp01(value);
        }

        /// <summary>
        /// BGM 볼륨이나 마스터 볼륨 변경 시, 현재 재생 중인 BGM 소스의 실시간 볼륨에 즉시 반영합니다.
        /// </summary>
        private void UpdateActiveBGMVolume()
        {
            if (_activeBgmSource != null && _activeBgmSource.isPlaying && _bgmFadeCoroutine == null)
            {
                _activeBgmSource.volume = _activeBgmBaseVolume * bgmVolume * masterVolume;
            }
        }

        #endregion

        /// <summary>
        /// 지정한 SFX 타입의 사운드를 재생합니다.
        /// </summary>
        public void PlaySFX(SFXType type, float volume = 1f, float pitch = 1f)
        {
            if (type == SFXType.None) return;

            if (!_sfxDict.TryGetValue(type, out var clip) || clip == null)
            {
                Debug.LogWarning($"[SoundManager] SFX 클립을 찾을 수 없습니다: {type}");
                return;
            }

            PlaySFXClip(clip, volume, pitch);
        }

        private void PlaySFXClip(AudioClip clip, float volume, float pitch)
        {
            AudioSource source = null;
            int index = -1;

            // 1. 현재 재생 중이지 않은 대기 상태의 오디오 소스를 찾습니다.
            for (int i = 0; i < _sfxPool.Count; i++)
            {
                if (!_sfxPool[i].isPlaying)
                {
                    source = _sfxPool[i];
                    index = i;
                    break;
                }
            }

            // 2. 대기 소스가 없고 최대 크기 미만인 경우, 풀을 1개 늘려서 사용합니다.
            if (source == null && _sfxPool.Count < maxPoolSize)
            {
                source = CreateNewSFXAudioSource();
                _sfxPool.Add(source);
                _sfxStartTimes.Add(0f);
                index = _sfxPool.Count - 1;
            }

            // 3. 동시 동작 개수 제한에 도달하여 대기 소스가 없다면 가장 오래된 사운드를 강제 중지하고 재사용합니다.
            if (source == null)
            {
                float oldestTime = float.MaxValue;
                int oldestIndex = 0;

                for (int i = 0; i < _sfxPool.Count; i++)
                {
                    if (_sfxStartTimes[i] < oldestTime)
                    {
                        oldestTime = _sfxStartTimes[i];
                        oldestIndex = i;
                    }
                }

                source = _sfxPool[oldestIndex];
                source.Stop();
                index = oldestIndex;
                Debug.LogWarning($"[SoundManager] SFX 동시 재생 최대치({maxPoolSize}) 도달. 가장 오래된 사운드를 강제 중지하고 재활용합니다: {source.name}");
            }

            // 오디오 소스 설정 및 재생 (마스터 볼륨 * SFX 볼륨을 곱하여 최종 출력 결정)
            source.clip = clip;
            source.volume = volume * sfxVolume * masterVolume;
            source.pitch = pitch;
            source.Play();
            _sfxStartTimes[index] = Time.time;
        }

        /// <summary>
        /// 지정한 BGM 타입의 사운드를 페이드하며 재생합니다.
        /// </summary>
        public void PlayBGM(BGMType type, bool loop = true, float volume = 1f, float fadeDuration = 0.5f)
        {
            if (type == BGMType.None)
            {
                StopBGM(fadeDuration);
                return;
            }

            if (!_bgmDict.TryGetValue(type, out var clip) || clip == null)
            {
                Debug.LogWarning($"[SoundManager] BGM 클립을 찾을 수 없습니다: {type}");
                return;
            }

            PlayBGMClip(clip, loop, volume, fadeDuration);
        }

        private void PlayBGMClip(AudioClip clip, bool loop, float targetVolume, float fadeDuration)
        {
            _activeBgmBaseVolume = targetVolume;

            // 동일한 BGM이 이미 활성화되어 플레이 중인 경우, 볼륨/루프 속성만 갱신하고 생략
            if (_activeBgmSource.clip == clip && _activeBgmSource.isPlaying)
            {
                _activeBgmSource.loop = loop;
                _activeBgmSource.volume = targetVolume * bgmVolume * masterVolume;
                return;
            }

            if (_bgmFadeCoroutine != null)
            {
                StopCoroutine(_bgmFadeCoroutine);
            }

            _bgmFadeCoroutine = StartCoroutine(CrossFadeBGM(clip, loop, targetVolume, fadeDuration));
        }

        private IEnumerator CrossFadeBGM(AudioClip newClip, bool loop, float targetVolume, float duration)
        {
            var prevSource = _activeBgmSource;
            var nextSource = _inactiveBgmSource;

            // 활성/비활성 오디오 소스 스왑
            _activeBgmSource = nextSource;
            _inactiveBgmSource = prevSource;

            nextSource.clip = newClip;
            nextSource.loop = loop;
            nextSource.volume = 0f;

            if (newClip != null)
            {
                nextSource.Play();
            }

            float elapsed = 0f;
            float startPrevVolume = prevSource.volume;

            if (duration > 0f)
            {
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float percent = elapsed / duration;

                    // 현재 프레임의 마스터/BGM 볼륨 반영
                    float currentBgmTargetVolume = targetVolume * bgmVolume * masterVolume;

                    prevSource.volume = Mathf.Lerp(startPrevVolume, 0f, percent);
                    nextSource.volume = Mathf.Lerp(0f, currentBgmTargetVolume, percent);

                    yield return null;
                }
            }

            prevSource.volume = 0f;
            prevSource.Stop();
            prevSource.clip = null;

            nextSource.volume = targetVolume * bgmVolume * masterVolume;
            _bgmFadeCoroutine = null;
        }

        /// <summary>
        /// 재생 중인 BGM을 부드럽게 페이드아웃 후 정지합니다.
        /// </summary>
        public void StopBGM(float fadeDuration = 0.5f)
        {
            _activeBgmBaseVolume = 0f;
            if (_bgmFadeCoroutine != null)
            {
                StopCoroutine(_bgmFadeCoroutine);
            }

            _bgmFadeCoroutine = StartCoroutine(FadeOutBGM(fadeDuration));
        }

        private IEnumerator FadeOutBGM(float duration)
        {
            float elapsed = 0f;
            float startVolumeA = _bgmSourceA.volume;
            float startVolumeB = _bgmSourceB.volume;

            if (duration > 0f)
            {
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float percent = elapsed / duration;

                    _bgmSourceA.volume = Mathf.Lerp(startVolumeA, 0f, percent);
                    _bgmSourceB.volume = Mathf.Lerp(startVolumeB, 0f, percent);

                    yield return null;
                }
            }

            _bgmSourceA.volume = 0f;
            _bgmSourceA.Stop();
            _bgmSourceA.clip = null;

            _bgmSourceB.volume = 0f;
            _bgmSourceB.Stop();
            _bgmSourceB.clip = null;

            _bgmFadeCoroutine = null;
        }

        /// <summary>
        /// 파일 이름을 C# Enum 식별자로 전환하기 위해 불필요한 특수 문자를 정제합니다.
        /// </summary>
        public static string SanitizeName(string originalName)
        {
            if (string.IsNullOrEmpty(originalName)) return "None";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            // 숫자로 시작하는 경우 앞에 언더바 추가
            if (char.IsDigit(originalName[0]))
            {
                sb.Append('_');
            }

            foreach (char c in originalName)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append('_');
                }
            }

            string result = sb.ToString();
            while (result.Contains("__"))
            {
                result = result.Replace("__", "_");
            }

            result = result.Trim('_');
            return string.IsNullOrEmpty(result) ? "None" : result;
        }
    }
}
