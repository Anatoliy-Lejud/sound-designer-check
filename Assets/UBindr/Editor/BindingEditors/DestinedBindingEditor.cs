using System;
using Assets.UBindr.Bindings;
using Assets.UBindr.Expressions;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor.UBindr.BindingEditors
{
    public class DestinedBindingEditor<TBinding> : SourcedBindingEditor<TBinding> where TBinding : DestinedBinding
    {
        protected object ReturnedDestination { get; set; }
        protected virtual bool CanBeTwoWay => true;
        protected virtual bool CanEditTwoWay => true;
        protected virtual bool DestinationPathDisabled => false;

        protected override void DrawBinding()
        {
            base.DrawBinding();
            EditorGUI.BeginDisabledGroup(!CanBeTwoWay || !CanEditTwoWay);
            ChangeTracked(() => EditorGUILayout.ToggleLeft("Two Way", Binding.TwoWay), v => Binding.TwoWay = v);
            EditorGUI.EndDisabledGroup();
            if (!CanBeTwoWay)
            {
                Binding.TwoWay = false;
            }

            if (Binding.TwoWay)
            {
                ChangeTracked(() => EditorGUILayout.ToggleLeft("Initialize with Destination Value", Binding.CopyInitialValueFromDestination), v => Binding.CopyInitialValueFromDestination = v);
            }
        }

        protected override void DrawBindingHelper()
        {
            BindingHelperDrawer = BindingHelperDrawer ?? new BindingHelperDrawer(Binding, ShowProperties, ShowFunctions, ShowMethods, s => Binding.SourceExpression = s, s => Binding.DestinationPath = s);
            BindingHelperDrawer.Draw();
        }

        protected override void DrawDestination()
        {
            base.DrawDestination();
            EditorHelper.Header("Destination");
            EditorGUILayout.LabelField("Destination Path");
            EditorGUI.BeginDisabledGroup(DestinationPathDisabled);
            ChangeTracked(() => EditorGUILayout.TextArea(Binding.DestinationPath), v => Binding.DestinationPath = v);
            EditorGUI.EndDisabledGroup();

            ChangeTracked(() => EditorGUILayout.ToggleLeft("Disable Execute During Design", Binding.DisableExecuteDuringDesign), v => Binding.DisableExecuteDuringDesign = v);

            ReturnedDestination = null;
            try
            {
                if (!string.IsNullOrEmpty(Binding.DestinationPath))
                {
                    if (Binding.IsValidPath(Binding.DestinationPath))
                    {
                        var expression = Binding.Scope.BuildOrGetExpression(Binding.DestinationPath);
                        Binding.Scope.DisableExecute = Binding.DisableExecuteDuringDesign;

                        try
                        {
                            ReturnedDestination = expression.Evaluate();
                            if (ReturnedDestination == null)
                            {
                                EditorHelper.NormalLabel("(returns null)");
                            }
                            else
                            {
                                EditorHelper.NormalLabel(string.Format("(returns {0}: {1})", MemberDescriber.TypeToString(ReturnedDestination.GetType()), ReturnedDestination.ToString().Ellipsis(100)));
                            }
                        }
                        finally
                        {
                            Binding.Scope.DisableExecute = false;
                        }
                    }
                    else
                    {
                        EditorHelper.WarningLabel("(destination path seems to be an expression)");
                    }
                }

                else
                {
                    EditorHelper.WarningLabel("(no destination path)");
                    Binding.SetBindingStateWarning("No Destination Path Specified");
                }
            }
            catch (Exception e)
            {
                EditorHelper.WarningLabel("(fails)");
                Binding.SetBindingStateWarning("Destination Path Error: " + e.Message);
            }

            ChangeTracked(() => (Component)EditorGUILayout.ObjectField("Destination Override", Binding.Destination, typeof(Component), true), v => Binding.Destination = v);
            BindingEditorHelper.ShowSelectGameObjectComponents(
                Binding.Destination, c =>
                {
                    Undo.RecordObject(target, "Changed");
                    Binding.Destination = c;
                    BindingHelperDrawer = null;
                });

            if (Binding.SourceExpression == Binding.DestinationPath)
            {
                Binding.SetBindingStateWarning("The Source Expression is the same as the Destination Path");
            }
        }
    }
}