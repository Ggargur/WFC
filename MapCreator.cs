using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
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

        private List<Cell> GetNextOptions()
        {
            var allOptions = _grid.Where(x => x.IsCollapsable).ToList();
            if (!allOptions.Any())
                return allOptions;

            var min = allOptions.Min(x => x.Entropy);
            var options = allOptions.Where(x => Mathf.Approximately(x.Entropy, min)).ToList();
            return options;
        }

        [ContextMenu("Create Map")]
        public void Create()
        {
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                Debug.LogException(e.Exception);
            };
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
            FillGrid();
            
            await CollapseAll();

            print($"Elapsed time: {_currentStopWatch.Elapsed}");
            _currentStopWatch.Stop();
            _currentStopWatch.Reset();
        }

        private async Task CollapseAll()
        {
            var first = SelectNext();
            Collapse(first);

            try
            {
                await Propagate();
            }
            finally
            {
                if (TryGetComponent(out Instantiator instantiator))
                {
                    instantiator.InstantiateCells(InstancedCells);
                }
            }
        }

        private async Task Propagate()
        {
            int iteration;
            for (iteration = 0; iteration < maxIterations; iteration++)
            {
                if (IsDone)
                    break;

                var next = SelectNext();

                Collapse(next);
                Debug.Log($"Iteration {iteration}, Testing: {next.Index}, Entropy: {next.Entropy}, Progress: {_grid.Count(c => c.IsCollapsed) / (float)_grid.Length}");
                await Task.Yield();
            }

            if (iteration >= maxIterations)
                throw new UnfortunateSeedException("Couldn't finish grid on available time.");
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

        private void FillGrid()
        {
            _grid = new Cell[GridMagnitude];
            for (var i = 0; i < matrixSize.x; i++)
            for (var j = 0; j < matrixSize.y; j++)
            for (var k = 0; k < matrixSize.z; k++)
            {
                var index = GetIndex(i, j, k);
                _grid[index] = new Cell(new Vector3Int(i, j, k), _systemRandom);
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