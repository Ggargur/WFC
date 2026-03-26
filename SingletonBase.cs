using System;
using UnityEngine;

namespace WaveFunction
{
    public abstract class SingletonBase<T> : MonoBehaviour where T : SingletonBase<T>
    {
        private static readonly Lazy<T> Lazy = new(FindAnyObjectByType<T>);

        public static T Instance => Lazy.Value;
    }
}