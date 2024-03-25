// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.PopulateSwitch;

internal abstract class AbstractPopulateSwitchStatementCodeFixProvider<
    TSwitchSyntax, TSwitchArmSyntax, TMemberAccessExpression> :
    AbstractPopulateSwitchCodeFixProvider<
        ISwitchOperation, TSwitchSyntax, TSwitchArmSyntax, TMemberAccessExpression>
    where TSwitchSyntax : SyntaxNode
    where TSwitchArmSyntax : SyntaxNode
    where TMemberAccessExpression : SyntaxNode
{
    protected AbstractPopulateSwitchStatementCodeFixProvider()
        : base(IDEDiagnosticIds.PopulateSwitchStatementDiagnosticId)
    {
    }

    protected sealed override void FixOneDiagnostic(
        Document document, SyntaxEditor editor, SemanticModel semanticModel,
        bool addCases, bool addDefaultCase, bool onlyOneDiagnostic,
        bool hasMissingCases, bool hasMissingDefaultCase,
        TSwitchSyntax switchNode, ISwitchOperation switchOperation)
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

    protected sealed override ITypeSymbol GetSwitchType(ISwitchOperation switchOperation)
        => switchOperation.Value.Type ?? throw ExceptionUtilities.Unreachable();

    protected sealed override ICollection<ISymbol> GetMissingEnumMembers(ISwitchOperation switchOperation)
        => PopulateSwitchStatementHelpers.GetMissingEnumMembers(switchOperation);

    protected sealed override bool HasNullSwitchArm(ISwitchOperation switchOperation)
        => PopulateSwitchStatementHelpers.HasNullSwitchArm(switchOperation);

    protected sealed override TSwitchSyntax InsertSwitchArms(SyntaxGenerator generator, TSwitchSyntax switchNode, int insertLocation, List<TSwitchArmSyntax> newArms)
        => (TSwitchSyntax)generator.InsertSwitchSections(switchNode, insertLocation, newArms);

    protected sealed override TSwitchArmSyntax CreateDefaultSwitchArm(SyntaxGenerator generator, Compilation compilation)
        => (TSwitchArmSyntax)generator.DefaultSwitchSection([generator.ExitSwitchStatement()]);

    protected sealed override TSwitchArmSyntax CreateSwitchArm(SyntaxGenerator generator, Compilation compilation, TMemberAccessExpression caseLabel)
        => (TSwitchArmSyntax)generator.SwitchSection(caseLabel, [generator.ExitSwitchStatement()]);

    protected override TSwitchArmSyntax CreateNullSwitchArm(SyntaxGenerator generator, Compilation compilation)
        => (TSwitchArmSyntax)generator.SwitchSection(generator.NullLiteralExpression(), [generator.ExitSwitchStatement()]);

    protected sealed override int InsertPosition(ISwitchOperation switchStatement)
    {
        // If the last section has a default label, then we want to be above that.
        // Otherwise, we just get inserted at the end.

        var cases = switchStatement.Cases;
        if (cases.Length > 0)
        {
            var lastCase = cases.Last();
            if (lastCase.Clauses.Any(static c => c.CaseKind == CaseKind.Default))
            {
                return cases.Length - 1;
            }
        }

        return cases.Length;
    }
}
