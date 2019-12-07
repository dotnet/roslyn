// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PopulateSwitch;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.PopulateSwitch
{
    using static SyntaxFactory;

    [ExportCodeFixProvider(LanguageNames.CSharp,
        Name = PredefinedCodeFixProviderNames.PopulateSwitch), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.ImplementInterface)]
    internal class PopulateSwitchExpressionCodeFixProvider
        : AbstractPopulateSwitchCodeFixProvider<
            ISwitchExpressionOperation,
            SwitchExpressionSyntax,
            SwitchExpressionArmSyntax,
            MemberAccessExpressionSyntax>
    {
        [ImportingConstructor]
        public PopulateSwitchExpressionCodeFixProvider()
            : base(IDEDiagnosticIds.PopulateSwitchExpressionDiagnosticId)
        {
        }

        protected override void FixOneDiagnostic(
            Document document, SyntaxEditor editor, SemanticModel semanticModel,
            bool addCases, bool addDefaultCase, bool onlyOneDiagnostic,
            bool hasMissingCases, bool hasMissingDefaultCase,
            SwitchExpressionSyntax switchNode, ISwitchExpressionOperation switchExpression)
        {
            var newSwitchNode = UpdateSwitchNode(
                editor, semanticModel, addCases, addDefaultCase,
                hasMissingCases, hasMissingDefaultCase,
                switchNode, switchExpression).WithAdditionalAnnotations<SwitchExpressionSyntax>(Formatter.Annotation);

            editor.ReplaceNode(switchNode, newSwitchNode);
        }

        protected override ITypeSymbol GetSwitchType(ISwitchExpressionOperation switchExpression)
            => switchExpression.Value.Type;

        protected override ICollection<ISymbol> GetMissingEnumMembers(ISwitchExpressionOperation switchOperation)
            => PopulateSwitchHelpers.GetMissingEnumMembers(switchOperation);

        protected override SwitchExpressionArmSyntax CreateDefaulSwitchArm(SyntaxGenerator generator, Compilation compilation)
            => SwitchExpressionArm(DiscardPattern(), Exception(generator, compilation));

        private static ExpressionSyntax Exception(SyntaxGenerator generator, Compilation compilation)
            => (ExpressionSyntax)generator.CreateThrowNotImplementedExpression(compilation);

        protected override SwitchExpressionArmSyntax CreateSwitchArm(SyntaxGenerator generator, Compilation compilation, MemberAccessExpressionSyntax caseLabel)
            => SwitchExpressionArm(ConstantPattern(caseLabel), Exception(generator, compilation));

        protected override SwitchExpressionSyntax InsertSwitchArms(SyntaxGenerator generator, SwitchExpressionSyntax switchNode, int insertLocation, List<SwitchExpressionArmSyntax> newArms)
            => switchNode.WithArms(switchNode.Arms.InsertRange(insertLocation, newArms));

        protected override int InsertPosition(ISwitchExpressionOperation switchExpression)
        {
            // If the last section has a default label, then we want to be above that.
            // Otherwise, we just get inserted at the end.

            var arms = switchExpression.Arms;
            return arms.Length > 0 && PopulateSwitchHelpers.IsDefault(arms[arms.Length - 1])
                ? arms.Length - 1
                : arms.Length;
        }
    }
}
