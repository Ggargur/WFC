using System.Collections.Generic;
using Plugins.SerializedCollections.Runtime.Scripts;
using UnityEngine;
using UnityEngine.Serialization;

namespace WaveFunction
{
    [CreateAssetMenu(fileName = "Cell Type", menuName = "Wave Function/Cell Type")]
    public class CellType : ScriptableObject
    {
        public float weight;
        [SerializeReference, SubclassSelector] public CellInstance CellInstance;

        public SerializedDictionary<Direction, Socket> sockets = new();

        [FormerlySerializedAs("verticallyMirrorable")] [FormerlySerializedAs("VerticallyMirrorable")]
        public bool turnable;
        public bool symmetric;

        public int MaxTurns => _maxTurns ??= turnable ? MapCreator.MaxTurnAmount : 1;
        private int? _maxTurns;

        public static Dictionary<Direction, Socket> GetFullyTurnedCompatibilities(Cell cell)
        {
            var cellType = cell?.CollapsedType;
            return GetFullyTurnedCompatibilities(cell.AmountOfTurns, cellType);
        }
        
        private static Dictionary<Direction, Socket> GetFullyTurnedCompatibilities(int rotations, CellType cellType)
        {
            if(!cellType) return null;

            Dictionary<Direction, Socket> compatMap = cellType.sockets;
            for (var i = 0; i < rotations; i++)
            {
                compatMap = GetClockWiseMap(compatMap);
            }
            return compatMap;
        }

        private static Dictionary<Direction, Socket> GetClockWiseMap(
            Dictionary<Direction, Socket> oldDictionary)
        {
            var newCompatibleCells = new Dictionary<Direction, Socket>();

            foreach (var (direction, socket) in oldDictionary)
            {
                newCompatibleCells[direction.ClockWiseTurn()] = socket;
            }

            return newCompatibleCells;
        }

        public bool IsCompatible(Direction direction, int rotations, Cell candidate)
        {
            if (candidate == null)
                return true;

            var other = candidate.CollapsedType;
            if (!other) return true;

            var otherRotations = candidate.AmountOfTurns;
            if (other == this && symmetric &&
                ((otherRotations == 2 && rotations == 0) ||
                 (otherRotations == 0 && rotations == 2))) // They're both symmetric
                return true;

            var thisCompatMap = GetFullyTurnedCompatibilities(rotations, this);
            var otherCompatMap = GetFullyTurnedCompatibilities(candidate);
            
            var opposite = direction.Opposite();
            var thisSocket = thisCompatMap[direction];
            var otherSocket = otherCompatMap[opposite];

            return thisSocket.compatibleSockets[direction].Contains(otherSocket) &&
                   otherSocket.compatibleSockets[opposite].Contains(thisSocket); 
        }
    }
}