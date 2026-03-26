using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WaveFunction
{
    public class Cell : IComparable<Cell>
    {
        public Dictionary<Direction, Cell> Neighbors
        {
            get
            {
                if (_neighbors == null)
                {
                    _neighbors = MapCreator.Instance.GetNeighbors(this);
                    foreach (var (_, neighbor) in _neighbors)
                    {
                        neighbor.OnCollapsed += SetDirty;
                    }
                }

                return _neighbors;
            }
        }

        public bool IsCollapsable => !IsCollapsed && Possibilities.Any();
        public bool IsCollapsed => CollapsedType != null;

        private event Action<Cell> OnCollapsed;

        private IEnumerable<CellType> Possibilities
        {
            get
            {
                if (!_isDirty) return _possibilities;

                _possibilities = new List<CellType>();

                var snapshot = CurrentSnapshot;
                _testedOptions.TryGetValue(snapshot, out var tried);
                var testedOptions = tried ?? new HashSet<CellType>();

                foreach (var possibility in MapCreator.Instance.AllPossibilities)
                {
                    if (testedOptions.Contains(possibility)) continue;

                    bool valid = true;

                    foreach (var direction in MapCreator.AllDirections)
                    {
                        Neighbors.TryGetValue(direction, out var neighbor);
                        if (neighbor == null) continue;

                        bool compatibleAnyRotation = false;

                        for (int rotation = 0; rotation < possibility.MaxTurns; rotation++)
                        {
                            if (possibility.IsCompatible(direction, rotation, neighbor))
                            {
                                compatibleAnyRotation = true;
                                break;
                            }
                        }

                        if (!compatibleAnyRotation)
                        {
                            valid = false;
                            break;
                        }
                    }

                    if (valid)
                        _possibilities.Add(possibility);
                }

                _isDirty = false;
                Entropy = ComputedEntropy;
                return _possibilities;
            }
        }

        public float Entropy { get; private set; }

        private float ComputedEntropy
        {
            get
            {
                var weights = Possibilities.Select(p => p.weight).ToList();
                if (!weights.Any())
                    return float.PositiveInfinity;

                var avg = weights.Average();
                var diffs = weights.Sum(n => (n - avg) * (n - avg));
                return diffs / weights.Count;
            }
        }

        public Vector3 Position => Vector3.Scale(Index, MapCreator.Instance.unitSize);

        private readonly Dictionary<NeighborSnapshot, HashSet<CellType>> _testedOptions = new();
        private List<CellType> _possibilities;

        public CellType CollapsedType
        {
            get => _collapsedType;
            private set
            {
                _collapsedType = value;
                OnCollapsed?.Invoke(this);
            }
        }

        private CellType _collapsedType;

        public int AmountOfTurns { get; private set; }

        private readonly System.Random _random;
        public readonly Vector3Int Index;

        private bool _isDirty = true;
        private Dictionary<Direction, Cell> _neighbors;

        public Cell(Vector3Int index, System.Random random)
        {
            Index = index;
            _random = random;
        }

        public int CompareTo(Cell other)
        {
            if (Entropy < other.Entropy) return -1;
            if (Entropy > other.Entropy) return 1;
            return 0;
        }

        public bool Collapse()
        {
            var snapshot = CurrentSnapshot;

            var possibilities = Possibilities.ToList();
            if (!possibilities.Any())
                return false;

            while (possibilities.Any())
            {
                var choice = possibilities
                    .Select(x => (Weight: x.weight, x))
                    .GetRandomByWeight(_random);

                SaveTried(snapshot, choice);

                CollapsedType = choice;

                bool success = false;

                for (AmountOfTurns = 0; AmountOfTurns < choice.MaxTurns; AmountOfTurns++)
                {
                    if (Neighbors.All(IsCompatible))
                    {
                        success = true;
                        break;
                    }
                }

                if (success)
                    return true;

                Uncollapse();
                possibilities.Remove(choice);
            }

            return false;
        }

        private bool IsCompatible(KeyValuePair<Direction, Cell> kvp)
        {
            var (direction, neighbor) = kvp;
            return CollapsedType.IsCompatible(direction, AmountOfTurns, neighbor);
        }

        private void SaveTried(NeighborSnapshot snapshot, CellType type)
        {
            if (!_testedOptions.TryGetValue(snapshot, out var set))
            {
                set = new HashSet<CellType>();
                _testedOptions[snapshot] = set;
            }

            set.Add(type);
        }

        public void Uncollapse()
        {
            CollapsedType = null;
            _isDirty = true;
        }

        private void SetDirty(Cell cell)
        {
            _isDirty = true;
        }

        private NeighborSnapshot CurrentSnapshot => new NeighborSnapshot(Neighbors);
    }
}
