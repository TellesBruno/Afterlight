public interface IDamageable
{
    /// <summary>
    /// Apply damage to this entity.
    /// </summary>
    /// <param name="amount">Damage amount (positive value).</param>
    /// <param name="lethal">If false the hit is non-lethal (e.g. tranquilizer); both states currently produce the same result.</param>
    void TakeDamage(float amount, bool lethal);
}
