// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InlineMethod
{
    internal abstract partial class AbstractInlineMethodRefactoringProvider<TMethodDeclarationSyntax, TStatementSyntax, TExpressionSyntax, TInvocationSyntax>
    {
        private readonly struct InlineMethodContext
        {
            /// <summary>
            /// Statements that should be inserted before the <see cref="StatementContainingCallee"/>.
            /// </summary>
            public ImmutableArray<TStatementSyntax> StatementsToInsertBeforeCallee { get; }

            /// <summary>
            /// Statement containing the callee. All the generated declarations should be put before this node.
            /// Example:
            /// void Caller()
            /// {
            ///     var x = Callee(Foo());
            /// }
            /// void Callee(int i) { DoSomething(); }
            /// LocalDeclarationSyntaxNode of x will the <see cref="StatementContainingCallee"/>.
            /// And if there is any statements needs inserted, it needs to be inserted before this node.
            /// </summary>
            public TStatementSyntax StatementContainingCallee { get; }

            /// <summary>
            /// Inline content for the callee method. It should replace <see cref="SyntaxNodeToReplace"/>.
            /// </summary>
            public SyntaxNode InlineSyntaxNode { get; }

            /// <summary>
            /// SyntaxNode needs to be replaced by <see cref="InlineSyntaxNode"/>
            /// </summary>
            public SyntaxNode SyntaxNodeToReplace { get; }

            /// <summary>
            /// Indicate if <see cref="InlineSyntaxNode"/> has AwaitExpression in it.
            /// </summary>
            public bool ContainsAwaitExpression { get; }

            public InlineMethodContext(
                ImmutableArray<TStatementSyntax> statementsToInsertBeforeCallee,
                TStatementSyntax statementContainingCallee,
                SyntaxNode inlineSyntaxNode,
                SyntaxNode syntaxNodeToReplace,
                bool containsAwaitExpression)
            {
                StatementsToInsertBeforeCallee = statementsToInsertBeforeCallee;
                StatementContainingCallee = statementContainingCallee;
                InlineSyntaxNode = inlineSyntaxNode;
                SyntaxNodeToReplace = syntaxNodeToReplace;
                ContainsAwaitExpression = containsAwaitExpression;
            }
        }

        private async Task<InlineMethodContext> GetInlineMethodContextAsync(
            Document document,
            TMethodDeclarationSyntax calleeMethodNode,
            TInvocationSyntax calleeInvocationNode,
            IMethodSymbol calleeMethodSymbol,
            TExpressionSyntax rawInlineExpression,
            TStatementSyntax statementContainsCallee,
            MethodParametersInfo methodParametersInfo,
            CancellationToken cancellationToken)
        {
            var inlineExpression = rawInlineExpression;
            var syntaxGenerator = SyntaxGenerator.GetGenerator(document);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

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
                calleeMethodNode,
                semanticModel,
                calleeInvocationNode,
                methodParametersInfo.ParametersToGenerateFreshVariablesFor
                    .SelectAsArray(parameterAndArgument => parameterAndArgument.parameterSymbol),
                cancellationToken);

            // For this case, merge the inline content and the variable declaration
            // Before:
            // void Caller()
            // {
            //     Callee(out var x);
            // }
            // void Callee(out int i) => i = 100;
            //
            // After:
            // void Caller()
            // {
            //     int x = 100;
            // }
            // void Callee(out int i) => i = 100;
            var mergeInlineContentAndVariableDeclarationArgument = MergeInlineContentAndVariableDeclarationArgument(
                calleeInvocationNode,
                semanticModel,
                methodParametersInfo.ParametersWithVariableDeclarationArgument
                    .SelectAsArray(paramAndName => paramAndName.parameterSymbol),
                rawInlineExpression,
                cancellationToken);

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
            var localDeclarationStatementsNeedInsert = AbstractInlineMethodRefactoringProvider<TMethodDeclarationSyntax, TStatementSyntax, TExpressionSyntax, TInvocationSyntax>.GetLocalDeclarationStatementsNeedInsert(
                syntaxGenerator,
                methodParametersInfo.ParametersToGenerateFreshVariablesFor,
                mergeInlineContentAndVariableDeclarationArgument
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

            var containsAwaitExpression = ContainsAwaitExpression(rawInlineExpression, calleeMethodNode);

            // Do the replacement work within the callee's body so that it can be inserted to the caller later.
            inlineExpression = await ReplaceAllSyntaxNodesForSymbolAsync(
               document,
               inlineExpression,
               syntaxGenerator,
               replacementTable,
               cancellationToken).ConfigureAwait(false);

            if (mergeInlineContentAndVariableDeclarationArgument)
            {
                var (parameterSymbol, name) = methodParametersInfo.ParametersWithVariableDeclarationArgument.Single();
                var rightHandSideValue = _syntaxFacts.GetRightHandSideOfAssignment(inlineExpression);
                return new InlineMethodContext(
                    localDeclarationStatementsNeedInsert,
                    statementContainsCallee,
                    syntaxGenerator
                        .LocalDeclarationStatement(parameterSymbol.Type, name, rightHandSideValue)
                        .WithTriviaFrom(statementContainsCallee),
                    statementContainsCallee,
                    containsAwaitExpression);
            }

            if (_syntaxFacts.IsThrowStatement(rawInlineExpression.Parent))
            {
                if (_syntaxFacts.IsExpressionStatement(calleeInvocationNode.Parent))
                {
                    return new InlineMethodContext(
                        localDeclarationStatementsNeedInsert,
                        statementContainsCallee,
                        syntaxGenerator.ThrowStatement(inlineExpression).WithTriviaFrom(statementContainsCallee),
                        statementContainsCallee,
                        containsAwaitExpression);
                }

                // Example:
                // Before:
                // void Caller() => Callee()
                // void Callee() { throw new Exception(); }
                // After:
                // void Caller() => throw new Exception();
                // void Callee() { throw new Exception(); }
                // Throw expression is converted to throw statement
                if (CanBeReplacedByThrowExpression(calleeInvocationNode))
                {
                    return new InlineMethodContext(
                        localDeclarationStatementsNeedInsert,
                        statementContainsCallee,
                        syntaxGenerator.ThrowExpression(inlineExpression).WithTriviaFrom(calleeInvocationNode),
                        calleeInvocationNode,
                        containsAwaitExpression);
                }
            }

            var isThrowExpression = _syntaxFacts.IsThrowExpression(inlineExpression);
            if (isThrowExpression)
            {
                // Example:
                // Before:
                // void Caller() { Callee(); }
                // void Callee() => throw new Exception();
                // After:
                // void Caller() { throw new Exception(); }
                // void Callee() => throw new Exception();
                // Throw expression is converted to throw statement
                if (_syntaxFacts.IsExpressionStatement(calleeInvocationNode.Parent))
                {
                    return new InlineMethodContext(
                        localDeclarationStatementsNeedInsert,
                        statementContainsCallee,
                        syntaxGenerator.ThrowStatement(
                            _syntaxFacts.GetExpressionOfThrowExpression(inlineExpression))
                            .WithTriviaFrom(statementContainsCallee),
                        statementContainsCallee,
                        containsAwaitExpression);
                }
            }

            if (_syntaxFacts.IsExpressionStatement(calleeInvocationNode.Parent)
                && !calleeMethodSymbol.ReturnsVoid
                && !IsValidExpressionUnderStatementExpression(inlineExpression))
            {
                // If the callee is invoked as ExpressionStatement, but the inlined expression in the callee can't be
                // placed under ExpressionStatement
                // Example:
                // void Caller()
                // {
                //     Callee();
                // }
                // int Callee()
                // {
                //     return 1;
                // };
                // After it should be:
                // void Caller()
                // {
                //     int temp = 1;
                // }
                // int Callee()
                // {
                //     return 1;
                // };
                // One variable declaration needs to be generated.
                var unusedLocalName =
                    _semanticFactsService.GenerateUniqueLocalName(
                        semanticModel,
                        calleeInvocationNode,
                        containerOpt: null,
                        TemporaryName,
                        renameTable.Values,
                        cancellationToken);

                return new InlineMethodContext(
                    localDeclarationStatementsNeedInsert,
                    statementContainsCallee,
                    syntaxGenerator
                        .LocalDeclarationStatement(calleeMethodSymbol.ReturnType, unusedLocalName.Text, inlineExpression),
                    statementContainsCallee,
                    containsAwaitExpression);
            }

            // Add type cast and parenthesis to the inline expression.
            // It is required to cover cases like:
            // Case 1 (parenthesis added):
            // Before:
            // void Caller() { var x = 3 * Callee(); }
            // int Callee() { return 1 + 2; }
            //
            // After
            // void Caller() { var x = 3 * (1 + 2); }
            // int Callee() { return 1 + 2; }
            //
            // Case 2 (type cast)
            // Before:
            // void Caller() { var x = Callee(); }
            // long Callee() { return 1 }
            //
            // After
            // void Caller() { var x = (long)1; }
            // int Callee() { return 1; }
            //
            // Case 3 (type cast & additional parenthesis)
            // // Before:
            // // void Caller() { var x = Callee()(); }
            // // Func<int> Callee() { return () => 1; }
            // // After:
            // // void Caller() { var x = ((Func<int>)(() => 1))(); }
            // // Func<int> Callee() { return () => 1; }
            if (!calleeMethodSymbol.ReturnsVoid && !isThrowExpression)
            {
                inlineExpression = (TExpressionSyntax)syntaxGenerator.AddParentheses(
                    syntaxGenerator.CastExpression(
                        GenerateTypeSyntax(calleeMethodSymbol.ReturnType, allowVar: false),
                        syntaxGenerator.AddParentheses(inlineExpression)));
            }

            return new InlineMethodContext(
                localDeclarationStatementsNeedInsert,
                statementContainsCallee,
                inlineExpression.WithTriviaFrom(calleeInvocationNode),
                calleeInvocationNode,
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

        private bool ContainsAwaitExpression(TExpressionSyntax inlineExpression, TMethodDeclarationSyntax calleeMethodDeclarationNode)
        {
            // Check if there is await expression. It is used later if the caller should be changed to async
            var awaitExpressions = inlineExpression
                .DescendantNodesAndSelf()
                .Where(node => node != null && _syntaxFacts.IsAwaitExpression(node))
                .ToImmutableArray();
            foreach (var awaitExpression in awaitExpressions)
            {
                var enclosingMethodLikeNode = GetEnclosingMethodLikeNode(awaitExpression);
                if (calleeMethodDeclarationNode.Equals(enclosingMethodLikeNode))
                {
                    return true;
                }
            }

            return false;
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
                    .FindReferencesAsync(symbol, document.Project.Solution, cancellationToken)
                    .ConfigureAwait(false);
                var allSyntaxNodesToReplace = allReferences
                    .SelectMany(reference => reference.Locations
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

            return renameTable
                .Select(kvp => (parameter: kvp.Key,
                    syntaxNode: syntaxGenerator.IdentifierName(kvp.Value)))
                .Concat(parametersWithVariableDeclarationArgumentQuery)
                .Concat(typeParametersReplacementQuery)
                .Concat(literalArgumentReplacementQuery)
                .ToImmutableDictionary(tuple => tuple.parameter, tuple => tuple.syntaxNode);
        }

        private static ImmutableDictionary<ISymbol, string> ComputeRenameTable(
            ISemanticFactsService semanticFacts,
            TMethodDeclarationSyntax calleeMethodNode,
            SemanticModel semanticModel,
            SyntaxNode calleeInvocationNode,
            ImmutableArray<IParameterSymbol> parametersNeedGenerateFreshVariableFor,
            CancellationToken cancellationToken)
        {
            var renameTable = new Dictionary<ISymbol, string>();
            var localSymbolsInCallee = LocalVariableDeclarationVisitor.GetAllSymbols(semanticModel, calleeMethodNode, cancellationToken);
            foreach (var symbol in parametersNeedGenerateFreshVariableFor.Concat(localSymbolsInCallee))
            {
                var usedNames = renameTable.Values;
                renameTable[symbol] = semanticFacts
                    .GenerateUniqueLocalName(
                        semanticModel,
                        calleeInvocationNode,
                        containerOpt: null,
                        symbol.Name,
                        usedNames,
                        cancellationToken).Text;
            }

            return renameTable.ToImmutableDictionary();
        }

        private bool MergeInlineContentAndVariableDeclarationArgument(
            TInvocationSyntax calleeInvocationNode,
            SemanticModel semanticModel,
            ImmutableArray<IParameterSymbol> parametersWithVariableDeclarationArgument,
            TExpressionSyntax inlineExpressionNode,
            CancellationToken cancellationToken)
        {
            if (parametersWithVariableDeclarationArgument.Length == 1
                && _syntaxFacts.IsExpressionStatement(calleeInvocationNode.Parent)
                && semanticModel.GetOperation(inlineExpressionNode, cancellationToken) is ISimpleAssignmentOperation simpleAssignmentOperation
                && simpleAssignmentOperation.Target is IParameterReferenceOperation parameterOperation
                && parameterOperation.Parameter.Equals(parametersWithVariableDeclarationArgument[0]))
            {
                return true;
            }

            return false;
        }

        private class LocalVariableDeclarationVisitor : OperationWalker
        {
            private readonly CancellationToken _cancellationToken;
            private readonly HashSet<ISymbol> _allSymbols;
            private LocalVariableDeclarationVisitor(CancellationToken cancellationToken)
            {
                _cancellationToken = cancellationToken;
                _allSymbols = new HashSet<ISymbol>();
            }

            public static ImmutableHashSet<ISymbol> GetAllSymbols(
                SemanticModel semanticModel,
                TMethodDeclarationSyntax methodDeclarationSyntax,
                CancellationToken cancellationToken)
            {
                var visitor = new LocalVariableDeclarationVisitor(cancellationToken);
                var operation = semanticModel.GetOperation(methodDeclarationSyntax, cancellationToken);
                if (operation != null)
                {
                    visitor.Visit(operation);
                }

                return visitor._allSymbols.ToImmutableHashSet();
            }

            public override void Visit(IOperation operation)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                if (operation is IVariableDeclaratorOperation variableDeclarationOperation)
                {
                    _allSymbols.Add(variableDeclarationOperation.Symbol);
                }

                if (operation is ILocalReferenceOperation localReferenceOperation
                    && localReferenceOperation.IsDeclaration)
                {
                    _allSymbols.Add(localReferenceOperation.Local);
                }

                base.Visit(operation);
            }
        }
    }
}
