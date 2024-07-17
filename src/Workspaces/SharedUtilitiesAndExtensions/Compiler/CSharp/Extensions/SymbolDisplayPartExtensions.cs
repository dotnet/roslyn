// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

internal static class SymbolDisplayPartExtensions
{
    public static SymbolDisplayPart MassageErrorTypeNames(this SymbolDisplayPart part, string? replacement = null)
    {
        if (part.Kind == SymbolDisplayPartKind.ErrorTypeName)
        {
            var text = part.ToString();
            if (text == string.Empty)
            {
                return replacement == null
                    ? new SymbolDisplayPart(SymbolDisplayPartKind.Keyword, null, "object")
                    : new SymbolDisplayPart(SymbolDisplayPartKind.Text, null, replacement);
            }

            if (SyntaxFacts.GetKeywordKind(text) != SyntaxKind.None)
            {
                return new SymbolDisplayPart(SymbolDisplayPartKind.ErrorTypeName, null, string.Format("@{0}", text));
            }
        }

        return part;
    }
}
