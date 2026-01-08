using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class AutoCorruptUpgradeManagerQueued : MonoBehaviour
{
    [Header("References")]
    public GameObject player;                
    public PlayerCorruptedXP corruptedXP;    

    [Header("Upgrades impures disponibles")]
    public List<CorruptUpgradeData> allUpgrades = new List<CorruptUpgradeData>(); 

    [Header("Regles")]
    public int levelsPerChoice = 3;            
    public bool avoidRepeatBackToBack = true;  

    [Header("Toast UI (bandeau haut)")]
    public GameObject toastPanel;  
    public Image toastIcon;        
    public TMP_Text toastTitle;    
    public TMP_Text toastDesc;     
    public float toastSeconds = 2.5f;

  
    readonly Queue<CorruptUpgradeData> toastQueue = new Queue<CorruptUpgradeData>();
    bool showing;
    CorruptUpgradeData lastPicked;

    void Awake()
    {
        if (toastPanel) toastPanel.SetActive(false);
    }

    void OnEnable()
    {
        if (corruptedXP) corruptedXP.onLevelUp.AddListener(OnCorruptLevelUp);
    }

    void OnDisable()
    {
        if (corruptedXP) corruptedXP.onLevelUp.RemoveListener(OnCorruptLevelUp);
    }

    void OnCorruptLevelUp(int newLevel)
    {
        if (levelsPerChoice <= 0 || allUpgrades == null || allUpgrades.Count == 0) return;
        if (newLevel % levelsPerChoice != 0) return;

        var up = PickRandomUpgrade();
        if (up == null) return;

        
        up.Apply(player);

        
        toastQueue.Enqueue(up);
        if (!showing) StartCoroutine(ToastRunner());
    }

    CorruptUpgradeData PickRandomUpgrade()
    {
        if (allUpgrades == null || allUpgrades.Count == 0) return null;

        if (!avoidRepeatBackToBack || lastPicked == null || allUpgrades.Count == 1)
        {
            var p = allUpgrades[Random.Range(0, allUpgrades.Count)];
            lastPicked = p;
            return p;
        }

        int tries = 8;
        CorruptUpgradeData pick = lastPicked;
        while (tries-- > 0 && pick == lastPicked)
            pick = allUpgrades[Random.Range(0, allUpgrades.Count)];

        lastPicked = pick;
        return pick;
    }

    IEnumerator ToastRunner()
    {
        showing = true;
        while (toastQueue.Count > 0)
        {
            var up = toastQueue.Dequeue();
            ShowToast(up);
            yield return new WaitForSeconds(toastSeconds);
            HideToast();
        }
        showing = false;
    }

    void ShowToast(CorruptUpgradeData up)
    {
        if (!toastPanel) return;
        if (toastTitle) toastTitle.text = string.IsNullOrEmpty(up.title) ? "Upgrade impure" : up.title;
        if (toastDesc) toastDesc.text = up.description ?? "";
        if (toastIcon) toastIcon.sprite = up.icon;
        toastPanel.SetActive(true);
    }

    void HideToast()
    {
        if (toastPanel) toastPanel.SetActive(false);
    }
}
