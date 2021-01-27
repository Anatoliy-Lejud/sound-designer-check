using UnityEngine;

namespace Assets.UBindr.Bindings
{
    public abstract class DestinationContextedBinding : SourcedBinding
    {
        public Component Destination;
        public Component ActualDestination { get; set; }

        public override bool UpdateBindingSources(bool force = false)
        {
            bool ran = base.UpdateBindingSources(force);

            if (!ran)
            {
                return false;
            }
            if (Destination != null)
            {
                ActualDestination = Destination;
                AddObjectRoot("dest", () => ActualDestination = Destination);
                var above = BindingHelper.GetDestination(this);
                if (above != null)
                {
                    AddObjectRoot("above", () => above);
                }
            }
            else
            {
                ActualDestination = BindingHelper.GetDestination(this);
                AddObjectRoot("dest", () => ActualDestination);
            }
            return true;
        }       
    }
}