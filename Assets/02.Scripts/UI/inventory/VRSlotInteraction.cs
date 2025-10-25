using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

// InventorySlot 구조체와 Inventory/QuickSlotManager 클래스가 전역에서 참조 가능하다고 가정합니다.

public class VRSlotInteraction : MonoBehaviour
{
    // --- [필드] ---
    [Header("Slot Data")]
    [SerializeField] private int slotIndex = -1;

    private Image slotImage;
    private CustomSlotGrabInteractable grabInteractable;
    private Collider slotCollider;
    private SlotUIUpdater uiUpdater;

    // 3D 모델 렌더링 및 텍스처 동적 변경을 위한 필드
    private MeshRenderer itemMeshRenderer;
    private Material itemModelMaterialInstance;
    private static readonly int BaseMapPropertyID = Shader.PropertyToID("_BaseMap");

    // Layer Switching 필드
    private int originalLayer;
    private int grabbedLayer;

    [Header("Highlight Settings")]
    [SerializeField] private Image highlightImage;
    [SerializeField] private Color defaultHighlightColor = Color.clear;
    [SerializeField] private Color hoverHighlightColor = Color.yellow;
    [SerializeField] private Color defaultHoverColor = Color.cyan;

    // Grabbed Index (출발지)
    private static int grabbedIndex = -1;

    // Hover 기반 스왑 타겟으로 사용 (XR 이벤트에만 의존)
    private static VRSlotInteraction lastHoveredSlot = null;

    // [위치 복원용 필드]
    private Transform originalParent;
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;

    // Static Dictionary: 모든 슬롯 인스턴스를 추적
    private static Dictionary<int, VRSlotInteraction> allSlotInteractions = new Dictionary<int, VRSlotInteraction>();


    // ----------------------------------------------------\
    // [Awake & Start]
    // ----------------------------------------------------\
    private void Awake()
    {
        // 컴포넌트 가져오기 및 초기화
        slotImage = GetComponent<Image>();
        slotCollider = GetComponent<Collider>();
        uiUpdater = GetComponent<SlotUIUpdater>();

        // CustomSlotGrabInteractable 가져오기
        grabInteractable = GetComponentInChildren<CustomSlotGrabInteractable>();

        if (grabInteractable != null)
        {
            // 3D 모델 렌더링 컴포넌트 가져오기
            itemMeshRenderer = grabInteractable.GetComponentInChildren<MeshRenderer>();

            if (itemMeshRenderer != null)
            {
                // Material 인스턴스를 가져와 개별적으로 수정할 수 있도록 준비
                itemModelMaterialInstance = itemMeshRenderer.material;
                // 초기에는 렌더러 비활성화
                itemMeshRenderer.enabled = false;
            }
            else
            {
                Debug.LogWarning($"[VRSlotInteraction] Slot Index {slotIndex}: CustomSlotGrabInteractable의 자식에서 MeshRenderer를 찾을 수 없습니다! 3D 아이템 모델을 확인하세요.");
            }

            // 인덱스 연결 및 이벤트 연결 로직
            grabInteractable.slotIndex = this.slotIndex;
            grabInteractable.selectEntered.AddListener(OnSelectStart);
            grabInteractable.selectExited.AddListener(OnSelectEndWithDelay);
            grabInteractable.hoverEntered.AddListener(OnHoverStart);
            grabInteractable.hoverExited.AddListener(OnHoverEnd);
            grabInteractable.activated.AddListener(OnActivatedForUse);
            grabInteractable.selectEntered.AddListener(OnSelectStartedOverrideParenting);

            // 레이어 설정 로직
            originalLayer = grabInteractable.gameObject.layer;
            int layerIndex = LayerMask.NameToLayer("GrabbedItem");
            grabbedLayer = (layerIndex == -1) ? originalLayer : layerIndex;
        }

        // Static Dictionary에 등록
        if (!allSlotInteractions.ContainsKey(slotIndex))
        {
            allSlotInteractions.Add(slotIndex, this);
        }
    }

    private void Start()
    {
        originalParent = transform.parent;
        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;

        if (highlightImage != null)
        {
            highlightImage.color = defaultHighlightColor;
        }

        // 퀵슬롯 영역이라면 이벤트 구독
        if (QuickSlotManager.Instance != null && QuickSlotManager.Instance.IsQuickSlotIndex(slotIndex))
        {
            QuickSlotManager.Instance.OnQuickSlotChanged += OnQuickSlotDataChanged;
        }
    }

    // ----------------------------------------------------\
    // [Grab/Drop 로직]
    // ----------------------------------------------------\

    private void OnSelectStartedOverrideParenting(SelectEnterEventArgs args)
    {
        grabInteractable.gameObject.layer = grabbedLayer;
    }

    private void OnSelectStart(SelectEnterEventArgs args)
    {
        grabbedIndex = this.slotIndex;
        ClearHighlightVisual();

        // Grab 시 아이템 데이터 가져와 3D 텍스처 업데이트
        ItemBaseSO itemData = GetItemDataForGlobalIndex(this.slotIndex);
        UpdateItemModelTexture(itemData);

        uiUpdater.UpdateSlotUI(null, 0);

        // DEBUG 로그 제거
    }

    // [VRSlotInteraction.cs - OnSelectEndWithDelay 함수 수정]
    private void OnSelectEndWithDelay(SelectExitEventArgs args)
    {
        // Select Exited는 Grab을 놓았을 때 호출됩니다.
        int targetIndex = -1;

        if (lastHoveredSlot != null && lastHoveredSlot.slotIndex != grabbedIndex)
        {
            // 1. 다른 슬롯에 Hover 후 드롭한 경우 (스왑)
            targetIndex = lastHoveredSlot.slotIndex;
            CallSwapManager(grabbedIndex, targetIndex);
        }
        else
        {
            // 2. 빈 공간 또는 출발지 슬롯에 드롭한 경우 (원래 위치로 복원)
            targetIndex = grabbedIndex;

            // DEBUG 로그 제거

            // 🔥 [수정]: 지연된 위치 복원 코루틴을 시작합니다.
            StartCoroutine(RestoreItemPositionDelayed());

            RestoreItem(targetIndex);
        }

        // 인덱스 초기화 및 레이어 복구
        grabbedIndex = -1;
        lastHoveredSlot = null;
        grabInteractable.gameObject.layer = originalLayer;
    }

    /// <summary>
    /// 아이템을 놓은 후 2 프레임을 기다려 위치 복원 로직을 실행합니다. 
    /// (XR 시스템이 아이템 제어권을 완전히 해제하고 모든 LateUpdate 처리를 마칠 때까지 기다림)
    /// </summary>
    private IEnumerator RestoreItemPositionDelayed()
    {
        // 1. Select Exit 이벤트 처리를 완료할 때까지 대기
        yield return null;

        // 2. XR 시스템의 LateUpdate나 강제 재정의 로직이 끝날 때까지 한 프레임 더 대기
        // 이 지연이 InventoryPanel로 튕기는 문제를 해결하는 핵심입니다.
        yield return null;

        RestoreItemPosition();
    }

    /// <summary>
    /// 잡고 있던 아이템을 원래 슬롯의 위치로 되돌립니다.
    /// </summary>
    private void RestoreItemPosition()
    {
        if (grabInteractable == null) return;
        Transform itemTransform = grabInteractable.transform;

        // DEBUG 로그 제거

        // 1. 부모를 원래대로 복원합니다. (이 VRSlotInteraction 오브젝트를 부모로 설정)
        // 2 프레임 지연으로 XR 시스템의 강제 부모 재정의를 무효화합니다.
        itemTransform.SetParent(this.transform);

        // 2. 위치와 회전을 초기 값 (슬롯의 중앙 위치, localPosition = 0,0,0)으로 복원합니다.
        itemTransform.localPosition = Vector3.zero;
        itemTransform.localRotation = Quaternion.identity;
        itemTransform.localScale = Vector3.one;

        // DEBUG 로그 제거
    }

    private void RestoreItem(int slotIndexToRefresh)
    {
        // 인벤토리/퀵슬롯 매니저에서 해당 인덱스의 아이템 데이터를 다시 가져와 UI를 갱신하도록 요청합니다.
        if (Inventory.Instance != null && slotIndexToRefresh < Inventory.Instance.Capacity)
        {
            // 인벤토리 슬롯의 경우, 3D 모델을 다시 확인하고 UI를 갱신합니다.
            var itemData = GetItemDataForGlobalIndex(slotIndexToRefresh);
            UpdateItemModelTexture(itemData);
            Inventory.Instance.RefreshSlotUI(slotIndexToRefresh); // Inventory.cs에 있는 메서드 호출
        }
        else if (QuickSlotManager.Instance != null)
        {
            int internalIndex = QuickSlotManager.Instance.GetQuickSlotInternalIndex(slotIndexToRefresh);
            if (internalIndex != -1)
            {
                QuickSlotManager.Instance.NotifySlotChanged(internalIndex);
            }
        }
    }

    private void CallSwapManager(int fromIndex, int toIndex)
    {
        if (QuickSlotManager.Instance != null)
        {
            QuickSlotManager.Instance.GlobalSwapItems(fromIndex, toIndex);
        }
        else
        {
            Debug.LogError("[VRSlotInteraction] QuickSlotManager가 없어 스왑 로직을 실행할 수 없습니다.");
        }
    }

    // ----------------------------------------------------\
    // [아이템 데이터 조회 및 3D 모델 갱신 로직]
    // ----------------------------------------------------\

    /// <summary>
    /// Global Index를 사용하여 해당 슬롯의 ItemBaseSO 데이터를 가져옵니다.
    /// </summary>
    private ItemBaseSO GetItemDataForGlobalIndex(int globalIndex)
    {
        // 퀵슬롯 영역 확인
        if (QuickSlotManager.Instance != null && QuickSlotManager.Instance.IsQuickSlotIndex(globalIndex))
        {
            // QuickSlotManager에서 데이터 가져오기 (QuickSlotManager.cs의 GetSlotData를 가정)
            var slotData = QuickSlotManager.Instance.GetSlotData(globalIndex);
            if (slotData.itemData != null) return slotData.itemData;
        }
        // 인벤토리 영역 확인
        else if (Inventory.Instance != null && globalIndex < Inventory.Instance.Capacity)
        {
            // Inventory.Instance.slots 배열을 직접 사용합니다.
            if (Inventory.Instance.slots != null && globalIndex < Inventory.Instance.slots.Length)
            {
                var slotData = Inventory.Instance.slots[globalIndex];
                if (slotData.itemData != null) return slotData.itemData;
            }
        }
        return null;
    }

    private void OnQuickSlotDataChanged(int internalIndex, ItemBaseSO itemData, int stackSize)
    {
        int currentSlotInternalIndex = QuickSlotManager.Instance.GetQuickSlotInternalIndex(this.slotIndex);

        if (currentSlotInternalIndex != internalIndex || currentSlotInternalIndex == -1) return;

        UpdateItemModelTexture(itemData);
    }

    /// <summary>
    /// 3D 모델의 Material 텍스처를 갱신하고 활성화/비활성화를 처리합니다.
    /// </summary>
    private void UpdateItemModelTexture(ItemBaseSO itemData)
    {
        if (itemMeshRenderer == null || itemModelMaterialInstance == null) return;

        Texture2D textureToApply = null;

        if (itemData != null)
        {
            // ItemBaseSO에 할당된 item3DTexture를 직접 사용합니다.
            textureToApply = itemData.item3DTexture;
        }

        if (textureToApply != null)
        {
            // 1. 아이템이 있을 경우: 텍스처를 설정하고 모델을 활성화
            itemModelMaterialInstance.SetTexture(BaseMapPropertyID, textureToApply);
            itemMeshRenderer.enabled = true;
        }
        else
        {
            // 2. 아이템이 비어있거나 텍스처가 없을 경우: 모델을 비활성화 (시야에서 사라지게 함)
            itemMeshRenderer.enabled = false;
            itemModelMaterialInstance.SetTexture(BaseMapPropertyID, null);
        }
    }

    // ----------------------------------------------------\
    // [Hover/Highlight 로직]
    // ----------------------------------------------------\

    private void OnHoverStart(HoverEnterEventArgs args)
    {
        if (grabbedIndex != -1 && this.slotIndex != grabbedIndex)
        {
            SetHighlightVisual(hoverHighlightColor);
            lastHoveredSlot = this;
        }
        else
        {
            SetHighlightVisual(defaultHoverColor);
        }
    }

    private void OnHoverEnd(HoverExitEventArgs args)
    {
        ClearHighlightVisual();
        if (lastHoveredSlot == this)
        {
            lastHoveredSlot = null;
        }
    }

    private void SetHighlightVisual(Color color)
    {
        if (highlightImage != null)
        {
            highlightImage.color = color;
        }
    }

    private void ClearHighlightVisual()
    {
        if (highlightImage != null)
        {
            highlightImage.color = defaultHighlightColor;
        }
    }

    // ----------------------------------------------------\
    // [아이템 사용 로직]
    // ----------------------------------------------------\

    // VR Interactor의 Activate 버튼 입력 시 호출됩니다.
    public void OnActivatedForUse(ActivateEventArgs args)
    {
        // 1. 사용 시도 전, 빈 슬롯 여부 확인 
        if (Inventory.Instance != null && Inventory.Instance.IsSlotEmpty(this.slotIndex))
        {
            if (QuickSlotManager.Instance == null) return;
        }

        // 2. 인벤토리 슬롯인 경우 아이템 사용 요청
        if (Inventory.Instance != null && this.slotIndex < Inventory.Instance.Capacity)
        {
            Inventory.Instance.UseItem(this.slotIndex);
        }
        // 3. 퀵슬롯 슬롯인 경우 아이템 사용 요청
        else if (QuickSlotManager.Instance != null)
        {
            int quickIndex = QuickSlotManager.Instance.GetQuickSlotInternalIndex(this.slotIndex);
            if (quickIndex != -1)
            {
                // QuickSlotManager.Instance.UseItemAtQuickSlotIndex(quickIndex);
            }
        }
    }

    // ----------------------------------------------------\
    // [Clean Up]
    // ----------------------------------------------------\

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            // 모든 리스너 제거
            grabInteractable.selectEntered.RemoveListener(OnSelectStart);
            grabInteractable.selectExited.RemoveListener(OnSelectEndWithDelay);
            grabInteractable.hoverEntered.RemoveListener(OnHoverStart);
            grabInteractable.hoverExited.RemoveListener(OnHoverEnd);
            grabInteractable.activated.RemoveListener(OnActivatedForUse);
            grabInteractable.selectEntered.RemoveListener(OnSelectStartedOverrideParenting);
        }

        // 퀵슬롯 데이터 변경 이벤트 구독 해지
        if (QuickSlotManager.Instance != null && QuickSlotManager.Instance.IsQuickSlotIndex(slotIndex))
        {
            QuickSlotManager.Instance.OnQuickSlotChanged -= OnQuickSlotDataChanged;
        }

        // Static Dictionary에서 제거
        if (allSlotInteractions.ContainsKey(slotIndex))
        {
            allSlotInteractions.Remove(slotIndex);
        }
    }
}