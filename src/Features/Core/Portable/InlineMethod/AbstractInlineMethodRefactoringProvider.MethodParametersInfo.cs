// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InlineMethod;

internal abstract partial class AbstractInlineMethodRefactoringProvider<TMethodDeclarationSyntax, TStatementSyntax, TExpressionSyntax, TInvocationSyntax>
{
    /// <summary>
    /// Information about the callee method parameters to compute <see cref="InlineMethodContext"/>.
    /// </summary>
    private readonly struct MethodParametersInfo(
        ImmutableArray<(IParameterSymbol parameterSymbol, string name)> parametersWithVariableDeclarationArgument,
        ImmutableArray<(IParameterSymbol parameterSymbol, TExpressionSyntax initExpression)> parametersToGenerateFreshVariablesFor,
        ImmutableDictionary<IParameterSymbol, TExpressionSyntax> parametersToReplace,
        bool mergeInlineContentAndVariableDeclarationArgument)
    {
        /// <summary>
        /// Parameters map to variable declaration argument's name.
        /// This is only used for C# to support the 'out' variable declaration. For VB it should always be empty.
        /// Before:
        /// void Caller()
        /// {
        ///     Callee(out var x);
        /// }
        /// void Callee(out int i) => i = 100;
        ///
        /// After:
        /// void Caller()
        /// {
        ///     int x = 100;
        /// }
        /// void Callee(out int i) => i = 100;
        /// </summary>
        public ImmutableArray<(IParameterSymbol parameterSymbol, string name)> ParametersWithVariableDeclarationArgument { get; } = parametersWithVariableDeclarationArgument;

        /// <summary>
        /// Operations that represent Parameter has argument but the argument is not identifier or literal.
        /// For these parameters they are considered to be put into a declaration statement after inlining.
        /// Note: params array could maps to multiple/zero arguments.
        /// Example:
        /// Before:
        /// void Caller(bool x)
        /// {
        ///     Callee(Foo(), x ? Foo() : Bar())
        /// }
        /// void Callee(int a, int b)
        /// {
        ///     DoSomething(a, b);
        /// }
        /// After:
        /// void Caller(bool x)
        /// {
        ///     int a = Foo();
        ///     int b = x ? Foo() : Bar();
        ///     DoSomething(a, b);
        /// }
        /// void Callee(int a, int b)
        /// {
        ///     DoSomething(a, b);
        /// }
        /// </summary>
        public ImmutableArray<(IParameterSymbol parameterSymbol, TExpressionSyntax initExpression)> ParametersToGenerateFreshVariablesFor { get; } = parametersToGenerateFreshVariablesFor;

        /// <summary>
        /// A dictionary that contains Parameter that should be directly replaced. Key is the parameter and Value is the replacement exprssion
        /// It includes
        /// 1. Parameter mapping to literal expression
        /// 2. Parameter that has default value, and it has no argument. It should be replaced by the default value.
        /// 3. Parameter mapping to identifier expression
        /// Before:
        /// void Caller(int i, int j, bool[] k)
        /// {
        ///     Callee(i, j, k);
        /// }
        /// void Callee(int a, int b, params bool[] c)
        /// {
        ///     DoSomething(a, b, c);
        /// }
        /// After:
        /// void Caller(int i, int j, bool[] k)
        /// {
        ///     DoSomething(i, j, k);
        /// }
        /// void Callee(int a, int b, params bool[] c)
        /// {
        ///     DoSomething(a, b, c);
        /// }
        /// 4. A special case, the parameter is only read once in the callee method body
        /// Before:
        /// void Caller(bool x)
        /// {
        ///     Callee(Foo(), Bar())
        /// }
        /// void Callee(int a, int b)
        /// {
        ///     DoSomething(a, b);
        /// }
        /// After:
        /// void Caller(bool x)
        /// {
        ///     DoSomething(Foo(), Bar());
        /// }
        /// void Callee(int a, int b)
        /// {
        ///     DoSomething(a, b);
        /// }
        /// In this case, parameters 'a' and 'b' should just be replaced by the argument expression.
        /// Note: this might cause semantics changes. It is by design.
        /// </summary>
        public ImmutableDictionary<IParameterSymbol, TExpressionSyntax> ParametersToReplace { get; } = parametersToReplace;

        /// <summary>
        /// Indicate should inline expression and variable declaration be merged into one line.
        /// Example:
        /// Before:
        /// void Caller()
        /// {
        ///     Callee(out var x);
        /// }
        /// void Callee(out int i) => i = 100;
        /// After:
        /// (Correct version)
        /// void Caller()
        /// {
        ///     int x = 100;
        /// }
        /// void Callee(out int i) => i = 100;
        /// (Wrong version)
        /// void Caller()
        /// {
        ///     int x;
        ///     x = 100;
        /// }
        /// void Callee(out int i) => i = 100;
        /// </summary>
        public bool MergeInlineContentAndVariableDeclarationArgument { get; } = mergeInlineContentAndVariableDeclarationArgument;
    }

    private async Task<MethodParametersInfo> GetMethodParametersInfoAsync(
        Document document,
        TInvocationSyntax calleeInvocationNode,
        TMethodDeclarationSyntax calleeMethodNode,
        TStatementSyntax? statementContainingInvocation,
        TExpressionSyntax rawInlineExpression,
        IInvocationOperation invocationOperation,
        CancellationToken cancellationToken)
    {
        var callerSemanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var allArgumentOperations = invocationOperation.Arguments;
        var calleeDocument = document.Project.Solution.GetRequiredDocument(calleeMethodNode.SyntaxTree);
        var syntaxGenerator = SyntaxGenerator.GetGenerator(document);
        if (statementContainingInvocation != null)
        {
            // 1. Find all the parameter maps to an identifier from caller. After inlining, this identifier would be used to replace the parameter in callee body.
            // For params array, it should be included here if it is accept an array identifier as argument.
            // Note: this might change the order of evaluation if the identifiers are property, this is by design because strictly
            // follow the semantics will cause strange code and this is a refactoring.
            // Example:
            // Before:
            // void Caller(int i, int j, bool[] k)
            // {
            //     Callee(i, j, k);
            // }
            // void Callee(int a, int b, params bool[] c)
            // {
            //     DoSomething(a, b, c);
            // }
            // After:
            // void Caller(int i, int j, bool[] k)
            // {
            //     DoSomething(i, j, k);
            // }
            // void Callee(int a, int b, params bool[] c)
            // {
            //     DoSomething(a, b, c);
            // }
            var operationsWithIdentifierArgument = allArgumentOperations
                .WhereAsArray(argument =>
                    _syntaxFacts.IsIdentifierName(argument.Value.Syntax) && argument.ArgumentKind == ArgumentKind.Explicit);

            // 2. Find all the declaration arguments (e.g. out var declaration in C#).
            // After inlining, an declaration needs to be put before the invocation. And also use the declared identifier to replace the mapping parameter in callee.
            // Example:
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
            //     int x;
            //     x = 100;
            // }
            // void Callee(out int i) => i = 100;
            var operationsWithVariableDeclarationArgument = allArgumentOperations
                .WhereAsArray(argument =>
                    _syntaxFacts.IsDeclarationExpression(argument.Value.Syntax) && argument.ArgumentKind == ArgumentKind.Explicit);

            // 3. Find the literal arguments, and the mapping parameter will be replaced by that literal expression
            // Example:
            // Before:
            // void Caller(int k)
            // {
            //     Callee(1, k);
            // }
            // void Callee(int i, int j)
            // {
            //     DoSomething(i, k);
            // }
            // After:
            // void Caller(int k)
            // {
            //     DoSomething(1, k);
            // }
            // void Callee(int i, int j)
            // {
            //     DoSomething(i, j);
            // }
            var operationsWithLiteralArgument = allArgumentOperations
                .WhereAsArray(argument =>
                    _syntaxFacts.IsLiteralExpression(argument.Value.Syntax) && argument.ArgumentKind == ArgumentKind.Explicit);

            // 4. Find the default value parameters. Similarly to 3, they should be replaced by the default value.
            // Example:
            // Before:
            // void Caller(int k)
            // {
            //     Callee();
            // }
            // void Callee(int i = 1, int j = 2)
            // {
            //     DoSomething(i, k);
            // }
            // After:
            // void Caller(int k)
            // {
            //     DoSomething(1, 2);
            // }
            // void Callee(int i = 1, int j = 2)
            // {
            //     DoSomething(i, j);
            // }
            var operationsWithDefaultValue = allArgumentOperations
                .WhereAsArray(argument => argument.ArgumentKind == ArgumentKind.DefaultValue);

            // 5. All the remaining arguments, which might includes method call and a lot of other expressions.
            // Generate a declaration in the caller.
            // Example:
            // Before:
            // void Caller(bool x)
            // {
            //     Callee(Foo(), x ? Foo() : Bar())
            // }
            // void Callee(int a, int b)
            // {
            //     DoSomething(a, b);
            // }
            // After:
            // void Caller(bool x)
            // {
            //     int a = Foo();
            //     int b = x ? Foo() : Bar();
            //     DoSomething(a, b);
            // }
            // void Callee(int a, int b)
            // {
            //     DoSomething(a, b);
            // }
            var operationsToGenerateFreshVariablesFor = allArgumentOperations
                .RemoveRange(operationsWithIdentifierArgument)
                .RemoveRange(operationsWithVariableDeclarationArgument)
                .RemoveRange(operationsWithLiteralArgument)
                .RemoveRange(operationsWithDefaultValue)
                .WhereAsArray(argument => argument.Value.Syntax is TExpressionSyntax);

            // There is a special case that should be treated differently. If the parameter is only read once in the method body.
            // Then use the argument expression to directly replace it.
            // void Caller(bool x)
            // {
            //     Callee(Foo(), Bar())
            // }
            // void Callee(int a, int b)
            // {
            //     DoSomething(a, b);
            // }
            // After:
            // void Caller(bool x)
            // {
            //     DoSomething(Foo(), Bar());
            // }
            // void Callee(int a, int b)
            // {
            //     DoSomething(a, b);
            // }
            // Note: this change might change the order of evaluation. Strictly keep the semantics will make the
            // code becomes strange so it is by design.
            var operationsReadOnlyOnce =
                await GetArgumentsReadOnlyOnceAsync(
                    calleeDocument,
                    operationsToGenerateFreshVariablesFor,
                    calleeMethodNode,
                    cancellationToken).ConfigureAwait(false);
            operationsToGenerateFreshVariablesFor = operationsToGenerateFreshVariablesFor.RemoveRange(operationsReadOnlyOnce);
            var parametersToGenerateFreshVariablesFor = operationsToGenerateFreshVariablesFor
                // We excluded arglist callees, so Parameter will always be non null
                .SelectAsArray(argument => (argument.Parameter!, GenerateArgumentExpression(syntaxGenerator, argument)));

            var parameterToReplaceMap =
                operationsWithLiteralArgument
                .Concat(operationsWithIdentifierArgument)
                .Concat(operationsReadOnlyOnce)
                .Concat(operationsWithDefaultValue)
                .ToImmutableDictionary(
                    // We excluded arglist callees, so Parameter will always be non null
                    keySelector: argument => argument.Parameter!,
                    elementSelector: argument => GenerateArgumentExpression(syntaxGenerator, argument));

            // Use array instead of dictionary because using dictionary will make the parameter becomes unordered.
            // Example:
            // Before:
            // void Caller()
            // {
            //     Callee(out var x, out var y);
            // }
            // void Callee(out int i, out int j) => DoSomething(out i, out j);
            //
            // After:
            // void Caller()
            // {
            //     int y;
            //     int x;
            //     DoSomething(out x, out y);
            // }
            // void Callee(out int i, out int j) => DoSomething(out i, out j);
            // 'y' might becomes the first declaration if using dictionary instead of array.
            var parametersWithVariableDeclarationArgument = operationsWithVariableDeclarationArgument
                .Select(argument => (
                    argument.Parameter,
                    callerSemanticModel.GetSymbolInfo(argument.Value.Syntax, cancellationToken).GetAnySymbol()?.Name))
                .Where(parameterAndArgumentName => parameterAndArgumentName.Name != null)
                .ToImmutableArray();

            var mergeInlineContentAndVariableDeclarationArgument = await ShouldMergeInlineContentAndVariableDeclarationArgumentAsync(
                calleeDocument,
                calleeInvocationNode,
                parametersWithVariableDeclarationArgument!,
                rawInlineExpression,
                cancellationToken).ConfigureAwait(false);

            return new MethodParametersInfo(
                parametersWithVariableDeclarationArgument!,
                parametersToGenerateFreshVariablesFor,
                parameterToReplaceMap,
                mergeInlineContentAndVariableDeclarationArgument);
        }
        else
        {
            // If the caller is this is invoked in an arrow function, we can't generate declaration
            // because there is nowhere to insert that.
            // This such case, just use the argument expression to parameter.
            // Note: this might also cause semantics changes but is acceptable for a refactoring
            var parameterToReplaceMap = allArgumentOperations
                .Where(argument => argument.Value.Syntax is TExpressionSyntax
                   && !_syntaxFacts.IsDeclarationExpression(argument.Value.Syntax))
                .ToImmutableDictionary(
                    // We excluded arglist callees, so Parameter will always be non null
                    keySelector: argument => argument.Parameter!,
                    elementSelector: argument => GenerateArgumentExpression(syntaxGenerator, argument));
            return new MethodParametersInfo(
                [],
                [],
                parameterToReplaceMap,
                false);
        }
    }

    /// <summary>
    /// Check if the parameter is referenced only once, and it is referenced as 'read'.
    /// Determine a special case for a parameter that should be replaced by the argument instead of generating a declaration
    /// for it.
    /// Example:
    /// Before:
    /// void Caller(bool x)
    /// {
    ///     Callee(Foo(), Bar())
    /// }
    /// void Callee(int a, int b)
    /// {
    ///     DoSomething(a, b);
    /// }
    /// After:
    /// void Caller(bool x)
    /// {
    ///     DoSomething(Foo(), Bar());
    /// }
    /// void Callee(int a, int b)
    /// {
    ///     DoSomething(a, b);
    /// }
    /// Parameters 'a' and 'b' are used only once in the Callee, and their value are read not write.
    /// For this case just use the argument to replace the parameter.
    /// Note: This might cause a semantic change. In the previous example, if it is
    /// void Caller(bool x)
    /// {
    ///     Callee(Foo(), Bar())
    /// }
    /// void Callee(int a, int b)
    /// {
    ///     DoSomething(b, a);
    /// }
    /// Then this operation will change the order of evaluation but is acceptable for a refactoring
    /// </summary>
    private static async Task<ImmutableArray<IArgumentOperation>> GetArgumentsReadOnlyOnceAsync(
        Document document,
        ImmutableArray<IArgumentOperation> arguments,
        TMethodDeclarationSyntax calleeMethodNode,
        CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<IArgumentOperation>.GetInstance(out var builder);
        foreach (var argument in arguments)
        {
            var parameterSymbol = argument.Parameter;
            Contract.ThrowIfNull(parameterSymbol, "We filtered out varags methods earlier.");
            var allReferences = await SymbolFinder
                .FindReferencesAsync(parameterSymbol, document.Project.Solution, ImmutableHashSet<Document>.Empty.Add(document), cancellationToken).ConfigureAwait(false);
            // Need to check if the node is in CalleeMethodNode, because for this case
            // void Caller() { Callee(i: 10); }
            // void Callee(int i) { DoSomething(); }
            // the 'i' in the caller will be considered as the referenced location
            var allReferencedLocations = allReferences
                .SelectMany(@ref => @ref.Locations)
                .Where(location => !location.IsImplicit && calleeMethodNode.Contains(location.Location.FindNode(getInnermostNodeForTie: true, cancellationToken)))
                .ToImmutableArray();

            if (allReferencedLocations.Length == 1
                && allReferencedLocations[0].SymbolUsageInfo.IsReadFrom())
            {
                builder.Add(argument);
            }
        }

        return builder.ToImmutable();
    }

    /// <summary>
    /// Check if there is only one variable declaration argument and it is used for assignment
    /// in the method body. In this case, the method body and argument will be merged into one statement.
    /// For example:
    /// Before:
    /// void Caller()
    /// {
    ///     Callee(out var x);
    /// }
    /// void Callee(out int i) => i = 100;
    ///
    /// After:
    /// void Caller()
    /// {
    ///     int x = 100;
    /// }
    /// void Callee(out int i) => i = 100;
    /// </summary>
    private async Task<bool> ShouldMergeInlineContentAndVariableDeclarationArgumentAsync(
        Document calleeDocument,
        TInvocationSyntax calleInvocationNode,
        ImmutableArray<(IParameterSymbol parameterSymbol, string name)> parametersWithVariableDeclarationArgument,
        TExpressionSyntax inlineExpressionNode,
        CancellationToken cancellationToken)
    {
        var semanticModel = await calleeDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        return parametersWithVariableDeclarationArgument.Length == 1
           && _syntaxFacts.IsExpressionStatement(calleInvocationNode.Parent)
           && semanticModel.GetOperation(inlineExpressionNode, cancellationToken) is ISimpleAssignmentOperation simpleAssignmentOperation
           && simpleAssignmentOperation.Target is IParameterReferenceOperation parameterOperation
           && parameterOperation.Parameter.Equals(parametersWithVariableDeclarationArgument[0].parameterSymbol);
    }

    private TExpressionSyntax GenerateArgumentExpression(
        SyntaxGenerator syntaxGenerator,
        IArgumentOperation argumentOperation)
    {
        var parameterSymbol = argumentOperation.Parameter;
        Debug.Assert(parameterSymbol is not null);
        var argumentExpressionOperation = argumentOperation.Value;
        if (argumentOperation.ArgumentKind == ArgumentKind.ParamArray
            && parameterSymbol.Type is IArrayTypeSymbol paramArrayParameter
            && argumentExpressionOperation is IArrayCreationOperation { Initializer: { } initializer }
            && argumentOperation.IsImplicit)
        {
            // if this argument is a param array & the array creation operation is implicitly generated,
            // it means it is in this format:
            // void caller() { Callee(1, 2, 3); }
            // void Callee(params int[] x) { }
            // Collect each of these arguments and generate a new array for it.
            // Note: it could be empty.
            return (TExpressionSyntax)syntaxGenerator.AddParentheses(
                syntaxGenerator.ArrayCreationExpression(
                    GenerateTypeSyntax(paramArrayParameter.ElementType, allowVar: false),
                    initializer.ElementValues.SelectAsArray(op => op.Syntax)));
        }

        // In all the other cases, one parameter should only maps to one argument.
        if (argumentOperation.ArgumentKind == ArgumentKind.DefaultValue
            && parameterSymbol.HasExplicitDefaultValue)
        {
            return GenerateLiteralExpression(parameterSymbol.Type, parameterSymbol.ExplicitDefaultValue);
        }

        return (TExpressionSyntax)syntaxGenerator.AddParentheses(argumentExpressionOperation.Syntax);
    }
}
