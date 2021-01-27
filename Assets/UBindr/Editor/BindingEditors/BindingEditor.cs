using Assets.UBindr.Bindings;
using UnityEditor;

namespace Assets.Editor.UBindr.BindingEditors.Editor
{
    [CustomEditor(typeof(Binding))]
    public class BindingEditor : DestinedBindingEditor<Binding>
    {       
    }
}