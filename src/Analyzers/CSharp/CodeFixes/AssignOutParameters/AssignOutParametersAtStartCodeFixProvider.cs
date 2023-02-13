// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.AssignOutParameters
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AssignOutParametersAtStart), Shared]
    internal class AssignOutParametersAtStartCodeFixProvider : AbstractAssignOutParametersCodeFixProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public AssignOutParametersAtStartCodeFixProvider()
        {
        }

        protected override void TryRegisterFix(CodeFixContext context, Document document, SyntaxNode container, SyntaxNode location)
        {
            // Don't offer if we're already the starting statement of the container. This case will
            // be handled by the AssignOutParametersAboveReturnCodeFixProvider class.
            if (location is ExpressionSyntax)
            {
                return;
            }

            if (location is LocalFunctionStatementSyntax { ExpressionBody: { } })
            {
                // This is an expression-bodied local function, which is also handled by the other code fix.
                return;
            }

            if (location is StatementSyntax statement &&
                statement.Parent is BlockSyntax block &&
                block.Statements[0] == statement &&
                block.Parent == container)
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    CSharpCodeFixesResources.Assign_out_parameters_at_start,
                    GetDocumentUpdater(context),
                    nameof(CSharpCodeFixesResources.Assign_out_parameters_at_start)),
                context.Diagnostics);
        }

        protected override void AssignOutParameters(
            SyntaxEditor editor, SyntaxNode container,
            MultiDictionary<SyntaxNode, (SyntaxNode exprOrStatement, ImmutableArray<IParameterSymbol> unassignedParameters)>.ValueSet values,
            CancellationToken cancellationToken)
        {
            var generator = editor.Generator;
            var unassignedParameters =
                values.SelectMany(t => t.unassignedParameters)
                      .Distinct()
                      .OrderBy(p => p.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken).SpanStart)
                      .ToImmutableArray();

            var statements = GenerateAssignmentStatements(generator, unassignedParameters);
            var originalStatements = generator.GetStatements(container).ToImmutableArray();

            var finalStatements = statements.AddRange(originalStatements);
            var updatedContainer = generator.WithStatements(container, finalStatements);

            editor.ReplaceNode(container, updatedContainer);
        }
    }
}
