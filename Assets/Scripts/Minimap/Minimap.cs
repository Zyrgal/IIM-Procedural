using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGenerator
{
    public class Minimap : MonoBehaviour
    {
        private static Minimap instance;
        public static Minimap Instance { get { if (instance == null) instance = FindObjectOfType<Minimap>(); return instance; } }

        public List<Tile> tiles = new List<Tile>();

        public GameObject tilePrefab;
        public RectTransform playerSprite;

        public Sprite unexploredTile;
        public Sprite exploredTile;

        public Color endTileColor;
        public Color startTileColor;
        public Color itemTileColor;
        public Color keyTileColor;

        Vector2 tileSize = new Vector2(32, 32);
        Vector2 bigTileSize = new Vector2(64, 64);

        public void InitMap(List<Node> nodes)
        {
            foreach (var tile in tiles)
            {
                if (tile.image != null)
                {
                    if (Application.isPlaying)
                        Destroy(tile.image.gameObject);
                    else
                        DestroyImmediate(tile.image.gameObject);
                }
            }
            tiles.Clear();


            foreach (var node in nodes)
            {
                Vector2 pos = node.Position * tileSize;
                Vector2 size = tileSize;
                GameObject tileObject = Instantiate(tilePrefab, transform.GetChild(0));
                tileObject.GetComponent<Image>().sprite = unexploredTile;

                switch (node.type)
                {
                    case NodeType.None:
                    case NodeType.Center:
                        continue;

                    case NodeType.MainPath:
                    case NodeType.Path:
                    case NodeType.Secret:
                    default:
                        break;

                    case NodeType.Start:
                        tileObject.GetComponent<Image>().color = startTileColor;
                        break;

                    case NodeType.End:
                        tileObject.GetComponent<Image>().color = endTileColor;
                        break;

                    case NodeType.Key:
                        tileObject.GetComponent<Image>().color = keyTileColor;
                        break;

                    case NodeType.Treasure:
                        tileObject.GetComponent<Image>().color = itemTileColor;
                        break;
                        
                    case NodeType.FourTile:
                        pos += new Vector2(16, 16);
                        size = bigTileSize;
                        break;
                }

                tileObject.GetComponent<RectTransform>().sizeDelta = size;
                tileObject.GetComponent<RectTransform>().localPosition = pos;
                tiles.Add(new Tile(tileObject, node));
            }

            UpdateVisual();
        }

        public void EnterRoom(Vector2 position)
        {
            if (tiles.Exists(e => e.Position == position))
            {
                Tile tile = tiles.Find(e => e.Position == position);
                tile.explored = true;
                tile.visible = true;

                playerSprite.localPosition = tile.Position * tileSize + (tile.node.type == NodeType.FourTile ? new Vector2(16, 16) : Vector2.zero);

                tiles.Where(e => e.node.type != NodeType.Secret && Vector3.Distance(e.node.Position, position) == 1).ToList().ForEach(e => e.visible = true);
            }

            UpdateVisual();
        }

        private void UpdateVisual()
        {
            foreach (var tile in tiles)
            {
                if (Application.isPlaying)
                    tile.image.enabled = tile.visible;

                tile.image.sprite = tile.explored ? exploredTile : unexploredTile;
            }
        }
    }

    [System.Serializable]
    public class Tile
    {
        public Node node;
        public Image image;
        public bool explored;
        public bool visible;
        public Vector2 Position => node.Position;

        public Tile(GameObject tileObj, Node node)
        {
            this.node = node;
            image = tileObj.GetComponent<Image>();

            explored = false;
            visible = false;
        }
    }
}