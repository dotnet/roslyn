// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.UseConditionalExpression;

namespace Microsoft.CodeAnalysis.CSharp.UseConditionalExpression
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpUseConditionalExpressionForAssignmentDiagnosticAnalyzer
        : AbstractUseConditionalExpressionForAssignmentDiagnosticAnalyzer<SyntaxKind>
    {
        public CSharpUseConditionalExpressionForAssignmentDiagnosticAnalyzer()
            : base(new LocalizableResourceString(nameof(CSharpFeaturesResources.if_statement_can_be_simplified), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources)))
        {
        }

        protected override ImmutableArray<SyntaxKind> GetIfStatementKinds()
            => ImmutableArray.Create(SyntaxKind.IfStatement);
    }
}
