using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chest : ACollectible
{
    protected override void OnCollect()
    {
        Player.Instance.RandomUpgrade();

        if (GetComponentInParent<Choice>() != null)
            GetComponentInParent<Choice>().Destroy();
    }
}
