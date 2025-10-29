using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Items/Ammo Item")]
public class AmmoSO : ItemBaseSO
{
    public enum AmmoType
    {
        None,
        Rifle,
        Pistol,
        Shotgun
        // 필요시 더 추가 가능
    }


    [Header("Ammo Stats")]
    public int ammoAmount = 30;
    public AmmoType ammoType = AmmoType.Rifle;

    protected void OnEnable()
    {
        itemType = ItemType.Ammo;
    }

    public override void Use(int slotIndex, PlayerHealthComponent playerHealth, EquipmentManager equipmentManager)
    {
        base.Use(slotIndex, playerHealth, equipmentManager);

        if (equipmentManager == null) return;

        WeaponComponent weapon = equipmentManager.GetEquippedWeapon();
        if (weapon == null)
        {
            Debug.LogWarning("[AmmoSO] 장착된 무기를 찾을 수 없습니다.");
            return;
        }

        if (weapon.weaponAmmoType == ammoType)
        {
            weapon.Reload(ammoAmount);
            Debug.Log($"[AmmoSO] {ammoAmount}개의 {ammoType} 탄약 충전 완료.");
        }
        else
        {
            Debug.LogWarning($"[AmmoSO] 탄약 타입 불일치: 무기({weapon.weaponAmmoType}) vs 탄약({ammoType})");
        }
    }
}
