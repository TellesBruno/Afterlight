using UnityEngine;

/// <summary>
/// Place on any GameObject with tag "EnviromentalDamage" that has a Collider set as Trigger.
/// Deals damage to the player on contact.
/// </summary>
public class EnvironmentalDamage : MonoBehaviour
{
    [SerializeField] private float damage = 20f;

    void OnTriggerEnter(Collider other)
    {
        var health = other.GetComponentInParent<PlayerHealth>();
        health?.TakeDamage(damage);
    }
}
