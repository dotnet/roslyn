// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.SimplifyTypeNames;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.SimplifyTypeNames;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SimplifyTypeNames
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.SimplifyNames), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.RemoveUnnecessaryCast)]
    internal partial class SimplifyTypeNamesCodeFixProvider : AbstractSimplifyTypeNamesCodeFixProvider
    {
        protected override bool IsCandidate(SyntaxNode node)
            => CSharpSimplifyTypeNamesDiagnosticAnalyzer.IsCandidate(node);

        protected override bool CanSimplifyTypeNameExpression(SemanticModel model, SyntaxNode node, OptionSet optionSet, out TextSpan issueSpan, out string diagnosticId, CancellationToken cancellationToken)
            => CSharpSimplifyTypeNamesDiagnosticAnalyzer.CanSimplifyTypeNameExpression(model, node, optionSet, out issueSpan, out diagnosticId, cancellationToken);

        protected override string GetTitle(string diagnosticId, string nodeText)
        {
            switch (diagnosticId)
            {
                case IDEDiagnosticIds.SimplifyNamesDiagnosticId:
                case IDEDiagnosticIds.PreferIntrinsicPredefinedTypeInDeclarationsDiagnosticId:
                    return string.Format(CSharpFeaturesResources.Simplify_name_0, nodeText);

                case IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId:
                case IDEDiagnosticIds.PreferIntrinsicPredefinedTypeInMemberAccessDiagnosticId:
                    return string.Format(CSharpFeaturesResources.Simplify_member_access_0, nodeText);

                case IDEDiagnosticIds.RemoveQualificationDiagnosticId:
                    return CSharpFeaturesResources.Remove_this_qualification;

                default:
                    throw ExceptionUtilities.UnexpectedValue(diagnosticId);
            }
        }

        protected override SyntaxNode AddSimplificationAnnotationTo(SyntaxNode expressionSyntax)
        {
            var annotatedexpressionSyntax = expressionSyntax.WithAdditionalAnnotations(Simplifier.Annotation, Formatter.Annotation);

            if (annotatedexpressionSyntax.Kind() == SyntaxKind.IsExpression || annotatedexpressionSyntax.Kind() == SyntaxKind.AsExpression)
            {
                var right = ((BinaryExpressionSyntax)annotatedexpressionSyntax).Right;
                annotatedexpressionSyntax = annotatedexpressionSyntax.ReplaceNode(right, right.WithAdditionalAnnotations(Simplifier.Annotation));
            }

            return annotatedexpressionSyntax;
        }
    }
}
