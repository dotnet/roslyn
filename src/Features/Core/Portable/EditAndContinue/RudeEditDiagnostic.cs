// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
