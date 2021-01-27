using UnityEngine;

namespace Assets.UBindr.Bindings
{
    public class RectTransformBinding : DestinedBinding
    {
        private ExposedFields _exposedFields;

        public class ExposedFields
        {
            private readonly RectTransform _parent;
            private float _x;
            private float _y;

            public ExposedFields(RectTransform parent)
            {
                _parent = parent;
            }

            public float height
            {
                set
                {
                    var sizeDelta = _parent.sizeDelta;
                    _parent.sizeDelta = new Vector2(sizeDelta.x, value);
                }
                get { return _parent.sizeDelta.y; }
            }
            public float width
            {
                set
                {
                    var sizeDelta = _parent.sizeDelta;
                    _parent.sizeDelta = new Vector2(value, sizeDelta.y);
                }
                get { return _parent.sizeDelta.x; }
            }
            public float x
            {
                set
                {
                    var sd = _parent.sizeDelta;
                    _parent.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, value, sd.x);
                    _x = value;
                }
                get { return _x; }
            }
            public float y
            {
                set
                {
                    var sd = _parent.sizeDelta;
                    _parent.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, value, sd.y);
                    _y = value;
                }
                get { return _y; }
            }
        }

        public override bool UpdateBindingSources(bool force = false)
        {
            var ran = base.UpdateBindingSources(force);
            if (ran)
            {
                _exposedFields = _exposedFields ?? new ExposedFields((RectTransform)transform);
                AddObjectRoot("dest", () => _exposedFields);
            }
            return ran;
        }
    }
}