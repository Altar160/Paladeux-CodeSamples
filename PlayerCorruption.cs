using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PlayerCorruption : MonoBehaviour
{
    [Header("Cycle corruption")]
    public float interval = 20f;   // temps "pur" entre deux corruptions
    public float duration = 6f;    // durée de la corruption

    [Header("Auto gameplay pendant corruption")]
    public float autoSpeed = 4f;
    public float seekRadius = 8f;
    public LayerMask seekMask;
    public DamageAura aura;

    [Header("Pénalité allié")]
    public float allyKillPenalty = 10f;

    [Header("Désactiver pendant corruption")]
    public Behaviour[] disableWhileCorrupted;

    [Header("Events")]
    public UnityEvent<bool> onCorruptionStateChanged;     // true = entre en corruption, false = sort
    public UnityEvent<GameObject> onKilledAlly;           

    public bool IsCorrupted { get; private set; }
    public float PhaseRemaining { get; private set; } // secondes restantes dans la phase actuelle
    public float PhaseTotal { get; private set; } // durée totale de la phase actuelle

    Rigidbody2D rb;
    Health hp;
    Coroutine loopCo, moveCo;
    bool[] wasEnabled;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        hp = GetComponent<Health>();
        if (aura && aura.gameObject.activeSelf) aura.gameObject.SetActive(false);

        // phase initiale = "pur"
        IsCorrupted = false;
        PhaseTotal = Mathf.Max(0.01f, interval);
        PhaseRemaining = PhaseTotal;
    }

    void OnEnable() { StartCorruptionLoop(); }
    void OnDisable()
    {
        if (loopCo != null) StopCoroutine(loopCo);
        if (moveCo != null) StopCoroutine(moveCo);
        if (IsCorrupted) ExitCorruption();
    }

    //  Boucle principale 
    void StartCorruptionLoop()
    {
        if (loopCo != null) StopCoroutine(loopCo);
        loopCo = StartCoroutine(CorruptionLoop());
    }

    IEnumerator CorruptionLoop()
    {
        while (true)
        {
            //Phase Pur
            IsCorrupted = false;
            PhaseTotal = Mathf.Max(0.01f, interval);
            PhaseRemaining = PhaseTotal;
            onCorruptionStateChanged?.Invoke(false);

            while (PhaseRemaining > 0f)
            {
                PhaseRemaining -= Time.deltaTime;
                yield return null;
            }

            EnterCorruption();

            //Phase Corrompue
            PhaseTotal = Mathf.Max(0.01f, duration);
            PhaseRemaining = PhaseTotal;

            while (PhaseRemaining > 0f)
            {
                PhaseRemaining -= Time.deltaTime;
                yield return null;
            }

            ExitCorruption();
        }
    }

    public void ResetNextCorruptionCountdown()
    {
        if (IsCorrupted) return;
        PhaseTotal = Mathf.Max(0.01f, interval);
        PhaseRemaining = PhaseTotal;
        StartCorruptionLoop();
    }

    void EnterCorruption()
    {
        if (IsCorrupted) return;
        IsCorrupted = true;
        onCorruptionStateChanged?.Invoke(true);

        if (hp) hp.SetGodMode(true);

        if (disableWhileCorrupted != null && disableWhileCorrupted.Length > 0)
        {
            wasEnabled = new bool[disableWhileCorrupted.Length];
            for (int i = 0; i < disableWhileCorrupted.Length; i++)
            {
                var b = disableWhileCorrupted[i];
                if (!b) continue;
                wasEnabled[i] = b.enabled;
                b.enabled = false;
            }
        }

        if (aura) aura.gameObject.SetActive(true);

        if (moveCo != null) StopCoroutine(moveCo);
        moveCo = StartCoroutine(AutoMove());
    }

    void ExitCorruption()
    {
        if (!IsCorrupted) return;
        IsCorrupted = false;
        onCorruptionStateChanged?.Invoke(false);

        if (moveCo != null) { StopCoroutine(moveCo); moveCo = null; }
        if (rb) rb.linearVelocity = Vector2.zero;

        if (aura) aura.gameObject.SetActive(false);

        if (disableWhileCorrupted != null && wasEnabled != null)
        {
            for (int i = 0; i < disableWhileCorrupted.Length; i++)
            {
                var b = disableWhileCorrupted[i];
                if (!b) continue;
                b.enabled = wasEnabled[i];
            }
        }
        wasEnabled = null;

        if (hp) hp.SetGodMode(false);
    }

    //Mini IA pendant corruption
    IEnumerator AutoMove()
    {
        while (IsCorrupted)
        {
            Vector2 dir = GetDirectionToClosestTarget();
            if (rb) rb.linearVelocity = dir * autoSpeed;
            yield return null;
        }
    }

    Vector2 GetDirectionToClosestTarget()
    {
        if (seekRadius <= 0f) return Vector2.zero;

        var hits = Physics2D.OverlapCircleAll(transform.position, seekRadius, seekMask);
        float best = float.MaxValue;
        Transform tgt = null;

        foreach (var h in hits)
        {
            if (!h || h.transform == transform) continue;
            var th = h.GetComponent<Health>() ?? h.GetComponentInParent<Health>();
            if (!th || !th.IsAlive) continue;

            float d = (h.transform.position - transform.position).sqrMagnitude;
            if (d < best) { best = d; tgt = h.transform; }
        }
        if (!tgt) return Vector2.zero;
        return ((Vector2)tgt.position - (Vector2)transform.position).normalized;
    }

    //Appelée par AllyDeathPenalty quand un ALLIÉ meurt tué par le joueur corrompue
    public void OnKilledAlly(GameObject ally)
    {
        if (!IsCorrupted) return;
        if (allyKillPenalty <= 0f) return;

        var playerHP = GetComponent<Health>();
        if (playerHP) playerHP.ForceDamage(allyKillPenalty, Vector2.zero, ally);

        onKilledAlly?.Invoke(ally);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.8f, 0.1f, 0.9f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, seekRadius);
    }
}
