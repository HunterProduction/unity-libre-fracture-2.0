using UnityEditor;
using UnityEngine;

namespace LibreFracture.Editor
{
    public static class CustomStyles
    {
        public static GUIStyle TitleStyle => new GUIStyle(EditorStyles.largeLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 15,
            fontStyle = FontStyle.Bold,
        };

        public static GUIStyle HeaderStyleCentered => new GUIStyle(EditorStyles.largeLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };

        public static GUIStyle HeaderStyleLeft => new GUIStyle(EditorStyles.whiteLargeLabel)
        {
            alignment = TextAnchor.MiddleLeft,
        };
    }
}