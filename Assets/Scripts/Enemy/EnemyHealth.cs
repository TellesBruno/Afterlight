using System;
using UnityEngine;

/// <summary>
/// Tracks an enemy's health. Implements IDamageable so projectiles can apply
/// damage without knowing the concrete type.
///
/// Setup:
///   1. Add to the root Enemy GameObject alongside EnemyAI.
///   2. Assign a unique enemyId in the Inspector (used by the save system).
///   3. Tag the root GameObject as "Enemy".
/// </summary>
public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private float _maxHealth = 100f;

    [Header("Save System")]
    [Tooltip("Unique ID used by SaveManager to persist incapacitated state. Must be unique per enemy in the scene.")]
    [SerializeField] private string _enemyId;

    public string EnemyId => _enemyId;
    public bool IsIncapacitated { get; private set; }

    /// <summary>Fired when health reaches zero (lethal or non-lethal hit).</summary>
    public event Action OnIncapacitated;

    private float _currentHealth;

    void Awake()
    {
        _currentHealth = _maxHealth;
    }

    // ── IDamageable ──────────────────────────────────────────────────────────

    /// <param name="lethal">Lethal and non-lethal hits currently have the same effect.</param>
    public void TakeDamage(float amount, bool lethal)
    {
        if (IsIncapacitated || amount <= 0f) return;

        _currentHealth = Mathf.Max(0f, _currentHealth - amount);

        if (_currentHealth <= 0f)
            Incapacitate();
    }

    // ── Internal ─────────────────────────────────────────────────────────────

    private void Incapacitate()
    {
        if (IsIncapacitated) return;
        IsIncapacitated = true;
        OnIncapacitated?.Invoke();
    }

    /// <summary>
    /// Called by SaveManager on load to restore incapacitated state without
    /// triggering combat events (CombatManager counters start fresh each session).
    /// </summary>
    public void ForceIncapacitate()
    {
        if (IsIncapacitated) return;
        IsIncapacitated = true;
        _currentHealth  = 0f;
        gameObject.SetActive(false);
    }
}
