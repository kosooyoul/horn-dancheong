using UnityEngine;

// 유닛 마커가 목표 월드 좌표로 부드럽게(보간) 이동하도록 돕는 컴포넌트.
// 그리드 한 칸씩 호출되며, 여러 칸이 연속으로 들어와도 큐에 쌓아 차례대로 미끄러지듯 이동한다.
public class UnitMover : MonoBehaviour
{
    [Tooltip("이동 속도 (월드 단위/초)")]
    [SerializeField] private float moveSpeed = 6f;

    [Tooltip("목표에 이만큼 가까워지면 도착으로 간주")]
    [SerializeField] private float arriveThreshold = 0.001f;
    
    [Tooltip("회전 속도 (도/초)")]
    [SerializeField] private float rotationSpeed = 360f;
    
    [Tooltip("이동 시 자동으로 이동 방향을 바라보도록 회전")]
    [SerializeField] private bool autoRotateToMoveDirection = true;
    
    [Tooltip("회전 방향 제한 (4: 상하좌우, 8: 8방향, 0: 제한없음)")]
    [SerializeField] private int directionCount = 4;
    
    [Tooltip("이 각도 이내로 회전하면 이동을 시작 (도 단위)")]
    [SerializeField] private float earlyMovementThreshold = 30f;

    // 연속 이동 입력을 순서대로 처리하기 위한 목표 지점 큐
    private readonly System.Collections.Generic.Queue<Vector3> targetQueue = new System.Collections.Generic.Queue<Vector3>();
    private Vector3 currentTarget;
    private bool hasTarget;
    
    // 회전 관련
    private Quaternion targetRotation;
    private bool isRotating;
    private float rotationStartTime;
    private bool canStartMovement; // 회전이 충분히 진행되어 이동을 시작할 수 있는지

    // 현재 위치 이동(보간) 중인지 여부 — 입력 잠금 등에 활용 가능
    // 주의: 회전 상태는 포함하지 않음 (회전 중에도 메뉴 입력 허용)
    public bool IsMoving => hasTarget || targetQueue.Count > 0;
    
    // 현재 회전 중인지 여부
    public bool IsRotating => isRotating;
    
    // 이동 또는 회전 중인지 여부 (필요시 사용)
    public bool IsMovingOrRotating => IsMoving || IsRotating;
    
    // 이동을 시작할 수 있는지 여부 (회전이 충분히 진행됨)
    public bool CanStartMovement => canStartMovement || !isRotating;

    public void SetMoveSpeed(float speed)
    {
        if (speed > 0f) moveSpeed = speed;
    }
    
    public void SetRotationSpeed(float speed)
    {
        if (speed > 0f) rotationSpeed = speed;
    }
    
    public void SetAutoRotate(bool autoRotate)
    {
        autoRotateToMoveDirection = autoRotate;
    }
    
    public void SetDirectionCount(int count)
    {
        directionCount = Mathf.Max(0, count);
    }
    
    public void SetEarlyMovementThreshold(float threshold)
    {
        earlyMovementThreshold = Mathf.Clamp(threshold, 0f, 180f);
    }

    // 목표 월드 좌표로 부드럽게 이동하도록 예약한다.
    public void MoveTo(Vector3 worldPosition)
    {
        targetQueue.Enqueue(worldPosition);
    }
    
    // 특정 방향을 바라보도록 회전한다.
    public void LookTowards(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.001f) return;
        
        // Y축 회전만 적용 (탑뷰이므로)
        direction.y = 0f;
        direction.Normalize();
        
        // 방향 제한 적용
        Vector3 constrainedDirection = ConstrainDirection(direction);
        targetRotation = Quaternion.LookRotation(constrainedDirection, Vector3.up);
        
        // 이미 올바른 방향을 바라보고 있다면 회전하지 않음
        if (Quaternion.Angle(transform.rotation, targetRotation) < 5f)
        {
            transform.rotation = targetRotation;
            isRotating = false;
            canStartMovement = true;
            return;
        }
        
        isRotating = true;
        canStartMovement = false;
        rotationStartTime = Time.time;
    }
    
    // 방향을 제한된 방향으로 스냅한다.
    private Vector3 ConstrainDirection(Vector3 direction)
    {
        if (directionCount <= 0) return direction; // 제한 없음
        
        // 현재 방향을 각도로 변환 (Y축 기준 회전)
        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        if (angle < 0f) angle += 360f;
        
        // 방향 간격 계산
        float angleStep = 360f / directionCount;
        
        // 가장 가까운 방향으로 스냅
        float snappedAngle = Mathf.Round(angle / angleStep) * angleStep;
        
        // 각도를 다시 벡터로 변환
        float radians = snappedAngle * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians));
    }

    // 보간 없이 즉시 위치를 맞춘다 (배치/리셋용).
    public void SnapTo(Vector3 worldPosition)
    {
        targetQueue.Clear();
        hasTarget = false;
        isRotating = false;
        canStartMovement = true;
        transform.position = worldPosition;
    }

    private void Update()
    {
        UpdateMovement();
        UpdateRotation();
    }
    
    private void UpdateMovement()
    {
        if (!hasTarget)
        {
            if (targetQueue.Count == 0) return;
            currentTarget = targetQueue.Dequeue();
            hasTarget = true;
            
            // 자동 회전이 활성화되어 있다면 이동 방향을 계산하여 회전
            if (autoRotateToMoveDirection)
            {
                Vector3 moveDirection = currentTarget - transform.position;
                if (moveDirection.sqrMagnitude > 0.001f)
                {
                    LookTowards(moveDirection);
                }
            }
        }

        // 회전이 충분히 진행되었거나 회전이 없을 때만 이동
        if (CanStartMovement)
        {
            Vector3 next = Vector3.MoveTowards(transform.position, currentTarget, moveSpeed * Time.deltaTime);
            transform.position = next;

            if ((transform.position - currentTarget).sqrMagnitude <= arriveThreshold * arriveThreshold)
            {
                transform.position = currentTarget;
                hasTarget = false;
            }
        }
    }
    
    private void UpdateRotation()
    {
        if (!isRotating) return;
        
        // 회전 타임아웃 체크 (최대 2초)
        if (Time.time - rotationStartTime > 2f)
        {
            transform.rotation = targetRotation;
            isRotating = false;
            canStartMovement = true;
            Debug.LogWarning("[UnitMover] 회전 타임아웃 - 강제 완료");
            return;
        }
        
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        
        float currentAngle = Quaternion.Angle(transform.rotation, targetRotation);
        
        // 조기 이동 허용 체크
        if (!canStartMovement && currentAngle <= earlyMovementThreshold)
        {
            canStartMovement = true;
        }
        
        // 회전 완료 조건을 더 관대하게 설정 (5도 이내)
        if (currentAngle < 5f)
        {
            transform.rotation = targetRotation;
            isRotating = false;
            canStartMovement = true;
        }
    }
}
