// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod
{
    internal partial class CSharpMethodExtractor : MethodExtractor
    {
        public CSharpMethodExtractor(CSharpSelectionResult result, ExtractMethodGenerationOptions options, bool localFunction)
            : base(result, options, localFunction)
        {
        }

        protected override Task<AnalyzerResult> AnalyzeAsync(SelectionResult selectionResult, bool localFunction, CancellationToken cancellationToken)
            => CSharpAnalyzer.AnalyzeAsync(selectionResult, localFunction, cancellationToken);

        protected override async Task<InsertionPoint> GetInsertionPointAsync(SemanticDocument document, CancellationToken cancellationToken)
        {
            var originalSpanStart = OriginalSelectionResult.OriginalSpan.Start;
            Contract.ThrowIfFalse(originalSpanStart >= 0);

            var root = await document.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var basePosition = root.FindToken(originalSpanStart);

            if (LocalFunction)
            {
                // If we are extracting a local function and are within a local function, then we want the new function to be created within the
                // existing local function instead of the overarching method.
                var localFunctionNode = basePosition.GetAncestor<LocalFunctionStatementSyntax>(node => (node.Body != null && node.Body.Span.Contains(OriginalSelectionResult.OriginalSpan)) ||
                                                                                                       (node.ExpressionBody != null && node.ExpressionBody.Span.Contains(OriginalSelectionResult.OriginalSpan)));
                if (localFunctionNode is object)
                {
                    return await InsertionPoint.CreateAsync(document, localFunctionNode, cancellationToken).ConfigureAwait(false);
                }
            }

            var memberNode = basePosition.GetAncestor<MemberDeclarationSyntax>();
            Contract.ThrowIfNull(memberNode);
            Contract.ThrowIfTrue(memberNode.Kind() == SyntaxKind.NamespaceDeclaration);

            if (LocalFunction && memberNode is BasePropertyDeclarationSyntax propertyDeclaration)
            {
                var accessorNode = basePosition.GetAncestor<AccessorDeclarationSyntax>();
                if (accessorNode is object)
                {
                    return await InsertionPoint.CreateAsync(document, accessorNode, cancellationToken).ConfigureAwait(false);
                }
            }

            if (memberNode is GlobalStatementSyntax globalStatement)
            {
                // check whether we are extracting whole global statement out
                if (OriginalSelectionResult.FinalSpan.Contains(memberNode.Span))
                {
                    return await InsertionPoint.CreateAsync(document, globalStatement.Parent, cancellationToken).ConfigureAwait(false);
                }

                // check whether the global statement is a statement container
                if (!globalStatement.Statement.IsStatementContainerNode() && !root.SyntaxTree.IsScript())
                {
                    // The extracted function will be a new global statement
                    return await InsertionPoint.CreateAsync(document, globalStatement.Parent, cancellationToken).ConfigureAwait(false);
                }

                return await InsertionPoint.CreateAsync(document, globalStatement.Statement, cancellationToken).ConfigureAwait(false);
            }

            return await InsertionPoint.CreateAsync(document, memberNode, cancellationToken).ConfigureAwait(false);
        }

        protected override async Task<TriviaResult> PreserveTriviaAsync(SelectionResult selectionResult, CancellationToken cancellationToken)
            => await CSharpTriviaResult.ProcessAsync(selectionResult, cancellationToken).ConfigureAwait(false);

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

        protected override Task<GeneratedCode> GenerateCodeAsync(InsertionPoint insertionPoint, SelectionResult selectionResult, AnalyzerResult analyzeResult, CodeGenerationOptions options, NamingStylePreferencesProvider namingPreferences, CancellationToken cancellationToken)
            => CSharpCodeGenerator.GenerateAsync(insertionPoint, selectionResult, analyzeResult, (CSharpCodeGenerationOptions)options, namingPreferences, LocalFunction, cancellationToken);

        protected override ImmutableArray<AbstractFormattingRule> GetCustomFormattingRules(Document document)
            => ImmutableArray.Create<AbstractFormattingRule>(new FormattingRule());

        protected override SyntaxToken GetMethodNameAtInvocation(IEnumerable<SyntaxNodeOrToken> methodNames)
            => (SyntaxToken)methodNames.FirstOrDefault(t => !t.Parent.IsKind(SyntaxKind.MethodDeclaration));

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

            if (type.TypeKind is TypeKind.Error or
                TypeKind.Unknown)
            {
                return OperationStatus.ErrorOrUnknownType;
            }

            // if it is type parameter, make sure we are getting same type parameter
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            foreach (var typeParameter in TypeParameterCollector.Collect(type))
            {
                var typeName = SyntaxFactory.ParseTypeName(typeParameter.Name);
                var currentType = semanticModel.GetSpeculativeTypeInfo(contextNode.SpanStart, typeName, SpeculativeBindingOption.BindAsTypeOrNamespace).Type;
                if (currentType == null || !SymbolEqualityComparer.Default.Equals(currentType, typeParameter))
                {
                    return new OperationStatus(OperationStatusFlag.BestEffort,
                        string.Format(FeaturesResources.Type_parameter_0_is_hidden_by_another_type_parameter_1,
                            typeParameter.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                            currentType == null ? string.Empty : currentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
                }
            }

            return OperationStatus.Succeeded;
        }

        protected override async Task<(Document document, SyntaxToken methodName, SyntaxNode methodDefinition)> InsertNewLineBeforeLocalFunctionIfNecessaryAsync(
            Document document,
            SyntaxToken methodName,
            SyntaxNode methodDefinition,
            CancellationToken cancellationToken)
        {
            // Checking to see if there is already an empty line before the local method declaration.
            var leadingTrivia = methodDefinition.GetLeadingTrivia();
            if (!leadingTrivia.Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia)) && !methodDefinition.FindTokenOnLeftOfPosition(methodDefinition.SpanStart).IsKind(SyntaxKind.OpenBraceToken))
            {
                var originalMethodDefinition = methodDefinition;
                var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
                methodDefinition = methodDefinition.WithPrependedLeadingTrivia(SpecializedCollections.SingletonEnumerable(SyntaxFactory.EndOfLine(options.GetOption(FormattingOptions2.NewLine))));

                if (!originalMethodDefinition.FindTokenOnLeftOfPosition(originalMethodDefinition.SpanStart).TrailingTrivia.Any(SyntaxKind.EndOfLineTrivia))
                {
                    // Add a second new line since there were no line endings in the original form
                    methodDefinition = methodDefinition.WithPrependedLeadingTrivia(SpecializedCollections.SingletonEnumerable(SyntaxFactory.EndOfLine(options.GetOption(FormattingOptions2.NewLine))));
                }

                // Generating the new document and associated variables.
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                document = document.WithSyntaxRoot(root.ReplaceNode(originalMethodDefinition, methodDefinition));

                var newRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                methodName = newRoot.FindToken(methodName.SpanStart);
            }

            return (document, methodName, methodDefinition);
        }
    }
}
