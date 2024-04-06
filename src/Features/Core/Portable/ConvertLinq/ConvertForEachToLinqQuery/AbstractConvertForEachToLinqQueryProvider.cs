// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ConvertLinq.ConvertForEachToLinqQuery;

internal abstract class AbstractConvertForEachToLinqQueryProvider<TForEachStatement, TStatement> : CodeRefactoringProvider
    where TForEachStatement : TStatement
    where TStatement : SyntaxNode
{
    /// <summary>
    /// Parses the forEachStatement until a child node cannot be converted into a query clause.
    /// </summary>
    /// <param name="forEachStatement"></param>
    /// <returns>
    /// 1) extended nodes that can be converted into clauses
    /// 2) identifiers introduced in nodes that can be converted
    /// 3) statements that cannot be converted into clauses
    /// 4) trailing comments to be added to the end
    /// </returns>
    protected abstract ForEachInfo<TForEachStatement, TStatement> CreateForEachInfo(
        TForEachStatement forEachStatement, SemanticModel semanticModel, bool convertLocalDeclarations);

    /// <summary>
    /// Tries to build a specific converter that covers e.g. Count, ToList, yield return or other similar cases.
    /// </summary>
    protected abstract bool TryBuildSpecificConverter(
        ForEachInfo<TForEachStatement, TStatement> forEachInfo,
        SemanticModel semanticModel,
        TStatement statementCannotBeConverted,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out IConverter<TForEachStatement, TStatement>? converter);

    /// <summary>
    /// Creates a default converter where foreach is joined with some children statements but other children statements are kept unmodified.
    /// Example:
    /// foreach(...)
    /// {
    ///     if (condition)
    ///     {
    ///        doSomething();
    ///     }
    /// }
    /// is converted to
    /// foreach(... where condition)
    /// {
    ///        doSomething(); 
    /// }
    /// </summary>
    protected abstract IConverter<TForEachStatement, TStatement> CreateDefaultConverter(
        ForEachInfo<TForEachStatement, TStatement> forEachInfo);

    protected abstract SyntaxNode AddLinqUsing(
        IConverter<TForEachStatement, TStatement> converter, SemanticModel semanticModel, SyntaxNode root);

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, _, cancellationToken) = context;

        var forEachStatement = await context.TryGetRelevantNodeAsync<TForEachStatement>().ConfigureAwait(false);
        if (forEachStatement == null)
        {
            return;
        }

        if (forEachStatement.ContainsDiagnostics)
        {
            return;
        }

        // Do not try to refactor queries with conditional compilation in them.
        if (forEachStatement.ContainsDirectives)
        {
            return;
        }

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        if (!TryBuildConverter(forEachStatement, semanticModel, convertLocalDeclarations: true, cancellationToken, out var queryConverter))
        {
            return;
        }

        if (semanticModel.GetDiagnostics(forEachStatement.Span, cancellationToken)
            .Any(static diagnostic => diagnostic.DefaultSeverity == DiagnosticSeverity.Error))
        {
            return;
        }

        // Offer refactoring to convert foreach to LINQ query expression. For example:
        //
        // INPUT:
        //  foreach (var n1 in c1)
        //      foreach (var n2 in c2)
        //          if (n1 > n2)
        //              yield return n1 + n2;
        //
        // OUTPUT:
        //  from n1 in c1
        //  from n2 in c2
        //  where n1 > n2
        //  select n1 + n2
        //
        context.RegisterRefactoring(
            CodeAction.Create(
                FeaturesResources.Convert_to_linq,
                c => ApplyConversionAsync(queryConverter, document, convertToQuery: true, c),
                nameof(FeaturesResources.Convert_to_linq)),
            forEachStatement.Span);

        // Offer refactoring to convert foreach to LINQ invocation expression. For example:
        //
        // INPUT:
        //   foreach (var n1 in c1)
        //      foreach (var n2 in c2)
        //          if (n1 > n2)
        //              yield return n1 + n2;
        //
        // OUTPUT:
        //   c1.SelectMany(n1 => c2.Where(n2 => n1 > n2).Select(n2 => n1 + n2))
        //
        // CONSIDER: Currently, we do not handle foreach statements with a local declaration statement for Convert_to_linq refactoring,
        //           though we do handle them for the Convert_to_query refactoring above.
        //           In future, we can consider supporting them by creating anonymous objects/tuples wrapping the local declarations.
        if (TryBuildConverter(forEachStatement, semanticModel, convertLocalDeclarations: false, cancellationToken, out var linqConverter))
        {
            context.RegisterRefactoring(
                CodeAction.Create(
                    FeaturesResources.Convert_to_linq_call_form,
                    c => ApplyConversionAsync(linqConverter, document, convertToQuery: false, c),
                    nameof(FeaturesResources.Convert_to_linq_call_form)),
                forEachStatement.Span);
        }
    }

    private Task<Document> ApplyConversionAsync(
        IConverter<TForEachStatement, TStatement> converter,
        Document document,
        bool convertToQuery,
        CancellationToken cancellationToken)
    {
        var editor = new SyntaxEditor(converter.ForEachInfo.SemanticModel.SyntaxTree.GetRoot(cancellationToken), document.Project.Solution.Services);
        converter.Convert(editor, convertToQuery, cancellationToken);
        var newRoot = editor.GetChangedRoot();
        var rootWithLinqUsing = AddLinqUsing(converter, converter.ForEachInfo.SemanticModel, newRoot);
        return Task.FromResult(document.WithSyntaxRoot(rootWithLinqUsing));
    }

    /// <summary>
    /// Tries to build either a specific or a default converter.
    /// </summary>
    private bool TryBuildConverter(
        TForEachStatement forEachStatement,
        SemanticModel semanticModel,
        bool convertLocalDeclarations,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out IConverter<TForEachStatement, TStatement>? converter)
    {
        var forEachInfo = CreateForEachInfo(forEachStatement, semanticModel, convertLocalDeclarations);

        if (forEachInfo.Statements.Length == 1 &&
            TryBuildSpecificConverter(forEachInfo, semanticModel, forEachInfo.Statements.Single(), cancellationToken, out converter))
        {
            return true;
        }

        // No sense to convert a single foreach to foreach over the same collection.
        if (forEachInfo.ConvertingExtendedNodes.Length >= 1)
        {
            converter = CreateDefaultConverter(forEachInfo);
            return true;
        }

        converter = null;
        return false;
    }
}
