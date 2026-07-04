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

    [Header("Consequences (optional, hidden)")]
    [Tooltip("Applied to the player's ConsequenceTracker if the hit that incapacitates this enemy is lethal damage (a kill). Leave empty for no consequence.")]
    [SerializeField] private ConsequenceData lethalKillConsequence;

    [Tooltip("Applied to the player's ConsequenceTracker if this enemy is incapacitated without ever taking lethal damage (a non-lethal takedown). Leave empty for no consequence.")]
    [SerializeField] private ConsequenceData nonLethalTakedownConsequence;

    public string EnemyId => _enemyId;
    public bool IsIncapacitated { get; private set; }

    /// <summary>True if the hit that incapacitated this enemy was lethal damage (a kill). Meaningless while IsIncapacitated is false.</summary>
    public bool WasKilledLethally { get; private set; }

    /// <summary>Fired when health reaches zero (lethal or non-lethal hit).</summary>
    public event Action OnIncapacitated;

    private float _currentHealth;
    private ConsequenceTracker _consequenceTracker;
    private QuestManager _questManager;

    void Awake()
    {
        _currentHealth = _maxHealth;

        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null)
        {
            _consequenceTracker = playerGO.GetComponent<ConsequenceTracker>();
            _questManager       = playerGO.GetComponent<QuestManager>();
        }
    }

    // ── IDamageable ──────────────────────────────────────────────────────────

    /// <param name="lethal">If this hit incapacitates the enemy, determines whether it counts as a kill (lethal) or a takedown (non-lethal).</param>
    public void TakeDamage(float amount, bool lethal)
    {
        if (IsIncapacitated || amount <= 0f) return;

        _currentHealth = Mathf.Max(0f, _currentHealth - amount);

        if (_currentHealth <= 0f)
            Incapacitate(lethal);
    }

    // ── Internal ─────────────────────────────────────────────────────────────

    private void Incapacitate(bool lethal)
    {
        if (IsIncapacitated) return;
        IsIncapacitated  = true;
        WasKilledLethally = lethal;

        var consequence = lethal ? lethalKillConsequence : nonLethalTakedownConsequence;
        if (consequence != null && _consequenceTracker != null)
            _consequenceTracker.ApplyConsequence(consequence);

        // Any incapacitation can be the missing piece for a quest condition,
        // even without a consequence attached to this enemy.
        _questManager?.RecheckActiveQuests();

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
