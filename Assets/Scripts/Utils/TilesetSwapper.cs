using CreativeSpore.SuperTilemapEditor;
using NaughtyAttributes;
using System;
using System.Collections.Generic;
using UnityEngine;

public class TilesetSwapper : MonoBehaviour
{
    [Serializable]
    public struct VariationTileset
    {
        public string tilemapLayer;
        public Tileset tileset;
    }

    [Serializable]
    public struct Variation
    {
        public string variationId;
        public List<VariationTileset> tilesets;
    }

    public List<Variation> variations = new List<Variation>();

    private int index = 0;

    [Button("Switch tileset")]
    void SwitchTileset()
    {
        if (variations.Count == 0)
            return;
        index = (index+1)% variations.Count;
        ApplyTilesetsOnTilemaps();
    }

    public void SetVariation(string variationId)
    {
        int found = variations.FindIndex(x => x.variationId == variationId);
        if (found == -1)
            return;
        index = found;
        ApplyTilesetsOnTilemaps();
    }

    void ApplyTilesetsOnTilemaps()
    {
        TilemapGroup[] tilemapGroups = GameObject.FindObjectsByType<TilemapGroup>(FindObjectsSortMode.InstanceID);
        if (tilemapGroups == null || tilemapGroups.Length == 0)
            return;
        foreach (TilemapGroup tilemapGroup in tilemapGroups)
        {
            foreach (VariationTileset tilesetData in variations[index].tilesets)
            {

                STETilemap tilemap = tilemapGroup.FindTilemapByName(tilesetData.tilemapLayer);
                if (tilemap == null)
                    continue;
                tilemap.Tileset = tilesetData.tileset;
            }

            TilesetSpriteSwapper[] spriteSwappers = tilemapGroup.gameObject.GetComponentsInChildren<TilesetSpriteSwapper>(true);
            foreach(TilesetSpriteSwapper spriteSwapper in spriteSwappers)
            {
                spriteSwapper.SetVariation(variations[index].variationId);
            }
        }
    }

}
