using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public enum Move { Sun, Water, Soil }
public enum EnemyType { Normal, Boss }
public enum BossKind { Sun, Water, Soil, Final }
enum PostDefeatAction { SpawnNextEnemy, StartNewCycle }

[Serializable]
public class BossVisual
{
    public BossKind kind;
    public string displayName;
    public Sprite bossSprite;
    public RuntimeAnimatorController animator;
}

[Serializable]
public class Enemy
{
    public EnemyType Type;
    public int MaxHealth;
    public int Health;

    public Enemy(EnemyType type, int hp)
    {
        Type = type;
        MaxHealth = hp;
        Health = hp;
    }
}
