// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    public struct RudeEditDiagnostic
    {
        internal readonly RudeEditKind Kind;
        internal readonly TextSpan Span;
        internal readonly ushort SyntaxKind;
        internal readonly string[] Arguments;

        internal RudeEditDiagnostic(RudeEditKind kind, TextSpan span, SyntaxNode node = null, string[] arguments = null)
        {
            this.Kind = kind;
            this.Span = span;
            this.SyntaxKind = (ushort)(node != null ? node.RawKind : 0);
            this.Arguments = arguments;
        }

        internal Diagnostic ToDiagnostic(SyntaxTree tree)
        {
            var descriptor = RudeEditDiagnosticDescriptors.GetDescriptor(this.Kind);
            return Diagnostic.Create(descriptor, tree.GetLocation(this.Span), Arguments);
        }
    }
}
