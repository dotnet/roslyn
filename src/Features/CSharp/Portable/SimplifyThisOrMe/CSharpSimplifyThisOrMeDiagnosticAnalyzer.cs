// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SimplifyThisOrMe;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.SimplifyThisOrMe
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpSimplifyThisOrMeDiagnosticAnalyzer
        : AbstractSimplifyThisOrMeDiagnosticAnalyzer<
            SyntaxKind,
            ExpressionSyntax,
            ThisExpressionSyntax,
            MemberAccessExpressionSyntax>
    {
        public CSharpSimplifyThisOrMeDiagnosticAnalyzer()
            : base(ImmutableArray.Create(SyntaxKind.SimpleMemberAccessExpression))
        {
        }

        protected override string GetLanguageName()
            => LanguageNames.CSharp;

        protected override ISyntaxFactsService GetSyntaxFactsService()
            => CSharpSyntaxFactsService.Instance;

        protected override bool CanSimplifyTypeNameExpression(
            SemanticModel model, MemberAccessExpressionSyntax node, OptionSet optionSet,
            out TextSpan issueSpan, CancellationToken cancellationToken)
        {
            return node.TryReduceOrSimplifyExplicitName(model, out _, out issueSpan, optionSet, cancellationToken);
        }
    }
}
