// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace Roslyn.Utilities
{
    internal static class UICultureUtilities
    {
        // TODO (DevDiv 1117307): Replace with CultureInfo.CurrentUICulture.set when available.
        private const string currentUICultureName = "CurrentUICulture";
        private static readonly Action<CultureInfo>? s_setCurrentUICulture;

        private static bool TryGetCurrentUICultureSetter([NotNullWhen(returnValue: true)] out Action<CultureInfo>? setter)
        {
            const string cultureInfoTypeName = "System.Globalization.CultureInfo";
            const string cultureInfoTypeNameGlobalization = cultureInfoTypeName + ", System.Globalization, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";

            try
            {
                var type = Type.GetType(cultureInfoTypeNameGlobalization) ?? typeof(object).GetTypeInfo().Assembly.GetType(cultureInfoTypeName);
                if ((object?)type == null)
                {
                    setter = null;
                    return false;
                }

                var currentUICultureSetter = type.GetTypeInfo().GetDeclaredProperty(currentUICultureName)?.SetMethod;
                if ((object?)currentUICultureSetter == null || !currentUICultureSetter.IsStatic || currentUICultureSetter.ContainsGenericParameters || currentUICultureSetter.ReturnType != typeof(void))
                {
                    setter = null;
                    return false;
                }

                var parameters = currentUICultureSetter.GetParameters();
                if (parameters.Length != 1 || parameters[0].ParameterType != typeof(CultureInfo))
                {
                    setter = null;
                    return false;
                }

                setter = (Action<CultureInfo>)currentUICultureSetter.CreateDelegate(typeof(Action<CultureInfo>));
                return true;
            }
            catch
            {
                setter = null;
                return false;
            }
        }

        private static bool TryGetCurrentThreadUICultureSetter([NotNullWhen(returnValue: true)] out Action<CultureInfo>? setter)
        {
            const string threadTypeName = "System.Threading.Thread";
            const string currentThreadName = "CurrentThread";

            try
            {
                var type = typeof(object).GetTypeInfo().Assembly.GetType(threadTypeName);
                if (type is null)
                {
                    setter = null;
                    return false;
                }

                var typeInfo = type.GetTypeInfo();
                var currentThreadGetter = typeInfo.GetDeclaredProperty(currentThreadName)?.GetMethod;
                if ((object?)currentThreadGetter == null || !currentThreadGetter.IsStatic || currentThreadGetter.ContainsGenericParameters || currentThreadGetter.ReturnType != type || currentThreadGetter.GetParameters().Length != 0)
                {
                    setter = null;
                    return false;
                }

                var currentUICultureSetter = typeInfo.GetDeclaredProperty(currentUICultureName)?.SetMethod;
                if ((object?)currentUICultureSetter == null || currentUICultureSetter.IsStatic || currentUICultureSetter.ContainsGenericParameters || currentUICultureSetter.ReturnType != typeof(void))
                {
                    setter = null;
                    return false;
                }

                var parameters = currentUICultureSetter.GetParameters();
                if (parameters.Length != 1 || parameters[0].ParameterType != typeof(CultureInfo))
                {
                    setter = null;
                    return false;
                }

                setter = culture =>
                {
                    currentUICultureSetter.Invoke(currentThreadGetter.Invoke(null, null), new[] { culture });
                };
                return true;
            }
            catch
            {
                setter = null;
                return false;
            }
        }

        static UICultureUtilities()
        {
            if (!TryGetCurrentUICultureSetter(out s_setCurrentUICulture) &&
                !TryGetCurrentThreadUICultureSetter(out s_setCurrentUICulture))
            {
                s_setCurrentUICulture = null;
            }
        }

        public static Action WithCurrentUICulture(Action action)
        {
            if (s_setCurrentUICulture == null)
            {
                return action;
            }

            var savedCulture = CultureInfo.CurrentUICulture;
            return () =>
            {
                var currentCulture = CultureInfo.CurrentUICulture;
                if (currentCulture != savedCulture)
                {
                    s_setCurrentUICulture(savedCulture);
                    try
                    {
                        action();
                    }
                    finally
                    {
                        s_setCurrentUICulture(currentCulture);
                    }
                }
                else
                {
                    action();
                }
            };
        }

        public static Action<T> WithCurrentUICulture<T>(Action<T> action)
        {
            if (s_setCurrentUICulture == null)
            {
                return action;
            }

            var savedCulture = CultureInfo.CurrentUICulture;
            return param =>
            {
                var currentCulture = CultureInfo.CurrentUICulture;
                if (currentCulture != savedCulture)
                {
                    s_setCurrentUICulture(savedCulture);
                    try
                    {
                        action(param);
                    }
                    finally
                    {
                        s_setCurrentUICulture(currentCulture);
                    }
                }
                else
                {
                    action(param);
                }
            };
        }

        public static Func<T> WithCurrentUICulture<T>(Func<T> func)
        {
            if (s_setCurrentUICulture == null)
            {
                return func;
            }

            var savedCulture = CultureInfo.CurrentUICulture;
            return () =>
            {
                var currentCulture = CultureInfo.CurrentUICulture;
                if (currentCulture != savedCulture)
                {
                    s_setCurrentUICulture(savedCulture);
                    try
                    {
                        return func();
                    }
                    finally
                    {
                        s_setCurrentUICulture(currentCulture);
                    }
                }
                else
                {
                    return func();
                }
            };
        }
    }
}
