﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class DocumentationCommentExtensions
    {
        public static bool IsMultilineDocComment(this DocumentationCommentTriviaSyntax documentationComment)
        {
            if (documentationComment == null)
            {
                return false;
            }
            return documentationComment.ToFullString().StartsWith("/**", StringComparison.Ordinal);
        }
    }
}
