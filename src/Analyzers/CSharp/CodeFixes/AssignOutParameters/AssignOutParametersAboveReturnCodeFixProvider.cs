// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.AssignOutParameters;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AssignOutParametersAboveReturn), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class AssignOutParametersAboveReturnCodeFixProvider() : AbstractAssignOutParametersCodeFixProvider
{
    protected override void TryRegisterFix(CodeFixContext context, Document document, SyntaxNode container, SyntaxNode location)
    {
        RegisterCodeFix(context, CSharpCodeFixesResources.Assign_out_parameters, nameof(CSharpCodeFixesResources.Assign_out_parameters));
    }

    protected override void AssignOutParameters(
        SyntaxEditor editor, SyntaxNode container,
        MultiDictionary<SyntaxNode, (SyntaxNode exprOrStatement, ImmutableArray<IParameterSymbol> unassignedParameters)>.ValueSet values,
        CancellationToken cancellationToken)
    {
        foreach (var (exprOrStatement, unassignedParameters) in values)
        {
            var statements = GenerateAssignmentStatements(editor.Generator, unassignedParameters);
            AddAssignmentStatements(editor, exprOrStatement, statements);
        }
    }

    private static void AddAssignmentStatements(
        SyntaxEditor editor, SyntaxNode exprOrStatement, ImmutableArray<SyntaxNode> statements)
    {
        var generator = editor.Generator;

        if (exprOrStatement is LocalFunctionStatementSyntax { ExpressionBody: { } localFunctionExpressionBody })
        {
            // Expression-bodied local functions report CS0177 on the method name instead of the expression.
            // Reassign exprOrStatement so the code fix implementation works as it does for other expression-bodied
            // members.
            exprOrStatement = localFunctionExpressionBody.Expression;
        }

        var parent = exprOrStatement.GetRequiredParent();
        if (parent.IsEmbeddedStatementOwner())
        {
            var newBody = SyntaxFactory.Block(statements.Add(exprOrStatement).Cast<StatementSyntax>());
            editor.ReplaceNode(exprOrStatement, newBody);
            editor.ReplaceNode(
                exprOrStatement.GetRequiredParent(),
                (c, _) => c.WithAdditionalAnnotations(Formatter.Annotation));
        }
        else if (parent is BlockSyntax or SwitchSectionSyntax)
        {
            editor.InsertBefore(exprOrStatement, statements);
        }
        else if (parent is ArrowExpressionClauseSyntax)
        {
            statements = statements.Add(generator.ReturnStatement(exprOrStatement));
            editor.ReplaceNode(
                parent.GetRequiredParent(),
                generator.WithStatements(parent.GetRequiredParent(), statements));
        }
        else
        {
            var lambda = (LambdaExpressionSyntax)parent;
            var newBody = SyntaxFactory.Block(statements.Add(generator.ReturnStatement(exprOrStatement)).Cast<StatementSyntax>());
            editor.ReplaceNode(
                lambda,
                lambda.WithBody(newBody)
                      .WithAdditionalAnnotations(Formatter.Annotation));
        }
    }
}
