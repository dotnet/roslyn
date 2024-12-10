// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.InitializeParameter;
internal abstract partial class AbstractInitializeParameterCodeRefactoringProvider<
    TTypeDeclarationSyntax,
    TParameterSyntax,
    TStatementSyntax,
    TExpressionSyntax> : CodeRefactoringProvider
    where TTypeDeclarationSyntax : SyntaxNode
    where TParameterSyntax : SyntaxNode
    where TStatementSyntax : SyntaxNode
    where TExpressionSyntax : SyntaxNode
{
    protected abstract bool IsFunctionDeclaration(SyntaxNode node);
    protected abstract bool IsImplicitConversion(Compilation compilation, ITypeSymbol source, ITypeSymbol destination);

    protected abstract SyntaxNode GetBody(SyntaxNode functionDeclaration);

    protected abstract Task<ImmutableArray<CodeAction>> GetRefactoringsForAllParametersAsync(
        Document document,
        SyntaxNode functionDeclaration,
        IMethodSymbol method,
        IBlockOperation? blockStatement,
        ImmutableArray<SyntaxNode> listOfParameterNodes,
        TextSpan parameterSpan,
        CancellationToken cancellationToken);

    protected abstract Task<ImmutableArray<CodeAction>> GetRefactoringsForSingleParameterAsync(
        Document document,
        TParameterSyntax parameterSyntax,
        IParameterSymbol parameter,
        SyntaxNode functionDeclaration,
        IMethodSymbol methodSymbol,
        IBlockOperation? blockStatement,
        CancellationToken cancellationToken);

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, _, cancellationToken) = context;

        // TODO: One could try to retrieve TParameterList and then filter out parameters that intersect with
        // textSpan and use that as `parameterNodes`, where `selectedParameter` would be the first one.

        var selectedParameter = await context.TryGetRelevantNodeAsync<TParameterSyntax>().ConfigureAwait(false);
        if (selectedParameter == null)
            return;

        var functionDeclaration = selectedParameter.FirstAncestorOrSelf<SyntaxNode>(IsFunctionDeclaration);
        if (functionDeclaration is null)
            return;

        var generator = SyntaxGenerator.GetGenerator(document);
        var parameterNodes = generator.GetParameters(functionDeclaration);
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

        // we can't just call GetDeclaredSymbol on functionDeclaration because it could an anonymous function,
        // so first we have to get the parameter symbol and then its containing method symbol
        if (!TryGetParameterSymbol(selectedParameter, semanticModel, out var parameter, cancellationToken))
            return;

        if (parameter.ContainingSymbol is not IMethodSymbol methodSymbol ||
            methodSymbol.IsAbstract ||
            methodSymbol.IsExtern ||
            methodSymbol.PartialImplementationPart != null ||
            methodSymbol.ContainingType.TypeKind == TypeKind.Interface)
        {
            return;
        }

        // We shouldn't offer a refactoring if the compilation doesn't contain the ArgumentNullException type,
        // as we use it later on in our computations.
        var argumentNullExceptionType = typeof(ArgumentNullException).FullName;
        if (argumentNullExceptionType is null || semanticModel.Compilation.GetTypeByMetadataName(argumentNullExceptionType) is null)
            return;

        if (CanOfferRefactoring(functionDeclaration, semanticModel, syntaxFacts, cancellationToken, out var blockStatementOpt))
        {
            // Ok.  Looks like the selected parameter could be refactored. Defer to subclass to 
            // actually determine if there are any viable refactorings here.
            var refactorings = await GetRefactoringsForSingleParameterAsync(
                document, selectedParameter, parameter, functionDeclaration, methodSymbol, blockStatementOpt, cancellationToken).ConfigureAwait(false);
            context.RegisterRefactorings(refactorings, context.Span);
        }

        // List with parameterNodes that pass all checks
        using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var listOfPotentiallyValidParametersNodes);
        foreach (var parameterNode in parameterNodes)
        {
            if (!TryGetParameterSymbol(parameterNode, semanticModel, out parameter, cancellationToken))
                return;

            // Update the list of valid parameter nodes
            listOfPotentiallyValidParametersNodes.Add(parameterNode);
        }

        if (listOfPotentiallyValidParametersNodes.Count > 1)
        {
            // Looks like we can offer a refactoring for more than one parameter. Defer to subclass to 
            // actually determine if there are any viable refactorings here.
            var refactorings = await GetRefactoringsForAllParametersAsync(
                document, functionDeclaration, methodSymbol, blockStatementOpt,
                listOfPotentiallyValidParametersNodes.ToImmutable(), selectedParameter.Span, cancellationToken).ConfigureAwait(false);
            context.RegisterRefactorings(refactorings, context.Span);
        }

        return;

        static bool TryGetParameterSymbol(
            SyntaxNode parameterNode,
            SemanticModel semanticModel,
            [NotNullWhen(true)] out IParameterSymbol? parameter,
            CancellationToken cancellationToken)
        {
            parameter = (IParameterSymbol?)semanticModel.GetDeclaredSymbol(parameterNode, cancellationToken);

            return parameter is { Name: not "" };
        }
    }

    protected bool CanOfferRefactoring(
        SyntaxNode functionDeclaration, SemanticModel semanticModel, ISyntaxFactsService syntaxFacts,
        CancellationToken cancellationToken, out IBlockOperation? blockStatement)
    {
        blockStatement = null;

        var functionBody = GetBody(functionDeclaration);
        if (functionBody == null)
        {
            // We support initializing parameters, even when the containing member doesn't have a
            // body. This is useful for when the user is typing a new constructor and hasn't written
            // the body yet.
            return true;
        }

        // In order to get the block operation for the body of an anonymous function, we need to
        // get it via `IAnonymousFunctionOperation.Body` instead of getting it directly from the body syntax.

        var operation = semanticModel.GetOperation(
            syntaxFacts.IsAnonymousFunctionExpression(functionDeclaration) ? functionDeclaration : functionBody,
            cancellationToken);

        if (operation == null)
            return false;

        switch (operation.Kind)
        {
            case OperationKind.AnonymousFunction:
                blockStatement = ((IAnonymousFunctionOperation)operation).Body;
                break;
            case OperationKind.Block:
                blockStatement = (IBlockOperation)operation;
                break;
            default:
                return false;
        }

        return true;
    }

    protected static bool ContainsParameterReference(
        SemanticModel semanticModel,
        IOperation condition,
        IParameterSymbol parameter,
        CancellationToken cancellationToken)
    {
        foreach (var child in condition.Syntax.DescendantNodes().OfType<TExpressionSyntax>())
        {
            var childOperation = semanticModel.GetOperation(child, cancellationToken);
            if (childOperation != null && InitializeParameterHelpersCore.IsParameterReference(childOperation, parameter))
                return true;
        }

        return false;
    }
}
