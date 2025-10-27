using UnityEngine;
using UnityEngine.InputSystem;

public class VRAnimatorController : MonoBehaviour
{
    [Header("VR Input & Animator Settings")]
    [Tooltip("왼쪽 컨트롤러 엄지 스틱의 Vector2 입력 액션입니다. (예: XRI LeftHand/Move)")]
    public InputActionProperty moveAction;

    [Tooltip("오른쪽 컨트롤러 엄지 스틱의 Vector2 입력 액션입니다. (예: XRI RightHand/Turn)")]
    // ⭐ ⭐ ⭐ 추가된 변수 ⭐ ⭐ ⭐
    public InputActionProperty turnAction;

    [Tooltip("캐릭터의 Animator 컴포넌트입니다.")]
    public Animator animator;

    [Tooltip("엄지 스틱의 최대 입력(1.0)을 Animator Speed의 어떤 값으로 매핑할지 결정합니다. (6.0 권장)")]
    public float maxAnimatorSpeedValue = 6.0f;

    // Animator 파라미터 이름
    private readonly int speedParameterHash = Animator.StringToHash("Speed");

    void OnEnable()
    {
        // 입력 액션들을 활성화합니다.
        if (moveAction.action != null)
        {
            moveAction.action.Enable();
        }
        // ⭐ ⭐ ⭐ 추가된 활성화 ⭐ ⭐ ⭐
        if (turnAction.action != null)
        {
            turnAction.action.Enable();
        }
    }

    void OnDisable()
    {
        // 입력 액션들을 비활성화하고 Animator Speed를 초기화합니다.
        if (animator != null)
        {
            animator.SetFloat(speedParameterHash, 0f);
        }

        if (moveAction.action != null)
        {
            moveAction.action.Disable();
        }
        // ⭐ ⭐ ⭐ 추가된 비활성화 ⭐ ⭐ ⭐
        if (turnAction.action != null)
        {
            turnAction.action.Disable();
        }
    }

    void Update()
    {
        // ====================================================================
        // 1. 왼쪽 스틱 (이동/애니메이션) 처리
        // ====================================================================
        Vector2 currentMoveInput = Vector2.zero;
        if (moveAction.action != null)
        {
            currentMoveInput = moveAction.action.ReadValue<Vector2>();
        }

        float inputMagnitude = currentMoveInput.magnitude;
        float animatorSpeedValue = inputMagnitude * maxAnimatorSpeedValue;

        if (animator != null)
        {
            animator.SetFloat(speedParameterHash, animatorSpeedValue);
        }

        // 왼쪽 스틱 디버그 (기존 로직 유지)
        if (animatorSpeedValue > 0.01f)
        {
            Debug.Log($"[VRAnimatorController] Left Move - Magnitude: {inputMagnitude:F2}, Animator Speed: {animatorSpeedValue:F2}");
        }

        // ====================================================================
        // 2. 오른쪽 스틱 (턴/회전) 디버그 처리 ⭐ ⭐ ⭐ 새로운 로직 ⭐ ⭐ ⭐
        // ====================================================================
        Vector2 currentTurnInput = Vector2.zero;
        if (turnAction.action != null)
        {
            currentTurnInput = turnAction.action.ReadValue<Vector2>();
        }

        // 턴 액션은 주로 X축 (좌우) 값만 사용됩니다.
        float turnX = currentTurnInput.x;

        // 엄지 스틱이 중앙(0)이 아닐 때만 로그를 출력합니다.
        if (Mathf.Abs(turnX) > 0.01f)
        {
            Debug.Log($"[VRAnimatorController] Right Turn - X Value: {turnX:F2}");
        }
    }
}