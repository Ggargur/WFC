using System.Collections.Generic;
using Plugins.SerializedCollections.Runtime.Scripts;
using UnityEngine;

namespace WaveFunction
{
    [CreateAssetMenu(fileName = "Socket", menuName = "Wave Function/Socket")]
    public class Socket : ScriptableObject
    {
        public SerializedDictionary<Direction, List<Socket>> compatibleSockets = new();
    }
}
