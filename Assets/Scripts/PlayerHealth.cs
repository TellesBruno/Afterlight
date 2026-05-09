using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;

    private float _currentHealth;

    /// <summary>Fired whenever health changes. Args: (currentHealth, maxHealth).</summary>
    public event Action<float, float> OnHealthChanged;

    void Awake()
    {
        _currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;
        _currentHealth = Mathf.Max(0f, _currentHealth - amount);
        OnHealthChanged?.Invoke(_currentHealth, maxHealth);
    }

    public float GetHealth() => _currentHealth;

    /// <summary>Restores health from save data and notifies listeners.</summary>
    public void LoadHealth(float value)
    {
        _currentHealth = Mathf.Clamp(value, 0f, maxHealth);
        OnHealthChanged?.Invoke(_currentHealth, maxHealth);
    }
}
