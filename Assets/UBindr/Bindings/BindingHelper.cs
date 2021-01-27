using System.Collections.Generic;
using UnityEngine;

namespace Assets.UBindr.Bindings
{
    public static class BindingHelper
    {
        public static Component GetDestination(Component destined)
        {
            // Use last non-binding component found *before* this component
            Component destinationContext = null;
            var components = new List<Component>();
            destined.gameObject.GetComponents(components);
            foreach (Component component in components)
            {
                if (!(component is IBinding))
                {
                    destinationContext = component;
                }

                if (component == destined)
                {
                    return destinationContext;
                }
            }

            return null;
        }     
    }
}