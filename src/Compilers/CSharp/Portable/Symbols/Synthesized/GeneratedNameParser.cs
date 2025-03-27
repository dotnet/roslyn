// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static class GeneratedNameParser
    {
        internal static bool IsSynthesizedLocalName(string name)
            => name.StartsWith(GeneratedNameConstants.SynthesizedLocalNamePrefix, StringComparison.Ordinal);

        // The type of generated name. See TryParseGeneratedName.
        internal static GeneratedNameKind GetKind(string name)
            => TryParseGeneratedName(name, out var kind, out _, out _) ? kind : GeneratedNameKind.None;

        // Parse the generated name. Returns true for names of the form
        // [CS$]<[middle]>c[__[suffix]] where [CS$] is included for certain
        // generated names, where [middle] and [__[suffix]] are optional,
        // and where c is a single character in [1-9a-z]
        // (csharp\LanguageAnalysis\LIB\SpecialName.cpp).
        internal static bool TryParseGeneratedName(
            string name,
            out GeneratedNameKind kind,
            out int openBracketOffset,
            out int closeBracketOffset)
        {
            openBracketOffset = -1;
            if (name.StartsWith("CS$<", StringComparison.Ordinal))
            {
                openBracketOffset = 3;
            }
            else if (name.StartsWith("<", StringComparison.Ordinal))
            {
                openBracketOffset = 0;
            }

            if (openBracketOffset >= 0)
            {
                closeBracketOffset = IndexOfBalancedParenthesis(name, openBracketOffset, '>');
                if (closeBracketOffset >= 0 && closeBracketOffset + 1 < name.Length)
                {
                    int c = name[closeBracketOffset + 1];
                    if (c is >= '1' and <= '9' or >= 'a' and <= 'z' or >= 'A' and <= 'Z') // Note '0' is not special.
                    {
                        kind = (GeneratedNameKind)c;
                        return true;
                    }
                }
            }

            kind = GeneratedNameKind.None;
            openBracketOffset = -1;
            closeBracketOffset = -1;
            return false;
        }

        private static int IndexOfBalancedParenthesis(string str, int openingOffset, char closing)
        {
            char opening = str[openingOffset];

            int depth = 1;
            for (int i = openingOffset + 1; i < str.Length; i++)
            {
                var c = str[i];
                if (c == opening)
                {
                    depth++;
                }
                else if (c == closing)
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        internal static bool TryParseSourceMethodNameFromGeneratedName(string generatedName, GeneratedNameKind requiredKind, [NotNullWhen(true)] out string? methodName)
        {
            if (!TryParseGeneratedName(generatedName, out var kind, out int openBracketOffset, out int closeBracketOffset))
            {
                methodName = null;
                return false;
            }

            if (requiredKind != 0 && kind != requiredKind)
            {
                methodName = null;
                return false;
            }

            methodName = generatedName.Substring(openBracketOffset + 1, closeBracketOffset - openBracketOffset - 1);

            if (kind.IsTypeName())
            {
                methodName = methodName.Replace(GeneratedNameConstants.DotReplacementInTypeNames, '.');
            }

            return true;
        }

        /// <summary>
        /// Parses generated local function name out of a generated method name.
        /// </summary>
        internal static bool TryParseLocalFunctionName(string generatedName, [NotNullWhen(true)] out string? localFunctionName)
        {
            localFunctionName = null;

            // '<' containing-method-name '>' 'g' '__' local-function-name '|' method-ordinal '_' lambda-ordinal
            if (!TryParseGeneratedName(generatedName, out var kind, out _, out int closeBracketOffset) || kind != GeneratedNameKind.LocalFunction)
            {
                return false;
            }

            int localFunctionNameStart = closeBracketOffset + 2 + GeneratedNameConstants.SuffixSeparator.Length;
            if (localFunctionNameStart >= generatedName.Length)
            {
                return false;
            }

            int localFunctionNameEnd = generatedName.IndexOf(GeneratedNameConstants.LocalFunctionNameTerminator, localFunctionNameStart);
            if (localFunctionNameEnd < 0)
            {
                return false;
            }

            localFunctionName = generatedName.Substring(localFunctionNameStart, localFunctionNameEnd - localFunctionNameStart);
            return true;
        }

        // Extracts the slot index from a name of a field that stores hoisted variables or awaiters.
        // Such a name ends with "__{slot index + 1}". 
        // Returned slot index is >= 0.
        internal static bool TryParseSlotIndex(string fieldName, out int slotIndex)
        {
            int lastUnder = fieldName.LastIndexOf('_');
            if (lastUnder - 1 < 0 || lastUnder == fieldName.Length || fieldName[lastUnder - 1] != '_')
            {
                slotIndex = -1;
                return false;
            }

            if (int.TryParse(fieldName.Substring(lastUnder + 1), NumberStyles.None, CultureInfo.InvariantCulture, out slotIndex) && slotIndex >= 1)
            {
                slotIndex--;
                return true;
            }

            slotIndex = -1;
            return false;
        }

        internal static bool TryParseAnonymousTypeParameterName(string typeParameterName, [NotNullWhen(true)] out string? propertyName)
        {
            if (typeParameterName.StartsWith("<", StringComparison.Ordinal) &&
                typeParameterName.EndsWith(">j__TPar", StringComparison.Ordinal))
            {
                propertyName = typeParameterName.Substring(1, typeParameterName.Length - 9);
                return true;
            }

            propertyName = null;
            return false;
        }

        internal static bool TryParsePrimaryConstructorParameterFieldName(string fieldName, [NotNullWhen(true)] out string? parameterName)
        {
            Debug.Assert((char)GeneratedNameKind.PrimaryConstructorParameter == 'P');

            if (fieldName.StartsWith("<", StringComparison.Ordinal) &&
                fieldName.EndsWith(">P", StringComparison.Ordinal))
            {
                parameterName = fieldName.Substring(1, fieldName.Length - 3);
                return true;
            }

            parameterName = null;
            return false;
        }

        public const char FileTypeNameStartChar = '<';

        private const int sha256LengthBytes = 32;
        private const int sha256LengthHexChars = sha256LengthBytes * 2;
        private static readonly string s_regexPatternString = $@"<([a-zA-Z_0-9]*)>F([0-9A-F]{{{sha256LengthHexChars}}})__";

        static GeneratedNameParser()
        {
            Debug.Assert(s_regexPatternString[0] == FileTypeNameStartChar);
        }

        // A full metadata name for a generic file-local type looks like:
        // <ContainingFile>FN__ClassName`A
        // where 'N' is the SHA256 checksum of the original file path, 'A' is the arity,
        // and 'ClassName' is the source name of the type.
        //
        // The "unmangled" name of a generic file-local type looks like:
        // <ContainingFile>FN__ClassName
        private static readonly Regex s_fileTypeOrdinalPattern = new Regex(s_regexPatternString, RegexOptions.Compiled);

        /// <remarks>
        /// This method will work with either unmangled or mangled type names as input, but it does not remove any arity suffix if present.
        /// </remarks>
        internal static bool TryParseFileTypeName(string generatedName, [NotNullWhen(true)] out string? displayFileName, [NotNullWhen(true)] out byte[]? checksum, [NotNullWhen(true)] out string? originalTypeName)
        {
            if (s_fileTypeOrdinalPattern.Match(generatedName) is Match { Success: true, Groups: var groups, Index: var index, Length: var length })
            {
                displayFileName = groups[1].Value;

                var checksumString = groups[2].Value;
                var builder = new byte[sha256LengthBytes];
                for (var i = 0; i < sha256LengthBytes; i++)
                {
                    builder[i] = (byte)((hexCharToByte(checksumString[i * 2]) << 4) | hexCharToByte(checksumString[i * 2 + 1]));
                }
                checksum = builder;

                var prefixEndsAt = index + length;
                originalTypeName = generatedName.Substring(prefixEndsAt);
                return true;
            }

            checksum = null;
            displayFileName = null;
            originalTypeName = null;
            return false;

            static byte hexCharToByte(char c)
            {
                return c switch
                {
                    >= '0' and <= '9' => (byte)(c - '0'),
                    >= 'A' and <= 'F' => (byte)(10 + c - 'A'),
                    _ => @throw(c)
                };

                static byte @throw(char c) => throw ExceptionUtilities.UnexpectedValue(c);
            }
        }
    }
}
