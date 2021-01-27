//using System;
//using Assets.UBindr.Bindings;
//using UnityEditor;

//namespace Assets.Editor.UBindr.BindingEditors
//{
//    [CustomEditor(typeof(StringBinding))]
//    public class StringBindingEditor : DestinedBindingEditor<StringBinding>
//    {
//        protected override void DrawBinding()
//        {
//            base.DrawBinding();
//            Binding.copyDestinationToSourceExpression = EditorGUILayout.ToggleLeft("Copy Destination to Source Expression", Binding.copyDestinationToSourceExpression);
//        }

//        protected override bool ShowMethods { get { return false; } }

//        protected override Type ExpectedSourceType()
//        {
//            return typeof(string);
//        }

//        protected override void DrawSource()
//        {
//            if (!Binding.copyDestinationToSourceExpression)
//            {
//                base.DrawSource();
//            }
//            else
//            {
//                CheckSourceExpression();
//            }
//        }
//    }
//}