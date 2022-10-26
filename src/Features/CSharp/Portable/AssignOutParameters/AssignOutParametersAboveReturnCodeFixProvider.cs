// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.AssignOutParameters
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AssignOutParametersAboveReturn), Shared]
    internal class AssignOutParametersAboveReturnCodeFixProvider : AbstractAssignOutParametersCodeFixProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public AssignOutParametersAboveReturnCodeFixProvider()
        {
        }

        protected override void TryRegisterFix(CodeFixContext context, Document document, SyntaxNode container, SyntaxNode location)
        {
            RegisterCodeFix(context, CSharpFeaturesResources.Assign_out_parameters, nameof(CSharpFeaturesResources.Assign_out_parameters));
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

            var parent = exprOrStatement.Parent;
            if (parent.IsEmbeddedStatementOwner())
            {
                var newBody = editor.Generator.ScopeBlock(statements.Add(exprOrStatement));
                editor.ReplaceNode(exprOrStatement, newBody);
                editor.ReplaceNode(
                    exprOrStatement.Parent,
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
                    parent.Parent,
                    generator.WithStatements(parent.Parent, statements));
            }
            else
            {
                var lambda = (LambdaExpressionSyntax)parent;
                var newBody = editor.Generator.ScopeBlock(statements.Add(generator.ReturnStatement(exprOrStatement)));
                editor.ReplaceNode(
                    lambda,
                    lambda.WithBody((CSharpSyntaxNode)newBody)
                          .WithAdditionalAnnotations(Formatter.Annotation));
            }
        }
    }
}
