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

            var caseLabels = new List<ExpressionSyntax>();
            foreach (var section in switchBlock.Sections)
            {
                foreach (var label in section.Labels)
                {
                    var caseLabel = label as CaseSwitchLabelSyntax;
                    if (caseLabel != null)
                    {
                        caseLabels.Add(caseLabel.Value);
                    }
                }
            }

            var missingLabels = GetMissingLabels(caseLabels, enumType);

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

                // ensure that the new cases are above the default case
                newSections = newSections.Add(section);
            }

            var containsDefaultLabel = SwitchContainsDefaultLabel(newSections);

            System.Diagnostics.Debug.WriteLine(containsDefaultLabel);
            if (!containsDefaultLabel)
            {
                newSections = newSections.Add(SyntaxFactory.SwitchSection(SyntaxFactory.List(
                        new List<SwitchLabelSyntax> { SyntaxFactory.DefaultSwitchLabel() }), statements));
            }

            var newNode = switchBlock.WithSections(newSections).WithAdditionalAnnotations(Formatter.Annotation, Simplifier.Annotation);

            var newRoot = root.ReplaceNode(switchBlock, newNode);
            return await Simplifier.ReduceAsync(document.WithSyntaxRoot(newRoot)).ConfigureAwait(false);
        }

        private static IEnumerable<string> GetMissingLabels(List<ExpressionSyntax> caseLabels, INamedTypeSymbol enumType)
        {
            var labels = new List<string>();
            foreach (var label in caseLabels)
            {
                // these are the labels like `MyEnum.EnumMember`
                var memberAccessExpression = label as MemberAccessExpressionSyntax;
                if (memberAccessExpression != null)
                {
                    labels.Add(memberAccessExpression.Name.Identifier.ValueText);
                    continue;
                }

                // these are the labels like `EnumMember` (such as when using `using static Namespace.MyEnum;`)
                var identifierName = label as IdentifierNameSyntax;
                if (identifierName != null)
                {
                    labels.Add(identifierName.Identifier.ValueText);
                }
            }

            var missingLabels = new List<string>();
            foreach (var memberName in enumType.MemberNames)
            {
                // don't create members like ".ctor"
                if (memberName.StartsWith("."))
                {
                    continue;
                }

                var memberNameExists = false;
                foreach (var label in labels)
                {
                    if (label == memberName)
                    {
                        memberNameExists = true;
                        break;
                    }
                }

                if (!memberNameExists)
                {
                    missingLabels.Add(memberName);
                }
            }

            return missingLabels;
        }

        private static bool SwitchContainsDefaultLabel(SyntaxList<SwitchSectionSyntax> sections)
        {
            foreach (var section in sections)
            {
                foreach (var label in section.Labels)
                {
                    if (label.IsKind(SyntaxKind.DefaultSwitchLabel))
                    {
                        return true;
                    }
                }
            }

            return false;
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
