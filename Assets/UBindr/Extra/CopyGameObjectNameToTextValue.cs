using Assets.UBindr.Expressions;
using UnityEngine;

namespace Assets.UBindr.Extra
{
    [ExecuteAlways]
    public class CopyGameObjectNameToTextValue : MonoBehaviour
    {
        public GameObject nameSource;
        public Component destination;

        public void Update()
        {
            if (nameSource == null || destination == null)
            {
                return;
            }

            var newValue = nameSource.name;
            var tw = TypeWrapper.GetTypeWrapper(destination.GetType());
            var textGetter = tw.Getters.SafeGetValue("text");
            var textSetter = tw.Setters.SafeGetValue("text");
            if (textSetter != null)
            {
                if (textGetter != null)
                {
                    var oldValue = textGetter.Invoke(destination);
                    if ((string) oldValue == newValue)
                    {
                        return;
                    }
                }
                tw.Setters["text"].Invoke(destination, newValue);
            }
        }
    }
}