using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class CountdownToVictory : MonoBehaviour
{
    [Header("Timer")]
    public float durationSeconds = 600f;   
    public bool autoStart = true;
    public bool stopOnPlayerDeath = true;

    [Header("Refs joueur")]
    public Health playerHealth;            //stopper si le joueur meurt
    public PlayerXP pureXP;                // niveau "pur"
    public PlayerCorruptedXP corruptXP;    // niveau "corrompu"

    [Header("UI (optionnel)")]
    public TMP_Text label;                 // "MM:SS"
    public Slider slider;                  // barre qui descend

    [Header("Scènes de victoire")]
    public string victoryPureSceneName = "VictoryPur";
    public string victoryCorruptSceneName = "VictoryCorrompu";
    public bool corruptWinsTie = false;    // si égalité: qui gagne ?

    float remaining;
    bool running;
    bool finished;

    void Awake()
    {
        remaining = Mathf.Max(0f, durationSeconds);
        UpdateUI();
    }

    void OnEnable()
    {
        running = autoStart && !finished;
        if (stopOnPlayerDeath && playerHealth)
            playerHealth.onDeath.AddListener(StopOnDeath);

        if (!pureXP)
        {
            var p = GameObject.FindWithTag("Player");
            if (p) pureXP = p.GetComponent<PlayerXP>();
        }
        if (!corruptXP)
        {
            var p = GameObject.FindWithTag("Player");
            if (p) corruptXP = p.GetComponent<PlayerCorruptedXP>();
        }
    }

    void OnDisable()
    {
        if (stopOnPlayerDeath && playerHealth)
            playerHealth.onDeath.RemoveListener(StopOnDeath);
    }

    void StopOnDeath() { running = false; }

    void Update()
    {
        if (!running || finished) return;

        // avance même si timeScale change
        remaining -= Time.unscaledDeltaTime;

        if (remaining <= 0f)
        {
            remaining = 0f;
            finished = true;
            UpdateUI();
            LoadVictoryScene();
            return;
        }

        UpdateUI();
    }

    void LoadVictoryScene()
    {
        // choix de la scène selon le niveau le plus élevé
        string scene = victoryPureSceneName;

        int pureLvl = pureXP ? Mathf.Max(1, pureXP.level) : 0;
        int corruptLvl = corruptXP ? Mathf.Max(1, corruptXP.level) : 0;

        if (corruptLvl > pureLvl) scene = victoryCorruptSceneName;
        else if (corruptLvl < pureLvl) scene = victoryPureSceneName;
        else scene = corruptWinsTie ? victoryCorruptSceneName : victoryPureSceneName;

        SceneManager.LoadScene(scene);
    }

    void UpdateUI()
    {
        if (label) label.text = Format(remaining);

        if (slider)
        {
            slider.interactable = false;
            slider.minValue = 0f;
            slider.maxValue = durationSeconds;
            slider.value = remaining;
        }
    }

    string Format(float s)
    {
        int t = Mathf.CeilToInt(s);
        int m = t / 60;
        int sec = t % 60;
        return $"{m:00}:{sec:00}";
    }

    public void Pause() { running = false; }
    public void Resume() { if (!finished) running = true; }
    public void AddSeconds(float s)
    {
        remaining = Mathf.Clamp(remaining + s, 0f, durationSeconds);
        UpdateUI();
    }
}