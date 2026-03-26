using System.Collections.Generic;
using UnityEngine;
using WaveFunction;

[CreateAssetMenu(menuName = "Wave Function/Database")]
public class WFCDatabase : ScriptableObject
{
    public List<CellType> cellTypes = new();
    public List<Socket> sockets = new();
}