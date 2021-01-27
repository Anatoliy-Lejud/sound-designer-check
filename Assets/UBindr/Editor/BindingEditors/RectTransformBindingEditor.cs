using Assets.UBindr.Bindings;
using UnityEditor;

namespace Assets.Editor.UBindr.BindingEditors
{
    [CustomEditor(typeof(RectTransformBinding))]
    public class RectTransformBindingEditor : DestinedBindingEditor<RectTransformBinding>
    {
        protected override bool ShowMethods => false;
        protected override bool ShowFunctions => false;
        protected override bool CanBeTwoWay => false;
    }
}