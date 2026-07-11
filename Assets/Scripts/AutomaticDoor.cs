using System.Collections;
using UnityEngine;

/// <summary>
/// Placed on an AutomaticDoor GameObject (BoxCollider with Is Trigger enabled).
/// Shows the DoorInteraction UI when the player enters the trigger zone.
/// If the player holds the required clue, pressing Interact (E) slides the door
/// open; the door auto-closes after a configurable delay, or immediately on a
/// second Interact press.
/// An Enemy-tagged collider entering the trigger opens the door immediately,
/// bypassing the clue requirement.
/// </summary>
public class AutomaticDoor : MonoBehaviour
{
    [Header("Required Clue")]
    [Tooltip("The clue the player must have collected to open this door.")]
    [SerializeField] private ClueData _requiredClue;

    [Header("Lockpicking")]
    [Tooltip("If true, a locked door can be picked open with F even without the required clue.")]
    [SerializeField] private bool _canBePicked;

    [Header("Door Settings")]
    [Tooltip("The child 3D object that physically slides when the door opens/closes.")]
    [SerializeField] private Transform _doorObject;
    [Tooltip("Local-space offset applied to the door object's starting position when fully open.")]
    [SerializeField] private Vector3 _openOffset = new Vector3(2f, 0f, 0f);
    [Tooltip("Units per second the door travels.")]
    [SerializeField] private float _moveSpeed = 3f;
    [Tooltip("Seconds before the door automatically closes after fully opening.")]
    [SerializeField] private float _autoCloseDelay = 4f;

    private enum DoorState { Closed, Opening, Open, Closing }
    private DoorState _state = DoorState.Closed;

    private Vector3 _closedLocalPos;
    private Vector3 _openLocalPos;

    private PlayerInputHandler _playerInput;
    private ClueInventory _inventory;

    private GameObject _doorInteractionUI;
    private GameObject _lockedDoorUI;
    private GameObject _opennedDoorUI;
    private GameObject _lockpickUI;

    private Coroutine _autoCloseCoroutine;

    void Awake()
    {
        if (_doorObject != null)
        {
            _closedLocalPos = _doorObject.localPosition;
            _openLocalPos = _closedLocalPos + _openOffset;
        }

        var playerUI = GameObject.FindWithTag("PlayerUI");
        if (playerUI != null)
        {
            var doorInteraction = playerUI.transform.Find("DoorInteraction");
            if (doorInteraction != null)
            {
                _doorInteractionUI = doorInteraction.gameObject;
                _lockedDoorUI = doorInteraction.Find("LockedDoor")?.gameObject;
                _opennedDoorUI = doorInteraction.Find("OpennedDoor")?.gameObject;
                _lockpickUI = doorInteraction.Find("Lockpick")?.gameObject;
            }
            else
            {
                Debug.LogWarning("[AutomaticDoor] 'DoorInteraction' not found under PlayerUI.");
            }
        }
        else
        {
            Debug.LogWarning("[AutomaticDoor] No GameObject with tag 'PlayerUI' found in scene.");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (_state == DoorState.Closed || _state == DoorState.Closing)
                OpenDoor();
            return;
        }

        var handler = other.GetComponent<PlayerInputHandler>();
        if (handler == null) return;

        _playerInput = handler;
        _inventory = handler.GetComponentInParent<ClueInventory>();

        ShowDoorUI();
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerInputHandler>() == null) return;

        _playerInput = null;
        _inventory = null;

        HideDoorUI();
    }

    void Update()
    {
        MoveDoor();

        if (_playerInput == null) return;

        if (_playerInput.GetInteractDown())
            TryInteract();

        if (_playerInput.GetPickLockDown())
            TryPickLock();
    }

    bool IsLocked =>
        _requiredClue != null && (_inventory == null || !_inventory.IsCollected(_requiredClue.id));

    void ShowDoorUI()
    {
        _doorInteractionUI?.SetActive(true);

        bool locked = IsLocked;
        _lockedDoorUI?.SetActive(locked);
        _opennedDoorUI?.SetActive(!locked);
        _lockpickUI?.SetActive(_canBePicked && locked);
    }

    void HideDoorUI()
    {
        _lockedDoorUI?.SetActive(false);
        _opennedDoorUI?.SetActive(false);
        _lockpickUI?.SetActive(false);
        _doorInteractionUI?.SetActive(false);
    }

    void TryInteract()
    {
        if (IsLocked) return;

        if (_state == DoorState.Closed || _state == DoorState.Closing)
            OpenDoor();
        else
            CloseDoor();
    }

    void TryPickLock()
    {
        if (!_canBePicked || !IsLocked) return;

        _lockpickUI?.SetActive(false);
        OpenDoor();
    }

    void OpenDoor()
    {
        if (_autoCloseCoroutine != null)
            StopCoroutine(_autoCloseCoroutine);

        _state = DoorState.Opening;
        _autoCloseCoroutine = StartCoroutine(AutoCloseAfterDelay());
    }

    void CloseDoor()
    {
        if (_autoCloseCoroutine != null)
        {
            StopCoroutine(_autoCloseCoroutine);
            _autoCloseCoroutine = null;
        }

        _state = DoorState.Closing;
    }

    void MoveDoor()
    {
        if (_doorObject == null) return;

        if (_state == DoorState.Opening)
        {
            _doorObject.localPosition = Vector3.MoveTowards(
                _doorObject.localPosition, _openLocalPos, _moveSpeed * Time.deltaTime);

            if (_doorObject.localPosition == _openLocalPos)
                _state = DoorState.Open;
        }
        else if (_state == DoorState.Closing)
        {
            _doorObject.localPosition = Vector3.MoveTowards(
                _doorObject.localPosition, _closedLocalPos, _moveSpeed * Time.deltaTime);

            if (_doorObject.localPosition == _closedLocalPos)
                _state = DoorState.Closed;
        }
    }

    IEnumerator AutoCloseAfterDelay()
    {
        yield return new WaitUntil(() => _state == DoorState.Open);
        yield return new WaitForSeconds(_autoCloseDelay);

        if (_state == DoorState.Open)
            CloseDoor();

        _autoCloseCoroutine = null;
    }
}
