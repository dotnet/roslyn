﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.UseConditionalExpression;

namespace Microsoft.CodeAnalysis.CSharp.UseConditionalExpression
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpUseConditionalExpressionForReturnDiagnosticAnalyzer
        : AbstractUseConditionalExpressionForReturnDiagnosticAnalyzer<IfStatementSyntax>
    {
        public CSharpUseConditionalExpressionForReturnDiagnosticAnalyzer()
            : base(new LocalizableResourceString(nameof(CSharpAnalyzersResources.if_statement_can_be_simplified), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
        {
        }

        protected override ISyntaxFacts GetSyntaxFacts()
            => CSharpSyntaxFacts.Instance;
    }
}
