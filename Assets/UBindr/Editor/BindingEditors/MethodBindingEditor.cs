using System;
using Assets.UBindr.Bindings;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor.UBindr.BindingEditors
{
    [CustomEditor(typeof(MethodBinding))]
    public class MethodBindingEditor : BindingWithBindingSourcesEditor<MethodBinding>
    {
        protected override bool ShowMethods => true;
        protected override bool ShowFunctions => true;
        protected override bool ShowProperties => false;

        public override void DrawHeader()
        {
            EditorHelper.WarningLabel("MethodBinding is deprecated, use CallableMethodBinding instead!");
        }

        protected override void DrawDestination()
        {
            base.DrawDestination();
            EditorHelper.Header("Method");
            ChangeTracked(() => EditorGUILayout.TextField("Method Path", Binding.MethodPath), v => Binding.MethodPath = v);
            ChangeTracked(() => (Component)EditorGUILayout.ObjectField("Method Destination Override", Binding.Destination, typeof(Component), true), v => Binding.Destination = v);

            try
            {
                if (!string.IsNullOrEmpty(Binding.MethodPath))
                {
                    var expression = Binding.Scope.BuildOrGetExpression(Binding.MethodPath);
                    try
                    {
                        Binding.Scope.DisableExecute = true;
                        expression.Evaluate();
                    }
                    finally
                    {
                        Binding.Scope.DisableExecute = true;
                    }
                }
                else
                {
                    EditorHelper.WarningLabel("(no method path)");
                    Binding.SetBindingStateWarning("No Method Path Specified");
                }
            }
            catch (Exception e)
            {
                EditorHelper.WarningLabel("(fails)");
                Binding.SetBindingStateWarning("Destination Path Error: " + e.Message);
            }
        }

        protected override void DrawBindingHelper()
        {
            BindingHelperDrawer = BindingHelperDrawer ?? new BindingHelperDrawer(Binding, ShowProperties, ShowFunctions, ShowMethods, s => Binding.MethodPath = s);
            BindingHelperDrawer.Draw();
        }
    }
}