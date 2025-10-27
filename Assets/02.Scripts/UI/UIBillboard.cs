using UnityEngine;

/// <summary>
/// UI를 지정된 Pivot 위치에 고정하고, 
/// 항상 Target(플레이어 카메라)을 부드럽게 바라보게 하는 빌보드 스크립트.
/// UI의 앞면이 플레이어를 향하도록 180도 회전 보정 포함.
/// </summary>
public class UIBillboard : MonoBehaviour
{
    [Header("Target & Pivot")]
    [SerializeField] private Transform targetTransform;  // 플레이어 카메라
    [SerializeField] private Transform pivotTransform;   // UI 위치 기준점

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 5f;   // 회전 보간 속도

    void Start()
    {
        // 타겟 자동 지정
        if (targetTransform == null)
        {
            if (Camera.main != null)
            {
                targetTransform = Camera.main.transform;
                Debug.LogWarning("[UIBillboard] Target Transform이 설정되지 않아 MainCamera를 자동 지정했습니다.");
            }
            else
            {
                Debug.LogError("[UIBillboard] Target Transform이 설정되지 않았고 MainCamera도 없습니다.");
            }
        }

        if (pivotTransform == null)
        {
            Debug.LogWarning("[UIBillboard] Pivot Transform이 설정되지 않았습니다. 현재 위치에 고정됩니다.");
        }
    }

    void LateUpdate()
    {
        if (targetTransform == null) return;

        // --- 1. 부드러운 회전 ---
        Vector3 directionToTarget = targetTransform.position - transform.position;

        // 기본적으로 카메라를 바라보는 회전
        Quaternion baseRotation = Quaternion.LookRotation(directionToTarget);

        // UI의 앞면이 카메라를 향하도록 180도 보정
        Quaternion flipRotation = Quaternion.Euler(0, 180, 0);
        Quaternion targetRotation = baseRotation * flipRotation;

        // Slerp로 부드럽게 회전 보간
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * rotationSpeed
        );

        // --- 2. 위치 고정 ---
        if (pivotTransform != null)
        {
            transform.position = pivotTransform.position;
        }
    }
}
