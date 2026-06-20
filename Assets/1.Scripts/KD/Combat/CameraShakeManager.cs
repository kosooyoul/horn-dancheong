using System.Collections;
using UnityEngine;

namespace KD
{
    // 카메라 흔들림 전담 컴포넌트
    // SkillData.useCameraShake == true 인 스킬의 임팩트 타이밍에
    // SimpleSkillFxPlayer가 Shake()를 호출한다.
    public class CameraShakeManager : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;

        private Coroutine _shakeCoroutine;

        private void Awake()
        {
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        public void Shake(float duration, float magnitude)
        {
            if (cameraTransform == null) return;
            if (_shakeCoroutine != null)
                StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = StartCoroutine(DoShake(duration, magnitude));
        }

        private IEnumerator DoShake(float duration, float magnitude)
        {
            Vector3 originalPos = cameraTransform.localPosition;
            float   elapsed     = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t        = elapsed / duration;
                float strength = Mathf.Lerp(magnitude, 0f, t);

                float x = Random.Range(-1f, 1f) * strength;
                float z = Random.Range(-1f, 1f) * strength;
                cameraTransform.localPosition = originalPos + new Vector3(x, 0f, z);

                yield return null;
            }

            cameraTransform.localPosition = originalPos;
            _shakeCoroutine = null;
        }
    }
}
