// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MakeMethodAsynchronous;

internal abstract partial class AbstractMakeMethodAsynchronousCodeFixProvider : CodeFixProvider
{
    protected abstract bool IsSupportedDiagnostic(Diagnostic diagnostic, CancellationToken cancellationToken);
    protected abstract bool IsAsyncSupportingFunctionSyntax(SyntaxNode node);

    protected abstract string GetMakeAsyncTaskFunctionResource();
    protected abstract string GetUsedAsDelegateWarningResource();
    protected abstract string GetMakeAsyncVoidFunctionResource();

    protected abstract bool IsAsyncReturnType(ITypeSymbol type, KnownTaskTypes knownTypes);

    protected abstract SyntaxNode FixMethodSignature(
        bool addAsyncModifier,
        bool keepVoid,
        IMethodSymbol methodSymbol,
        SyntaxNode node,
        KnownTaskTypes knownTypes);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var document = context.Document;
        var diagnostic = context.Diagnostics.First();
        var cancellationToken = context.CancellationToken;

        if (!IsSupportedDiagnostic(diagnostic, cancellationToken))
            return;

        var node = GetContainingFunction(diagnostic, cancellationToken);
        if (node == null)
            return;

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var compilation = semanticModel.Compilation;

        // Find the symbols for Task, Task<T> and ValueTask<T>.  Note that the first
        // two are mandatory (since we need them to generate the return types for our
        // method if we convert it.  The last is optional.  It is only needed to know
        // if our member is already Task-Like, and that functionality recognizes
        // ValueTask if it is available, but does not care if it is not.
        var knownTypes = new KnownTaskTypes(compilation);
        if (knownTypes.TaskType == null || knownTypes.TaskOfTType == null)
            return;

        var methodSymbol = GetMethodSymbol(semanticModel, node, cancellationToken);
        if (methodSymbol is null)
            return;

        // Heuristic to recognize the common case for entry point method
        var isEntryPoint = methodSymbol.IsStatic && IsLikelyEntryPointName(methodSymbol.Name, document);

        // Always register the `async Task` fix first.
        var taskTitle = GetMakeAsyncTaskFunctionResource();
        context.RegisterCodeFix(
            CodeAction.Create(
                taskTitle,
                cancellationToken => FixNodeAsync(
                    document, diagnostic, keepVoid: false, isEntryPoint, cancellationToken),
                taskTitle),
            context.Diagnostics);

        // Also offer `async void` if the method is void-returning and not an entry point.
        if (methodSymbol.IsOrdinaryMethodOrLocalFunction() && methodSymbol.ReturnsVoid && !isEntryPoint)
        {
            var asyncVoidTitle = GetMakeAsyncVoidFunctionResource();
            context.RegisterCodeFix(
                CodeAction.Create(
                    asyncVoidTitle,
                    cancellationToken => FixNodeAsync(
                        document, diagnostic, keepVoid: true, isEntryPoint: false, cancellationToken),
                    asyncVoidTitle),
                context.Diagnostics);
        }
    }

    /// <summary>
    /// Looks in the current project for a location where <paramref name="methodSymbol"/>
    /// is converted to a delegate.
    /// </summary>
    /// <returns><see langword="true"/> iff such a reference is found.</returns>
    private static async Task<bool> HasReferenceAsDelegateInThisProjectAsync(
        Project project,
        IMethodSymbol methodSymbol,
        CancellationToken cancellationToken)
    {
        // 3 == methodSymbol + partial definition + partial implementation
        // `seenSymbols` will filter out duplicates.
        var symbolsToSearch = ImmutableArray.CreateBuilder<IMethodSymbol>(3);
        symbolsToSearch.Add(methodSymbol);

        if (methodSymbol.PartialDefinitionPart is { } partialDefinitionPart)
            symbolsToSearch.Add(partialDefinitionPart);

        if (methodSymbol.PartialImplementationPart is { } partialImplementationPart)
            symbolsToSearch.Add(partialImplementationPart);

        var cache = new Dictionary<DocumentId, SemanticDocument>();
        var seenSymbols = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
        var documentSet = project.Documents.ToImmutableHashSet();

        foreach (var symbol in symbolsToSearch)
        {
            if (!seenSymbols.Add(symbol))
                continue;

            var referencedSymbols = await SymbolFinder.FindReferencesAsync(
                symbol,
                project.Solution,
                documentSet,
                cancellationToken).ConfigureAwait(false);

            foreach (var referencedSymbol in referencedSymbols)
            {
                foreach (var location in referencedSymbol.Locations)
                {
                    var document = location.Document;
                    if (!cache.TryGetValue(document.Id, out var entry))
                    {
                        entry = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);
                        cache.Add(document.Id, entry);
                    }

                    var syntaxNode = entry.Root.FindNode(location.Location.SourceSpan, getInnermostNodeForTie: true);
                    if (IsDelegateReference(entry.SemanticModel, syntaxNode, symbol, cancellationToken))
                        return true;
                }
            }
        }

        return false;
    }

    /// <returns>
    /// Whether <paramref name="syntaxNode"/> is a reference to a method that is being
    /// converted to a delegate.
    /// </returns>
    private static bool IsDelegateReference(
        SemanticModel semanticModel,
        SyntaxNode syntaxNode,
        IMethodSymbol methodSymbol,
        CancellationToken cancellationToken)
    {
        IOperation? operation = null;
        foreach (var currentNode in syntaxNode.GetAncestorsOrThis<SyntaxNode>())
        {
            operation = semanticModel.GetOperation(currentNode, cancellationToken);
            if (operation != null)
                break;
        }

        for (; operation != null; operation = operation.Parent)
        {
            // Don't walk past lambda/local-function boundaries. A method invocation inside a lambda
            // that is then converted to a delegate should not be treated as a delegate reference to that method.
            if (operation is IAnonymousFunctionOperation or ILocalFunctionOperation)
                return false;

            if (operation is IDelegateCreationOperation delegateCreation)
            {
                if (delegateCreation.Target is not IMethodReferenceOperation methodReference)
                    return false;

                return SymbolEqualityComparer.Default.Equals(
                    methodReference.Method.OriginalDefinition,
                    methodSymbol.OriginalDefinition);
            }
        }

        return false;
    }

    private static IMethodSymbol? GetMethodSymbol(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
    {
        // GetDeclaredSymbol for methods/local-functions.  GetSymbolInfo for lambdas.
        var symbol = semanticModel.GetDeclaredSymbol(node, cancellationToken) ?? semanticModel.GetSymbolInfo(node, cancellationToken).GetAnySymbol();
        return symbol as IMethodSymbol;
    }

    private static bool IsLikelyEntryPointName(string name, Document document)
    {
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        return syntaxFacts.StringComparer.Equals(name, "Main");
    }

    private const string AsyncSuffix = "Async";

    private async Task<Solution> FixNodeAsync(
        Document document,
        Diagnostic diagnostic,
        bool keepVoid,
        bool isEntryPoint,
        CancellationToken cancellationToken)
    {
        var node = GetContainingFunction(diagnostic, cancellationToken);
        Contract.ThrowIfNull(node);

        // See if we're on an actual method declaration (otherwise we're on a lambda declaration).
        // If we're on a method declaration, we'll get an IMethodSymbol back.  In that case, check
        // if it has the 'Async' suffix, and remove that suffix if so.
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var methodSymbol = GetMethodSymbol(semanticModel, node, cancellationToken);
        Contract.ThrowIfNull(methodSymbol);

        var knownTypes = new KnownTaskTypes(semanticModel.Compilation);

        var returnTypeWillChange = methodSymbol.ReturnsVoid
            ? !keepVoid
            : !IsAsyncReturnType(methodSymbol.ReturnType, knownTypes);
        var addWarningAnnotation = returnTypeWillChange
            && await HasReferenceAsDelegateInThisProjectAsync(
                document.Project, methodSymbol, cancellationToken).ConfigureAwait(false);

        return NeedsRename()
            ? await RenameThenAddAsyncTokenAsync(keepVoid, addWarningAnnotation, document, node, methodSymbol, knownTypes, cancellationToken).ConfigureAwait(false)
            : await FixRelatedSignaturesAsync(keepVoid, addWarningAnnotation, document, methodSymbol, knownTypes, node, cancellationToken).ConfigureAwait(false);

        bool NeedsRename()
        {
            // We don't need to rename methods that don't have a name
            if (!methodSymbol.IsOrdinaryMethodOrLocalFunction())
                return false;

            // We don't need to rename methods that already have an Async suffix
            if (methodSymbol.Name.EndsWith(AsyncSuffix))
                return false;

            // We don't need to rename entry point methods
            if (isEntryPoint)
                return false;

            // Only rename if the return type will change
            if (methodSymbol.ReturnsVoid)
                return !keepVoid;

            return !IsAsyncReturnType(methodSymbol.ReturnType, knownTypes);
        }
    }

    private SyntaxNode? GetContainingFunction(Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        var token = diagnostic.Location.FindToken(cancellationToken);
        var node = token.GetAncestor(IsAsyncSupportingFunctionSyntax);
        return node;
    }

    private async Task<Solution> RenameThenAddAsyncTokenAsync(
        bool keepVoid,
        bool addWarningAnnotation,
        Document document,
        SyntaxNode node,
        IMethodSymbol methodSymbol,
        KnownTaskTypes knownTypes,
        CancellationToken cancellationToken)
    {
        var name = methodSymbol.Name;
        var newName = name + AsyncSuffix;
        var solution = document.Project.Solution;

        // Store the path to this node.  That way we can find it post rename.
        var syntaxPath = new SyntaxPath(node);

        // Rename the method to add the 'Async' suffix, then add the 'async' keyword.
        var newSolution = await Renamer.RenameSymbolAsync(solution, methodSymbol, new SymbolRenameOptions(), newName, cancellationToken).ConfigureAwait(false);

        var newDocument = newSolution.GetRequiredDocument(document.Id);
        var newRoot = await newDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (syntaxPath.TryResolve(newRoot, out SyntaxNode? newNode))
        {
            var semanticModel = await newDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var newMethod = (IMethodSymbol)semanticModel.GetRequiredDeclaredSymbol(newNode, cancellationToken);
            return await FixRelatedSignaturesAsync(keepVoid, addWarningAnnotation, newDocument, newMethod, knownTypes, newNode, cancellationToken).ConfigureAwait(false);
        }

        return newSolution;
    }

    private async Task<Solution> FixRelatedSignaturesAsync(
        bool keepVoid,
        bool addWarningAnnotation,
        Document document,
        IMethodSymbol methodSymbol,
        KnownTaskTypes knownTypes,
        SyntaxNode node,
        CancellationToken cancellationToken)
    {
        var newNode = FixMethodSignature(addAsyncModifier: true, keepVoid, methodSymbol, node, knownTypes);
        if (addWarningAnnotation)
            newNode = newNode.WithAdditionalAnnotations(WarningAnnotation.Create(GetUsedAsDelegateWarningResource()));

        var solution = document.Project.Solution;
        var solutionEditor = new SolutionEditor(solution);
        var mainDocumentEditor = await solutionEditor.GetDocumentEditorAsync(document.Id, cancellationToken).ConfigureAwait(false);

        mainDocumentEditor.ReplaceNode(node, newNode);

        if (!keepVoid && methodSymbol.PartialDefinitionPart is { Locations: [{ } partialDefinitionLocation] })
        {
            var partialDefinitionNode = partialDefinitionLocation.FindNode(cancellationToken);
            var fixedPartialDefinitionNode =
                FixMethodSignature(addAsyncModifier: false, keepVoid, methodSymbol, partialDefinitionNode, knownTypes);

            var partialDefinitionDocument = solution.GetDocument(partialDefinitionNode.SyntaxTree);
            Contract.ThrowIfNull(partialDefinitionDocument);
            var partialDefinitionDocumentEditor = await solutionEditor.GetDocumentEditorAsync(partialDefinitionDocument.Id, cancellationToken).ConfigureAwait(false);
            partialDefinitionDocumentEditor.ReplaceNode(partialDefinitionNode, fixedPartialDefinitionNode);
        }

        return solutionEditor.GetChangedSolution();
    }
}
