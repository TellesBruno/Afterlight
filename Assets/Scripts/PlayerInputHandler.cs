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
    private InputAction _pickLockAction;

    private InputAction _saveStateAction;
    private InputAction _loadStateAction;
    private InputAction _clueMenuAction;
    private InputAction _questMenuAction;
    private InputAction _highlightQuestsAction;
    private InputAction _returnAction;

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
        _pickLockAction   = InputSystem.actions.FindAction("Player/PickLock");

        _saveStateAction       = InputSystem.actions.FindAction("UI/SaveState");
        _loadStateAction       = InputSystem.actions.FindAction("UI/LoadState");
        _clueMenuAction        = InputSystem.actions.FindAction("UI/ClueMenuKey");
        _questMenuAction       = InputSystem.actions.FindAction("UI/QuestMenuKey");
        _highlightQuestsAction = InputSystem.actions.FindAction("UI/HighlightQuests");
        _returnAction          = InputSystem.actions.FindAction("UI/Return");

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
        _pickLockAction?.Enable();

        _saveStateAction?.Enable();
        _loadStateAction?.Enable();
        _clueMenuAction?.Enable();
        _questMenuAction?.Enable();
        _highlightQuestsAction?.Enable();
        _returnAction?.Enable();
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

    /// <summary>Returns +1 (next slot), -1 (previous slot), or 0. Scroll wheel only — Previous/Next keys select a fixed slot, see <see cref="GetSelectWeaponInput"/>.</summary>
    public int GetWeaponSwitchInput()
    {
        if (!CanProcessInput()) return 0;

        var mouse = Mouse.current;
        if (mouse != null)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll > 0f) return  1;
            if (scroll < 0f) return -1;
        }

        return 0;
    }

    /// <summary>Returns 1–9 for direct slot selection, or 0 if nothing was pressed. Previous/Next select weapons 1 and 2 directly.</summary>
    public int GetSelectWeaponInput()
    {
        if (!CanProcessInput()) return 0;

        if (_prevWeaponAction?.WasPressedThisFrame() ?? false) return 1;
        if (_nextWeaponAction?.WasPressedThisFrame() ?? false) return 2;

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

    /// <summary>True the frame F is pressed, during normal gameplay (cursor locked). Used to pick locked doors.</summary>
    public bool GetPickLockDown()
    {
        return CanProcessInput() && (_pickLockAction?.WasPressedThisFrame() ?? false);
    }

    // ── UI / Global actions (bypass CanProcessInput — work regardless of cursor state) ──

    /// <summary>True the frame F5 is pressed. Works even when a menu is open.</summary>
    public bool GetSaveDown() => _saveStateAction?.WasPressedThisFrame() ?? false;

    /// <summary>True the frame F8 is pressed. Works even when a menu is open.</summary>
    public bool GetLoadDown() => _loadStateAction?.WasPressedThisFrame() ?? false;

    /// <summary>True the frame I is pressed. Works regardless of cursor lock state.</summary>
    public bool GetClueMenuDown() => _clueMenuAction?.WasPressedThisFrame() ?? false;

    /// <summary>True the frame J is pressed. Works regardless of cursor lock state.</summary>
    public bool GetQuestMenuDown() => _questMenuAction?.WasPressedThisFrame() ?? false;

    /// <summary>True the frame Tab is pressed. Only during normal gameplay (cursor locked) — this is a HUD glance, not a menu toggle.</summary>
    public bool GetHighlightQuestsDown() => CanProcessInput() && (_highlightQuestsAction?.WasPressedThisFrame() ?? false);

    /// <summary>True the frame Return/Esc is pressed. Works regardless of cursor lock state.</summary>
    public bool GetReturnDown() => _returnAction?.WasPressedThisFrame() ?? false;
}
