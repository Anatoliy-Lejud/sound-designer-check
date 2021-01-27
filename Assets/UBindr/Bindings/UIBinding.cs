using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.UBindr.Bindings
{
    public class UIBinding : TypedDestinedBinding
    {
        public bool copyDestinationToSourceExpression;
        public bool updateOnEndEdit = true;
        public string OptionsExpression;
        public string LabelExpression;

        public bool editorInitialized;
        public bool LockedForEdit { get; set; }

        private bool IsDropdown { get { return ActualDestination is Dropdown; } }
        private bool IsSlider { get { return ActualDestination is Slider; } }

        public override void Initialize()
        {
            if (copyDestinationToSourceExpression)
            {
                SourceExpression = (string)GetDestinationValue();
            }
            base.Initialize();
        }

        public override void Update()
        {
            if (LockedForEdit)
            {
                return;
            }
            base.Update();
        }

        public void Start()
        {
            UpdateBindingSources(true);
            InputField inputField = ActualDestination as InputField;
            if (inputField != null && updateOnEndEdit)
            {
                // How can we do the same thing for the TextMeshPro version?
                inputField.onValueChanged.AddListener(v => { LockedForEdit = true; });
                inputField.onEndEdit.AddListener(v => { LockedForEdit = false; });
            }
        }

        public override string GetSourceExpression()
        {
            if (Application.isPlaying || !copyDestinationToSourceExpression)
            {
                return SourceExpression;
            }
            else
            {
                if (string.IsNullOrEmpty(DestinationPath))
                {
                    SetBindingStateWarning("No Destination Path provided");
                    return "";
                }

                try
                {
                    return (string)GetDestinationValue();
                }
                catch (Exception e)
                {
                    SetBindingStateWarning(e.ToString());
                    return "";
                }
            }
        }

        public override void AfterUpdate()
        {
            base.AfterUpdate();
            if (!string.IsNullOrEmpty(OptionsExpression) && IsDropdown)
            {
                SetOptions(ActualDestination as Dropdown);
            }
        }

        private bool _formatApplied;
        protected override object ApplyFormatToSourceValue(object value)
        {
            // Value comes from source
            var result = base.ApplyFormatToSourceValue(value);
            _formatApplied = false;
            if (IsDropdown)
            {
                if (result is int)
                {
                    return result;
                }
                else
                {
                    _formatApplied = true;
                    return _rawOptions.IndexOf(result);
                }
            }

            if (IsSlider)
            {
                return value;
                //if (value is float && CurrentSourceValue is int)
                //{
                //    return (int)value;
                //    //return Convert.ToInt32(value);
                //}
            }

            return result;
        }

        protected override object ApplyUnformatToDestinationValue(object value)
        {
            var result = base.ApplyUnformatToDestinationValue(value);
            if (IsDropdown)
            {
                if (_formatApplied)
                {
                    if (value is int)
                    {
                        return _rawOptions[(int)value];
                    }
                    else
                    {
                        return value;
                    }
                }
            }

            if (IsSlider)
            {
                if (value is float && CurrentSourceValue is int)
                {
                    //return (int)value;
                    return Convert.ToInt32(value);
                }
            }

            return result;
        }

        private int _oldHash = -99;
        private void SetOptions(Dropdown dropdown)
        {
            try
            {
                var options = Evaluate(OptionsExpression) as IEnumerable;
                if (options == null)
                {
                    dropdown.options = new List<Dropdown.OptionData>();
                    return;
                }
                int hash = 123;
                int p = 1024;
                foreach (object option in options)
                {
                    if (option != null)
                    {
                        hash ^= option.GetHashCode();
                    }
                    else
                    {
                        hash ^= p++;
                    }
                }
                if (hash != _oldHash)
                {
                    _oldHash = hash;
                    dropdown.options = EnumerableToOptionDatas(options);
                }
            }
            catch (Exception e)
            {
                SetBindingStateWarning(e.ToString());
            }
        }

        public List<object> _rawOptions = new List<object>();
        public List<Dropdown.OptionData> EnumerableToOptionDatas(IEnumerable value)
        {
            List<Dropdown.OptionData> result = new List<Dropdown.OptionData>();
            if (value == null)
            {
                return result;
            }

            _rawOptions.Clear();
            var enumerator = value.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (enumerator.Current is Dropdown.OptionData)
                {
                    result.Add(enumerator.Current as Dropdown.OptionData);
                    _rawOptions.Add(enumerator.Current);
                }
                else if (enumerator.Current != null)
                {
                    _rawOptions.Add(enumerator.Current);
                    if (string.IsNullOrEmpty(LabelExpression))
                    {
                        result.Add(new Dropdown.OptionData(enumerator.Current.ToString()));
                    }
                    else
                    {
                        AddObjectRoot("_opt", () => enumerator.Current);
                        var label = "";
                        try
                        {
                            label = (Evaluate(LabelExpression) ?? "").ToString();
                        }
                        catch
                        {
                            label = "err";
                        }

                        result.Add(new Dropdown.OptionData(label));
                        RemoveRoot("_opt");
                    }
                }
                else
                {
                    _rawOptions.Add(null);
                    result.Add(new Dropdown.OptionData(""));
                }
            }
            return result;
        }
    }
}