using UnityEngine;

public class FollowSimulatedCamera : MonoBehaviour
{
    [Header("XR Camera (HMD)")]
    public Transform cameraTransform; // XROrigin 하위 Main Camera 지정

    private Vector3 lastCameraLocalPos;

    void Start()
    {
        if (cameraTransform != null)
            lastCameraLocalPos = cameraTransform.localPosition;
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        // 카메라가 이동한 만큼 Origin도 이동시킴
        Vector3 delta = cameraTransform.localPosition - lastCameraLocalPos;
        transform.position += delta;

        // 카메라의 로컬 위치 초기화
        cameraTransform.localPosition -= delta;

        lastCameraLocalPos = cameraTransform.localPosition;
    }
}
