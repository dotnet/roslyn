// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal struct RudeEditDiagnostic
    {
        public readonly RudeEditKind Kind;
        public readonly TextSpan Span;
        public readonly ushort SyntaxKind;
        public readonly string[] Arguments;

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
