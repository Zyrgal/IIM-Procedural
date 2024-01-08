using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spike is an hitbox script that continuously applies hits to player while touched.
/// </summary>
public class Spike : MonoBehaviour
{
	private void OnTriggerStay2D(Collider2D collision)
	{
		if (Player.Instance == null)
			return;
		if(collision.attachedRigidbody.gameObject != Player.Instance.gameObject)
			return;

		Player.Instance.ApplyHit(null);
	}

}
