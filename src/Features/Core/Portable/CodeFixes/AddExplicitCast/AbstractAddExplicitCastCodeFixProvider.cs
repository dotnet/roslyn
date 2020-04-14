// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.AddExplicitCast
{
    internal abstract class AbstractAddExplicitCastCodeFixProvider<TExpressionSyntax> : SyntaxEditorBasedCodeFixProvider
        where TExpressionSyntax : SyntaxNode
    {
        /// <summary>
        /// Give a set of least specific types with a limit, and the part exceeding the limit doesn't show any code fix, but logs telemetry 
        /// </summary>
        private const int MaximumConversionOptions = 3;

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.Compile;

        // First title is for single option, second title is for multiple options
        protected abstract string GetDescription(CodeFixContext context, SemanticModel semanticModel,
            SyntaxNode? targetNode = null, ITypeSymbol? conversionType = null);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var diagnostic = context.Diagnostics.First();

            var root = await document.GetRequiredSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var spanNode = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true)
                .GetAncestorsOrThis<TExpressionSyntax>().FirstOrDefault();
            if (spanNode == null)
                return;

            var hasSolution = TryGetTargetTypeInfo(document,
                semanticModel, root, diagnostic.Id, spanNode, cancellationToken,
                out var potentialConversionTypes);
            if (!hasSolution)
            {
                return;
            }

            if (potentialConversionTypes.Length == 1)
            {
                context.RegisterCodeFix(new MyCodeAction(
                    GetDescription(context, semanticModel),
                    c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                    context.Diagnostics);
            }
            else
            {
                var actions = ArrayBuilder<CodeAction>.GetInstance();

                // MaximumConversionOptions: we show at most [MaximumConversionOptions] options for this code fixer
                for (var i = 0; i < Math.Min(MaximumConversionOptions, potentialConversionTypes.Length); i++)
                {
                    var targetNode = potentialConversionTypes[i].Item1;
                    var conversionType = potentialConversionTypes[i].Item2;
                    actions.Add(new MyCodeAction(
                        GetDescription(context, semanticModel, targetNode, conversionType),
                        _ => ApplySingleConversionToDocumentAsync(document, ApplyFix(root, targetNode, conversionType))));
                }

                if (potentialConversionTypes.Length > MaximumConversionOptions)
                {
                    // If the number of potential conversion types is larger than options we could show, report telemetry
                    Logger.Log(FunctionId.CodeFixes_AddExplicitCast,
                        KeyValueLogMessage.Create(m =>
                        {
                            m["NumberOfCandidates"] = potentialConversionTypes.Length;
                        }));
                }

                context.RegisterCodeFix(new CodeAction.CodeActionWithNestedActions(
                    GetDescription(context, semanticModel),
                    actions.ToImmutableAndFree(), isInlinable: false),
                    context.Diagnostics);
            }
        }

        protected abstract SyntaxNode ApplyFix(SyntaxNode currentRoot, TExpressionSyntax targetNode, ITypeSymbol conversionType);

        private static Task<Document> ApplySingleConversionToDocumentAsync(Document document, SyntaxNode currentRoot)
            => Task.FromResult(document.WithSyntaxRoot(currentRoot));

        /// <summary>
        /// Output the current type information of the target node and the conversion type(s) that the target node is going to be cast by.
        /// Implicit downcast can appear on Variable Declaration, Return Statement, and Function Invocation
        /// <para/>
        /// For example:
        /// Base b; Derived d = [||]b;       
        /// "b" is the current node with type "Base", and the potential conversion types list which "b" can be cast by is {Derived}
        /// </summary>
        /// <param name="potentialConversionTypes"> Output (target expression, potential conversion type) pairs</param>
        /// <returns>
        /// True, if there is at least one potential conversion pair, and they are assigned to "potentialConversionTypes"
        /// False, if there is no potential conversion pair.
        /// </returns>
        protected abstract bool TryGetTargetTypeInfo(Document document,
            SemanticModel semanticModel, SyntaxNode root, string diagnosticId, TExpressionSyntax spanNode,
            CancellationToken cancellationToken, out ImmutableArray<Tuple<TExpressionSyntax, ITypeSymbol>> potentialConversionTypes);

        protected abstract bool IsObjectCreationExpression(TExpressionSyntax targetNode);

        protected abstract bool IsConversionUserDefined(
            SemanticModel semanticModel, TExpressionSyntax expression, ITypeSymbol type);

        protected ImmutableArray<Tuple<TExpressionSyntax, ITypeSymbol>> FilterValidPotentialConversionTypes(
            SemanticModel semanticModel,
            ArrayBuilder<Tuple<TExpressionSyntax, ITypeSymbol>> mutablePotentialConversionTypes)
        {
            using var _ = ArrayBuilder<Tuple<TExpressionSyntax, ITypeSymbol>>.GetInstance(out var validPotentialConversionTypes);
            foreach (var conversionTuple in mutablePotentialConversionTypes)
            {
                var targetNode = conversionTuple.Item1;
                var targetNodeConversionType = conversionTuple.Item2;

                // For cases like object creation expression. for example:
                // Derived d = [||]new Base();
                // It is always invalid except the target node has explicit conversion operator or is numeric.
                if (IsObjectCreationExpression(targetNode)
                    && !IsConversionUserDefined(semanticModel, targetNode, targetNodeConversionType))
                {
                    continue;
                }

                validPotentialConversionTypes.Add(conversionTuple);
            }
            return validPotentialConversionTypes.Distinct().ToImmutableArray();
        }

        protected abstract bool ClassifyConversionExists(
            SemanticModel semanticModel, TExpressionSyntax expression, ITypeSymbol type);

        protected abstract bool IsConversionIdentity(
            SemanticModel semanticModel, TExpressionSyntax expression, ITypeSymbol type);

        protected abstract SeparatedSyntaxList<SyntaxNode> GetArguments(SyntaxNode argumentList);

        protected abstract SyntaxNode GenerateNewArgument(SyntaxNode oldArgument, ITypeSymbol conversionType);

        protected abstract TExpressionSyntax? GetArgumentExpression(SyntaxNode argument);
        protected abstract bool IsDeclarationExpression(TExpressionSyntax expression);

        protected abstract string? TryGetName(SyntaxNode argument);

        protected abstract void SortConversionTypes(SemanticModel semanticModel, ArrayBuilder<Tuple<TExpressionSyntax, ITypeSymbol>> conversionTypes, SyntaxNode argumentList);

        /// <summary>
        /// Test if all argument types can be converted to corresponding parameter types.
        /// </summary>
        /// For example:
        /// class Base { }
        /// class Derived1 : Base { }
        /// class Derived2 : Base { }
        /// class Derived3 : Base { }
        /// void DoSomething(int i, Derived1 d) { }
        /// void DoSomething(string s, Derived2 d) { }
        /// void DoSomething(int i, Derived3 d) { }
        /// 
        /// Base b;
        /// DoSomething(1, [||]b);
        ///
        /// *void DoSomething(string s, Derived2 d) { }* is not the perfect match candidate function for
        /// *DoSomething(1, [||]b)* because int and string are not ancestor-descendant relationship. Thus,
        /// Derived2 is not a potential conversion type.
        /// 
        /// <param name="argumentList"> The argument list of invocation expression</param>
        /// <param name="parameters"> The parameters of function</param>
        /// <param name="spanArgument"> The argument that contains the span, could be null when the span is 
        /// on invocation identifier name, i.e. BC30518, BC30519 in VB .NET</param>
        /// <param name="targetArgument">The argument need to be cast. The value is the same as "spanArgument"
        /// if "spanArgument" is not null. Otherwise the value is *the first argument* that need to be cast in the 
        /// invocation expression</param>
        /// <param name="targetArgumentConversionType"> Output the corresponding parameter type of
        /// the target arugment if function returns true</param>
        /// <returns>
        /// True, if arguments and parameters match perfectly. <paramref name="targetArgumentConversionType"/> Output the corresponding parameter type
        /// False, otherwise.
        /// </returns>
        private bool CanArgumentTypesBeConvertedToParameterTypes(
            SemanticModel semanticModel, SyntaxNode root, SyntaxNode argumentList,
            ImmutableArray<IParameterSymbol> parameters, SyntaxNode? spanArgument,
            CancellationToken cancellationToken, [NotNullWhen(true)] out SyntaxNode? targetArgument,
            [NotNullWhen(true)] out ITypeSymbol? targetArgumentConversionType)
        {
            targetArgumentConversionType = null;
            targetArgument = spanArgument;

            // No conversion happens under this case
            if (parameters.Length == 0)
                return false;

            var arguments = GetArguments(argumentList);
            var newArguments = new List<SyntaxNode>();

            for (var i = 0; i < arguments.Count; i++)
            {
                // Parameter index cannot out of its range, #arguments is larger than #parameter only if the last parameter with keyword params
                var parameterIndex = Math.Min(i, parameters.Length - 1);

                // If the argument has a name, get the corresponding parameter index
                if (TryGetName(arguments[i]) is string name
                    && !FindCorrespondingParameterByName(name, parameters, ref parameterIndex))
                {
                    return false;
                }

                // The argument is either in order with parameters, or have a matched name with parameters.
                var argumentExpression = GetArgumentExpression(arguments[i]);
                if (argumentExpression == null)
                {
                    // argumentExpression is null when it is an omitted argument in VB .NET
                    newArguments.Add(arguments[i]);
                    continue;
                }
                var parameterType = parameters[parameterIndex].Type;

                if (parameters[parameterIndex].IsParams
                    && parameterType is IArrayTypeSymbol paramsType
                    && ClassifyConversionExists(semanticModel, argumentExpression, paramsType.ElementType))
                {
                    newArguments.Add(GenerateNewArgument(arguments[i], paramsType.ElementType));

                    if (targetArgument is null && !IsConversionIdentity(semanticModel, argumentExpression, paramsType.ElementType))
                        targetArgument = arguments[i];
                    if (arguments[i].Equals(targetArgument))
                        targetArgumentConversionType = paramsType.ElementType;
                }
                else if (ClassifyConversionExists(semanticModel, argumentExpression, parameterType))
                {
                    newArguments.Add(GenerateNewArgument(arguments[i], parameterType));

                    if (targetArgument is null && !IsConversionIdentity(semanticModel, argumentExpression, parameterType))
                        targetArgument = arguments[i];
                    if (arguments[i].Equals(targetArgument))
                        targetArgumentConversionType = parameterType;
                }
                else if (IsDeclarationExpression(argumentExpression)
                    && semanticModel.GetTypeInfo(argumentExpression, cancellationToken).Type is ITypeSymbol argumentType
                    && semanticModel.Compilation.ClassifyCommonConversion(argumentType, parameterType).IsIdentity)
                {
                    // Direct conversion from a declaration expression to a type is unspecified, thus we classify the
                    // conversion from the type of declaration expression to the parameter type
                    // An example for this case:
                    // void Foo(out int i) { i = 1; }
                    // Foo([|out var i|]);
                    // "var i" is a declaration expression
                    // 
                    // In addition, since this case is with keyword "out", the type of declaration expression and the
                    // parameter type must be identical in order to match.
                    newArguments.Add(arguments[i]);
                }
                else
                {
                    return false;
                }
            }

            return targetArgumentConversionType != null && targetArgument != null
                && IsInvocationExpressionWithNewArgumentsApplicable(semanticModel, root, argumentList, newArguments, targetArgument);
        }

        protected abstract SyntaxNode GenerateNewArgumentList(
            SyntaxNode oldArgumentList, List<SyntaxNode> newArguments);

        /// <summary>
        /// Check whether the invocation expression with new arguments is applicatble.
        /// </summary>
        /// <param name="oldArgumentList" >old argumentList node</param>
        /// <param name="newArguments"> new arguments that are cast by corresponding parameter types</param>
        /// <param name="targetNode"> The node needs to be cast.</param>
        /// <returns>
        /// Return true if the invocation expression with new arguments is applicatble.
        /// Otherwise, return false
        /// </returns>
        protected abstract bool IsInvocationExpressionWithNewArgumentsApplicable(
            SemanticModel semanticModel, SyntaxNode root, SyntaxNode oldArgumentList,
            List<SyntaxNode> newArguments, SyntaxNode targetNode);

        protected ImmutableArray<Tuple<TExpressionSyntax, ITypeSymbol>> GetPotentialConversionTypes(
                SemanticModel semanticModel, SyntaxNode root, SyntaxNode? spanNode,
                SyntaxNode argumentList, SyntaxNode invocationNode, CancellationToken cancellationToken)
        {
            // Implicit downcast appears on the argument of invocation node,
            // get all candidate functions and extract potential conversion types 
            var symbolInfo = semanticModel.GetSymbolInfo(invocationNode, cancellationToken);
            using var _ = ArrayBuilder<ISymbol>.GetInstance(out var candidateSymbols);
            candidateSymbols.AddRange(symbolInfo.CandidateSymbols);
            if (symbolInfo.Symbol != null) // BC42016: the only candidate symbol is symbolInfo.Symbol
                candidateSymbols.Add(symbolInfo.Symbol);

            using var __ = ArrayBuilder<Tuple<TExpressionSyntax, ITypeSymbol>>.GetInstance(out var mutablePotentialConversionTypes);
            foreach (var candidateSymbol in candidateSymbols.OfType<IMethodSymbol>())
            {
                if (CanArgumentTypesBeConvertedToParameterTypes(
                        semanticModel, root, argumentList, candidateSymbol.Parameters, spanNode,
                        cancellationToken, out var targetArgument, out var targetArgumentConversionType)
                    && GetArgumentExpression(targetArgument) is TExpressionSyntax argumentExpression)
                {
                    mutablePotentialConversionTypes.Add(Tuple.Create(argumentExpression, targetArgumentConversionType));
                }
            }

            // Sort the potential conversion types by inheritance distance, so that
            // operations are in order and user can choose least specific types(more accurate)
            SortConversionTypes(semanticModel, mutablePotentialConversionTypes, argumentList);

            return mutablePotentialConversionTypes.ToImmutable();
        }

        protected bool FindCorrespondingParameterByName(
            string argumentName, ImmutableArray<IParameterSymbol> parameters, ref int parameterIndex)
        {
            for (var j = 0; j < parameters.Length; j++)
            {
                if (argumentName.Equals(parameters[j].Name))
                {
                    parameterIndex = j;
                    return true;
                }
            }

            return false;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var targetNodes = diagnostics.SelectAsArray(
                d => root.FindNode(d.Location.SourceSpan, getInnermostNodeForTie: true)
                         .GetAncestorsOrThis<TExpressionSyntax>().FirstOrDefault());

            await editor.ApplyExpressionLevelSemanticEditsAsync(
                document, targetNodes,
                (semanticModel, targetNode) => true,
                (semanticModel, currentRoot, targetNode) =>
                {
                    // All diagnostics have the same error code
                    if (TryGetTargetTypeInfo(document, semanticModel, currentRoot, diagnostics[0].Id, targetNode,
                        cancellationToken, out var potentialConversionTypes)
                        && potentialConversionTypes.Length == 1)
                    {
                        return ApplyFix(currentRoot, potentialConversionTypes[0].Item1, potentialConversionTypes[0].Item2);
                    }

                    return currentRoot;
                },
                cancellationToken).ConfigureAwait(false);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument, equivalenceKey: title)
            {
            }
        }
    }
}
