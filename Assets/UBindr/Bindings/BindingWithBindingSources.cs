using System;
using System.Linq;
using Assets.UBindr.Expressions;
using UnityEngine;

namespace Assets.UBindr.Bindings
{
    public abstract class BindingWithBindingSources : MonoBehaviour, IBinding
    {
        private bool _compiled;

        public bool recompileOnEachUpdate = false;
        public Component sourceOverride;

        public string bindingStateHint { get; set; }
        public string bindingStateWarning { get; set; }
        public TopDownParser.Scope Scope { get; set; }

        public bool HasBindingStateWarning => !string.IsNullOrEmpty(bindingStateWarning);

        public void SetBindingStateWarning(string newBindingStateWarning)
        {
            bindingStateWarning = bindingStateWarning ?? newBindingStateWarning;
        }

        public virtual bool UpdateBindingSources(bool force = false)
        {
            Scope = Scope ?? new TopDownParser.Scope(CSharpTopDownParser.Singleton);
            if (_compiled && !recompileOnEachUpdate && !force)
            {
                return false;
            }
            _compiled = true;
            Scope.Clear();

            // Globals are added first - anything added *later* overrides these
            var globalBindingSources = FindObjectsOfType<BindingSource>();
            foreach (BindingSource bindingSource in globalBindingSources)
            {
                bindingSource.Register(Scope, true, this);
            }

            var localBindingSources = GetComponentsInParent<IBindingSource>();
            foreach (IBindingSource bindingSource in localBindingSources.Reverse())
            {
                bindingSource.Register(Scope, false, this);
            }

            AddSourceOverride();
            return true;
        }

        protected void AddSourceOverride()
        {
            if (sourceOverride != null)
            {
                AddObjectRoot("source", () => sourceOverride);
            }
        }

        protected void AddObjectRoot(string rootName, Func<object> func)
        {
            Scope.AddObjectRoot(rootName, func);
        }

        protected void RemoveRoot(string rootName)
        {
            Scope.RemoveRoot(rootName);
        }

        protected Type GetPathResultType(string sourceExpression)
        {
            return CSharpTopDownParser.Singleton.GetPathResultType(sourceExpression, Scope);
        }

        public object Evaluate(string code)
        {
            // Update scope to check for existing expressions
            try
            {
                return CSharpTopDownParser.Singleton.Evaluate(code, Scope);
            }
            catch (Exception e)
            {
                Debug.Log($"Failed to execute {code} in {GetGameObjectPath(gameObject)}");
                Debug.LogException(e);
                return null;
            }
        }

        private string GetGameObjectPath(GameObject gameObject)
        {
            string result = "";
            while (gameObject != null)
            {
                if (result != "")
                {
                    result = "." + result;
                }
                result = gameObject.name + result;
                gameObject = gameObject.transform.parent?.gameObject;
            }

            return result;
        }

        protected void SetValue(string path, object value)
        {
            CSharpTopDownParser.Singleton.SetValue(Scope.BuildOrGetExpression(path), value);
        }

        public bool IsValidPath(string destinationPath)
        {
            var expression = Scope.BuildOrGetExpression(destinationPath);
            return CSharpTopDownParser.Singleton.GetIsValidSetValueExpression(expression);
        }
    }
}