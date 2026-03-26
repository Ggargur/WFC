using System.Collections.Generic;
using UnityEngine;

namespace WaveFunction
{
    public abstract class Instantiator : MonoBehaviour
    {
        public abstract void InstantiateCells(IEnumerable<Cell> cells);
        
        public abstract void ClearCells();
    }
}