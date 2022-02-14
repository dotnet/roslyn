// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;

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
                   EnforceOnBuildValues.UseCollectionInitializer,
                   CodeStyleOptions2.PreferCollectionInitializer,
                   new LocalizableResourceString(nameof(AnalyzersResources.Simplify_collection_initialization), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
                   new LocalizableResourceString(nameof(AnalyzersResources.Collection_initialization_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
        {
        }

        protected abstract ISyntaxFacts GetSyntaxFacts();
        protected abstract bool AreCollectionInitializersSupported(Compilation compilation);

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterCompilationStartAction(OnCompilationStart);

        private void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            if (!AreCollectionInitializersSupported(context.Compilation))
                return;

            var ienumerableType = context.Compilation.GetTypeByMetadataName(typeof(IEnumerable).FullName!);
            if (ienumerableType != null)
            {
                var syntaxKinds = GetSyntaxFacts().SyntaxKinds;

                using var matchKinds = TemporaryArray<TSyntaxKind>.Empty;
                matchKinds.Add(syntaxKinds.Convert<TSyntaxKind>(syntaxKinds.ObjectCreationExpression));
                if (syntaxKinds.ImplicitObjectCreationExpression != null)
                    matchKinds.Add(syntaxKinds.Convert<TSyntaxKind>(syntaxKinds.ImplicitObjectCreationExpression.Value));

                context.RegisterSyntaxNodeAction(
                    nodeContext => AnalyzeNode(nodeContext, ienumerableType),
                    matchKinds.ToImmutableAndClear());
            }
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context, INamedTypeSymbol ienumerableType)
        {
            var semanticModel = context.SemanticModel;
            var objectCreationExpression = (TObjectCreationExpressionSyntax)context.Node;
            var language = objectCreationExpression.Language;
            var cancellationToken = context.CancellationToken;

            var option = context.GetOption(CodeStyleOptions2.PreferCollectionInitializer, language);
            if (!option.Value)
            {
                // not point in analyzing if the option is off.
                return;
            }

            // Object creation can only be converted to collection initializer if it
            // implements the IEnumerable type.
            var objectType = context.SemanticModel.GetTypeInfo(objectCreationExpression, cancellationToken);
            if (objectType.Type == null || !objectType.Type.AllInterfaces.Contains(ienumerableType))
                return;

            var matches = UseCollectionInitializerAnalyzer<TExpressionSyntax, TStatementSyntax, TObjectCreationExpressionSyntax, TMemberAccessExpressionSyntax, TInvocationExpressionSyntax, TExpressionStatementSyntax, TVariableDeclaratorSyntax>.Analyze(
                semanticModel, GetSyntaxFacts(), objectCreationExpression, cancellationToken);

            if (matches == null || matches.Value.Length == 0)
                return;

            var containingStatement = objectCreationExpression.FirstAncestorOrSelf<TStatementSyntax>();
            if (containingStatement == null)
                return;

            var nodes = ImmutableArray.Create<SyntaxNode>(containingStatement).AddRange(matches.Value);
            var syntaxFacts = GetSyntaxFacts();
            if (syntaxFacts.ContainsInterleavedDirective(nodes, cancellationToken))
                return;

            var locations = ImmutableArray.Create(objectCreationExpression.GetLocation());

            context.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                objectCreationExpression.GetFirstToken().GetLocation(),
                option.Notification.Severity,
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
                CodeStyleOptions2.PreferCollectionInitializer_FadeOutCode, context.Node.Language);
            if (!fadeOutCode)
                return;

            var syntaxFacts = GetSyntaxFacts();

            foreach (var match in matches)
            {
                var expression = syntaxFacts.GetExpressionOfExpressionStatement(match);

                if (syntaxFacts.IsInvocationExpression(expression))
                {
                    var arguments = syntaxFacts.GetArgumentsOfInvocationExpression(expression);
                    var additionalUnnecessaryLocations = ImmutableArray.Create(
                        syntaxTree.GetLocation(TextSpan.FromBounds(match.SpanStart, arguments[0].SpanStart)),
                        syntaxTree.GetLocation(TextSpan.FromBounds(arguments.Last().FullSpan.End, match.Span.End)));

                    // Report the diagnostic at the first unnecessary location. This is the location where the code fix
                    // will be offered.
                    var location1 = additionalUnnecessaryLocations[0];

                    context.ReportDiagnostic(DiagnosticHelper.CreateWithLocationTags(
                        Descriptor,
                        location1,
                        ReportDiagnostic.Default,
                        additionalLocations: locations,
                        additionalUnnecessaryLocations: additionalUnnecessaryLocations));
                }
            }
        }
    }
}
