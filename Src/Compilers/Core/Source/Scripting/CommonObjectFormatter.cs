using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Roslyn.Utilities;
using Ref = System.Reflection;

namespace Roslyn.Scripting
{
    /// <summary>
    /// Object pretty printer.
    /// </summary>
    public abstract partial class CommonObjectFormatter
    {
        protected CommonObjectFormatter()
        {
        }

        public string FormatObject(object obj, ObjectFormattingOptions options = null)
        {
            return new Formatter(this, options).FormatObject(obj);
        }

        #region Reflection Helpers

        private static bool HasOverriddenToString(Type type)
        {
            Ref.MethodInfo method = type.GetMethod(
                "ToString",
                Ref.BindingFlags.Public | Ref.BindingFlags.Instance,
                null,
                Type.EmptyTypes,
                null
            );
            return method.DeclaringType != typeof(object);
        }

        private static DebuggerDisplayAttribute GetApplicableDebuggerDisplayAttribute(Ref.MemberInfo member)
        {
            var result = (DebuggerDisplayAttribute)member.GetCustomAttributes(typeof(DebuggerDisplayAttribute), inherit: true).FirstOrDefault();
            if (result != null)
            {
                return result;
            }

            // TODO (tomat): which assembly should we look at for dd attributes?
            Type type = member as Type;
            if (type != null)
            {
                foreach (DebuggerDisplayAttribute attr in type.Assembly.GetCustomAttributes(typeof(DebuggerDisplayAttribute), inherit: true))
                {
                    if (IsApplicableAttribute(type, attr.Target, attr.TargetTypeName))
                    {
                        return attr;
                    }
                }
            }
            return null;
        }

        private static DebuggerTypeProxyAttribute GetApplicableDebuggerTypeProxyAttribute(Type type)
        {
            var result = (DebuggerTypeProxyAttribute)type.GetCustomAttributes(typeof(DebuggerTypeProxyAttribute), inherit: true).FirstOrDefault();
            if (result != null)
            {
                return result;
            }

            // TODO (tomat): which assembly should we look at for proxy attributes?
            foreach (DebuggerTypeProxyAttribute attr in type.Assembly.GetCustomAttributes(typeof(DebuggerTypeProxyAttribute), inherit: true))
            {
                if (IsApplicableAttribute(type, attr.Target, attr.TargetTypeName))
                {
                    return attr;
                }
            }

            return null;
        }

        private static bool IsApplicableAttribute(Type type, Type targetType, string targetTypeName)
        {
            return type != null && targetType.IsEquivalentTo(type)
                || targetTypeName != null && type.FullName == targetTypeName;
        }

        private static object GetDebuggerTypeProxy(object obj)
        {
            // use proxy type if defined:
            Type type = obj.GetType();
            var debuggerTypeProxy = GetApplicableDebuggerTypeProxyAttribute(type);
            if (debuggerTypeProxy != null)
            {
                var proxyType = Type.GetType(debuggerTypeProxy.ProxyTypeName, false, false);
                if (proxyType != null)
                {
                    try
                    {
                        if (proxyType.IsGenericTypeDefinition)
                        {
                            proxyType = proxyType.MakeGenericType(type.GetGenericArguments());
                        }

                        return Activator.CreateInstance(
                            proxyType,
                            Ref.BindingFlags.Instance | Ref.BindingFlags.NonPublic | Ref.BindingFlags.Public,
                            null,
                            new object[] { obj },
                            null,
                            null
                        );
                    }
                    catch (Exception)
                    {
                        // no-op, ignore proxy if it is implemented incorrectly
                    }
                }
            }

            return null;
        }

        private static Ref.MemberInfo ResolveMember(object obj, string memberName, bool callableOnly)
        {
            Type type = obj.GetType();
            const Ref.BindingFlags flags = Ref.BindingFlags.Instance | Ref.BindingFlags.Static | Ref.BindingFlags.Public | Ref.BindingFlags.NonPublic |
                Ref.BindingFlags.DeclaredOnly;

            // case-sensitive:
            Type currentType = type;
            while (currentType != null)
            {
                if (!callableOnly)
                {
                    var field = currentType.GetField(memberName, flags);
                    if (field != null)
                    {
                        return field;
                    }

                    var property = currentType.GetProperty(memberName, flags, null, null, Type.EmptyTypes, null);
                    if (property != null)
                    {
                        var getter = property.GetGetMethod(nonPublic: true);
                        if (getter != null)
                        {
                            return getter;
                        }
                    }
                }

                var method = currentType.GetMethod(memberName, flags, null, Type.EmptyTypes, null);
                if (method != null)
                {
                    return method;
                }

                currentType = currentType.BaseType;
            }

            // case-insensitive:
            currentType = type;
            while (currentType != null)
            {
                IEnumerable<Ref.MemberInfo> members;
                if (callableOnly)
                {
                    members = type.GetMethods(flags);
                }
                else
                {
                    members = ((IEnumerable<Ref.MemberInfo>)type.GetFields(flags)).Concat(type.GetProperties(flags));
                }

                Ref.MemberInfo candidate = null;
                foreach (var member in members)
                {
                    if (StringComparer.OrdinalIgnoreCase.Equals(memberName, member.Name))
                    {
                        if (candidate == null)
                        {
                            switch (member.MemberType)
                            {
                                case Ref.MemberTypes.Field:
                                    candidate = member;
                                    break;

                                case Ref.MemberTypes.Method:
                                    if (((Ref.MethodInfo)member).GetParameters().Length == 0)
                                    {
                                        candidate = member;
                                    }

                                    break;

                                case Ref.MemberTypes.Property:
                                    var getter = ((Ref.PropertyInfo)member).GetGetMethod(nonPublic: true);
                                    if (getter != null && getter.GetParameters().Length == 0)
                                    {
                                        candidate = member;
                                    }
                                    break;

                                default:
                                    throw Contract.Unreachable;
                            }
                        }
                        else
                        {
                            return null;
                        }
                    }
                }

                if (candidate != null)
                {
                    return candidate;
                }
                currentType = currentType.BaseType;
            }

            return null;
        }

        private static object GetMemberValue(Ref.MemberInfo member, object obj, out Exception exception)
        {
            exception = null;
            try
            {
                switch (member.MemberType)
                {
                    case Ref.MemberTypes.Field:
                        var field = (Ref.FieldInfo)member;
                        return field.GetValue(obj);

                    case Ref.MemberTypes.Method:
                        var method = (Ref.MethodInfo)member;
                        return (method.ReturnType == typeof(void)) ? VoidValue : method.Invoke(obj, SpecializedCollections.EmptyObjects);

                    case Ref.MemberTypes.Property:
                        var property = (Ref.PropertyInfo)member;
                        return property.GetValue(obj, SpecializedCollections.EmptyObjects);

                    default:
                        throw Contract.Unreachable;
                }
            }
            catch (Ref.TargetInvocationException e)
            {
                exception = e.InnerException;
            }
            return null;
        }

        private static readonly object VoidValue = new object();

        #endregion

        #region String Builder Helpers

        private sealed class Builder
        {
            private readonly ObjectFormattingOptions options;
            private readonly StringBuilder sb;
            private readonly int lineLengthLimit;
            private readonly int lengthLimit;
            private readonly bool insertEllipsis;
            private int currentLimit;

            public Builder(int lengthLimit, ObjectFormattingOptions options, bool insertEllipsis)
            {
                Debug.Assert(lengthLimit <= options.MaxOutputLength);

                int lineLengthLimit = options.MaxLineLength;
                if (insertEllipsis)
                {
                    lengthLimit = Math.Max(0, lengthLimit - options.Ellipsis.Length - 1);
                    lineLengthLimit = Math.Max(0, lineLengthLimit - options.Ellipsis.Length - 1);
                }

                this.lengthLimit = lengthLimit;
                this.lineLengthLimit = lineLengthLimit;
                this.currentLimit = Math.Min(lineLengthLimit, lengthLimit);
                this.insertEllipsis = insertEllipsis;

                this.options = options;
                this.sb = new StringBuilder();
            }

            public bool LimitReached
            {
                get { return sb.Length == lengthLimit; }
            }

            public int Remaining
            {
                get { return lengthLimit - sb.Length; }
            }

            // can be negative (the min value is -Ellipsis.Length - 1)
            private int CurrentRemaining
            {
                get { return currentLimit - sb.Length; }
            }

            public void AppendLine()
            {
                // remove line length limit so that we can insert a new line even 
                // if the previous one hit maxed out the line limit:
                currentLimit = lengthLimit;

                Append(options.NewLine);

                // recalc limit for the next line:
                currentLimit = (int)Math.Min((long)sb.Length + lineLengthLimit, lengthLimit);
            }

            private void AppendEllipsis()
            {
                if (sb.Length > 0 && sb[sb.Length - 1] != ' ')
                {
                    sb.Append(' ');
                }

                sb.Append(options.Ellipsis);
            }

            public void Append(char c, int count = 1)
            {
                if (CurrentRemaining < 0)
                {
                    return;
                }

                int length = Math.Min(count, CurrentRemaining);

                sb.Append(c, length);

                if (insertEllipsis && length < count)
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
                sb.Append(str, start, length);

                if (insertEllipsis && length < count)
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
                    Append(options.MemberIndentation);
                }
            }

            internal void AppendInfiniteRecursionMarker()
            {
                AppendGroupOpening();
                AppendCollectionItemSeparator(isFirst: true, inline: true);
                Append(options.Ellipsis);
                AppendGroupClosing(inline: true);
            }

            public override string ToString()
            {
                return sb.ToString();
            }
        }

        #endregion

        #region Language Specific Formatting

        private string FormatPrimitive(object obj, bool quoteStrings, bool useHexadecimalNumbers, bool includeCodePoints)
        {
            if (ReferenceEquals(obj, VoidValue))
            {
                return String.Empty;
            }

            if (obj == null)
            {
                return NullLiteral;
            }

            if (obj is bool)
            {
                return FormatLiteral((bool)obj);
            }

            string str;
            if ((str = obj as string) != null)
            {
                return FormatLiteral(str, quoteStrings, useHexadecimalNumbers);
            }

            if (obj is char)
            {
                return FormatLiteral((char)obj, quoteStrings, useHexadecimalNumbers, includeCodePoints);
            }

            if (obj is sbyte)
            {
                return FormatLiteral((sbyte)obj, useHexadecimalNumbers);
            }

            if (obj is byte)
            {
                return FormatLiteral((byte)obj, useHexadecimalNumbers);
            }

            if (obj is short)
            {
                return FormatLiteral((short)obj, useHexadecimalNumbers);
            }

            if (obj is ushort)
            {
                return FormatLiteral((ushort)obj, useHexadecimalNumbers);
            }

            if (obj is int)
            {
                return FormatLiteral((int)obj, useHexadecimalNumbers);
            }

            if (obj is uint)
            {
                return FormatLiteral((uint)obj, useHexadecimalNumbers);
            }

            if (obj is long)
            {
                return FormatLiteral((long)obj, useHexadecimalNumbers);
            }

            if (obj is ulong)
            {
                return FormatLiteral((ulong)obj, useHexadecimalNumbers);
            }

            if (obj is double)
            {
                return FormatLiteral((double)obj);
            }

            if (obj is float)
            {
                return FormatLiteral((float)obj);
            }

            if (obj is decimal)
            {
                return FormatLiteral((decimal)obj);
            }

            if (obj.GetType().IsEnum)
            {
                return obj.ToString();
            }

            return null;
        }

        /// <summary>
        /// String that describes "void" return type in the language.
        /// </summary>
        public abstract object VoidDisplayString { get; }

        /// <summary>
        /// String that describes "null" literal in the language.
        /// </summary>
        public abstract string NullLiteral { get; }

        public abstract string FormatLiteral(bool value);
        public abstract string FormatLiteral(string value, bool quote, bool useHexadecimalNumbers = false);
        public abstract string FormatLiteral(char value, bool quote, bool useHexadecimalNumbers = false, bool includeCodePoints = false);
        public abstract string FormatLiteral(sbyte value, bool useHexadecimalNumbers = false);
        public abstract string FormatLiteral(byte value, bool useHexadecimalNumbers = false);
        public abstract string FormatLiteral(short value, bool useHexadecimalNumbers = false);
        public abstract string FormatLiteral(ushort value, bool useHexadecimalNumbers = false);
        public abstract string FormatLiteral(int value, bool useHexadecimalNumbers = false);
        public abstract string FormatLiteral(uint value, bool useHexadecimalNumbers = false);
        public abstract string FormatLiteral(long value, bool useHexadecimalNumbers = false);
        public abstract string FormatLiteral(ulong value, bool useHexadecimalNumbers = false);
        public abstract string FormatLiteral(double value);
        public abstract string FormatLiteral(float value);
        public abstract string FormatLiteral(decimal value);

        // TODO (tomat): Use DebuggerDisplay.Type if specified?
        public abstract string FormatTypeName(Type type, ObjectFormattingOptions options);
        public abstract string FormatMemberName(Ref.MemberInfo member);

        /// <summary>
        /// Formats an array type name (vector or multidimensional).
        /// </summary>
        public abstract string FormatArrayTypeName(Array array, ObjectFormattingOptions options);

        /// <summary>
        /// Returns true if the member shouldn't be displayed (e.g. it's a compiler generated field).
        /// </summary>
        public virtual bool IsHiddenMember(Ref.MemberInfo member)
        {
            return false;
        }

        #endregion
    }
}
