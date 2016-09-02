// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.PopulateSwitch
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = PredefinedCodeFixProviderNames.PopulateSwitch), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.ImplementInterface)]
    internal class PopulateSwitchCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(IDEDiagnosticIds.PopulateSwitchDiagnosticId);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var properties = diagnostic.Properties;
            var missingCases = bool.Parse(properties[PopulateSwitchHelpers.MissingCases]);
            var missingDefaultCase = bool.Parse(properties[PopulateSwitchHelpers.MissingDefaultCase]);

            Debug.Assert(missingCases || missingDefaultCase);

            if (missingCases)
            {
                context.RegisterCodeFix(
                    new MyCodeAction(
                        FeaturesResources.Add_missing_cases,
                        c => AddMissingSwitchCasesAsync(context, includeMissingCases: true, includeDefaultCase: false)),
                    context.Diagnostics);
            }

            if (missingDefaultCase)
            {
                context.RegisterCodeFix(
                    new MyCodeAction(
                        FeaturesResources.Add_default_case,
                        c => AddMissingSwitchCasesAsync(context, includeMissingCases: false, includeDefaultCase: true)),
                    context.Diagnostics);
            }

            if (missingCases && missingDefaultCase)
            {
                context.RegisterCodeFix(
                    new MyCodeAction(
                        FeaturesResources.Add_both,
                        c => AddMissingSwitchCasesAsync(context, includeMissingCases: true, includeDefaultCase: true)),
                    context.Diagnostics);
            }

            return SpecializedTasks.EmptyTask;
        }

        private async Task<Document> AddMissingSwitchCasesAsync(
            CodeFixContext context, bool includeMissingCases, bool includeDefaultCase)
        {
            var document = context.Document;
            var span = context.Span;
            var cancellationToken = context.CancellationToken;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var switchNode = root.FindNode(span);
            var internalMethod = typeof(SemanticModel).GetTypeInfo().GetDeclaredMethod("GetOperationInternal");
            var switchStatement = (ISwitchStatement)internalMethod.Invoke(model, new object[] { switchNode, cancellationToken });
            var enumType = switchStatement.Value.Type;

            var generator = SyntaxGenerator.GetGenerator(document);

            var sectionStatements = new[] { generator.ExitSwitchStatement() };

            var newSections = new List<SyntaxNode>();

            if (includeMissingCases)
            {
                var missingEnumMembers = PopulateSwitchHelpers.GetMissingEnumMembers(switchStatement);
                var missingSections =
                    from e in missingEnumMembers
                    let caseLabel = generator.MemberAccessExpression(generator.TypeExpression(enumType), e.Name).WithAdditionalAnnotations(Simplifier.Annotation)
                    let section = generator.SwitchSection(caseLabel, sectionStatements)
                    select section;

                newSections.AddRange(missingSections);
            }

            if (includeDefaultCase)
            {
                // Always add the default clause at the end.
                newSections.Add(generator.DefaultSwitchSection(sectionStatements));
            }

            var insertLocation = InsertPosition(switchStatement);

            var newSwitchNode = generator.InsertSwitchSections(switchNode, insertLocation, newSections)
                .WithAdditionalAnnotations(Formatter.Annotation);

            // Make sure we didn't cause any braces to be imbalanced when we added members
            // to the switch.
            AddMissingBraces(document, ref root, ref switchNode);

            var newRoot = root.ReplaceNode(switchNode, newSwitchNode);

            return document.WithSyntaxRoot(newRoot);
        }

        private void AddMissingBraces(
            Document document,
            ref SyntaxNode root,
            ref SyntaxNode switchNode)
        {
            // Parsing of the switch may have caused imbalanced braces.  i.e. the switch
            // may have consumed a brace that was intended for a higher level construct.
            // So balance the tree first, then do the switch replacement.
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            SyntaxNode newRoot;
            SyntaxNode newSwitchNode;
            syntaxFacts.AddFirstMissingCloseBrace(
                root, switchNode, out newRoot, out newSwitchNode);

            root = newRoot;
            switchNode = newSwitchNode;
        }

        private int InsertPosition(ISwitchStatement switchStatement)
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

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(title, createChangedDocument)
            {
            }
        }
    }
}