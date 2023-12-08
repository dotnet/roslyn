// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
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
                : checkId[..position];
            errorCode = errorCode.Trim();
            return errorCode;
        }
    }
}
