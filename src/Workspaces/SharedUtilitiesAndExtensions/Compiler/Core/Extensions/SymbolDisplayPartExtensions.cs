// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class SymbolDisplayPartExtensions
    {
        public static string GetFullText(this IEnumerable<SymbolDisplayPart> parts)
            => string.Join(string.Empty, parts.Select(p => p.ToString()));

        public static void AddLineBreak(this IList<SymbolDisplayPart> parts, string text = "\r\n")
            => parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.LineBreak, null, text));

        public static void AddMethodName(this IList<SymbolDisplayPart> parts, string text)
            => parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.MethodName, null, text));

        public static void AddPunctuation(this IList<SymbolDisplayPart> parts, string text)
            => parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, null, text));

        public static void AddSpace(this IList<SymbolDisplayPart> parts, string text = " ")
            => parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Space, null, text));

        public static void AddText(this IList<SymbolDisplayPart> parts, string text)
            => parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Text, null, text));
    }
}
