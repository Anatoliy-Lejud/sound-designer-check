using System;

namespace Assets.UBindr.Bindings
{
    public abstract class DestinedBinding : DestinationContextedBinding
    {
        public string DestinationPath;
        public bool TwoWay;
        public bool CopyInitialValueFromDestination;
        public bool DisableExecuteDuringDesign;
        public bool ConvertDestinationToString;
        public bool ConvertSourceToTargetType;

        private object _previousSourceValue;
        private object _previousDestinationValue;
        private bool _initialized;
        private string _usedDestinationPath;
        private string _usedSourcePath;
        private int _updatesSinceCheck;
        private bool _copiedInitialValueFromDestination;

        protected object CurrentSourceValue { get; set; }
        protected object CurrentDestinationValue { get; set; }

        public virtual void Update()
        {
            UpdateBindingSources();
            if (!_initialized)
            {
                Initialize();
            }

            if (string.IsNullOrEmpty(SourceExpression) || string.IsNullOrEmpty(DestinationPath))
            {
                return;
            }

            try
            {
                _updatesSinceCheck++;

                if (_usedDestinationPath != DestinationPath || _usedSourcePath != SourceExpression || _updatesSinceCheck > 30)
                {
                    _previousSourceValue = null;
                    _previousDestinationValue = null;
                    _updatesSinceCheck = 0;
                }

                bindingStateWarning = null;

                if (!_copiedInitialValueFromDestination && TwoWay && CopyInitialValueFromDestination)
                {
                    _copiedInitialValueFromDestination = true;
                    SetSourceValue(ApplyUnformatToDestinationValue(GetDestinationValue()));
                }

                CurrentSourceValue = GetSourceValue();
                if (!Equals(CurrentSourceValue, _previousSourceValue))
                {
                    // Source changed
                    var formatted = ApplyFormatToSourceValue(CurrentSourceValue);
                    SetDestinationValue(formatted);
                    _previousSourceValue = CurrentSourceValue;
                    _previousDestinationValue = formatted;

                }
                else if (TwoWay)
                {
                    CurrentDestinationValue = GetDestinationValue();
                    if (!Equals(CurrentDestinationValue, _previousDestinationValue))
                    {
                        // Destination changed
                        var unformatted = ApplyUnformatToDestinationValue(CurrentDestinationValue);
                        SetSourceValue(unformatted);
                        _previousDestinationValue = CurrentDestinationValue;
                        _previousSourceValue = unformatted;
                    }
                }

                _usedDestinationPath = DestinationPath;
                _usedSourcePath = SourceExpression;

                AfterUpdate();
            }
            catch (Exception e)
            {
                bindingStateWarning = e.ToString();

                // Boo
            }
        }

        public virtual void AfterUpdate()
        {
        }

        public virtual void Initialize()
        {
            _initialized = true;
            Update();
        }

        public object GetDestination()
        {
            if (Destination != null)
            {
                return Destination;
            }

            return BindingHelper.GetDestination(this);
        }

        protected virtual object ApplyFormatToSourceValue(object value)
        {
            if (ConvertDestinationToString && value != null && !(value is string))
            {
                value = value.ToString();
            }
            return value;
        }

        protected virtual object ApplyUnformatToDestinationValue(object value)
        {
            var destinationType = GetDestinationType();
            if (Equals(destinationType, typeof(float)) && value is string)
            {
                return float.Parse((string)value);
            }
            if (Equals(destinationType, typeof(double)) && value is string)
            {
                return double.Parse((string)value);
            }
            if (Equals(destinationType, typeof(int)) && value is string)
            {
                return int.Parse((string)value);
            }

            if (ConvertSourceToTargetType && value != null)
            {
                var targetType = GetPathResultType(SourceExpression);
                return ConvertTo(value, targetType);
            }

            return value;
        }

        private object GetDestinationType() { return GetPathResultType(DestinationPath); }

        public virtual object GetDestinationValue()
        {
            return Evaluate(DestinationPath);
        }

        public virtual void SetDestinationValue(object value)
        {
            if (IsValidPath(DestinationPath))
            {
                SetValue(DestinationPath, value);
            }
            else
            {
                AddObjectRoot("value", () => value);
                try
                {
                    Evaluate(DestinationPath);
                }
                finally
                {
                    RemoveRoot("value");
                }
            }
        }

        public object ConvertTo(object value, Type type)
        {
            if (type == typeof(int)) { return Convert.ToInt32(value); }
            if (type == typeof(string)) { return Convert.ToString(value); }
            if (type == typeof(bool)) { return Convert.ToBoolean(value); }
            if (type == typeof(byte)) { return Convert.ToByte(value); }
            if (type == typeof(sbyte)) { return Convert.ToSByte(value); }
            if (type == typeof(char)) { return Convert.ToChar(value); }
            if (type == typeof(decimal)) { return Convert.ToDecimal(value); }
            if (type == typeof(double)) { return Convert.ToDouble(value); }
            if (type == typeof(float)) { return Convert.ToSingle(value); }
            if (type == typeof(uint)) { return Convert.ToUInt32(value); }
            if (type == typeof(long)) { return Convert.ToInt64(value); }
            if (type == typeof(ulong)) { return Convert.ToUInt64(value); }
            if (type == typeof(object)) { return value; }
            if (type == typeof(short)) { return Convert.ToInt16(value); }
            if (type == typeof(ushort)) { return Convert.ToUInt16(value); }

            return value;
        }
    }
}