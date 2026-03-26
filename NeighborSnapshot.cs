using System;
using System.Collections.Generic;
using System.Linq;

namespace WaveFunction
{
    public class NeighborSnapshot : IEquatable<NeighborSnapshot>
    {
        private readonly Dictionary<Direction, CellType> _state;

        public NeighborSnapshot(Dictionary<Direction, Cell> neighbors)
        {
            _state = neighbors.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.CollapsedType
            );
        }

        public bool Equals(NeighborSnapshot other)
        {
            if (other == null) return false;
            if (_state.Count != other._state.Count) return false;

            foreach (var kvp in _state)
            {
                if (!other._state.TryGetValue(kvp.Key, out var val)) return false;
                if (kvp.Value != val) return false;
            }

            return true;
        }

        public override bool Equals(object obj) => Equals(obj as NeighborSnapshot);

        public override int GetHashCode()
        {
            int hash = 17;
            foreach (var kvp in _state)
            {
                hash = hash * 31 + kvp.Key.GetHashCode();
                hash = hash * 31 + (kvp.Value != null ? kvp.Value.GetHashCode() : 0);
            }

            return hash;
        }
    }
}