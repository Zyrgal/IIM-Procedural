using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Key collectible
/// </summary>
public class KeyCollectible : ACollectible {

    protected override void OnCollect()
    {
        Player.Instance.KeyCount++;
    }
}
