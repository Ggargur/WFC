using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using WaveFunction;

[ExecuteInEditMode]
public class WFCGenerator : MonoBehaviour
{
    public WFCDatabase database;

    [ContextMenu("Generate WFC Data")]
    public void Generate()
    {
        if (!database)
        {
            Debug.LogError("Database is null!");
            return;
        }

        var tilemap = GetComponent<Tilemap>();
        if (!tilemap)
        {
            Debug.LogError("Tilemap not found!");
            return;
        }

        var bounds = tilemap.cellBounds;

        // Clear
        database.cellTypes.Clear();
        database.sockets.Clear();
        ClearDatabase();

        var tileToCellType = new Dictionary<TileBase, CellType>();
        var socketMap = new Dictionary<(TileBase, Direction), Socket>();

        // EMPTY socket
        var emptySocket = ScriptableObject.CreateInstance<Socket>();
        emptySocket.name = "EMPTY";
        emptySocket.compatibleSockets = new();

        database.sockets.Add(emptySocket);
        AssetDatabase.AddObjectToAsset(emptySocket, database);

        foreach (var pos in bounds.allPositionsWithin)
        {
            var tile = tilemap.GetTile(pos);
            if (!tile) continue;

            if (!tileToCellType.ContainsKey(tile))
            {
                var cellType = ScriptableObject.CreateInstance<CellType>();
                cellType.name = tile.name;
                cellType.sockets = new();
                cellType.weight = 1f;
                cellType.CellInstance = new SingleTileCell(tile);

                tileToCellType[tile] = cellType;
                database.cellTypes.Add(cellType);

                AssetDatabase.AddObjectToAsset(cellType, database);
            }
        }

        foreach (var (tile, cellType) in tileToCellType)
        {
            foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
            {
                var socket = ScriptableObject.CreateInstance<Socket>();
                socket.name = $"{tile.name}_{dir}";
                socket.compatibleSockets = new();

                socketMap[(tile, dir)] = socket;
                cellType.sockets[dir] = socket;

                database.sockets.Add(socket);
                AssetDatabase.AddObjectToAsset(socket, database);
            }
        }

        foreach (var pos in bounds.allPositionsWithin)
        {
            var tile = tilemap.GetTile(pos);
            if (!tile) continue;

            foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
            {
                var neighborPos = pos + dir.ToVector3();
                var neighborTile = tilemap.GetTile(neighborPos);

                var socketA = socketMap[(tile, dir)];
                
                if(!neighborTile) continue;
                
                var socketB = socketMap[(neighborTile, dir.Opposite())];

                // A → B
                if (!socketA.compatibleSockets.ContainsKey(dir))
                    socketA.compatibleSockets[dir] = new List<Socket>();

                if (!socketA.compatibleSockets[dir].Contains(socketB))
                    socketA.compatibleSockets[dir].Add(socketB);

                // B → A
                var opposite = dir.Opposite();

                if (!socketB.compatibleSockets.ContainsKey(opposite))
                    socketB.compatibleSockets[opposite] = new List<Socket>();

                if (!socketB.compatibleSockets[opposite].Contains(socketA))
                    socketB.compatibleSockets[opposite].Add(socketA);
            }
        }

        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();

        Debug.Log("WFC generated!");
    }

    private void ClearDatabase()
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(database));

        foreach (var asset in assets)
        {
            if (asset == database) continue;

            Object.DestroyImmediate(asset, true);
        }

        database.cellTypes.Clear();
        database.sockets.Clear();

        EditorUtility.SetDirty(database);
    }
}