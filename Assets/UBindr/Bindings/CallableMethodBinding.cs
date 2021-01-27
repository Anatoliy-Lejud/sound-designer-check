using System;
using UnityEditor;
using UnityEngine;

namespace Assets.UBindr.Bindings
{
    public class CallableMethodBinding : BindingWithBindingSources
    {
        public string MethodPath;

        private bool _initialized;
        public static bool _warningHasBeenSent;

        public void Start()
        {
            if (!_warningHasBeenSent && Selection.activeTransform != null && Selection.activeTransform.gameObject.GetComponent<UnityEngine.UI.Button>() != null)
            {
                Debug.LogError("A button is selected in the editor, this will prevent method bindings to function!");
                _warningHasBeenSent = true;
            }
        }

        public void Update()
        {
            try
            {
                if (recompileOnEachUpdate || !_initialized)
                {
                    UpdateBindingSources();
                }
            }
            catch (Exception e)
            {
                bindingStateWarning = e.ToString();
            }
        }

        public override bool UpdateBindingSources(bool force = false)
        {
            _initialized = true;
            var ran = base.UpdateBindingSources(force);
            if (!ran)
            {
                return false;
            }
            return true;
        }

        public void ExecuteMethod()
        {
            Evaluate(MethodPath);
        }
    }
}