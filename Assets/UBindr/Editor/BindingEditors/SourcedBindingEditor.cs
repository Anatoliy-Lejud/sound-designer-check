using System;
using Assets.UBindr.Bindings;
using Assets.UBindr.Expressions;
using UnityEditor;

namespace Assets.Editor.UBindr.BindingEditors
{
    public class SourcedBindingEditor<TBinding> : BindingWithBindingSourcesEditor<TBinding> where TBinding : SourcedBinding
    {
        protected object ReturnedSource { get; set; }
        public TopDownParser.Expression ReturnedSourceExpression { get; set; }

        protected override void DrawSource()
        {
            base.DrawSource();
            EditorGUILayout.LabelField("Source Expression");
            ChangeTracked(() => EditorGUILayout.TextArea(Binding.SourceExpression), v => Binding.SourceExpression = v);
            ReturnedSource = null;
            CheckSourceExpression();
        }

        protected override void DrawBindingHelper()
        {
            BindingHelperDrawer = BindingHelperDrawer ?? new BindingHelperDrawer(Binding, ShowProperties, ShowFunctions, ShowMethods, s => Binding.SourceExpression = s);
            BindingHelperDrawer.Draw();
        }

        protected virtual Type ExpectedSourceType()
        {
            return null;
        }

        protected virtual void CheckSourceExpression()
        {
            try
            {
                if (!string.IsNullOrEmpty(Binding.GetSourceExpression()))
                {
                    ReturnedSourceExpression = Binding.Scope.BuildOrGetExpression(Binding.GetSourceExpression());
                    ReturnedSource = ReturnedSourceExpression.Evaluate();

                    if (ReturnedSource == null)
                    {
                        EditorHelper.NormalLabel("(returns null)");
                    }
                    else
                    {
                        EditorHelper.NormalLabel(string.Format("(returns {0}: {1})", MemberDescriber.TypeToString(ReturnedSource.GetType()), ReturnedSource.ToString().Ellipsis(100)));
                        WarnIfWrongReturnedSource();
                    }
                }
                else
                {
                    EditorHelper.WarningLabel("(no source expression)");
                    Binding.SetBindingStateWarning("No source expression specified");
                }
            }
            catch (Exception e)
            {
                EditorHelper.WarningLabel("(fails)");
                Binding.SetBindingStateWarning("Source Expression Error:" + e.Message);
            }
        }

        protected void WarnIfWrongReturnedSource()
        {
            if (ExpectedSourceType() != null)
            {
                if (!ReturnedSource.GetType().IsAssignableFrom(ExpectedSourceType()))
                {
                    EditorHelper.WarningLabel(string.Format("Expected return type {0} but received {1}", ExpectedSourceType(), ReturnedSource.GetType()));
                }
            }
        }
    }
}