// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.QuickInfo
{
    internal static class CSharpDiagnosticAnalyzerQuickInfoProviderExtensions
    {
        public static string? ToStringOrNull(this LocalizableString @this)
        {
            var result = @this.ToString();
            if (string.IsNullOrWhiteSpace(result))
            {
                return null;
            }

            return result;
        }

        public static bool IsSuppressMessageAttribute(this NameSyntax? name)
        {
            if (name == null)
            {
                return false;
            }

            var nameValue = name.GetNameToken().ValueText;
            var stringComparer = StringComparer.Ordinal;
            return
                stringComparer.Equals(nameValue, nameof(SuppressMessageAttribute)) ||
                stringComparer.Equals(nameValue, "SuppressMessage");
        }

        public static string ExtractErrorCodeFromCheckId(this string checkId)
        {
            // checkId short and long name rules:
            // https://docs.microsoft.com/en-us/visualstudio/code-quality/in-source-suppression-overview?view=vs-2019#suppressmessage-attribute
            var position = checkId.IndexOf(':');
            var errorCode = position == -1
                ? checkId
                : checkId.Substring(0, position);
            errorCode = errorCode.Trim();
            return errorCode;
        }

        public static string FormatPragmaWarningErrorCode(this string errorCode)
        {
            // https://docs.microsoft.com/en-US/dotnet/csharp/language-reference/preprocessor-directives/preprocessor-pragma-warning
            // warning-list: A comma-separated list of warning numbers. The "CS" prefix is optional.
            // We expect a single errorCode from the warning-list in the form of CS0219, 0219 or 219
            // We return: CS0219
            errorCode = errorCode.Trim();
            if (errorCode.StartsWithLetter())
            {
                return errorCode;
            }

            if (int.TryParse(errorCode, out var errorNumber))
            {
                return $"CS{errorNumber:0000}";
            }

            return errorCode;
        }

        public static bool StartsWithLetter(this string text)
        {
            if (text.Length > 0)
            {
                return char.GetUnicodeCategory(text[0]) is
                    UnicodeCategory.UppercaseLetter or
                    UnicodeCategory.LowercaseLetter or
                    UnicodeCategory.TitlecaseLetter or
                    UnicodeCategory.ModifierLetter or
                    UnicodeCategory.OtherLetter;
            }

            return false;
        }
    }
}
