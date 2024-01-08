using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Abstract class for any collectible. A collectible behaviour must be associated to a hitbox in "trigger" mode.
/// When a collectible is collected, it triggers OnCollect then destroys itself.
/// </summary>
public abstract class ACollectible : MonoBehaviour {

    /// <summary>
    /// Checks if collectible is correctly associated with a trigger hitbox
    /// </summary>
    protected void Awake()
    {
        Collider2D collider = GetComponentInChildren<Collider2D>(true);
        Debug.Assert(collider != null && collider.isTrigger, "Collectible need a Collider2D in \"trigger\" mode to work properly");
    }

    // To be redefined !
    protected abstract void OnCollect();

    /// <summary>
    /// Calls OnCollect, then destroys itself.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.transform.parent != Player.Instance.gameObject.transform)
            return;

        OnCollect();
        Destroy(gameObject);
    }
}
