// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.Extensions
{
    internal static class SyntaxTokenExtensions
    {
        public static T? GetAncestor<T>(this SyntaxToken token, Func<T, bool>? predicate = null)
            where T : SyntaxNode
            => token.Parent?.FirstAncestorOrSelf(predicate);
    }
}
