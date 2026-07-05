using UnityEngine;

public class ProjectileScript : MonoBehaviour
{
    [Tooltip("Forward speed in units/second")]
    public float speed = 20f;

    [Tooltip("Seconds before the projectile self-destructs")]
    public float lifetime = 5f;

    [Tooltip("Damage applied to the target on hit. Set by the weapon or enemy that fires this projectile.")]
    public float damage = 10f;

    [Tooltip("If false the hit is non-lethal (tranquilizer). Both states currently produce the same result on enemies.")]
    public bool isLethal = true;

    [Tooltip("True when fired by the player (targets enemies). False when fired by an enemy (targets the player).")]
    public bool firedByPlayer = true;

    void Start()
    {
        Destroy(gameObject, lifetime);

        // Physics components live on a child (e.g. BulletBody).
        // Add a forwarder to that child so trigger events reach this script.
        var col = GetComponentInChildren<Collider>();
        if (col != null && col.gameObject != gameObject)
        {
            var forwarder = col.gameObject.AddComponent<ProjectileTriggerForwarder>();
            forwarder.owner = this;
        }
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    public void OnHit(Collider other)
    {
        // Ignore trigger zones (RestrictedArea, LevelExit, etc.)
        if (other.isTrigger) return;

        if (firedByPlayer)
        {
            var target = other.GetComponentInParent<IDamageable>();
            target?.TakeDamage(damage, isLethal);
        }
        else
        {
            var target = other.GetComponentInParent<PlayerHealth>();
            target?.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}

// Relay added at runtime to the child that owns the Collider.
// Not intended to be added manually in the Editor.
public class ProjectileTriggerForwarder : MonoBehaviour
{
    [HideInInspector] public ProjectileScript owner;

    void OnTriggerEnter(Collider other)
    {
        owner?.OnHit(other);
    }
}
