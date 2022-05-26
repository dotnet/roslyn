// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class DocumentationCommentExtensions
    {
        public static bool IsMultilineDocComment([NotNullWhen(true)] this DocumentationCommentTriviaSyntax? documentationComment)
        {
            if (documentationComment == null)
            {
                return false;
            }

            return documentationComment.ToFullString().StartsWith("/**", StringComparison.Ordinal);
        }
    }
}
