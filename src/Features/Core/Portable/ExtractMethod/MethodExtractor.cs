// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod;

internal abstract partial class AbstractExtractMethodService<
    TStatementSyntax,
    TExecutableStatementSyntax,
    TExpressionSyntax>
{
    internal abstract partial class MethodExtractor(
        SelectionResult selectionResult,
        ExtractMethodGenerationOptions options,
        bool localFunction)
    {
        protected readonly SelectionResult OriginalSelectionResult = selectionResult;
        protected readonly ExtractMethodGenerationOptions Options = options;
        protected readonly bool LocalFunction = localFunction;

        protected abstract SyntaxNode ParseTypeName(string name);
        protected abstract AnalyzerResult Analyze(CancellationToken cancellationToken);
        protected abstract SyntaxNode GetInsertionPointNode(AnalyzerResult analyzerResult, CancellationToken cancellationToken);
        protected abstract Task<TriviaResult> PreserveTriviaAsync(SyntaxNode root, CancellationToken cancellationToken);

        protected abstract CodeGenerator CreateCodeGenerator(SelectionResult selectionResult, AnalyzerResult analyzerResult);

        protected abstract AbstractFormattingRule GetCustomFormattingRule(Document document);

        protected abstract Task<(Document document, SyntaxToken invocationNameToken)> InsertNewLineBeforeLocalFunctionIfNecessaryAsync(
            Document document, SyntaxToken invocationNameToken, SyntaxNode methodDefinition, CancellationToken cancellationToken);

        public ExtractMethodResult ExtractMethod(OperationStatus initialStatus, CancellationToken cancellationToken)
        {
            var originalSemanticDocument = OriginalSelectionResult.SemanticDocument;
            var analyzeResult = Analyze(cancellationToken);

            var status = CheckVariableTypes(analyzeResult.Status.With(initialStatus), analyzeResult);
            if (status.Failed)
                return ExtractMethodResult.Fail(status);

            var insertionPointNode = GetInsertionPointNode(analyzeResult, cancellationToken);

            if (!CanAddTo(originalSemanticDocument.Document, insertionPointNode, out var canAddStatus))
                return ExtractMethodResult.Fail(canAddStatus);

            cancellationToken.ThrowIfCancellationRequested();
            var codeGenerator = this.CreateCodeGenerator(this.OriginalSelectionResult, analyzeResult);

            var statements = codeGenerator.GetNewMethodStatements(insertionPointNode, cancellationToken);
            if (statements.Status.Failed)
                return ExtractMethodResult.Fail(statements.Status);

            return ExtractMethodResult.Success(
                status,
                async cancellationToken =>
                {
                    var analyzedDocument = await GetAnnotatedDocumentAndInsertionPointAsync(
                        OriginalSelectionResult, analyzeResult, insertionPointNode, cancellationToken).ConfigureAwait(false);

                    var triviaResult = await PreserveTriviaAsync(analyzedDocument.Root, cancellationToken).ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();

                    var generator = this.CreateCodeGenerator(
                        OriginalSelectionResult.With(triviaResult.SemanticDocument),
                        analyzeResult);
                    var generatedCode = await generator.GenerateAsync(cancellationToken).ConfigureAwait(false);

                    var afterTriviaRestored = await triviaResult.ApplyAsync(generatedCode, cancellationToken).ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();

                    var documentWithoutFinalFormatting = afterTriviaRestored.Document;

                    cancellationToken.ThrowIfCancellationRequested();

                    var newRoot = afterTriviaRestored.Root;

                    var invocationNameToken = newRoot.GetAnnotatedTokens(MethodNameAnnotation).Single();

                    // Do some final patchups of whitespace when inserting a local function.
                    if (LocalFunction)
                    {
                        var methodDefinition = newRoot.GetAnnotatedNodesAndTokens(MethodDefinitionAnnotation).FirstOrDefault().AsNode();
                        (documentWithoutFinalFormatting, invocationNameToken) = await InsertNewLineBeforeLocalFunctionIfNecessaryAsync(
                            documentWithoutFinalFormatting, invocationNameToken, methodDefinition, cancellationToken).ConfigureAwait(false);
                    }

                    return await GetFormattedDocumentAsync(
                        documentWithoutFinalFormatting, invocationNameToken, cancellationToken).ConfigureAwait(false);
                });

            bool CanAddTo(Document document, SyntaxNode insertionPointNode, out OperationStatus status)
            {
                var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
                var syntaxKinds = syntaxFacts.SyntaxKinds;
                var codeGenService = document.GetLanguageService<ICodeGenerationService>();

                if (insertionPointNode is null)
                {
                    status = OperationStatus.NoValidLocationToInsertMethodCall;
                    return false;
                }

                var destination = insertionPointNode;
                if (!LocalFunction)
                {
                    var mappedPoint = insertionPointNode.RawKind == syntaxKinds.GlobalStatement
                        ? insertionPointNode.Parent
                        : insertionPointNode;
                    destination = mappedPoint.Parent ?? mappedPoint;
                }

                if (!codeGenService.CanAddTo(destination, document.Project.Solution, cancellationToken))
                {
                    status = OperationStatus.OverlapsHiddenPosition;
                    return false;
                }

                status = OperationStatus.SucceededStatus;
                return true;
            }
        }

        private async Task<(Document document, SyntaxToken? invocationNameToken)> GetFormattedDocumentAsync(
            Document document,
            SyntaxToken? invocationNameToken,
            CancellationToken cancellationToken)
        {
            var annotation = new SyntaxAnnotation();

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            if (invocationNameToken != null)
                root = root.ReplaceToken(invocationNameToken.Value, invocationNameToken.Value.WithAdditionalAnnotations(annotation));

            var annotatedDocument = document.WithSyntaxRoot(root);
            var simplifiedDocument = await Simplifier.ReduceAsync(annotatedDocument, Simplifier.Annotation, this.Options.CodeCleanupOptions.SimplifierOptions, cancellationToken).ConfigureAwait(false);
            var simplifiedRoot = await simplifiedDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var services = document.Project.Solution.Services;

            var formattingRules = GetFormattingRules(document);
            var formattedDocument = simplifiedDocument.WithSyntaxRoot(
                Formatter.Format(simplifiedRoot, Formatter.Annotation, services, this.Options.CodeCleanupOptions.FormattingOptions, formattingRules, cancellationToken));

            var formattedRoot = await formattedDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var finalInvocationNameToken = formattedRoot.GetAnnotatedTokens(annotation).SingleOrDefault();
            return (formattedDocument, finalInvocationNameToken == default ? null : finalInvocationNameToken);
        }

        private static async Task<SemanticDocument> GetAnnotatedDocumentAndInsertionPointAsync(
            SelectionResult originalSelectionResult,
            AnalyzerResult analyzeResult,
            SyntaxNode insertionPointNode,
            CancellationToken cancellationToken)
        {
            var document = originalSelectionResult.SemanticDocument;

            var tokenMap = new MultiDictionary<SyntaxToken, SyntaxAnnotation>();
            foreach (var variable in analyzeResult.Variables)
                variable.AddIdentifierTokenAnnotationPair(tokenMap, cancellationToken);

            var exitPoints = originalSelectionResult.IsExtractMethodOnExpression
                ? []
                : originalSelectionResult.GetStatementControlFlowAnalysis().ExitPoints;
            var finalRoot = document.Root.ReplaceSyntax(
                nodes: exitPoints.Append(insertionPointNode),
                computeReplacementNode: (o, n) =>
                {
                    // intentionally using 'n' (new) here.  We want to see any updated sub tokens that were updated in computeReplacementToken
                    if (o == insertionPointNode)
                        return n.WithAdditionalAnnotations(InsertionPointAnnotation);
                    else
                        return n.WithAdditionalAnnotations(ExitPointAnnotation);
                },
                tokens: tokenMap.Keys,
                computeReplacementToken: (o, n) => o.WithAdditionalAnnotations(tokenMap[o]),
                trivia: null,
                computeReplacementTrivia: null);

            var finalDocument = await document.WithSyntaxRootAsync(finalRoot, cancellationToken).ConfigureAwait(false);

            return finalDocument;
        }

        private ImmutableArray<AbstractFormattingRule> GetFormattingRules(Document document)
            => [GetCustomFormattingRule(document), .. Formatter.GetDefaultFormattingRules(document)];

        private OperationStatus CheckVariableTypes(
            OperationStatus status,
            AnalyzerResult analyzeResult)
        {
            var semanticModel = OriginalSelectionResult.SemanticDocument.SemanticModel;

            if (status.Failed)
                return status;

            foreach (var variable in analyzeResult.Variables)
            {
                status = status.With(CheckType(semanticModel, variable.SymbolType));
                if (status.Failed)
                    return status;
            }

            return status.With(CheckType(semanticModel, analyzeResult.CoreReturnType));
        }

        private OperationStatus CheckType(
            SemanticModel semanticModel, ITypeSymbol type)
        {
            Contract.ThrowIfNull(type);

            // this happens when there is no return type
            if (type.SpecialType == SpecialType.System_Void)
                return OperationStatus.SucceededStatus;

            if (type.TypeKind is TypeKind.Error or TypeKind.Unknown)
                return OperationStatus.ErrorOrUnknownType;

            // if it is type parameter, make sure we are getting same type parameter
            foreach (var typeParameter in TypeParameterCollector.Collect(type))
            {
                var typeName = ParseTypeName(typeParameter.Name);
                var currentType = semanticModel.GetSpeculativeTypeInfo(this.OriginalSelectionResult.FinalSpan.Start, typeName, SpeculativeBindingOption.BindAsTypeOrNamespace).Type;
                if (currentType == null || !SymbolEqualityComparer.Default.Equals(currentType, semanticModel.ResolveType(typeParameter)))
                {
                    return new OperationStatus(succeeded: true,
                        string.Format(FeaturesResources.Type_parameter_0_is_hidden_by_another_type_parameter_1,
                            typeParameter.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                            currentType == null ? string.Empty : currentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
                }
            }

            return OperationStatus.SucceededStatus;
        }

        protected static string MakeMethodName(string prefix, string originalName, bool camelCase)
        {
            var startingWithLetter = originalName.ToCharArray().SkipWhile(c => !char.IsLetter(c)).ToArray();
            var name = startingWithLetter.Length == 0 ? originalName : new string(startingWithLetter);

            if (camelCase && !prefix.IsEmpty())
            {
                prefix = char.ToLowerInvariant(prefix[0]) + prefix[1..];
            }

            return char.IsUpper(name[0])
                ? prefix + name
                : prefix + char.ToUpper(name[0]).ToString() + name[1..];
        }
    }
}
