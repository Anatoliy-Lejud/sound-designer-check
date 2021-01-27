namespace Assets.UBindr.Bindings
{
    public class UIEnabledBinding : TypedDestinedBinding
    {
        public bool controlGameObject = true;     

        public override void SetDestinationValue(object value)
        {
            if (DestinationPath == "dest.gameObject.active")
            {
                if (ActualDestination != null && value is bool)
                {
                    ActualDestination.gameObject.SetActive((bool)value);
                }
            }
            else
            {
                SetValue(DestinationPath, value);
            }
        }
    }
}