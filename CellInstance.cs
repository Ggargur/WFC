using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace WaveFunction
{
    public abstract class CellInstance
    {
    }

    public abstract class InstantiableCell<T> : CellInstance
    {
        public abstract T Instantiable { get; }
    }
    
    [System.Serializable]
    public class SinglePrefabCell : InstantiableCell<GameObject>
    {
        public override GameObject Instantiable => prefab;
        [SerializeField] private GameObject prefab; 
    }


    [System.Serializable]
    public class PrefabPoolCell : InstantiableCell<GameObject>
    {
        [System.Serializable]
        public class PrefabPair
        {
            public GameObject prefab;
            public float weight;
        }

        public override GameObject Instantiable => prefabs.Select(p => (p.weight, p.prefab)).GetRandomByWeight();
        public List<PrefabPair> prefabs;
    }
    
    [System.Serializable]
    public class SingleTileCell : InstantiableCell<TileBase>
    {
        public override TileBase Instantiable => tile;
        [SerializeField] private TileBase tile;

        public SingleTileCell(TileBase instantiable)
        {
            tile = instantiable;
        }
    }
    
    [System.Serializable]
    public class TilePoolCell : InstantiableCell<TileBase>
    {
        [System.Serializable]
        public class TilePair
        {
            public TileBase tile;
            public float weight;
        }

        public override TileBase Instantiable => prefabs.Select(p => (p.weight, p.tile)).GetRandomByWeight();
        public List<TilePair> prefabs;
    }
}