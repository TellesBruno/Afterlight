using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Three-state enemy AI: Patrol → Hostile → Incapacitated.
///
/// The enemy patrols a set of waypoints using NavMeshAgent.
/// It only becomes Hostile when the player is inside a RestrictedArea
/// AND within the enemy's field-of-view cone (angle + distance + line-of-sight).
/// While Hostile it chases the player and fires projectiles.
///
/// Setup:
///   1. Add NavMeshAgent to this GameObject and bake a NavMesh in the scene.
///   2. Assign waypoints, projectile prefab, and muzzle point in the Inspector.
///   3. Add a CapsuleCollider (non-trigger) so projectiles can detect this enemy.
///   4. Add EnemyHealth to this same GameObject.
///   5. Tag this GameObject as "Enemy".
///   6. Place a CombatManager in the scene.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyAI : MonoBehaviour
{
    // ── States ───────────────────────────────────────────────────────────────

    private enum State { Patrol, Hostile, Incapacitated }
    private State _state = State.Patrol;

    // ── Patrol ───────────────────────────────────────────────────────────────

    [Header("Patrol")]
    [Tooltip("Ordered list of positions the enemy walks between (loops).")]
    [SerializeField] private Transform[] _waypoints;
    [SerializeField] private float _patrolSpeed     = 2f;
    [SerializeField] private float _waypointReachDistance = 0.5f;

    // ── Detection ────────────────────────────────────────────────────────────

    [Header("Detection")]
    [Tooltip("Total horizontal angle of the vision cone (degrees).")]
    [SerializeField] private float _viewAngle    = 90f;
    [Tooltip("Maximum detection distance (metres).")]
    [SerializeField] private float _viewDistance = 12f;

    // ── Combat ───────────────────────────────────────────────────────────────

    [Header("Combat")]
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform  _muzzlePoint;
    [Tooltip("Damage per projectile.")]
    [SerializeField] private float _damage       = 15f;
    [Tooltip("Shots per second.")]
    [SerializeField] private float _fireRate     = 1f;
    [Tooltip("Distance at which the enemy starts shooting.")]
    [SerializeField] private float _shootingRange = 8f;
    [SerializeField] private float _chaseSpeed   = 4f;
    [Tooltip("Seconds without line-of-sight before the enemy returns to patrol.")]
    [SerializeField] private float _loseSightDuration = 15f;
    [Tooltip("Height above the player's root transform used as the aim/detection target. " +
             "Set this to the CharacterController center Y so shots hit the body, not the feet.")]
    [SerializeField] private float _playerAimHeight = 0.9f;

    // ── Death ────────────────────────────────────────────────────────────────

    [Header("Death")]
    [Tooltip("The visible mesh/model child (EnemyBody) whose pose is overridden on death.")]
    [SerializeField] private Transform _enemyBody;
    [Tooltip("Empty child whose local position/rotation represent the desired death pose.")]
    [SerializeField] private Transform _deathPose;

    // ── Private ──────────────────────────────────────────────────────────────

    private NavMeshAgent _agent;
    private EnemyHealth  _health;
    private Transform    _playerTransform;

    private int   _currentWaypointIndex;
    private float _lastShotTime  = Mathf.NegativeInfinity;
    private float _lastSightTime = Mathf.NegativeInfinity;

    // Eye height offset for line-of-sight raycasts (approximate)
    private const float EyeHeight = 1.5f;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    void Awake()
    {
        _agent  = GetComponent<NavMeshAgent>();
        _health = GetComponent<EnemyHealth>();
        _health.OnIncapacitated += HandleIncapacitated;
    }

    void Start()
    {
        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null) _playerTransform = playerGO.transform;
        else Debug.LogWarning($"[EnemyAI] No GameObject tagged 'Player' found. Assign the 'Player' tag to the player root.");

        _agent.speed = _patrolSpeed;

        if (_waypoints != null && _waypoints.Length > 0)
            _agent.SetDestination(_waypoints[0].position);
    }

    void OnDestroy()
    {
        if (_health != null)
            _health.OnIncapacitated -= HandleIncapacitated;
    }

    void Update()
    {
        switch (_state)
        {
            case State.Patrol:      PatrolUpdate();      break;
            case State.Hostile:     HostileUpdate();     break;
            case State.Incapacitated:                    break;
        }
    }

    // ── State updates ────────────────────────────────────────────────────────

    void PatrolUpdate()
    {
        if (_waypoints == null || _waypoints.Length == 0) return;

        // Advance waypoint when close enough
        if (!_agent.pathPending && _agent.remainingDistance <= _waypointReachDistance)
        {
            _currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Length;
            _agent.SetDestination(_waypoints[_currentWaypointIndex].position);
        }

        // Check for aggro: player must be in a RestrictedArea AND within this enemy's FOV
        if (CombatManager.Instance != null &&
            CombatManager.Instance.PlayerInRestrictedArea &&
            PlayerInFOV())
        {
            SetHostile();
        }
    }

    void HostileUpdate()
    {
        if (_playerTransform == null) return;

        if (PlayerInFOV())
            _lastSightTime = Time.time;
        else if (Time.time - _lastSightTime >= _loseSightDuration)
        {
            ReturnToPatrol();
            return;
        }

        float dist = Vector3.Distance(transform.position, _playerTransform.position);

        if (dist <= _shootingRange)
        {
            // Stop and face the player to shoot
            _agent.SetDestination(transform.position);
            FaceTarget(_playerTransform.position);
            TryShoot();
        }
        else
        {
            _agent.SetDestination(_playerTransform.position);
        }
    }

    // ── Transitions ──────────────────────────────────────────────────────────

    void SetHostile()
    {
        _lastSightTime = Time.time;
        _state         = State.Hostile;
        _agent.speed   = _chaseSpeed;
        CombatManager.Instance?.RegisterHostile();
    }

    void ReturnToPatrol()
    {
        CombatManager.Instance?.UnregisterHostile();
        _state       = State.Patrol;
        _agent.speed = _patrolSpeed;

        // Resume towards the nearest waypoint
        if (_waypoints != null && _waypoints.Length > 0)
            _agent.SetDestination(_waypoints[_currentWaypointIndex].position);
    }

    void HandleIncapacitated()
    {
        if (_state == State.Hostile)
            CombatManager.Instance?.UnregisterHostile();

        _state           = State.Incapacitated;
        _agent.isStopped = true;
        _agent.enabled   = false;

        // Apply death pose to the visible body mesh
        if (_enemyBody != null && _deathPose != null)
        {
            _enemyBody.localPosition = _deathPose.localPosition;
            _enemyBody.localRotation = _deathPose.localRotation;
        }
    }

    // ── Combat ───────────────────────────────────────────────────────────────

    void TryShoot()
    {
        if (_projectilePrefab == null || _muzzlePoint == null) return;
        if (Time.time < _lastShotTime + 1f / _fireRate) return;

        _lastShotTime = Time.time;

        Vector3 dir = (_playerTransform.position + Vector3.up * _playerAimHeight - _muzzlePoint.position).normalized;
        var go  = Instantiate(_projectilePrefab, _muzzlePoint.position, Quaternion.LookRotation(dir));
        var proj = go.GetComponent<ProjectileScript>();
        if (proj != null)
        {
            proj.damage        = _damage;
            proj.isLethal      = true;
            proj.firedByPlayer = false;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    bool PlayerInFOV()
    {
        if (_playerTransform == null) return false;

        Vector3 eyePos    = transform.position + Vector3.up * EyeHeight;
        Vector3 targetPos = _playerTransform.position + Vector3.up * _playerAimHeight;
        Vector3 dir       = targetPos - eyePos;
        float   dist      = dir.magnitude;

        if (dist > _viewDistance) return false;

        float angle = Vector3.Angle(transform.forward, dir.normalized);
        if (angle > _viewAngle / 2f) return false;

        // Line-of-sight raycast — if nothing blocks the path, player is visible
        if (Physics.Raycast(eyePos, dir.normalized, out RaycastHit hit, dist))
            return hit.collider.CompareTag("Player") ||
                   hit.transform.GetComponentInParent<PlayerHealth>() != null;

        return true;
    }

    void FaceTarget(Vector3 target)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.LookRotation(dir);
    }
}
