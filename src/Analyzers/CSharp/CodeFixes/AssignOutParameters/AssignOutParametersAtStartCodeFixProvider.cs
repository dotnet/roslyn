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

namespace Microsoft.CodeAnalysis.CSharp.AssignOutParameters;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AssignOutParametersAtStart), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class AssignOutParametersAtStartCodeFixProvider() : AbstractAssignOutParametersCodeFixProvider
{
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

        RegisterCodeFix(context, CSharpCodeFixesResources.Assign_out_parameters_at_start, nameof(CSharpCodeFixesResources.Assign_out_parameters_at_start));
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

        var updatedContainer = generator.WithStatements(
            container,
            [.. GenerateAssignmentStatements(generator, unassignedParameters),
             .. generator.GetStatements(container)]);

        editor.ReplaceNode(container, updatedContainer);
    }
}
