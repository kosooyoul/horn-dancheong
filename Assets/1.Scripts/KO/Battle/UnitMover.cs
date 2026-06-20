using UnityEngine;

// 유닛 마커가 목표 월드 좌표로 부드럽게(보간) 이동하도록 돕는 컴포넌트.
// 그리드 한 칸씩 호출되며, 여러 칸이 연속으로 들어와도 큐에 쌓아 차례대로 미끄러지듯 이동한다.
public class UnitMover : MonoBehaviour
{
    [Tooltip("이동 속도 (월드 단위/초)")]
    [SerializeField] private float moveSpeed = 6f;

    [Tooltip("목표에 이만큼 가까워지면 도착으로 간주")]
    [SerializeField] private float arriveThreshold = 0.001f;

    // 연속 이동 입력을 순서대로 처리하기 위한 목표 지점 큐
    private readonly System.Collections.Generic.Queue<Vector3> targetQueue = new System.Collections.Generic.Queue<Vector3>();
    private Vector3 currentTarget;
    private bool hasTarget;

    // 현재 이동(보간) 중인지 여부 — 입력 잠금 등에 활용 가능
    public bool IsMoving => hasTarget || targetQueue.Count > 0;

    public void SetMoveSpeed(float speed)
    {
        if (speed > 0f) moveSpeed = speed;
    }

    // 목표 월드 좌표로 부드럽게 이동하도록 예약한다.
    public void MoveTo(Vector3 worldPosition)
    {
        targetQueue.Enqueue(worldPosition);
    }

    // 보간 없이 즉시 위치를 맞춘다 (배치/리셋용).
    public void SnapTo(Vector3 worldPosition)
    {
        targetQueue.Clear();
        hasTarget = false;
        transform.position = worldPosition;
    }

    private void Update()
    {
        if (!hasTarget)
        {
            if (targetQueue.Count == 0) return;
            currentTarget = targetQueue.Dequeue();
            hasTarget = true;
        }

        Vector3 next = Vector3.MoveTowards(transform.position, currentTarget, moveSpeed * Time.deltaTime);
        transform.position = next;

        if ((transform.position - currentTarget).sqrMagnitude <= arriveThreshold * arriveThreshold)
        {
            transform.position = currentTarget;
            hasTarget = false;
        }
    }
}
