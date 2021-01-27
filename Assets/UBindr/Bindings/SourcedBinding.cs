using System;

namespace Assets.UBindr.Bindings
{
    public abstract class SourcedBinding : BindingWithBindingSources
    {
        public string SourceExpression;

        public virtual string GetSourceExpression()
        {
            return SourceExpression;
        }

        public object GetSourceValue()
        {
            if (string.IsNullOrEmpty(SourceExpression))
            {
                return null;
            }

            return Evaluate(SourceExpression);
        }

        public void SetSourceValue(object value)
        {
            if (string.IsNullOrEmpty(SourceExpression))
            {
                return;
            }
           
            SetValue(SourceExpression, value);
        }     
    }
}