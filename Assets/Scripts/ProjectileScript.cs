using UnityEngine;

public class ProjectileScript : MonoBehaviour
{
    [Tooltip("Forward speed in units/second")]
    public float speed = 20f;

    [Tooltip("Seconds before the projectile self-destructs")]
    public float lifetime = 5f;

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
        if (other.CompareTag("Enemy"))
            Destroy(other.gameObject);

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
