// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal readonly struct RudeEditDiagnostic
    {
        public readonly RudeEditKind Kind;
        public readonly TextSpan Span;
        public readonly ushort SyntaxKind;
        public readonly string[] Arguments;

        internal RudeEditDiagnostic(RudeEditKind kind, TextSpan span, SyntaxNode node = null, string[] arguments = null)
        {
            Kind = kind;
            Span = span;
            SyntaxKind = (ushort)(node != null ? node.RawKind : 0);
            Arguments = arguments;
        }

        internal Diagnostic ToDiagnostic(SyntaxTree tree)
        {
            var descriptor = EditAndContinueDiagnosticDescriptors.GetDescriptor(Kind);
            return Diagnostic.Create(descriptor, tree.GetLocation(Span), Arguments);
        }
    }
}
