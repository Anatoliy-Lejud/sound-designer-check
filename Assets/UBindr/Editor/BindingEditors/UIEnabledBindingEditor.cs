using System;
using Assets.UBindr.Bindings;
using UnityEditor;

namespace Assets.Editor.UBindr.BindingEditors
{
    [CustomEditor(typeof(UIEnabledBinding))]
    public class UIEnabledBindingEditor : DestinedBindingEditor<UIEnabledBinding>
    {
        protected override bool DestinationPathDisabled { get { return true; } }

        protected override void AfterUpdateBindingSources()
        {
            base.AfterUpdateBindingSources();
            UpdateUIBindings();
        }

        protected override Type ExpectedSourceType()
        {
            return typeof(bool);
        }

        protected override void DrawBinding()
        {
            base.DrawBinding();
            ChangeTracked(() => EditorGUILayout.ToggleLeft("Control Game Object", Binding.controlGameObject), v => Binding.controlGameObject = v);
        }

        protected override bool ShowMethods { get { return false; } }

        protected override void DrawBindingHelper()
        {
            BindingHelperDrawer = BindingHelperDrawer ?? new BindingHelperDrawer(Binding, ShowProperties, ShowFunctions, ShowMethods, s => Binding.SourceExpression = s);
            BindingHelperDrawer.Draw();
        }

        public void UpdateUIBindings()
        {
            if (Binding.uIType == null)
            {
                return;
            }

            // TODO: Should call GameObject.SetActive(xxx)            
            Binding.DestinationPath = Binding.controlGameObject ? "dest.gameObject.active" : "dest.enabled";

            if (Binding.ActualDestination.gameObject == Binding.gameObject && Binding.controlGameObject)
            {
                Binding.SetBindingStateWarning("UI Enabled Binding should not control the game object it lives on, it should be placed on a parent. Once it's disabed, it will not be enabled again!");
            }
        }
    }
}