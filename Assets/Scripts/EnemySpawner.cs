using UnityEngine;
using System.Collections.Generic;
using TMPro;
public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Settings")]
    public int normalHealth = 5;
    public int bossHealth = 10;
    public int normalsPerCycle = 1;
    public float hpScalePerSet = 1.25f;
    public float finalBossHpMultiplier = 1.3f;

    [Header("Visual Mappings")]
    public Sprite normalEnemySpriteOverride;
    public RuntimeAnimatorController normalAnimator;
    public List<BossVisual> bossVisuals = new();

    [Header("UI Targets")]
    public UnityEngine.UI.Image enemyImage;
    public TMP_Text bossTitle;
    public TMP_Text bossAppearsText;
    public Animator plantAnimationController;

    // State
    public Enemy currentEnemy { get; private set; }
    public int normalsRemainingInCycle { get; private set; }
    public int cycleNumber { get; private set; } = 1;
   public float currentHpScale { get; set; } = 1f;
    public int setNumber { get; set; } = 1;

    public bool bossThisCycleIsFinal { get; private set; } = false;
    public BossKind currentBossKind { get; private set; } = BossKind.Sun;
    public List<BossKind> pendingBosses = new();

    int wave = 1;

    public void StartNewSet()
    {
        pendingBosses.Clear();
        pendingBosses.Add(BossKind.Sun);
        pendingBosses.Add(BossKind.Water);
        pendingBosses.Add(BossKind.Soil);
        ShuffleList(pendingBosses);
        bossThisCycleIsFinal = false;
        cycleNumber = 1;
        wave = 1;
    }

    public void StartNewCycle()
    {
        normalsRemainingInCycle = normalsPerCycle;
        ClearBossUI();
        SetAnimBoolsForCycleStart();
    }

    public void SpawnNextEnemy()
    {
        if (normalsRemainingInCycle > 0)
        {
            int hp = Mathf.RoundToInt(normalHealth * currentHpScale);
            currentEnemy = new Enemy(EnemyType.Normal, hp);
            ApplyNormalVisuals();
            normalsRemainingInCycle--;
        }
        else
        {
            if (!bossThisCycleIsFinal)
            {
                if (pendingBosses.Count > 0) currentBossKind = pendingBosses[0];
                else { bossThisCycleIsFinal = true; currentBossKind = BossKind.Final; }
            }

            float mult = bossThisCycleIsFinal ? finalBossHpMultiplier : 1f;
            int hp = Mathf.RoundToInt(bossHealth * currentHpScale * mult);
            currentEnemy = new Enemy(EnemyType.Boss, hp);
            ApplyBossVisuals(currentBossKind);
            ShowBossTitle(currentBossKind);
            if (bossTitle) bossTitle.text = BossLabel(currentBossKind);
            wave++;
        }
    }

    public void AdvanceAfterBossDefeat()
    {
        if (!bossThisCycleIsFinal)
        {
            if (currentBossKind == BossKind.Sun)   ; // unlocks handled elsewhere
            if (currentBossKind == BossKind.Water) ;
            if (currentBossKind == BossKind.Soil)  ;
            if (pendingBosses.Count > 0) pendingBosses.RemoveAt(0);
        }
        else
        {
            setNumber++;
            currentHpScale *= hpScalePerSet;
            StartNewSet();
        }
        cycleNumber++;
    }

    // helpers
    void ApplyNormalVisuals()
    {
        if (enemyImage && normalEnemySpriteOverride)
            enemyImage.sprite = normalEnemySpriteOverride;

        if (plantAnimationController && normalAnimator)
        {
            plantAnimationController.runtimeAnimatorController = normalAnimator;
            plantAnimationController.Rebind();
            plantAnimationController.Update(0f);
            plantAnimationController.Play("Idle", 0, 0f);
        }
    }

    BossVisual GetBossVisual(BossKind kind)
    {
        foreach (var v in bossVisuals) if (v.kind == kind) return v;
        return null;
    }

  void ApplyBossVisuals(BossKind kind)
    {
        var vis = GetBossVisual(kind);
        if (vis == null) return;

        if (enemyImage)
        {
            if (vis.bossSprite) enemyImage.sprite = vis.bossSprite;
            enemyImage.enabled = true;
        }

        if (plantAnimationController)
        {
            if (vis.animator)
            {
                plantAnimationController.runtimeAnimatorController = vis.animator;
                plantAnimationController.Rebind();
                plantAnimationController.Update(0f);
                plantAnimationController.Play("Idle", 0, 0f);
            }
        }

        if (bossTitle) bossTitle.text = vis.displayName;
    }


    void ShowBossTitle(BossKind kind)
    {
        if (!bossAppearsText) return;
        string title = kind switch
        {
            BossKind.Sun   => "Sunny Appears!",
            BossKind.Water => "Watero Appears!",
            BossKind.Soil  => "Soiler Appears!",
            BossKind.Final => "The mcfuckler Appears!",
            _              => "Boss Appears!"
        };
        bossAppearsText.text = title;
    }

    string BossLabel(BossKind k) =>
        k == BossKind.Sun ? "Sunny" :
        k == BossKind.Water ? "Watero" :
        k == BossKind.Soil ? "Soiler" :
        "the mcfuckler";

    void ClearBossUI()
    {
        if (bossTitle) bossTitle.text = "";
        if (bossAppearsText) bossAppearsText.text = "";
    }

    void SetAnimBoolsForCycleStart()
    {
        if (!plantAnimationController) return;
        plantAnimationController.SetBool("shouldTransitionTo1", true);
        plantAnimationController.SetBool("shouldTransitionTo2", false);
        plantAnimationController.SetBool("shouldTransitionTo3", false);
        plantAnimationController.SetBool("shouldTransitionTo4", false);
        plantAnimationController.SetBool("shouldTransitionTo5", false);
        plantAnimationController.SetBool("shouldTransitionTo6", false);
        plantAnimationController.SetBool("shouldTransitionTo7", false);
        plantAnimationController.SetBool("shouldTransitionTo8", false);
    }

    void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
