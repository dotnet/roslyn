// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.PopulateSwitch
{
    internal abstract class AbstractPopulateSwitchStatementDiagnosticAnalyzer<TSwitchSyntax> :
        AbstractPopulateSwitchDiagnosticAnalyzer<ISwitchOperation, TSwitchSyntax>
        where TSwitchSyntax : SyntaxNode
    {
        protected AbstractPopulateSwitchStatementDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.PopulateSwitchStatementDiagnosticId,
                   EnforceOnBuildValues.PopulateSwitchStatement)
        {
        }

        protected sealed override OperationKind OperationKind => OperationKind.Switch;

        protected sealed override bool IsSwitchTypeUnknown(ISwitchOperation operation)
            => operation.Value.Type is null;

        protected sealed override ICollection<ISymbol> GetMissingEnumMembers(ISwitchOperation operation)
            => PopulateSwitchStatementHelpers.GetMissingEnumMembers(operation);

        protected sealed override bool HasDefaultCase(ISwitchOperation operation)
            => PopulateSwitchStatementHelpers.HasDefaultCase(operation);

        protected sealed override Location GetDiagnosticLocation(TSwitchSyntax switchBlock)
            => switchBlock.GetFirstToken().GetLocation();
    }
}
