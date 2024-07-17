// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.AssignOutParameters;

internal abstract class AbstractAssignOutParametersCodeFixProvider : SyntaxEditorBasedCodeFixProvider
{
    private const string CS0177 = nameof(CS0177); // The out parameter 'x' must be assigned to before control leaves the current method

    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
        [CS0177];

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var cancellationToken = context.CancellationToken;
        var document = context.Document;
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var (container, location) = GetContainer(root, context.Span);
        if (container != null)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var dataFlow = semanticModel.AnalyzeDataFlow(location);
            if (dataFlow.Succeeded)
            {
                TryRegisterFix(context, document, container, location);
            }
        }
    }

    protected abstract void TryRegisterFix(CodeFixContext context, Document document, SyntaxNode container, SyntaxNode location);

    private static (SyntaxNode container, SyntaxNode exprOrStatement) GetContainer(SyntaxNode root, TextSpan span)
    {
        var location = root.FindNode(span);
        if (IsValidLocation(location))
        {
            var container = GetContainer(location);
            if (container != null)
            {
                return (container, location);
            }
        }

        return default;
    }

    private static bool IsValidLocation(SyntaxNode location)
    {
        if (location is StatementSyntax)
        {
            return location.Parent is BlockSyntax
                || location.Parent is SwitchSectionSyntax
                || location.Parent.IsEmbeddedStatementOwner();
        }

        if (location is ExpressionSyntax)
        {
            return location.Parent is ArrowExpressionClauseSyntax or LambdaExpressionSyntax;
        }

        return false;
    }

    private static SyntaxNode? GetContainer(SyntaxNode node)
    {
        for (var current = node; current != null; current = current.Parent)
        {
            var parameterList = current.GetParameterList();
            if (parameterList != null)
            {
                return current;
            }
        }

        return null;
    }

    private static async Task<MultiDictionary<SyntaxNode, (SyntaxNode exprOrStatement, ImmutableArray<IParameterSymbol>)>> GetUnassignedParametersAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var containersAndLocations =
            diagnostics.SelectAsArray(d => GetContainer(root, d.Location.SourceSpan))
                       .WhereAsArray(t => t.container != null);

        var result = new MultiDictionary<SyntaxNode, (SyntaxNode exprOrStatement, ImmutableArray<IParameterSymbol>)>();
        foreach (var group in containersAndLocations.GroupBy(t => t.container))
        {
            var container = group.Key;

            var parameterList = container.GetParameterList();
            Contract.ThrowIfNull(parameterList);

            var outParameters = parameterList.Parameters
                .Select(p => semanticModel.GetRequiredDeclaredSymbol(p, cancellationToken))
                .Where(p => p.RefKind == RefKind.Out)
                .ToImmutableArray();

            var distinctExprsOrStatements = group.Select(t => t.exprOrStatement).Distinct();
            foreach (var exprOrStatement in distinctExprsOrStatements)
            {
                var dataFlow = semanticModel.AnalyzeDataFlow(exprOrStatement);
                var unassignedParameters = outParameters.WhereAsArray(
                    p => !dataFlow.DefinitelyAssignedOnExit.Contains(p));

                if (unassignedParameters.Length > 0)
                {
                    result.Add(container, (exprOrStatement, unassignedParameters));
                }
            }
        }

        return result;
    }

    protected sealed override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CancellationToken cancellationToken)
    {
        var unassignedParameters = await GetUnassignedParametersAsync(
            document, diagnostics, cancellationToken).ConfigureAwait(false);

        foreach (var container in unassignedParameters.Keys.OrderByDescending(n => n.Span.Start))
        {
            AssignOutParameters(
                editor, container, unassignedParameters[container], cancellationToken);
        }
    }

    protected abstract void AssignOutParameters(
        SyntaxEditor editor, SyntaxNode container,
        MultiDictionary<SyntaxNode, (SyntaxNode exprOrStatement, ImmutableArray<IParameterSymbol> unassignedParameters)>.ValueSet values,
        CancellationToken cancellationToken);

    protected static ImmutableArray<SyntaxNode> GenerateAssignmentStatements(
        SyntaxGenerator generator, ImmutableArray<IParameterSymbol> unassignedParameters)
    {
        var result = ArrayBuilder<SyntaxNode>.GetInstance();

        foreach (var parameter in unassignedParameters)
        {
            result.Add(generator.ExpressionStatement(generator.AssignmentStatement(
                generator.IdentifierName(parameter.Name),
                ExpressionGenerator.GenerateExpression(generator, parameter.Type, value: null, canUseFieldReference: false))));
        }

        return result.ToImmutableAndFree();
    }
}
