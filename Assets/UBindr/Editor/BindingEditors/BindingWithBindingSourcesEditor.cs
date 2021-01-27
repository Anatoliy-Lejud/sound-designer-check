using System;
using Assets.UBindr.Bindings;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor.UBindr.BindingEditors
{
    public class BindingWithBindingSourcesEditor<TBinding> : UnityEditor.Editor where TBinding : BindingWithBindingSources
    {
        protected BindingHelperDrawer BindingHelperDrawer { get; set; }
        protected TBinding Binding => (TBinding)target;

        protected virtual bool ShowMethods => true;
        protected virtual bool ShowFunctions => true;
        protected virtual bool ShowProperties => true;

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

        public virtual void DrawHeader()
        {
        }

        public override void OnInspectorGUI()
        {
            //DrawDefaultInspector();
            if (!Application.isPlaying)
            {
                Binding.UpdateBindingSources(true);
                AfterUpdateBindingSources();
            }

            DrawHeader();
            DrawBinding();
            DrawSource();
            DrawDestination();
            DrawState();
            DrawBindingHelper();
        }

        protected virtual void AfterUpdateBindingSources()
        {
        }

        protected virtual void DrawBinding()
        {
            EditorHelper.Header("Binding");
            ChangeTracked(() => EditorGUILayout.ToggleLeft("Recompile", Binding.recompileOnEachUpdate), v => Binding.recompileOnEachUpdate = v);
        }

        protected virtual void DrawSource()
        {
            EditorHelper.Header("Source");
            ChangeTracked(() => (Component)EditorGUILayout.ObjectField("Source Override", Binding.sourceOverride, typeof(Component), true), v => Binding.sourceOverride = v);
            BindingEditorHelper.ShowSelectGameObjectComponents(Binding.sourceOverride, c =>
            {
                Undo.RecordObject(target, "Changed");
                Binding.sourceOverride = c;
                BindingHelperDrawer = null;
            });
        }

        protected virtual void DrawDestination()
        {
        }

        protected virtual void DrawState()
        {
            EditorHelper.Header("Binding State");
            if (Binding.HasBindingStateWarning)
            {
                EditorHelper.WarningTextArea(Binding.bindingStateWarning);
            }
            else
            {
                EditorGUILayout.LabelField("Ok");
            }
            if (!Application.isPlaying)
            {
                Binding.bindingStateWarning = null;
            }
        }

        protected virtual void DrawBindingHelper()
        {
            BindingHelperDrawer = BindingHelperDrawer ?? new BindingHelperDrawer(Binding, ShowProperties, ShowFunctions, ShowMethods);
            BindingHelperDrawer.Draw();
        }
    }
}