//using System;
//using UnityEngine;

//namespace Assets.UBindr.Bindings
//{
//    public class StringBinding : DestinedBinding
//    {
//        public bool copyDestinationToSourceExpression = true;
//        public override void Initialize()
//        {
//            if (copyDestinationToSourceExpression)
//            {
//                SourceExpression = (string)GetDestinationValue();
//            }
//            base.Initialize();
//        }

//        protected override object ApplyFormatToSourceValue(object value)
//        {
//            if (value == null)
//            {
//                return "";
//            }

//            return value.ToString();
//        }

//        public override string GetSourceExpression()
//        {
//            if (Application.isPlaying || !copyDestinationToSourceExpression)
//            {
//                return SourceExpression;
//            }
//            else
//            {
//                if (string.IsNullOrEmpty(DestinationPath))
//                {
//                    SetBindingStateWarning("No Destination Path provided");
//                    return "";
//                }

//                try
//                {
//                    return (string)GetDestinationValue();
//                }
//                catch (Exception e)
//                {
//                    SetBindingStateWarning(e.ToString());
//                    return "";
//                }
//            }
//        }
//    }
//}