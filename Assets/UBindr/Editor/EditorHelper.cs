using UnityEditor;
using UnityEngine;

namespace Assets.Editor.UBindr
{
    public static class EditorHelper
    {
        public static void WarningLabel(string text)
        {
            GUIStyle textFieldStyles = new GUIStyle(EditorStyles.largeLabel);
            textFieldStyles.normal.textColor = Color.red;
            GUILayout.Label(text, textFieldStyles);
        }

        public static void WarningTextArea(string text)
        {
            GUIStyle textFieldStyles = new GUIStyle(EditorStyles.textArea);
            textFieldStyles.normal.textColor = Color.red;
            GUILayout.TextArea(text, textFieldStyles);
        }

        public static void OkLabel(string text)
        {
            GUILayout.Label(text, EditorStyles.label);
        }

        public static void LargeLabel(string text)
        {
            GUIStyle textFieldStyles = new GUIStyle(EditorStyles.largeLabel);
            GUILayout.Label(text, textFieldStyles);
        }

        public static void Header(string label)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        }

        public static void NormalLabel(string label)
        {
            EditorGUILayout.LabelField(label);
        }
    }
}