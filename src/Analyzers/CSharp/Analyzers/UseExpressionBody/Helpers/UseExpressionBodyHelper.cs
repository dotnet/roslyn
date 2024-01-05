// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    internal abstract class UseExpressionBodyHelper
    {
        public abstract Option2<CodeStyleOption2<ExpressionBodyPreference>> Option { get; }
        public abstract LocalizableString UseExpressionBodyTitle { get; }
        public abstract LocalizableString UseBlockBodyTitle { get; }
        public abstract string DiagnosticId { get; }
        public abstract EnforceOnBuild EnforceOnBuild { get; }
        public abstract ImmutableArray<SyntaxKind> SyntaxKinds { get; }

        public abstract CodeStyleOption2<ExpressionBodyPreference> GetExpressionBodyPreference(CSharpCodeGenerationOptions options);
        public abstract BlockSyntax? GetBody(SyntaxNode declaration);
        public abstract ArrowExpressionClauseSyntax? GetExpressionBody(SyntaxNode declaration);
        public abstract bool IsRelevantDeclarationNode(SyntaxNode node);

        public abstract bool CanOfferUseExpressionBody(CodeStyleOption2<ExpressionBodyPreference> preference, SyntaxNode declaration, bool forAnalyzer, CancellationToken cancellationToken);
        public abstract bool CanOfferUseBlockBody(CodeStyleOption2<ExpressionBodyPreference> preference, SyntaxNode declaration, bool forAnalyzer, out bool fixesError, [NotNullWhen(true)] out ArrowExpressionClauseSyntax? expressionBody);
        public abstract SyntaxNode Update(SemanticModel semanticModel, SyntaxNode declaration, bool useExpressionBody, CancellationToken cancellationToken);

        public abstract Location GetDiagnosticLocation(SyntaxNode declaration);

        public static readonly ImmutableArray<UseExpressionBodyHelper> Helpers =
            ImmutableArray.Create<UseExpressionBodyHelper>(
                UseExpressionBodyForConstructorsHelper.Instance,
                UseExpressionBodyForConversionOperatorsHelper.Instance,
                UseExpressionBodyForIndexersHelper.Instance,
                UseExpressionBodyForMethodsHelper.Instance,
                UseExpressionBodyForOperatorsHelper.Instance,
                UseExpressionBodyForPropertiesHelper.Instance,
                UseExpressionBodyForAccessorsHelper.Instance,
                UseExpressionBodyForLocalFunctionHelper.Instance);
    }
}
