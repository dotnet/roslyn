// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.ConditionalExpressionInStringInterpolation
{
    //PredefinedCodeFixProviderNames
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddParenthesisAroundConditionalExpressionInInterpolatedString), Shared]
    internal partial class CSharpAddParenthesisAroundConditionalExpressionInInterpolatedStringCodeFixProvider : CodeFixProvider
    {
        private const string ERR_ConditionalInInterpolation = "CS8361"; //A conditional expression cannot be used directly in a string interpolation because the ':' ends the interpolation.Parenthesize the conditional expression.

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ERR_ConditionalInInterpolation);

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var token = root.FindToken(diagnosticSpan.Start);
            var conditionalExpressionSyntax = token.GetAncestor<ConditionalExpressionSyntax>();
            var interpolationSyntax = token.GetAncestor<InterpolationSyntax>();
            if (conditionalExpressionSyntax != null && interpolationSyntax!=null)
            {
                context.RegisterCodeFix(new AddParenthesisCodeAction(context.Document, conditionalExpressionSyntax, interpolationSyntax), context.Diagnostics);
            }
        }

    }
}
