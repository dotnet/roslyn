// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.PopulateSwitch;

internal abstract class AbstractPopulateSwitchStatementDiagnosticAnalyzer<TSwitchSyntax>()
    : AbstractPopulateSwitchDiagnosticAnalyzer<ISwitchOperation, TSwitchSyntax>(
        IDEDiagnosticIds.PopulateSwitchStatementDiagnosticId,
        EnforceOnBuildValues.PopulateSwitchStatement)
    where TSwitchSyntax : SyntaxNode
{
    protected sealed override OperationKind OperationKind => OperationKind.Switch;

    protected override bool IsKnownToBeExhaustive(ISwitchOperation switchOperation)
        => false;

    protected override IOperation GetValueOfSwitchOperation(ISwitchOperation operation)
        => operation.Value;

    protected sealed override bool IsSwitchTypeUnknown(ISwitchOperation operation)
        => operation.Value.Type is null;

    protected sealed override ICollection<ISymbol> GetMissingEnumMembers(ISwitchOperation operation)
        => PopulateSwitchStatementHelpers.GetMissingEnumMembers(operation);

    protected sealed override bool HasDefaultCase(ISwitchOperation operation)
        => PopulateSwitchStatementHelpers.HasDefaultCase(operation);

    protected override bool HasExhaustiveNullAndTypeCheckCases(ISwitchOperation operation)
        => PopulateSwitchStatementHelpers.HasExhaustiveNullAndTypeCheckCases(operation);

    protected sealed override Location GetDiagnosticLocation(TSwitchSyntax switchBlock)
        => switchBlock.GetFirstToken().GetLocation();

    protected override bool HasConstantCase(ISwitchOperation operation, object? value)
    {
        foreach (var opCase in operation.Cases)
        {
            foreach (var clause in opCase.Clauses)
            {
                if (clause is ISingleValueCaseClauseOperation singleValueCase &&
                    ConstantValueEquals(singleValueCase.Value.ConstantValue, value))
                {
                    return true;
                }
                else if (clause is IPatternCaseClauseOperation { Guard: null, Pattern: IConstantPatternOperation constantPattern } &&
                    ConstantValueEquals(constantPattern.Value.ConstantValue, value))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
