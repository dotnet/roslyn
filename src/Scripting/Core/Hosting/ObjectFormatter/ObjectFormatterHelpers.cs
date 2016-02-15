// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    using TypeInfo = System.Reflection.TypeInfo;

    internal static class ObjectFormatterHelpers
    {
        internal static readonly object VoidValue = new object();

        internal const int NumberRadixDecimal = 10;
        internal const int NumberRadixHexadecimal = 16;

        internal static bool HasOverriddenToString(TypeInfo type)
        {
            if (type.IsInterface)
            {
                return false;
            }

            while (type.AsType() != typeof(object))
            {
                if (type.GetDeclaredMethod("ToString", Type.EmptyTypes) != null)
                {
                    return true;
                }

                type = type.BaseType.GetTypeInfo();
            }

            return false;
        }

        internal static DebuggerDisplayAttribute GetApplicableDebuggerDisplayAttribute(MemberInfo member)
        {
            // Includes inherited attributes. The debugger uses the first attribute if multiple are applied.
            var result = member.GetCustomAttributes<DebuggerDisplayAttribute>().FirstOrDefault();
            if (result != null)
            {
                return result;
            }

            // TODO (tomat): which assembly should we look at for dd attributes?
            var type = member as TypeInfo;
            if (type != null)
            {
                foreach (DebuggerDisplayAttribute attr in type.Assembly.GetCustomAttributes<DebuggerDisplayAttribute>())
                {
                    if (IsApplicableAttribute(type, attr.Target.GetTypeInfo(), attr.TargetTypeName))
                    {
                        return attr;
                    }
                }
            }

            return null;
        }

        private static DebuggerTypeProxyAttribute GetApplicableDebuggerTypeProxyAttribute(TypeInfo type)
        {
            // includes inherited attributes. The debugger uses the first attribute if multiple are applied.
            var result = type.GetCustomAttributes<DebuggerTypeProxyAttribute>().FirstOrDefault();
            if (result != null)
            {
                return result;
            }

            // TODO (tomat): which assembly should we look at for proxy attributes?
            foreach (DebuggerTypeProxyAttribute attr in type.Assembly.GetCustomAttributes<DebuggerTypeProxyAttribute>())
            {
                if (IsApplicableAttribute(type, attr.Target.GetTypeInfo(), attr.TargetTypeName))
                {
                    return attr;
                }
            }

            return null;
        }

        private static bool IsApplicableAttribute(TypeInfo type, TypeInfo targetType, string targetTypeName)
        {
            return type != null && AreEquivalent(targetType, type)
                || targetTypeName != null && type.FullName == targetTypeName;
        }

        private static bool AreEquivalent(TypeInfo type, TypeInfo other)
        {
            // TODO: Unify NoPIA interfaces
            // https://github.com/dotnet/corefx/issues/2101
            return type.Equals(other);
        }

        internal static object GetDebuggerTypeProxy(object obj)
        {
            // use proxy type if defined:
            var type = obj.GetType().GetTypeInfo();
            var debuggerTypeProxy = GetApplicableDebuggerTypeProxyAttribute(type);
            if (debuggerTypeProxy != null)
            {
                try
                {
                    var proxyType = Type.GetType(debuggerTypeProxy.ProxyTypeName, throwOnError: false, ignoreCase: false);
                    if (proxyType != null)
                    {
                        if (proxyType.GetTypeInfo().IsGenericTypeDefinition)
                        {
                            proxyType = proxyType.MakeGenericType(type.GenericTypeArguments);
                        }

                        return Activator.CreateInstance(proxyType, new object[] { obj });
                    }
                }
                catch (Exception)
                {
                    // no-op, ignore proxy if it is implemented incorrectly or can't be loaded
                }
            }

            return null;
        }

        internal static MemberInfo ResolveMember(object obj, string memberName, bool callableOnly)
        {
            TypeInfo type = obj.GetType().GetTypeInfo();

            // case-sensitive:
            TypeInfo currentType = type;
            while (true)
            {
                if (!callableOnly)
                {
                    var field = currentType.GetDeclaredField(memberName);
                    if (field != null)
                    {
                        return field;
                    }

                    var property = currentType.GetDeclaredProperty(memberName);
                    if (property != null)
                    {
                        var getter = property.GetMethod;
                        if (getter != null)
                        {
                            return getter;
                        }
                    }
                }

                var method = currentType.GetDeclaredMethod(memberName, Type.EmptyTypes);
                if (method != null)
                {
                    return method;
                }

                if (currentType.BaseType == null)
                {
                    break;
                }

                currentType = currentType.BaseType.GetTypeInfo();
            }

            // case-insensitive:
            currentType = type;
            while (true)
            {
                IEnumerable<MemberInfo> members;
                if (callableOnly)
                {
                    members = type.DeclaredMethods;
                }
                else
                {
                    members = ((IEnumerable<MemberInfo>)type.DeclaredFields).Concat(type.DeclaredProperties);
                }

                MemberInfo candidate = null;
                foreach (var member in members)
                {
                    if (StringComparer.OrdinalIgnoreCase.Equals(memberName, member.Name))
                    {
                        if (candidate != null)
                        {
                            return null;
                        }

                        MethodInfo method;

                        if (member is FieldInfo)
                        {
                            candidate = member;
                        }
                        else if ((method = member as MethodInfo) != null)
                        {
                            if (method.GetParameters().Length == 0)
                            {
                                candidate = member;
                            }
                        }
                        else
                        {
                            var getter = ((PropertyInfo)member).GetMethod;
                            if (getter?.GetParameters().Length == 0)
                            {
                                candidate = member;
                            }
                        }
                    }
                }

                if (candidate != null)
                {
                    return candidate;
                }

                if (currentType.BaseType == null)
                {
                    break;
                }

                currentType = currentType.BaseType.GetTypeInfo();
            }

            return null;
        }

        internal static object GetMemberValue(MemberInfo member, object obj, out Exception exception)
        {
            exception = null;

            try
            {
                FieldInfo field;
                MethodInfo method;

                if ((field = member as FieldInfo) != null)
                {
                    return field.GetValue(obj);
                }

                if ((method = member as MethodInfo) != null)
                {
                    return (method.ReturnType == typeof(void)) ? VoidValue : method.Invoke(obj, SpecializedCollections.EmptyObjects);
                }

                var property = (PropertyInfo)member;
                if (property.GetMethod == null)
                {
                    return null;
                }

                return property.GetValue(obj, SpecializedCollections.EmptyObjects);
            }
            catch (TargetInvocationException e)
            {
                exception = e.InnerException;
            }

            return null;
        }

        internal static SpecialType GetPrimitiveSpecialType(Type type)
        {
            Debug.Assert(type != null);

            if (type == typeof(int))
            {
                return SpecialType.System_Int32;
            }

            if (type == typeof(string))
            {
                return SpecialType.System_String;
            }

            if (type == typeof(bool))
            {
                return SpecialType.System_Boolean;
            }

            if (type == typeof(char))
            {
                return SpecialType.System_Char;
            }

            if (type == typeof(long))
            {
                return SpecialType.System_Int64;
            }

            if (type == typeof(double))
            {
                return SpecialType.System_Double;
            }

            if (type == typeof(byte))
            {
                return SpecialType.System_Byte;
            }

            if (type == typeof(decimal))
            {
                return SpecialType.System_Decimal;
            }

            if (type == typeof(uint))
            {
                return SpecialType.System_UInt32;
            }

            if (type == typeof(ulong))
            {
                return SpecialType.System_UInt64;
            }

            if (type == typeof(float))
            {
                return SpecialType.System_Single;
            }

            if (type == typeof(short))
            {
                return SpecialType.System_Int16;
            }

            if (type == typeof(ushort))
            {
                return SpecialType.System_UInt16;
            }

            if (type == typeof(DateTime))
            {
                return SpecialType.System_DateTime;
            }

            if (type == typeof(sbyte))
            {
                return SpecialType.System_SByte;
            }

            if (type == typeof(object))
            {
                return SpecialType.System_Object;
            }

            if (type == typeof(void))
            {
                return SpecialType.System_Void;
            }

            return SpecialType.None;
        }

        internal static ObjectDisplayOptions GetObjectDisplayOptions(bool useQuotes = false, bool escapeNonPrintable = false, bool includeCodePoints = false, int numberRadix = NumberRadixDecimal)
        {
            var options = ObjectDisplayOptions.None;

            if (useQuotes)
            {
                options |= ObjectDisplayOptions.UseQuotes;
            }

            if (escapeNonPrintable)
            {
                options |= ObjectDisplayOptions.EscapeNonPrintableCharacters;
            }

            if (includeCodePoints)
            {
                options |= ObjectDisplayOptions.IncludeCodePoints;
            }

            switch (numberRadix)
            {
                case NumberRadixDecimal:
                    break;
                case NumberRadixHexadecimal:
                    options |= ObjectDisplayOptions.UseHexadecimalNumbers;
                    break;
                default:
                    // If we ever support a radix other than decimal or hex, we'll
                    // need to propagate the numeric (vs boolean) option down to
                    // ObjectDisplay.
                    throw new ArgumentNullException(nameof(numberRadix));
            }

            return options;
        }

        // Parses
        // <clr-member-name>
        // <clr-member-name> ',' 'nq'
        // <clr-member-name> '(' ')' 
        // <clr-member-name> '(' ')' ',' 'nq'
        internal static string ParseSimpleMemberName(string str, int start, int end, out bool noQuotes, out bool isCallable)
        {
            Debug.Assert(str != null && start >= 0 && end >= start);

            isCallable = false;
            noQuotes = false;

            // no-quotes suffix:
            if (end - 3 >= start && str[end - 2] == 'n' && str[end - 1] == 'q')
            {
                int j = end - 3;
                while (j >= start && Char.IsWhiteSpace(str[j]))
                {
                    j--;
                }

                if (j >= start && str[j] == ',')
                {
                    noQuotes = true;
                    end = j;
                }
            }

            int i = end - 1;
            EatTrailingWhiteSpace(str, start, ref i);
            if (i > start && str[i] == ')')
            {
                int closingParen = i;
                i--;
                EatTrailingWhiteSpace(str, start, ref i);
                if (str[i] != '(')
                {
                    i = closingParen;
                }
                else
                {
                    i--;
                    EatTrailingWhiteSpace(str, start, ref i);
                    isCallable = true;
                }
            }

            EatLeadingWhiteSpace(str, ref start, i);

            return str.Substring(start, i - start + 1);
        }

        private static void EatTrailingWhiteSpace(string str, int start, ref int i)
        {
            while (i >= start && Char.IsWhiteSpace(str[i]))
            {
                i--;
            }
        }

        private static void EatLeadingWhiteSpace(string str, ref int i, int end)
        {
            while (i < end && Char.IsWhiteSpace(str[i]))
            {
                i++;
            }
        }
    }
}
