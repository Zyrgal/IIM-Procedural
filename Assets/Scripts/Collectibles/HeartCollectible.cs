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
    }
}
