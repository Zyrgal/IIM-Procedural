using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class DungeonGenerator : MonoBehaviour
{
    public List<GenerationPreset> generationPresets = new List<GenerationPreset>();
    public int difficulty = 0;
    public int maxAttempts;
    public int nodeMaxAttempts;
    public Vector2Int roomSize;

    private Vector2Int secondaryPathLength;
    private int numberOfSecondaryPaths;

    public float generationDelay = 0.1f;

    public List<Node> nodes;
    public List<Connection> connections;

    [Header("Rooms")]
    public List<Room> basicRooms;
    public List<Room> deadEndRooms;
    public List<Room> startRoom;
    public List<Room> endRoom;
    public List<Room> bigRooms;
    public List<Room> keyRooms;
    public List<Room> secretRooms;
    public List<Room> itemRooms;

    private void Start()
    {
        StartCoroutine(GenerateDungeon());
    }

    public IEnumerator GenerateDungeon()
    {
        if (difficulty >= generationPresets.Count)
            difficulty = generationPresets.Count - 1;

        int attempts = 0;
        while (attempts < maxAttempts)
        {
            nodes = new List<Node>();
            connections = new List<Connection>();

            yield return StartCoroutine(CreateMainPath());

            if (nodes.Count() >= generationPresets[difficulty].mainPathLength.x)
            {
                yield return StartCoroutine(CreateSecondaryPaths());
                yield return StartCoroutine(ApplyAdditionalRules());

                if (IsDungeonValid())
                {
                    // Convert the graph into a playable level (Instantiate GameObjects, set positions, etc.)
                    if (Application.isPlaying)
                        SpawnRooms();

                    Debug.Log("Dungeon generated successfully!");
                    break;
                }
                else
                {
                    attempts++;
                }
            }
            else
                attempts++;
        }

        yield return null;
    }

    private IEnumerator CreateMainPath()
    {
        // Create Start node
        int pathLength = Random.Range(generationPresets[difficulty].mainPathLength.x, generationPresets[difficulty].mainPathLength.y);
        int x = 0; // Random.Range(-generationPresets[difficulty].width / 2, generationPresets[difficulty].width / 2);
        int y = 0; // Random.Range(-generationPresets[difficulty].height/ 2, generationPresets[difficulty].height / 2);
        Node startNode = new Node(x, y, NodeType.Start);
        nodes.Add(startNode);

        // Generate main path
        yield return StartCoroutine(CreatePath(startNode, PathType.Main, pathLength));

        // Replace the last node of the path by an End node and connect it
        nodes.Last().type = NodeType.End;
        connections.First(e => e.IsConnectedTo(NodeType.End)).type = ConnectionType.NeedKey;

        yield return null;
    }

    private IEnumerator CreateSecondaryPaths()
    {
        // Get the main path to start secondary path from it
        List<Node> mainPath = nodes.Where(node => node.type == NodeType.MainPath || node.type == NodeType.End).ToList();

        // Automatically determine the paths length and how many there should be
        secondaryPathLength = new Vector2Int(2, Mathf.Max(mainPath.Count / 3, 4));
        numberOfSecondaryPaths = mainPath.Count / 2;

        int n = 0;
        while (n <= numberOfSecondaryPaths || mainPath.Count <= 0)
        {
            // 50% chance to only be 1 node long or be longer
            int pathLength = Random.Range(0, 2) == 0 ? 1 : Random.Range(secondaryPathLength.x, secondaryPathLength.y);
            Node originNode = mainPath[Random.Range(0, mainPath.Count)];

            yield return StartCoroutine(CreatePath(originNode, PathType.Secondary, pathLength));

            // Remove the used node from the pool of possible path origin
            mainPath.Remove(originNode);
            n++;
        }

        yield return null;
    }

    private IEnumerator CreatePath(Node from, PathType pathType, int pathLength)
    {
        Node previousNode = from;

        // Need to change the GetRandomDirection to a GetRandomValidDirection or something to be more reliable
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

            previousNode = AddNode(newPosition, pathType == PathType.Main ? NodeType.MainPath : NodeType.Path);

            yield return new WaitForSeconds(generationDelay);
        }
    }

    public Node AddNode(Vector2 position, NodeType nodeType)
    {
        Node node = new Node((int)position.x, (int)position.y, nodeType);
        ConnectionType connectionType;

        switch (nodeType)
        {
            case NodeType.None:
                connectionType = ConnectionType.None;
                break;

            case NodeType.Secret:
                connectionType = ConnectionType.Hidden;
                break;

            case NodeType.End:
                connectionType = ConnectionType.NeedKey;
                break;

            case NodeType.Start:
            case NodeType.MainPath:
            case NodeType.Path:
            case NodeType.FourTile:
            case NodeType.Center:
            case NodeType.Key:
            default:
                connectionType = ConnectionType.Open;
                break;
        }

        nodes.Add(node);
        foreach (var neighboor in GetNeighboors(node.Position))
        {
            Connection connection = new Connection(node, neighboor, connectionType);
            connections.Add(connection);
        }

        return node;
    }

    private IEnumerator ApplyAdditionalRules()
    {
        // Tag the node that are surrounded by 8 other
        foreach (var node in nodes)
            if (node.type != NodeType.Start && HasEigthNeighboors(node.Position))
                node.type = NodeType.Center;

        // Remove them and their connection
        nodes.RemoveAll(e => e.type == NodeType.Center);
        connections.RemoveAll(e => e.fromNode.type == NodeType.Center || e.toNode.type == NodeType.Center);

        yield return new WaitForSeconds(generationDelay);

        // Get all the available slots
        List<Vector2> emptySlots = new List<Vector2>(); 
        foreach (var node in nodes)
            emptySlots.AddRange(GetEmptyNeighboors(node.Position, PathType.Other));
        emptySlots = emptySlots.Distinct().ToList();

        // Key room
        emptySlots = emptySlots.OrderByDescending(e => GetNeighboors(e).Count).ToList();
        AddNode(GetDeadEnd(), NodeType.Key);
        emptySlots.RemoveAt(emptySlots.Count - 1);

        yield return new WaitForSeconds(generationDelay);

        // Treasure room
        AddNode(GetDeadEnd(), NodeType.Treasure);
        emptySlots.RemoveAt(emptySlots.Count - 1);

        yield return new WaitForSeconds(generationDelay);

        // Secret room
        emptySlots = emptySlots.OrderByDescending(e => GetAllNeighboors(e).Count).ToList();
        AddNode(emptySlots[0], NodeType.Secret);
        emptySlots.RemoveAt(0);

        yield return new WaitForSeconds(generationDelay);

        // Fuse pack of 4 rooms
        // C'est pas giga beau mais time
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].type == NodeType.MainPath || nodes[i].type != NodeType.Path)
                continue;

            Node bottomLeft = nodes[i];

            Node topLeft = nodes.Find(e => (e.type == NodeType.MainPath || e.type == NodeType.Path) && e.Position == bottomLeft.Position + Vector2.up);
            Node bottomRight = nodes.Find(e => (e.type == NodeType.MainPath || e.type == NodeType.Path) && e.Position == bottomLeft.Position + Vector2.right);
            Node topRight = nodes.Find(e => (e.type == NodeType.MainPath || e.type == NodeType.Path) && e.Position == bottomLeft.Position + Vector2.up + Vector2.right);

            if (topLeft != null && bottomRight != null && topRight != null)
            {
                bottomLeft.type = NodeType.FourTile;
                nodes.Remove(topLeft);
                nodes.Remove(bottomRight);
                nodes.Remove(topRight);

                i= 0;
                yield return new WaitForSeconds(generationDelay);
            }
        }


        yield return null;
    }

    #region Conditions
    private bool IsDungeonValid()
    {
        // Implement validation logic here
        return true;
    }

    private bool IsSlotValid(Vector2 position, PathType pathType)
    {
        // Check if the node is within the bounds of the dungeon
        if (pathType != PathType.Other && !IsWithinBounds(position))
            return false;

        // Check if the slot is not at the same position as another existing node
        if (IsNodeOverlap(position))
            return false;

        // Make sure it cannot generate a room next to another one in the main path (excepts the one it's from)
        if (pathType == PathType.Main && GetNeighboors(position).Count > 1)
            return false;

        // Check if the slot is not next to the End node
        if (IsNextToEndNode(position))
            return false;

        // If all conditions are met, the slot is valid
        return true;
    }

    private bool IsWithinBounds(Vector2 position)
    {
        // Implement the logic to check if the position is within the bounds of the dungeon
        return Mathf.Abs(position.x) <= generationPresets[difficulty].width / 2 && Mathf.Abs(position.y) <= generationPresets[difficulty].height / 2;
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
        return GetAllNeighboors(position).Count == 8;
    }
    #endregion

    private List<Node> GetNeighboors(Vector2 position)
    {
        return nodes.Where(e => Vector3.Distance(e.Position, position) == 1).ToList();
    }

    private List<Node> GetAllNeighboors(Vector2 position)
    {
        return nodes.Where(node => node.Position != position &&
                                   Mathf.Abs(node.x - position.x) <= 1 &&
                                   Mathf.Abs(node.y - position.y) <= 1).ToList();
    }

    private List<Vector2> GetEmptyNeighboors(Vector2 position, PathType pathType)
    {
        List<Vector2> neighboors = new List<Vector2>();

        if (IsSlotValid(position + Vector2.up, pathType))
            neighboors.Add(position + Vector2.up);

        if (IsSlotValid(position + Vector2.right, pathType))
            neighboors.Add(position + Vector2.right);

        if (IsSlotValid(position + Vector2.down, pathType))
            neighboors.Add(position + Vector2.down);

        if (IsSlotValid(position + Vector2.left, pathType))
            neighboors.Add(position + Vector2.left);

        return neighboors;
    }

    private Vector2 GetDeadEnd()
    {
        List<Vector2> possibleSlots = new List<Vector2>();

        foreach (var node in nodes)
        {
            if (node.type != NodeType.Path || node.type != NodeType.MainPath)

            possibleSlots.AddRange(GetEmptyNeighboors(node.Position, PathType.Main));
        }

        return possibleSlots[Random.Range(0, possibleSlots.Count)];
    }

    private ConnectionType GetConnectionType(Vector2 position, Utils.ORIENTATION orientation)
    {
        Vector2 positionToCheck = Vector2.zero;

        switch (orientation)
        {
            case Utils.ORIENTATION.NONE:
                return ConnectionType.None;
            case Utils.ORIENTATION.NORTH:
                positionToCheck = position + Vector2.up;
                break;
            case Utils.ORIENTATION.EAST:
                positionToCheck = position + Vector2.right;
                break;
            case Utils.ORIENTATION.SOUTH:
                positionToCheck = position + Vector2.down;
                break;
            case Utils.ORIENTATION.WEST:
                positionToCheck = position + Vector2.left;
                break;
        }

        // Check if a connection corresponding to the orientation exists and return it's type if it does
        Connection connection = connections.FirstOrDefault(e => e.fromNode.Position == position && e.toNode.Position == positionToCheck ||
                                                                e.fromNode.Position == positionToCheck && e.toNode.Position == position);

        if (connection != default)
            return connection.type;

        return ConnectionType.None;
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

    private List<Vector2> GetValidDirections(Vector2 position, PathType pathType)
    {
        List<Vector2> orientations = new List<Vector2>();

        if (IsSlotValid(position + Vector2.up, pathType))
            orientations.Add(Vector2.up);

        if (IsSlotValid(position + Vector2.right, pathType))
            orientations.Add(Vector2.right);

        if (IsSlotValid(position + Vector2.down, pathType))
            orientations.Add(Vector2.down);

        if (IsSlotValid(position + Vector2.left, pathType))
            orientations.Add(Vector2.left);

        return orientations;
    }

    public void ClearRooms()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (!Application.isPlaying)
                DestroyImmediate(transform.GetChild(i).gameObject);
            else
                Destroy(transform.GetChild(i).gameObject);
        }
    }

    public void SpawnRooms()
    {
        ClearRooms();

        // Generate rooms
        foreach (var node in nodes)
        {
            Room room = null;

            // Determine which room to place
            switch (node.type)
            {
                // Start rooms
                case NodeType.Start:    
                    room = startRoom[Random.Range(0, startRoom.Count)];
                    break;

                // End rooms
                case NodeType.End:      
                    room = endRoom[Random.Range(0, endRoom.Count)];
                    break;

                // 4 tile rooms
                case NodeType.FourTile:   
                    room = bigRooms[Random.Range(0, bigRooms.Count)];
                    break;

                // Key rooms
                case NodeType.Key:
                    room = keyRooms[Random.Range(0, keyRooms.Count)];
                    break;

                // Secret rooms
                case NodeType.Secret:
                    room = secretRooms[Random.Range(0, secretRooms.Count)];
                    break;

                case NodeType.Treasure:
                    room = itemRooms[Random.Range(0, itemRooms.Count)];
                    break;

                // Basic/Default rooms
                case NodeType.MainPath:
                case NodeType.Path:
                default:
                    room = basicRooms[Random.Range(0, basicRooms.Count)];
                    break;
            }

            // Spawn and setup the room
            room = Instantiate(room, node.Position * roomSize, Quaternion.identity);
            room.transform.parent = transform;
            room.position = new Vector2Int(node.x, node.y);

            // Setup the doors (there is a lot of optimization to be made concerning the connections and doors, but time)
            // Doors will not be setup properly without being in runtime
            for (int x = 0; x < room.size.x; x++)
            {
                for (int y = 0; y < room.size.y; y++)
                {
                    foreach (var door in room.GetDoors(new Vector2Int(x, y)))
                    {
                        switch (GetConnectionType(node.Position + new Vector2Int(x, y), door.Orientation))
                        {
                            case ConnectionType.None:
                                door.SetState(Door.STATE.WALL);
                                break;
                            case ConnectionType.Open:
                                door.SetState(Door.STATE.OPEN);
                                break;
                            case ConnectionType.NeedKey:
                                door.SetState(Door.STATE.CLOSED);
                                break;
                            case ConnectionType.Hidden:
                                door.SetState(Door.STATE.SECRET);
                                break;
                        }
                    }
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 roomGizmoSize = new Vector2(5.5f, 4.5f);
        Vector2 bigRoomGizmoSize = roomGizmoSize * 3;
        float connectionGizmoSize = 0.5f;

        // Room visuals
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
                    Gizmos.color = Color.cyan;
                    break;
                case NodeType.FourTile:
                    Gizmos.color = Color.blue;
                    break;
                case NodeType.Secret:
                    Gizmos.color = Color.white;
                    break;
                case NodeType.Key:
                    Gizmos.color = Color.magenta;
                    break;
                case NodeType.Treasure:
                    Gizmos.color = Color.yellow;
                    break;
                case NodeType.End:
                    Gizmos.color = Color.red;
                    break;
            }

            Vector2 pos = node.Position * roomSize + roomSize / 2;
            if (node.type == NodeType.FourTile)
                Gizmos.DrawCube(pos + roomSize / 2, bigRoomGizmoSize);
            else
                Gizmos.DrawCube(pos, roomGizmoSize);
        }

        // Connection visuals
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
            Vector2 pos = (connection.fromNode.Position + connection.toNode.Position) / 2 * roomSize + roomSize / 2;
            Gizmos.DrawSphere(pos, connectionGizmoSize);
        }

        // Generation limits
        Gizmos.color = Color.black;
        Gizmos.DrawCube(new Vector2(generationPresets[difficulty].width / 2, generationPresets[difficulty].height / 2) * roomSize, Vector2.one);
        Gizmos.DrawCube(new Vector2(generationPresets[difficulty].width / 2, -generationPresets[difficulty].height / 2) * roomSize, Vector2.one);
        Gizmos.DrawCube(new Vector2(-generationPresets[difficulty].width / 2, generationPresets[difficulty].height / 2) * roomSize, Vector2.one);
        Gizmos.DrawCube(new Vector2(-generationPresets[difficulty].width / 2, -generationPresets[difficulty].height / 2) * roomSize, Vector2.one);
    }
}

#region Classes and enums
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

    public bool IsConnectedTo(Node node)
    {
        if (fromNode == node || toNode == node)
            return true;
        else
            return false;
    }

    public bool IsConnectedTo(NodeType nodeType)
    {
        if (fromNode.type == nodeType || toNode.type == nodeType)
            return true;
        else
            return false;
    }
}

[System.Serializable]
public struct GenerationPreset
{
    public Vector2Int mainPathLength;
    public int width;
    public int height;

    public GenerationPreset(Vector2Int mainPathLength)
    {
        this.mainPathLength = mainPathLength;
        width = 8;
        height = 8;
    }
}

public enum NodeType
{
    None,
    Start,
    MainPath,
    Path,
    End,
    FourTile,
    Center,
    Secret,
    Key,
    Treasure,
}

public enum ConnectionType
{
    None,
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
#endregion

#if UNITY_EDITOR
[CustomEditor(typeof(DungeonGenerator))]
public class DungeonGeneratorEditor : Editor
{
    DungeonGenerator source => target as DungeonGenerator;

    private void OnEnable()
    {
        LoadRooms();
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.Space();
        GUILayout.BeginHorizontal("box");
        if (GUILayout.Button("Generate graph"))
            source.StartCoroutine(source.GenerateDungeon());
        if (GUILayout.Button("Clear graph"))
        {
            source.nodes = new List<Node>();
            source.connections = new List<Connection>();
        }
        GUILayout.EndHorizontal();

        //EditorGUILayout.Space();

        GUILayout.BeginHorizontal("box");
        if (GUILayout.Button("Generate layout from graph"))
            source.SpawnRooms();
        if (GUILayout.Button("Clear layout"))
            source.ClearRooms();
        GUILayout.EndHorizontal();
    }

    void LoadRooms()
    {
        string pathBasic = "Assets/Prefabs/Rooms/BasicRooms";
        source.basicRooms.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new string[] { pathBasic }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            source.basicRooms.Add(AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<Room>());
        }


        string pathBig = "Assets/Prefabs/Rooms/BigRooms";
        source.bigRooms.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new string[] { pathBig }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            source.bigRooms.Add(AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<Room>());
        }


        string pathStart = "Assets/Prefabs/Rooms/StartRooms";
        source.startRoom.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new string[] { pathStart }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            source.startRoom.Add(AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<Room>());
        }


        string pathEnd = "Assets/Prefabs/Rooms/EndRooms";
        source.endRoom.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new string[] { pathEnd }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            source.endRoom.Add(AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<Room>());
        }


        string pathDeadEnd = "Assets/Prefabs/Rooms/DeadEndRooms";
        source.deadEndRooms.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new string[] { pathDeadEnd }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            source.deadEndRooms.Add(AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<Room>());
        }


        string pathKey = "Assets/Prefabs/Rooms/KeyRooms";
        source.keyRooms.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new string[] { pathKey }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            source.keyRooms.Add(AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<Room>());
        }


        string pathSecret = "Assets/Prefabs/Rooms/SecretRooms";
        source.secretRooms.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new string[] { pathSecret }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            source.secretRooms.Add(AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<Room>());
        }

        string pathItem = "Assets/Prefabs/Rooms/ItemRooms";
        source.itemRooms.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new string[] { pathItem }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            source.itemRooms.Add(AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<Room>());
        }
    }
}
#endif