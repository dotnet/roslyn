// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal static class MetadataHelpers
    {
        public const char DotDelimiter = '.';
        public const string DotDelimiterString = ".";
        public const char GenericTypeNameManglingChar = '`';
        private const string GenericTypeNameManglingString = "`";
        public const int MaxStringLengthForParamSize = 22;
        public const int MaxStringLengthForIntToStringConversion = 22;
        public const string SystemString = "System";

        internal struct AssemblyQualifiedTypeName
        {
            internal readonly string TopLevelType;
            internal readonly string[] NestedTypes;
            internal readonly AssemblyQualifiedTypeName[] TypeArguments;
            internal readonly int[] ArrayRanks;
            internal readonly string AssemblyName;

            internal AssemblyQualifiedTypeName(
                string topLevelType,
                string[] nestedTypes,
                AssemblyQualifiedTypeName[] typeArguments,
                int[] arrayRanks,
                string assemblyName)
            {
                this.TopLevelType = topLevelType;
                this.NestedTypes = nestedTypes;
                this.TypeArguments = typeArguments;
                this.ArrayRanks = arrayRanks;
                this.AssemblyName = assemblyName;
            }
        }

        /// <summary>
        /// Decodes a serialized type name in its canonical form. The canonical name is its full type name, followed
        /// optionally by the assembly where it is defined, its version, culture and public key token.  If the assembly
        /// name is omitted, the type name is in the current assembly otherwise it is in the referenced assembly. The
        /// full type name is the fully qualified metadata type name. 
        /// </summary>
        internal struct SerializedTypeDecoder
        {
            private static readonly char[] TypeNameDelimiters = new char[] { '+', ',', '[', ']' };
            private string input;
            private int offset;

            void Advance()
            {
                if (!EndOfInput)
                {
                    offset++;
                }
            }

            void AdvanceTo(int i)
            {
                if (i <= input.Length)
                {
                    offset = i;
                }
            }

            bool EndOfInput
            {
                get
                {
                    return offset >= input.Length;
                }
            }

            int Offset
            {
                get
                {
                    return offset;
                }
            }

            char Current
            {
                get
                {
                    return input[offset];
                }
            }


            internal AssemblyQualifiedTypeName DecodeTypeName(string s)
            {
                input = s;
                offset = 0;
                return DecodeTypeName();
            }

            /// <summary>
            /// Decodes a type name.  A type name is a string which is terminated by the end of the string or one of the
            /// delimiters '+', ',', '[', ']'. '+' separates nested classes. '[' and ']'
            /// enclosed generic type arguments.  ',' separates types.
            /// </summary>
            private AssemblyQualifiedTypeName DecodeTypeName(bool isTypeArgument = false, bool isTypeArgumentWithAssemblyName = false)
            {
                Debug.Assert(!isTypeArgumentWithAssemblyName || isTypeArgument);

                string topLevelType = null;
                ArrayBuilder<string> nestedTypesbuilder = null;
                AssemblyQualifiedTypeName[] typeArguments = null;
                ArrayBuilder<int> arrayRanksBuilder = null;
                string assemblyName = null;
                bool decodingTopLevelType = true;
                bool isGenericTypeName = false;

                var pooledStrBuilder = PooledStringBuilder.GetInstance();
                StringBuilder typeNamebuilder = pooledStrBuilder.Builder;

                while (!EndOfInput)
                {
                    int i = input.IndexOfAny(TypeNameDelimiters, offset);
                    if (i >= 0)
                    {
                        char c = input[i];

                        // Found name, which could be a generic name with arity.
                        // Generic type parameter count, if any, are handled in DecodeGenericName.
                        string decodedString = DecodeGenericName(i);
                        Debug.Assert(decodedString != null);

                        // Type name is generic if the decoded name of the top level type OR any of the outer types of a nested type had the '`' character.
                        isGenericTypeName = isGenericTypeName || decodedString.IndexOf(GenericTypeNameManglingChar) >= 0;
                        typeNamebuilder.Append(decodedString);

                        switch (c)
                        {
                            case '+':
                                if (arrayRanksBuilder != null)
                                {
                                    // Error case, array shape must be specified at the end of the type name.
                                    // Process as a regular character and continue.
                                    typeNamebuilder.Append('+');
                                }
                                else
                                {
                                    // Type followed by nested type. Handle nested class separator and collect the nested types.
                                    HandleDecodedTypeName(typeNamebuilder.ToString(), decodingTopLevelType, ref topLevelType, ref nestedTypesbuilder);
                                    typeNamebuilder.Clear();
                                    decodingTopLevelType = false;
                                }

                                Advance();
                                break;

                            case '[':
                                // Is type followed by generic type arguments?
                                if (isGenericTypeName && typeArguments == null)
                                {
                                    Advance();
                                    if (arrayRanksBuilder != null)
                                    {
                                        // Error case, array shape must be specified at the end of the type name.
                                        // Process as a regular character and continue.
                                        typeNamebuilder.Append('[');
                                    }
                                    else
                                    {
                                        // Decode type arguments.
                                        typeArguments = DecodeTypeArguments();
                                    }
                                }
                                else
                                {
                                    // Decode array shape.
                                    DecodeArrayShape(typeNamebuilder, ref arrayRanksBuilder);
                                }

                                break;

                            case ']':
                                if (isTypeArgument)
                                {
                                    // End of type arguments.  This occurs when the last type argument is a type in the
                                    // current assembly.
                                    goto ExitDecodeTypeName;
                                }
                                else
                                {
                                    // Error case, process as a regular character and continue.
                                    typeNamebuilder.Append(']');
                                    Advance();
                                    break;
                                }

                            case ',':
                                // A comma may separate a type name from its assembly name or a type argument from
                                // another type argument.
                                // If processing non-type argument or a type argument with assembly name,
                                // process the characters after the comma as an assembly name.
                                if (!isTypeArgument || isTypeArgumentWithAssemblyName)
                                {
                                    Advance();
                                    if (!EndOfInput && Char.IsWhiteSpace(Current))
                                    {
                                        Advance();
                                    }

                                    assemblyName = DecodeAssemblyName(isTypeArgumentWithAssemblyName);
                                }
                                goto ExitDecodeTypeName;

                            default:
                                throw ExceptionUtilities.UnexpectedValue(c);
                        }
                    }
                    else
                    {
                        typeNamebuilder.Append(DecodeGenericName(input.Length));
                        goto ExitDecodeTypeName;
                    }
                }

            ExitDecodeTypeName:
                HandleDecodedTypeName(typeNamebuilder.ToString(), decodingTopLevelType, ref topLevelType, ref nestedTypesbuilder);
                pooledStrBuilder.Free();

                return new AssemblyQualifiedTypeName(
                    topLevelType,
                    nestedTypesbuilder != null ? nestedTypesbuilder.ToArrayAndFree() : null,
                    typeArguments,
                    arrayRanksBuilder != null ? arrayRanksBuilder.ToArrayAndFree() : null,
                    assemblyName);
            }

            private static void HandleDecodedTypeName(string decodedTypeName, bool decodingTopLevelType, ref string topLevelType, ref ArrayBuilder<string> nestedTypesbuilder)
            {
                if (decodedTypeName.Length != 0)
                {
                    if (decodingTopLevelType)
                    {
                        Debug.Assert(topLevelType == null);
                        topLevelType = decodedTypeName;
                    }
                    else
                    {
                        if (nestedTypesbuilder == null)
                        {
                            nestedTypesbuilder = ArrayBuilder<string>.GetInstance();
                        }

                        nestedTypesbuilder.Add(decodedTypeName);
                    }
                }
            }

            /// <summary>
            /// Decodes a generic name.  This is a type name followed optionally by a type parameter count
            /// </summary>
            private string DecodeGenericName(int i)
            {
                Debug.Assert(i == input.Length || TypeNameDelimiters.Contains(input[i]));

                var length = i - offset;
                if (length == 0)
                {
                    return String.Empty;
                }

                // Save start of name. The name should be the emitted name including the '`'  and arity.
                int start = offset;
                AdvanceTo(i);

                // Get the emitted name.
                return input.Substring(start, offset - start);
            }

            private AssemblyQualifiedTypeName[] DecodeTypeArguments()
            {
                if (EndOfInput)
                {
                    return null;
                }

                var typeBuilder = ArrayBuilder<AssemblyQualifiedTypeName>.GetInstance();

                while (!EndOfInput)
                {
                    typeBuilder.Add(DecodeTypeArgument());

                    if (!EndOfInput)
                    {
                        switch (Current)
                        {
                            case ',':
                                // More type arguments follow
                                Advance();
                                if (!EndOfInput && Char.IsWhiteSpace(Current))
                                {
                                    Advance();
                                }
                                break;

                            case ']':
                                // End of type arguments
                                Advance();
                                return typeBuilder.ToArrayAndFree();

                            default:
                                throw ExceptionUtilities.UnexpectedValue(EndOfInput);
                        }
                    }
                }

                return typeBuilder.ToArrayAndFree();
            }

            private AssemblyQualifiedTypeName DecodeTypeArgument()
            {
                bool isTypeArgumentWithAssemblyName = false;
                if (Current == '[')
                {
                    isTypeArgumentWithAssemblyName = true;
                    Advance();
                }

                AssemblyQualifiedTypeName result = DecodeTypeName(isTypeArgument: true, isTypeArgumentWithAssemblyName: isTypeArgumentWithAssemblyName);

                if (isTypeArgumentWithAssemblyName)
                {
                    if (!EndOfInput && Current == ']')
                    {
                        Advance();
                    }
                }

                return result;
            }

            private string DecodeAssemblyName(bool isTypeArgumentWithAssemblyName)
            {
                if (EndOfInput)
                {
                    return null;
                }

                int i = -1;
                if (isTypeArgumentWithAssemblyName)
                {
                    i = input.IndexOf(']', offset);
                    if (i < 0)
                    {
                        i = input.Length;
                    }
                }
                else
                {
                    i = input.Length;
                }

                string name = input.Substring(offset, i - offset);
                AdvanceTo(i);
                return name;
            }

            private void DecodeArrayShape(StringBuilder typeNameBuilder, ref ArrayBuilder<int> arrayRanksBuilder)
            {
                Debug.Assert(Current == '[');

                int start = this.offset;
                int rank = 1;
                Advance();

                while (!EndOfInput)
                {
                    switch (Current)
                    {
                        case ',':
                            rank++;
                            Advance();
                            break;

                        case ']':
                            if (arrayRanksBuilder == null)
                            {
                                arrayRanksBuilder = ArrayBuilder<int>.GetInstance();
                            }

                            arrayRanksBuilder.Add(rank);
                            Advance();
                            return;

                        default:
                            // Error case, process as regular characters
                            Advance();
                            typeNameBuilder.Append(input.Substring(start, offset - start));
                            return;

                    }
                }

                // Error case, process as regular characters
                typeNameBuilder.Append(input.Substring(start, offset - start));
            }
        }

        private static readonly string[] aritySuffixesOneToNine = { "`1", "`2", "`3", "`4", "`5", "`6", "`7", "`8", "`9" };

        internal static string GetAritySuffix(int arity)
        {
            Debug.Assert(arity > 0);
            return (arity <= 9) ? aritySuffixesOneToNine[arity - 1] : string.Concat(GenericTypeNameManglingString, arity.ToString(CultureInfo.InvariantCulture));
        }

        internal static string ComposeAritySuffixedMetadataName(string name, int arity)
        {
            return name + GetAritySuffix(arity);
        }

        internal static int InferTypeArityFromMetadataName(string emittedTypeName)
        {
            int suffixStartsAt;
            return InferTypeArityFromMetadataName(emittedTypeName, out suffixStartsAt);
        }

        private static short InferTypeArityFromMetadataName(string emittedTypeName, out int suffixStartsAt)
        {
            Debug.Assert(emittedTypeName != null, "NULL actual name unexpected!!!");
            int emittedTypeNameLength = emittedTypeName.Length;

            int indexOfManglingChar;
            for (indexOfManglingChar = emittedTypeNameLength; indexOfManglingChar >= 1; indexOfManglingChar--)
            {
                if (emittedTypeName[indexOfManglingChar - 1] == GenericTypeNameManglingChar)
                {
                    break;
                }
            }

            if (indexOfManglingChar < 2 ||
               (emittedTypeNameLength - indexOfManglingChar) == 0 ||
               emittedTypeNameLength - indexOfManglingChar > MaxStringLengthForParamSize)
            {
                suffixStartsAt = -1;
                return 0;
            }

            // Given a name corresponding to <unmangledName>`<arity>,
            // extract the arity.
            string stringRepresentingArity = emittedTypeName.Substring(indexOfManglingChar);

            int arity;
            bool nonNumericCharFound = !int.TryParse(stringRepresentingArity, NumberStyles.None, CultureInfo.InvariantCulture, out arity);

            if (nonNumericCharFound || arity < 0 || arity > short.MaxValue ||
                stringRepresentingArity != arity.ToString())
            {
                suffixStartsAt = -1;
                return 0;
            }

            suffixStartsAt = indexOfManglingChar - 1;
            return (short)arity;
        }

        internal static string InferTypeArityAndUnmangleMetadataName(string emittedTypeName, out short arity)
        {
            int suffixStartsAt;
            arity = InferTypeArityFromMetadataName(emittedTypeName, out suffixStartsAt);

            if (arity == 0)
            {
                Debug.Assert(suffixStartsAt == -1);
                return emittedTypeName;
            }

            Debug.Assert(suffixStartsAt > 0 && suffixStartsAt < emittedTypeName.Length - 1);
            return emittedTypeName.Substring(0, suffixStartsAt);
        }

        internal static string UnmangleMetadataNameForArity(string emittedTypeName, int arity)
        {
            Debug.Assert(arity > 0);

            int suffixStartsAt;
            if (arity == InferTypeArityFromMetadataName(emittedTypeName, out suffixStartsAt))
            {
                Debug.Assert(suffixStartsAt > 0 && suffixStartsAt < emittedTypeName.Length - 1);
                return emittedTypeName.Substring(0, suffixStartsAt);
            }

            return emittedTypeName;
        }

        /// <summary>
        /// An ImmutableArray representing the single string "System"
        /// </summary>
        private static readonly ImmutableArray<string> SplitQualifiedNameSystem = ImmutableArray.Create(SystemString);

        internal static ImmutableArray<string> SplitQualifiedName(
              string name)
        {
            Debug.Assert(name != null);

            if (name.Length == 0)
            {
                return ImmutableArray<string>.Empty;
            }

            // PERF: Avoid String.Split because of the allocations. Also, we can special-case
            // for "System" if it is the first or only part.

            int dots = 0;
            foreach (char ch in name)
            {
                if (ch == DotDelimiter)
                {
                    dots++;
                }
            }

            if (dots == 0)
            {
                return name == SystemString ? SplitQualifiedNameSystem : ImmutableArray.Create(name);
            }

            var result = ArrayBuilder<string>.GetInstance(dots + 1);

            int start = 0;
            for (int i = 0; dots > 0; i++)
            {
                if (name[i] == DotDelimiter)
                {
                    int len = i - start;
                    if (len == 6 && start == 0 && name.StartsWith(SystemString))
                    {
                        result.Add(SystemString);
                    }
                    else
                    {
                        result.Add(name.Substring(start, len));
                    }

                    dots--;
                    start = i + 1;
                }
            }

            result.Add(name.Substring(start));

            return result.ToImmutableAndFree();
        }

        internal static string SplitQualifiedName(
            string pstrName,
            out string qualifier)
        {
            Debug.Assert(pstrName != null);

            int delimiter = pstrName.LastIndexOf(DotDelimiter);

            if (delimiter < 0)
            {
                qualifier = string.Empty;
                return pstrName;
            }

            if (delimiter == 6 && pstrName.StartsWith(SystemString))
            {
                qualifier = SystemString;
            }
            else
            {
                qualifier = pstrName.Substring(0, delimiter);
            }

            return pstrName.Substring(delimiter + 1);
        }

        internal static string BuildQualifiedName(
            string qualifier,
            string name)
        {
            Debug.Assert(name != null);

            if (qualifier != null && qualifier.Length > 0)
            {
                return String.Concat(qualifier, DotDelimiterString, name);
            }

            return name;
        }

        /// <summary>
        /// Calculates information about types and namespaces immediately contained within a namespace.
        /// </summary>
        /// <param name="namespaceNameLength">
        /// Length of the fully-qualified name of this namespace.
        /// </param>
        /// <param name="typesByNS">
        /// The sequence of groups of TypeDef row ids for types contained within the namespace, 
        /// recursively including those from nested namespaces. The row ids must be grouped by the 
        /// fully-qualified namespace name in case-sensitive manner. 
        /// Key of each IGrouping is a fully-qualified namespace name, which starts with the name of 
        /// this namespace. There could be multiple groups for each fully-qualified namespace name.
        /// 
        /// The groups must be sorted by the keys in a manner consistent with comparer passed in as
        /// nameComparer. Therefore, all types immediately contained within THIS namespace, if any, 
        /// must be in several IGrouping at the very beginning of the sequence.
        /// </param>
        /// <param name="nameComparer">
        /// Equality comparer to compare namespace names.
        /// </param>
        /// <param name="types">
        /// Output parameter, never null:
        /// A sequence of groups of TypeDef row ids for types immediately contained within this namespace.
        /// </param>
        /// <param name="namespaces">
        /// Output parameter, never null:
        /// A sequence with information about namespaces immediately contained within this namespace.
        /// For each pair:
        ///   Key - contains simple name of a child namespace.
        ///   Value – contains a sequence similar to the one passed to this function, but
        ///           calculated for the child namespace. 
        /// </param>
        /// <remarks></remarks>
        public static void GetInfoForImmediateNamespaceMembers(
            int namespaceNameLength,
            IEnumerable<IGrouping<string, TypeHandle>> typesByNS,
            StringComparer nameComparer,
            out IEnumerable<IGrouping<string, TypeHandle>> types,
            out IEnumerable<KeyValuePair<string, IEnumerable<IGrouping<string, TypeHandle>>>> namespaces)
        {
            Debug.Assert(typesByNS != null);
            Debug.Assert(namespaceNameLength >= 0);

            // A list of groups of TypeDef row ids for types immediately contained within this namespace.
            var nestedTypes = new List<IGrouping<string, TypeHandle>>();

            // A list accumulating information about namespaces immediately contained within this namespace.
            // For each pair:
            //   Key - contains simple name of a child namespace.
            //   Value – contains a sequence similar to the one passed to this function, but
            //           calculated for the child namespace. 
            var nestedNamespaces = new List<KeyValuePair<string, IEnumerable<IGrouping<string, TypeHandle>>>>();

            var enumerator = typesByNS.GetEnumerator();

            using (enumerator)
            {
                if (enumerator.MoveNext())
                {
                    var pair = enumerator.Current;

                    // Simple name of the last encountered child namespace.
                    string lastChildNamespaceName = string.Empty;

                    // A list accumulating information about types within the last encountered child namespace.
                    // The list is similar to the sequence passed to this function.
                    List<IGrouping<string, TypeHandle>> typesInLastChildNamespace = null;

                    // if there are any types in this namespace,
                    // they will be in the first several groups if if their key length 
                    // is equal to namespaceNameLength.
                    while (pair.Key.Length == namespaceNameLength)
                    {
                        nestedTypes.Add(pair);

                        if (!enumerator.MoveNext())
                        {
                            goto DoneWithSequence;
                        }

                        pair = enumerator.Current;
                    }

                    // Account for the dot following THIS namespace name.
                    if (namespaceNameLength != 0)
                    {
                        namespaceNameLength++;
                    }

                    do
                    {
                        pair = enumerator.Current;

                        string childNamespaceName = ExtractSimpleNameOfChildNamespace(namespaceNameLength, pair.Key);

                        if (nameComparer.Equals(childNamespaceName, lastChildNamespaceName))
                        {
                            // We are still processing the same child namespace
                            typesInLastChildNamespace.Add(pair);
                        }
                        else
                        {
                            // This is a new child namespace

                            // Preserve information about previous child namespace.
                            if (typesInLastChildNamespace != null)
                            {
                                Debug.Assert(typesInLastChildNamespace.Count != 0);
                                nestedNamespaces.Add(
                                    new KeyValuePair<string, IEnumerable<IGrouping<string, TypeHandle>>>(
                                        lastChildNamespaceName, typesInLastChildNamespace));
                            }

                            typesInLastChildNamespace = new List<IGrouping<string, TypeHandle>>();
                            lastChildNamespaceName = childNamespaceName;

                            typesInLastChildNamespace.Add(pair);
                        }
                    }
                    while (enumerator.MoveNext());

                    // Preserve information about last child namespace.
                    if (typesInLastChildNamespace != null)
                    {
                        Debug.Assert(typesInLastChildNamespace.Count != 0);
                        nestedNamespaces.Add(
                            new KeyValuePair<string, IEnumerable<IGrouping<string, TypeHandle>>>(
                                lastChildNamespaceName, typesInLastChildNamespace));
                    }

                DoneWithSequence:
                /*empty statement*/
                    ;
                }
            } // using

            types = nestedTypes;
            namespaces = nestedNamespaces;

            Debug.Assert(types != null);
            Debug.Assert(namespaces != null);
        }

        /// <summary>
        /// Extract a simple name of a top level child namespace from potentially qualified namespace name.
        /// </summary>
        /// <param name="parentNamespaceNameLength">
        /// Parent namespace name length plus the dot.
        /// </param>
        /// <param name="fullName">
        /// Fully qualified namespace name.
        /// </param>
        /// <returns>
        /// Simple name of a top level child namespace, the left-most name following parent namespace name 
        /// in the fully qualified name.
        /// </returns>
        /// <remarks></remarks>
        private static string ExtractSimpleNameOfChildNamespace(
            int parentNamespaceNameLength,
            string fullName)
        {
            int index = fullName.IndexOf('.', parentNamespaceNameLength);

            if (index < 0)
            {
                return fullName.Substring(parentNamespaceNameLength);
            }
            else
            {
                return fullName.Substring(parentNamespaceNameLength, index - parentNamespaceNameLength);
            }
        }

        /// <summary>
        /// Determines whether given string can be used as a non-empty metadata identifier (a NUL-terminated UTF8 string).
        /// </summary>
        internal static bool IsValidMetadataIdentifier(string str)
        {
            return !String.IsNullOrEmpty(str) && IsValidUnicodeString(str, allowNullCharacters: false);
        }

        /// <summary>
        /// True if the string doesn't contain incomplete surrogates.
        /// </summary>
        internal static bool IsValidUnicodeString(string str)
        {
            return String.IsNullOrEmpty(str) || IsValidUnicodeString(str, allowNullCharacters: true);
        }

        private static bool IsValidUnicodeString(string str, bool allowNullCharacters)
        {
            int i = 0;
            while (i < str.Length)
            {
                char c = str[i++];
                if (c == 0 && !allowNullCharacters)
                {
                    return false;
                }

                // (high surrogate, low surrogate) makes a valid pair, anything else is invalid:
                if (Char.IsHighSurrogate(c))
                {
                    if (i < str.Length && Char.IsLowSurrogate(str[i]))
                    {
                        i++;
                    }
                    else
                    {
                        // high surrogate not followed by low surrogate
                        return false;
                    }
                }
                else if (Char.IsLowSurrogate(c))
                {
                    // previous character wasn't a high surrogate
                    return false;
                }
            }

            return true;
        }

        internal static void ValidateAssemblyOrModuleName(string name, string argumentName)
        {
            var e = CheckAssemblyOrModuleName(name, argumentName);
            if (e != null)
            {
                throw e;
            }
        }

        internal static bool IsValidAssemblyOrModuleName(string name)
        {
            return CheckAssemblyOrModuleName(name, argumentName: null) == null;
        }

        internal static Exception CheckAssemblyOrModuleName(string name, string argumentName)
        {
            if (name == null)
            {
                return new ArgumentNullException(argumentName);
            }

            // Dev11 VB can produce assembly with no name (vbc /out:".dll" /target:library). 
            // We disallow it. PEVerify reports an error: Assembly has no name.
            if (name.Length == 0)
            {
                return new ArgumentException(CodeAnalysisResources.NameCannotBeEmpty, argumentName);
            }

            // Dev11 VB can produce assembly that starts with whitespace (vbc /out:" a.dll" /target:library). 
            // We disallow it. PEVerify reports an error: Assembly name contains leading spaces.
            if (Char.IsWhiteSpace(name[0]))
            {
                return new ArgumentException(CodeAnalysisResources.NameCannotStartWithWhitespace, argumentName);
            }

            if (PathUtilities.HasDirectorySeparators(name) || !IsValidMetadataIdentifier(name))
            {
                return new ArgumentException(CodeAnalysisResources.NameContainsInvalidCharacter, argumentName);
            }

            return null;
        }

        /// <summary>
        /// Determine if the given namespace and type names combine to produce the given fully qualified name.
        /// </summary>
        /// <param name="namespaceName">The namespace part of the split name.</param>
        /// <param name="typeName">The type name part of the split name.</param>
        /// <param name="fullyQualified">The fully qualified name to compare with.</param>
        /// <returns>true if the combination of <paramref name="namespaceName"/> and <paramref name="typeName"/> equals the fully-qualified name given by <paramref name="fullyQualified"/></returns>
        internal static bool SplitNameEqualsFullyQualifiedName(string namespaceName, string typeName, string fullyQualified)
        {
            // Look for "[namespaceName].[typeName]" exactly
            return fullyQualified.Length == namespaceName.Length + typeName.Length + 1 &&
                   fullyQualified[namespaceName.Length] == MetadataHelpers.DotDelimiter &&
                   fullyQualified.StartsWith(namespaceName, StringComparison.Ordinal) &&
                   fullyQualified.EndsWith(typeName, StringComparison.Ordinal);
        }
    }
}
