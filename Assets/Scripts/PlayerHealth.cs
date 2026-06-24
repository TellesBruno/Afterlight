using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public float maxHealth = 100f;

    private float _currentHealth;
    private bool  _isDead;

    /// <summary>Fired whenever health changes. Args: (currentHealth, maxHealth).</summary>
    public event Action<float, float> OnHealthChanged;

    /// <summary>Fired once when health reaches zero.</summary>
    public event Action OnDeath;

    void Awake()
    {
        _currentHealth = maxHealth;
    }

    // ── IDamageable ──────────────────────────────────────────────────────────

    public void TakeDamage(float amount, bool lethal) => TakeDamage(amount);

    // ── Public API ───────────────────────────────────────────────────────────

    public void TakeDamage(float amount)
    {
        if (amount <= 0f || _isDead) return;
        _currentHealth = Mathf.Max(0f, _currentHealth - amount);
        OnHealthChanged?.Invoke(_currentHealth, maxHealth);

        if (_currentHealth <= 0f)
        {
            _isDead = true;
            OnDeath?.Invoke();
        }
    }

    public float GetHealth() => _currentHealth;

    /// <summary>Restores health from save data and notifies listeners.</summary>
    public void LoadHealth(float value)
    {
        _isDead        = false;
        _currentHealth = Mathf.Clamp(value, 0f, maxHealth);
        OnHealthChanged?.Invoke(_currentHealth, maxHealth);
    }
}
