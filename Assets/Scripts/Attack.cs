using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attack is an hitbox script that destroy itself after a given lifetime or when triggered.
/// When hitting player or enemy, applies damages and knockback to hit entity. See Player and Enemy "OnTriggerEnter2D" method for more details.
/// </summary>
public class Attack : MonoBehaviour {

    public int damages = 1;
	public bool hasInfiniteLifetime = false;
	[HideIf("hasInfiniteLifetime")]
    public float lifetime = 0.3f;
    public float knockbackSpeed = 3;
    public float knockbackDuration = 0.5f;
	public LayerMask destroyOnHit;

	[System.NonSerialized]
    public GameObject owner;
	
	void Update () {
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
