using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.PopulateSwitch
{
    internal abstract class AbstractPopulateSwitchCodeFixProvider<TSwitchBlockSyntax, TExpressionSyntax, TSwitchSectionSyntax> : CodeFixProvider 
            where TSwitchBlockSyntax : SyntaxNode
            where TSwitchSectionSyntax : SyntaxNode
            where TExpressionSyntax : SyntaxNode
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(IDEDiagnosticIds.PopulateSwitchDiagnosticId);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                new MyCodeAction(
                    FeaturesResources.AddMissingSwitchCases,
                    c => AddMissingSwitchLabelsAsync(context)),
                context.Diagnostics);

            return SpecializedTasks.EmptyTask;
        }

        protected abstract TExpressionSyntax GetSwitchExpression(TSwitchBlockSyntax switchBlock);

        protected abstract int InsertPosition(SyntaxList<TSwitchSectionSyntax> sections);

        protected abstract SyntaxList<TSwitchSectionSyntax> GetSwitchSections(TSwitchBlockSyntax switchBlock);

        protected abstract TSwitchBlockSyntax NewSwitchNode(TSwitchBlockSyntax switchBlock, SyntaxList<TSwitchSectionSyntax> sections);

        protected abstract List<TExpressionSyntax> GetCaseLabels(TSwitchBlockSyntax switchBlock, out bool containsDefaultLabel);

        private async Task<Document> AddMissingSwitchLabelsAsync(CodeFixContext context)
        {
            var document = context.Document;
            var span = context.Span;
            var cancellationToken = context.CancellationToken;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var switchNode = (TSwitchBlockSyntax) root.FindNode(span);

            var enumType = (INamedTypeSymbol)model.GetTypeInfo(GetSwitchExpression(switchNode)).Type;

            bool containsDefaultLabel;
            var missingLabels = GetMissingLabels(switchNode, model, enumType, out containsDefaultLabel);

            var generator = SyntaxGenerator.GetGenerator(document);

            var switchExitStatement = generator.ExitSwitchStatement();
            var statements = new List<SyntaxNode> { switchExitStatement };

            var newSections = GetSwitchSections(switchNode);
            foreach (var label in missingLabels)
            {
                var caseLabel = generator.MemberAccessExpression(generator.TypeExpression(enumType), label);

                var section = (TSwitchSectionSyntax)generator.SwitchSection(caseLabel, new List<SyntaxNode> { switchExitStatement });

                // ensure that the new cases are above the last section if a default case exists, but below all other sections
                newSections = containsDefaultLabel
                    ? newSections.Insert(InsertPosition(newSections), section)
                    : newSections.Add(section);
            }

            if (!containsDefaultLabel)
            {
                newSections = newSections.Add((TSwitchSectionSyntax)generator.DefaultSwitchSection(statements));
            }

            var newNode = NewSwitchNode(switchNode, newSections)
                .WithAdditionalAnnotations(Formatter.Annotation, Simplifier.Annotation);

            var newRoot = root.ReplaceNode(switchNode, newNode);
            return document.WithSyntaxRoot(newRoot);
        }

        private List<string> GetMissingLabels(TSwitchBlockSyntax switchBlock, SemanticModel model, INamedTypeSymbol enumType, out bool containsDefaultLabel)
        {
            var caseLabels = GetCaseLabels(switchBlock, out containsDefaultLabel);

            var symbols = caseLabels.Select(label => model.GetSymbolInfo(label).Symbol).Where(symbol => symbol != null).ToList();

            return (from member in enumType.GetMembers()
                let field = member as IFieldSymbol
                where field != null && field.Type.SpecialType == SpecialType.None
                let memberExists = symbols.Any(symbol => symbol == member)
                where !memberExists
                select member.Name)
                .ToList();
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(title, createChangedDocument)
            {
            }
        }
    }
}
