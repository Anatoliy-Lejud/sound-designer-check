using System.Collections;
using Assets.UBindr.Bindings;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor.UBindr.BindingEditors
{
    [CustomEditor(typeof(RepeatBinding))]
    public class RepeatBindingEditor : SourcedBindingEditor<RepeatBinding>
    {
        protected override void DrawBindingHelper()
        {
            BindingHelperDrawer = BindingHelperDrawer ?? new BindingHelperDrawer(Binding, ShowProperties, ShowFunctions, ShowMethods, s => Binding.SourceExpression = s, null, new BindingHelperDrawer.SetMethod("Order By", s => Binding.orderBy = s));
            BindingHelperDrawer.Draw();
        }

        protected override void DrawSource()
        {
            base.DrawSource();
            ChangeTracked(() => EditorGUILayout.TextField("Order By", Binding.orderBy), v => Binding.orderBy = v);
            ChangeTracked(() => EditorGUILayout.Toggle("Descending", Binding.descending), v => Binding.descending = v);
            ChangeTracked(() => EditorGUILayout.IntField("Max Items", Binding.maxItems), v => Binding.maxItems = v);

            if (ReturnedSource != null)
            {
                if (ReturnedSource is string)
                {
                    Binding.SetBindingStateWarning("Expects source to implement IEnumerable (but it shouldn't be a string)!");
                }
                else if (!(ReturnedSource is IEnumerable))
                {
                    Binding.SetBindingStateWarning("Expects source to implement IEnumerable!");
                }
            }
        }

        protected override void DrawDestination()
        {
            base.DrawDestination();
            EditorHelper.Header("Destination");
            ChangeTracked(() => EditorGUILayout.TextField("Row Source Name", Binding.rowSourceName), v => Binding.rowSourceName = v);
            ChangeTracked(() => (UnityEngine.GameObject)EditorGUILayout.ObjectField("Item Prefab", Binding.ItemPrefab, typeof(UnityEngine.GameObject), true), v =>
            {
                Debug.Log($"Changed from {Binding.ItemPrefab} to {v}");
                Binding.ItemPrefab = v;
            });
            if (Binding.ItemPrefab == null)
            {
                Binding.SetBindingStateWarning("No prefab provided!");
            }
        }
    }
}