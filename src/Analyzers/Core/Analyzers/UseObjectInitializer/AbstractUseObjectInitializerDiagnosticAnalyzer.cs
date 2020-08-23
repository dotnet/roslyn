// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.UseObjectInitializer
{
    internal abstract partial class AbstractUseObjectInitializerDiagnosticAnalyzer<
        TSyntaxKind,
        TExpressionSyntax,
        TStatementSyntax,
        TObjectCreationExpressionSyntax,
        TMemberAccessExpressionSyntax,
        TAssignmentStatementSyntax,
        TVariableDeclaratorSyntax>
        : AbstractBuiltInCodeStyleDiagnosticAnalyzer
        where TSyntaxKind : struct
        where TExpressionSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
        where TObjectCreationExpressionSyntax : TExpressionSyntax
        where TMemberAccessExpressionSyntax : TExpressionSyntax
        where TAssignmentStatementSyntax : TStatementSyntax
        where TVariableDeclaratorSyntax : SyntaxNode
    {
        protected abstract bool FadeOutOperatorToken { get; }

        protected AbstractUseObjectInitializerDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseObjectInitializerDiagnosticId,
                   CodeStyleOptions2.PreferObjectInitializer,
                   new LocalizableResourceString(nameof(AnalyzersResources.Simplify_object_initialization), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
                   new LocalizableResourceString(nameof(AnalyzersResources.Object_initialization_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
        {
        }

        protected override void InitializeWorker(AnalysisContext context)
        {
            var syntaxKinds = GetSyntaxFacts().SyntaxKinds;
            context.RegisterSyntaxNodeAction(
                AnalyzeNode, syntaxKinds.Convert<TSyntaxKind>(syntaxKinds.ObjectCreationExpression));
        }

        protected abstract bool AreObjectInitializersSupported(SyntaxNodeAnalysisContext context);

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (!AreObjectInitializersSupported(context))
            {
                return;
            }

            var objectCreationExpression = (TObjectCreationExpressionSyntax)context.Node;
            var language = objectCreationExpression.Language;
            var option = context.GetOption(CodeStyleOptions2.PreferObjectInitializer, language);
            if (!option.Value)
            {
                // not point in analyzing if the option is off.
                return;
            }

            var syntaxFacts = GetSyntaxFacts();
            var matches = ObjectCreationExpressionAnalyzer<TExpressionSyntax, TStatementSyntax, TObjectCreationExpressionSyntax, TMemberAccessExpressionSyntax, TAssignmentStatementSyntax, TVariableDeclaratorSyntax>.Analyze(
                context.SemanticModel, syntaxFacts, objectCreationExpression, context.CancellationToken);

            if (matches == null || matches.Value.Length == 0)
            {
                return;
            }

            var containingStatement = objectCreationExpression.FirstAncestorOrSelf<TStatementSyntax>();
            if (containingStatement == null)
            {
                return;
            }

            var nodes = ImmutableArray.Create<SyntaxNode>(containingStatement).AddRange(matches.Value.Select(m => m.Statement));
            if (syntaxFacts.ContainsInterleavedDirective(nodes, context.CancellationToken))
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
            ImmutableArray<Match<TExpressionSyntax, TStatementSyntax, TMemberAccessExpressionSyntax, TAssignmentStatementSyntax>> matches,
            ImmutableArray<Location> locations)
        {
            var syntaxTree = context.Node.SyntaxTree;

            var fadeOutCode = context.GetOption(
                CodeStyleOptions2.PreferObjectInitializer_FadeOutCode, context.Node.Language);
            if (!fadeOutCode)
            {
                return;
            }

            var syntaxFacts = GetSyntaxFacts();

            foreach (var match in matches)
            {
                var end = FadeOutOperatorToken
                    ? syntaxFacts.GetOperatorTokenOfMemberAccessExpression(match.MemberAccessExpression).Span.End
                    : syntaxFacts.GetExpressionOfMemberAccessExpression(match.MemberAccessExpression).Span.End;

                var location1 = Location.Create(syntaxTree, TextSpan.FromBounds(
                    match.MemberAccessExpression.SpanStart, end));

                if (match.Statement.Span.End > match.Initializer.FullSpan.End)
                {
                    context.ReportDiagnostic(DiagnosticHelper.CreateWithLocationTags(
                        Descriptor,
                        location1,
                        ReportDiagnostic.Default,
                        additionalLocations: locations,
                        additionalUnnecessaryLocations: ImmutableArray.Create(
                            syntaxTree.GetLocation(TextSpan.FromBounds(match.Initializer.FullSpan.End, match.Statement.Span.End)))));
                }
                else
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Descriptor, location1, additionalLocations: locations));
                }
            }
        }

        protected abstract ISyntaxFacts GetSyntaxFacts();

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
    }
}
