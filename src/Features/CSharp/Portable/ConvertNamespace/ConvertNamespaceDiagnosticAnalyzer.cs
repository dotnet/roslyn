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

namespace Microsoft.CodeAnalysis.CSharp.ConvertNamespace
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class ConvertNamespaceDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_useRegularNamespaceDescriptor = CreateDescriptorWithId(
            IDEDiagnosticIds.UseRegularNamespaceDiagnosticId,
            EnforceOnBuildValues.UseRegularNamespace,
            new LocalizableResourceString(nameof(CSharpFeaturesResources.Convert_to_regular_namespace), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources)));

        private static readonly DiagnosticDescriptor s_useFileScopedNamespaceDescriptor = CreateDescriptorWithId(
            IDEDiagnosticIds.UseFileScopedNamespaceDiagnosticId,
            EnforceOnBuildValues.UseFileScopedNamespace,
            new LocalizableResourceString(nameof(CSharpFeaturesResources.Convert_to_file_scoped_namespace), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources)));

        private static readonly ImmutableDictionary<DiagnosticDescriptor, ILanguageSpecificOption> s_descriptors =
            ImmutableDictionary<DiagnosticDescriptor, ILanguageSpecificOption>.Empty
                .Add(s_useRegularNamespaceDescriptor, CSharpCodeStyleOptions.PreferFileScopedNamespace)
                .Add(s_useFileScopedNamespaceDescriptor, CSharpCodeStyleOptions.PreferFileScopedNamespace);

        public ConvertNamespaceDiagnosticAnalyzer()
            : base(s_descriptors, LanguageNames.CSharp)
        {
        }

        public sealed override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeNamespace, SyntaxKind.NamespaceDeclaration, SyntaxKind.FileScopedNamespaceDeclaration);

        private void AnalyzeNamespace(SyntaxNodeAnalysisContext context)
        {
            var options = context.Options;
            var namespaceDeclaration = (BaseNamespaceDeclarationSyntax)context.Node;
            var syntaxTree = namespaceDeclaration.SyntaxTree;

            var cancellationToken = context.CancellationToken;
            var root = (CompilationUnitSyntax)syntaxTree.GetRoot(cancellationToken);

            var optionSet = options.GetAnalyzerOptionSet(syntaxTree, cancellationToken);

            var diagnostic = AnalyzeNamespace(optionSet, root, namespaceDeclaration);
            if (diagnostic != null)
                context.ReportDiagnostic(diagnostic);
        }

        private static Diagnostic? AnalyzeNamespace(OptionSet optionSet, CompilationUnitSyntax root, BaseNamespaceDeclarationSyntax declaration)
        {
            var tree = declaration.SyntaxTree;
            var preferFileScopedNamespace = optionSet.GetOption(CSharpCodeStyleOptions.PreferFileScopedNamespace);
            var severity = preferFileScopedNamespace.Notification.Severity;

            if (ConvertNamespaceHelper.CanOfferUseRegular(optionSet, declaration, forAnalyzer: true))
            {
                var location = GetDiagnosticLocation(declaration, tree, severity);

                var additionalLocations = ImmutableArray.Create(declaration.GetLocation());
                return DiagnosticHelper.Create(
                    s_useRegularNamespaceDescriptor, location, severity,
                    additionalLocations, ImmutableDictionary<string, string>.Empty);
            }

            if (ConvertNamespaceHelper.CanOfferUseFileScoped(optionSet, root, declaration, forAnalyzer: true))
            {
                var location = GetDiagnosticLocation(declaration, tree, severity);

                var additionalLocations = ImmutableArray.Create(declaration.GetLocation());
                return DiagnosticHelper.Create(
                    s_useFileScopedNamespaceDescriptor, location, severity,
                    additionalLocations, ImmutableDictionary<string, string>.Empty);
            }

            return null;
        }

        private static Location GetDiagnosticLocation(BaseNamespaceDeclarationSyntax declaration, SyntaxTree tree, ReportDiagnostic severity)
        {
            // if the diagnostic is hidden, show it anywhere from the `namespace` keyword through the name.
            // otherwise, if it's not hidden, just squiggle the name.
            return severity.WithDefaultSeverity(DiagnosticSeverity.Hidden) == ReportDiagnostic.Hidden
                ? tree.GetLocation(TextSpan.FromBounds(declaration.SpanStart, declaration.Name.Span.End))
                : declaration.Name.GetLocation();
        }
    }
}
