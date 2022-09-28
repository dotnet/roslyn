// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.AddExplicitCast;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.AddExplicitCast
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddExplicitCast), Shared]
    internal sealed partial class CSharpAddExplicitCastCodeFixProvider
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

        private readonly ArgumentFixer _argumentFixer;
        private readonly AttributeArgumentFixer _attributeArgumentFixer;

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpAddExplicitCastCodeFixProvider() : base(CSharpSyntaxFacts.Instance)
        {
            _argumentFixer = new ArgumentFixer(this);
            _attributeArgumentFixer = new AttributeArgumentFixer(this);
        }

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CS0266, CS1503);

        protected override SyntaxNode ApplyFix(SyntaxNode currentRoot, ExpressionSyntax targetNode, ITypeSymbol conversionType)
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
                    mutablePotentialConversionTypes.AddRange(_argumentFixer.GetPotentialConversionTypes(
                        semanticModel, root, targetArgument, argumentList, invocationNode, cancellationToken));
                }
                else if (spanNode.GetAncestorOrThis<AttributeArgumentSyntax>() is AttributeArgumentSyntax targetAttributeArgument
                    && targetAttributeArgument.Parent is AttributeArgumentListSyntax attributeArgumentList
                    && attributeArgumentList.Parent is AttributeSyntax attributeNode)
                {
                    // attribute node
                    mutablePotentialConversionTypes.AddRange(_attributeArgumentFixer.GetPotentialConversionTypes(
                        semanticModel, root, targetAttributeArgument, attributeArgumentList, attributeNode, cancellationToken));
                }
            }

            // clear up duplicate types
            potentialConversionTypes = FilterValidPotentialConversionTypes(semanticModel, mutablePotentialConversionTypes);
            return !potentialConversionTypes.IsEmpty;
        }

        protected override CommonConversion ClassifyConversion(SemanticModel semanticModel, ExpressionSyntax expression, ITypeSymbol type)
            => semanticModel.ClassifyConversion(expression, type).ToCommonConversion();
    }
}
