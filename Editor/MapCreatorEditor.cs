using UnityEditor;
using UnityEngine;

namespace WaveFunction.Editor
{
    [CustomEditor(typeof(MapCreator))]
    public class MapCreatorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (target is not MapCreator t) return;
            DrawDefaultInspector();
            if (GUILayout.Button("Create"))
            {
                // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
                t.Create();
            }

            if (GUILayout.Button("Clear"))
            {
                t.Clear();
            }
        }
    }
}