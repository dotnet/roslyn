// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.AssignOutParameters
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class AssignOutParametersAtStartCodeFixProvider : AbstractAssignOutParametersCodeFixProvider
    {
        protected override void TryRegisterFix(CodeFixContext context, Document document, SyntaxNode container, SyntaxNode location)
        {
            // Don't offer if we're already the starting statement of the container. This case will
            // be handled by the AssignOutParametersAboveReturnCodeFixProvider class.
            if (location is ExpressionSyntax)
            {
                return;
            }

            if (location is StatementSyntax
            {
                Parent: BlockSyntax { Parent: container } block
            } statement && block.Statements[0] is statement)
            {
                return;
            }

            context.RegisterCodeFix(new MyCodeAction(
               CSharpFeaturesResources.Assign_out_parameters_at_start,
               c => FixAsync(document, context.Diagnostics[0], c)), context.Diagnostics);
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
