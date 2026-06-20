using UnityEngine;

// 카메라가 맵 중앙(원점)을 위에서 내려다보도록 하는 컴포넌트.
[RequireComponent(typeof(Camera))]
public class BattleCameraFollow : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("비우면 씬에서 자동으로 찾는다.")]
    [SerializeField] private BattleScript battleScript;

    [Header("추적 설정")]
    [Tooltip("대상까지 따라가는 데 걸리는 시간(작을수록 빠름).")]
    [SerializeField] private float smoothTime = 0.2f;

    [Header("카메라 구도 (위에서 내려다보기)")]
    [Tooltip("대상 기준 오프셋(월드 좌표). y가 높이, z가 거리. 높이와 거리(z 절댓값)가 같으면 정확히 45°.")]
    private Vector3 cameraOffset = new Vector3(0f, 12f, -12f);

    [Tooltip("카메라 회전(오일러 각). X 45°면 대각선 45° 내려다보기, 90°면 수직 탑다운.")]
    private Vector3 cameraEulerAngles = new Vector3(45f, 0f, 0f);

    [Tooltip("켜면 카메라가 항상 대상을 바라보도록 회전을 자동 계산한다(위 회전값 무시).")]
    private bool lookAtTarget = false;

    [Header("투영 설정")]
    [Tooltip("켜면 직교(orthographic) 투영으로 보여준다.")]
    private bool useOrthographic = true;

    [Tooltip("직교 투영 시 세로 절반 높이(클수록 더 넓은 영역이 보임).")]
    private float orthographicSize = 5f;

    private Camera cachedCamera;
    private Vector3 followVelocity;

    private void Start()
    {
        if (battleScript == null)
        {
            battleScript = FindObjectOfType<BattleScript>();
        }

        ApplyProjection();
        ApplyRotation(GetTargetPosition());
        // 시작 즉시 올바른 위치로 스냅(첫 프레임에 엉뚱한 곳을 비추지 않도록)
        transform.position = GetTargetPosition() + cameraOffset;
    }

    // 직교/원근 투영을 코드에서 강제 적용 (인스펙터 직렬화 값에 막히지 않도록)
    private void ApplyProjection()
    {
        if (cachedCamera == null)
        {
            cachedCamera = GetComponent<Camera>();
        }

        cachedCamera.orthographic = useOrthographic;
        if (useOrthographic)
        {
            cachedCamera.orthographicSize = orthographicSize;
        }
    }

    private void LateUpdate()
    {
        if (battleScript == null) return;

        Vector3 targetPosition = GetTargetPosition();
        Vector3 desiredPosition = targetPosition + cameraOffset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref followVelocity, smoothTime);

        ApplyRotation(targetPosition);
    }

    // 추적 대상 위치 — BattleScript는 맵을 원점 중심으로 생성하므로 (0,0,0)이 맵 중앙
    private Vector3 GetTargetPosition()
    {
        return Vector3.zero;
    }

    private void ApplyRotation(Vector3 targetPosition)
    {
        if (lookAtTarget)
        {
            Vector3 direction = targetPosition - transform.position;
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }
            return;
        }

        transform.rotation = Quaternion.Euler(cameraEulerAngles);
    }
}
