using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace WaveFunction
{
    public class MapCreator : SingletonBase<MapCreator>
    {
        public WFCDatabase database;
        public List<CellType> AllPossibilities => database.cellTypes;

        [Header("Configs")] public Vector3Int unitSize = Vector3Int.one;

        public int seed = 111;
        public bool useRandomizedSeed;
        public Vector3Int matrixSize = Vector3Int.one;

        [Header("Chunking")] public Vector3Int chunkSize;
        public int maxChunkRetries = 10;

        private readonly Stopwatch _currentStopWatch = new();
        private readonly Stack<Cell> _instancedCells = new();
        private bool IsDone => InstancedCells.Count == _grid.Length;
        [SerializeField] private int maxIterations = 1000;
        private Cell[] _grid;

        public static readonly Direction[]
            AllDirections = Enum.GetValues(typeof(Direction)).Cast<Direction>().ToArray();

        public const int MaxTurnAmount = 4;
        private System.Random _systemRandom = new();
        private bool HasCreatedMistake => !IsDone && _grid.Any(x => !x.IsCollapsable && !x.IsCollapsed);
        public List<Cell> InstancedCells => _instancedCells.ToList();
        private int GridMagnitude => matrixSize.x * matrixSize.y * matrixSize.z;


        private void Start()
        {
            if (useRandomizedSeed) return;

            _systemRandom = new System.Random(seed);
        }

        public Dictionary<Direction, Cell> GetNeighbors(Cell cell)
        {
            Dictionary<Direction, Cell> neighbors = new();
            var index = Array.IndexOf(_grid, cell);

            if (index < 0) return neighbors;

            var (x, y, z) = GetPosition(index);
            var pos = new Vector3Int(x, y, z);
            foreach (var dir in AllDirections)
            {
                var offset = dir.ToVector3();
                var neighborPos = pos + offset;

                if (IsInBounds(neighborPos)) neighbors.TryAdd(dir, _grid[GetIndex(neighborPos)]);
            }

            return neighbors;
        }

        private bool IsInBounds(Vector3Int neighborPos)
        {
            return neighborPos.x >= 0 && neighborPos.x < matrixSize.x &&
                   neighborPos.y >= 0 && neighborPos.y < matrixSize.y &&
                   neighborPos.z >= 0 && neighborPos.z < matrixSize.z;
        }

        private Cell SelectNext()
        {
            var options = GetNextOptions();
            if (options.Any()) return options.GetRandom(_systemRandom);

            if (!InstancedCells.Any()) return null;

            PopStack();
            return SelectNext();
        }

        private void PopStack()
        {
            var forcedOption = _instancedCells.Pop();
            forcedOption.Uncollapse();
        }

        private IEnumerable<Cell> GetNextOptions()
        {
            var allOptions = _grid.Where(x => x.IsCollapsable).ToList();
            if (!allOptions.Any())
                return allOptions;

            var min = allOptions.Min(x => x.Entropy);
            return allOptions.Where(x => Mathf.Approximately(x.Entropy, min));
        }

        [ContextMenu("Create Map")]
        public void Create()
        {
            TaskScheduler.UnobservedTaskException += (sender, e) => { Debug.LogException(e.Exception); };
            Clear();

            CreateRoutine().ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Debug.LogException(t.Exception);
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private async Task CreateRoutine()
        {
            _currentStopWatch.Start();
            _grid = GetGrid().ToArray();

            await CollapseAll();

            print($"Elapsed time: {_currentStopWatch.Elapsed}");
            _currentStopWatch.Stop();
            _currentStopWatch.Reset();
        }

        private async Task CollapseAll()
        {
            var chunks = GetChunks();

            foreach (var chunk in chunks)
            {
                bool success = false;

                for (int attempt = 0; attempt < maxChunkRetries; attempt++)
                {
                    ResetChunk(chunk);

                    try
                    {
                        await SolveChunk(chunk);
                        success = true;
                        break;
                    }
                    catch
                    {
                        // retry
                    }
                }

                if (!success)
                    throw new UnfortunateSeedException($"Chunk {chunk} failed.");
            }

            if (TryGetComponent(out Instantiator instantiator))
            {
                instantiator.InstantiateCells(_grid.Where(c => c.IsCollapsed).ToList());
            }
        }

        private void ResetChunk(List<Cell> chunk)
        {
            foreach (var cell in chunk)
            {
                cell.Uncollapse();
            }

            _instancedCells.Clear();
        }


        private IEnumerable<List<Cell>> GetChunks()
        {
            List<List<Cell>> chunks = new();

            for (int x = 0; x < matrixSize.x; x += chunkSize.x)
            for (int y = 0; y < matrixSize.y; y += chunkSize.y)
            for (int z = 0; z < matrixSize.z; z += chunkSize.z)
            {
                List<Cell> chunk = new();

                for (int i = 0; i < chunkSize.x; i++)
                for (int j = 0; j < chunkSize.y; j++)
                for (int k = 0; k < chunkSize.z; k++)
                {
                    var pos = new Vector3Int(x + i, y + j, z + k);

                    if (IsInBounds(pos))
                        chunk.Add(_grid[GetIndex(pos)]);
                }

                chunks.Add(chunk);
            }

            return chunks;
        }

        private async Task SolveChunk(List<Cell> chunk)
        {
            var iteration = 0;

            while (chunk.Any(c => !c.IsCollapsed))
            {
                if (iteration++ > maxIterations)
                    throw new UnfortunateSeedException();

                var next = SelectNext();

                Collapse(next);

                if (chunk.Any(c => !c.IsCollapsable && !c.IsCollapsed))
                    throw new UnfortunateSeedException();

                await Task.Yield();
            }
        }

        private void Collapse(Cell next)
        {
            if (next == null)
                throw new UnfortunateSeedException();

            if (!next.Collapse()) return;

            _instancedCells.Push(next);

            if (HasCreatedMistake)
                PopStack();
        }

        private IEnumerable<Cell> GetGrid()
        {
            _grid = new Cell[GridMagnitude];
            for (var i = 0; i < matrixSize.x; i++)
            for (var j = 0; j < matrixSize.y; j++)
            for (var k = 0; k < matrixSize.z; k++)
            {
                yield return new Cell(new Vector3Int(i, j, k), _systemRandom);
            }
        }

        [ContextMenu("Clear")]
        public void Clear()
        {
            _instancedCells.Clear();
            var allCells = FindObjectsByType<CellBehaviour>(FindObjectsSortMode.None);
            foreach (var cell in allCells) DestroyImmediate(cell.gameObject);
        }

        private int GetIndex(int x, int y, int z)
        {
            return z + y * matrixSize.z + x * matrixSize.y * matrixSize.z;
        }

        private int GetIndex(Vector3Int vector3)
        {
            return GetIndex(vector3.x, vector3.y, vector3.z);
        }

        private (int x, int y, int z) GetPosition(int index)
        {
            var x = index / (matrixSize.y * matrixSize.z);
            var remainder = index % (matrixSize.y * matrixSize.z);
            var y = remainder / matrixSize.z;
            var z = remainder % matrixSize.z;
            return (x, y, z);
        }

        private void OnValidate()
        {
            if (maxIterations <= GridMagnitude) maxIterations = GridMagnitude;
        }
    }
}