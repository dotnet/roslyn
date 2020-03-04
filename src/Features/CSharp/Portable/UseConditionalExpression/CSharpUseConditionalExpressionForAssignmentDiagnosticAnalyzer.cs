﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.UseConditionalExpression;

namespace Microsoft.CodeAnalysis.CSharp.UseConditionalExpression
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpUseConditionalExpressionForAssignmentDiagnosticAnalyzer
        : AbstractUseConditionalExpressionForAssignmentDiagnosticAnalyzer<IfStatementSyntax>
    {
        public CSharpUseConditionalExpressionForAssignmentDiagnosticAnalyzer()
            : base(new LocalizableResourceString(nameof(CSharpFeaturesResources.if_statement_can_be_simplified), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources)))
        {
        }

        protected override ISyntaxFacts GetSyntaxFacts()
            => CSharpSyntaxFacts.Instance;
    }
}
