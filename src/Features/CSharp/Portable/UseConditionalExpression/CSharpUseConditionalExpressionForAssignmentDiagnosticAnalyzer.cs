// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.UseConditionalExpression;

namespace Microsoft.CodeAnalysis.CSharp.UseConditionalExpression
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpUseConditionalExpressionForAssignmentDiagnosticAnalyzer
        : AbstractUseConditionalExpressionForAssignmentDiagnosticAnalyzer<SyntaxKind>
    {
        protected override ImmutableArray<SyntaxKind> GetIfStatementKinds()
            => ImmutableArray.Create(SyntaxKind.IfStatement);
    }

    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpUseConditionalExpressionForAssignmentCodeRefactoringProvider
        : AbstractUseConditionalExpressionForAssignmentCodeFixProvider<LocalDeclarationStatementSyntax>
    {
        protected override LocalDeclarationStatementSyntax AddSimplificationToType(LocalDeclarationStatementSyntax statement)
            => statement.WithDeclaration(statement.Declaration.WithType(
                statement.Declaration.Type.WithAdditionalAnnotations(Simplifier.Annotation)));
    }
}
