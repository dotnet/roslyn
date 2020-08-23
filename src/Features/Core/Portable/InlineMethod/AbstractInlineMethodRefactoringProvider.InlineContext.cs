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

namespace Microsoft.CodeAnalysis.InlineMethod
{
    internal abstract partial class AbstractInlineMethodRefactoringProvider<TInvocationSyntax, TExpressionSyntax, TMethodDeclarationSyntax, TStatementSyntax, TLocalDeclarationSyntax>
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
            /// Indicate is <see cref="InlineSyntaxNode"/> has Await Expression.
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
            TInvocationSyntax calleeInvocationNode,
            IMethodSymbol calleeMethodSymbol,
            TMethodDeclarationSyntax calleeMethodNode,
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
                methodParametersInfo.OperationsToGenerateFreshVariablesFor.SelectAsArray(operation => operation.Parameter),
                methodParametersInfo.ParametersWithVariableDeclarationArgument,
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
                methodParametersInfo.OperationsToGenerateFreshVariablesFor,
                methodParametersInfo.ParametersWithVariableDeclarationArgument,
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
            //     Callee<int>();
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
            //     Callee(20)
            // }
            // void Callee(int i, int j = 10)
            // {
            //     Bar(20, 10);
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
            //     Callee(a, b)
            // }
            // void Callee(int i, int j = 10)
            // {
            //     Bar(a, b, out int c1);
            // }
            var replacementTable = ComputeReplacementTable(
                calleeMethodSymbol,
                methodParametersInfo.ParametersWithLiteralArgument,
                methodParametersInfo.ParametersWithDefaultValue,
                syntaxGenerator,
                renameTable);

            // Do the replacement work within the callee's body so that it can be inserted to the caller later.
            inlineExpression = await ReplaceAllSyntaxNodesForSymbolAsync(
               document,
               inlineExpression,
               syntaxGenerator,
               replacementTable,
               cancellationToken).ConfigureAwait(false);

            // Check if there is await expression. It is used later if the caller should be changed to async
            var containsAwaitExpression = ContainsAwaitExpression(rawInlineExpression, calleeMethodNode);

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
                        null,
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

            // Handle the special cases for C# if the return type is delegate.
            // Case 1:
            // Before:
            // void Caller() { var x = Callee(); }
            // Action Callee() { return () => {}; }
            //
            // After inline it should be
            // void Caller() { Action x = () => {};}
            // Action Callee() { return () => {}; }
            // because 'var' can't be used as the delegate type in local declaration syntax
            // For VB, 'Dim' can be used for 'Sub' and 'Function'
            //
            // Case 2:
            // Before:
            // void Caller() { var x = Callee()(); }
            // Func<int> Callee() { return () => 1; }
            // After:
            // void Caller() { var x = ((Func<int>)(() => 1))(); }
            // Func<int> Callee() { return () => 1; }
            // This is also not a problem for VB
            if (calleeMethodSymbol.ReturnType.IsDelegateType()
                && TryGetInlineSyntaxNodeAndReplacementNodeForDelegate(
                    calleeInvocationNode,
                    calleeMethodSymbol,
                    inlineExpression,
                    statementContainsCallee,
                    syntaxGenerator,
                    out var inlineExpresionNode,
                    out var syntaxNodeToReplace))
            {
                return new InlineMethodContext(
                    localDeclarationStatementsNeedInsert,
                    statementContainsCallee,
                    inlineExpresionNode!,
                    syntaxNodeToReplace!,
                    containsAwaitExpression);
            }

            return new InlineMethodContext(
                localDeclarationStatementsNeedInsert,
                statementContainsCallee,
                Parenthesize(inlineExpression)
                    // add the trivia of the calleeInvocationSyntaxNode to make sure the format is correct
                    .WithTriviaFrom(calleeInvocationNode),
                calleeInvocationNode,
                containsAwaitExpression);
        }

        private ImmutableArray<SyntaxNode> GetLocalDeclarationStatementsNeedInsert(
            SyntaxGenerator syntaxGenerator,
            ImmutableArray<IArgumentOperation> operationsToGenerateFreshVariablesFor,
            ImmutableArray<(IParameterSymbol parameterSymbol, string name)> parametersWithVariableDeclarationArgument,
            ImmutableDictionary<ISymbol, string> renameTable)
        {
            var declarationsQuery = operationsToGenerateFreshVariablesFor
                .Select(parameterAndArguments => CreateLocalDeclarationStatement(syntaxGenerator, renameTable, parameterAndArguments));

            var declarationsForVariableDeclarationArgumentQuery = parametersWithVariableDeclarationArgument
                .Select(parameterAndName =>
                    syntaxGenerator.LocalDeclarationStatement(parameterAndName.parameterSymbol.Type, parameterAndName.name));

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

        private SyntaxNode CreateLocalDeclarationStatement(
            SyntaxGenerator syntaxGenerator,
            ImmutableDictionary<ISymbol, string> renameTable,
            IArgumentOperation argumentOperation)
        {
            var parameterSymbol = argumentOperation.Parameter;
            var name = renameTable.TryGetValue(parameterSymbol, out var newName) ? newName : parameterSymbol.Name;
            // Check has been done before to make sure there is only one child.
            var argumentExpressionOperation = argumentOperation.Children.Single();
            if (argumentOperation.ArgumentKind == ArgumentKind.ParamArray
                && argumentExpressionOperation is IArrayCreationOperation arrayCreationOperation
                && argumentOperation.IsImplicit)
            {
                // if this argument is a param array & the array creation operation is implicitly generated,
                // it means it is in this format:
                // void caller() { Callee(1, 2, 3); }
                // void Callee(params int[] x) { }
                // Collect each of these arguments and generate a new array for it.
                // Note: it could be empty.
                return syntaxGenerator.LocalDeclarationStatement(
                    parameterSymbol.Type,
                    name,
                    GenerateArrayInitializerExpression(arrayCreationOperation.Initializer.ElementValues.SelectAsArray(op => op.Syntax)));
            }
            else
            {
                // In all the other cases, one parameter should only maps to one arguments.
                return syntaxGenerator.LocalDeclarationStatement(parameterSymbol.Type, name, argumentExpressionOperation.Syntax);
            }
        }

        private static async Task<TExpressionSyntax> ReplaceAllSyntaxNodesForSymbolAsync(
            Document document,
            TExpressionSyntax inlineExpression,
            SyntaxGenerator syntaxGenerator,
            ImmutableDictionary<ISymbol, SyntaxNode> replacementTable,
            CancellationToken cancellationToken)
        {
            var editor = new SyntaxEditor(inlineExpression, syntaxGenerator);

            foreach (var kvp in replacementTable)
            {
                var allReferences = await SymbolFinder
                    .FindReferencesAsync(kvp.Key, document.Project.Solution, cancellationToken)
                    .ConfigureAwait(false);
                var allSyntaxNodesToReplace = allReferences
                    .SelectMany(reference => reference.Locations
                        .Select(location => location.Location.FindNode(getInnermostNodeForTie: true, cancellationToken)))
                    .ToImmutableArray();

                foreach (var nodeToReplace in allSyntaxNodesToReplace)
                {
                    if (editor.OriginalRoot.Contains(nodeToReplace))
                    {
                        var replacementNodeWithTrivia = kvp.Value
                            .WithLeadingTrivia(nodeToReplace.GetLeadingTrivia())
                            .WithTrailingTrivia(nodeToReplace.GetTrailingTrivia());
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
            ImmutableArray<IParameterSymbol> parametersWithDefaultValue,
            SyntaxGenerator syntaxGenerator,
            ImmutableDictionary<ISymbol, string> renameTable)
        {
            var typeParametersReplacementQuery = calleeMethodSymbol.TypeParameters
                .Zip(calleeMethodSymbol.TypeArguments,
                    (parameter, argument) => (parameter: (ISymbol)parameter,
                        syntaxNode: GenerateTypeSyntax(argument, allowVar: true)));
            var defaultValueReplacementQuery = parametersWithDefaultValue
                .Select(symbol => (parameter: (ISymbol)symbol,
                    syntaxNode: syntaxGenerator.LiteralExpression(symbol.ExplicitDefaultValue)));
            var literalArgumentReplacementQuery = parametersWithLiteralArgument
                .Select(parameterAndExpressionPair => (parameter: (ISymbol)parameterAndExpressionPair.Key,
                    syntaxNode: (SyntaxNode)parameterAndExpressionPair.Value));

            // Rename table has all the local identifier needs rename
            return renameTable
                .Select(kvp => (parameter: kvp.Key,
                    syntaxNode: syntaxGenerator.IdentifierName(kvp.Value)))
                .Concat(typeParametersReplacementQuery)
                .Concat(defaultValueReplacementQuery)
                .Concat(literalArgumentReplacementQuery)
                .ToImmutableDictionary(tuple => tuple.parameter, tuple => tuple.syntaxNode);
        }

        private static ImmutableDictionary<ISymbol, string> ComputeRenameTable(
            ISemanticFactsService semanticFacts,
            SemanticModel semanticModel,
            SyntaxNode calleeInvocationNode,
            SyntaxNode inlineSyntaxNode,
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
                            inlineSyntaxNode,
                            containerOpt: null,
                            variableName,
                            usedNames,
                            cancellationToken).Text;
                }
            }

            var existingSymbolInCalleeQuery = semanticModel.LookupSymbols(inlineSyntaxNode.Span.End)
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
    }
}
