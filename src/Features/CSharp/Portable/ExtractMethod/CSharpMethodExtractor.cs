// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod
{
    internal partial class CSharpMethodExtractor : MethodExtractor
    {
        public CSharpMethodExtractor(CSharpSelectionResult result)
            : base(result)
        {
        }

        protected override Task<AnalyzerResult> AnalyzeAsync(SelectionResult selectionResult, CancellationToken cancellationToken)
            => CSharpAnalyzer.AnalyzeAsync(selectionResult, cancellationToken);

        protected override async Task<InsertionPoint> GetInsertionPointAsync(SemanticDocument document, int position, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(position >= 0);

            var root = await document.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var basePosition = root.FindToken(position);

            var memberNode = basePosition.GetAncestor<MemberDeclarationSyntax>();
            Contract.ThrowIfNull(memberNode);
            Contract.ThrowIfTrue(memberNode.Kind() == SyntaxKind.NamespaceDeclaration);

            if (memberNode is GlobalStatementSyntax globalStatement)
            {
                // check whether we are extracting whole global statement out
                if (this.OriginalSelectionResult.FinalSpan.Contains(memberNode.Span))
                {
                    return await InsertionPoint.CreateAsync(document, globalStatement.Parent, cancellationToken).ConfigureAwait(false);
                }

                return await InsertionPoint.CreateAsync(document, globalStatement.Statement, cancellationToken).ConfigureAwait(false);
            }

            return await InsertionPoint.CreateAsync(document, memberNode, cancellationToken).ConfigureAwait(false);
        }

        protected override async Task<TriviaResult> PreserveTriviaAsync(SelectionResult selectionResult, CancellationToken cancellationToken)
        {
            return await CSharpTriviaResult.ProcessAsync(selectionResult, cancellationToken).ConfigureAwait(false);
        }

        protected override async Task<SemanticDocument> ExpandAsync(SelectionResult selection, CancellationToken cancellationToken)
        {
            var lastExpression = selection.GetFirstTokenInSelection().GetCommonRoot(selection.GetLastTokenInSelection()).GetAncestors<ExpressionSyntax>().LastOrDefault();
            if (lastExpression == null)
            {
                return selection.SemanticDocument;
            }

            var newExpression = await Simplifier.ExpandAsync(lastExpression, selection.SemanticDocument.Document, n => n != selection.GetContainingScope(), expandParameter: false, cancellationToken: cancellationToken).ConfigureAwait(false);
            return await selection.SemanticDocument.WithSyntaxRootAsync(selection.SemanticDocument.Root.ReplaceNode(lastExpression, newExpression), cancellationToken).ConfigureAwait(false);
        }

        protected override Task<MethodExtractor.GeneratedCode> GenerateCodeAsync(InsertionPoint insertionPoint, SelectionResult selectionResult, AnalyzerResult analyzeResult, CancellationToken cancellationToken)
        {
            return CSharpCodeGenerator.GenerateAsync(insertionPoint, selectionResult, analyzeResult, cancellationToken);
        }

        protected override IEnumerable<AbstractFormattingRule> GetFormattingRules(Document document)
        {
            return SpecializedCollections.SingletonEnumerable(new FormattingRule()).Concat(Formatter.GetDefaultFormattingRules(document));
        }

        protected override SyntaxToken GetMethodNameAtInvocation(IEnumerable<SyntaxNodeOrToken> methodNames)
        {
            return (SyntaxToken)methodNames.FirstOrDefault(t => !t.Parent.IsKind(SyntaxKind.MethodDeclaration));
        }

        protected override async Task<OperationStatus> CheckTypeAsync(
            Document document,
            SyntaxNode contextNode,
            Location location,
            ITypeSymbol type,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(type);

            // this happens when there is no return type
            if (type.SpecialType == SpecialType.System_Void)
            {
                return OperationStatus.Succeeded;
            }

            if (type.TypeKind == TypeKind.Error ||
                type.TypeKind == TypeKind.Unknown)
            {
                return OperationStatus.ErrorOrUnknownType;
            }

            // if it is type parameter, make sure we are getting same type parameter
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            foreach (var typeParameter in TypeParameterCollector.Collect(type))
            {
                var typeName = SyntaxFactory.ParseTypeName(typeParameter.Name);
                var currentType = semanticModel.GetSpeculativeTypeInfo(contextNode.SpanStart, typeName, SpeculativeBindingOption.BindAsTypeOrNamespace).Type;
                if (currentType == null || !AllNullabilityIgnoringSymbolComparer.Instance.Equals(currentType, typeParameter))
                {
                    return new OperationStatus(OperationStatusFlag.BestEffort,
                        string.Format(FeaturesResources.Type_parameter_0_is_hidden_by_another_type_parameter_1,
                            typeParameter.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                            currentType == null ? string.Empty : currentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
                }
            }

            return OperationStatus.Succeeded;
        }
    }
}
