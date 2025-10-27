using UnityEngine;

/// <summary>
/// 퀵슬롯 UI를 손목 Pivot에 따라 움직이게 하다가,
/// 인벤토리가 열리면 원래 위치/회전/크기로 되돌리는 스크립트.
/// 평소에는 카메라를 부드럽게 바라보도록 회전.
/// </summary>
public class QuickSlotBillboard : MonoBehaviour
{
    [Header("Target & Pivot")]
    [SerializeField] private Transform targetCamera;     // 플레이어 카메라
    [SerializeField] private Transform pivotTransform;   // 손목 Pivot (인벤토리 닫혔을 때 따라다님)

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 5f;   // Slerp 회전 속도

    [Header("Scale Settings")]
    [SerializeField] private Vector3 pivotScale = new Vector3(0.06f, 0.06f, 0.06f); // 손목 상태 크기

    // --- 내부 상태 ---
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 initialScale;

    private bool followPivot = true; // Pivot을 따라다니는 중인지
    private bool lookAtCamera = true; // 카메라를 바라볼지 여부

    void Start()
    {
        // --- 카메라 자동 설정 ---
        if (targetCamera == null)
        {
            if (Camera.main != null)
            {
                targetCamera = Camera.main.transform;
                Debug.LogWarning("[QuickSlotBillboard] targetCamera가 설정되지 않아 MainCamera를 자동 지정했습니다.");
            }
            else
            {
                Debug.LogError("[QuickSlotBillboard] targetCamera가 설정되지 않았고 MainCamera도 없습니다.");
            }
        }

        // --- 초기 상태 저장 ---
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialScale = transform.localScale;

        // --- 시작 시 손목 상태로 설정 ---
        followPivot = true;
        lookAtCamera = true;
        transform.localScale = pivotScale;

        Debug.Log("[QuickSlotBillboard] 초기화 완료 — 손목 크기로 설정됨.");
    }

    void LateUpdate()
    {
        // --- 회전 처리 ---
        if (lookAtCamera && targetCamera != null)
        {
            Vector3 directionToCamera = targetCamera.position - transform.position;
            Quaternion baseRotation = Quaternion.LookRotation(directionToCamera);
            Quaternion flipRotation = Quaternion.Euler(0, 180, 0);
            Quaternion targetRotation = baseRotation * flipRotation;

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * rotationSpeed
            );
        }

        // --- 위치 처리 ---
        if (followPivot && pivotTransform != null)
        {
            transform.position = pivotTransform.position;
        }
    }

    // ===========================================================
    // 🔸 외부에서 호출되는 함수 (QuickSlotManager에서 제어)
    // ===========================================================

    /// <summary>
    /// 인벤토리 UI가 열릴 때 호출 → 원래 위치/회전/크기로 복귀하고 카메라 회전 중단
    /// </summary>
    public void DetachFromPivot()
    {
        followPivot = false;
        lookAtCamera = false;

        transform.position = initialPosition;
        transform.rotation = initialRotation;
        transform.localScale = initialScale;

        Debug.Log("[QuickSlotBillboard] Pivot에서 분리 — 초기 위치/회전/크기로 복귀 및 회전 고정.");
    }

    /// <summary>
    /// 인벤토리 UI가 닫힐 때 호출 → Pivot을 따라가며 손목 크기로 변경하고 카메라 회전 재개
    /// </summary>
    public void AttachToPivot()
    {
        followPivot = true;
        lookAtCamera = true;

        transform.localScale = pivotScale;

        Debug.Log("[QuickSlotBillboard] Pivot에 연결 — 손목 크기로 전환 및 카메라 추적 재개.");
    }
}
