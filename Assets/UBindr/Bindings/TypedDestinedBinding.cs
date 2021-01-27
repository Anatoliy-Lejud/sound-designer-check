using System;

namespace Assets.UBindr.Bindings
{
    public abstract class TypedDestinedBinding : DestinedBinding
    {
        public Type uIType { get; set; }
        public override bool UpdateBindingSources(bool force = false)
        {
            var ran = base.UpdateBindingSources(force);
            if (!ran)
            {
                return false;
            }

            if (ActualDestination == null)
            {

                SetBindingStateWarning("Unable to resolve destination");
                uIType = null;
            }
            else
            {
                uIType = ActualDestination.GetType();
            }
            return true;
        }
    }
}