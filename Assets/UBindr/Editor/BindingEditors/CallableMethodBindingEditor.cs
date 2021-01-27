using System;
using System.Runtime.Remoting.Messaging;
using Assets.UBindr.Bindings;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Editor.UBindr.BindingEditors
{
    [CustomEditor(typeof(CallableMethodBinding))]
    public class CallableMethodBindingEditor : BindingWithBindingSourcesEditor<CallableMethodBinding>
    {
        protected override bool ShowMethods => true;
        protected override bool ShowFunctions => true;
        protected override bool ShowProperties => false;

        protected override void DrawSource()
        {
            // Nope
        }

        protected override void DrawDestination()
        {
            base.DrawDestination();
            DrawMethod();
        }

        private void DrawMethod()
        {
            EditorHelper.Header("Method");

            ValidateEvents();
            AddConnectionButton();
            ChangeTracked(() => EditorGUILayout.TextField("Method Path", Binding.MethodPath), v => Binding.MethodPath = v);
          
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

        private void AddConnectionButton()
        {
            // Doesn't actually work :/
            //if (GUILayout.Button("Connect Event"))
            //{
            //    var destination = BindingHelper.GetDestination(Binding);
            //    if (destination is Button button)
            //    {
            //        EditorGUI.BeginChangeCheck();
            //        button.onClick.AddListener(() => Binding.ExecuteMethod());
            //        if (EditorGUI.EndChangeCheck())
            //        {
            //            Undo.RecordObject(button, "Added method");
            //         }
            //        else
            //        {
            //            Debug.Log("Failed to add meth");
            //        }
            //    }
            //}

        }

        private void ValidateEvents()
        {
            var destination = BindingHelper.GetDestination(Binding);
            if (destination is Button button)
            {
                var count = button.onClick.GetPersistentEventCount();
                if (count == 0)
                {
                    EditorHelper.WarningLabel("Add an OnClick event to your Button!");
                }
                else if (button.onClick.GetPersistentMethodName(0) != "ExecuteMethod")
                {
                    EditorHelper.WarningLabel("Set your OnClick event to CallableMethodBinding.ExecuteMethod()!");
                }

                return;
            }

            var eventTrigger = destination as EventTrigger;
            if (eventTrigger != null)
            {
                bool found = false;
                foreach (EventTrigger.Entry entry in eventTrigger.triggers)
                {
                    //var @event = new EventTrigger.TriggerEvent();
                    //entry.callback = @event;
                    //entry.callback.AddListener(x => Evaluate(MethodPath));
                    if (entry.callback.GetPersistentEventCount() > 0 && entry.callback.GetPersistentMethodName(0) == "ExecuteMethod")
                    {
                        found = true;
                    }
                }

                if (!found)
                {
                    EditorHelper.WarningLabel("Add an Event to your EventTrigger and connect it to the CallableMethodBinding.ExecuteMethod()!");
                }

                return;
            }

            EditorHelper.WarningLabel($"Unrecognized destination {destination?.GetType().Name}: add an event that calls CallableMethodBinding.ExecuteMethod()!");
        }

        protected override void DrawBindingHelper()
        {
            BindingHelperDrawer = BindingHelperDrawer ?? new BindingHelperDrawer(Binding, ShowProperties, ShowFunctions, ShowMethods, s => Binding.MethodPath = s);
            BindingHelperDrawer.Draw();
        }
    }
}