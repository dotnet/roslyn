// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PopulateSwitch;

namespace Microsoft.CodeAnalysis.CSharp.PopulateSwitch
{
    [DiagnosticAnalyzer(LanguageNames.CSharp), Shared]
    internal sealed class PopulateSwitchExpressionDiagnosticAnalyzer :
        AbstractPopulateSwitchDiagnosticAnalyzer<ISwitchExpressionOperation, SwitchExpressionSyntax>
    {
        public PopulateSwitchExpressionDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.PopulateSwitchExpressionDiagnosticId)
        {
        }

        protected override OperationKind OperationKind => OperationKind.Switch;

        protected override ICollection<ISymbol> GetMissingEnumMembers(ISwitchExpressionOperation operation)
            => PopulateSwitchHelpers.GetMissingEnumMembers(operation);

        protected override bool HasDefaultCase(ISwitchExpressionOperation operation)
            => PopulateSwitchHelpers.HasDefaultCase(operation);

        protected override Location GetDiagnosticLocation(SwitchExpressionSyntax switchBlock)
            => switchBlock.SwitchKeyword.GetLocation();
    }

    [ExportCodeFixProvider(LanguageNames.CSharp,
        Name = PredefinedCodeFixProviderNames.PopulateSwitch), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.ImplementInterface)]
    internal class PopulateSwitchExpressionCodeFixProvider
        : AbstractPopulateSwitchCodeFixProvider<ISwitchExpressionOperation, SwitchExpressionSyntax>
    {
        [ImportingConstructor]
        public PopulateSwitchExpressionCodeFixProvider()
            : base(IDEDiagnosticIds.PopulateSwitchExpressionDiagnosticId)
        {
        }

        protected override void FixOneDiagnostic(
            Document document, SyntaxEditor editor,
            bool addCases, bool addDefaultCase, bool onlyOneDiagnostic,
            bool hasMissingCases, bool hasMissingDefaultCase,
            SwitchExpressionSyntax switchNode, ISwitchExpressionOperation switchStatement)
        {
            var enumType = switchStatement.Value.Type;

            var generator = editor.Generator;

            var sectionStatements = new[] { generator.ExitSwitchStatement() };

            var newSections = new List<SyntaxNode>();

            if (hasMissingCases && addCases)
            {
                var missingEnumMembers = PopulateSwitchHelpers.GetMissingEnumMembers(switchStatement);
                var missingSections =
                    from e in missingEnumMembers
                    let caseLabel = generator.MemberAccessExpression(generator.TypeExpression(enumType), e.Name).WithAdditionalAnnotations<SyntaxNode>(Simplifier.Annotation)
                    let section = generator.SwitchSection(caseLabel, sectionStatements)
                    select section;

                newSections.AddRange(missingSections);
            }

            if (hasMissingDefaultCase && addDefaultCase)
            {
                // Always add the default clause at the end.
                newSections.Add(generator.DefaultSwitchSection(sectionStatements));
            }

            var insertLocation = InsertPosition(switchStatement);

            var newSwitchNode = generator.InsertSwitchSections(switchNode, insertLocation, newSections)
                .WithAdditionalAnnotations(Formatter.Annotation);

            if (onlyOneDiagnostic)
            {
                // If we're only fixing up one issue in this document, then also make sure we 
                // didn't cause any braces to be imbalanced when we added members to the switch.
                // Note: i'm only doing this for the single case because it feels too complex
                // to try to support this during fix-all.
                var root = editor.OriginalRoot;
                AddMissingBraces(document, ref root, ref switchNode);

                var newRoot = root.ReplaceNode<SyntaxNode>(switchNode, newSwitchNode);
                editor.ReplaceNode(editor.OriginalRoot, newRoot);
            }
            else
            {
                editor.ReplaceNode(switchNode, newSwitchNode);
            }
        }

        private int InsertPosition(ISwitchOperation switchStatement)
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
