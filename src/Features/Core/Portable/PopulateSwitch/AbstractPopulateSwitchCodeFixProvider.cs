using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.PopulateSwitch
{
    internal abstract class AbstractPopulateSwitchCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(IDEDiagnosticIds.PopulateSwitchDiagnosticId);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var span = context.Span;
            var cancellationToken = context.CancellationToken;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var node = GetSwitchStatementNode(root, span);
            if (node == null)
            {
                return;
            }

            context.RegisterCodeFix(
                new MyCodeAction(
                    FeaturesResources.AddSwitchLabels,
                    c => AddMissingSwitchLabelsAsync(model, document, root, node)),
                context.Diagnostics);
        }

        protected abstract SyntaxNode GetSwitchExpression(SyntaxNode node);

        protected abstract List<string> GetMissingLabels(SyntaxNode node, SemanticModel model, INamedTypeSymbol enumType,
            out bool containsDefaultLabel);

        protected abstract int InsertPosition(List<SyntaxNode> sections);

        protected abstract List<SyntaxNode> GetSwitchSections(SyntaxNode node);

        protected abstract SyntaxNode NewSwitchNode(SyntaxNode node, List<SyntaxNode> sections);

        protected abstract SyntaxNode GetSwitchStatementNode(SyntaxNode root, TextSpan span);

        private Task<Document> AddMissingSwitchLabelsAsync(SemanticModel model, Document document, SyntaxNode root, SyntaxNode switchNode)
        {
            var enumType = (INamedTypeSymbol)model.GetTypeInfo(GetSwitchExpression(switchNode)).Type;
            var fullyQualifiedEnumType = enumType.ToDisplayString();

            bool containsDefaultLabel;
            var missingLabels = GetMissingLabels(switchNode, model, enumType, out containsDefaultLabel);

            var generator = SyntaxGenerator.GetGenerator(document);

            var switchExitStatement = generator.ExitSwitchStatement();
            var statements = new List<SyntaxNode> { switchExitStatement };

            var newSections = GetSwitchSections(switchNode);
            foreach (var label in missingLabels)
            {
                var caseLabel = generator.DottedName($"{fullyQualifiedEnumType}.{label}");

                var section = generator.SwitchSection(caseLabel, new List<SyntaxNode> { switchExitStatement });

                // ensure that the new cases are above the last section if a default case exists, but below all other sections
                if (containsDefaultLabel)
                {
                    newSections.Insert(InsertPosition(newSections), section);
                }
                else
                {
                    newSections.Add(section);
                }
            }

            if (!containsDefaultLabel)
            {
                newSections.Add(generator.DefaultSwitchSection(statements));
            }

            var newNode = NewSwitchNode(switchNode, newSections);

            var newRoot = root.ReplaceNode(switchNode, newNode);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
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
