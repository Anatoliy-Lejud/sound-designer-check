using System;
using Assets.UBindr.Bindings;
using UnityEditor;

namespace Assets.Editor.UBindr.BindingEditors.Editor
{
    [CustomEditor(typeof(UIInteractableBinding))]
    public class UIInteractableBindingEditor : DestinedBindingEditor<UIInteractableBinding>
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

            switch (Binding.uIType.Name)
            {
                case "Button":
                    Binding.DestinationPath = "dest.interactable";
                    break;
                case "Toggle":
                    Binding.DestinationPath = "dest.interactable";
                    break;
                case "Dropdown":
                case "TMP_Dropdown":
                    Binding.DestinationPath = "dest.interactable";
                    break;
                case "InputField":
                case "TMP_InputField":
                    Binding.DestinationPath = "dest.interactable";
                    break;
                case "Slider":
                    Binding.DestinationPath = "dest.interactable";
                    break;
                default:
                    Binding.SetBindingStateWarning(string.Format("Unhandled Destination Type: {0}", Binding.uIType.Name));
                    break;
            }
        }       
    }
}