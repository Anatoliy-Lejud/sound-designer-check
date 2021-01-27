using System;
using System.Collections.Generic;
using Assets.UBindr.Expressions;
using UnityEngine;

namespace Assets.UBindr
{
    [ExecuteInEditMode]
    public class BindingSource : MonoBehaviour, IBindingSource
    {
        public string sourceName;
        public string SourceName { get { return sourceName; } }
        public Component sourceOverride;
        public bool isGlobal;
        public bool isStatic;
        public string staticClassName;

        public string Warning { get; set; }
        public bool HasWarning { get { return !string.IsNullOrEmpty(Warning); } }
        public void SetWarning(string newWarning) { Warning = Warning ?? newWarning; }

        [ExecuteInEditMode]
        public void Start()
        {
            if (sourceName != null)
            {
                return;
            }
            var data = GetData();
            if (data != null)
            {
                sourceName = data.GetType().Name;
            }
        }

        // Either use the provided Data, or find the component just before this component
        public Component GetData()
        {
            if (sourceOverride != null)
            {
                return sourceOverride;
            }
            var components = new List<Component>();
            gameObject.GetComponents(components);
            var index = components.IndexOf(this);
            if (index > 0)
            {
                return components[index - 1];
            }
            else
            {
                return null;
            }
        }

        public void Register(TopDownParser.Scope scope, bool global, object source)
        {
            if (isGlobal != global)
            {
                return;
            }

            if (isStatic)
            {
                scope.AddStaticRoot(SourceName, GetPublishedType());
            }
            else
            {
                if (sourceOverride == null)
                {
                    sourceOverride = GetData();
                }
                scope.AddObjectRoot(SourceName, () => sourceOverride);
            }
        }

        public Type GetPublishedType()
        {
            var type = Type.GetType(staticClassName);
            if (type != null) return type;
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = a.GetType(staticClassName);
                if (type != null)
                {
                    return type;
                }
            }
            return null;
        }
    }
}