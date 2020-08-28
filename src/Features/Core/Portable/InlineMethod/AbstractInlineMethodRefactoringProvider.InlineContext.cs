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
    internal abstract partial class AbstractInlineMethodRefactoringProvider<TInvocationSyntax, TExpressionSyntax, TMethodDeclarationSyntax, TStatementSyntax>
    {
        private readonly struct InlineMethodContext
        {
            /// <summary>
            /// Statements should be inserted before the <see cref="StatementContainingCallee"/>.
            /// </summary>
            public ImmutableArray<SyntaxNode> StatementsToInsertBeforeCallee { get; }

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
            public SyntaxNode StatementContainingCallee { get; }

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
                ImmutableArray<SyntaxNode> statementsToInsertBeforeCallee,
                SyntaxNode statementContainingCallee,
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
            // Example 1 (for identifier):
            // Before:
            // void Caller()
            // {
            //     int i = 10;
            //     Callee(i)
            // }
            // void Callee(int j)
            // {
            //     int i = 100;
            // }
            // After inline it should be:
            // void Caller()
            // {
            //     int i = 10;
            //     int i1 = 100;
            // }
            // void Callee(int j)
            // {
            //     int i = 100;
            // }
            // Example 2 (for variable declaration):
            // Before:
            // void Caller()
            // {
            //     int i = 10;
            //     Callee(out i)
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
                semanticModel,
                calleeInvocationNode,
                inlineExpression,
                methodParametersInfo.ParametersWithIdentifierArgument,
                methodParametersInfo.ParametersToGenerateFreshVariablesFor.SelectAsArray(parameterAndArgument => parameterAndArgument.parameterSymbol),
                methodParametersInfo.ParametersWithVariableDeclarationArgument,
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
            var localDeclarationStatementsNeedInsert = GetLocalDeclarationStatementsNeedInsert(
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
                var singleVariableDeclarationParameter =
                    methodParametersInfo.ParametersWithVariableDeclarationArgument.Single().parameterSymbol;
                var name = renameTable[singleVariableDeclarationParameter];
                var rightHandSideValue = _syntaxFacts.GetRightHandSideOfAssignment(inlineExpression);
                return new InlineMethodContext(
                    localDeclarationStatementsNeedInsert,
                    statementContainsCallee,
                    syntaxGenerator
                        .LocalDeclarationStatement(singleVariableDeclarationParameter.Type, name, rightHandSideValue)
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

        private ImmutableArray<SyntaxNode> GetLocalDeclarationStatementsNeedInsert(
            SyntaxGenerator syntaxGenerator,
            ImmutableArray<(IParameterSymbol parameterSymbol, TExpressionSyntax expression)> parametersToGenerateFreshVariablesFor,
            ImmutableArray<(IParameterSymbol parameterSymbol, string name)> parametersWithVariableDeclarationArgument,
            ImmutableDictionary<ISymbol, string> renameTable)
        {
            var declarationsQuery = parametersToGenerateFreshVariablesFor
                .Select(parameterAndArguments => CreateLocalDeclarationStatement(syntaxGenerator, renameTable, parameterAndArguments));

            var declarationsForVariableDeclarationArgumentQuery = parametersWithVariableDeclarationArgument
                .Select(parameterAndName =>
                    syntaxGenerator.LocalDeclarationStatement(
                        parameterAndName.parameterSymbol.Type,
                        renameTable.TryGetValue(parameterAndName.parameterSymbol, out var newName) ? newName : parameterAndName.name));

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

        private static SyntaxNode CreateLocalDeclarationStatement(
            SyntaxGenerator syntaxGenerator,
            ImmutableDictionary<ISymbol, string> renameTable,
            (IParameterSymbol parameterSymbol, TExpressionSyntax expression) parameterAndExpression)
        {
            var (parameterSymbol, expression) = parameterAndExpression;
            var name = renameTable.TryGetValue(parameterSymbol, out var newName) ? newName : parameterSymbol.Name;
            return syntaxGenerator.LocalDeclarationStatement(parameterSymbol.Type, name, expression);
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
            ImmutableDictionary<IParameterSymbol, TExpressionSyntax> parametersWithLiteralArgument,
            SyntaxGenerator syntaxGenerator,
            ImmutableDictionary<ISymbol, string> renameTable)
        {
            var typeParametersReplacementQuery = calleeMethodSymbol.TypeParameters
                .Zip(calleeMethodSymbol.TypeArguments,
                    (parameter, argument) => (parameter: (ISymbol)parameter,
                        syntaxNode: GenerateTypeSyntax(argument, allowVar: true)));
            var literalArgumentReplacementQuery = parametersWithLiteralArgument
                .Select(parameterAndExpressionPair => (parameter: (ISymbol)parameterAndExpressionPair.Key,
                    syntaxNode: (SyntaxNode)parameterAndExpressionPair.Value));

            // Rename table has all the local identifier needs rename
            return renameTable
                .Select(kvp => (parameter: kvp.Key,
                    syntaxNode: syntaxGenerator.IdentifierName(kvp.Value)))
                .Concat(typeParametersReplacementQuery)
                .Concat(literalArgumentReplacementQuery)
                .ToImmutableDictionary(tuple => tuple.parameter, tuple => tuple.syntaxNode);
        }

        private static ImmutableDictionary<ISymbol, string> ComputeRenameTable(
            ISemanticFactsService semanticFacts,
            SemanticModel semanticModel,
            SyntaxNode calleeInvocationNode,
            SyntaxNode inlineExpression,
            ImmutableDictionary<IParameterSymbol, string> identifierArguments,
            ImmutableArray<IParameterSymbol> parametersNeedGenerateDeclaration,
            ImmutableArray<(IParameterSymbol parameterSymbol, string variableName)> variableDeclarationArguments,
            CancellationToken cancellationToken)
        {
            var renameTable = new Dictionary<ISymbol, string>();
            foreach (var (parameterSymbol, variableName) in identifierArguments.Select(kvp => (kvp.Key, kvp.Value)).Concat(variableDeclarationArguments))
            {
                if (!parameterSymbol.Name.Equals(variableName))
                {
                    var usedNames = renameTable.Values;
                    renameTable[parameterSymbol] = semanticFacts
                        .GenerateUniqueLocalName(
                            semanticModel,
                            inlineExpression,
                            containerOpt: null,
                            variableName,
                            usedNames,
                            cancellationToken).Text;
                }
            }

            var existingSymbolInCalleeQuery = semanticModel.LookupSymbols(inlineExpression.Span.End)
                .Where(symbol => symbol.IsKind(SymbolKind.Local));
            foreach (var symbol in parametersNeedGenerateDeclaration.Concat(existingSymbolInCalleeQuery))
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
    }
}
