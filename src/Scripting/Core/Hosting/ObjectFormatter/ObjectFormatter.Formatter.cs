// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Roslyn.Utilities;
using System.Reflection;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    using TypeInfo = System.Reflection.TypeInfo;

    public abstract partial class ObjectFormatter
    {
        // internal for testing
        internal sealed class Formatter
        {
            private readonly ObjectFormatter _language;
            private readonly ObjectFormattingOptions _options;
            private HashSet<object> _lazyVisitedObjects;

            private HashSet<object> VisitedObjects
            {
                get
                {
                    if (_lazyVisitedObjects == null)
                    {
                        _lazyVisitedObjects = new HashSet<object>(ReferenceEqualityComparer.Instance);
                    }

                    return _lazyVisitedObjects;
                }
            }

            public Formatter(ObjectFormatter language, ObjectFormattingOptions options)
            {
                _options = options ?? ObjectFormattingOptions.Default;
                _language = language;
            }

            private Builder MakeMemberBuilder(int limit)
            {
                return new Builder(Math.Min(_options.MaxLineLength, limit), _options, insertEllipsis: false);
            }

            public string FormatObject(object obj)
            {
                try
                {
                    var builder = new Builder(_options.MaxOutputLength, _options, insertEllipsis: true);
                    string _;
                    return FormatObjectRecursive(builder, obj, _options.QuoteStrings, _options.MemberFormat, out _).ToString();
                }
                catch (InsufficientExecutionStackException)
                {
                    return ScriptingResources.StackOverflowWhileEvaluating;
                }
            }

            private Builder FormatObjectRecursive(Builder result, object obj, bool quoteStrings, MemberDisplayFormat memberFormat, out string name)
            {
                name = null;
                string primitive = _language.FormatPrimitive(obj, quoteStrings, _options.IncludeCodePoints, _options.UseHexadecimalNumbers);
                if (primitive != null)
                {
                    result.Append(primitive);
                    return result;
                }

                object originalObj = obj;
                TypeInfo originalType = originalObj.GetType().GetTypeInfo();

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
                if (originalType.IsGenericType && originalType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    if (memberFormat != MemberDisplayFormat.InlineValue)
                    {
                        result.Append(_language.FormatTypeName(originalType.AsType(), _options));
                        result.Append(' ');
                    }

                    FormatKeyValuePair(result, originalObj);
                    return result;
                }

                if (originalType.IsArray)
                {
                    if (!VisitedObjects.Add(originalObj))
                    {
                        result.AppendInfiniteRecursionMarker();
                        return result;
                    }

                    FormatArray(result, (Array)originalObj, inline: memberFormat != MemberDisplayFormat.List);

                    VisitedObjects.Remove(originalObj);
                    return result;
                }

                DebuggerDisplayAttribute debuggerDisplay = GetApplicableDebuggerDisplayAttribute(originalType);
                if (debuggerDisplay != null)
                {
                    name = debuggerDisplay.Name;
                }

                bool suppressMembers = false;

                //
                // TypeName(count) for ICollection implementers
                // or
                // TypeName([[DebuggerDisplay.Value]])        // Inline
                // [[DebuggerDisplay.Value]]                  // InlineValue
                // or
                // [[ToString()]] if ToString overridden
                // or
                // TypeName 
                // 
                ICollection collection;
                if ((collection = originalObj as ICollection) != null)
                {
                    FormatCollectionHeader(result, collection);
                }
                else if (debuggerDisplay != null && !String.IsNullOrEmpty(debuggerDisplay.Value))
                {
                    if (memberFormat != MemberDisplayFormat.InlineValue)
                    {
                        result.Append(_language.FormatTypeName(originalType.AsType(), _options));
                        result.Append('(');
                    }

                    FormatWithEmbeddedExpressions(result, debuggerDisplay.Value, originalObj);

                    if (memberFormat != MemberDisplayFormat.InlineValue)
                    {
                        result.Append(')');
                    }

                    suppressMembers = true;
                }
                else if (HasOverriddenToString(originalType))
                {
                    ObjectToString(result, originalObj);
                    suppressMembers = true;
                }
                else
                {
                    result.Append(_language.FormatTypeName(originalType.AsType(), _options));
                }

                if (memberFormat == MemberDisplayFormat.NoMembers)
                {
                    return result;
                }

                bool includeNonPublic = memberFormat == MemberDisplayFormat.List;
                object proxy = GetDebuggerTypeProxy(obj);
                if (proxy != null)
                {
                    obj = proxy;
                    includeNonPublic = false;
                    suppressMembers = false;
                }

                if (memberFormat != MemberDisplayFormat.List && suppressMembers)
                {
                    return result;
                }

                // TODO (tomat): we should not use recursion
                RuntimeHelpers.EnsureSufficientExecutionStack();

                result.Append(' ');

                if (!VisitedObjects.Add(originalObj))
                {
                    result.AppendInfiniteRecursionMarker();
                    return result;
                }

                // handle special types only if a proxy isn't defined
                if (proxy == null)
                {
                    IDictionary dictionary;
                    if ((dictionary = obj as IDictionary) != null)
                    {
                        FormatDictionary(result, dictionary, inline: memberFormat != MemberDisplayFormat.List);
                        return result;
                    }

                    IEnumerable enumerable;
                    if ((enumerable = obj as IEnumerable) != null)
                    {
                        FormatSequence(result, enumerable, inline: memberFormat != MemberDisplayFormat.List);
                        return result;
                    }
                }

                FormatObjectMembers(result, obj, originalType, includeNonPublic, inline: memberFormat != MemberDisplayFormat.List);

                VisitedObjects.Remove(obj);

                return result;
            }

            #region Members

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
            private void FormatObjectMembers(Builder result, object obj, TypeInfo originalType, bool includeNonPublic, bool inline)
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
                bool useCollectionFormat = UseCollectionFormat(members, originalType);

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

                    if (result.LimitReached)
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

            private struct FormattedMember
            {
                // Non-negative if the member is an inlined element of an array (DebuggerBrowsableState.RootHidden applied on a member of array type).
                public readonly int Index;

                // Formatted name of the member or null if it doesn't have a name (Index is >=0 then).
                public readonly string Name;

                // Formatted value of the member.
                public readonly string Value;

                public FormattedMember(int index, string name, string value)
                {
                    Name = name;
                    Index = index;
                    Value = value;
                }

                public int MinimalLength
                {
                    get { return (Name != null ? Name.Length : "[0]".Length) + Value.Length; }
                }

                public string GetDisplayName()
                {
                    return Name ?? "[" + Index.ToString() + "]";
                }

                public bool HasKeyName()
                {
                    return Index >= 0 && Name != null && Name.Length >= 2 && Name[0] == '[' && Name[Name.Length - 1] == ']';
                }

                public bool AppendAsCollectionEntry(Builder result)
                {
                    // Some BCL collections use [{key.ToString()}]: {value.ToString()} pattern to display collection entries.
                    // We want them to be printed initializer-style, i.e. { <key>, <value> } 
                    if (HasKeyName())
                    {
                        result.AppendGroupOpening();
                        result.AppendCollectionItemSeparator(isFirst: true, inline: true);
                        result.Append(Name, 1, Name.Length - 2);
                        result.AppendCollectionItemSeparator(isFirst: false, inline: true);
                        result.Append(Value);
                        result.AppendGroupClosing(inline: true);
                    }
                    else
                    {
                        result.Append(Value);
                    }

                    return true;
                }

                public bool Append(Builder result, string separator)
                {
                    result.Append(GetDisplayName());
                    result.Append(separator);
                    result.Append(Value);
                    return true;
                }
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
                    if (_language.IsHiddenMember(member))
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

                    FieldInfo field = member as FieldInfo;
                    if (field != null)
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
                        string k = FormatWithEmbeddedExpressions(lengthLimit, debuggerDisplay.Name, obj) ?? _language.FormatMemberName(member);
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
                        if (!AddMember(result, new FormattedMember(-1, _language.FormatMemberName(member), memberValueBuilder.ToString()), ref lengthLimit))
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
                                    FormatObjectRecursive(valueBuilder, item, _options.QuoteStrings, MemberDisplayFormat.InlineValue, out name);

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
                            else if (_language.FormatPrimitive(value, _options.QuoteStrings, _options.IncludeCodePoints, _options.UseHexadecimalNumbers) == null && VisitedObjects.Add(value))
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
                        FormatObjectRecursive(valueBuilder, value, _options.QuoteStrings, MemberDisplayFormat.InlineValue, out name);

                        if (String.IsNullOrEmpty(name))
                        {
                            name = _language.FormatMemberName(member);
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
                result.Append(_language.FormatTypeName(exception.GetType(), _options));
                result.Append('>');
            }

            #endregion

            #region Collections

            private void FormatKeyValuePair(Builder result, object obj)
            {
                TypeInfo type = obj.GetType().GetTypeInfo();
                object key = type.GetDeclaredProperty("Key").GetValue(obj, SpecializedCollections.EmptyObjects);
                object value = type.GetDeclaredProperty("Value").GetValue(obj, SpecializedCollections.EmptyObjects);
                string _;
                result.AppendGroupOpening();
                result.AppendCollectionItemSeparator(isFirst: true, inline: true);
                FormatObjectRecursive(result, key, quoteStrings: true, memberFormat: MemberDisplayFormat.InlineValue, name: out _);
                result.AppendCollectionItemSeparator(isFirst: false, inline: true);
                FormatObjectRecursive(result, value, quoteStrings: true, memberFormat: MemberDisplayFormat.InlineValue, name: out _);
                result.AppendGroupClosing(inline: true);
            }

            private void FormatCollectionHeader(Builder result, ICollection collection)
            {
                Array array = collection as Array;
                if (array != null)
                {
                    result.Append(_language.FormatArrayTypeName(array.GetType(), array, _options));
                    return;
                }

                result.Append(_language.FormatTypeName(collection.GetType(), _options));
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

            private void FormatArray(Builder result, Array array, bool inline)
            {
                FormatCollectionHeader(result, array);

                if (array.Rank > 1)
                {
                    FormatMultidimensionalArray(result, array, inline);
                }
                else
                {
                    result.Append(' ');
                    FormatSequence(result, (IEnumerable)array, inline);
                }
            }

            private void FormatDictionary(Builder result, IDictionary dict, bool inline)
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
                            FormatObjectRecursive(result, entry.Key, quoteStrings: true, memberFormat: MemberDisplayFormat.InlineValue, name: out _);
                            result.AppendCollectionItemSeparator(isFirst: false, inline: true);
                            FormatObjectRecursive(result, entry.Value, quoteStrings: true, memberFormat: MemberDisplayFormat.InlineValue, name: out _);
                            result.AppendGroupClosing(inline: true);
                            i++;
                        }
                    }
                    finally
                    {
                        if (disposable != null)
                        {
                            disposable.Dispose();
                        }
                    }
                }
                catch (Exception e)
                {
                    result.AppendCollectionItemSeparator(isFirst: i == 0, inline: inline);
                    FormatException(result, e);
                    result.Append(' ');
                    result.Append(_options.Ellipsis);
                }

                result.AppendGroupClosing(inline);
            }

            private void FormatSequence(Builder result, IEnumerable sequence, bool inline)
            {
                result.AppendGroupOpening();
                int i = 0;

                try
                {
                    foreach (var item in sequence)
                    {
                        string name;
                        result.AppendCollectionItemSeparator(isFirst: i == 0, inline: inline);
                        FormatObjectRecursive(result, item, quoteStrings: true, memberFormat: MemberDisplayFormat.InlineValue, name: out name);
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

            private void FormatMultidimensionalArray(Builder result, Array array, bool inline)
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

                    string name;
                    FormatObjectRecursive(result, array.GetValue(indices), quoteStrings: true, memberFormat: MemberDisplayFormat.InlineValue, name: out name);

                    indices[indices.Length - 1]++;
                    flatIndex++;
                }
            }

            #endregion

            #region Scalars

            private void ObjectToString(Builder result, object obj)
            {
                try
                {
                    string str = obj.ToString();
                    result.Append('[');
                    result.Append(str);
                    result.Append(']');
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
            /// E.g. "foo = {GetFooString(),nq}, bar = {Bar}".
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
            /// Only parameter less members are considered.
            /// </remarks>
            private string FormatWithEmbeddedExpressions(int lengthLimit, string format, object obj)
            {
                if (String.IsNullOrEmpty(format))
                {
                    return null;
                }

                return FormatWithEmbeddedExpressions(new Builder(lengthLimit, _options, insertEllipsis: false), format, obj).ToString();
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
                                    string name;
                                    FormatObjectRecursive(result, value, !noQuotes, MemberDisplayFormat.NoMembers, out name);
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

            // Parses
            // <clr-member-name>
            // <clr-member-name> ',' 'nq'
            // <clr-member-name> '(' ')' 
            // <clr-member-name> '(' ')' ',' 'nq'
            //
            // Internal for testing purposes.
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

            #endregion
        }
    }
}
