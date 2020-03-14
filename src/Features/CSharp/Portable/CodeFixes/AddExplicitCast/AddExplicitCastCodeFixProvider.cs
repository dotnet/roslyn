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
using Microsoft.CodeAnalysis.CodeFixes.AddExplicitCast;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.AddExplicitCast
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddExplicitCast), Shared]
    internal sealed partial class AddExplicitCastCodeFixProvider
        : AbstractAddExplicitCastCodeFixProvider<
            ExpressionSyntax,
            ArgumentListSyntax,
            ArgumentSyntax>
    {
        /// <summary>
        /// CS0266: Cannot implicitly convert from type 'x' to 'y'. An explicit conversion exists (are you missing a cast?)
        /// </summary>
        private const string CS0266 = nameof(CS0266);

        /// <summary>
        /// CS1503: Argument 1: cannot convert from 'x' to 'y'
        /// </summary>
        private const string CS1503 = nameof(CS1503);

        /// <summary>
        /// Give a set of least specific types with a limit, and the part exceeding the limit doesn't show any code fix, but logs telemetry 
        /// </summary>
        private const int MaximumConversionOptions = 3;

        [ImportingConstructor]
        public AddExplicitCastCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CS0266, CS1503);

        internal override CodeFixCategory CodeFixCategory => CodeFixCategory.Compile;

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CS0266, CS1503);

        protected override string GetDescription(CodeFixContext context, SemanticModel semanticModel, ITypeSymbol? conversionType = null)
        {
            if (conversionType is object)
            {
                return string.Format(
                    CSharpFeaturesResources.Convert_type_to_0,
                    conversionType.ToMinimalDisplayString(semanticModel, context.Span.Start));
            }
            return CSharpFeaturesResources.Add_explicit_cast;
        }
        protected override SyntaxNode ApplyFix(SyntaxNode currentRoot, ExpressionSyntax targetNode, ITypeSymbol conversionType)
        {
            // TODO:
            // the Simplifier doesn't remove the redundant cast from the expression
            // Issue link: https://github.com/dotnet/roslyn/issues/41500
            var castExpression = targetNode.Cast(conversionType).WithAdditionalAnnotations(Simplifier.Annotation);
            var newRoot = currentRoot.ReplaceNode(targetNode, castExpression);
            return newRoot;
        }

        /// <summary>
        /// Output the current type information of the target node and the conversion type(s) that the target node is going to be cast by.
        /// Implicit downcast can appear on Variable Declaration, Return Statement, and Function Invocation
        /// <para/>
        /// For example:
        /// Base b; Derived d = [||]b;       
        /// "b" is the current node with type "Base", and the potential conversion types list which "b" can be cast by is {Derived}
        /// </summary>
        /// <param name="diagnosticId"> The ID of the diagnostic.</param>
        /// <param name="targetNode"> The node to be cast.</param>
        /// <param name="targetNodeType"> Output the type of "targetNode".</param>
        /// <param name="potentialConversionTypes"> Output the potential conversions types that "targetNode" can be cast to</param>
        /// <returns>
        /// True, if the target node has at least one potential conversion type, and they are assigned to "potentialConversionTypes"
        /// False, if the target node has no conversion type.
        /// </returns>
        protected override bool TryGetTargetTypeInfo(
            SemanticModel semanticModel, SyntaxNode root, string diagnosticId, ExpressionSyntax targetNode,
            CancellationToken cancellationToken, [NotNullWhen(true)] out ITypeSymbol? targetNodeType,
            out ImmutableArray<ITypeSymbol> potentialConversionTypes)
        {
            potentialConversionTypes = ImmutableArray<ITypeSymbol>.Empty;

            var targetNodeInfo = semanticModel.GetTypeInfo(targetNode, cancellationToken);
            targetNodeType = targetNodeInfo.Type;

            if (targetNodeType == null)
                return false;

            // The error happens either on an assignement operation or on an invocation expression.
            // If the error happens on assignment operation, "ConvertedType" is different from the current "Type"
            using var _ = ArrayBuilder<ITypeSymbol>.GetInstance(out var mutablePotentialConversionTypes);
            if (diagnosticId == CS0266
                && targetNodeInfo.ConvertedType != null
                && !targetNodeType.Equals(targetNodeInfo.ConvertedType))
            {
                mutablePotentialConversionTypes.Add(targetNodeInfo.ConvertedType);
            }
            else if (diagnosticId == CS1503
                && targetNode.GetAncestorsOrThis<ArgumentSyntax>().FirstOrDefault() is ArgumentSyntax targetArgument
                && targetArgument.Parent is ArgumentListSyntax argumentList
                && argumentList.Parent is SyntaxNode invocationNode) // invocation node could be Invocation Expression, Object Creation, Base Constructor...
            {
                mutablePotentialConversionTypes.AddRange(GetPotentialConversionTypes(semanticModel, root, targetNodeType,
                    targetArgument, argumentList, invocationNode, cancellationToken));
            }

            // clear up duplicate types
            potentialConversionTypes = FilterValidPotentialConversionTypes(semanticModel, targetNode, targetNodeType,
                mutablePotentialConversionTypes);
            return !potentialConversionTypes.IsEmpty;

            static ImmutableArray<ITypeSymbol> FilterValidPotentialConversionTypes(
                SemanticModel semanticModel, ExpressionSyntax targetNode, ITypeSymbol targetNodeType,
                ArrayBuilder<ITypeSymbol> mutablePotentialConversionTypes)
            {
                using var _ = ArrayBuilder<ITypeSymbol>.GetInstance(out var validPotentialConversionTypes);
                foreach (var targetNodeConversionType in mutablePotentialConversionTypes)
                {
                    var commonConversion = semanticModel.Compilation.ClassifyCommonConversion(
                        targetNodeType, targetNodeConversionType);

                    // For cases like object creation expression. for example:
                    // Derived d = [||]new Base();
                    // It is always invalid except the target node has explicit conversion operator or is numeric.
                    if (targetNode.IsKind(SyntaxKind.ObjectCreationExpression)
                        && !commonConversion.IsUserDefined)
                    {
                        continue;
                    }

                    if (commonConversion.Exists)
                    {
                        validPotentialConversionTypes.Add(targetNodeConversionType);
                    }
                }
                return validPotentialConversionTypes.Distinct().ToImmutableArray();
            }
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
        /// <param name="targetArgument"> The target argument that contains target node</param>
        /// <param name="targetArgumentConversionType"> Output the corresponding parameter type of
        /// the target arugment if function returns true</param>
        /// <returns>
        /// True, if arguments and parameters match perfectly. <paramref name="targetArgumentConversionType"/> Output the corresponding parameter type
        /// False, otherwise.
        /// </returns>
        protected override bool CanArgumentTypesBeConvertedToParameterTypes(
            SemanticModel semanticModel, SyntaxNode root, ArgumentListSyntax argumentList,
            ImmutableArray<IParameterSymbol> parameters, ArgumentSyntax targetArgument,
            CancellationToken cancellationToken, [NotNullWhen(true)] out ITypeSymbol? targetArgumentConversionType)
        {
            targetArgumentConversionType = null;

            // No conversion happens under this case
            if (parameters.Length == 0)
                return false;

            var arguments = argumentList.Arguments;
            var newArguments = new List<ArgumentSyntax>();

            for (var i = 0; i < arguments.Count; i++)
            {
                // Parameter index cannot out of its range, #arguments is larger than #parameter only if the last parameter with keyword params
                var parameterIndex = Math.Min(i, parameters.Length - 1);

                // If the argument has a name, get the corresponding parameter index
                var nameSyntax = arguments[i].NameColon?.Name;
                if (nameSyntax != null)
                {
                    var name = nameSyntax.Identifier.ValueText;
                    if (!FindCorrespondingParameterByName(name, parameters, ref parameterIndex))
                        return false;
                }

                // The argument is either in order with parameters, or have a matched name with parameters.
                var argumentExpression = arguments[i].Expression;
                var parameterType = parameters[parameterIndex].Type;

                if (parameters[parameterIndex].IsParams
                    && parameters.Last().Type is IArrayTypeSymbol paramsType
                    && (semanticModel.ClassifyConversion(argumentExpression, paramsType.ElementType).Exists))
                {
                    newArguments.Add(arguments[i].WithExpression(argumentExpression.Cast(paramsType.ElementType)));

                    if (arguments[i].Equals(targetArgument))
                        targetArgumentConversionType = paramsType.ElementType;
                }
                else if (semanticModel.ClassifyConversion(argumentExpression, parameterType).Exists)
                {
                    newArguments.Add(arguments[i].WithExpression(argumentExpression.Cast(parameterType)));

                    if (arguments[i].Equals(targetArgument))
                        targetArgumentConversionType = parameterType;
                }
                else if (argumentExpression.Kind() == SyntaxKind.DeclarationExpression
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

            return IsInvocationExpressionWithNewArgumentsApplicable(semanticModel, root, argumentList, newArguments, targetArgument);
        }

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
        protected override bool IsInvocationExpressionWithNewArgumentsApplicable(
            SemanticModel semanticModel, SyntaxNode root, ArgumentListSyntax oldArgumentList,
            List<ArgumentSyntax> newArguments, SyntaxNode targetNode)
        {
            var separatedSyntaxList = SyntaxFactory.SeparatedList(newArguments);
            var newRoot = root.ReplaceNode(oldArgumentList, oldArgumentList.WithArguments(separatedSyntaxList));

            var newArgumentListNode = newRoot.FindNode(targetNode.Span).GetAncestorsOrThis<ArgumentListSyntax>().FirstOrDefault();
            if (newArgumentListNode.Parent is SyntaxNode newExpression)
            {
                var symbolInfo = semanticModel.GetSpeculativeSymbolInfo(newExpression.SpanStart, newExpression,
                    SpeculativeBindingOption.BindAsExpression);
                return symbolInfo.Symbol != null;
            }
            return false;
        }
    }
}
