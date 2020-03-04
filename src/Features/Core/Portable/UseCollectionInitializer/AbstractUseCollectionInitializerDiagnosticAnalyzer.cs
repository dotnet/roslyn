﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseCollectionInitializer
{
    internal abstract partial class AbstractUseCollectionInitializerDiagnosticAnalyzer<
        TSyntaxKind,
        TExpressionSyntax,
        TStatementSyntax,
        TObjectCreationExpressionSyntax,
        TMemberAccessExpressionSyntax,
        TInvocationExpressionSyntax,
        TExpressionStatementSyntax,
        TVariableDeclaratorSyntax>
        : AbstractBuiltInCodeStyleDiagnosticAnalyzer
        where TSyntaxKind : struct
        where TExpressionSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
        where TObjectCreationExpressionSyntax : TExpressionSyntax
        where TMemberAccessExpressionSyntax : TExpressionSyntax
        where TInvocationExpressionSyntax : TExpressionSyntax
        where TExpressionStatementSyntax : TStatementSyntax
        where TVariableDeclaratorSyntax : SyntaxNode
    {
        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected AbstractUseCollectionInitializerDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseCollectionInitializerDiagnosticId,
                   CodeStyleOptions.PreferCollectionInitializer,
                   new LocalizableResourceString(nameof(FeaturesResources.Simplify_collection_initialization), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Collection_initialization_can_be_simplified), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterCompilationStartAction(OnCompilationStart);

        private void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            var ienumerableType = context.Compilation.GetTypeByMetadataName(typeof(IEnumerable).FullName);
            if (ienumerableType != null)
            {
                var syntaxKinds = GetSyntaxFacts().SyntaxKinds;
                context.RegisterSyntaxNodeAction(
                    nodeContext => AnalyzeNode(nodeContext, ienumerableType),
                    syntaxKinds.Convert<TSyntaxKind>(syntaxKinds.ObjectCreationExpression));
            }
        }

        protected abstract bool AreCollectionInitializersSupported(SyntaxNodeAnalysisContext context);

        private void AnalyzeNode(SyntaxNodeAnalysisContext context, INamedTypeSymbol ienumerableType)
        {
            if (!AreCollectionInitializersSupported(context))
            {
                return;
            }

            var semanticModel = context.SemanticModel;
            var objectCreationExpression = (TObjectCreationExpressionSyntax)context.Node;
            var language = objectCreationExpression.Language;
            var cancellationToken = context.CancellationToken;

            var option = context.GetOption(CodeStyleOptions.PreferCollectionInitializer, language);
            if (!option.Value)
            {
                // not point in analyzing if the option is off.
                return;
            }

            // Object creation can only be converted to collection initializer if it
            // implements the IEnumerable type.
            var objectType = context.SemanticModel.GetTypeInfo(objectCreationExpression, cancellationToken);
            if (objectType.Type == null || !objectType.Type.AllInterfaces.Contains(ienumerableType))
            {
                return;
            }

            var matches = ObjectCreationExpressionAnalyzer<TExpressionSyntax, TStatementSyntax, TObjectCreationExpressionSyntax, TMemberAccessExpressionSyntax, TInvocationExpressionSyntax, TExpressionStatementSyntax, TVariableDeclaratorSyntax>.Analyze(
                semanticModel, GetSyntaxFacts(), objectCreationExpression, cancellationToken);

            if (matches == null || matches.Value.Length == 0)
            {
                return;
            }

            var containingStatement = objectCreationExpression.FirstAncestorOrSelf<TStatementSyntax>();
            if (containingStatement == null)
            {
                return;
            }

            var nodes = ImmutableArray.Create<SyntaxNode>(containingStatement).AddRange(matches.Value);
            var syntaxFacts = GetSyntaxFacts();
            if (syntaxFacts.ContainsInterleavedDirective(nodes, cancellationToken))
            {
                return;
            }

            var locations = ImmutableArray.Create(objectCreationExpression.GetLocation());

            var severity = option.Notification.Severity;
            context.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                objectCreationExpression.GetLocation(),
                severity,
                additionalLocations: locations,
                properties: null));

            FadeOutCode(context, matches.Value, locations);
        }

        private void FadeOutCode(
            SyntaxNodeAnalysisContext context,
            ImmutableArray<TExpressionStatementSyntax> matches,
            ImmutableArray<Location> locations)
        {
            var syntaxTree = context.Node.SyntaxTree;

            var fadeOutCode = context.GetOption(
                CodeStyleOptions.PreferCollectionInitializer_FadeOutCode, context.Node.Language);
            if (!fadeOutCode)
            {
                return;
            }

            var syntaxFacts = GetSyntaxFacts();

            foreach (var match in matches)
            {
                var expression = syntaxFacts.GetExpressionOfExpressionStatement(match);

                if (syntaxFacts.IsInvocationExpression(expression))
                {
                    var arguments = syntaxFacts.GetArgumentsOfInvocationExpression(expression);
                    var location1 = Location.Create(syntaxTree, TextSpan.FromBounds(
                        match.SpanStart, arguments[0].SpanStart));

                    RoslynDebug.AssertNotNull(UnnecessaryWithSuggestionDescriptor);
                    context.ReportDiagnostic(Diagnostic.Create(
                        UnnecessaryWithSuggestionDescriptor, location1, additionalLocations: locations));

                    RoslynDebug.AssertNotNull(UnnecessaryWithoutSuggestionDescriptor);
                    context.ReportDiagnostic(Diagnostic.Create(
                        UnnecessaryWithoutSuggestionDescriptor,
                        Location.Create(syntaxTree, TextSpan.FromBounds(
                            arguments.Last().FullSpan.End,
                            match.Span.End)),
                        additionalLocations: locations));
                }
            }
        }

        protected abstract ISyntaxFacts GetSyntaxFacts();
    }
}
