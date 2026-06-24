using System.Collections;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("Info")]
    public string weaponName = "Weapon";

    [Header("Shooting")]
    public float damage = 20f;
    public float fireRate = 0.2f;
    public bool isAutomatic = false;
    public float recoilForce = 0.5f;

    [Header("Weapon Type")]
    [Tooltip("Unchecked = non-lethal (e.g. tranquilizer). Both modes currently incapacitate enemies equally.")]
    public bool isLethal = true;

    [Header("Ammo")]
    public int maxAmmo = 30;
    public int currentAmmo = 30;
    public int totalAmmo = 90;

    [Header("Aiming")]
    public float aimZoomRatio = 0.7f;
    public Vector3 aimOffset = Vector3.zero;

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public Transform gunMuzzle;

    public GameObject SourcePrefab { get; set; }
    public GameObject Owner { get; set; }

    public bool IsReloading { get; private set; }
    public bool IsCharging  { get; private set; }
    public bool AutomaticReload => false;

    public float CurrentAmmoRatio => maxAmmo > 0 ? (float)currentAmmo / maxAmmo : 0f;

    private float _lastTimeShot = Mathf.NegativeInfinity;

    /// <summary>
    /// Called every frame by WeaponManager. Returns true if a shot was fired this frame.
    /// </summary>
    public bool HandleShootInputs(bool inputDown, bool inputHeld, bool inputReleased)
    {
        if (IsReloading || currentAmmo <= 0) return false;

        bool shouldFire = isAutomatic ? inputHeld : inputDown;
        if (!shouldFire) return false;

        if (Time.time < _lastTimeShot + fireRate) return false;

        _lastTimeShot = Time.time;
        currentAmmo--;

        if (projectilePrefab != null && gunMuzzle != null)
        {
            var go   = Instantiate(projectilePrefab, gunMuzzle.position, gunMuzzle.rotation);
            var proj = go.GetComponent<ProjectileScript>();
            if (proj != null)
            {
                proj.damage        = damage;
                proj.isLethal      = isLethal;
                proj.firedByPlayer = true;
            }
        }

        return true;
    }

    public void StartReloadAnimation()
    {
        if (IsReloading || currentAmmo == maxAmmo) return;
        StartCoroutine(ReloadCoroutine());
    }

    private IEnumerator ReloadCoroutine()
    {
        IsReloading = true;
        yield return new WaitForSeconds(2f); // placeholder — replace with animation length
        int needed  = maxAmmo - currentAmmo;
        int drawn   = Mathf.Min(needed, totalAmmo);
        currentAmmo += drawn;
        totalAmmo   -= drawn;
        IsReloading = false;
    }

    public void ShowWeapon(bool show)
    {
        gameObject.SetActive(show);
    }
}
