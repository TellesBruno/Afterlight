using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(PlayerInputHandler))]
public class WeaponManager : MonoBehaviour
{
    public enum WeaponSwitchState { Up, Down, PutDownPrevious, PutUpNew }

    [Header("Starting Weapons")]
    public List<WeaponController> startingWeapons = new List<WeaponController>();

    [Header("References")]
    [Tooltip("Main player camera — used for FOV changes")]
    public Camera playerCamera;
    [Tooltip("Optional secondary camera rendered on top, prevents weapons clipping through geometry")]
    public Camera weaponCamera;
    [Tooltip("Parent transform all weapon instances are spawned under")]
    public Transform weaponParentSocket;
    [Tooltip("Resting weapon position")]
    public Transform defaultWeaponPosition;
    [Tooltip("Weapon position while aiming down sights")]
    public Transform aimingWeaponPosition;
    [Tooltip("Weapon position while holstered / switching")]
    public Transform downWeaponPosition;

    [Header("Weapon Bob")]
    public float bobFrequency = 10f;
    public float bobSharpness = 10f;
    [Tooltip("Bob amplitude when walking/running")]
    public float defaultBobAmount = 0.02f;
    [Tooltip("Bob amplitude while aiming")]
    public float aimingBobAmount = 0.008f;
    [Tooltip("Character speed at which the bob reaches full amplitude (match PlayerScript.sprintSpeed)")]
    public float maxCharacterSpeed = 10f;

    [Header("Weapon Recoil")]
    public float recoilSharpness = 50f;
    public float maxRecoilDistance = 0.5f;
    public float recoilRestitutionSharpness = 10f;

    [Header("Misc")]
    public float aimingAnimationSpeed = 10f;
    public float defaultFov = 60f;
    [Tooltip("Weapon camera FOV = player FOV × this multiplier")]
    public float weaponFovMultiplier = 1f;
    [Tooltip("Minimum seconds between weapon switch inputs (prevents scroll spam)")]
    public float weaponSwitchDelay = 0.5f;
    [Tooltip("Layer applied to all weapon child objects for FPS rendering")]
    public LayerMask fpsWeaponLayer;

    public bool IsAiming            { get; private set; }
    public int  ActiveWeaponIndex   { get; private set; }

    public UnityAction<WeaponController>       OnSwitchedToWeapon;
    public UnityAction<WeaponController, int>  OnAddedWeapon;
    public UnityAction<WeaponController, int>  OnRemovedWeapon;

    private readonly WeaponController[] _weaponSlots = new WeaponController[9];
    private PlayerInputHandler  _inputHandler;
    private CharacterController _characterController;

    private float _weaponBobFactor;
    private Vector3 _weaponMainLocalPosition;
    private Vector3 _weaponBobLocalPosition;
    private Vector3 _weaponRecoilLocalPosition;
    private Vector3 _accumulatedRecoil;

    private float            _timeStartedWeaponSwitch;
    private WeaponSwitchState _switchState;
    private int              _switchNewWeaponIndex;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    void Start()
    {
        ActiveWeaponIndex = -1;
        _switchState      = WeaponSwitchState.Down;

        _inputHandler       = GetComponent<PlayerInputHandler>();
        _characterController = GetComponent<CharacterController>();

        OnSwitchedToWeapon += OnWeaponSwitched;

        SetFov(defaultFov);

        foreach (var weapon in startingWeapons)
            AddWeapon(weapon);

        SwitchWeapon(true);
    }

    void Update()
    {
        WeaponController active = GetActiveWeapon();

        // While reloading, block all further input
        if (active != null && active.IsReloading)
            return;

        if (active != null && _switchState == WeaponSwitchState.Up)
        {
            // Manual reload
            if (!active.AutomaticReload && _inputHandler.GetReloadDown() && active.CurrentAmmoRatio < 1f)
            {
                IsAiming = false;
                active.StartReloadAnimation();
                return;
            }

            // Aiming
            IsAiming = _inputHandler.GetAimHeld();

            // Firing
            bool hasFired = active.HandleShootInputs(
                _inputHandler.GetFireDown(),
                _inputHandler.GetFireHeld(),
                _inputHandler.GetFireReleased());

            if (hasFired)
            {
                _accumulatedRecoil += Vector3.back * active.recoilForce;
                _accumulatedRecoil  = Vector3.ClampMagnitude(_accumulatedRecoil, maxRecoilDistance);
            }
        }

        // Weapon switching (not allowed while aiming or charging)
        if (!IsAiming &&
            (active == null || !active.IsCharging) &&
            (_switchState == WeaponSwitchState.Up || _switchState == WeaponSwitchState.Down))
        {
            int switchInput = _inputHandler.GetWeaponSwitchInput();
            if (switchInput != 0)
            {
                SwitchWeapon(switchInput > 0);
            }
            else
            {
                int selectInput = _inputHandler.GetSelectWeaponInput();
                if (selectInput != 0 && GetWeaponAtSlotIndex(selectInput - 1) != null)
                    SwitchToWeaponIndex(selectInput - 1);
            }
        }
    }

    void LateUpdate()
    {
        UpdateWeaponAiming();
        UpdateWeaponBob();
        UpdateWeaponRecoil();
        UpdateWeaponSwitching();

        weaponParentSocket.localPosition =
            _weaponMainLocalPosition + _weaponBobLocalPosition + _weaponRecoilLocalPosition;
    }

    // -------------------------------------------------------------------------
    // FOV
    // -------------------------------------------------------------------------

    public void SetFov(float fov)
    {
        if (playerCamera != null)
            playerCamera.fieldOfView = fov;
        if (weaponCamera != null)
            weaponCamera.fieldOfView = fov * weaponFovMultiplier;
    }

    // -------------------------------------------------------------------------
    // Animated updates (LateUpdate)
    // -------------------------------------------------------------------------

    void UpdateWeaponAiming()
    {
        if (_switchState != WeaponSwitchState.Up) return;

        WeaponController active = GetActiveWeapon();
        if (IsAiming && active != null)
        {
            _weaponMainLocalPosition = Vector3.Lerp(
                _weaponMainLocalPosition,
                aimingWeaponPosition.localPosition + active.aimOffset,
                aimingAnimationSpeed * Time.deltaTime);

            if (playerCamera != null)
                SetFov(Mathf.Lerp(playerCamera.fieldOfView,
                    active.aimZoomRatio * defaultFov,
                    aimingAnimationSpeed * Time.deltaTime));
        }
        else
        {
            _weaponMainLocalPosition = Vector3.Lerp(
                _weaponMainLocalPosition,
                defaultWeaponPosition.localPosition,
                aimingAnimationSpeed * Time.deltaTime);

            if (playerCamera != null)
                SetFov(Mathf.Lerp(playerCamera.fieldOfView, defaultFov, aimingAnimationSpeed * Time.deltaTime));
        }
    }

    void UpdateWeaponBob()
    {
        if (Time.deltaTime <= 0f) return;

        float horizontalSpeed = new Vector3(
            _characterController.velocity.x, 0f, _characterController.velocity.z).magnitude;

        float movementFactor = _characterController.isGrounded
            ? Mathf.Clamp01(horizontalSpeed / maxCharacterSpeed)
            : 0f;

        _weaponBobFactor = Mathf.Lerp(_weaponBobFactor, movementFactor, bobSharpness * Time.deltaTime);

        float amplitude  = IsAiming ? aimingBobAmount : defaultBobAmount;
        float hBob = Mathf.Sin(Time.time * bobFrequency) * amplitude * _weaponBobFactor;
        float vBob = ((Mathf.Sin(Time.time * bobFrequency * 2f) * 0.5f) + 0.5f) * amplitude * _weaponBobFactor;

        _weaponBobLocalPosition.x = hBob;
        _weaponBobLocalPosition.y = Mathf.Abs(vBob);
    }

    void UpdateWeaponRecoil()
    {
        if (_weaponRecoilLocalPosition.z >= _accumulatedRecoil.z * 0.99f)
        {
            _weaponRecoilLocalPosition = Vector3.Lerp(
                _weaponRecoilLocalPosition, _accumulatedRecoil, recoilSharpness * Time.deltaTime);
        }
        else
        {
            _weaponRecoilLocalPosition = Vector3.Lerp(
                _weaponRecoilLocalPosition, Vector3.zero, recoilRestitutionSharpness * Time.deltaTime);
            _accumulatedRecoil = _weaponRecoilLocalPosition;
        }
    }

    void UpdateWeaponSwitching()
    {
        float t = weaponSwitchDelay <= 0f
            ? 1f
            : Mathf.Clamp01((Time.time - _timeStartedWeaponSwitch) / weaponSwitchDelay);

        if (t >= 1f)
        {
            if (_switchState == WeaponSwitchState.PutDownPrevious)
            {
                GetWeaponAtSlotIndex(ActiveWeaponIndex)?.ShowWeapon(false);

                ActiveWeaponIndex = _switchNewWeaponIndex;
                t = 0f;

                WeaponController next = GetWeaponAtSlotIndex(ActiveWeaponIndex);
                OnSwitchedToWeapon?.Invoke(next);

                if (next != null)
                {
                    _timeStartedWeaponSwitch = Time.time;
                    _switchState = WeaponSwitchState.PutUpNew;
                }
                else
                {
                    _switchState = WeaponSwitchState.Down;
                }
            }
            else if (_switchState == WeaponSwitchState.PutUpNew)
            {
                _switchState = WeaponSwitchState.Up;
            }
        }

        if (_switchState == WeaponSwitchState.PutDownPrevious)
        {
            _weaponMainLocalPosition = Vector3.Lerp(
                defaultWeaponPosition.localPosition, downWeaponPosition.localPosition, t);
        }
        else if (_switchState == WeaponSwitchState.PutUpNew)
        {
            _weaponMainLocalPosition = Vector3.Lerp(
                downWeaponPosition.localPosition, defaultWeaponPosition.localPosition, t);
        }
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    public void SwitchWeapon(bool ascending)
    {
        int best     = -1;
        int bestDist = _weaponSlots.Length;

        for (int i = 0; i < _weaponSlots.Length; i++)
        {
            if (i == ActiveWeaponIndex || _weaponSlots[i] == null) continue;
            int dist = GetDistanceBetweenSlots(ActiveWeaponIndex, i, ascending);
            if (dist < bestDist) { bestDist = dist; best = i; }
        }

        SwitchToWeaponIndex(best);
    }

    public void SwitchToWeaponIndex(int newIndex, bool force = false)
    {
        if (!force && (newIndex == ActiveWeaponIndex || newIndex < 0)) return;

        _switchNewWeaponIndex    = newIndex;
        _timeStartedWeaponSwitch = Time.time;

        if (GetActiveWeapon() == null)
        {
            _weaponMainLocalPosition = downWeaponPosition.localPosition;
            _switchState             = WeaponSwitchState.PutUpNew;
            ActiveWeaponIndex        = _switchNewWeaponIndex;
            OnSwitchedToWeapon?.Invoke(GetWeaponAtSlotIndex(ActiveWeaponIndex));
        }
        else
        {
            _switchState = WeaponSwitchState.PutDownPrevious;
        }
    }

    public bool AddWeapon(WeaponController weaponPrefab)
    {
        if (HasWeapon(weaponPrefab) != null) return false;

        for (int i = 0; i < _weaponSlots.Length; i++)
        {
            if (_weaponSlots[i] != null) continue;

            WeaponController instance = Instantiate(weaponPrefab, weaponParentSocket);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.Owner       = gameObject;
            instance.SourcePrefab = weaponPrefab.gameObject;
            instance.ShowWeapon(false);

            if (fpsWeaponLayer.value != 0)
            {
                int layerIndex = Mathf.RoundToInt(Mathf.Log(fpsWeaponLayer.value, 2));
                foreach (Transform t in instance.GetComponentsInChildren<Transform>(true))
                    t.gameObject.layer = layerIndex;
            }

            _weaponSlots[i] = instance;
            OnAddedWeapon?.Invoke(instance, i);
            return true;
        }

        // Slots full — auto-switch if nothing is active
        if (GetActiveWeapon() == null)
            SwitchWeapon(true);

        return false;
    }

    public bool RemoveWeapon(WeaponController weaponInstance)
    {
        for (int i = 0; i < _weaponSlots.Length; i++)
        {
            if (_weaponSlots[i] != weaponInstance) continue;

            _weaponSlots[i] = null;
            OnRemovedWeapon?.Invoke(weaponInstance, i);
            Destroy(weaponInstance.gameObject);

            if (i == ActiveWeaponIndex)
                SwitchWeapon(true);

            return true;
        }
        return false;
    }

    public WeaponController HasWeapon(WeaponController weaponPrefab)
    {
        foreach (var w in _weaponSlots)
            if (w != null && w.SourcePrefab == weaponPrefab.gameObject)
                return w;
        return null;
    }

    public WeaponController GetActiveWeapon()           => GetWeaponAtSlotIndex(ActiveWeaponIndex);
    public WeaponController GetWeaponAtSlotIndex(int i) =>
        (i >= 0 && i < _weaponSlots.Length) ? _weaponSlots[i] : null;

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    int GetDistanceBetweenSlots(int from, int to, bool ascending)
    {
        int dist = ascending ? (to - from) : -(to - from);
        if (dist < 0) dist += _weaponSlots.Length;
        return dist;
    }

    void OnWeaponSwitched(WeaponController newWeapon)
    {
        newWeapon?.ShowWeapon(true);
    }
}
