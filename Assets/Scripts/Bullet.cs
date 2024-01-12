using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum AttackType
{
    NONE = 0,
    ABSORBABLE = 1,
}

public enum AttackBonusType
{
    DAMAGE,
    RANGE,
    MOVESPEED,
}

public struct AttackData
{
    public AttackType attackType;
    public AttackBonusType attackBonusType;
    public int damage;
}

public class Bullet : MonoBehaviour
{
    public AttackData attackData;
    public float bulletSpeed = 1f;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        int rand = Random.Range(0, System.Enum.GetValues(typeof(AttackBonusType)).Length);
        attackData.attackType = (AttackType)rand;
        if (rand == 1)
        {
            SetAttackBonusType();
            SetBulletColor();
        }
    }

    private void SetAttackBonusType()
    {
        float rand = UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(AttackBonusType)).Length);
        attackData.attackBonusType = (AttackBonusType)rand;
    }

    private void SetBulletColor()
    {
        switch (attackData.attackBonusType)
        {
            case AttackBonusType.DAMAGE:
                spriteRenderer.color = Color.red;
                break;
            case AttackBonusType.RANGE:
                spriteRenderer.color = Color.cyan;
                break;
            case AttackBonusType.MOVESPEED:
                spriteRenderer.color = Color.yellow;
                break;
            default:
                break;
        }
    }

    void Update()
    {
        rb.velocity = transform.right * bulletSpeed;
    }
}
