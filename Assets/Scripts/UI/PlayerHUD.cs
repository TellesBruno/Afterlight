using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the in-game HUD: life counter and ammo display.
///
/// Expects a GameObject tagged "PlayerUI" in the scene with this hierarchy:
///   PlayerUI
///   ├── LifeStatus/LifeCount         (TMP_Text)
///   ├── AmmoStatus/CurrentAmmo       (TMP_Text)
///   └── AmmoStatus/TotalAmmo         (TMP_Text)
/// </summary>
public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private Image healthFill;

    private TMP_Text _lifeCount;
    private TMP_Text _currentAmmo;
    private TMP_Text _totalAmmo;

    private WeaponController _trackedWeapon;

    void Start()
    {
        var root = GameObject.FindWithTag("PlayerUI");
        if (root != null)
        {
            _lifeCount   = root.transform.Find("LifeStatus/LifeCount")?.GetComponent<TMP_Text>();
            _currentAmmo = root.transform.Find("AmmoStatus/CurrentAmmo")?.GetComponent<TMP_Text>();
            _totalAmmo   = root.transform.Find("AmmoStatus/TotalAmmo")?.GetComponent<TMP_Text>();
        }
        else
        {
            Debug.LogWarning("[PlayerHUD] No GameObject with tag 'PlayerUI' found in scene.");
        }

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += RefreshHealth;
            RefreshHealth(playerHealth.GetHealth(), playerHealth.maxHealth);
        }

        if (weaponManager != null)
            weaponManager.OnSwitchedToWeapon += OnWeaponSwitched;
    }

    void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= RefreshHealth;

        if (weaponManager != null)
            weaponManager.OnSwitchedToWeapon -= OnWeaponSwitched;
    }

    void Update()
    {
        RefreshAmmo();
    }

    // ── Callbacks ─────────────────────────────────────────────────────────────

    private void RefreshHealth(float current, float max)
    {
        if (_lifeCount != null)
            _lifeCount.text = Mathf.CeilToInt(current).ToString();

        if (healthFill != null)
            healthFill.fillAmount = max > 0f ? Mathf.Clamp01(current / max) : 0f;
    }

    private void OnWeaponSwitched(WeaponController weapon)
    {
        _trackedWeapon = weapon;
        RefreshAmmo();
    }

    private void RefreshAmmo()
    {
        if (_trackedWeapon == null && weaponManager != null)
            _trackedWeapon = weaponManager.GetActiveWeapon();

        if (_currentAmmo != null)
            _currentAmmo.text = _trackedWeapon != null ? _trackedWeapon.currentAmmo.ToString() : "-";

        if (_totalAmmo != null)
            _totalAmmo.text = _trackedWeapon != null ? _trackedWeapon.totalAmmo.ToString() : "-";
    }
}
