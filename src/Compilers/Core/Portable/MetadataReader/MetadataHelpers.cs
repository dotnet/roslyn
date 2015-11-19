// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        // These can appear in the interface name that precedes an explicit interface implementation member.
        public const char MangledNameRegionStartChar = '<';
        public const char MangledNameRegionEndChar = '>';

        internal struct AssemblyQualifiedTypeName
        {
            internal readonly string TopLevelType;
            internal readonly string[] NestedTypes;
            internal readonly AssemblyQualifiedTypeName[] TypeArguments;
            internal readonly int PointerCount;
            internal readonly int[] ArrayRanks;
            internal readonly string AssemblyName;

            internal AssemblyQualifiedTypeName(
                string topLevelType,
                string[] nestedTypes,
                AssemblyQualifiedTypeName[] typeArguments,
                int pointerCount,
                int[] arrayRanks,
                string assemblyName)
            {
                this.TopLevelType = topLevelType;
                this.NestedTypes = nestedTypes;
                this.TypeArguments = typeArguments;
                this.PointerCount = pointerCount;
                this.ArrayRanks = arrayRanks;
                this.AssemblyName = assemblyName;
            }
        }

        internal static AssemblyQualifiedTypeName DecodeTypeName(string s)
        {
            var decoder = new SerializedTypeDecoder(s);
            return decoder.DecodeTypeName();
        }

        /// <summary>
        /// Decodes a serialized type name in its canonical form. The canonical name is its full type name, followed
        /// optionally by the assembly where it is defined, its version, culture and public key token.  If the assembly
        /// name is omitted, the type name is in the current assembly otherwise it is in the referenced assembly. The
        /// full type name is the fully qualified metadata type name. 
        /// </summary>
        private struct SerializedTypeDecoder
        {
            private static readonly char[] s_typeNameDelimiters = { '+', ',', '[', ']', '*' };
            private readonly string _input;
            private int _offset;

            internal SerializedTypeDecoder(string s)
            {
                _input = s;
                _offset = 0;
            }

            private void Advance()
            {
                if (!EndOfInput)
                {
                    _offset++;
                }
            }

            private void AdvanceTo(int i)
            {
                if (i <= _input.Length)
                {
                    _offset = i;
                }
            }

            private bool EndOfInput
            {
                get
                {
                    return _offset >= _input.Length;
                }
            }

            private int Offset
            {
                get
                {
                    return _offset;
                }
            }

            private char Current
            {
                get
                {
                    return _input[_offset];
                }
            }

            /// <summary>
            /// Decodes a type name.  A type name is a string which is terminated by the end of the string or one of the
            /// delimiters '+', ',', '[', ']'. '+' separates nested classes. '[' and ']'
            /// enclosed generic type arguments.  ',' separates types.
            /// </summary>
            internal AssemblyQualifiedTypeName DecodeTypeName(bool isTypeArgument = false, bool isTypeArgumentWithAssemblyName = false)
            {
                Debug.Assert(!isTypeArgumentWithAssemblyName || isTypeArgument);

                string topLevelType = null;
                ArrayBuilder<string> nestedTypesBuilder = null;
                AssemblyQualifiedTypeName[] typeArguments = null;
                int pointerCount = 0;
                ArrayBuilder<int> arrayRanksBuilder = null;
                string assemblyName = null;
                bool decodingTopLevelType = true;
                bool isGenericTypeName = false;

                var pooledStrBuilder = PooledStringBuilder.GetInstance();
                StringBuilder typeNameBuilder = pooledStrBuilder.Builder;

                while (!EndOfInput)
                {
                    int i = _input.IndexOfAny(s_typeNameDelimiters, _offset);
                    if (i >= 0)
                    {
                        char c = _input[i];

                        // Found name, which could be a generic name with arity.
                        // Generic type parameter count, if any, are handled in DecodeGenericName.
                        string decodedString = DecodeGenericName(i);
                        Debug.Assert(decodedString != null);

                        // Type name is generic if the decoded name of the top level type OR any of the outer types of a nested type had the '`' character.
                        isGenericTypeName = isGenericTypeName || decodedString.IndexOf(GenericTypeNameManglingChar) >= 0;
                        typeNameBuilder.Append(decodedString);

                        switch (c)
                        {
                            case '*':
                                if (arrayRanksBuilder != null)
                                {
                                    // Error case, array shape must be specified at the end of the type name.
                                    // Process as a regular character and continue.
                                    typeNameBuilder.Append(c);
                                }
                                else
                                {
                                    pointerCount++;
                                }

                                Advance();
                                break;

                            case '+':
                                if (arrayRanksBuilder != null || pointerCount > 0)
                                {
                                    // Error case, array shape must be specified at the end of the type name.
                                    // Process as a regular character and continue.
                                    typeNameBuilder.Append(c);
                                }
                                else
                                {
                                    // Type followed by nested type. Handle nested class separator and collect the nested types.
                                    HandleDecodedTypeName(typeNameBuilder.ToString(), decodingTopLevelType, ref topLevelType, ref nestedTypesBuilder);
                                    typeNameBuilder.Clear();
                                    decodingTopLevelType = false;
                                }

                                Advance();
                                break;

                            case '[':
                                // Is type followed by generic type arguments?
                                if (isGenericTypeName && typeArguments == null)
                                {
                                    Advance();
                                    if (arrayRanksBuilder != null || pointerCount > 0)
                                    {
                                        // Error case, array shape must be specified at the end of the type name.
                                        // Process as a regular character and continue.
                                        typeNameBuilder.Append(c);
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
                                    DecodeArrayShape(typeNameBuilder, ref arrayRanksBuilder);
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
                                    typeNameBuilder.Append(c);
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
                        typeNameBuilder.Append(DecodeGenericName(_input.Length));
                        goto ExitDecodeTypeName;
                    }
                }

            ExitDecodeTypeName:
                HandleDecodedTypeName(typeNameBuilder.ToString(), decodingTopLevelType, ref topLevelType, ref nestedTypesBuilder);
                pooledStrBuilder.Free();

                return new AssemblyQualifiedTypeName(
                    topLevelType,
                    nestedTypesBuilder?.ToArrayAndFree(),
                    typeArguments,
                    pointerCount,
                    arrayRanksBuilder?.ToArrayAndFree(),
                    assemblyName);
            }

            private static void HandleDecodedTypeName(string decodedTypeName, bool decodingTopLevelType, ref string topLevelType, ref ArrayBuilder<string> nestedTypesBuilder)
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
                        if (nestedTypesBuilder == null)
                        {
                            nestedTypesBuilder = ArrayBuilder<string>.GetInstance();
                        }

                        nestedTypesBuilder.Add(decodedTypeName);
                    }
                }
            }

            /// <summary>
            /// Decodes a generic name.  This is a type name followed optionally by a type parameter count
            /// </summary>
            private string DecodeGenericName(int i)
            {
                Debug.Assert(i == _input.Length || s_typeNameDelimiters.Contains(_input[i]));

                var length = i - _offset;
                if (length == 0)
                {
                    return String.Empty;
                }

                // Save start of name. The name should be the emitted name including the '`'  and arity.
                int start = _offset;
                AdvanceTo(i);

                // Get the emitted name.
                return _input.Substring(start, _offset - start);
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

                int i;
                if (isTypeArgumentWithAssemblyName)
                {
                    i = _input.IndexOf(']', _offset);
                    if (i < 0)
                    {
                        i = _input.Length;
                    }
                }
                else
                {
                    i = _input.Length;
                }

                string name = _input.Substring(_offset, i - _offset);
                AdvanceTo(i);
                return name;
            }

            private void DecodeArrayShape(StringBuilder typeNameBuilder, ref ArrayBuilder<int> arrayRanksBuilder)
            {
                Debug.Assert(Current == '[');

                int start = _offset;
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
                            typeNameBuilder.Append(_input.Substring(start, _offset - start));
                            return;
                    }
                }

                // Error case, process as regular characters
                typeNameBuilder.Append(_input.Substring(start, _offset - start));
            }
        }

        private static readonly string[] s_aritySuffixesOneToNine = { "`1", "`2", "`3", "`4", "`5", "`6", "`7", "`8", "`9" };

        internal static string GetAritySuffix(int arity)
        {
            Debug.Assert(arity > 0);
            return (arity <= 9) ? s_aritySuffixesOneToNine[arity - 1] : string.Concat(GenericTypeNameManglingString, arity.ToString(CultureInfo.InvariantCulture));
        }

        internal static string ComposeAritySuffixedMetadataName(string name, int arity)
        {
            return arity == 0 ? name : name + GetAritySuffix(arity);
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
        private static readonly ImmutableArray<string> s_splitQualifiedNameSystem = ImmutableArray.Create(SystemString);

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
                return name == SystemString ? s_splitQualifiedNameSystem : ImmutableArray.Create(name);
            }

            var result = ArrayBuilder<string>.GetInstance(dots + 1);

            int start = 0;
            for (int i = 0; dots > 0; i++)
            {
                if (name[i] == DotDelimiter)
                {
                    int len = i - start;
                    if (len == 6 && start == 0 && name.StartsWith(SystemString, StringComparison.Ordinal))
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

            // In mangled names, the original unmangled name is frequently included,
            // surrounded by angle brackets.  The unmangled name may contain dots
            // (e.g. if it is an explicit interface implementation) or paired angle
            // brackets (e.g. if the explicitly implemented interface is generic).
            var angleBracketDepth = 0;
            var delimiter = -1;
            for (int i = 0; i < pstrName.Length; i++)
            {
                switch (pstrName[i])
                {
                    case MangledNameRegionStartChar:
                        angleBracketDepth++;
                        break;
                    case MangledNameRegionEndChar:
                        angleBracketDepth--;
                        break;
                    case DotDelimiter:
                        // If we see consecutive dots, the second is part of the method name
                        // (i.e. ".ctor" or ".cctor").
                        if (angleBracketDepth == 0 && (i == 0 || delimiter < i - 1))
                        {
                            delimiter = i;
                        }
                        break;
                }
            }
            Debug.Assert(angleBracketDepth == 0);

            if (delimiter < 0)
            {
                qualifier = string.Empty;
                return pstrName;
            }

            if (delimiter == 6 && pstrName.StartsWith(SystemString, StringComparison.Ordinal))
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

            if (!string.IsNullOrEmpty(qualifier))
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
            IEnumerable<IGrouping<string, TypeDefinitionHandle>> typesByNS,
            StringComparer nameComparer,
            out IEnumerable<IGrouping<string, TypeDefinitionHandle>> types,
            out IEnumerable<KeyValuePair<string, IEnumerable<IGrouping<string, TypeDefinitionHandle>>>> namespaces)
        {
            Debug.Assert(typesByNS != null);
            Debug.Assert(namespaceNameLength >= 0);

            // A list of groups of TypeDef row ids for types immediately contained within this namespace.
            var nestedTypes = new List<IGrouping<string, TypeDefinitionHandle>>();

            // A list accumulating information about namespaces immediately contained within this namespace.
            // For each pair:
            //   Key - contains simple name of a child namespace.
            //   Value – contains a sequence similar to the one passed to this function, but
            //           calculated for the child namespace. 
            var nestedNamespaces = new List<KeyValuePair<string, IEnumerable<IGrouping<string, TypeDefinitionHandle>>>>();

            var enumerator = typesByNS.GetEnumerator();

            using (enumerator)
            {
                if (enumerator.MoveNext())
                {
                    var pair = enumerator.Current;

                    // Simple name of the last encountered child namespace.
                    string lastChildNamespaceName = null;

                    // A list accumulating information about types within the last encountered child namespace.
                    // The list is similar to the sequence passed to this function.
                    List<IGrouping<string, TypeDefinitionHandle>> typesInLastChildNamespace = null;

                    // if there are any types in this namespace,
                    // they will be in the first several groups if their key length 
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
                                    new KeyValuePair<string, IEnumerable<IGrouping<string, TypeDefinitionHandle>>>(
                                        lastChildNamespaceName, typesInLastChildNamespace));
                            }

                            typesInLastChildNamespace = new List<IGrouping<string, TypeDefinitionHandle>>();
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
                            new KeyValuePair<string, IEnumerable<IGrouping<string, TypeDefinitionHandle>>>(
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
            return !string.IsNullOrEmpty(str) && str.IsValidUnicodeString() && str.IndexOf('\0') == -1;
        }

        /// <summary>
        /// True if the string doesn't contain incomplete surrogates.
        /// </summary>
        internal static bool IsValidUnicodeString(string str)
        {
            return str == null || str.IsValidUnicodeString();
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
            if (char.IsWhiteSpace(name[0]))
            {
                return new ArgumentException(CodeAnalysisResources.NameCannotStartWithWhitespace, argumentName);
            }

            if (!IsValidMetadataFileName(name))
            {
                return new ArgumentException(CodeAnalysisResources.NameContainsInvalidCharacter, argumentName);
            }

            return null;
        }

        /// <summary>
        /// Checks that the specified name is a valid metadata String and a file name.
        /// The specification isn't entirely consistent and complete but it mentions:
        /// 
        /// 22.19.2: "Name shall index a non-empty string in the String heap. It shall be in the format {filename}.{extension} (e.g., 'foo.dll', but not 'c:\utils\foo.dll')."
        /// 22.30.2: "The format of Name is {file name}.{file extension} with no path or drive letter; on POSIX-compliant systems Name contains no colon, no forward-slash, no backslash."
        ///          As Microsoft specific constraint.
        /// 
        /// A reasonable restriction seems to be a valid UTF8 non-empty string that doesn't contain '\0', '\', '/', ':' characters.
        /// </summary>
        internal static bool IsValidMetadataFileName(string name)
        {
            return FileNameUtilities.IsFileName(name) && IsValidMetadataIdentifier(name);
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

        internal static bool IsValidPublicKey(ImmutableArray<byte> bytes) => CryptoBlobParser.IsValidPublicKey(bytes);

        /// <summary>
        /// Given an input string changes it to be acceptable as a part of a type name.
        /// </summary>
        internal static string MangleForTypeNameIfNeeded(string moduleName)
        {
            var pooledStrBuilder = PooledStringBuilder.GetInstance();
            var s = pooledStrBuilder.Builder;
            s.Append(moduleName);
            s.Replace("Q", "QQ");
            s.Replace("_", "Q_");
            s.Replace('.', '_');

            return pooledStrBuilder.ToStringAndFree();
        }
    }
}
