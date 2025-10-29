using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Linq;

// 이 스크립트는 XRGrabInteractable이 부모 오브젝트에 있을 때 아웃라인 효과를 제어합니다.
public class OutlineController : MonoBehaviour
{
    [Header("아웃라인 설정")]
    [Tooltip("사용자가 만든 Outline Shader가 적용된 매터리얼을 여기에 할당하세요.")]
    public Material outlineMaterial;

    [Tooltip("원래 오브젝트의 기본 매터리얼 배열입니다. 비어있으면 현재 매터리얼을 사용합니다.")]
    public Material[] baseMaterials;

    [Range(0.01f, 0.1f)]
    public float outlineWidth = 0.03f;
    public Color outlineColor = Color.yellow;

    private Renderer targetRenderer;

    [SerializeField]private XRGrabInteractable grabInteractable;
    private Material[] originalMaterials;

    void Awake()
    {
        // 공통 Renderer 컴포넌트 찾기
        targetRenderer = GetComponent<Renderer>();
        if (targetRenderer == null)
        {
            Debug.LogError("Renderer 컴포넌트가 없습니다. 아웃라인을 적용할 수 없습니다.");
            return;
        }

        // Grab 인터랙터 찾기
        grabInteractable = GetComponentInParent<XRGrabInteractable>();
        if (grabInteractable == null)
        {
            Debug.LogError("부모 오브젝트에 XRGrabInteractable이 없습니다.");
            return;
        }

        // 매터리얼 초기화
        originalMaterials = targetRenderer.sharedMaterials;
        if (baseMaterials == null || baseMaterials.Length == 0)
        {
            baseMaterials = originalMaterials;
        }
    }
    void Start()
    {
        // 4. XRGrabInteractable의 Hover 이벤트 리스너 등록
        grabInteractable.hoverEntered.AddListener(OnHoverEntered);
        grabInteractable.hoverExited.AddListener(OnHoverExited);

        // 시작 시에는 아웃라인을 비활성화 상태로 유지
        DisableOutline();
    }

    void OnDestroy()
    {
        // 이벤트 리스너 정리
        if (grabInteractable != null)
        {
            grabInteractable.hoverEntered.RemoveListener(OnHoverEntered);
            grabInteractable.hoverExited.RemoveListener(OnHoverExited);
        }
    }

    void OnValidate()
    {
        if (outlineMaterial != null)
        {
            outlineMaterial.SetFloat("_OutlineWidth", outlineWidth);
            outlineMaterial.SetColor("_OutlineColor", outlineColor);
        }
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        EnableOutline();
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        // 잡혀있는 동안에는 아웃라인이 꺼지지 않도록 방지
        if (!grabInteractable.isSelected)
        {
            DisableOutline();
        }
    }

    // 아웃라인 활성화 로직
    private void EnableOutline()
    {
        if (targetRenderer == null || outlineMaterial == null) return;

        outlineMaterial.SetFloat("_OutlineWidth", outlineWidth);
        outlineMaterial.SetColor("_OutlineColor", outlineColor);

        Material[] finalMaterials = new Material[] { outlineMaterial }.Concat(baseMaterials).ToArray();
        targetRenderer.sharedMaterials = finalMaterials;
    }

    public void DisableOutline()
    {
        if (targetRenderer != null && originalMaterials != null)
        {
            targetRenderer.sharedMaterials = originalMaterials;
        }
    }

}