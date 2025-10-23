// UIBillboard.cs

using UnityEngine;

/// <summary>
/// 이 스크립트는 UI 패널의 위치를 지정된 Pivot 오브젝트에 고정하고, 
/// Slerp를 사용하여 지정된 Target Transform(카메라)을 부드럽게 바라보도록 회전시킵니다.
/// UI의 앞면이 플레이어를 향하도록 180도 보정합니다.
/// </summary>
public class UIBillboard : MonoBehaviour
{
    // [SerializeField] 속성으로 인스펙터에 노출. 플레이어 카메라 Transform
    [SerializeField]
    private Transform targetTransform;

    // UI 패널의 위치를 고정할 Pivot Transform
    [SerializeField]
    private Transform pivotTransform;

    // Slerp 회전 속도 조절 (값이 높을수록 빠르게 목표를 따라감)
    [SerializeField]
    private float rotationSpeed = 5f;

    void Start()
    {
        // Target Transform 설정 확인 및 대체 로직 유지
        if (targetTransform == null)
        {
            if (Camera.main != null)
            {
                targetTransform = Camera.main.transform;
                Debug.LogWarning("Target Transform이 설정되지 않아, 'MainCamera'를 타겟으로 지정했습니다.");
            }
            else
            {
                Debug.LogError("씬에 'MainCamera'가 없거나 Target Transform이 설정되지 않았습니다.");
            }
        }

        if (pivotTransform == null)
        {
            Debug.LogWarning("Pivot Transform이 설정되지 않았습니다. UI 패널의 위치는 현재 위치에 고정됩니다.");
        }
    }

    void Update()
    {
        // 1. 회전 처리 (Slerp 적용)
        if (targetTransform != null)
        {
            // A. 목표 회전 (Target Rotation) 계산
            // 현재 위치에서 타겟을 바라보는 방향 벡터
            Vector3 directionToTarget = targetTransform.position - transform.position;

            // 타겟을 바라보는 기본 회전
            Quaternion baseRotation = Quaternion.LookRotation(directionToTarget);

            // UI의 앞면이 플레이어를 향하도록 180도 회전값 (Y축 기준)을 정의
            Quaternion flipRotation = Quaternion.Euler(0, 180, 0);

            // 최종 목표 회전 (회전 + 반전)
            Quaternion targetRotation = baseRotation * flipRotation;

            // B. Slerp를 사용하여 현재 회전을 목표 회전으로 부드럽게 업데이트
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * rotationSpeed
            );

            // [선택 사항] 패널이 플레이어의 머리 높이 변화에 따라 위아래로 기울어지는 것을 완전히 방지하고 싶다면:
            /*
            Vector3 currentEuler = transform.localEulerAngles;
            currentEuler.x = 0; // X축(피치) 회전 고정
            currentEuler.z = 0; // Z축(롤) 회전 고정
            transform.localEulerAngles = currentEuler;
            */
        }

        // 2. 위치 고정 처리
        if (pivotTransform != null)
        {
            // UI 패널의 위치를 Pivot 오브젝트의 위치로 고정합니다.
            transform.position = pivotTransform.position;
        }
    }
}