using UnityEngine;

public class UIManager : MonoBehaviour
{
    // 지워도 되는 스크립트
    [Header("Dependencies")]
    // 인스펙터에서 할당할 UI 패널 (최상위 부모 오브젝트)
    [SerializeField] private GameObject inventoryPanel;
 
    [Header("VR Input Settings")]
    [Tooltip("인벤토리를 토글할 VR Input Manager 버튼 이름")]
    [SerializeField] private string toggleInventoryButton = "VR_Menu_Inventory";
 

    private bool isInventoryOpen = false;
    private bool isStatusOpen = false;

    private void Start()
    {
        // 게임 시작 시 모든 패널 닫기
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
      
    }

    private void Update()
    {
        // 인벤토리 토글 버튼 감지
        if (!string.IsNullOrEmpty(toggleInventoryButton) && Input.GetButtonDown(toggleInventoryButton))
        {
            ToggleInventory();
        }

    }

    // ----------------------------------------------------
    // [인벤토리 토글/닫기 로직]
    // ----------------------------------------------------
    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(isInventoryOpen);          
            // TODO: TimeScale 조정 로직 추가 (일시 정지)
        }
    }

    // 다른 메뉴에서 호출 시 인벤토리 닫기
    public void CloseInventory()
    {
        if (isInventoryOpen)
        {
            isInventoryOpen = false;
            inventoryPanel.SetActive(false);
        }
    }


}