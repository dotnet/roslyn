// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Cci;
using Roslyn.Utilities;
using ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    using static ObjectFormatterHelpers;
    using TypeInfo = System.Reflection.TypeInfo;

    internal abstract partial class CommonObjectFormatter
    {
        private sealed partial class Visitor
        {
            private readonly CommonObjectFormatter _formatter;

            private readonly BuilderOptions _builderOptions;
            private CommonPrimitiveFormatterOptions _primitiveOptions;
            private readonly CommonTypeNameFormatterOptions _typeNameOptions;
            private MemberDisplayFormat _memberDisplayFormat;

            private HashSet<object> _lazyVisitedObjects;

            private HashSet<object> VisitedObjects
            {
                get
                {
                    _lazyVisitedObjects ??= new HashSet<object>(ReferenceEqualityComparer.Instance);

                    return _lazyVisitedObjects;
                }
            }

            public Visitor(
                CommonObjectFormatter formatter,
                BuilderOptions builderOptions,
                CommonPrimitiveFormatterOptions primitiveOptions,
                CommonTypeNameFormatterOptions typeNameOptions,
                MemberDisplayFormat memberDisplayFormat)
            {
                _formatter = formatter;
                _builderOptions = builderOptions;
                _primitiveOptions = primitiveOptions;
                _typeNameOptions = typeNameOptions;
                _memberDisplayFormat = memberDisplayFormat;
            }

            private Builder MakeMemberBuilder(int limit)
            {
                return new Builder(_builderOptions.WithMaximumOutputLength(Math.Min(_builderOptions.MaximumLineLength, limit)), suppressEllipsis: true);
            }

            public string FormatObject(object obj)
            {
                try
                {
                    var builder = new Builder(_builderOptions, suppressEllipsis: false);
                    string _;
                    return FormatObjectRecursive(builder, obj, isRoot: true, debuggerDisplayName: out _).ToString();
                }
                catch (InsufficientExecutionStackException)
                {
                    return ScriptingResources.StackOverflowWhileEvaluating;
                }
            }

            private Builder FormatObjectRecursive(Builder result, object obj, bool isRoot, out string debuggerDisplayName)
            {
                // TODO (https://github.com/dotnet/roslyn/issues/6689): remove this
                if (!isRoot && _memberDisplayFormat == MemberDisplayFormat.SeparateLines)
                {
                    _memberDisplayFormat = MemberDisplayFormat.SingleLine;
                }

                debuggerDisplayName = null;
                string primitive = _formatter.PrimitiveFormatter.FormatPrimitive(obj, _primitiveOptions);
                if (primitive != null)
                {
                    result.Append(primitive);
                    return result;
                }

                Type type = obj.GetType();
                TypeInfo typeInfo = type.GetTypeInfo();

                //
                // Override KeyValuePair<,>.ToString() to get better dictionary elements formatting:
                //
                // { { format(key), format(value) }, ... }
                // instead of
                // { [key.ToString(), value.ToString()], ... } 
                //
                // This is more general than overriding Dictionary<,> debugger proxy attribute since it applies on all
                // types that return an array of KeyValuePair in their DebuggerDisplay to display items.
                //
                if (typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    if (isRoot)
                    {
                        result.Append(_formatter.TypeNameFormatter.FormatTypeName(type, _typeNameOptions));
                        result.Append(' ');
                    }

                    FormatKeyValuePair(result, obj);
                    return result;
                }

                if (typeInfo.IsArray)
                {
                    if (VisitedObjects.Add(obj))
                    {
                        FormatArray(result, (Array)obj);

                        VisitedObjects.Remove(obj);
                    }
                    else
                    {
                        result.AppendInfiniteRecursionMarker();
                    }

                    return result;
                }

                DebuggerDisplayAttribute debuggerDisplay = GetApplicableDebuggerDisplayAttribute(typeInfo);
                if (debuggerDisplay != null)
                {
                    debuggerDisplayName = debuggerDisplay.Name;
                }

                // Suppresses members if inlineMembers is true,
                // does nothing otherwise.
                bool suppressInlineMembers = false;

                //
                // TypeName(count) for ICollection implementers
                // or
                // TypeName([[DebuggerDisplay.Value]])        // Inline
                // [[DebuggerDisplay.Value]]                  // Inline && !isRoot
                // or
                // [[ToString()]] if ToString overridden
                // or
                // TypeName 
                // 
                ICollection collection;
                if ((collection = obj as ICollection) != null)
                {
                    FormatCollectionHeader(result, collection);
                }
                else if (debuggerDisplay != null && !string.IsNullOrEmpty(debuggerDisplay.Value))
                {
                    if (isRoot)
                    {
                        result.Append(_formatter.TypeNameFormatter.FormatTypeName(type, _typeNameOptions));
                        result.Append('(');
                    }

                    FormatWithEmbeddedExpressions(result, debuggerDisplay.Value, obj);

                    if (isRoot)
                    {
                        result.Append(')');
                    }

                    suppressInlineMembers = true;
                }
                else if (HasOverriddenToString(typeInfo))
                {
                    ObjectToString(result, obj);
                    suppressInlineMembers = true;
                }
                else
                {
                    result.Append(_formatter.TypeNameFormatter.FormatTypeName(type, _typeNameOptions));
                }

                MemberDisplayFormat memberFormat = _memberDisplayFormat;

                if (memberFormat == MemberDisplayFormat.Hidden)
                {
                    if (collection != null)
                    {
                        // NB: Collections specifically ignore MemberDisplayFormat.Hidden.
                        memberFormat = MemberDisplayFormat.SingleLine;
                    }
                    else
                    {
                        return result;
                    }
                }

                bool includeNonPublic = memberFormat == MemberDisplayFormat.SeparateLines;
                bool inlineMembers = memberFormat == MemberDisplayFormat.SingleLine;

                object proxy = GetDebuggerTypeProxy(obj);
                if (proxy != null)
                {
                    includeNonPublic = false;
                    suppressInlineMembers = false;
                }

                if (!suppressInlineMembers || !inlineMembers)
                {
                    FormatMembers(result, obj, proxy, includeNonPublic, inlineMembers);
                }

                return result;
            }

            #region Members

            private void FormatMembers(Builder result, object obj, object proxy, bool includeNonPublic, bool inlineMembers)
            {
                // TODO (tomat): we should not use recursion
                RuntimeHelpers.EnsureSufficientExecutionStack();

                result.Append(' ');

                // Note: Even if we've seen it before, we show a header 
                if (!VisitedObjects.Add(obj))
                {
                    result.AppendInfiniteRecursionMarker();
                    return;
                }

                bool membersFormatted = false;

                // handle special types only if a proxy isn't defined
                if (proxy == null)
                {
                    IDictionary dictionary;
                    IEnumerable enumerable;
                    if ((dictionary = obj as IDictionary) != null)
                    {
                        FormatDictionaryMembers(result, dictionary, inlineMembers);
                        membersFormatted = true;
                    }
                    else if ((enumerable = obj as IEnumerable) != null)
                    {
                        FormatSequenceMembers(result, enumerable, inlineMembers);
                        membersFormatted = true;
                    }
                }

                if (!membersFormatted)
                {
                    FormatObjectMembers(result, proxy ?? obj, obj.GetType().GetTypeInfo(), includeNonPublic, inlineMembers);
                }

                VisitedObjects.Remove(obj);
            }

            /// <summary>
            /// Formats object members to a list.
            /// 
            /// Inline == false:
            /// <code>
            /// { A=true, B=false, C=new int[3] { 1, 2, 3 } }
            /// </code>
            /// 
            /// Inline == true:
            /// <code>
            /// {
            ///   A: true,
            ///   B: false,
            ///   C: new int[3] { 1, 2, 3 }
            /// }
            /// </code>
            /// </summary>
            private void FormatObjectMembers(Builder result, object obj, TypeInfo preProxyTypeInfo, bool includeNonPublic, bool inline)
            {
                int lengthLimit = result.Remaining;
                if (lengthLimit < 0)
                {
                    return;
                }

                var members = new List<FormattedMember>();

                // Limits the number of members added into the result. Some more members may be added than it will fit into the result
                // and will be thrown away later but not many more.
                FormatObjectMembersRecursive(members, obj, includeNonPublic, ref lengthLimit);
                bool useCollectionFormat = UseCollectionFormat(members, preProxyTypeInfo);

                result.AppendGroupOpening();

                for (int i = 0; i < members.Count; i++)
                {
                    result.AppendCollectionItemSeparator(isFirst: i == 0, inline: inline);
                    if (useCollectionFormat)
                    {
                        members[i].AppendAsCollectionEntry(result);
                    }
                    else
                    {
                        members[i].Append(result, inline ? "=" : ": ");
                    }

                    if (result.Remaining <= 0)
                    {
                        break;
                    }
                }

                result.AppendGroupClosing(inline);
            }

            private static bool UseCollectionFormat(IEnumerable<FormattedMember> members, TypeInfo originalType)
            {
                return typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(originalType) && members.All(member => member.Index >= 0);
            }

            /// <summary>
            /// Enumerates sorted object members to display.
            /// </summary>
            private void FormatObjectMembersRecursive(List<FormattedMember> result, object obj, bool includeNonPublic, ref int lengthLimit)
            {
                Debug.Assert(obj != null);

                var members = new List<MemberInfo>();

                var type = obj.GetType().GetTypeInfo();
                while (type != null)
                {
                    members.AddRange(type.DeclaredFields.Where(f => !f.IsStatic));
                    members.AddRange(type.DeclaredProperties.Where(f => f.GetMethod != null && !f.GetMethod.IsStatic));
                    type = type.BaseType?.GetTypeInfo();
                }

                members.Sort((x, y) =>
                {
                    // Need case-sensitive comparison here so that the order of members is
                    // always well-defined (members can differ by case only). And we don't want to
                    // depend on that order.
                    int comparisonResult = StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name);
                    if (comparisonResult == 0)
                    {
                        comparisonResult = StringComparer.Ordinal.Compare(x.Name, y.Name);
                    }

                    return comparisonResult;
                });

                foreach (var member in members)
                {
                    if (!_formatter.Filter.Include(member))
                    {
                        continue;
                    }

                    bool rootHidden = false, ignoreVisibility = false;
                    var browsable = (DebuggerBrowsableAttribute)member.GetCustomAttributes(typeof(DebuggerBrowsableAttribute), false).FirstOrDefault();
                    if (browsable != null)
                    {
                        if (browsable.State == DebuggerBrowsableState.Never)
                        {
                            continue;
                        }

                        ignoreVisibility = true;
                        rootHidden = browsable.State == DebuggerBrowsableState.RootHidden;
                    }

                    if (member is FieldInfo field)
                    {
                        if (!(includeNonPublic || ignoreVisibility || field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        PropertyInfo property = (PropertyInfo)member;

                        var getter = property.GetMethod;
                        if (getter == null)
                        {
                            continue;
                        }

                        var setter = property.SetMethod;

                        // If not ignoring visibility include properties that has a visible getter or setter.
                        if (!(includeNonPublic || ignoreVisibility ||
                            getter.IsPublic || getter.IsFamily || getter.IsFamilyOrAssembly ||
                            (setter != null && (setter.IsPublic || setter.IsFamily || setter.IsFamilyOrAssembly))))
                        {
                            continue;
                        }

                        if (getter.GetParameters().Length > 0)
                        {
                            continue;
                        }
                    }

                    var debuggerDisplay = GetApplicableDebuggerDisplayAttribute(member);
                    if (debuggerDisplay != null)
                    {
                        string k = FormatWithEmbeddedExpressions(lengthLimit, debuggerDisplay.Name, obj) ?? member.Name;
                        string v = FormatWithEmbeddedExpressions(lengthLimit, debuggerDisplay.Value, obj) ?? string.Empty; // TODO: ?
                        if (!AddMember(result, new FormattedMember(-1, k, v), ref lengthLimit))
                        {
                            return;
                        }

                        continue;
                    }

                    Exception exception;
                    object value = GetMemberValue(member, obj, out exception);
                    if (exception != null)
                    {
                        var memberValueBuilder = MakeMemberBuilder(lengthLimit);
                        FormatException(memberValueBuilder, exception);
                        if (!AddMember(result, new FormattedMember(-1, member.Name, memberValueBuilder.ToString()), ref lengthLimit))
                        {
                            return;
                        }

                        continue;
                    }

                    if (rootHidden)
                    {
                        if (value != null && !VisitedObjects.Contains(value))
                        {
                            Array array;
                            if ((array = value as Array) != null)  // TODO (tomat): n-dim arrays
                            {
                                int i = 0;
                                foreach (object item in array)
                                {
                                    string name;
                                    Builder valueBuilder = MakeMemberBuilder(lengthLimit);
                                    FormatObjectRecursive(valueBuilder, item, isRoot: false, debuggerDisplayName: out name);

                                    if (!string.IsNullOrEmpty(name))
                                    {
                                        name = FormatWithEmbeddedExpressions(MakeMemberBuilder(lengthLimit), name, item).ToString();
                                    }

                                    if (!AddMember(result, new FormattedMember(i, name, valueBuilder.ToString()), ref lengthLimit))
                                    {
                                        return;
                                    }

                                    i++;
                                }
                            }
                            else if (_formatter.PrimitiveFormatter.FormatPrimitive(value, _primitiveOptions) == null && VisitedObjects.Add(value))
                            {
                                FormatObjectMembersRecursive(result, value, includeNonPublic, ref lengthLimit);
                                VisitedObjects.Remove(value);
                            }
                        }
                    }
                    else
                    {
                        string name;
                        Builder valueBuilder = MakeMemberBuilder(lengthLimit);
                        FormatObjectRecursive(valueBuilder, value, isRoot: false, debuggerDisplayName: out name);

                        if (string.IsNullOrEmpty(name))
                        {
                            name = member.Name;
                        }
                        else
                        {
                            name = FormatWithEmbeddedExpressions(MakeMemberBuilder(lengthLimit), name, value).ToString();
                        }

                        if (!AddMember(result, new FormattedMember(-1, name, valueBuilder.ToString()), ref lengthLimit))
                        {
                            return;
                        }
                    }
                }
            }

            private bool AddMember(List<FormattedMember> members, FormattedMember member, ref int remainingLength)
            {
                // Add this item even if we exceed the limit - its prefix might be appended to the result.
                members.Add(member);

                // We don't need to calculate an exact length, just a lower bound on the size.
                // We can add more members to the result than it will eventually fit, we shouldn't add less.
                // Add 2 more, even if only one or half of it fit, so that the separator is included in edge cases.

                if (remainingLength == int.MinValue)
                {
                    return false;
                }

                remainingLength -= member.MinimalLength;
                if (remainingLength <= 0)
                {
                    remainingLength = int.MinValue;
                }

                return true;
            }

            private void FormatException(Builder result, Exception exception)
            {
                result.Append("!<");
                result.Append(_formatter.TypeNameFormatter.FormatTypeName(exception.GetType(), _typeNameOptions));
                result.Append('>');
            }

            #endregion

            #region Collections

            private void FormatKeyValuePair(Builder result, object obj)
            {
                TypeInfo type = obj.GetType().GetTypeInfo();
                object key = type.GetDeclaredProperty("Key").GetValue(obj, Array.Empty<object>());
                object value = type.GetDeclaredProperty("Value").GetValue(obj, Array.Empty<object>());
                string _;
                result.AppendGroupOpening();
                result.AppendCollectionItemSeparator(isFirst: true, inline: true);
                FormatObjectRecursive(result, key, isRoot: false, debuggerDisplayName: out _);
                result.AppendCollectionItemSeparator(isFirst: false, inline: true);
                FormatObjectRecursive(result, value, isRoot: false, debuggerDisplayName: out _);
                result.AppendGroupClosing(inline: true);
            }

            private void FormatCollectionHeader(Builder result, ICollection collection)
            {
                if (collection is Array array)
                {
                    result.Append(_formatter.TypeNameFormatter.FormatArrayTypeName(array.GetType(), array, _typeNameOptions));
                    return;
                }

                result.Append(_formatter.TypeNameFormatter.FormatTypeName(collection.GetType(), _typeNameOptions));
                try
                {
                    result.Append('(');
                    result.Append(collection.Count.ToString());
                    result.Append(')');
                }
                catch (Exception)
                {
                    // skip
                }
            }

            private void FormatArray(Builder result, Array array)
            {
                FormatCollectionHeader(result, array);

                // NB: Arrays specifically ignore MemberDisplayFormat.Hidden.

                if (array.Rank > 1)
                {
                    FormatMultidimensionalArrayElements(result, array, inline: _memberDisplayFormat != MemberDisplayFormat.SeparateLines);
                }
                else
                {
                    result.Append(' ');
                    FormatSequenceMembers(result, array, inline: _memberDisplayFormat != MemberDisplayFormat.SeparateLines);
                }
            }

            private void FormatDictionaryMembers(Builder result, IDictionary dict, bool inline)
            {
                result.AppendGroupOpening();

                int i = 0;
                try
                {
                    IDictionaryEnumerator enumerator = dict.GetEnumerator();
                    IDisposable disposable = enumerator as IDisposable;
                    try
                    {
                        while (enumerator.MoveNext())
                        {
                            var entry = enumerator.Entry;
                            string _;
                            result.AppendCollectionItemSeparator(isFirst: i == 0, inline: inline);
                            result.AppendGroupOpening();
                            result.AppendCollectionItemSeparator(isFirst: true, inline: true);
                            FormatObjectRecursive(result, entry.Key, isRoot: false, debuggerDisplayName: out _);
                            result.AppendCollectionItemSeparator(isFirst: false, inline: true);
                            FormatObjectRecursive(result, entry.Value, isRoot: false, debuggerDisplayName: out _);
                            result.AppendGroupClosing(inline: true);
                            i++;
                        }
                    }
                    finally
                    {
                        disposable?.Dispose();
                    }
                }
                catch (Exception e)
                {
                    result.AppendCollectionItemSeparator(isFirst: i == 0, inline: inline);
                    FormatException(result, e);
                    result.Append(' ');
                    result.Append(_builderOptions.Ellipsis);
                }

                result.AppendGroupClosing(inline);
            }

            private void FormatSequenceMembers(Builder result, IEnumerable sequence, bool inline)
            {
                result.AppendGroupOpening();
                int i = 0;

                try
                {
                    foreach (var item in sequence)
                    {
                        string _;
                        result.AppendCollectionItemSeparator(isFirst: i == 0, inline: inline);
                        FormatObjectRecursive(result, item, isRoot: false, debuggerDisplayName: out _);
                        i++;
                    }
                }
                catch (Exception e)
                {
                    result.AppendCollectionItemSeparator(isFirst: i == 0, inline: inline);
                    FormatException(result, e);
                    result.Append(" ...");
                }

                result.AppendGroupClosing(inline);
            }

            private void FormatMultidimensionalArrayElements(Builder result, Array array, bool inline)
            {
                Debug.Assert(array.Rank > 1);

                if (array.Length == 0)
                {
                    result.AppendCollectionItemSeparator(isFirst: true, inline: true);
                    result.AppendGroupOpening();
                    result.AppendGroupClosing(inline: true);
                    return;
                }

                int[] indices = new int[array.Rank];
                for (int i = array.Rank - 1; i >= 0; i--)
                {
                    indices[i] = array.GetLowerBound(i);
                }

                int nesting = 0;
                int flatIndex = 0;
                while (true)
                {
                    // increment indices (lower index overflows to higher):
                    int i = indices.Length - 1;
                    while (indices[i] > array.GetUpperBound(i))
                    {
                        indices[i] = array.GetLowerBound(i);
                        result.AppendGroupClosing(inline: inline || nesting != 1);
                        nesting--;

                        i--;
                        if (i < 0)
                        {
                            return;
                        }

                        indices[i]++;
                    }

                    result.AppendCollectionItemSeparator(isFirst: flatIndex == 0, inline: inline || nesting != 1);

                    i = indices.Length - 1;
                    while (i >= 0 && indices[i] == array.GetLowerBound(i))
                    {
                        result.AppendGroupOpening();
                        nesting++;

                        // array isn't empty, so there is always an element following this separator
                        result.AppendCollectionItemSeparator(isFirst: true, inline: inline || nesting != 1);

                        i--;
                    }

                    string _;
                    FormatObjectRecursive(result, array.GetValue(indices), isRoot: false, debuggerDisplayName: out _);

                    indices[^1]++;
                    flatIndex++;
                }
            }

            #endregion

            #region Scalars

            private bool IsTuple(object obj)
            {
#if NETSTANDARD2_0
                if (obj is null)
                {
                    return false;
                }

                var type = obj.GetType();
                if (!type.IsGenericType)
                {
                    return false;
                }

                int backtick = type.FullName.IndexOf('`');
                if (backtick < 0)
                {
                    return false;
                }

                var nonGenericName = type.FullName[0..backtick];
                return nonGenericName == "System.ValueTuple" || nonGenericName == "System.Tuple";
#else
                return obj is ITuple;
#endif
            }

            private void ObjectToString(Builder result, object obj)
            {
                try
                {
                    string str = obj.ToString();
                    if (IsTuple(obj))
                    {
                        result.Append(str);
                    }
                    else
                    {
                        result.Append('[');
                        result.Append(str);
                        result.Append(']');
                    }
                }
                catch (Exception e)
                {
                    FormatException(result, e);
                }
            }

            #endregion

            #region DebuggerDisplay Embedded Expressions

            /// <summary>
            /// Evaluate a format string with possible member references enclosed in braces. 
            /// E.g. "goo = {GetGooString(),nq}, bar = {Bar}".
            /// </summary>
            /// <remarks>
            /// Although in theory any expression is allowed to be embedded in the string such behavior is in practice fundamentally broken.
            /// The attribute doesn't specify what language (VB, C#, F#, etc.) to use to parse these expressions. Even if it did all languages 
            /// would need to be able to evaluate each other language's expressions, which is not viable and the Expression Evaluator doesn't 
            /// work that way today. Instead it evaluates the embedded expressions in the language of the current method frame. When consuming 
            /// VB objects from C#, for example, the evaluation might fail due to language mismatch (evaluating VB expression using C# parser).
            /// 
            /// Therefore we limit the expressions to a simple language independent syntax: {clr-member-name} '(' ')' ',nq', 
            /// where parentheses and ,nq suffix (no-quotes) are optional and the name is an arbitrary CLR field, property, or method name.
            /// We then resolve the member by name using case-sensitive lookup first with fallback to case insensitive and evaluate it.
            /// If parentheses are present we only look for methods.
            /// Only parameterless members are considered.
            /// </remarks>
            private string FormatWithEmbeddedExpressions(int lengthLimit, string format, object obj)
            {
                if (string.IsNullOrEmpty(format))
                {
                    return null;
                }

                var builder = new Builder(_builderOptions.WithMaximumOutputLength(lengthLimit), suppressEllipsis: true);
                return FormatWithEmbeddedExpressions(builder, format, obj).ToString();
            }

            private Builder FormatWithEmbeddedExpressions(Builder result, string format, object obj)
            {
                int i = 0;
                while (i < format.Length)
                {
                    char c = format[i++];
                    if (c == '{')
                    {
                        if (i >= 2 && format[i - 2] == '\\')
                        {
                            result.Append('{');
                        }
                        else
                        {
                            int expressionEnd = format.IndexOf('}', i);

                            bool noQuotes, callableOnly;
                            string memberName;
                            if (expressionEnd == -1 || (memberName = ParseSimpleMemberName(format, i, expressionEnd, out noQuotes, out callableOnly)) == null)
                            {
                                // the expression isn't properly formatted
                                result.Append(format, i - 1, format.Length - i + 1);
                                break;
                            }

                            MemberInfo member = ResolveMember(obj, memberName, callableOnly);
                            if (member == null)
                            {
                                result.AppendFormat(callableOnly ? "!<Method '{0}' not found>" : "!<Member '{0}' not found>", memberName);
                            }
                            else
                            {
                                Exception exception;
                                object value = GetMemberValue(member, obj, out exception);

                                if (exception != null)
                                {
                                    FormatException(result, exception);
                                }
                                else
                                {
                                    MemberDisplayFormat oldMemberDisplayFormat = _memberDisplayFormat;
                                    CommonPrimitiveFormatterOptions oldPrimitiveOptions = _primitiveOptions;

                                    _memberDisplayFormat = MemberDisplayFormat.Hidden;
                                    _primitiveOptions = new CommonPrimitiveFormatterOptions(
                                        _primitiveOptions.NumberRadix,
                                        _primitiveOptions.IncludeCharacterCodePoints,
                                        quoteStringsAndCharacters: !noQuotes,
                                        escapeNonPrintableCharacters: _primitiveOptions.EscapeNonPrintableCharacters,
                                        cultureInfo: _primitiveOptions.CultureInfo);

                                    string _;
                                    FormatObjectRecursive(result, value, isRoot: false, debuggerDisplayName: out _);

                                    _primitiveOptions = oldPrimitiveOptions;
                                    _memberDisplayFormat = oldMemberDisplayFormat;
                                }
                            }
                            i = expressionEnd + 1;
                        }
                    }
                    else
                    {
                        result.Append(c);
                    }
                }

                return result;
            }

            #endregion
        }
    }
}
