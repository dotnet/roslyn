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
    internal abstract partial class AbstractInlineMethodRefactoringProvider<TInvocationNode, TExpression, TArgumentSyntax, TMethodDeclarationSyntax, TIdentifierName>
        where TInvocationNode : SyntaxNode
        where TExpression : SyntaxNode
        where TArgumentSyntax : SyntaxNode
        where TMethodDeclarationSyntax : SyntaxNode
        where TIdentifierName : SyntaxNode
    {
        private class InlineMethodContext
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

            /// <summary>
            /// A preferred name used to generated a declaration when the
            /// inline method has a return value but is not assigned to a variable.
            /// Example:
            /// void Caller()
            /// {
            ///     Callee();
            /// }
            /// int Callee()
            /// {
            ///     return 1;
            /// };
            /// After it should be:
            /// void Caller()
            /// {
            ///     int temp = 1;
            /// }
            /// int Callee()
            /// {
            ///     return 1;
            /// };
            /// </summary>
            private const string TemporaryName = "temp";

            private InlineMethodContext(
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

            public static async Task<InlineMethodContext> GetInlineContextAsync(
                AbstractInlineMethodRefactoringProvider<TInvocationNode, TExpression, TArgumentSyntax, TMethodDeclarationSyntax, TIdentifierName> inlineMethodRefactoringProvider,
                Document document,
                SyntaxNode calleeInvocationNode,
                IMethodSymbol calleeMethodSymbol,
                TMethodDeclarationSyntax calleeMethodDeclarationNode,
                SyntaxNode statementContainsCallee,
                MethodParametersInfo methodParametersInfo,
                CancellationToken cancellationToken)
            {
                var syntaxFacts = inlineMethodRefactoringProvider._syntaxFacts;
                var semanticFactsService = inlineMethodRefactoringProvider._semanticFactsService;
                var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var syntaxGenerator = SyntaxGenerator.GetGenerator(document);
                var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var inlineNode = inlineMethodRefactoringProvider.GetInlineStatement(calleeMethodDeclarationNode);

                // Get the parameterSymbol to identifier's name mappings.
                // Example:
                // void Caller()
                // {
                //     int i = 10;
                //     Callee(i);
                // }
                // void Callee(int a)
                // {
                //     DoSomething(a);
                // }
                // Input is an array of (ParameterSymbol(int a), IdentifierSyntaxNode(int i))
                // Output is an array of (ParameterSymbol(int a), string i)
                var parametersWithIdentifierArgumentToName = methodParametersInfo.ParametersWithIdentifierArgument
                    .Select(parameterAndArgument =>
                        (parameterAndArgument.parameterSymbol, semanticModel.GetSymbolInfo(parameterAndArgument.identifierSyntaxNode, cancellationToken).GetAnySymbol()?.Name))
                    .Where(parameterAndName => parameterAndName.Name != null)
                    .ToImmutableArray();

                // Get the parameterSymbol to variable declaration name mapping.
                // Example:
                // void Caller()
                // {
                //     Callee(out int i);
                // }
                // void Callee(out int a)
                // {
                //     DoSomething(out a);
                // }
                // Input is an array of (ParameterSymbol(int a), IdentifierSyntaxNode(int i))
                // Output is an array of (ParameterSymbol(int a), string i)
                var parametersWithVariableDeclarationArgumentToName = methodParametersInfo
                    .ParametersWithVariableDeclarationArgument
                    .Select(parameterAndArgument =>
                        (parameterAndArgument.parameterSymbol,
                            semanticModel
                                .GetSymbolInfo(parameterAndArgument.variableDeclarationNode, cancellationToken)
                                .GetAnySymbol()?.Name))
                    .Where(parameterAndName => parameterAndName.Name != null)
                    .ToImmutableArray();

                // Based on the information get above, generate a map which the key is the symbol need renaming, value is the new name.
                var renameTable = ComputeRenameTable(
                    semanticFactsService,
                    semanticModel,
                    calleeInvocationNode,
                     inlineNode,
                    parametersWithIdentifierArgumentToName!,
                    methodParametersInfo.OperationsNeedGenerateDeclarations.SelectAsArray(operation => operation.Parameter),
                    parametersWithVariableDeclarationArgumentToName!,
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
                    inlineMethodRefactoringProvider,
                    syntaxGenerator,
                    methodParametersInfo.OperationsNeedGenerateDeclarations,
                    parametersWithVariableDeclarationArgumentToName!,
                    renameTable);

                // Get a table which the key is the symbol needs replacement work. Value is the replacement syntax node
                var replacementTable = ComputeReplacementTable(
                    inlineMethodRefactoringProvider,
                    calleeMethodSymbol,
                    methodParametersInfo.ParametersWithLiteralArgument,
                    methodParametersInfo.ParametersWithDefaultValue,
                    syntaxGenerator,
                    renameTable);

                // Do the replacement work within the callee's body so that it can be inserted to the caller later.
                inlineNode = (TExpression)await ReplaceAllSyntaxNodesForSymbolAsync(
                   document,
                   inlineNode,
                   syntaxGenerator,
                   root,
                   replacementTable,
                   cancellationToken).ConfigureAwait(false);

                // Check if there is await expression. It is used later if the caller should be changed to async
                var containsAwaitExpression = inlineNode
                    .DescendantNodesAndSelf()
                    .Any(node => node != null && syntaxFacts.IsAwaitExpression(node));

                if (syntaxFacts.IsExpressionStatement(statementContainsCallee)
                    && !calleeMethodSymbol.ReturnsVoid
                    && !syntaxFacts.IsArgument(calleeInvocationNode.Parent))
                {
                    // If the callee is invoked like
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
                        semanticFactsService.GenerateUniqueLocalName(
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
                            .LocalDeclarationStatement(calleeMethodSymbol.ReturnType, unusedLocalName.Text,
                                 inlineNode),
                        statementContainsCallee,
                        containsAwaitExpression);
                }

                return new InlineMethodContext(
                    localDeclarationStatementsNeedInsert,
                    statementContainsCallee,
                    inlineMethodRefactoringProvider.Parenthesize(inlineNode)
                        // add the trivia of the calleeInvocationSyntaxNode so that the format is correct
                        .WithLeadingTrivia(calleeInvocationNode.GetLeadingTrivia())
                        .WithTrailingTrivia(calleeInvocationNode.GetTrailingTrivia()),
                    calleeInvocationNode,
                    containsAwaitExpression);
            }

            private static ImmutableArray<SyntaxNode> GetLocalDeclarationStatementsNeedInsert(
                AbstractInlineMethodRefactoringProvider<TInvocationNode, TExpression, TArgumentSyntax, TMethodDeclarationSyntax, TIdentifierName> inlineMethodRefactoringProvider,
                SyntaxGenerator syntaxGenerator,
                ImmutableArray<IArgumentOperation> operationsNeedGenerateDeclarations,
                ImmutableArray<(IParameterSymbol parameterSymbol, string name)> parametersWithVariableDeclarationArgument,
                ImmutableDictionary<ISymbol, string> renameTable)
                => operationsNeedGenerateDeclarations
                    .Select(parameterAndArguments =>
                        CreateLocalDeclarationStatement(inlineMethodRefactoringProvider, syntaxGenerator, renameTable, parameterAndArguments))
                    .Concat(parametersWithVariableDeclarationArgument
                        .Select(parameterAndName =>
                            syntaxGenerator.LocalDeclarationStatement(parameterAndName.parameterSymbol.Type,
                                parameterAndName.name)))
                    .ToImmutableArray();

            private static SyntaxNode CreateLocalDeclarationStatement(
                AbstractInlineMethodRefactoringProvider<TInvocationNode, TExpression, TArgumentSyntax, TMethodDeclarationSyntax, TIdentifierName> inlineMethodRefactoringProvider,
                SyntaxGenerator syntaxGenerator,
                ImmutableDictionary<ISymbol, string> renameTable,
                IArgumentOperation argumentOperation)
            {
                var parameterSymbol = argumentOperation.Parameter;
                var name = renameTable.ContainsKey(parameterSymbol) ? renameTable[parameterSymbol] : parameterSymbol.Name;
                // Check has been done before to make sure there is only one child.
                var argumentExpressionOperation = argumentOperation.Children.First();
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
                        inlineMethodRefactoringProvider.GenerateArrayInitializerExpression(arrayCreationOperation.Initializer.ElementValues.SelectAsArray(op => op.Syntax)));
                }
                else
                {
                    // In all the other cases, one parameter should only maps to one arguments.
                    return syntaxGenerator.LocalDeclarationStatement(parameterSymbol.Type, name, argumentExpressionOperation.Syntax);
                }
            }

            private static async Task<SyntaxNode> ReplaceAllSyntaxNodesForSymbolAsync(
                Document document,
                TExpression inlineNode,
                SyntaxGenerator syntaxGenerator,
                SyntaxNode root,
                ImmutableDictionary<ISymbol, SyntaxNode> replacementTable,
                CancellationToken cancellationToken)
            {
                var editor = new SyntaxEditor(inlineNode, syntaxGenerator);

                foreach (var kvp in replacementTable)
                {
                    var allReferences = await SymbolFinder
                        .FindReferencesAsync(kvp.Key, document.Project.Solution, cancellationToken)
                        .ConfigureAwait(false);
                    var allSyntaxNodesToReplace = allReferences
                        .SelectMany(reference => reference.Locations
                            .Select(location => root.FindNode(location.Location.SourceSpan)))
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

                return editor.GetChangedRoot();
            }

            /// <summary>
            /// Generate a dictionary which key is the symbol in Callee (include local variable and parameter), value
            /// is the replacement syntax node for all its occurence.
            /// </summary>
            private static ImmutableDictionary<ISymbol, SyntaxNode> ComputeReplacementTable(
                AbstractInlineMethodRefactoringProvider<TInvocationNode, TExpression, TArgumentSyntax, TMethodDeclarationSyntax, TIdentifierName>
                    inlineMethodRefactoringProvider,
                IMethodSymbol calleeMethodSymbol,
                ImmutableArray<(IParameterSymbol parameterSymbol, TExpression literalExpression)> parametersWithLiteralArgument,
                ImmutableArray<IParameterSymbol> parametersWithDefaultValue,
                SyntaxGenerator syntaxGenerator,
                ImmutableDictionary<ISymbol, string> renameTable)
            {
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
                var typeParametersReplacementQuery = calleeMethodSymbol.TypeParameters
                    .Zip(calleeMethodSymbol.TypeArguments,
                        (parameter, argument) => (parameter: (ISymbol)parameter,
                            syntaxNode: inlineMethodRefactoringProvider.GenerateTypeSyntax(argument)));
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
                var defaultValueReplacementQuery = parametersWithDefaultValue
                    .Select(symbol => (parameter: (ISymbol)symbol,
                        syntaxNode: syntaxGenerator.LiteralExpression(symbol.ExplicitDefaultValue)));
                var literalArgumentReplacementQuery = parametersWithLiteralArgument
                    .Select(parameterAndArgument => (parameter: (ISymbol)parameterAndArgument.parameterSymbol,
                        syntaxNode: (SyntaxNode)parameterAndArgument.literalExpression));

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
                // The Rename table has all the local identifier needs rename
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
                ImmutableArray<(IParameterSymbol parameterSymbol, string variableName)> identifierArguments,
                ImmutableArray<IParameterSymbol> parametersNeedGenerateDeclaration,
                ImmutableArray<(IParameterSymbol parameterSymbol, string variableName)> variableDeclarationArguments,
                CancellationToken cancellationToken)
            {
                var renameTable = new Dictionary<ISymbol, string>();
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
                foreach (var (parameterSymbol, variableName) in identifierArguments.Concat(variableDeclarationArguments))
                {
                    if (!parameterSymbol.Name.Equals(variableName))
                    {
                        var usedNames = renameTable.Values;
                        renameTable[parameterSymbol] = semanticFacts
                            .GenerateUniqueLocalName(
                                semanticModel,
                                inlineSyntaxNode,
                                null,
                                variableName,
                                usedNames,
                                cancellationToken).Text;
                    }
                }

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
                var existingSymbolInCalleeQuery = semanticModel.LookupSymbols(inlineSyntaxNode.Span.End)
                    .Where(symbol => symbol.IsKind(SymbolKind.Local));
                foreach (var symbol in parametersNeedGenerateDeclaration.Concat(existingSymbolInCalleeQuery))
                {
                    var usedNames = renameTable.Values;
                    renameTable[symbol] = semanticFacts
                        .GenerateUniqueLocalName(
                            semanticModel,
                            calleeInvocationNode,
                            null,
                            symbol.Name,
                            usedNames,
                            cancellationToken).Text;
                }

                return renameTable.ToImmutableDictionary();
            }
        }
    }
}
