// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static class SymbolDisplayPartExtensions
{
    extension(IEnumerable<SymbolDisplayPart> parts)
    {
        public string GetFullText()
        => string.Join(string.Empty, parts.Select(p => p.ToString()));
    }

    extension(IList<SymbolDisplayPart> parts)
    {
        public void AddLineBreak(string text = "\r\n")
        => parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.LineBreak, null, text));

        public void AddMethodName(string text)
            => parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.MethodName, null, text));

        public void AddPunctuation(string text)
            => parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, null, text));

        public void AddSpace(string text = " ")
            => parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Space, null, text));

        public void AddText(string text)
            => parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Text, null, text));
    }
}
