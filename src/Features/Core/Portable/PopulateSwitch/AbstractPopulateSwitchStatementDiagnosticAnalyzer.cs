// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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
            : base(IDEDiagnosticIds.PopulateSwitchStatementDiagnosticId)
        {
        }

        protected sealed override OperationKind OperationKind => OperationKind.Switch;

        protected sealed override ICollection<ISymbol> GetMissingEnumMembers(ISwitchOperation operation)
            => PopulateSwitchStatementHelpers.GetMissingEnumMembers(operation);

        protected sealed override bool HasDefaultCase(ISwitchOperation operation)
            => PopulateSwitchStatementHelpers.HasDefaultCase(operation);

        protected sealed override Location GetDiagnosticLocation(TSwitchSyntax switchBlock)
            => switchBlock.GetFirstToken().GetLocation();
    }
}
