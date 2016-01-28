// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    using TypeInfo = System.Reflection.TypeInfo;

    /// <summary>
    /// Object pretty printer.
    /// </summary>
    public abstract partial class ObjectFormatter
    {
        internal ObjectFormatter()
        {
        }

        public string FormatObject(object obj)
        {
            return new Formatter(this, null).FormatObject(obj);
        }

        internal string FormatObject(object obj, ObjectFormattingOptions options)
        {
            return new Formatter(this, options).FormatObject(obj);
        }

        #region Reflection Helpers

        private static bool HasOverriddenToString(TypeInfo type)
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

        private static DebuggerDisplayAttribute GetApplicableDebuggerDisplayAttribute(MemberInfo member)
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

        private static object GetDebuggerTypeProxy(object obj)
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

        private static MemberInfo ResolveMember(object obj, string memberName, bool callableOnly)
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

        private static object GetMemberValue(MemberInfo member, object obj, out Exception exception)
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
                    return (method.ReturnType == typeof(void)) ? s_voidValue : method.Invoke(obj, SpecializedCollections.EmptyObjects);
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

        private static readonly object s_voidValue = new object();

        #endregion

        #region String Builder Helpers

        private sealed class Builder
        {
            private readonly ObjectFormattingOptions _options;
            private readonly StringBuilder _sb;
            private readonly int _lineLengthLimit;
            private readonly int _lengthLimit;
            private readonly bool _insertEllipsis;
            private int _currentLimit;

            public Builder(int lengthLimit, ObjectFormattingOptions options, bool insertEllipsis)
            {
                Debug.Assert(lengthLimit <= options.MaxOutputLength);

                int lineLengthLimit = options.MaxLineLength;
                if (insertEllipsis)
                {
                    lengthLimit = Math.Max(0, lengthLimit - options.Ellipsis.Length - 1);
                    lineLengthLimit = Math.Max(0, lineLengthLimit - options.Ellipsis.Length - 1);
                }

                _lengthLimit = lengthLimit;
                _lineLengthLimit = lineLengthLimit;
                _currentLimit = Math.Min(lineLengthLimit, lengthLimit);
                _insertEllipsis = insertEllipsis;

                _options = options;
                _sb = new StringBuilder();
            }

            public bool LimitReached
            {
                get { return _sb.Length == _lengthLimit; }
            }

            public int Remaining
            {
                get { return _lengthLimit - _sb.Length; }
            }

            // can be negative (the min value is -Ellipsis.Length - 1)
            private int CurrentRemaining
            {
                get { return _currentLimit - _sb.Length; }
            }

            public void AppendLine()
            {
                // remove line length limit so that we can insert a new line even
                // if the previous one hit maxed out the line limit:
                _currentLimit = _lengthLimit;

                Append(_options.NewLine);

                // recalc limit for the next line:
                _currentLimit = (int)Math.Min((long)_sb.Length + _lineLengthLimit, _lengthLimit);
            }

            private void AppendEllipsis()
            {
                if (_sb.Length > 0 && _sb[_sb.Length - 1] != ' ')
                {
                    _sb.Append(' ');
                }

                _sb.Append(_options.Ellipsis);
            }

            public void Append(char c, int count = 1)
            {
                if (CurrentRemaining < 0)
                {
                    return;
                }

                int length = Math.Min(count, CurrentRemaining);

                _sb.Append(c, length);

                if (_insertEllipsis && length < count)
                {
                    AppendEllipsis();
                }
            }

            public void Append(string str, int start = 0, int count = Int32.MaxValue)
            {
                if (str == null || CurrentRemaining < 0)
                {
                    return;
                }

                count = Math.Min(count, str.Length - start);
                int length = Math.Min(count, CurrentRemaining);
                _sb.Append(str, start, length);

                if (_insertEllipsis && length < count)
                {
                    AppendEllipsis();
                }
            }

            public void AppendFormat(string format, params object[] args)
            {
                Append(String.Format(format, args));
            }

            public void AppendGroupOpening()
            {
                Append('{');
            }

            public void AppendGroupClosing(bool inline)
            {
                if (inline)
                {
                    Append(" }");
                }
                else
                {
                    AppendLine();
                    Append('}');
                    AppendLine();
                }
            }

            public void AppendCollectionItemSeparator(bool isFirst, bool inline)
            {
                if (isFirst)
                {
                    if (inline)
                    {
                        Append(' ');
                    }
                    else
                    {
                        AppendLine();
                    }
                }
                else
                {
                    if (inline)
                    {
                        Append(", ");
                    }
                    else
                    {
                        Append(',');
                        AppendLine();
                    }
                }

                if (!inline)
                {
                    Append(_options.MemberIndentation);
                }
            }

            internal void AppendInfiniteRecursionMarker()
            {
                AppendGroupOpening();
                AppendCollectionItemSeparator(isFirst: true, inline: true);
                Append(_options.Ellipsis);
                AppendGroupClosing(inline: true);
            }

            public override string ToString()
            {
                return _sb.ToString();
            }
        }

        #endregion

        #region Language Specific Formatting

        /// <summary>
        /// String that describes "void" return type in the language.
        /// </summary>
        internal abstract object VoidDisplayString { get; }

        /// <summary>
        /// String that describes "null" literal in the language.
        /// </summary>
        internal abstract string NullLiteral { get; }

        internal abstract string FormatLiteral(bool value);
        internal abstract string FormatLiteral(string value, bool quote, bool useHexadecimalNumbers = false);
        internal abstract string FormatLiteral(char value, bool quote, bool includeCodePoints = false, bool useHexadecimalNumbers = false);
        internal abstract string FormatLiteral(sbyte value, bool useHexadecimalNumbers = false);
        internal abstract string FormatLiteral(byte value, bool useHexadecimalNumbers = false);
        internal abstract string FormatLiteral(short value, bool useHexadecimalNumbers = false);
        internal abstract string FormatLiteral(ushort value, bool useHexadecimalNumbers = false);
        internal abstract string FormatLiteral(int value, bool useHexadecimalNumbers = false);
        internal abstract string FormatLiteral(uint value, bool useHexadecimalNumbers = false);
        internal abstract string FormatLiteral(long value, bool useHexadecimalNumbers = false);
        internal abstract string FormatLiteral(ulong value, bool useHexadecimalNumbers = false);
        internal abstract string FormatLiteral(double value);
        internal abstract string FormatLiteral(float value);
        internal abstract string FormatLiteral(decimal value);
        internal abstract string FormatLiteral(DateTime value);

        // TODO (tomat): Use DebuggerDisplay.Type if specified?
        internal abstract string FormatGeneratedTypeName(Type type);
        internal abstract string FormatMemberName(MemberInfo member);
        internal abstract string GetPrimitiveTypeName(SpecialType type);

        /// <summary>
        /// Returns a method signature display string. Used to display stack frames.
        /// </summary>
        /// <returns>Null if the method is a compiler generated method that shouldn't be displayed to the user.</returns>
        internal virtual string FormatMethodSignature(MethodBase method)
        {
            // TODO: https://github.com/dotnet/roslyn/issues/5250

            if (method.Name.IndexOfAny(s_generatedNameChars) >= 0 ||
                method.DeclaringType.Name.IndexOfAny(s_generatedNameChars) >= 0 ||
                method.GetCustomAttributes<DebuggerHiddenAttribute>().Any() ||
                method.DeclaringType.GetTypeInfo().GetCustomAttributes<DebuggerHiddenAttribute>().Any())
            {
                return null;
            }

            return $"{method.DeclaringType.ToString()}.{method.Name}({string.Join(", ", method.GetParameters().Select(p => p.ToString()))})";
        }

        private static readonly char[] s_generatedNameChars = { '$', '<' };

        internal abstract string GenericParameterOpening { get; }
        internal abstract string GenericParameterClosing { get; }

        /// <summary>
        /// Formats an array type name (vector or multidimensional).
        /// </summary>
        internal abstract string FormatArrayTypeName(Type arrayType, Array arrayOpt, ObjectFormattingOptions options);

        /// <summary>
        /// Returns true if the member shouldn't be displayed (e.g. it's a compiler generated field).
        /// </summary>
        internal virtual bool IsHiddenMember(MemberInfo member) => false;

        internal static CultureInfo UIFormatCulture => CultureInfo.CurrentUICulture;

        internal static ObjectDisplayOptions GetObjectDisplayOptions(bool useHexadecimalNumbers)
        {
            return useHexadecimalNumbers
                ? ObjectDisplayOptions.UseHexadecimalNumbers
                : ObjectDisplayOptions.None;
        }

        /// <summary>
        /// Returns null if the type is not considered primitive in the target language.
        /// </summary>
        private string FormatPrimitive(object obj, bool quoteStrings, bool includeCodePoints, bool useHexadecimalNumbers)
        {
            if (ReferenceEquals(obj, s_voidValue))
            {
                return string.Empty;
            }

            if (obj == null)
            {
                return NullLiteral;
            }

            var type = obj.GetType();

            if (type.GetTypeInfo().IsEnum)
            {
                return obj.ToString();
            }

            switch (GetPrimitiveSpecialType(type))
            {
                case SpecialType.System_Int32:
                    return FormatLiteral((int)obj, useHexadecimalNumbers);

                case SpecialType.System_String:
                    return FormatLiteral((string)obj, quoteStrings, useHexadecimalNumbers);

                case SpecialType.System_Boolean:
                    return FormatLiteral((bool)obj);

                case SpecialType.System_Char:
                    return FormatLiteral((char)obj, quoteStrings, includeCodePoints, useHexadecimalNumbers);

                case SpecialType.System_Int64:
                    return FormatLiteral((long)obj, useHexadecimalNumbers);

                case SpecialType.System_Double:
                    return FormatLiteral((double)obj);

                case SpecialType.System_Byte:
                    return FormatLiteral((byte)obj, useHexadecimalNumbers);

                case SpecialType.System_Decimal:
                    return FormatLiteral((decimal)obj);

                case SpecialType.System_UInt32:
                    return FormatLiteral((uint)obj, useHexadecimalNumbers);

                case SpecialType.System_UInt64:
                    return FormatLiteral((ulong)obj, useHexadecimalNumbers);

                case SpecialType.System_Single:
                    return FormatLiteral((float)obj);

                case SpecialType.System_Int16:
                    return FormatLiteral((short)obj, useHexadecimalNumbers);

                case SpecialType.System_UInt16:
                    return FormatLiteral((ushort)obj, useHexadecimalNumbers);

                case SpecialType.System_DateTime:
                    return FormatLiteral((DateTime)obj);

                case SpecialType.System_SByte:
                    return FormatLiteral((sbyte)obj, useHexadecimalNumbers);

                case SpecialType.System_Object:
                case SpecialType.System_Void:
                case SpecialType.None:
                    return null;

                default:
                    throw ExceptionUtilities.Unreachable;
            }
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

        internal string FormatTypeName(Type type, ObjectFormattingOptions options)
        {
            string result = GetPrimitiveTypeName(GetPrimitiveSpecialType(type));
            if (result != null)
            {
                return result;
            }

            result = FormatGeneratedTypeName(type);
            if (result != null)
            {
                return result;
            }

            if (type.IsArray)
            {
                return FormatArrayTypeName(type, arrayOpt: null, options: options);
            }

            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsGenericType)
            {
                return FormatGenericTypeName(typeInfo, options);
            }

            if (typeInfo.DeclaringType != null)
            {
                return typeInfo.Name.Replace('+', '.');
            }

            return typeInfo.Name;
        }

        private string FormatGenericTypeName(TypeInfo typeInfo, ObjectFormattingOptions options)
        {
            var pooledBuilder = PooledStringBuilder.GetInstance();
            var builder = pooledBuilder.Builder;

            // consolidated generic arguments (includes arguments of all declaring types):
            Type[] genericArguments = typeInfo.GenericTypeArguments;

            if (typeInfo.DeclaringType != null)
            {
                var nestedTypes = ArrayBuilder<TypeInfo>.GetInstance();
                do
                {
                    nestedTypes.Add(typeInfo);
                    typeInfo = typeInfo.DeclaringType?.GetTypeInfo();
                }
                while (typeInfo != null);

                int typeArgumentIndex = 0;
                for (int i = nestedTypes.Count - 1; i >= 0; i--)
                {
                    AppendTypeInstantiation(builder, nestedTypes[i], genericArguments, ref typeArgumentIndex, options);
                    if (i > 0)
                    {
                        builder.Append('.');
                    }
                }

                nestedTypes.Free();
            }
            else
            {
                int typeArgumentIndex = 0;
                AppendTypeInstantiation(builder, typeInfo, genericArguments, ref typeArgumentIndex, options);
            }

            return pooledBuilder.ToStringAndFree();
        }

        private void AppendTypeInstantiation(StringBuilder builder, TypeInfo typeInfo, Type[] genericArguments, ref int genericArgIndex, ObjectFormattingOptions options)
        {
            // generic arguments of all the outer types and the current type;
            int currentArgCount = (typeInfo.IsGenericTypeDefinition ? typeInfo.GenericTypeParameters.Length : typeInfo.GenericTypeArguments.Length) - genericArgIndex;

            if (currentArgCount > 0)
            {
                string name = typeInfo.Name;

                int backtick = name.IndexOf('`');
                if (backtick > 0)
                {
                    builder.Append(name.Substring(0, backtick));
                }
                else
                {
                    builder.Append(name);
                }

                builder.Append(GenericParameterOpening);

                for (int i = 0; i < currentArgCount; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(", ");
                    }
                    builder.Append(FormatTypeName(genericArguments[genericArgIndex++], options));
                }

                builder.Append(GenericParameterClosing);
            }
            else
            {
                builder.Append(typeInfo.Name);
            }
        }

        #endregion
    }
}
