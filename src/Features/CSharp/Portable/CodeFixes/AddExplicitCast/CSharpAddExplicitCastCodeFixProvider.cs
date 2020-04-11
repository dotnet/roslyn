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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.AddExplicitCast;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.AddExplicitCast
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddExplicitCast), Shared]
    internal sealed partial class CSharpAddExplicitCastCodeFixProvider
        : AbstractAddExplicitCastCodeFixProvider<
            ExpressionSyntax,
            ArgumentListSyntax,
            ArgumentSyntax,
            AttributeSyntax>
    {
        /// <summary>
        /// CS0266: Cannot implicitly convert from type 'x' to 'y'. An explicit conversion exists (are you missing a cast?)
        /// </summary>
        private const string CS0266 = nameof(CS0266);

        /// <summary>
        /// CS1503: Argument 1: cannot convert from 'x' to 'y'
        /// </summary>
        private const string CS1503 = nameof(CS1503);

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpAddExplicitCastCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CS0266, CS1503);

        protected override string GetDescription(CodeFixContext context, SemanticModel semanticModel, SyntaxNode? targetNode = null, ITypeSymbol? conversionType = null)
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

        protected override bool IsObjectCreationExpression(ExpressionSyntax targetNode)
            => targetNode.IsKind(SyntaxKind.ObjectCreationExpression);

        /// <summary>
        /// Output the current type information of the target node and the conversion type(s) that the target node is going to be cast by.
        /// Implicit downcast can appear on Variable Declaration, Return Statement, and Function Invocation
        /// <para/>
        /// For example:
        /// Base b; Derived d = [||]b;       
        /// "b" is the current node with type "Base", and the potential conversion types list which "b" can be cast by is {Derived}
        /// </summary>
        /// <param name="potentialConversionTypes"> Output the potential conversions types that "targetNode" can be cast to</param>
        /// <returns>
        /// True, if the target node has at least one potential conversion type, and they are assigned to "potentialConversionTypes"
        /// False, if the target node has no conversion type.
        /// </returns>
        protected override bool TryGetTargetTypeInfo(Document document,
            SemanticModel semanticModel, SyntaxNode root, string diagnosticId, ExpressionSyntax spanNode,
            CancellationToken cancellationToken, out ImmutableArray<Tuple<ExpressionSyntax, ITypeSymbol>> potentialConversionTypes)
        {
            potentialConversionTypes = ImmutableArray<Tuple<ExpressionSyntax, ITypeSymbol>>.Empty;

            var targetNodeInfo = semanticModel.GetTypeInfo(spanNode, cancellationToken);
            var targetNodeType = targetNodeInfo.Type;

            if (targetNodeType == null)
                return false;

            // The error happens either on an assignement operation or on an invocation expression.
            // If the error happens on assignment operation, "ConvertedType" is different from the current "Type"
            using var _ = ArrayBuilder<Tuple<ExpressionSyntax, ITypeSymbol>>.GetInstance(out var mutablePotentialConversionTypes);
            if (diagnosticId == CS0266
                && targetNodeInfo.ConvertedType != null
                && !targetNodeType.Equals(targetNodeInfo.ConvertedType))
            {
                mutablePotentialConversionTypes.Add(Tuple.Create(spanNode, targetNodeInfo.ConvertedType));
            }
            else if (diagnosticId == CS1503
                && spanNode.GetAncestorsOrThis<ArgumentSyntax>().FirstOrDefault() is ArgumentSyntax targetArgument
                && targetArgument.Parent is ArgumentListSyntax argumentList
                && argumentList.Parent is SyntaxNode invocationNode) // invocation node could be Invocation Expression, Object Creation, Base Constructor...
            {
                mutablePotentialConversionTypes.AddRange(GetPotentialConversionTypes(semanticModel, root,
                    targetArgument, argumentList, invocationNode, cancellationToken));
            }

            // clear up duplicate types
            potentialConversionTypes = FilterValidPotentialConversionTypes(semanticModel, mutablePotentialConversionTypes);
            return !potentialConversionTypes.IsEmpty;
        }

        protected override bool ClassifyConversionExists(SemanticModel semanticModel, ExpressionSyntax expression, ITypeSymbol type)
            => semanticModel.ClassifyConversion(expression, type).Exists;

        protected override bool IsConversionIdentity(SemanticModel semanticModel, ExpressionSyntax expression, ITypeSymbol type)
            => semanticModel.ClassifyConversion(expression, type).IsIdentity;

        protected override SeparatedSyntaxList<ArgumentSyntax> GetArguments(ArgumentListSyntax argumentList)
            => argumentList.Arguments;

        protected override ArgumentSyntax GenerateNewArgument(ArgumentSyntax oldArgument, ITypeSymbol conversionType)
            => oldArgument.WithExpression(oldArgument.Expression.Cast(conversionType));

        protected override ExpressionSyntax GetArgumentExpression(ArgumentSyntax argument)
            => argument.Expression;

        protected override bool IsDeclarationExpression(ExpressionSyntax expression)
            => expression.Kind() == SyntaxKind.DeclarationExpression;

        protected override string? TryGetName(ArgumentSyntax argument)
            => argument.NameColon?.Name.Identifier.ValueText;

        protected override void SortConversionTypes(SemanticModel semanticModel, ArrayBuilder<Tuple<ExpressionSyntax, ITypeSymbol>> conversionTypes, ArgumentListSyntax argumentList)
            => conversionTypes.Sort(new InheritanceDistanceComparer<ExpressionSyntax, ArgumentSyntax>(semanticModel, argumentList.Arguments));

        protected override ArgumentListSyntax GenerateNewArgumentList(
            ArgumentListSyntax oldArgumentList, List<ArgumentSyntax> newArguments)
            => oldArgumentList.WithArguments(SyntaxFactory.SeparatedList(newArguments));

        protected override bool IsConversionUserDefined(SemanticModel semanticModel, ExpressionSyntax expression, ITypeSymbol type)
            => semanticModel.ClassifyConversion(expression, type).IsUserDefined;

        protected override SymbolInfo GetSpeculativeAttributeSymbolInfo(SemanticModel semanticModel, int position, AttributeSyntax attribute)
            => semanticModel.GetSpeculativeSymbolInfo(position, attribute);
    }
}
