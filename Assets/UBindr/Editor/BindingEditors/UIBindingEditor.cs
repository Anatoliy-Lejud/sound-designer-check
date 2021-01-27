using System;
using System.Collections;
using Assets.UBindr.Bindings;
using Assets.UBindr.Expressions;
using UnityEditor;
using UnityEngine.UI;

namespace Assets.Editor.UBindr.BindingEditors
{
    [CustomEditor(typeof(UIBinding))]
    public class UIBindingEditor : DestinedBindingEditor<UIBinding>
    {
        protected override void AfterUpdateBindingSources()
        {
            base.AfterUpdateBindingSources();
            UpdateUiBindings();
        }

        protected override bool ShowMethods { get { return false; } }
        protected override bool CanEditTwoWay { get { return false; } }
        protected override bool DestinationPathDisabled { get { return true; } }

        protected override void DrawBindingHelper()
        {
            if (BindingHelperDrawer == null)
            {
                BindingHelperDrawer = new BindingHelperDrawer(Binding, ShowProperties, ShowFunctions, ShowMethods, SetSourceExpression);
                if (GetIsDropdownField())
                {
                    BindingHelperDrawer.SetMethods.Add(new BindingHelperDrawer.SetMethod("Options", s => Binding.OptionsExpression = s));
                }
            }

            BindingHelperDrawer.Draw();
        }

        protected void SetSourceExpression(string s)
        {
            if (Binding.copyDestinationToSourceExpression && GetIsTextField())
            {
                TypeWrapper wrapper = TypeWrapper.GetTypeWrapper(Binding.uIType);
                if (wrapper.Setters.ContainsKey("text"))
                {
                    wrapper.Set(Binding.ActualDestination, "text", s);
                }
            }

            Binding.SourceExpression = s;
        }

        protected override void DrawBinding()
        {
            base.DrawBinding();
            if (GetIsTextField())
            {
                ChangeTracked(() => EditorGUILayout.ToggleLeft("Copy Destination to Source Expression", Binding.copyDestinationToSourceExpression), v => Binding.copyDestinationToSourceExpression = v);
                if (Binding.TwoWay)
                {
                    ChangeTracked(() => EditorGUILayout.ToggleLeft("Update On End Edit", Binding.updateOnEndEdit), v => Binding.updateOnEndEdit = v);
                }
            }
            else
            {
                Binding.copyDestinationToSourceExpression = false;
            }
        }

        private bool GetIsTextField()
        {
            return
                Binding.uIType != null &&
                (Binding.uIType.Name == "Text"
                 || Binding.uIType.Name == "TextMeshProUGUI"
                 || Binding.uIType.Name == "InputField"
                 || Binding.uIType.Name == "TMP_InputField");
        }

        private bool GetIsDropdownField()
        {
            return Binding.uIType != null && (Binding.uIType.Name == "Dropdown" || Binding.uIType.Name == "TMP_Dropdown");
        }

        protected override void DrawSource()
        {
            if (!Binding.copyDestinationToSourceExpression)
            {
                base.DrawSource();

                if (GetIsDropdownField())
                {
                    EditorGUILayout.LabelField("Options Expression");
                    ChangeTracked(() => EditorGUILayout.TextArea(Binding.OptionsExpression), v => Binding.OptionsExpression = v);

                    EditorGUILayout.LabelField("Label Expression [ will be applied to returned option, default is _opt.ToString() ]");
                    ChangeTracked(() => EditorGUILayout.TextArea(Binding.LabelExpression), v => Binding.LabelExpression = v);

                    //BindingHelperDrawer.SetMethods.Add(new BindingHelperDrawer.SetMethod("Label", s => Binding.LabelExpression = s));

                    if (!string.IsNullOrEmpty(Binding.OptionsExpression))
                    {
                        try
                        {
                            ReturnedSource = Binding.Evaluate(Binding.OptionsExpression);
                            if (ReturnedSource == null)
                            {
                                Binding.SetBindingStateWarning("Options Expression returns null, expected an IEnumerable");
                            }
                            else
                            {
                                if (ReturnedSource is string || !(ReturnedSource is IEnumerable))
                                {
                                    Binding.SetBindingStateWarning(string.Format("Options Expression returns {0}, expected an IEnumerable", MemberDescriber.TypeToString(ReturnedSource.GetType())));
                                }
                                else
                                {
                                    // Yay.
                                }
                            }

                        }
                        catch (Exception e)
                        {
                            Binding.SetBindingStateWarning(e.ToString());
                        }
                    }
                }
            }
            else
            {
                CheckSourceExpression();
            }
        }

        public void UpdateUiBindings()
        {
            if (Binding.uIType == null)
            {
                return;
            }

            Binding.ConvertDestinationToString = false;
            Binding.ConvertSourceToTargetType = false;
            switch (Binding.uIType.Name)
            {
                case "Toggle":
                    Binding.DestinationPath = "dest.isOn";
                    Binding.TwoWay = true;
                    break;
                case "Dropdown":
                case "TMP_Dropdown":
                    Binding.DestinationPath = "dest.value";
                    Binding.TwoWay = true;
                    break;
                case "InputField":
                case "TMP_InputField":
                    Binding.DestinationPath = "dest.text";
                    Binding.TwoWay = true;
                    Binding.ConvertDestinationToString = true;
                    Binding.ConvertSourceToTargetType = true;
                    if (!Binding.editorInitialized)
                    {
                        Binding.copyDestinationToSourceExpression = true;
                        Binding.editorInitialized = true;
                    }
                    break;
                case "TextMeshProUGUI":
                case "Text":
                    Binding.DestinationPath = "dest.text";
                    Binding.TwoWay = false;
                    Binding.ConvertDestinationToString = true;
                    if (!Binding.editorInitialized)
                    {
                        Binding.copyDestinationToSourceExpression = true;
                        Binding.editorInitialized = true;
                    }
                    break;
                case "Slider":
                    Binding.DestinationPath = "dest.value";
                    Binding.TwoWay = true;

                    if (ReturnedSourceExpression != null)
                    {
                        var brange = CSharpTopDownParser.GetAttribute<UnityEngine.RangeAttribute>(ReturnedSourceExpression);

                        if (brange != null)
                        {
                            Slider slider = (Slider)Binding.ActualDestination;
                            slider.minValue = brange.min;
                            slider.maxValue = brange.max;
                        }
                    }

                    break;
                case "Scrollbar":
                    Binding.DestinationPath = "dest.value";
                    Binding.TwoWay = true;
                    break;
                default:
                    Binding.SetBindingStateWarning(string.Format("Unhandled Destination Type: {0}", Binding.uIType.Name));
                    break;
            }
        }
    }
}