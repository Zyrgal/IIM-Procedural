using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

/// <summary>
/// A simple pillarboxing script always keeping vertical bars around game for a given game view ratio (can be used if pixel-perfect camera is not wanted or to have a different pillar boxing visual)
/// </summary>
public class PillarBoxer : MonoBehaviour
{
    public RectTransform leftPillar;
    public RectTransform rightPillar;
    public float viewRatio = 11f/9f;

    float currentPillarSize = -1;

    void Start()
    {
        RefreshPillarSize();
    }

    private void Update()
    {
        RefreshPillarSize();
    }

    void RefreshPillarSize()
    {        
        float pillarSize = Mathf.Max(Screen.width - viewRatio * Screen.height, 0f) / 2f;
        if (currentPillarSize == pillarSize)
            return;

        currentPillarSize = pillarSize;
        leftPillar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, pillarSize);
        rightPillar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, pillarSize);
    }
}
