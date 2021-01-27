using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Assets.UBindr.Expressions
{
    public class TypeWrapper
    {
        static TypeWrapper()
        {
            GetSetWrappers = new Dictionary<Type, TypeWrapper>();
        }

        public static TypeWrapper GetTypeWrapper(Type type)
        {
            if (!GetSetWrappers.ContainsKey(type))
            {
                GetSetWrappers.Add(type, new TypeWrapper(type));
            }
            return GetSetWrappers[type];
        }

        public static Dictionary<Type, TypeWrapper> GetSetWrappers { get; private set; }
        public Type Type { get; private set; }
        public Dictionary<string, Func<object, object>> Getters { get; private set; }
        public Dictionary<string, Action<object, object>> Setters { get; private set; }
        public Dictionary<string, Type> Types { get; private set; }
        public Dictionary<string, Action<object>> InputFreeMethods { get; private set; }
        public List<MethodInfo> MethodInfos { get; private set; }
        public List<PropertyInfo> PropertyInfos { get; private set; }

        public TypeWrapper(Type type)
        {
            Getters = new Dictionary<string, Func<object, object>>();
            Setters = new Dictionary<string, Action<object, object>>();
            Types = new Dictionary<string, Type>();
            InputFreeMethods = new Dictionary<string, Action<object>>();
            MethodInfos = new List<MethodInfo>();
            PropertyInfos = new List<PropertyInfo>();
            Type = type;
            BuildGettersAndGetters();
        }

        public static Type GetTypeWithGenericDefinition(Type type, Type definition)
        {
            if (definition.IsInterface)
            {
                foreach (var interfaceType in type.GetInterfaces())
                    if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == definition)
                    {
                        return interfaceType;
                    }
            }

            for (Type t = type; t != null; t = t.BaseType)
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == definition)
                {
                    return t;
                }
            }

            return null;
        }

        public static bool IsTypeDerivedFromGenericType(Type type, Type definition)
        {
            return GetTypeWithGenericDefinition(type, definition) != null;
        }

        public object Get(object context, string name)
        {
            if (name == null)
            {
                throw new InvalidOperationException("No getter name has been specified");
            }
            var getter = Getters.SafeGetValue(name);
            if (getter == null)
            {
                throw new InvalidOperationException(string.Format("Unable to find getter for {0}!", name));
            }

            return getter(context);
        }

        public void Set(object context, string name, object value)
        {
            if (name == null)
            {
                throw new InvalidOperationException("No setter name has been specified");
            }

            var setter = Setters.SafeGetValue(name);
            if (setter == null)
            {
                throw new InvalidOperationException(String.Format("Unable to find setter for {0}!", name));
            }

            setter(context, value);
        }

        private void BuildGettersAndGetters()
        {
            PropertyInfo[] pinfos = Type.GetProperties();
            foreach (PropertyInfo pi in pinfos)
            {
                if (pi.CanRead)
                {
                    Getters.SafeAddValue(pi.Name, x => pi.GetValue(x, null));
                }
                if (pi.CanWrite)
                {
                    Setters.SafeAddValue(pi.Name, (x, y) => pi.SetValue(x, y, null));
                }
                Types.SafeAddValue(pi.Name, pi.PropertyType);
                PropertyInfos.Add(pi);
            }

            FieldInfo[] finfos = Type.GetFields();
            foreach (FieldInfo fi in finfos)
            {
                Getters.SafeAddValue(fi.Name, x => fi.GetValue(x));
                Setters.SafeAddValue(fi.Name, (x, y) => fi.SetValue(x, y));
                Types.Add(fi.Name, fi.FieldType);
            }

            MethodInfo[] methodInfos = Type.GetMethods();
            MethodInfos.AddRange(methodInfos);
            foreach (MethodInfo methodInfo in methodInfos)
            {
                if (!methodInfo.GetParameters().Any())
                {
                    InputFreeMethods.SafeAddValue(methodInfo.Name, x => methodInfo.Invoke(x, new object[0]));
                }
            }
        }

        public object ExecuteMethod(object context, string member, bool disableExecute, object[] parameters)
        {
            Type[] parameterTypes = parameters.Select(p => p != null ? p.GetType() : null).ToArray();
            var method = Type.GetMethod(member, parameterTypes);
            if (method != null)
            {
                if (!disableExecute)
                {
                    return method.Invoke(context, parameters);
                }
                else
                {
                    return null;
                }
            }

            method = Type.GetMethod(member);
            if (method == null)
            {
                throw new InvalidOperationException(string.Format("Unable to find method {0} on {1} that accepts {2}", member, context, parameterTypes.SJoin()));
            }

            List<object> otherParams = new List<object>();
            var expectedParameterTypes = method.GetParameters().ToList();

            if (parameters.Length != expectedParameterTypes.Count)
            {
                throw new InvalidOperationException(string.Format("Unable to find method {0} on {1} that accepts {2}", member, context, parameterTypes.SJoin()));
            }

            for (var index = 0; index < parameters.Length; index++)
            {
                object parameter = parameters[index];
                ParameterInfo expectedParameterType = expectedParameterTypes[index];
                parameter = TryConvertToType(parameter, expectedParameterType.ParameterType);
                otherParams.Add(parameter);
            }

            //var otherParamTypes = otherParams.Select(p => p != null ? p.GetType() : null).ToArray();

            if (!disableExecute)
            {
                return method.Invoke(context, otherParams.ToArray());
            }
            else
            {
                return null;
            }
        }

        public object ExecuteIndexer(object context, string member, bool disableExecute, object[] parameters)
        {
            var parameterTypes = parameters.Select(p => p != null ? p.GetType() : null).ToArray();
            var method =
                Type.GetMethod("get_Item", parameterTypes)
                ?? Type.GetMethod("GetValue", parameterTypes);

            if (method != null)
            {
                if (!disableExecute)
                {
                    return method.Invoke(context, parameters);
                }
                else
                {
                    return null;
                }
            }

            var iparams = parameters.Select(Convert.ToInt32).Cast<object>().ToArray();
            var iparamTypes = iparams.Select(x => x.GetType()).ToArray();
            method =
                Type.GetMethod("get_Item", iparamTypes)
                ?? Type.GetMethod("GetValue", iparamTypes);

            if (method != null)
            {
                if (!disableExecute)
                {
                    return method.Invoke(context, iparams);
                }
                else
                {
                    return null;
                }
            }

            throw new InvalidOperationException(string.Format(@"Unable to find indexer {0} for parameters {1} on {2}", member, parameterTypes.SJoin(), context));
        }

        public object TryConvertToType(object source, Type targetType)

        {
            if (targetType == typeof(int))
            {
                return Convert.ToInt32(source);
            }
            else if (targetType == typeof(float))
            {
                return Convert.ToSingle(source);
            }
            else if (targetType == typeof(byte))
            {
                return Convert.ToByte(source);
            }
            else if (targetType == typeof(double))
            {
                return Convert.ToDouble(source);
            }
            else if (targetType == typeof(short))
            {
                return Convert.ToInt16(source);
            }
            else if (targetType == typeof(long))
            {
                return Convert.ToInt64(source);
            }

            return source;
        }
    }
}