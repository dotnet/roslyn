// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.LanguageService;

internal static class IHeaderFactsExtensions
{
    /// <summary>
    /// Checks if the position is on the header of a type (from the start of the type up through it's name).
    /// </summary>
    public static bool IsOnTypeHeader(this IHeaderFacts headerFacts, SyntaxNode root, int position, [NotNullWhen(true)] out SyntaxNode? typeDeclaration)
        => headerFacts.IsOnTypeHeader(root, position, fullHeader: false, out typeDeclaration);
}
