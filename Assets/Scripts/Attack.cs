﻿using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attack is an hitbox script that destroy itself after a given lifetime or when triggered.
/// When hitting player or enemy, applies damages and knockback to hit entity. See Player and Enemy "OnTriggerEnter2D" method for more details.
/// </summary>
public class Attack : MonoBehaviour {

	public int damage;
	public float range;
    public bool hasInfiniteLifetime = false;
	[HideIf("hasInfiniteLifetime")]
    public float lifetime = 0.3f;
    public float knockbackSpeed = 3;
    public float knockbackDuration = 0.5f;
	public LayerMask destroyOnHit;
	public bool isBullet = true;

	[SerializeField] private float attackSpeed;

	void Update () {

        if (hasInfiniteLifetime)
			return;

		if (!isBullet)
		{
            if (Player.Instance.transform.rotation.z != 0)
                transform.Rotate(transform.rotation.x, transform.rotation.y, transform.rotation.z + Time.deltaTime + attackSpeed, Space.Self);
            else
                transform.Rotate(transform.rotation.x, transform.rotation.y, transform.rotation.z + Time.deltaTime + attackSpeed + 1f, Space.Self);
        }

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
