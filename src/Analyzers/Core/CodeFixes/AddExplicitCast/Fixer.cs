// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeFixes.AddExplicitCast;

internal abstract partial class AbstractAddExplicitCastCodeFixProvider<TExpressionSyntax>
{
    protected abstract class Fixer<TArgumentSyntax, TArgumentListSyntax, TInvocationSyntax>
        where TArgumentSyntax : SyntaxNode
        where TArgumentListSyntax : SyntaxNode
        where TInvocationSyntax : SyntaxNode
    {
        protected abstract TExpressionSyntax GetExpressionOfArgument(TArgumentSyntax argument);
        protected abstract TArgumentSyntax GenerateNewArgument(TArgumentSyntax oldArgument, ITypeSymbol conversionType);
        protected abstract TArgumentListSyntax GenerateNewArgumentList(TArgumentListSyntax oldArgumentList, ArrayBuilder<TArgumentSyntax> newArguments);
        protected abstract SeparatedSyntaxList<TArgumentSyntax> GetArgumentsOfArgumentList(TArgumentListSyntax argumentList);
        protected abstract SymbolInfo GetSpeculativeSymbolInfo(SemanticModel semanticModel, TArgumentListSyntax newArgumentList);

        /// <summary>
        /// Collect all the available cast pairs, format is (target argument expression, potential conversion type)
        /// </summary>
        /// <param name="targetArgument"> The argument that need to be cast</param>
        /// <param name="argumentList"> The argument list that contains the target argument to be cast </param>
        /// <param name="invocationNode"> The invocation node that is the parent of "argumentList"</param>
        /// <returns>
        /// Return all the available cast pairs, format is (target argument expression, potential conversion type)
        /// </returns>
        public ImmutableArray<(TExpressionSyntax, ITypeSymbol)> GetPotentialConversionTypes(
            Document document,
            SemanticModel semanticModel,
            SyntaxNode root,
            TArgumentSyntax targetArgument,
            TArgumentListSyntax argumentList,
            TInvocationSyntax invocationNode,
            CancellationToken cancellationToken)
        {
            // Implicit downcast appears on the argument of invocation node,
            // get all candidate functions and extract potential conversion types 
            var symbolInfo = semanticModel.GetSymbolInfo(invocationNode, cancellationToken);
            using var _ = ArrayBuilder<ISymbol>.GetInstance(out var candidateSymbols);
            if (symbolInfo.Symbol != null) // BC42016: the only candidate symbol is symbolInfo.Symbol
            {
                candidateSymbols.Add(symbolInfo.Symbol);
            }
            else
            {
                candidateSymbols.AddRange(symbolInfo.CandidateSymbols);
            }

            using var __ = ArrayBuilder<(TExpressionSyntax, ITypeSymbol)>.GetInstance(out var mutablePotentialConversionTypes);
            foreach (var candidateSymbol in candidateSymbols.OfType<IMethodSymbol>())
            {
                if (CanArgumentTypesBeConvertedToParameterTypes(
                        document, semanticModel, root, argumentList, candidateSymbol.Parameters,
                        targetArgument, cancellationToken, out var targetArgumentConversionType)
                    && GetExpressionOfArgument(targetArgument) is TExpressionSyntax argumentExpression)
                {
                    mutablePotentialConversionTypes.Add((argumentExpression, targetArgumentConversionType));
                }
            }

            // Sort the potential conversion types by inheritance distance, so that
            // operations are in order and user can choose least specific types(more accurate)
            mutablePotentialConversionTypes.Sort(new InheritanceDistanceComparer<TExpressionSyntax>(semanticModel));

            return mutablePotentialConversionTypes.ToImmutableAndClear();
        }

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
        /// <param name="targetArgument">The argument need to be cast.</param>
        /// <param name="targetArgumentConversionType"> Output the corresponding parameter type of
        /// "targetArgument" if function returns true</param>
        /// <returns>
        /// True, if arguments and parameters match perfectly.
        /// "targetArgumentConversionType" outputs the corresponding parameter type of "targetArgument"
        /// False, otherwise.
        /// </returns>
        public bool CanArgumentTypesBeConvertedToParameterTypes(
            Document document,
            SemanticModel semanticModel,
            SyntaxNode root,
            TArgumentListSyntax argumentList,
            ImmutableArray<IParameterSymbol> parameters,
            TArgumentSyntax targetArgument,
            CancellationToken cancellationToken,
            [NotNullWhen(true)] out ITypeSymbol? targetArgumentConversionType)
        {
            targetArgumentConversionType = null;

            // No conversion happens under this case
            if (parameters.Length == 0)
                return false;

            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var semanticFacts = document.GetRequiredLanguageService<ISemanticFactsService>();

            var arguments = GetArgumentsOfArgumentList(argumentList);
            using var _ = ArrayBuilder<TArgumentSyntax>.GetInstance(out var newArguments);

            for (var i = 0; i < arguments.Count; i++)
            {
                // Parameter index cannot out of its range, #arguments is larger than #parameter only if 
                // the last parameter with keyword params
                var parameterIndex = Math.Min(i, parameters.Length - 1);

                // If the argument has a name, get the corresponding parameter index
                if (syntaxFacts.GetNameForArgument(arguments[i]) is string name
                    && name != string.Empty
                    && !FindCorrespondingParameterByName(name, parameters, ref parameterIndex))
                {
                    return false;
                }

                // The argument is either in order with parameters, or have a matched name with parameters.
                var argumentExpression = GetExpressionOfArgument(arguments[i]);
                if (argumentExpression == null)
                {
                    // argumentExpression is null when it is an omitted argument in VB .NET
                    newArguments.Add(arguments[i]);
                    continue;
                }

                var parameterType = parameters[parameterIndex].Type;
                if (parameters[parameterIndex].IsParams
                    && parameterType is IArrayTypeSymbol paramsType
                    && semanticFacts.ClassifyConversion(semanticModel, argumentExpression, paramsType.ElementType).Exists)
                {
                    newArguments.Add(GenerateNewArgument(arguments[i], paramsType.ElementType));
                    if (arguments[i].Equals(targetArgument))
                        targetArgumentConversionType = paramsType.ElementType;
                }
                else if (semanticFacts.ClassifyConversion(semanticModel, argumentExpression, parameterType).Exists)
                {
                    newArguments.Add(GenerateNewArgument(arguments[i], parameterType));
                    if (arguments[i].Equals(targetArgument))
                        targetArgumentConversionType = parameterType;
                }
                else if (syntaxFacts.IsDeclarationExpression(argumentExpression)
                    && semanticModel.GetTypeInfo(argumentExpression, cancellationToken).Type is ITypeSymbol argumentType
                    && semanticModel.Compilation.ClassifyCommonConversion(argumentType, parameterType).IsIdentity)
                {
                    // Direct conversion from a declaration expression to a type is unspecified, thus we classify the
                    // conversion from the type of declaration expression to the parameter type
                    // An example for this case:
                    // void Goo(out int i) { i = 1; }
                    // Goo([|out var i|]);
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

            return targetArgumentConversionType != null
                && IsInvocationExpressionWithNewArgumentsApplicable(
                    semanticModel, root, argumentList, newArguments, targetArgument);
        }

        /// <summary>
        /// Check whether the invocation expression with new arguments is applicable.
        /// </summary>
        /// <param name="oldArgumentList" >old argumentList node</param>
        /// <param name="newArguments"> new arguments that are cast by corresponding parameter types</param>
        /// <param name="targetNode"> The node needs to be cast.</param>
        /// <returns>
        /// Return true if the invocation expression with new arguments is applicable.
        /// Otherwise, return false
        /// </returns>
        private bool IsInvocationExpressionWithNewArgumentsApplicable(SemanticModel semanticModel,
            SyntaxNode root,
            TArgumentListSyntax oldArgumentList,
            ArrayBuilder<TArgumentSyntax> newArguments,
            SyntaxNode targetNode)
        {
            var newRoot = root.ReplaceNode(oldArgumentList, GenerateNewArgumentList(oldArgumentList, newArguments));
            if (newRoot.FindNode(targetNode.Span).GetAncestorOrThis<TArgumentListSyntax>() is TArgumentListSyntax newArgumentList)
            {
                var symbolInfo = GetSpeculativeSymbolInfo(semanticModel, newArgumentList);
                return symbolInfo.Symbol != null;
            }

            return false;
        }
    }
}
