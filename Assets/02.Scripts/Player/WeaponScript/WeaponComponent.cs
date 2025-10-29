using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class WeaponComponent : MonoBehaviour
{
    [Header("Ammo Settings")]
    public AmmoSO.AmmoType weaponAmmoType = AmmoSO.AmmoType.Rifle;
    public int currentAmmo = 30;
    public int maxAmmo = 30;

    [Header("Fire Settings")]
    [SerializeField] private float fireCooldown = 0.05f;
    private float nextFireTime = 0f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI currentAmmoText;
    [SerializeField] private TextMeshProUGUI maxAmmoText;
    [SerializeField] private Canvas ammoCanvas; //

    [Header("Input Action")]
    [SerializeField] private InputActionProperty fireAction;
    private bool triggerPressedLastFrame = false;

    [Header("Effects")]
    [SerializeField] private Transform firePoint; // 🔥 총구 위치 (필수)
    [SerializeField] private ParticleSystem fireParticle; // 파티클 드래그
    [SerializeField] private AudioSource audioSource; // 총 발사 사운드 재생기
    [SerializeField] private AudioClip fireClip; // 총 발사 사운드 클립
    [SerializeField] private Animator animator;

    [Header("Bullet Pooling")]
    [SerializeField] private string bulletKey = "Bullet_Rifle"; // 🔑 풀링 키 (일치하게)

    private bool isEquipped = false;

    private void OnEnable()
    {
        UpdateAmmoUI();
        if (ammoCanvas != null)
            ammoCanvas.gameObject.SetActive(false); // 시작 시 비활성화
    }

    public void SetEquipped(bool equipped)
    {
        isEquipped = equipped;

        if (ammoCanvas != null)
        {
            ammoCanvas.gameObject.SetActive(equipped);

            if (equipped && ammoCanvas.renderMode == RenderMode.WorldSpace)
            {
                ammoCanvas.worldCamera = Camera.main; // ✅ 월드 스페이스용 카메라 연결
            }
        }
    }
    private void Start()
    {
        currentAmmo = maxAmmo;
        UpdateAmmoUI();
    }



    private void Update()
    {
        if (!isEquipped || fireAction == null || fireAction.action == null)
        {
            Debug.LogWarning("[WeaponComponent] 조건 불충족 - isEquipped: " + isEquipped + ", fireAction null? " + (fireAction == null) + ", action null? " + (fireAction.action == null));
            triggerPressedLastFrame = false;
            return;
        }

        float triggerValue = fireAction.action.ReadValue<float>();
        bool isTriggerPressed = triggerValue > 0.8f;

        Debug.Log($"[WeaponComponent] 트리거값: {triggerValue}, isTriggerPressed: {isTriggerPressed}, currentAmmo: {currentAmmo}, isEquipped: {isEquipped}, nextFireTime: {nextFireTime}, Time: {Time.time}");


        if (isTriggerPressed && !triggerPressedLastFrame && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireCooldown;
            Fire();
        }

        triggerPressedLastFrame = isTriggerPressed;
    }

    public void Fire()
    {
        if (!isEquipped || currentAmmo <= 0)
        {
            Debug.LogWarning("[WeaponComponent] 발사 불가 (장착 X 또는 탄약 없음)");
            return;
        }

        currentAmmo--;
        UpdateAmmoUI();
        Debug.Log("[WeaponComponent] 🔫 발사!");

        // 1. 총알 가져오기
        GameObject bulletObj = BulletPoolingManager.Instance.GetBullet(bulletKey);

        if (bulletObj != null && firePoint != null)
        {
            bulletObj.transform.position = firePoint.position;
            bulletObj.transform.rotation = firePoint.rotation;

            // 2. 총알 발사 (BulletProjectile 스크립트에 위임)
            BulletProjectile bullet = bulletObj.GetComponent<BulletProjectile>();
            if (bullet != null)
            {
                bullet.Fire(firePoint.forward);
            }
        }

        // 3. 파티클
        if (fireParticle != null)
        {
            fireParticle.Play();
        }

        // 4. 사운드
        if (audioSource != null && fireClip != null)
        {
            audioSource.PlayOneShot(fireClip);
        }
    }


    public void Reload(int amount)
    {
        currentAmmo = Mathf.Min(currentAmmo + amount, maxAmmo);
        UpdateAmmoUI();
    }

    private void UpdateAmmoUI()
    {
        if (currentAmmoText != null)
            currentAmmoText.text = currentAmmo.ToString();

        if (maxAmmoText != null)
            maxAmmoText.text = $"/ {maxAmmo}";
    }

    public void FireFromAnimation()
    {
        Fire();
    }
}
