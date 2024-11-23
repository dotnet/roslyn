// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InlineMethod;

internal abstract partial class AbstractInlineMethodRefactoringProvider<TMethodDeclarationSyntax, TStatementSyntax, TExpressionSyntax, TInvocationSyntax>
{
    private readonly struct InlineMethodContext(
        ImmutableArray<TStatementSyntax> statementsToInsertBeforeInvocationOfCallee,
        TExpressionSyntax inlineExpression,
        bool containsAwaitExpression)
    {
        /// <summary>
        /// Statements that should be inserted to before the invocation location of callee.
        /// </summary>
        public ImmutableArray<TStatementSyntax> StatementsToInsertBeforeInvocationOfCallee { get; } = statementsToInsertBeforeInvocationOfCallee;

        /// <summary>
        /// Inline content for the callee method.
        /// </summary>
        public TExpressionSyntax InlineExpression { get; } = inlineExpression;

        /// <summary>
        /// Indicate if <see cref="InlineExpression"/> has AwaitExpression in it.
        /// </summary>
        public bool ContainsAwaitExpression { get; } = containsAwaitExpression;
    }

    private async Task<InlineMethodContext> GetInlineMethodContextAsync(
        Document document,
        TMethodDeclarationSyntax calleeMethodNode,
        TInvocationSyntax calleeInvocationNode,
        IMethodSymbol calleeMethodSymbol,
        TExpressionSyntax rawInlineExpression,
        MethodParametersInfo methodParametersInfo,
        CancellationToken cancellationToken)
    {
        var inlineExpression = rawInlineExpression;
        var syntaxGenerator = SyntaxGenerator.GetGenerator(document);
        var callerSemanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var calleeDocument = document.Project.Solution.GetRequiredDocument(calleeMethodNode.SyntaxTree);
        var calleeSemanticModel = await calleeDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        // Generate a map which the key is the symbol need renaming, value is the new name.
        // After inlining, there might be naming conflict because
        // case 1: caller's identifier is introduced to callee.
        // Example (for identifier):
        // Before:
        // void Caller()
        // {
        //     int i = 10;
        //     Callee(i)
        // }
        // void Callee(int j)
        // {
        //     DoSomething(out var i, j);
        // }
        // After inline it should be:
        // void Caller()
        // {
        //     int i = 10;
        //     DoSomething(out var i1, i);
        // }
        // void Callee(int j)
        // {
        //     DoSomething(out var i, j);
        // }
        // Case 2: callee's parameter is introduced to caller
        // Before:
        // void Caller()
        // {
        //     int i = 0;
        //     int j = 1;
        //     Callee(Foo())
        // }
        // void Callee(int i)
        // {
        //     Bar(i, out int j);
        // }
        // After:
        // void Caller()
        // {
        //     int i = 10;
        //     int j = 1;
        //     int i1 = Foo();
        //     Bar(i1, out int j2);
        // }
        // void Callee(int i)
        // {
        //     Bar(i, out int j);
        // }
        var renameTable = ComputeRenameTable(
            _semanticFactsService,
            callerSemanticModel,
            calleeSemanticModel,
            calleeInvocationNode,
            rawInlineExpression,
            methodParametersInfo.ParametersToGenerateFreshVariablesFor
                .SelectAsArray(parameterAndArgument => parameterAndArgument.parameterSymbol), cancellationToken);

        // Generate all the statements need to be put in the caller.
        // Use the parameter's name to generate declarations in caller might cause conflict in the caller,
        // so the rename table is needed to provide with a valid name.
        // Example:
        // Before:
        // void Caller(int i, int j)
        // {
        //     Callee(Foo(), Bar());
        // }
        // void Callee(int i, int j)
        // {
        //     DoSomething(i, j);
        // }
        // After:
        // void Caller(int i, int j)
        // {
        //     int i1 = Foo();
        //     int j1 = Bar();
        //     DoSomething(i1, j1)
        // }
        // void Callee(int i, int j)
        // {
        //     DoSomething(i, j);
        // }
        // Here two declaration is generated.
        // Another case is
        // void Caller()
        // {
        //     Callee(out int i)
        // }
        // void Callee(out int j)
        // {
        //     DoSomething(out j, out int i);
        // }
        // After:
        // void Caller()
        // {
        //     int i = 10;
        //     DoSomething(out i, out int i2);
        // }
        // void Callee(out int j)
        // {
        //     DoSomething(out j, out int i);
        // }
        var localDeclarationStatementsNeedInsert = GetLocalDeclarationStatementsNeedInsert(
            syntaxGenerator,
            methodParametersInfo.ParametersToGenerateFreshVariablesFor,
            methodParametersInfo.MergeInlineContentAndVariableDeclarationArgument
                ? ImmutableArray<(IParameterSymbol, string)>.Empty
                : methodParametersInfo.ParametersWithVariableDeclarationArgument,
            renameTable);

        // Get a table which the key is the symbol needs replacement. Value is the replacement syntax node
        // Included 3 cases:
        // 1. Type arguments (generics)
        // Example:
        // Before:
        // void Caller()
        // {
        //     Callee<int>();
        // }
        // void Callee<T>() => Print(typeof<T>);
        // After:
        // void Caller()
        // {
        //     Print(typeof<int>);
        // }
        // void Callee<T>() => Print(typeof<int>);
        // 2. Literal replacement
        // Example:
        // Before:
        // void Caller()
        // {
        //     Callee(20)
        // }
        // void Callee(int i, int j = 10)
        // {
        //     Bar(i, j);
        // }
        // After:
        // void Caller()
        // {
        //     Bar(20, 10);
        // }
        // void Callee(int i, int j = 10)
        // {
        //     Bar(i, j);
        // }
        // 3. Identifier
        // Example:
        // Before:
        // void Caller()
        // {
        //     int a, b, c;
        //     Callee(a, b)
        // }
        // void Callee(int i, int j)
        // {
        //     Bar(i, j, out int c);
        // }
        // After:
        // void Caller()
        // {
        //     int a, b, c;
        //     Bar(a, b, out int c1);
        // }
        // void Callee(int i, int j = 10)
        // {
        //     Bar(i, j, out int c);
        // }
        var replacementTable = ComputeReplacementTable(
            calleeMethodSymbol,
            methodParametersInfo.ParametersWithVariableDeclarationArgument,
            methodParametersInfo.ParametersToReplace,
            syntaxGenerator,
            renameTable);

        var containsAwaitExpression = ContainsAwaitExpression(rawInlineExpression);

        // Do the replacement work within the callee's body so that it can be inserted to the caller later.
        inlineExpression = await ReplaceAllSyntaxNodesForSymbolAsync(
           calleeDocument,
           inlineExpression,
           syntaxGenerator,
           replacementTable,
           cancellationToken).ConfigureAwait(false);

        return new InlineMethodContext(
            localDeclarationStatementsNeedInsert,
            inlineExpression,
            containsAwaitExpression);
    }

    private static ImmutableArray<TStatementSyntax> GetLocalDeclarationStatementsNeedInsert(
        SyntaxGenerator syntaxGenerator,
        ImmutableArray<(IParameterSymbol parameterSymbol, TExpressionSyntax expression)> parametersToGenerateFreshVariablesFor,
        ImmutableArray<(IParameterSymbol parameterSymbol, string identifierName)> parametersWithVariableDeclarationArgument,
        ImmutableDictionary<ISymbol, string> renameTable)
    {
        var declarationsQuery = parametersToGenerateFreshVariablesFor
            .Select(parameterAndArguments => CreateLocalDeclarationStatement(syntaxGenerator, renameTable, parameterAndArguments));

        var declarationsForVariableDeclarationArgumentQuery = parametersWithVariableDeclarationArgument
            .Select(parameterAndName =>
                (TStatementSyntax)syntaxGenerator.LocalDeclarationStatement(
                    parameterAndName.parameterSymbol.Type,
                    parameterAndName.identifierName));

        return declarationsQuery.Concat(declarationsForVariableDeclarationArgumentQuery).ToImmutableArray();
    }

    private bool ContainsAwaitExpression(TExpressionSyntax inlineExpression)
    {
        // Check if there is await expression. It is used later if the caller should be changed to async
        var awaitExpressions = inlineExpression
            .DescendantNodesAndSelf(node => !_syntaxFacts.IsAnonymousFunctionExpression(node))
            .Where(_syntaxFacts.IsAwaitExpression)
            .ToImmutableArray();
        return !awaitExpressions.IsEmpty;
    }

    private static TStatementSyntax CreateLocalDeclarationStatement(
        SyntaxGenerator syntaxGenerator,
        ImmutableDictionary<ISymbol, string> renameTable,
        (IParameterSymbol parameterSymbol, TExpressionSyntax expression) parameterAndExpression)
    {
        var (parameterSymbol, expression) = parameterAndExpression;
        var name = renameTable.TryGetValue(parameterSymbol, out var newName) ? newName : parameterSymbol.Name;
        return (TStatementSyntax)syntaxGenerator.LocalDeclarationStatement(parameterSymbol.Type, name, expression);
    }

    private static async Task<TExpressionSyntax> ReplaceAllSyntaxNodesForSymbolAsync(
        Document document,
        TExpressionSyntax inlineExpression,
        SyntaxGenerator syntaxGenerator,
        ImmutableDictionary<ISymbol, SyntaxNode> replacementTable,
        CancellationToken cancellationToken)
    {
        var editor = new SyntaxEditor(inlineExpression, syntaxGenerator);

        foreach (var (symbol, syntaxNode) in replacementTable)
        {
            var allReferences = await SymbolFinder
                .FindReferencesAsync(symbol, document.Project.Solution, ImmutableHashSet<Document>.Empty.Add(document), cancellationToken)
                .ConfigureAwait(false);
            var allSyntaxNodesToReplace = allReferences
                .SelectMany(reference => reference.Locations
                    .Where(location => !location.IsImplicit)
                    .Select(location => location.Location.FindNode(getInnermostNodeForTie: true, cancellationToken)))
                .ToImmutableArray();

            foreach (var nodeToReplace in allSyntaxNodesToReplace)
            {
                if (editor.OriginalRoot.Contains(nodeToReplace))
                {
                    var replacementNodeWithTrivia = syntaxNode.WithTriviaFrom(nodeToReplace);
                    editor.ReplaceNode(nodeToReplace, replacementNodeWithTrivia);
                }
            }
        }

        return (TExpressionSyntax)editor.GetChangedRoot();
    }

    /// <summary>
    /// Generate a dictionary which key is the symbol in Callee (include local variable and parameter), value
    /// is the replacement syntax node for all its occurence.
    /// </summary>
    private ImmutableDictionary<ISymbol, SyntaxNode> ComputeReplacementTable(
        IMethodSymbol calleeMethodSymbol,
        ImmutableArray<(IParameterSymbol parameter, string identifierName)> parametersWithVariableDeclarationArgument,
        ImmutableDictionary<IParameterSymbol, TExpressionSyntax> parametersToReplace,
        SyntaxGenerator syntaxGenerator,
        ImmutableDictionary<ISymbol, string> renameTable)
    {
        var typeParametersReplacementQuery = calleeMethodSymbol.TypeParameters
            .Zip(calleeMethodSymbol.TypeArguments,
                (parameter, argument) => (parameter: (ISymbol)parameter,
                    syntaxNode: GenerateTypeSyntax(argument, allowVar: true)));
        var literalArgumentReplacementQuery = parametersToReplace
            .Select(parameterAndExpressionPair => (parameter: (ISymbol)parameterAndExpressionPair.Key,
                syntaxNode: (SyntaxNode)parameterAndExpressionPair.Value));

        var parametersWithVariableDeclarationArgumentQuery = parametersWithVariableDeclarationArgument
            .Select(parameterAndName => (parameter: (ISymbol)parameterAndName.parameter,
                syntaxNode: syntaxGenerator.IdentifierName(parameterAndName.identifierName)));

        var parametersNeedRenameQuery = renameTable
            .Select(kvp => (parameter: kvp.Key, syntaxNode: syntaxGenerator.IdentifierName(kvp.Value)));

        return parametersNeedRenameQuery
            .Concat(parametersWithVariableDeclarationArgumentQuery)
            .Concat(typeParametersReplacementQuery)
            .Concat(literalArgumentReplacementQuery)
            .ToImmutableDictionary(tuple => tuple.parameter, tuple => tuple.syntaxNode);
    }

    private static ImmutableDictionary<ISymbol, string> ComputeRenameTable(
        ISemanticFactsService semanticFacts,
        SemanticModel callerSemanticModel,
        SemanticModel calleeSemanticModel,
        SyntaxNode calleeInvocationNode,
        TExpressionSyntax rawInlineExpression,
        ImmutableArray<IParameterSymbol> parametersNeedGenerateFreshVariableFor,
        CancellationToken cancellationToken)
    {
        var renameTable = new Dictionary<ISymbol, string>();
        var localSymbolsInCallee = LocalVariableDeclarationVisitor
            .GetAllSymbols(calleeSemanticModel, rawInlineExpression, cancellationToken);
        foreach (var symbol in parametersNeedGenerateFreshVariableFor.Concat(localSymbolsInCallee))
        {
            var usedNames = renameTable.Values;
            renameTable[symbol] = semanticFacts
                .GenerateUniqueLocalName(
                    callerSemanticModel,
                    calleeInvocationNode,
                    container: null,
                    symbol.Name,
                    usedNames,
                    cancellationToken).Text;
        }

        return renameTable.ToImmutableDictionary();
    }

    private sealed class LocalVariableDeclarationVisitor : OperationWalker
    {
        private readonly CancellationToken _cancellationToken;
        private readonly HashSet<ISymbol> _allSymbols = [];

        private LocalVariableDeclarationVisitor(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
        }

        public static ImmutableHashSet<ISymbol> GetAllSymbols(
            SemanticModel semanticModel,
            TExpressionSyntax methodDeclarationSyntax,
            CancellationToken cancellationToken)
        {
            var visitor = new LocalVariableDeclarationVisitor(cancellationToken);
            var operation = semanticModel.GetOperation(methodDeclarationSyntax, cancellationToken);
            visitor.Visit(operation);

            return [.. visitor._allSymbols];
        }

        public override void Visit(IOperation? operation)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            if (operation == null)
            {
                return;
            }

            if (operation is IVariableDeclaratorOperation variableDeclarationOperation)
            {
                _allSymbols.Add(variableDeclarationOperation.Symbol);
            }

            if (operation is ILocalReferenceOperation localReferenceOperation
                && localReferenceOperation.IsDeclaration)
            {
                _allSymbols.Add(localReferenceOperation.Local);
            }

            // Stop when meet lambda or local function
            if (operation.Kind is OperationKind.AnonymousFunction or OperationKind.LocalFunction)
            {
                return;
            }

            base.Visit(operation);
        }
    }
}
