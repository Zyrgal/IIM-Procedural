using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class DungeonGenerator : MonoBehaviour
{
    public Vector2Int mainPathLength = new Vector2Int(5, 10);
    private Vector2Int secondaryPathLength;
    private int numberOfSecondaryPaths;
    public int width = 10;
    public int height = 10;
    public int maxAttempts = 5;
    public int nodeMaxAttempts = 5;

    private List<Node> nodes;
    private List<Connection> connections;

    private void Start()
    {
        GenerateDungeon();
    }

    public void GenerateDungeon()
    {
        int attempts = 0;
        while (attempts < maxAttempts)
        {
            nodes = new List<Node>();
            connections = new List<Connection>();

            if (!CreateMainPath())
            {
                attempts++;
                continue;
            }

            CreateSecondaryPaths();
            ApplyAdditionalRules();

            if (IsDungeonValid())
            {
                // Convert the graph into a playable level (Instantiate GameObjects, set positions, etc.)
                // This step can be implemented separately
                Debug.Log("Dungeon generated successfully!");
                break;
            }
            else
            {
                attempts++;
            }
        }
    }

    private bool CreateMainPath()
    {
        int pathLength = Random.Range(mainPathLength.x, mainPathLength.y);
        Node startNode = new Node(0, 0, NodeType.Start);
        nodes.Add(startNode);

        CreatePath(startNode, PathType.Main, pathLength);

        nodes.Last().type = NodeType.End;
        connections.First(e => e.fromNode.type == NodeType.End || e.toNode.type == NodeType.End).type = ConnectionType.NeedKey;

        return true;
    }

    private void CreateSecondaryPaths()
    {
        List<Node> mainPath = nodes.Where(node => node.type == NodeType.MainPath || node.type == NodeType.End).ToList();
        int n = 0;

        secondaryPathLength = new Vector2Int(2, Mathf.Max(mainPath.Count / 3, 4));
        numberOfSecondaryPaths = mainPath.Count / 2;

        while (n <= numberOfSecondaryPaths || mainPath.Count <= 0)
        {
            int pathLength = Random.Range(0, 2) == 0 ? 1 : Random.Range(secondaryPathLength.x, secondaryPathLength.y);
            Node originNode = mainPath[Random.Range(0, mainPath.Count)];

            CreatePath(originNode, PathType.Secondary, pathLength);

            mainPath.Remove(originNode);
            n++;
        }
    }

    private void CreatePath(Node from, PathType pathType, int pathLength)
    {
        Node previousNode = from;
        Vector2 currentDirection = GetRandomDirection();

        for (int i = 1; i <= pathLength; i++)
        {
            Vector2 newPosition = previousNode.Position;
            int attempts = 0;
            while (!IsSlotValid(newPosition, pathType) && attempts < nodeMaxAttempts)
            {
                int randomTurn = Random.Range(0, 2); // 0: straight, 1: turn

                if (randomTurn == 1)
                    currentDirection = GetRandomDirection();

                newPosition = previousNode.Position + currentDirection;
                attempts++;
            }

            if (attempts >= nodeMaxAttempts && !IsSlotValid(newPosition, pathType))
                continue;

            Node currentNode = new Node((int)newPosition.x, (int)newPosition.y, pathType == PathType.Main ? NodeType.MainPath : NodeType.Path);
            nodes.Add(currentNode);

            foreach (var node in GetNeighboors(currentNode.Position))
            {
                Connection connection = new Connection(node, currentNode, ConnectionType.Open);
                connections.Add(connection);
            }

            previousNode = currentNode;
        }
    }

    private void ApplyAdditionalRules()
    {
        foreach (var node in nodes)
            if (HasEigthNeighboors(node.Position))
                node.type = NodeType.Center;

        nodes.RemoveAll(e => e.type == NodeType.Center);
        connections.RemoveAll(e => e.fromNode.type == NodeType.Center || e.toNode.type == NodeType.Center);
    }

    private bool IsDungeonValid()
    {
        // Implement validation logic here
        return true;
    }

    private bool IsSlotValid(Vector2 position, PathType pathType)
    {
        // Check if the node is within the bounds of the dungeon
        if (!IsWithinBounds(position))
            return false;

        // Check if the slot is not at the same position as another existing node
        if (IsNodeOverlap(position))
            return false;

        if (pathType == PathType.Main)
        {
            // Make sure it cannot generate a room next to another one (excepts the one it's from)
            if (GetNeighboors(position).Count > 1)
                return false;
        }
        else
        {
            // Check if the slot is not next to the End node
            if (IsNextToEndNode(position))
                return false;
        }

        // If all conditions are met, the slot is valid
        return true;
    }

    private bool IsWithinBounds(Vector2 position)
    {
        // Implement the logic to check if the position is within the bounds of the dungeon
        return Mathf.Abs(position.x) <= width / 2 && Mathf.Abs(position.y) <= height / 2;
    }

    private bool IsNodeOverlap(Vector2 position)
    {
        return nodes.Exists(node => node.Position == position);
    }

    private bool IsNextToEndNode(Vector2 position)
    {
        // Check if the slot is next to the End node
        Node endNode = nodes.Find(node => node.type == NodeType.End);

        if (endNode != null)
            return Mathf.Abs(position.x - endNode.x) <= 1 && Mathf.Abs(position.y - endNode.y) <= 1;

        return false;
    }

    private bool HasEigthNeighboors(Vector2 position)
    {
        // Wip c'est pas bon
        return nodes.Where(node => node.Position != position &&
                                   Mathf.Abs(node.x - position.x) < 2 && 
                                   Mathf.Abs(node.y - position.y) < 2).Count() == 8;
    }

    private List<Node> GetNeighboors(Vector2 position)
    {
        return nodes.Where(e => Vector3.Distance(e.Position, position) == 1).ToList();
    }

    private List<Vector2> GetEmptyNeighboors(Vector2 position)
    {
        List<Vector2> emptySlots = new List<Vector2>();

        // WIP

        return emptySlots;
    }

    private Vector2 GetRandomDirection()
    {
        int randomIndex = Random.Range(0, 4);
        switch (randomIndex)
        {
            case 0: return Vector2.up;
            case 1: return Vector2.down;
            case 2: return Vector2.left;
            case 3: return Vector2.right;
            default: return Vector2.right;
        }
    }

    private void OnDrawGizmosSelected()
    {
        foreach (var node in nodes)
        {
            switch (node.type)
            {
                case NodeType.Start:
                    Gizmos.color = Color.green;
                    break;
                case NodeType.MainPath:
                    Gizmos.color = Color.blue;
                    break;
                case NodeType.Path:
                    Gizmos.color = Color.magenta;
                    break;
                case NodeType.End:
                    Gizmos.color = Color.red;
                    break;
            }
            Gizmos.DrawCube(node.Position * 2, Vector3.one);
        }

        foreach (var connection in connections)
        {
            switch (connection.type)
            {
                case ConnectionType.Open:
                    Gizmos.color = Color.green;
                    break;
                case ConnectionType.NeedKey:
                    Gizmos.color = Color.red;
                    break;
                case ConnectionType.Hidden:
                    Gizmos.color = Color.blue;
                    break;
            }
            Vector2 pos = connection.fromNode.Position + connection.toNode.Position;
            Gizmos.DrawSphere(pos, 0.2f);
        }
    }
}

[System.Serializable]
public class Node
{
    public int x;
    public int y;
    public NodeType type;

    public Vector2 Position => new Vector2(x, y);

    public Node(int x, int y, NodeType type)
    {
        this.x = x;
        this.y = y;
        this.type = type;
    }
}

[System.Serializable]
public class Connection
{
    public Node fromNode;
    public Node toNode;
    public ConnectionType type;

    public Connection(Node fromNode, Node toNode, ConnectionType type)
    {
        this.fromNode = fromNode;
        this.toNode = toNode;
        this.type = type;
    }
}

public enum NodeType
{
    Start,
    MainPath,
    Path,
    End,
    Fusion,
    Center,
}

public enum ConnectionType
{
    Open,
    NeedKey,
    Hidden,
}

public enum PathType
{
    Main,
    Secondary,
    Other,
}

#if UNITY_EDITOR
[CustomEditor(typeof(DungeonGenerator))]
public class DungeonGeneratorEditor : Editor
{
    DungeonGenerator source => target as DungeonGenerator;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.Space();
        if (GUILayout.Button("Generate"))
        {
            source.GenerateDungeon();
        }
    }
}
#endif