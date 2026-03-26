using System.Collections.Generic;
using UnityEngine;

namespace WaveFunction
{
    public class PrefabInstantiator : Instantiator
    {
        public override void InstantiateCells(IEnumerable<Cell> cells)
        {
            foreach (var cell in cells)
            {
                InstantiatePrefab(cell);
            }
        }
        
        private void InstantiatePrefab(Cell cell)
        {
            var turns = cell.AmountOfTurns;
            var collapsedType = cell.CollapsedType;
            var position = cell.Position;
            var origin = transform;
            var index = cell.Index;
            var cellInstance = collapsedType.CellInstance;
            if (cellInstance is not InstantiableCell<GameObject> prefabCell) return;
            
            var prefab = prefabCell.Instantiable;
            
            var rotation = Quaternion.Euler(Vector3.up * (turns * 90f));
            var go = Instantiate(prefab, position, rotation * prefab.transform.rotation, origin);
            go.name = $"Cell ({index.x}, {index.y}, {index.z}): {collapsedType.name}";
            if (!go.TryGetComponent<CellBehaviour>(out var cellBehaviour))
            {
                cellBehaviour = go.AddComponent<CellBehaviour>();
            }

            cellBehaviour.Cell = cell;
        }
    }
}