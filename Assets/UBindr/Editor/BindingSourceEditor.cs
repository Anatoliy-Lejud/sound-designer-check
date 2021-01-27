using System;
using System.Text.RegularExpressions;
using Assets.Editor.UBindr.BindingEditors;
using Assets.UBindr;
using Assets.UBindr.Bindings;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor.UBindr
{
    [CustomEditor(typeof(BindingSource))]
    public class BindingSourceEditor : UnityEditor.Editor
    {
        protected BindingSource BindingSource { get { return (BindingSource)target; } }

        protected void ChangeTracked<T>(Func<T> uiMethod, Action<T> onChange, string changeDescriptor = "")
        {
            EditorGUI.BeginChangeCheck();
            var result = uiMethod();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Changed " + changeDescriptor);
                onChange(result);
            }
        }

        public override void OnInspectorGUI()
        {
            //DrawDefaultInspector();

            EditorHelper.Header("Settings");
            ChangeTracked(() => EditorGUILayout.TextField("Source Name", BindingSource.sourceName), v => BindingSource.sourceName = v, "Source Name");
            if (string.IsNullOrEmpty(BindingSource.sourceName))
            {
                BindingSource.SetWarning("No context name specified");
            }
            else if (!Regex.IsMatch(BindingSource.sourceName, "^[a-zA-Z_][a-zA-Z_0-9]*$", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Multiline))
            {
                BindingSource.SetWarning("Binding name is invalid");
            }

            //ChangeTracked(() => EditorGUILayout.TextField("Source Name", BindingSource.sourceName), v => BindingSource.sourceName = v, "Source Name");
            //ChangeTracked(() => EditorGUILayout.TextField("Source Name", BindingSource.sourceName), v => BindingSource.sourceName = v, "Source Name");

            ChangeTracked(() => EditorGUILayout.Toggle("Global", BindingSource.isGlobal), v => BindingSource.isGlobal = v);
            ChangeTracked(() => EditorGUILayout.Toggle("Static", BindingSource.isStatic), v => BindingSource.isStatic = v);

            if (BindingSource.isStatic)
            {
                DrawStaticGui();
            }
            else
            {
                DrawNonStaticGui();
            }

            DrawState();
        }

        private void DrawState()
        {
            EditorHelper.Header("State");
            if (BindingSource.HasWarning)
            {
                EditorHelper.WarningTextArea(BindingSource.Warning);
            }
            else
            {
                EditorGUILayout.LabelField("Ok");
            }
            BindingSource.Warning = null;
        }

        private void DrawStaticGui()
        {
            EditorHelper.Header("Static Class Binding");
            ChangeTracked(() => EditorGUILayout.TextField("Class Name", BindingSource.staticClassName), v => BindingSource.staticClassName = v);
            if (string.IsNullOrEmpty(BindingSource.staticClassName))
            {
                BindingSource.SetWarning("No static class name specified");
            }

            EditorHelper.NormalLabel("The static class name must be qualified, here are some examples:");

            if (GUILayout.Button("Mathf"))
            {
                Undo.RecordObject(target, "Changed");
                BindingSource.sourceName = "Mathf";
                BindingSource.staticClassName = typeof(Mathf).FullName;
            }
            if (GUILayout.Button("Time"))
            {
                Undo.RecordObject(target, "Changed");
                BindingSource.sourceName = "Time";
                BindingSource.staticClassName = typeof(Time).FullName;
            }
            if (GUILayout.Button("UTools"))
            {
                Undo.RecordObject(target, "Changed");
                BindingSource.sourceName = "UTools";
                BindingSource.staticClassName = typeof(UTools).FullName;
            }

            try
            {
                var type = BindingSource.GetPublishedType();
                if (type == null)
                {
                    BindingSource.SetWarning(string.Format("Unable to find type {0}", BindingSource.staticClassName));
                }
            }
            catch (Exception e)
            {
                BindingSource.SetWarning(e.ToString());
            }
        }

        private void DrawNonStaticGui()
        {
            EditorHelper.Header("Non-static Class Binding");
            ChangeTracked(() => (Component)EditorGUILayout.ObjectField("Source Override", BindingSource.sourceOverride, typeof(Component), true), v => BindingSource.sourceOverride = v);

            BindingEditorHelper.ShowSelectGameObjectComponents(BindingSource.sourceOverride, c =>
            {
                Undo.RecordObject(target, "Changed");
                BindingSource.sourceOverride = c;
            });

            try
            {
                var data = BindingSource.GetData();
                if (data == null)
                {
                    BindingSource.SetWarning("No data found, assing Data or place BindingSource after wanted that component you wish to make available.");
                    EditorHelper.WarningLabel("(returns null, see below)");
                }
                else if (BindingSource.sourceOverride == null)
                {
                    EditorHelper.OkLabel(string.Format("Using {0} as Source", data.GetType().FullName));
                }
            }
            catch (Exception e)
            {
                BindingSource.SetWarning(e.ToString());
            }
        }
    }
}