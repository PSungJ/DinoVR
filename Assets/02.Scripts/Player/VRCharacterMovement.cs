using UnityEngine;
using UnityEngine.InputSystem;

public class VRAnimatorController : MonoBehaviour
{
    [Header("VR Input & Animator Settings")]
    [Tooltip("왼쪽 컨트롤러 엄지 스틱의 Vector2 입력 액션입니다. (예: XRI LeftHand/Move)")]
    // !!! 오른쪽에서 왼쪽으로 변경되었습니다 !!!
    public InputActionProperty moveAction;

    [Tooltip("캐릭터의 Animator 컴포넌트입니다.")]
    public Animator animator;

    [Tooltip("엄지 스틱의 최대 입력(1.0)을 Animator Speed의 어떤 값으로 매핑할지 결정합니다. (6.0 권장: 2 이상 걷기, 6 이상 달리기)")]
    public float maxAnimatorSpeedValue = 6.0f;

    // Animator 파라미터 이름 (인스펙터에서 수정 가능)
    private readonly int speedParameterHash = Animator.StringToHash("Speed");

    void OnEnable()
    {
        // 입력 액션을 활성화합니다.
        if (moveAction.action != null)
        {
            moveAction.action.Enable();
        }
    }

    void OnDisable()
    {
        // 입력 액션을 비활성화합니다.
        // 캐릭터가 움직이지 않을 때 Speed를 0으로 설정하여 Idle 상태로 돌아가도록 합니다.
        if (animator != null)
        {
            animator.SetFloat(speedParameterHash, 0f);
        }

        if (moveAction.action != null)
        {
            moveAction.action.Disable();
        }
    }

    void Update()
    {
        // 1. 엄지 스틱 입력 값 (Vector2) 읽기
        Vector2 currentInput = Vector2.zero;
        if (moveAction.action != null)
        {
            currentInput = moveAction.action.ReadValue<Vector2>();
        }

        // 2. 입력의 크기 (Magnitude) 계산 (0.0 ~ 1.0)
        float inputMagnitude = currentInput.magnitude;

        // 3. Animator에 전달할 Speed 값 계산
        float animatorSpeedValue = inputMagnitude * maxAnimatorSpeedValue;

        // 4. Animator 파라미터 업데이트
        if (animator != null)
        {
            animator.SetFloat(speedParameterHash, animatorSpeedValue);
        }

        // ⭐ ⭐ ⭐ 추가된 디버그 코드 ⭐ ⭐ ⭐
        // 매 프레임 너무 많은 로그가 찍히는 것을 방지하기 위해 0.01 이상일 때만 로그를 찍거나, 
        // Debug.LogFormat을 사용하여 더 자세한 정보를 출력할 수 있습니다.
        if (animatorSpeedValue > 0.01f)
        {
            Debug.Log($"[VRAnimatorController] Input Magnitude: {inputMagnitude:F2}, Animator Speed: {animatorSpeedValue:F2}");
        }
    }
}