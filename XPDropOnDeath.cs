using UnityEngine;

[RequireComponent(typeof(Health))]
public class XPDropOnDeath : MonoBehaviour
{
    [Header("XP donnee a la mort")]
    public float pureXP = 5f;     
    public float corruptXP = 5f;  

    private Health hp;

    void Awake() { hp = GetComponent<Health>(); }
    void OnEnable() { if (hp) hp.onDeath.AddListener(OnDeath); }
    void OnDisable() { if (hp) hp.onDeath.RemoveListener(OnDeath); }

    void OnDeath()
    {
        var killer = hp.lastHitBy;
        if (!killer) return;

        var pc = killer.GetComponent<PlayerCorruption>();
        bool isCorrupted = pc && pc.IsCorrupted;

        if (isCorrupted)
        {
            var cxp = killer.GetComponent<PlayerCorruptedXP>();
            if (cxp) cxp.AddXP(corruptXP);
        }
        else
        {
            var pxp = killer.GetComponent<PlayerXP>();
            if (pxp) pxp.AddXP(pureXP);
        }
    }
}
