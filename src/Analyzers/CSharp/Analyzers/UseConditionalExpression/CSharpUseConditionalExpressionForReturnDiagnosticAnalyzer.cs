// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.UseConditionalExpression;

namespace Microsoft.CodeAnalysis.CSharp.UseConditionalExpression;

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

    protected override bool IsStatementSupported(IOperation statement)
    {
        // Return statements wrapped in an unsafe, checked or unchecked block are not supported
        // because having these enclosing blocks makes it difficult or impossible to convert
        // the blocks to expressions
        return !IsWrappedByCheckedOrUnsafe(statement);
    }

    private static bool IsWrappedByCheckedOrUnsafe(IOperation statement)
    {
        if (statement is not IReturnOperation { Parent: IBlockOperation block })
            return false;

        if (block.Syntax.Parent is UnsafeStatementSyntax or CheckedStatementSyntax)
            return true;

        return false;
    }
}
