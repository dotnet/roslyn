﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class DocumentationCommentExtensions
    {
        public static bool IsMultilineDocComment(this DocumentationCommentTriviaSyntax documentationComment)
        {
            return documentationComment.ToFullString().StartsWith("/**", StringComparison.Ordinal);
        }
    }
}
