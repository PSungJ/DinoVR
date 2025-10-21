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

    private SkinnedMeshRenderer skinnedMeshRenderer;
    [SerializeField]private XRGrabInteractable grabInteractable;
    private Material[] originalMaterials;

    void Awake()
    {
        // 1. SkinnedMeshRenderer 찾기 (스크립트가 부착된 오브젝트)
        skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        if (skinnedMeshRenderer == null)
        {
            Debug.LogError("SkinnedMeshRenderer 컴포넌트가 현재 오브젝트에 없습니다. 아웃라인을 적용할 수 없습니다.");
            return;
        }

        // 2. XRGrabInteractable 찾기 (부모 오브젝트)
        // GetComponentInParent를 사용하여 부모 오브젝트(혹은 자기 자신)에서 찾습니다.
        grabInteractable = GetComponentInParent<XRGrabInteractable>();
        if (grabInteractable == null)
        {
            Debug.LogError("부모 오브젝트를 포함하여 XRGrabInteractable 컴포넌트가 발견되지 않았습니다. 이 스크립트는 해당 컴포넌트와 함께 사용해야 합니다.");
            return;
        }

        // 3. 초기 매터리얼 백업 및 기본 매터리얼 설정
        originalMaterials = skinnedMeshRenderer.sharedMaterials;

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
        if (skinnedMeshRenderer == null || outlineMaterial == null) return;

        // 쉐이더 속성 최신화
        outlineMaterial.SetFloat("_OutlineWidth", outlineWidth);
        outlineMaterial.SetColor("_OutlineColor", outlineColor);

        // 아웃라인 매터리얼 + 본체 매터리얼 순서로 배열 병합
        Material[] finalMaterials = new Material[] { outlineMaterial }.Concat(baseMaterials).ToArray();

        skinnedMeshRenderer.sharedMaterials = finalMaterials;
    }

    // 아웃라인 비활성화 로직 (원래 매터리얼로 복구)
    public void DisableOutline()
    {
        if (skinnedMeshRenderer != null && originalMaterials != null)
        {
            skinnedMeshRenderer.sharedMaterials = originalMaterials;
        }
    }
}