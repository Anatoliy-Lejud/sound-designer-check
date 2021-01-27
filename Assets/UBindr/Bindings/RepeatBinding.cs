using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.UBindr.Expressions;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Assets.UBindr.Bindings
{
    public class RepeatBinding : SourcedBinding, IBindingSource
    {
        public string rowSourceName = "row";
        public string orderBy;
        public bool descending = true;
        public int maxItems = -1;
        public GameObject ItemPrefab;

        public void Start()
        {
            // Remove all children, to prevent them from trying to run an update
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }

        public void Register(TopDownParser.Scope scope, bool global, object source)
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying && !global)
            {
                AddFakeRow(scope, ReferenceEquals(source, this));
            }
#endif
        }

        public void Update()
        {
            UpdateBindingSources();

            if (ItemPrefab == null)
            {
                return;
            }

            bindingStateWarning = null;
            try
            {
                var wantedChildren = GetWantedChildren();
                var currentChildren = GetCurrentChildren();

                for (var index = 0; index < wantedChildren.Count; index++)
                {
                    object wantedChild = wantedChildren[index];
                    var currentChild = currentChildren.SingleOrDefault(x => x.Data == wantedChild);

                    if (currentChild == null)
                    {
                        GameObject itemInstance = Instantiate(ItemPrefab, transform);
                        currentChild = itemInstance.AddComponent(typeof(RepeatBindingRow)) as RepeatBindingRow;
                        
                        currentChild.Data = wantedChild;
                        currentChild.RepeatBinding = this;
                        currentChildren.Insert(index, currentChild);

                        foreach (BindingWithBindingSources bindingWithBindingSource in currentChild.GetComponentsInChildren<BindingWithBindingSources>())
                        {
                            bindingWithBindingSource.UpdateBindingSources(false);
                        }

                        //added++;
                    }
                    if (currentChild.transform.GetSiblingIndex() != index)
                    {
                        currentChild.transform.SetSiblingIndex(index);
                        //moved++;
                    }

                    currentChild.Info.index = index;
                    currentChild.Info.first = index == 0;
                    currentChild.Info.last = index == wantedChildren.Count - 1;
                    currentChild.Info.middle = !currentChild.Info.first && !currentChild.Info.last;
                    currentChild.Info.even = index % 2 == 0;
                    currentChild.Info.odd = !currentChild.Info.even;
                }
                CullExcessChildren(currentChildren, wantedChildren);
            }
            catch (Exception e)
            {
                bindingStateWarning = e.ToString();
            }

            //if (added > 0 || destroyed > 0 || moved > 0)
            //{
            //    Debug.Log(string.Format("Added {0}, Destroyed {1}, Moved {2}", added, destroyed, moved));
            //}
        }

        protected List<object> GetWantedChildren()
        {
            var wantedChildren = new List<object>();
            var data = (IEnumerable)GetSourceValue();
            if (data != null)
            {
                foreach (var wantedChild in data)
                {
                    wantedChildren.Add(wantedChild);
                }
            }

            wantedChildren = SortRows(wantedChildren);

            if (maxItems > -1 && wantedChildren.Count > maxItems)
            {
                wantedChildren = wantedChildren.Take(maxItems).ToList();
            }

            return wantedChildren;
        }

        private List<object> SortRows(List<object> wantedChildren)
        {
            try
            {
                if (string.IsNullOrEmpty(orderBy))
                {
                    return wantedChildren;
                }

                object currentRow = null;
                AddObjectRoot(rowSourceName, () => currentRow);
                List<object[]> toSort = new List<object[]>();
                foreach (object wantedChild in wantedChildren)
                {
                    currentRow = wantedChild;
                    var sortValue = Evaluate(orderBy);
                    toSort.Add(new object[] { sortValue, currentRow });
                }
                RemoveRoot(rowSourceName);

                return @descending
                    ? toSort.OrderByDescending(x => x[0]).Select(x => x[1]).ToList()
                    : toSort.OrderBy(x => x[0]).Select(x => x[1]).ToList();
            }
            catch (Exception e)
            {
                bindingStateWarning = e.ToString();
                return wantedChildren;
            }
        }

        private List<RepeatBindingRow> GetCurrentChildren()
        {
            var currentChildren = new List<RepeatBindingRow>();
            foreach (Transform child in transform)
            {
                var row = child.GetComponent<RepeatBindingRow>();
                if (row == null)
                {
                    // Doesn't count as a destroy!
                    Destroy(child.gameObject);
                }
                else
                {
                    currentChildren.Add(row);
                }
            }
            return currentChildren;
        }

        private static void CullExcessChildren(List<RepeatBindingRow> currentChildren, List<object> wantedChildren)
        {
            foreach (RepeatBindingRow repeatBindingRow in currentChildren)
            {
                if (!wantedChildren.Contains(repeatBindingRow.Data))
                {
                    //destroyed++;
                    Destroy(repeatBindingRow.gameObject);
                }
            }
        }

        private void AddFakeRow(TopDownParser.Scope scope, bool addingToSelf)
        {
            if (!addingToSelf)
            {
                UpdateBindingSources(true);
            }
            else
            {
                AddSourceOverride();
            }

            // Pretends to be a Binding Source during design, but it really isn't.
            // This code shouldn't be visited during normal running.
            try
            {
                //Debug.LogWarning(expressionEvaluator.MemberPathNavigator.MemberRoots.SJoin(x => x.Key));
                var sourceValue = Evaluate(SourceExpression);

                if (sourceValue != null)
                {
                    IEnumerable enumerable = (IEnumerable)sourceValue;
                    var enumerator = enumerable.GetEnumerator();

                    var row = enumerator.MoveNext() ? enumerator.Current : null;

                    // Add the row to the child expressionEvaluator, not our ExpressionEvaluator
                    scope.AddObjectRoot(rowSourceName, () => row);
                    if (!addingToSelf)
                    {
                        scope.AddObjectRoot(rowSourceName + "_info", () => new RepeatBindingRow.RepeatBindingInfo
                        {
                            first = true,
                            middle = true,
                            last = true
                        });
                    }
                }
            }
            catch (Exception e)
            {
                SetBindingStateWarning(e.ToString());
            }
        }

        internal class RepeatBindingRow : MonoBehaviour, IBindingSource
        {
            public object Data { get; set; }
            public RepeatBinding RepeatBinding { get; set; }
            public RepeatBindingInfo Info = new RepeatBindingInfo();

            public void Register(TopDownParser.Scope scope, bool global, object source)
            {
                if (global)
                {
                    return;
                }
                scope.AddObjectRoot(RepeatBinding.rowSourceName, () => Data);
                scope.AddObjectRoot(RepeatBinding.rowSourceName + "_info", () => Info);
            }

            public class RepeatBindingInfo
            {
                // https://docs.angularjs.org/api/ng/directive/ngRepeat
                public int index;
                public bool odd;
                public bool even;
                public bool first;
                public bool middle;
                public bool last;
            }
        }
    }
}