// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.RemoveUnnecessaryCast;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.RemoveUnnecessaryCast
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpRemoveUnnecessaryCastDiagnosticAnalyzer : RemoveUnnecessaryCastDiagnosticAnalyzerBase<SyntaxKind>
    {
        private static readonly ImmutableArray<SyntaxKind> s_kindsOfInterest = ImmutableArray.Create(SyntaxKind.CastExpression);

        public override ImmutableArray<SyntaxKind> SyntaxKindsOfInterest
        {
            get
            {
                return s_kindsOfInterest;
            }
        }

        protected override bool IsUnnecessaryCast(SemanticModel model, SyntaxNode node, CancellationToken cancellationToken)
        {
            var cast = (CastExpressionSyntax)node;
            return cast.IsUnnecessaryCast(model, cancellationToken);
        }

        protected override TextSpan GetDiagnosticSpan(SyntaxNode node)
        {
            var cast = (CastExpressionSyntax)node;
            return TextSpan.FromBounds(cast.OpenParenToken.SpanStart, cast.CloseParenToken.Span.End);
        }
    }
}
