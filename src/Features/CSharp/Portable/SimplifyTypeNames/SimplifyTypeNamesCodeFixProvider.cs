// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.SimplifyTypeNames;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.SimplifyTypeNames;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SimplifyTypeNames
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.SimplifyNames), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.RemoveUnnecessaryCast)]
    internal partial class SimplifyTypeNamesCodeFixProvider : AbstractSimplifyTypeNamesCodeFixProvider<SyntaxKind, CSharpSimplifierOptions>
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public SimplifyTypeNamesCodeFixProvider()
            : base(new CSharpSimplifyTypeNamesDiagnosticAnalyzer())
        {
        }

        protected override string GetTitle(string diagnosticId, string nodeText)
        {
            switch (diagnosticId)
            {
                case IDEDiagnosticIds.SimplifyNamesDiagnosticId:
                case IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId:
                    return string.Format(CSharpFeaturesResources.Simplify_name_0, nodeText);

                case IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId:
                    return string.Format(CSharpFeaturesResources.Simplify_member_access_0, nodeText);

                default:
                    throw ExceptionUtilities.UnexpectedValue(diagnosticId);
            }
        }

        protected override SyntaxNode AddSimplificationAnnotationTo(SyntaxNode expressionSyntax)
        {
            // Add the DoNotAllowVarAnnotation annotation.  All the code fixer
            // does is pass the tagged node to the simplifier.  And we do *not*
            // ever want the simplifier to produce 'var' in the 'Simplify type
            // names' fixer.  only the 'Use var' fixer should produce 'var'.
            var annotatedexpressionSyntax = expressionSyntax.WithAdditionalAnnotations(
                Simplifier.Annotation, Formatter.Annotation, DoNotAllowVarAnnotation.Annotation);

            if (annotatedexpressionSyntax.Kind() is SyntaxKind.IsExpression or SyntaxKind.AsExpression)
            {
                var right = ((BinaryExpressionSyntax)annotatedexpressionSyntax).Right;
                annotatedexpressionSyntax = annotatedexpressionSyntax.ReplaceNode(right, right.WithAdditionalAnnotations(Simplifier.Annotation));
            }

            return annotatedexpressionSyntax;
        }
    }
}
