using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor.UBindr.BindingEditors
{
    public static class BindingEditorHelper
    {
        public static void ShowSelectGameObjectComponents(Component sibling, Action<Component> func)
        {
            if (sibling == null)
            {
                return;
            }

            var components = new List<Component>();
            sibling.GetComponents(components);

            //foreach (var component in components)
            //{
            //    if (GUILayout.Button(string.Format("Select {0}", component.GetType().FullName)))
            //    {
            //        func(component);
            //    }
            //}
            var names = components.Select(x => x.GetType().FullName).ToList();
            names.Insert(0, "[None]");
            components.Insert(0, null);
            var selected = components.IndexOf(sibling);
            int newSelected = EditorGUILayout.Popup("Sibling", selected, names.ToArray());
            if (newSelected != selected)
            {
                func(components[newSelected]);
            }
        }
    }
}