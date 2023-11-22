﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.UseCollectionInitializer;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionInitializer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpUseCollectionInitializerDiagnosticAnalyzer :
    AbstractUseCollectionInitializerDiagnosticAnalyzer<
        SyntaxKind,
        ExpressionSyntax,
        StatementSyntax,
        BaseObjectCreationExpressionSyntax,
        MemberAccessExpressionSyntax,
        InvocationExpressionSyntax,
        ExpressionStatementSyntax,
        LocalDeclarationStatementSyntax,
        VariableDeclaratorSyntax,
        CSharpUseCollectionInitializerAnalyzer>
{
    protected override CSharpUseCollectionInitializerAnalyzer GetAnalyzer()
        => CSharpUseCollectionInitializerAnalyzer.Allocate();

    protected override bool AreCollectionInitializersSupported(Compilation compilation)
        => compilation.LanguageVersion() >= LanguageVersion.CSharp3;

    protected override bool AreCollectionExpressionsSupported(Compilation compilation)
        => compilation.LanguageVersion().SupportsCollectionExpressions();

    protected override ISyntaxFacts GetSyntaxFacts()
        => CSharpSyntaxFacts.Instance;

    protected override bool CanUseCollectionExpression(SemanticModel semanticModel, BaseObjectCreationExpressionSyntax objectCreationExpression, CancellationToken cancellationToken)
        => UseCollectionExpressionHelpers.CanReplaceWithCollectionExpression(semanticModel, objectCreationExpression, skipVerificationForReplacedNode: true, cancellationToken);
}
