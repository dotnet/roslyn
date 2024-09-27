// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.UseObjectInitializer;

namespace Microsoft.CodeAnalysis.CSharp.UseObjectInitializer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpUseObjectInitializerDiagnosticAnalyzer :
    AbstractUseObjectInitializerDiagnosticAnalyzer<
        SyntaxKind,
        ExpressionSyntax,
        StatementSyntax,
        BaseObjectCreationExpressionSyntax,
        MemberAccessExpressionSyntax,
        ExpressionStatementSyntax,
        LocalDeclarationStatementSyntax,
        VariableDeclaratorSyntax,
        InitializerExpressionSyntax,
        CSharpUseNamedMemberInitializerAnalyzer>
{
    protected override bool FadeOutOperatorToken => true;

    protected override CSharpUseNamedMemberInitializerAnalyzer GetAnalyzer()
        => CSharpUseNamedMemberInitializerAnalyzer.Allocate();

    protected override bool AreObjectInitializersSupported(Compilation compilation)
    {
        // object initializers are only available in C# 3.0 and above.  Don't offer this refactoring
        // in projects targeting a lesser version.
        return compilation.LanguageVersion() >= LanguageVersion.CSharp3;
    }

    protected override ISyntaxFacts GetSyntaxFacts() => CSharpSyntaxFacts.Instance;

    protected override bool IsValidContainingStatement(StatementSyntax node)
    {
        // We don't want to offer this for using declarations because the way they are lifted means all
        // initialization is done before entering try block. For example
        // 
        // using var c = new Disposable() { Goo = 2 };
        //
        // is lowered to:
        //
        // var __c = new Disposable();
        // __c.Goo = 2;
        // var c = __c;
        // try
        // {
        // }
        // finally
        // {
        //     if (c != null)
        //     {
        //         ((IDisposable)c).Dispose();
        //     }
        // }
        //
        // As can be seen, if initializing throws any kind of exception, the newly created instance will not
        // be disposed properly.
        return node is not LocalDeclarationStatementSyntax localDecl ||
            localDecl.UsingKeyword == default;
    }
}
