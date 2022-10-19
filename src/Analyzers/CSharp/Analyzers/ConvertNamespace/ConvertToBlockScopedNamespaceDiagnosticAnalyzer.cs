// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
#endif

namespace Microsoft.CodeAnalysis.CSharp.ConvertNamespace
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class ConvertToBlockScopedNamespaceDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public ConvertToBlockScopedNamespaceDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseBlockScopedNamespaceDiagnosticId,
                   EnforceOnBuildValues.UseBlockScopedNamespace,
                   CSharpCodeStyleOptions.NamespaceDeclarations,
                   LanguageNames.CSharp,
                   new LocalizableResourceString(nameof(CSharpAnalyzersResources.Convert_to_block_scoped_namespace), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
        {
        }

        public sealed override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeNamespace, SyntaxKind.FileScopedNamespaceDeclaration);

        private void AnalyzeNamespace(SyntaxNodeAnalysisContext context)
        {
            var options = context.Options;
            var namespaceDeclaration = (FileScopedNamespaceDeclarationSyntax)context.Node;
            var syntaxTree = namespaceDeclaration.SyntaxTree;

            var cancellationToken = context.CancellationToken;
            var optionSet = options.GetAnalyzerOptionSet(syntaxTree, cancellationToken);

            var diagnostic = AnalyzeNamespace(optionSet, namespaceDeclaration);
            if (diagnostic != null)
                context.ReportDiagnostic(diagnostic);
        }

        private Diagnostic? AnalyzeNamespace(OptionSet optionSet, FileScopedNamespaceDeclarationSyntax declaration)
        {
            var tree = declaration.SyntaxTree;
            var option = optionSet.GetOption(CSharpCodeStyleOptions.NamespaceDeclarations);

            if (!ConvertNamespaceAnalysis.CanOfferUseBlockScoped(optionSet, declaration, forAnalyzer: true))
                return null;

            // if the diagnostic is hidden, show it anywhere from the `namespace` keyword through the name.
            // otherwise, if it's not hidden, just squiggle the name.
            var severity = option.Notification.Severity;
            var diagnosticLocation = severity.WithDefaultSeverity(DiagnosticSeverity.Hidden) != ReportDiagnostic.Hidden
                ? declaration.Name.GetLocation()
                : tree.GetLocation(TextSpan.FromBounds(declaration.SpanStart, declaration.SemicolonToken.Span.End));

            return DiagnosticHelper.Create(
                this.Descriptor,
                diagnosticLocation,
                severity,
                ImmutableArray.Create(declaration.GetLocation()),
                ImmutableDictionary<string, string?>.Empty);
        }
    }
}
