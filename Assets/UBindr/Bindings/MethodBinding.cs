using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.UBindr.Bindings
{
    public class MethodBinding : BindingWithBindingSources
    {
        public Component Destination;
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

                if (!_initialized)
                {
                    Initialize();
                }
            }
            catch (Exception e)
            {
                bindingStateWarning = e.ToString();
            }
        }

        public override bool UpdateBindingSources(bool force = false)
        {
            var ran = base.UpdateBindingSources(force);
            if (!ran)
            {
                return false;
            }
            if (Destination != null)
            {
                AddObjectRoot("dest", () => Destination);
            }
            else
            {
                AddObjectRoot("dest", () => BindingHelper.GetDestination(this));
            }
            return true;
        }

        private void Initialize()
        {
            _initialized = true;
            var destination = GetDestination();

            var button = destination as Button;
            if (button != null)
            {
                // This seems to fail if the button is selected in the UI!
                button.onClick.AddListener(EvaluateMethodPath);
                return;
            }
            var eventTrigger = destination as EventTrigger;
            if (eventTrigger != null)
            {
                foreach (EventTrigger.Entry entry in eventTrigger.triggers)
                {
                    //var @event = new EventTrigger.TriggerEvent();
                    //entry.callback = @event;
                    entry.callback.AddListener(x => Evaluate(MethodPath));
                }
                return;
            }
            throw new InvalidOperationException(string.Format("Destination type is unhandled: {0}!", destination != null ? destination.GetType().Name : "null"));
        }

        public void EvaluateMethodPath()
        {
            Evaluate(MethodPath);
        }

        private Component GetDestination()
        {
            Component destination;
            if (Destination == null)
            {
                destination = BindingHelper.GetDestination(this);
            }
            else
            {
                destination = Destination;
            }

            return destination;
        }
    }
}