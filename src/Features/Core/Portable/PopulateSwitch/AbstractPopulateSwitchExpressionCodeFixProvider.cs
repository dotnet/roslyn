// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.PopulateSwitch
{
    internal abstract class AbstractPopulateSwitchExpressionCodeFixProvider<
        TExpressionSyntax, TSwitchSyntax, TSwitchArmSyntax, TMemberAccessExpressionSyntax>
        : AbstractPopulateSwitchCodeFixProvider<
            ISwitchExpressionOperation, TSwitchSyntax, TSwitchArmSyntax, TMemberAccessExpressionSyntax>
        where TExpressionSyntax : SyntaxNode
        where TSwitchSyntax : TExpressionSyntax
        where TSwitchArmSyntax : SyntaxNode
        where TMemberAccessExpressionSyntax : TExpressionSyntax
    {
        protected AbstractPopulateSwitchExpressionCodeFixProvider()
            : base(IDEDiagnosticIds.PopulateSwitchExpressionDiagnosticId)
        {
        }

        protected sealed override void FixOneDiagnostic(
            Document document, SyntaxEditor editor, SemanticModel semanticModel,
            bool addCases, bool addDefaultCase, bool onlyOneDiagnostic,
            bool hasMissingCases, bool hasMissingDefaultCase,
            TSwitchSyntax switchNode, ISwitchExpressionOperation switchExpression)
        {
            var newSwitchNode = UpdateSwitchNode(
                editor, semanticModel, addCases, addDefaultCase,
                hasMissingCases, hasMissingDefaultCase,
                switchNode, switchExpression).WithAdditionalAnnotations(Formatter.Annotation);

            editor.ReplaceNode(switchNode, newSwitchNode);
        }

        protected sealed override ITypeSymbol GetSwitchType(ISwitchExpressionOperation switchExpression)
            => switchExpression.Value.Type;

        protected sealed override ICollection<ISymbol> GetMissingEnumMembers(ISwitchExpressionOperation switchOperation)
            => PopulateSwitchExpressionHelpers.GetMissingEnumMembers(switchOperation);

        protected static TExpressionSyntax Exception(SyntaxGenerator generator, Compilation compilation)
            => (TExpressionSyntax)generator.CreateThrowNotImplementedExpression(compilation);

        protected sealed override int InsertPosition(ISwitchExpressionOperation switchExpression)
        {
            // If the last section has a default label, then we want to be above that.
            // Otherwise, we just get inserted at the end.

            var arms = switchExpression.Arms;
            return arms.Length > 0 && PopulateSwitchExpressionHelpers.IsDefault(arms[arms.Length - 1])
                ? arms.Length - 1
                : arms.Length;
        }
    }
}
