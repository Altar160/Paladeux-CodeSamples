using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [Header("HP")]
    public float maxHP = 100f;
    [SerializeField] private float currentHP = 100f;

    [Header("Hit reaction")]
    public float invulnAfterHit = 0.2f;
    public bool disableCollidersOnDeath = true;
    public bool destroyOnDeath = false;

    [Header("Debug")]
    public GameObject lastHitBy;

    //Events
    [System.Serializable] public class HPChangedEvent : UnityEvent<float, float> { } //current, max
    public HPChangedEvent onHPChanged = new HPChangedEvent();     //nouveau nom
    public HPChangedEvent onHealthChanged = new HPChangedEvent(); //alias pour l'ancien code
    public UnityEvent onDeath = new UnityEvent();
    [System.Serializable] public class KilledByEvent : UnityEvent<GameObject> { }
    public KilledByEvent onKilledBy = new KilledByEvent();

    //Propriétés lecture seule
    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;
    public bool IsAlive => currentHP > 0f;

    //God mode
    bool godMode = false;

    Rigidbody2D rb;
    float invulnUntil;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHP = Mathf.Clamp(currentHP <= 0 ? maxHP : currentHP, 0f, maxHP);
        NotifyHPChanged();
    }

    public void SetGodMode(bool enabled) => godMode = enabled;

    public void TakeDamage(float amount, Vector2 knockback, GameObject instigator = null)
        => TakeDamageInternal(amount, knockback, instigator, ignoreIFrames: false);

    public void ForceDamage(float amount, Vector2 knockback, GameObject instigator = null)
        => TakeDamageInternal(amount, knockback, instigator, ignoreIFrames: true);

    void TakeDamageInternal(float amount, Vector2 knockback, GameObject instigator, bool ignoreIFrames)
    {
        if (amount <= 0f || !IsAlive) return;
        if (godMode) return;
        if (!ignoreIFrames && Time.time < invulnUntil) return;

        lastHitBy = instigator;
        invulnUntil = Time.time + invulnAfterHit;

        currentHP = Mathf.Max(0f, currentHP - amount);
        if (rb && knockback != Vector2.zero) rb.AddForce(knockback, ForceMode2D.Impulse);

        NotifyHPChanged();

        if (currentHP <= 0f)
        {
            if (disableCollidersOnDeath)
            {
                var cols = GetComponentsInChildren<Collider2D>();
                foreach (var c in cols) if (c) c.enabled = false;
            }

            onKilledBy?.Invoke(lastHitBy);
            onDeath?.Invoke();

            if (destroyOnDeath) Destroy(gameObject);
        }
    }

    public void Heal(float amount)
    {
        if (amount <= 0f || !IsAlive) return;
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        NotifyHPChanged();
    }

    public void SetMaxHP(float newMax, bool healProportionally = false)
    {
        newMax = Mathf.Max(1f, newMax);
        float ratio = maxHP > 0f ? currentHP / maxHP : 1f;

        maxHP = newMax;
        currentHP = healProportionally ? Mathf.Clamp(newMax * ratio, 0f, newMax)
                                       : Mathf.Min(currentHP, newMax);

        NotifyHPChanged();
    }

    public void Kill()
    {
        if (!IsAlive) return;
        ForceDamage(currentHP, Vector2.zero, null);
    }

    void NotifyHPChanged()
    {
        onHPChanged?.Invoke(currentHP, maxHP);
        onHealthChanged?.Invoke(currentHP, maxHP);
    }
}
