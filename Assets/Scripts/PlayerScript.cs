using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerScript : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 10f;
    public float crouchSpeed = 2.5f;
    public float jumpForce = 5f;
    public float gravity = -9.81f;

    [Header("Crouch")]
    public float standingHeight = 2f;
    public float crouchHeight = 1f;
    public float crouchTransitionSpeed = 10f;
    [Tooltip("How many units the camera drops when crouching")]
    public float crouchCameraOffset = 0.5f;

    [Header("Footsteps")]
    [Tooltip("Sons de passo; um deles é escolhido aleatoriamente a cada passo.")]
    public AudioClip[] footstepSounds;
    [Tooltip("Segundos entre passos andando normalmente.")]
    public float footstepIntervalWalk = 0.5f;
    [Tooltip("Segundos entre passos correndo.")]
    public float footstepIntervalSprint = 0.3f;
    [Tooltip("Segundos entre passos agachado.")]
    public float footstepIntervalCrouch = 0.7f;

    [Header("References")]
    public Transform cameraTransform;

    private readonly float _maximumPitch = 80f;
    private readonly float _minimumPitch = -80f;
    private float _cameraPitch = 0f;

    private CharacterController _controller;
    private PlayerInputHandler _inputHandler;
    private AudioSource _audioSource;
    private float _verticalVelocity;
    private bool _isCrouching;
    private float _footstepTimer;
    private Vector3 _standingCameraLocalPos;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        _controller   = GetComponent<CharacterController>();
        _inputHandler = GetComponent<PlayerInputHandler>();
        _audioSource  = GetComponent<AudioSource>();

        if (cameraTransform != null)
            _standingCameraLocalPos = cameraTransform.localPosition;
    }

    void Update()
    {
        HandleLook();
        HandleMovement();
        HandleCrouch();
    }

    void HandleLook()
    {
        Vector2 look = _inputHandler.GetLookInput();

        transform.Rotate(Vector3.up, look.x, Space.Self);

        _cameraPitch -= look.y;
        _cameraPitch = Mathf.Clamp(_cameraPitch, _minimumPitch, _maximumPitch);
        if (cameraTransform != null)
            cameraTransform.localEulerAngles = new Vector3(_cameraPitch, 0f, 0f);
    }

    void HandleMovement()
    {
        Vector3 moveDir    = _inputHandler.GetMoveInput();
        bool isSprinting   = _inputHandler.GetSprintHeld() && !_isCrouching;
        float speed        = _isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : moveSpeed);

        // Keep a small downward force when grounded so isGrounded stays reliable
        if (_controller.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -2f;

        if (_inputHandler.GetJumpDown() && _controller.isGrounded && !_isCrouching)
            _verticalVelocity = jumpForce;

        _verticalVelocity += gravity * Time.deltaTime;

        Vector3 move = transform.TransformDirection(moveDir) * speed + Vector3.up * _verticalVelocity;
        _controller.Move(move * Time.deltaTime);

        HandleFootsteps(moveDir, isSprinting);
    }

    void HandleFootsteps(Vector3 moveDir, bool isSprinting)
    {
        if (moveDir.sqrMagnitude < 0.01f || !_controller.isGrounded)
        {
            _footstepTimer = 0f;
            return;
        }

        float interval = _isCrouching ? footstepIntervalCrouch : (isSprinting ? footstepIntervalSprint : footstepIntervalWalk);

        _footstepTimer += Time.deltaTime;
        if (_footstepTimer < interval) return;

        _footstepTimer = 0f;
        if (footstepSounds != null && footstepSounds.Length > 0)
            _audioSource.PlayOneShot(footstepSounds[Random.Range(0, footstepSounds.Length)]);
    }

    void HandleCrouch()
    {
        if (_inputHandler.GetCrouchDown())     _isCrouching = true;
        if (_inputHandler.GetCrouchReleased()) _isCrouching = false;

        // Smoothly adjust capsule height; keep center aligned so feet stay on the ground
        float targetHeight = _isCrouching ? crouchHeight : standingHeight;
        _controller.height = Mathf.Lerp(_controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        _controller.center = new Vector3(0f, _controller.height / 2f, 0f);

        // Smoothly move camera down/up — only Y, leaving look pitch untouched
        if (cameraTransform != null)
        {
            float targetCameraY = _standingCameraLocalPos.y - (_isCrouching ? crouchCameraOffset : 0f);
            Vector3 camPos = cameraTransform.localPosition;
            camPos.y = Mathf.Lerp(camPos.y, targetCameraY, crouchTransitionSpeed * Time.deltaTime);
            cameraTransform.localPosition = camPos;
        }
    }
}
