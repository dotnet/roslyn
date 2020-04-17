// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.AddExplicitCast
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddExplicitCast), Shared]
    internal sealed partial class AddExplicitCastCodeFixProvider
        : AbstractAddExplicitCastCodeFixProvider<ExpressionSyntax>
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
        public AddExplicitCastCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CS0266, CS1503);

        protected override string GetDescription(CodeFixContext context, SemanticModel semanticModel,
            SyntaxNode? targetNode = null, ITypeSymbol? conversionType = null)
        {
            if (conversionType is object)
            {
                return string.Format(
                    CSharpFeaturesResources.Convert_type_to_0,
                    conversionType.ToMinimalDisplayString(semanticModel, context.Span.Start));
            }
            return FeaturesResources.Add_explicit_cast;
        }
        protected override SyntaxNode ApplyFix(SyntaxNode currentRoot, ExpressionSyntax targetNode,
            ITypeSymbol conversionType)
        {
            // TODO:
            // the Simplifier doesn't remove the redundant cast from the expression
            // Issue link: https://github.com/dotnet/roslyn/issues/41500
            var castExpression = targetNode.Cast(conversionType).WithAdditionalAnnotations(Simplifier.Annotation);
            var newRoot = currentRoot.ReplaceNode(targetNode, castExpression);
            return newRoot;
        }

        protected override bool TryGetTargetTypeInfo(Document document, SemanticModel semanticModel, SyntaxNode root,
            string diagnosticId, ExpressionSyntax spanNode, CancellationToken cancellationToken,
            out ImmutableArray<(ExpressionSyntax, ITypeSymbol)> potentialConversionTypes)
        {
            potentialConversionTypes = ImmutableArray<(ExpressionSyntax, ITypeSymbol)>.Empty;

            using var _ = ArrayBuilder<(ExpressionSyntax, ITypeSymbol)>.GetInstance(out var mutablePotentialConversionTypes);
            if (diagnosticId == CS0266)
            {
                var inferenceService = document.GetRequiredLanguageService<ITypeInferenceService>();
                var conversionType = inferenceService.InferType(semanticModel, spanNode, objectAsDefault: false, cancellationToken);
                if (conversionType is null)
                    return false;
                mutablePotentialConversionTypes.Add((spanNode, conversionType));
            }
            else if (diagnosticId == CS1503)
            {
                if (spanNode.GetAncestorOrThis<ArgumentSyntax>() is ArgumentSyntax targetArgument
                    && targetArgument.Parent is ArgumentListSyntax argumentList
                    && argumentList.Parent is SyntaxNode invocationNode)
                {
                    // invocationNode could be Invocation Expression, Object Creation, Base Constructor...)
                    mutablePotentialConversionTypes.AddRange(GetPotentialConversionTypes(semanticModel, root,
                    targetArgument, argumentList, invocationNode, cancellationToken));
                }
                else if (spanNode.GetAncestorOrThis<AttributeArgumentSyntax>() is AttributeArgumentSyntax targetAttributeArgument
                    && targetAttributeArgument.Parent is AttributeArgumentListSyntax attributeArgumentList
                    && attributeArgumentList.Parent is SyntaxNode attributeNode)
                {
                    // attribute node
                    mutablePotentialConversionTypes.AddRange(GetPotentialConversionTypes(semanticModel, root,
                    targetAttributeArgument, attributeArgumentList, attributeNode, cancellationToken));
                }
            }

            // clear up duplicate types
            potentialConversionTypes = FilterValidPotentialConversionTypes(semanticModel,
                document.GetRequiredLanguageService<ISyntaxFactsService>(),
                mutablePotentialConversionTypes);
            return !potentialConversionTypes.IsEmpty;
        }

        protected override SeparatedSyntaxList<SyntaxNode> GetArguments(SyntaxNode argumentList)
        {
            if (argumentList is ArgumentListSyntax normalArgumentList)
            {
                return normalArgumentList.Arguments;
            }
            else if (argumentList is AttributeArgumentListSyntax attributeArgumentList)
            {
                return attributeArgumentList.Arguments;
            }
            return SyntaxFactory.SeparatedList<SyntaxNode>();
        }

        protected override SyntaxNode GenerateNewArgument(SyntaxNode oldArgument, ITypeSymbol conversionType)
        {
            if (oldArgument is ArgumentSyntax oldNormalArgument)
            {
                return oldNormalArgument.WithExpression(oldNormalArgument.Expression.Cast(conversionType));
            }
            else if (oldArgument is AttributeArgumentSyntax oldAttributeArgument)
            {
                return oldAttributeArgument.WithExpression(oldAttributeArgument.Expression.Cast(conversionType));
            }
            return oldArgument;
        }

        protected override ExpressionSyntax? GetArgumentExpression(SyntaxNode argument)
        {
            if (argument is ArgumentSyntax normalArgument)
            {
                return normalArgument.Expression;
            }
            else if (argument is AttributeArgumentSyntax attributeArgument)
            {
                return attributeArgument.Expression;
            }
            return null;
        }

        protected override bool IsDeclarationExpression(ExpressionSyntax expression)
            => expression.Kind() == SyntaxKind.DeclarationExpression;

        protected override string? TryGetName(SyntaxNode argument)
        {
            if (argument is ArgumentSyntax normalArgument)
            {
                return normalArgument.NameColon?.Name.Identifier.ValueText;
            }
            else if (argument is AttributeArgumentSyntax attributeArgument)
            {
                return attributeArgument.NameColon?.Name.Identifier.ValueText;
            }
            return null;
        }

        protected override SyntaxNode GenerateNewArgumentList(
            SyntaxNode oldArgumentList, List<SyntaxNode> newArguments)
        {
            if (oldArgumentList is ArgumentListSyntax oldNormalArgumentList)
            {
                return oldNormalArgumentList.WithArguments(SyntaxFactory.SeparatedList(newArguments));
            }
            else if (oldArgumentList is AttributeArgumentListSyntax oldAttributeArgumentList)
            {
                return oldAttributeArgumentList.WithArguments(SyntaxFactory.SeparatedList(newArguments));
            }
            return oldArgumentList;
        }

        protected override CommonConversion ClassifyConversion(SemanticModel semanticModel, ExpressionSyntax expression,
            ITypeSymbol type)
            => semanticModel.ClassifyConversion(expression, type).ToCommonConversion();

        protected override bool IsInvocationExpressionWithNewArgumentsApplicable(
            SemanticModel semanticModel, SyntaxNode root, SyntaxNode oldArgumentList,
            List<SyntaxNode> newArguments, SyntaxNode targetNode)
        {
            var newRoot = root.ReplaceNode(oldArgumentList, GenerateNewArgumentList(oldArgumentList, newArguments));
            if (targetNode is AttributeArgumentSyntax attributeArugment)
            {
                var attributeNode = newRoot.FindNode(attributeArugment.Span).GetAncestorsOrThis<AttributeSyntax>().FirstOrDefault();
                var symbolInfo = semanticModel.GetSpeculativeSymbolInfo(attributeNode.SpanStart, attributeNode);
                return symbolInfo.Symbol != null;
            }
            else if (targetNode is ArgumentSyntax arugment)
            {
                var newArgumentListNode = newRoot.FindNode(arugment.Span).GetAncestorsOrThis<ArgumentListSyntax>().FirstOrDefault();
                if (newArgumentListNode?.Parent is SyntaxNode newNode)
                {
                    var symbolInfo = semanticModel.GetSpeculativeSymbolInfo(
                        newNode.SpanStart, newNode, SpeculativeBindingOption.BindAsExpression);
                    return symbolInfo.Symbol != null;
                }
            }

            return false;
        }
    }
}
