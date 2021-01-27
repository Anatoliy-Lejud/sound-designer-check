using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.UBindr.Expressions;
using UnityEngine;

namespace Assets.Editor.UBindr
{
    public class MemberDescriber
    {
        public static MemberDesciptionGroup GetMemberDesciptionGroup(string basePath, Type type, Type filterType)
        {
            MemberDesciptionGroup memberDesciptionGroup = new MemberDesciptionGroup(type, basePath);
            AddFields(basePath, type, filterType, memberDesciptionGroup);
            AddProperties(basePath, type, filterType, memberDesciptionGroup);
            memberDesciptionGroup.FieldsAndProperties = memberDesciptionGroup.FieldsAndProperties.OrderBy(x => x.Usage).ToList();
            AddMethods(basePath, type, filterType, memberDesciptionGroup);

            return memberDesciptionGroup;
        }

        private static void AddFields(string basePath, Type type, Type filterType, MemberDesciptionGroup memberDesciptionGroup)
        {
            foreach (PropertyInfo propertyInfo in type.GetProperties().OrderBy(x => x.Name))
            {
                if (propertyInfo.DeclaringType == filterType)
                {
                    continue;
                }

                if (propertyInfo.CanRead && propertyInfo.GetGetMethod() != null && propertyInfo.GetGetMethod().IsPublic
                    || propertyInfo.CanWrite && propertyInfo.GetSetMethod() != null && propertyInfo.GetSetMethod().IsPublic)
                {
                    memberDesciptionGroup.FieldsAndProperties.Add(ToDescriber(basePath, propertyInfo,
                        propertyInfo.DeclaringType != type || propertyInfo.GetCustomAttributes(true).OfType<HideInInspector>().Any()));
                }
            }
        }

        private static void AddProperties(string basePath, Type type, Type filterType, MemberDesciptionGroup memberDesciptionGroup)
        {
            foreach (FieldInfo fieldInfo in type.GetFields().OrderBy(x => x.Name))
            {
                if (fieldInfo.DeclaringType == filterType)
                {
                    continue;
                }

                if (fieldInfo.IsPublic)
                {
                    memberDesciptionGroup.FieldsAndProperties.Add(ToDescriber(basePath, fieldInfo, fieldInfo.DeclaringType != type));
                }
            }
        }

        private static void AddMethods(string basePath, Type type, Type filterType, MemberDesciptionGroup memberDesciptionGroup)
        {
            foreach (MethodInfo methodInfo in type.GetMethods().OrderBy(x => x.Name))
            {
                if (methodInfo.DeclaringType == filterType)
                {
                    continue;
                }

                if (methodInfo.IsPublic)
                {
                    var metohodDescriber = ToDescriber(basePath, methodInfo, methodInfo.DeclaringType != type);
                    if (memberDesciptionGroup.FieldsAndProperties.Any(x => x.IsProperty && "get_" + x.Name == metohodDescriber.Name))
                    {
                        metohodDescriber.IsPropertyGetterOrSetter = true;
                    }
                    if (memberDesciptionGroup.FieldsAndProperties.Any(x => x.IsProperty && "set_" + x.Name == metohodDescriber.Name))
                    {
                        metohodDescriber.IsPropertyGetterOrSetter = true;
                    }
                    memberDesciptionGroup.Methods.Add(metohodDescriber);
                }
            }
        }

        public static MemberDesciption ToDescriber(string basePath, PropertyInfo propertyInfo, bool inherited)
        {
            if (propertyInfo.GetIndexParameters().Any())
            {
                return new MemberDesciption
                {
                    Desc = string.Format("this[{1}] {{ {2} }} ({0})", TypeToString(propertyInfo.PropertyType), propertyInfo.GetIndexParameters().SJoin(x => Param(x), ", "), SetGet(propertyInfo)),
                    Usage = string.Format("{0}[{1}]", basePath, propertyInfo.GetIndexParameters().SJoin(x => "", ",")),
                    Name = propertyInfo.Name,
                    ReturnType = propertyInfo.PropertyType,
                    IsInherited = inherited,
                    IsProperty = true
                };
            }
            else
            {
                return new MemberDesciption
                {
                    Desc = string.Format("{1} {{ {2} }} ({0})", TypeToString(propertyInfo.PropertyType), propertyInfo.Name, SetGet(propertyInfo)),
                    Usage = string.Format("{0}.{1}", basePath, propertyInfo.Name),
                    Name = propertyInfo.Name,
                    ReturnType = propertyInfo.PropertyType,
                    IsInherited = inherited,
                    IsProperty = true
                };
            }
        }

        public static MemberDesciption ToDescriber(string basePath, FieldInfo fieldInfo, bool inherited)
        {
            return new MemberDesciption
            {
                Desc = string.Format("{1} ({0})", TypeToString(fieldInfo.FieldType), fieldInfo.Name),
                Usage = string.Format("{0}.{1}", basePath, fieldInfo.Name),
                Name = fieldInfo.Name,
                ReturnType = fieldInfo.FieldType,
                IsInherited = inherited,
                IsProperty = true
            };
        }

        public static MemberDesciption ToDescriber(string basePath, MethodInfo methodInfo, bool inherited)
        {
            return new MemberDesciption
            {
                Desc = string.Format("{1}({2}) ({0})", TypeToString(methodInfo.ReturnType), methodInfo.Name, methodInfo.GetParameters().SJoin(x => Param(x), ", ")),
                Usage = string.Format("{0}.{1}({2})", basePath, methodInfo.Name, methodInfo.GetParameters().SJoin(x => "", ",")),
                Name = methodInfo.Name,
                ReturnType = methodInfo.ReturnType,
                IsInherited = inherited,
                IsFunction = methodInfo.ReturnType != typeof(void),
                IsMethod = methodInfo.ReturnType == typeof(void)
            };
        }

        public static string SetGet(PropertyInfo propertyInfo)
        {
            var str = new List<string>();
            if (propertyInfo.CanRead && propertyInfo.GetGetMethod() != null && propertyInfo.GetGetMethod().IsPublic)
            {
                str.Add("get");
            }
            if (propertyInfo.CanWrite && propertyInfo.GetSetMethod() != null && propertyInfo.GetSetMethod().IsPublic)
            {
                str.Add("set");
            }

            return str.SJoin("; ");
        }

        public static string Param(ParameterInfo parameterInfo)
        {
            return string.Format("{0} {1}", TypeToString(parameterInfo.ParameterType), parameterInfo.Name);
        }

        public static string TypeToString(Type type)
        {
            if (type == typeof(int)) { return "int"; }
            if (type == typeof(string)) { return "string"; }
            if (type == typeof(bool)) { return "bool"; }
            if (type == typeof(byte)) { return "byte"; }
            if (type == typeof(sbyte)) { return "sbyte"; }
            if (type == typeof(char)) { return "char"; }
            if (type == typeof(decimal)) { return "decimal"; }
            if (type == typeof(double)) { return "double"; }
            if (type == typeof(float)) { return "float"; }
            if (type == typeof(uint)) { return "uint"; }
            if (type == typeof(long)) { return "long"; }
            if (type == typeof(ulong)) { return "ulong"; }
            if (type == typeof(object)) { return "object"; }
            if (type == typeof(short)) { return "short"; }
            if (type == typeof(ushort)) { return "ushort"; }

            if (type.GetGenericArguments().Length > 0)
            {
                return type.Name.Substring(0, type.Name.Length - 2) + "<" + type.GetGenericArguments().SJoin(x => TypeToString(x.UnderlyingSystemType), ",") + ">";
            }

            return type.Name;
        }

        public class MemberDesciptionGroup
        {
            public MemberDesciptionGroup(Type type, string basePath)
            {
                Type = type;
                BasePath = basePath;
                FieldsAndProperties = new List<MemberDesciption>();
                Methods = new List<MemberDesciption>();
            }

            public Type Type { get; private set; }
            public string BasePath { get; private set; }

            public List<MemberDesciption> FieldsAndProperties { get; set; }
            public List<MemberDesciption> Methods { get; set; }

            public int Count(Func<MemberDesciption, bool> predicate)
            {
                return FieldsAndProperties.Count(predicate) + Methods.Count(predicate);
                //if (includeInherited)
                //{
                //    return FieldsAndProperties.Count + Methods.Count;
                //}
                //else
                //{
                //    return FieldsAndProperties.Count(x => !x.Inherited) + Methods.Count(x => !x.Inherited);
                //}
            }
        }

        public class MemberDesciption
        {
            public string Name { get; set; }
            public string Desc { get; set; }
            public string Usage { get; set; }
            public Type ReturnType { get; set; }
            public bool IsInherited { get; set; }
            public bool IsProperty { get; set; }
            public bool IsMethod { get; set; }
            public bool IsFunction { get; set; }
            public bool IsPropertyGetterOrSetter { get; set; }
        }
    }
}