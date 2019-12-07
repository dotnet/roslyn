// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.PopulateSwitch
{
    internal abstract class AbstractPopulateSwitchCodeFixProvider<
        TSwitchOperation,
        TSwitchSyntax,
        TSwitchArmSyntax,
        TMemberAccessExpression>
        : SyntaxEditorBasedCodeFixProvider
        where TSwitchOperation : IOperation
        where TSwitchSyntax : SyntaxNode
        where TSwitchArmSyntax : SyntaxNode
        where TMemberAccessExpression : SyntaxNode
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds { get; }

        protected AbstractPopulateSwitchCodeFixProvider(string diagnosticId)
        {
            FixableDiagnosticIds = ImmutableArray.Create(diagnosticId);
        }

        protected abstract ITypeSymbol GetSwitchType(TSwitchOperation switchStatement);
        protected abstract ICollection<ISymbol> GetMissingEnumMembers(TSwitchOperation switchOperation);

        protected abstract TSwitchArmSyntax CreateSwitchArm(SyntaxGenerator generator, Compilation compilation, TMemberAccessExpression caseLabel);
        protected abstract TSwitchArmSyntax CreateDefaulSwitchArm(SyntaxGenerator generator, Compilation compilation);
        protected abstract int InsertPosition(TSwitchOperation switchOperation);
        protected abstract TSwitchSyntax InsertSwitchArms(SyntaxGenerator generator, TSwitchSyntax switchNode, int insertLocation, List<TSwitchArmSyntax> newArms);

        protected abstract void FixOneDiagnostic(
            Document document, SyntaxEditor editor, SemanticModel semanticModel,
            bool addCases, bool addDefaultCase, bool onlyOneDiagnostic,
            bool hasMissingCases, bool hasMissingDefaultCase,
            TSwitchSyntax switchNode, TSwitchOperation switchOperation);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.Custom;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var properties = diagnostic.Properties;
            var missingCases = bool.Parse(properties[PopulateSwitchHelpers.MissingCases]);
            var missingDefaultCase = bool.Parse(properties[PopulateSwitchHelpers.MissingDefaultCase]);

            Debug.Assert(missingCases || missingDefaultCase);

            var document = context.Document;
            if (missingCases)
            {
                context.RegisterCodeFix(
                    new MyCodeAction(
                        FeaturesResources.Add_missing_cases,
                        c => FixAsync(document, diagnostic,
                            addCases: true, addDefaultCase: false,
                            cancellationToken: c)),
                    context.Diagnostics);
            }

            if (missingDefaultCase)
            {
                context.RegisterCodeFix(
                    new MyCodeAction(
                        FeaturesResources.Add_default_case,
                        c => FixAsync(document, diagnostic,
                            addCases: false, addDefaultCase: true,
                            cancellationToken: c)),
                    context.Diagnostics);
            }

            if (missingCases && missingDefaultCase)
            {
                context.RegisterCodeFix(
                    new MyCodeAction(
                        FeaturesResources.Add_both,
                        c => FixAsync(document, diagnostic,
                            addCases: true, addDefaultCase: true,
                            cancellationToken: c)),
                    context.Diagnostics);
            }

            return Task.CompletedTask;
        }

        private Task<Document> FixAsync(
            Document document, Diagnostic diagnostic,
            bool addCases, bool addDefaultCase,
            CancellationToken cancellationToken)
        {
            return FixAllAsync(document, ImmutableArray.Create(diagnostic),
                addCases, addDefaultCase, cancellationToken);
        }

        private Task<Document> FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            bool addCases, bool addDefaultCase,
            CancellationToken cancellationToken)
        {
            return FixAllWithEditorAsync(document,
                editor => FixWithEditorAsync(document, editor, diagnostics, addCases, addDefaultCase, cancellationToken),
                cancellationToken);
        }

        private async Task FixWithEditorAsync(
            Document document, SyntaxEditor editor, ImmutableArray<Diagnostic> diagnostics,
            bool addCases, bool addDefaultCase,
            CancellationToken cancellationToken)
        {
            foreach (var diagnostic in diagnostics)
            {
                await FixOneDiagnostic(
                    document, editor, diagnostic, addCases, addDefaultCase,
                    diagnostics.Length == 1, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task FixOneDiagnostic(
            Document document, SyntaxEditor editor, Diagnostic diagnostic,
            bool addCases, bool addDefaultCase, bool onlyOneDiagnostic,
            CancellationToken cancellationToken)
        {
            var hasMissingCases = bool.Parse(diagnostic.Properties[PopulateSwitchHelpers.MissingCases]);
            var hasMissingDefaultCase = bool.Parse(diagnostic.Properties[PopulateSwitchHelpers.MissingDefaultCase]);

            var switchLocation = diagnostic.AdditionalLocations[0];
            var switchNode = switchLocation.FindNode(getInnermostNodeForTie: true, cancellationToken) as TSwitchSyntax;
            if (switchNode == null)
                return;

            var model = await document.RequireSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var switchStatement = (TSwitchOperation)model.GetOperation(switchNode, cancellationToken);

            FixOneDiagnostic(
                document, editor, model, addCases, addDefaultCase, onlyOneDiagnostic,
                hasMissingCases, hasMissingDefaultCase, switchNode, switchStatement);
        }

        protected TSwitchSyntax UpdateSwitchNode(
            SyntaxEditor editor, SemanticModel semanticModel,
            bool addCases, bool addDefaultCase,
            bool hasMissingCases, bool hasMissingDefaultCase,
            TSwitchSyntax switchNode, TSwitchOperation switchOperation)
        {
            var enumType = GetSwitchType(switchOperation);

            var generator = editor.Generator;

            var newArms = new List<TSwitchArmSyntax>();

            if (hasMissingCases && addCases)
            {
                var missingArms =
                    from e in GetMissingEnumMembers(switchOperation)
                    let caseLabel = (TMemberAccessExpression)generator.MemberAccessExpression(generator.TypeExpression(enumType), e.Name).WithAdditionalAnnotations(Simplifier.Annotation)
                    select CreateSwitchArm(generator, semanticModel.Compilation, caseLabel);

                newArms.AddRange(missingArms);
            }

            if (hasMissingDefaultCase && addDefaultCase)
            {
                // Always add the default clause at the end.
                newArms.Add(CreateDefaulSwitchArm(generator, semanticModel.Compilation));
            }

            var insertLocation = InsertPosition(switchOperation);

            var newSwitchNode = InsertSwitchArms(generator, switchNode, insertLocation, newArms)
                .WithAdditionalAnnotations(Formatter.Annotation);
            return newSwitchNode;
        }

        protected static void AddMissingBraces(
            Document document,
            ref SyntaxNode root,
            ref TSwitchSyntax switchNode)
        {
            // Parsing of the switch may have caused imbalanced braces.  i.e. the switch
            // may have consumed a brace that was intended for a higher level construct.
            // So balance the tree first, then do the switch replacement.
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            syntaxFacts.AddFirstMissingCloseBrace(
                root, switchNode, out var newRoot, out var newSwitchNode);

            root = newRoot;
            switchNode = newSwitchNode;
        }

        protected override Task FixAllAsync(
            Document document,
            ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor,
            CancellationToken cancellationToken)
        {
            // If the user is performing a fix-all, then fix up all the issues we see. i.e.
            // add missing cases and missing 'default' cases for any switches we reported an
            // issue on.
            return FixWithEditorAsync(document, editor, diagnostics,
                addCases: true, addDefaultCase: true,
                cancellationToken: cancellationToken);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }

    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic,
        Name = PredefinedCodeFixProviderNames.PopulateSwitch), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.ImplementInterface)]
    internal class PopulateSwitchStatementCodeFixProvider : AbstractPopulateSwitchCodeFixProvider<
        ISwitchOperation, SyntaxNode, SyntaxNode, SyntaxNode>
    {
        [ImportingConstructor]
        public PopulateSwitchStatementCodeFixProvider()
            : base(IDEDiagnosticIds.PopulateSwitchStatementDiagnosticId)
        {
        }

        protected override void FixOneDiagnostic(
            Document document, SyntaxEditor editor, SemanticModel semanticModel,
            bool addCases, bool addDefaultCase, bool onlyOneDiagnostic,
            bool hasMissingCases, bool hasMissingDefaultCase,
            SyntaxNode switchNode, ISwitchOperation switchOperation)
        {
            var newSwitchNode = UpdateSwitchNode(
                editor, semanticModel, addCases, addDefaultCase,
                hasMissingCases, hasMissingDefaultCase,
                switchNode, switchOperation).WithAdditionalAnnotations(Formatter.Annotation);

            if (onlyOneDiagnostic)
            {
                // If we're only fixing up one issue in this document, then also make sure we 
                // didn't cause any braces to be imbalanced when we added members to the switch.
                // Note: i'm only doing this for the single case because it feels too complex
                // to try to support this during fix-all.
                var root = editor.OriginalRoot;
                AddMissingBraces(document, ref root, ref switchNode);

                var newRoot = root.ReplaceNode(switchNode, newSwitchNode);
                editor.ReplaceNode(editor.OriginalRoot, newRoot);
            }
            else
            {
                editor.ReplaceNode(switchNode, newSwitchNode);
            }
        }

        protected override ITypeSymbol GetSwitchType(ISwitchOperation switchOperation)
            => switchOperation.Value.Type;

        protected override ICollection<ISymbol> GetMissingEnumMembers(ISwitchOperation switchOperation)
            => PopulateSwitchHelpers.GetMissingEnumMembers(switchOperation);

        protected override SyntaxNode InsertSwitchArms(SyntaxGenerator generator, SyntaxNode switchNode, int insertLocation, List<SyntaxNode> newArms)
            => generator.InsertSwitchSections(switchNode, insertLocation, newArms);

        protected override SyntaxNode CreateDefaulSwitchArm(SyntaxGenerator generator, Compilation compilation)
            => generator.DefaultSwitchSection(new[] { generator.ExitSwitchStatement() });

        protected override SyntaxNode CreateSwitchArm(SyntaxGenerator generator, Compilation compilation, SyntaxNode caseLabel)
            => generator.SwitchSection(caseLabel, new[] { generator.ExitSwitchStatement() });

        protected override int InsertPosition(ISwitchOperation switchStatement)
        {
            // If the last section has a default label, then we want to be above that.
            // Otherwise, we just get inserted at the end.

            var cases = switchStatement.Cases;
            if (cases.Length > 0)
            {
                var lastCase = cases.Last();
                if (lastCase.Clauses.Any(c => c.CaseKind == CaseKind.Default))
                {
                    return cases.Length - 1;
                }
            }

            return cases.Length;
        }
    }
}
