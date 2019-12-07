// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
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
}
