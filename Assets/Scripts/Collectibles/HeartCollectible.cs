using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Heart collectible
/// </summary>
public class HeartCollectible : ACollectible {

    protected override void OnCollect()
    {
        Player.Instance.life++;

        if (GetComponentInParent<Choice>() != null)
            GetComponentInParent<Choice>().Destroy();
    }
}
