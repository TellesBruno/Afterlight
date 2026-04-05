using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    [Header("Look Settings")]
    public float lookSensitivity = 0.1f;
    public bool invertXAxis = false;
    public bool invertYAxis = false;

    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _jumpAction;
    private InputAction _sprintAction;
    private InputAction _crouchAction;
    private InputAction _fireAction;
    private InputAction _aimAction;
    private InputAction _reloadAction;
    private InputAction _prevWeaponAction;
    private InputAction _nextWeaponAction;
    private InputAction _interactAction;

    private bool _fireWasHeld;

    void Start()
    {
        _moveAction       = InputSystem.actions.FindAction("Player/Move");
        _lookAction       = InputSystem.actions.FindAction("Player/Look");
        _jumpAction       = InputSystem.actions.FindAction("Player/Jump");
        _sprintAction     = InputSystem.actions.FindAction("Player/Sprint");
        _crouchAction     = InputSystem.actions.FindAction("Player/Crouch");
        _fireAction       = InputSystem.actions.FindAction("Player/Attack");
        _aimAction        = InputSystem.actions.FindAction("Player/Aim");
        _reloadAction     = InputSystem.actions.FindAction("Player/Reload");
        _prevWeaponAction = InputSystem.actions.FindAction("Player/Previous");
        _nextWeaponAction = InputSystem.actions.FindAction("Player/Next");
        _interactAction   = InputSystem.actions.FindAction("Player/Interact");

        _moveAction?.Enable();
        _lookAction?.Enable();
        _jumpAction?.Enable();
        _sprintAction?.Enable();
        _crouchAction?.Enable();
        _fireAction?.Enable();
        _aimAction?.Enable();
        _reloadAction?.Enable();
        _prevWeaponAction?.Enable();
        _nextWeaponAction?.Enable();
        _interactAction?.Enable();
    }

    void LateUpdate()
    {
        _fireWasHeld = GetFireHeld();
    }

    public bool CanProcessInput() => Cursor.lockState == CursorLockMode.Locked;

    /// <summary>World-space XZ move direction, magnitude clamped to 1.</summary>
    public Vector3 GetMoveInput()
    {
        if (!CanProcessInput()) return Vector3.zero;
        var raw = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
        return Vector3.ClampMagnitude(new Vector3(raw.x, 0f, raw.y), 1f);
    }

    /// <summary>Look delta already scaled by sensitivity and invert settings.</summary>
    public Vector2 GetLookInput()
    {
        if (!CanProcessInput()) return Vector2.zero;
        var raw = _lookAction != null ? _lookAction.ReadValue<Vector2>() : Vector2.zero;
        return new Vector2(
            raw.x * lookSensitivity * (invertXAxis ? -1f : 1f),
            raw.y * lookSensitivity * (invertYAxis ? -1f : 1f));
    }

    public bool GetJumpDown()
    {
        return CanProcessInput() && (_jumpAction?.WasPressedThisFrame() ?? false);
    }

    public bool GetSprintHeld()
    {
        return CanProcessInput() && (_sprintAction?.IsPressed() ?? false);
    }

    public bool GetCrouchDown()
    {
        return CanProcessInput() && (_crouchAction?.WasPressedThisFrame() ?? false);
    }

    public bool GetCrouchReleased()
    {
        return CanProcessInput() && (_crouchAction?.WasReleasedThisFrame() ?? false);
    }

    public bool GetFireDown()     => GetFireHeld() && !_fireWasHeld;
    public bool GetFireReleased() => !GetFireHeld() && _fireWasHeld;

    public bool GetFireHeld()
    {
        return CanProcessInput() && (_fireAction?.IsPressed() ?? false);
    }

    public bool GetAimHeld()
    {
        return CanProcessInput() && (_aimAction?.IsPressed() ?? false);
    }

    public bool GetReloadDown()
    {
        return CanProcessInput() && (_reloadAction?.WasPressedThisFrame() ?? false);
    }

    /// <summary>Returns +1 (next slot), -1 (previous slot), or 0.</summary>
    public int GetWeaponSwitchInput()
    {
        if (!CanProcessInput()) return 0;

        // Scroll wheel (takes priority)
        var mouse = Mouse.current;
        if (mouse != null)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll > 0f) return  1;
            if (scroll < 0f) return -1;
        }

        // Keyboard Next / Previous actions
        if (_nextWeaponAction?.WasPressedThisFrame() ?? false) return  1;
        if (_prevWeaponAction?.WasPressedThisFrame() ?? false) return -1;

        return 0;
    }

    /// <summary>Returns 1–9 for direct slot selection, or 0 if no digit key pressed.</summary>
    public int GetSelectWeaponInput()
    {
        if (!CanProcessInput()) return 0;
        var kb = Keyboard.current;
        if (kb == null) return 0;

        if (kb.digit1Key.wasPressedThisFrame) return 1;
        if (kb.digit2Key.wasPressedThisFrame) return 2;
        if (kb.digit3Key.wasPressedThisFrame) return 3;
        if (kb.digit4Key.wasPressedThisFrame) return 4;
        if (kb.digit5Key.wasPressedThisFrame) return 5;
        if (kb.digit6Key.wasPressedThisFrame) return 6;
        if (kb.digit7Key.wasPressedThisFrame) return 7;
        if (kb.digit8Key.wasPressedThisFrame) return 8;
        if (kb.digit9Key.wasPressedThisFrame) return 9;
        return 0;
    }

    /// <summary>Stub — wired to the Interact action (E key, hold interaction). Extend freely.</summary>
    public bool GetInteractDown()
    {
        return CanProcessInput() && (_interactAction?.WasPressedThisFrame() ?? false);
    }
}
