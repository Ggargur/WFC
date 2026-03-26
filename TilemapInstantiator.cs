using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace WaveFunction
{
    public class TilemapInstantiator : Instantiator
    {
        [SerializeField] private Tilemap tilemap;
        [SerializeField] private Vector3Int offset;
        
        public override void InstantiateCells(IEnumerable<Cell> cells)
        {
            foreach (var cell in cells)
            {
                InstantiateTile(cell);
            }
        }

        private void InstantiateTile(Cell cell)
        {
            var collapsedType = cell.CollapsedType;
            var index = cell.Index;
            var cellInstance = collapsedType.CellInstance;
            if (cellInstance is not InstantiableCell<TileBase> tileCell) return;
            
            var tile = tileCell.Instantiable;
            
            tilemap.SetTile(index + offset, tile);
        }
    }
}