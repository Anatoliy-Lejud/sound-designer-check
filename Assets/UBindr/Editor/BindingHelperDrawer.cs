using System;
using System.Collections.Generic;
using System.Linq;
using Assets.UBindr.Bindings;
using Assets.UBindr.Expressions;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor.UBindr
{
    public class BindingHelperDrawer
    {
        private static bool _showBindingHelper = true;
        private readonly Dictionary<string, bool> _expanded = new Dictionary<string, bool>();
        private readonly Dictionary<string, MemberDescriber.MemberDesciptionGroup> _memberDescriptionGroupCache = new Dictionary<string, MemberDescriber.MemberDesciptionGroup>();

        private string _lastClicked = "";
        private bool _showInherited;
        private bool _showProperties;
        private bool _showFunctions;
        private bool _showMethods;
        public List<SetMethod> SetMethods { get; set; }
        public BindingWithBindingSources Binding { get; private set; }

        public BindingHelperDrawer(
            BindingWithBindingSources binding,
            bool showProperties,
            bool showFunctions,
            bool showMethods,
            Action<string> setAsSource = null,
            Action<string> setAsDestination = null,
            params SetMethod[] setMethods)
        {
            _showProperties = showProperties;
            _showFunctions = showFunctions;
            _showMethods = showMethods;

            Binding = binding;
            SetMethods = new List<SetMethod>();
            if (setAsSource != null)
            {
                SetMethods.Add(new SetMethod("Source", setAsSource));
            }
            if (setAsDestination != null)
            {
                SetMethods.Add(new SetMethod("Destination", setAsDestination));
            }
            SetMethods.AddRange(setMethods);
        }

        public void Draw()
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorHelper.Header("Binding Helper");

            _showBindingHelper = EditorGUILayout.Foldout(_showBindingHelper, "Show/Hide");
            if (!_showBindingHelper)
            {
                return;
            }

            EditorGUILayout.LabelField("(Click on row to select value)");

            EditorGUILayout.BeginHorizontal();
            try
            {
                GUILayout.Label("Properties");
                _showProperties = EditorGUILayout.Toggle(_showProperties);

                GUILayout.Label("Functions");
                _showFunctions = EditorGUILayout.Toggle(_showFunctions);

                GUILayout.Label("Methods");
                _showMethods = EditorGUILayout.Toggle(_showMethods);

                GUILayout.Label("Inherited");
                _showInherited = EditorGUILayout.Toggle(_showInherited);
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.LabelField("Expr: " + _lastClicked);

            if (SetMethods.Any() && !string.IsNullOrEmpty(_lastClicked))
            {
                EditorGUILayout.BeginHorizontal();
                DrawButtons();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.LabelField("");
            }

            Binding.UpdateBindingSources(true);

            var roots = Binding.Scope.MemberRoots.Values.ToList();
            foreach (TopDownParser.Scope.MemberRoot memberRoot in roots.OrderBy(x => x.Name))
            {
                var _wasExpanded = GetExpanded(memberRoot.Name);
                bool expanded = _expanded[memberRoot.Name] = EditorGUILayout.ToggleLeft(memberRoot.Name, _wasExpanded);

                if (expanded != _wasExpanded)
                {
                    _lastClicked = memberRoot.Name;
                }

                if (expanded)
                {
                    Type type = memberRoot.MemberType;
                    if (type == null)
                    {
                        EditorHelper.WarningLabel($"Unable to find Type for {memberRoot.Name}");
                        break;
                    }

                    Recurse(type, memberRoot.Name, 1);
                }
            }
        }

        private void DrawButtons()
        {
            if (_lastClicked != null && SetMethods.Any())
            {
                for (var index = 0; index < SetMethods.Count; index++)
                {
                    SetMethod setMethod = SetMethods[index];
                    GUIStyle style = EditorStyles.miniButton;
                    if (SetMethods.Count == 0 || index > 0 && index < SetMethods.Count - 1)
                    {
                        style = EditorStyles.miniButton;
                    }
                    else if (index == 0)
                    {
                        style = EditorStyles.miniButtonLeft;
                    }
                    else
                    {
                        style = EditorStyles.miniButtonRight;
                    }

                    if (GUILayout.Button("-> " + setMethod.Name, style))
                    {
                        Undo.RecordObject(Binding, "Changed " + setMethod.Name);
                        setMethod.Method(_lastClicked);
                    }
                }
            }
        }

        private void Recurse(Type type, string path, int indentLevel)
        {
            var memberDescriptionGroup = GetMemberDescriptionGroup(type, path);
            var pre = EditorGUI.indentLevel;
            EditorGUI.indentLevel = indentLevel;
            AddDescriptions(indentLevel, memberDescriptionGroup.FieldsAndProperties);
            AddDescriptions(indentLevel, memberDescriptionGroup.Methods);
            EditorGUI.indentLevel = pre;
        }

        private MemberDescriber.MemberDesciptionGroup GetMemberDescriptionGroup(Type type, string path)
        {
            return _memberDescriptionGroupCache.GetOrCreate(path, () => MemberDescriber.GetMemberDesciptionGroup(path, type, typeof(MonoBehaviour)));
        }

        private void AddDescriptions(int indentLevel, List<MemberDescriber.MemberDesciption> memberDescriptions)
        {
            foreach (MemberDescriber.MemberDesciption memberDescription in memberDescriptions)
            {
                if (!Wanted(memberDescription))
                {
                    continue;
                }
                string subPath = memberDescription.Usage;
                var childMemberDescriptionGroup = GetMemberDescriptionGroup(memberDescription.ReturnType, subPath);

                if (memberDescription.ReturnType != null
                    && !memberDescription.ReturnType.IsValueType
                    && memberDescription.ReturnType != typeof(string)
                    && childMemberDescriptionGroup.Count(Wanted) > 0)
                {
                    var _wasExpanded = GetExpanded(subPath);
                    var expanded = _expanded[subPath] = EditorGUILayout.ToggleLeft($"{memberDescription.Desc} ({childMemberDescriptionGroup.Count(Wanted)})", _wasExpanded);
                    if (expanded != _wasExpanded)
                    {
                        _lastClicked = subPath;
                        EditorGUIUtility.systemCopyBuffer = _lastClicked;
                    }

                    if (expanded)
                    {
                        Recurse(memberDescription.ReturnType, subPath, indentLevel + 1);
                    }
                }
                else
                {
                    AddCopyButton(memberDescription.Desc, subPath, indentLevel + 1);
                }
            }
        }

        private bool Wanted(MemberDescriber.MemberDesciption memberDesciption)
        {
            if (!_showInherited && memberDesciption.IsInherited)
            {
                return false;
            }

            if (!_showProperties && memberDesciption.IsProperty)
            {
                return false;
            }

            if (!_showMethods && memberDesciption.IsMethod)
            {
                return false;
            }

            if (!_showFunctions && memberDesciption.IsFunction)
            {
                return false;
            }

            if (memberDesciption.IsPropertyGetterOrSetter)
            {
                return false;
            }

            return true;
        }

        private void AddCopyButton(string text, string subPath, int indentLevel)
        {
            var s = new GUIStyle { contentOffset = new Vector2(indentLevel * 16, 0) };
            s.normal.textColor = Color.white;
            
            if (GUILayout.Button(text, s))
            {
                _lastClicked = subPath;
                EditorGUIUtility.systemCopyBuffer = subPath;
            }
        }

        private bool GetExpanded(string key)
        {
            return _expanded.GetOrCreate(key, () => false);
        }

        public class SetMethod
        {
            public SetMethod(string name, Action<string> method)
            {
                Name = name;
                Method = method;
            }
            public string Name { get; set; }
            public Action<string> Method { get; set; }
        }
    }
}