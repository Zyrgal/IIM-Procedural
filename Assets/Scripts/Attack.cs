using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attack is an hitbox script that destroy itself after a given lifetime or when triggered.
/// When hitting player or enemy, applies damages and knockback to hit entity. See Player and Enemy "OnTriggerEnter2D" method for more details.
/// </summary>
public class Attack : MonoBehaviour {

	public enum AttackType
	{
		NONE = 0,
		ABSORBABLE = 1,
	}

	public enum AttackBonusType
	{
		DAMAGE,
		RANGE,
	}

	public struct AttackData
	{
		public AttackType attackType;
		public AttackBonusType attackBonusType;
		public int damage;
    }

	public AttackData attackData;
    public bool isRangeAttack = false;
	[ShowIf("isRangeAttack")]
	public float bulletSpeed = 1f;

    public bool hasInfiniteLifetime = false;
	[HideIf("hasInfiniteLifetime")]
    public float lifetime = 0.3f;
    public float knockbackSpeed = 3;
    public float knockbackDuration = 0.5f;
	public LayerMask destroyOnHit;

	private Rigidbody2D rb;

	[System.NonSerialized]
    public GameObject owner;

    void Start()
	{
        rb = GetComponent<Rigidbody2D>();
        attackData.attackType = (AttackType)1;

        if (isRangeAttack)
		{
            SetAttackBonusType();
        }
	}

	private void SetAttackBonusType()
	{
        float rand = UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(AttackBonusType)).Length);
		attackData.attackBonusType = (AttackBonusType)rand;
    }

	void Update () {
        if (isRangeAttack)
        {
            rb.velocity = transform.right * bulletSpeed;
        }

        if (hasInfiniteLifetime)
			return;

		lifetime -= Time.deltaTime;
		if (lifetime <= 0.0f)
		{
			GameObject.Destroy(gameObject);
		}
	}

    private void OnTriggerEnter2D(Collider2D collision)
    {
		if(((1 << collision.gameObject.layer) & destroyOnHit) != 0)
		{
			GameObject.Destroy(gameObject);
		}
	}
}
