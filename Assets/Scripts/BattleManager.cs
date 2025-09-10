using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class BattleManager : MonoBehaviour
{
    [Header("Refs")]
    public EnemySpawner spawner;
    public EffectsState fx;
    public TurnEngine turnEngine;
    public SpecialSystem special;
    public UIController ui;
    public AnimationControllerBridge animBridge;

    [Header("Buttons Animators")]
    public Animator sunButtonAnimator;
    public Animator waterButtonAnimator;
    public Animator soilButtonAnimator;
    public Animator specialButtonAnimator;
    public Animator plantAnimationController;

    [Header("Player")]
    public int playerMaxHealth = 10;
    public int playerHealth = 10;

    [Header("Timing")]
    public float beatSeconds = 0.5f;
    private readonly string[] cadenceWords = { "Sun...", "Soil...", "Water..." };

    [Header("Quips")]
    public int quipOneInN = 2;
    public float quipDuration = 1.5f;

    bool inputLocked = false;
    bool specialWasReady = false;

    void Start()
    {
        ui.MakeNoDim(ui.sunButton);
        ui.MakeNoDim(ui.waterButton);
        ui.MakeNoDim(ui.soilButton);
        ui.MakeNoDim(ui.specialButton);

        ui.sunButton.onClick.AddListener(OnSunPressed);
        ui.waterButton.onClick.AddListener(OnWaterPressed);
        ui.soilButton.onClick.AddListener(OnSoilPressed);
        if (ui.specialButton) ui.specialButton.onClick.AddListener(OnSpecialPressed);

        special.OnBecameReady += () =>
        {
            if (specialButtonAnimator) specialButtonAnimator.SetTrigger("ReadyOnce");
        };

        spawner.StartNewSet();
        StartNewCycle();
        RefreshAllUI();
    }

    void StartNewCycle()
    {
        playerHealth = playerMaxHealth;
        fx.ResetAll();
        inputLocked = false;
        spawner.StartNewCycle();

        spawner.SpawnNextEnemy();
        RefreshAllUI();
    }

    void RefreshAllUI()
    {
        ui.RefreshHP(ui.playerHpText, ui.enemyHpText, playerHealth, playerMaxHealth, spawner.currentEnemy);
        special.RefreshUI();
        RefreshStatusUI();
        if (animBridge) { animBridge.SetAttacking(false); animBridge.SetSpecialing(false); }
        ui.SetButtonsInteractable(!inputLocked, fx.playerSunLocked, special.IsReady);
    }

    void RefreshStatusUI()
    {
        string enemyTxt = BuildEnemyStatusText();
        string playerTxt = BuildPlayerStatusText();
        ui.RefreshStatus(ui.enemyStatusText, ui.playerStatusText, enemyTxt, playerTxt);
    }

    string BuildEnemyStatusText()
    {
        System.Collections.Generic.List<string> l = new();
        if (fx.enemyBurnTurns > 0) l.Add($"Burning({fx.enemyBurnTurns})");
        if (fx.enemySoaked) l.Add("Soaked");
        if (fx.enemySunLocked) l.Add("Rooted");
        return l.Count > 0 ? string.Join(" • ", l) : "";
    }
    string BuildPlayerStatusText()
    {
        System.Collections.Generic.List<string> l = new();
        if (fx.playerBurnTurns > 0) l.Add($"Burning({fx.playerBurnTurns})");
        if (fx.playerSoaked) l.Add("Soaked");
        if (fx.playerSunLocked) l.Add("Rooted");
        return l.Count > 0 ? string.Join(" • ", l) : "";
    }

    bool TryLockInput()
    {
        if (inputLocked) return false;
        inputLocked = true;
        ui.SetButtonsInteractable(false, fx.playerSunLocked, special.IsReady);
        return true;
    }

    public void OnSunPressed()
    {
        if (fx.playerSunLocked)
        {
            if (ui.playerCadenceText) ui.playerCadenceText.text = "Rooted! Can't use Sun this turn.";
            return;
        }
        if (sunButtonAnimator) sunButtonAnimator.Play("SunButtonPressed", -1, 0f);
        if (TryLockInput()) StartCoroutine(RoundWithCountdown(Move.Sun));
    }
    public void OnWaterPressed()
    {
        if (waterButtonAnimator) waterButtonAnimator.Play("WaterButtonPressed", -1, 0f);
        if (TryLockInput()) StartCoroutine(RoundWithCountdown(Move.Water));
    }
    public void OnSoilPressed()
    {
        if (soilButtonAnimator) soilButtonAnimator.Play("SoilButtonPressed", -1, 0f);
        if (TryLockInput()) StartCoroutine(RoundWithCountdown(Move.Soil));
    }
    public void OnSpecialPressed()
    {
        if (!special.IsReady) return;
        if (!TryLockInput()) return;
        if (specialButtonAnimator) specialButtonAnimator.Play("SpecialButtonPressed", -1, 0f);
        if (animBridge) animBridge.SetSpecialing(true);
        StartCoroutine(RoundWithCountdownSpecial());
    }

    IEnumerator RoundWithCountdown(Move playerMove)
    {
        if (spawner.currentEnemy == null) yield break;
        if (fx.playerSunLocked && playerMove != Move.Sun) fx.playerSunLocked = false;

        Move enemyMove = GetEnemyMove(spawner.currentEnemy.Type);

        ui.ClearCadence();
        foreach (var word in cadenceWords)
        {
            ui.SetCadence(word, word);
            yield return new WaitForSeconds(beatSeconds);
        }
        ui.SetCadence(playerMove.ToString() + "!", enemyMove.ToString() + "!");
        yield return new WaitForSeconds(beatSeconds);

        if (animBridge) animBridge.SetAttacking(true);

        var result = turnEngine.ApplyOutcome(
            playerMove, enemyMove,
            spawner.currentEnemy,
            spawner.currentEnemy.Type,
            spawner.currentBossKind,
            fx);

        ApplyHpDeltas(result);
        special.Increment(1);

        MaybeQuip();

        ui.ClearCadence();
        CheckRoundOver();
        inputLocked = false;
        ui.SetButtonsInteractable(true, fx.playerSunLocked, special.IsReady);
    }

    IEnumerator RoundWithCountdownSpecial()
    {
        if (spawner.currentEnemy == null) yield break;

        Move enemyMove = GetEnemyMove(spawner.currentEnemy.Type);

        ui.ClearCadence();
        foreach (var word in cadenceWords)
        {
            ui.SetCadence(word, word);
            yield return new WaitForSeconds(beatSeconds);
        }
        ui.SetCadence("Special!", enemyMove.ToString() + "!");
        yield return new WaitForSeconds(beatSeconds);

        special.Consume();
        special.AddHitStreak();

        int dmg = special.CalculateDamage(fx.enemySoaked, fx.enemySunLocked, fx.enemyBurnTurns);
        if (special.specialConsumesSoaked && fx.enemySoaked) fx.enemySoaked = false;

        spawner.currentEnemy.Health = Mathf.Max(0, spawner.currentEnemy.Health - dmg);

        // FX
        if (ui.enemyHpText) StartCoroutine(ui.ShakeRect(ui.enemyHpText.rectTransform, ui.shakeDuration, ui.shakeMagnitude));
        if (ui.audioController) ui.audioController.PlaySpecial();
        StartCoroutine(ui.ShakeScreen(special.specialShakeDuration, special.specialShakeMagnitude));

        RefreshAllUI();
        CheckRoundOver();

        inputLocked = false;
        ui.SetButtonsInteractable(true, fx.playerSunLocked, special.IsReady);
    }

    void ApplyHpDeltas(TurnResult r)
    {
        if (r.enemyHpDelta != 0 && spawner.currentEnemy != null)
        {
            spawner.currentEnemy.Health = Mathf.Clamp(spawner.currentEnemy.Health + r.enemyHpDelta, 0, spawner.currentEnemy.MaxHealth);
            if (r.enemyHpDelta < 0 && ui.enemyHpText) StartCoroutine(ui.ShakeRect(ui.enemyHpText.rectTransform, ui.shakeDuration, ui.shakeMagnitude));
        }
        if (r.playerHpDelta != 0)
        {
            playerHealth = Mathf.Clamp(playerHealth + r.playerHpDelta, 0, playerMaxHealth);
            if (r.playerHpDelta < 0 && ui.playerHpText) StartCoroutine(ui.ShakeRect(ui.playerHpText.rectTransform, ui.shakeDuration, ui.shakeMagnitude));
        }
        RefreshAllUI();
    }

    Move GetEnemyMove(EnemyType type)
    {
        if (fx.enemySunLocked)
        {
            fx.enemySunLocked = false;
            return (Random.Range(0, 2) == 0) ? Move.Water : Move.Soil;
        }
        return (Move)Random.Range(0, 3);
    }

    void CheckRoundOver()
    {
        if (playerHealth <= 0)
        {
            special.Consume();
            spawner.setNumber = 1;
            spawner.currentHpScale = 1f;
            spawner.StartNewSet();
            StartNewCycle();
            return;
        }

        if (spawner.currentEnemy != null && spawner.currentEnemy.Health <= 0)
        {
            if (spawner.currentEnemy.Type == EnemyType.Normal)
            {
                playerHealth = playerMaxHealth;
                StartCoroutine(ui.EnemySlideTransition(SpawnNextNormal));
                return;
            }
            else
            {
                spawner.AdvanceAfterBossDefeat();
                StartCoroutine(ui.EnemySlideTransition(StartNextCycle));
                return;
            }
        }
        RefreshAllUI();
    }

    IEnumerator SpawnNextNormal()
    {
        spawner.SpawnNextEnemy();
        RefreshAllUI();
        yield break;
    }
    IEnumerator StartNextCycle()
    {
        StartNewCycle();
        yield break;
    }

    void MaybeQuip()
    {
        if (spawner.currentEnemy != null && spawner.currentEnemy.Health > 0 && playerHealth > 0)
        {
            if (Random.Range(1, quipOneInN + 1) == 1)
            {
                ui.MaybeQuip(RandomPlantTaunt, quipDuration);
            }
        }
    }

    string RandomPlantTaunt()
    {
        string[] lines = {
            "You’re about to get MULCHED!",
            "Photosynthesize this, punk.",
            "I’m ROOTIN for your demise!",
            "I'm gonna SOIL your plans!",
            "You're all BARK and no bite!",
            "I'll chlory-fill you with holes!"
        };
        return lines[Random.Range(0, lines.Length)];
    }
}
