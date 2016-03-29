using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.PopulateSwitch
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddLabelsToSwitch), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.ImplementInterface)]
    internal partial class PopulateSwitchCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(IDEDiagnosticIds.PopulateSwitchDiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return PopulateSwitchFixAllProvider.Instance;
        }

        private static SwitchStatementSyntax GetSwitchStatementNode(SyntaxNode root, TextSpan span)
        {
            var token = root.FindToken(span.Start);
            if (!token.Span.IntersectsWith(span))
            {
                return null;
            }

            return (SwitchStatementSyntax)root.FindNode(span);
        }

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
                    CSharpFeaturesResources.AddSwitchLabels,
                    (c) => AddMissingSwitchLabelsAsync(model, document, root, node)),
                context.Diagnostics);
        }

        private static async Task<Document> AddMissingSwitchLabelsAsync(SemanticModel model, Document document, SyntaxNode root, SwitchStatementSyntax switchBlock)
        {
            var enumType = (INamedTypeSymbol)model.GetTypeInfo(switchBlock.Expression).Type;
            var fullyQualifiedEnumType = enumType.ToDisplayString();

            var containsDefaultLabel = false;

            var caseLabels = new List<ExpressionSyntax>();
            foreach (var section in switchBlock.Sections)
            {
                foreach (var label in section.Labels)
                {
                    var caseLabel = label as CaseSwitchLabelSyntax;
                    if (caseLabel != null)
                    {
                        caseLabels.Add(caseLabel.Value);
                        continue;
                    }

                    if (label.IsKind(SyntaxKind.DefaultSwitchLabel))
                    {
                        containsDefaultLabel = true;
                    }
                }
            }

            var missingLabels = GetMissingLabels(model, caseLabels, enumType);

            var breakStatement = SyntaxFactory.BreakStatement();
            var statements = SyntaxFactory.List(new List<StatementSyntax> { breakStatement });

            var newSections = SyntaxFactory.List(switchBlock.Sections);
            foreach (var label in missingLabels)
            {
                // If an existing simplified label exists, it means we can assume that works already and do it ourselves as well (ergo: there is a static using)
                var caseLabel =
                    SyntaxFactory.CaseSwitchLabel(
                        SyntaxFactory.ParseExpression($"{fullyQualifiedEnumType}.{label}")
                                     .WithTrailingTrivia(SyntaxFactory.ParseTrailingTrivia(Environment.NewLine)));

                var section =
                    SyntaxFactory.SwitchSection(SyntaxFactory.List(new List<SwitchLabelSyntax> { caseLabel }), statements)
                                 .WithAdditionalAnnotations(Formatter.Annotation);

                // ensure that the new cases are above the last section if a default case exists, but below all other sections
                if (containsDefaultLabel)
                {
                    // this will not give an index-out-of-bounds error because we know there is at least one case block
                    newSections = newSections.Insert(switchBlock.Sections.Count - 1, section);
                }
                else
                {
                    newSections = newSections.Add(section);
                }
            }
            
            if (!containsDefaultLabel)
            {
                newSections = newSections.Add(SyntaxFactory.SwitchSection(SyntaxFactory.List(
                        new List<SwitchLabelSyntax> { SyntaxFactory.DefaultSwitchLabel() }), statements));
            }

            var newNode = switchBlock.WithSections(newSections).WithAdditionalAnnotations(Formatter.Annotation, Simplifier.Annotation);

            var newRoot = root.ReplaceNode(switchBlock, newNode);
            return await Simplifier.ReduceAsync(document.WithSyntaxRoot(newRoot)).ConfigureAwait(false);
        }

        private static IEnumerable<string> GetMissingLabels(SemanticModel model, List<ExpressionSyntax> caseLabels, INamedTypeSymbol enumType)
        {
            var symbols = new List<ISymbol>();
            foreach (var label in caseLabels)
            {
                // these are the labels like `MyEnum.EnumMember`
                var memberAccessExpression = label as MemberAccessExpressionSyntax;
                if (memberAccessExpression != null)
                {
                    var symbol = model.GetSymbolInfo(memberAccessExpression).Symbol;
                    if (symbol != null)
                    {
                        symbols.Add(symbol);
                    }
                    continue;
                }

                // these are the labels like `EnumMember` (such as when using `using static Namespace.MyEnum;`)
                var identifierName = label as IdentifierNameSyntax;
                if (identifierName != null)
                {
                    var symbol = model.GetSymbolInfo(identifierName).Symbol;
                    if (symbol != null)
                    {
                        symbols.Add(symbol);
                    }
                }
            }

            var missingLabels = new List<string>();
            foreach (var member in enumType.GetMembers())
            {
                // don't create members like ".ctor"
                if (member.IsImplicitlyDeclared)
                {
                    continue;
                }

                var memberExists = false;
                foreach (var symbol in symbols)
                {
                    if (symbol == member)
                    {
                        memberExists = true;
                        break;
                    }
                }

                if (!memberExists)
                {
                    missingLabels.Add(member.Name);
                }
            }

            return missingLabels;
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
