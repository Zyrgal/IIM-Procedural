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

        public Sprite unexploredTile;
        public Sprite exploredTile;

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


            GameObject tileObject;
            foreach (var node in nodes)
            {
                switch (node.type)
                {
                    case NodeType.None:
                    case NodeType.Center:
                        continue;
                    case NodeType.Start:
                    case NodeType.MainPath:
                    case NodeType.Path:
                    case NodeType.End:
                    case NodeType.Secret:
                    case NodeType.Key:
                    case NodeType.Treasure:
                    default:
                        Vector3 pos = node.Position * tileSize;
                        tileObject = Instantiate(tilePrefab, transform);

                        tileObject.GetComponent<Image>().sprite = unexploredTile;
                        tileObject.GetComponent<RectTransform>().sizeDelta = tileSize;
                        tileObject.GetComponent<RectTransform>().localPosition = pos;
                        break;
                    case NodeType.FourTile:
                        Vector3 posBig = node.Position * tileSize + new Vector2(16, 16);
                        tileObject = Instantiate(tilePrefab, transform);

                        tileObject.GetComponent<Image>().sprite = unexploredTile;
                        tileObject.GetComponent<RectTransform>().sizeDelta = bigTileSize;
                        tileObject.GetComponent<RectTransform>().localPosition = posBig;

                        break;
                }

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