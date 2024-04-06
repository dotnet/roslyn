// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal abstract partial class StructuredTriviaSyntax : CSharpSyntaxNode
    {
        internal StructuredTriviaSyntax(SyntaxKind kind, DiagnosticInfo[] diagnostics = null, SyntaxAnnotation[] annotations = null)
            : base(kind, diagnostics, annotations)
        {
            SetFlags(NodeFlags.ContainsStructuredTrivia);

            if (this.Kind == SyntaxKind.SkippedTokensTrivia)
            {
                SetFlags(NodeFlags.ContainsSkippedText);
            }
        }

        public sealed override bool IsStructuredTrivia => true;
    }
}
