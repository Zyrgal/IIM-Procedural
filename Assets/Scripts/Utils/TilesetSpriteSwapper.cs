using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TilesetSpriteSwapper : MonoBehaviour
{
    [Serializable]
    public struct TilesetSprite
    {
        public string variationId;
        public Sprite sprite;
    }

    public List<TilesetSprite> spritePerTileset = new List<TilesetSprite>();
    SpriteRenderer spriteRenderer;

    public void SetVariation(string variationId) {
        if (spriteRenderer == null) {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        int found = spritePerTileset.FindIndex(x => x.variationId == variationId);
        if (found == -1)
            return;
        spriteRenderer.sprite = spritePerTileset[found].sprite;
    }
}
