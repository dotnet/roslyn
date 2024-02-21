// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Test.Utilities
{
    internal sealed partial class SourceWithMarkedNodes
    {
        internal readonly struct MarkedSpan
        {
            public readonly TextSpan MarkedSyntax;
            public readonly TextSpan MatchedSpan;
            public readonly string TagName;
            public readonly int SyntaxKind;
            public readonly int Id;
            public readonly int ParentId;

            public MarkedSpan(TextSpan markedSyntax, TextSpan matchedSpan, string tagName, int syntaxKind, int id, int parentId)
            {
                MarkedSyntax = markedSyntax;
                MatchedSpan = matchedSpan;
                TagName = tagName;
                SyntaxKind = syntaxKind;
                Id = id;
                ParentId = parentId;
            }
        }
    }
}
