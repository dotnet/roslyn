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
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.PopulateSwitch
{
    internal abstract class AbstractPopulateSwitchCodeFixProvider<TSwitchBlockSyntax> : CodeFixProvider where TSwitchBlockSyntax : SyntaxNode
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(IDEDiagnosticIds.PopulateSwitchDiagnosticId);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var span = context.Span;
            var cancellationToken = context.CancellationToken;
            
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var node = GetSwitchStatementNode(root, span);

            context.RegisterCodeFix(
                new MyCodeAction(
                    FeaturesResources.AddMissingSwitchCases,
                    c => AddMissingSwitchLabelsAsync(document, root, (TSwitchBlockSyntax)node, cancellationToken)),
                context.Diagnostics);
        }

        protected abstract SyntaxNode GetSwitchExpression(TSwitchBlockSyntax switchBlock);

        protected abstract int InsertPosition(List<SyntaxNode> sections);

        protected abstract List<SyntaxNode> GetSwitchSections(TSwitchBlockSyntax switchBlock);

        protected abstract SyntaxNode NewSwitchNode(TSwitchBlockSyntax switchBlock, List<SyntaxNode> sections);

        protected abstract SyntaxNode GetSwitchStatementNode(SyntaxNode root, TextSpan span);

        protected abstract List<SyntaxNode> GetCaseLabels(TSwitchBlockSyntax switchBlock, out bool containsDefaultLabel);

        private async Task<Document> AddMissingSwitchLabelsAsync(Document document, SyntaxNode root, TSwitchBlockSyntax switchNode, CancellationToken cancellationToken)
        {
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

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
