﻿// Licensed to the .NET Foundation under one or more agreements.
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
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod;

internal abstract partial class AbstractExtractMethodService<
    TStatementSyntax,
    TExecutableStatementSyntax,
    TExpressionSyntax>
{
    internal abstract partial class MethodExtractor
    {
        public static readonly SyntaxAnnotation MethodNameAnnotation = new();
        public static readonly SyntaxAnnotation MethodDefinitionAnnotation = new();
        public static readonly SyntaxAnnotation CallSiteAnnotation = new();

        protected abstract class CodeGenerator
        {
            /// <summary>
            /// Used to produced the set of statements that will go into the generated method.
            /// </summary>
            public abstract OperationStatus<ImmutableArray<SyntaxNode>> GetNewMethodStatements(
                SyntaxNode insertionPointNode, CancellationToken cancellationToken);

            public abstract Task<SemanticDocument> GenerateAsync(CancellationToken cancellationToken);
        }

        protected abstract partial class CodeGenerator<TNodeUnderContainer, TCodeGenerationOptions> : CodeGenerator
            where TNodeUnderContainer : SyntaxNode
            where TCodeGenerationOptions : CodeGenerationOptions
        {
            private static readonly CodeGenerationContext s_codeGenerationContext = new(addImports: false);

            protected readonly SelectionResult SelectionResult;
            protected readonly AnalyzerResult AnalyzerResult;

            protected readonly ExtractMethodGenerationOptions ExtractMethodGenerationOptions;
            protected readonly TCodeGenerationOptions Options;

            protected readonly bool LocalFunction;

            protected CodeGenerator(
                SelectionResult selectionResult,
                AnalyzerResult analyzerResult,
                ExtractMethodGenerationOptions options,
                bool localFunction)
            {
                SelectionResult = selectionResult;
                AnalyzerResult = analyzerResult;

                ExtractMethodGenerationOptions = options;
                Options = (TCodeGenerationOptions)options.CodeGenerationOptions;
                LocalFunction = localFunction;
            }

            protected SemanticDocument SemanticDocument => SelectionResult.SemanticDocument;

            #region method to be implemented in sub classes

            protected abstract SyntaxNode GetCallSiteContainerFromOutermostMoveInVariable();

            protected abstract Task<SyntaxNode> GenerateBodyForCallSiteContainerAsync(SyntaxNode insertionPointNode, SyntaxNode outermostCallSiteContainer, CancellationToken cancellationToken);
            protected abstract IMethodSymbol GenerateMethodDefinition(SyntaxNode insertionPointNode, CancellationToken cancellationToken);
            protected abstract bool ShouldLocalFunctionCaptureParameter(SyntaxNode node);

            protected abstract SyntaxToken CreateIdentifier(string name);
            protected abstract SyntaxToken CreateMethodName();
            protected abstract bool LastStatementOrHasReturnStatementInReturnableConstruct();

            protected abstract TNodeUnderContainer GetFirstStatementOrInitializerSelectedAtCallSite();
            protected abstract TNodeUnderContainer GetLastStatementOrInitializerSelectedAtCallSite();
            protected abstract Task<TNodeUnderContainer> GetStatementOrInitializerContainingInvocationToExtractedMethodAsync(CancellationToken cancellationToken);

            protected abstract TExpressionSyntax CreateCallSignature();
            protected abstract TStatementSyntax CreateDeclarationStatement(ImmutableArray<VariableInfo> variables, TExpressionSyntax initialValue, CancellationToken cancellationToken);
            protected abstract TStatementSyntax CreateAssignmentExpressionStatement(ImmutableArray<VariableInfo> variables, TExpressionSyntax rvalue);
            protected abstract TStatementSyntax CreateReturnStatement(params string[] identifierNames);

            protected abstract ImmutableArray<TStatementSyntax> GetInitialStatementsForMethodDefinitions();

            protected abstract Task<SemanticDocument> UpdateMethodAfterGenerationAsync(
                SemanticDocument originalDocument, IMethodSymbol methodSymbolResult, CancellationToken cancellationToken);

            protected abstract Task<SemanticDocument> PerformFinalTriviaFixupAsync(
                SemanticDocument newDocument, CancellationToken cancellationToken);
            #endregion

            private static SyntaxNode GetInsertionPoint(SemanticDocument document)
                => document.Root.GetAnnotatedNodes(InsertionPointAnnotation).Single();

            public sealed override async Task<SemanticDocument> GenerateAsync(CancellationToken cancellationToken)
            {
                var semanticDocument = SelectionResult.SemanticDocument;
                var insertionPoint = GetInsertionPoint(semanticDocument);
                var newMethodDefinition = GenerateMethodDefinition(insertionPoint, cancellationToken);
                var callSiteDocument = await InsertMethodAndUpdateCallSiteAsync(semanticDocument, newMethodDefinition, cancellationToken).ConfigureAwait(false);

                // For nullable reference types, we can provide a better experience by reducing use of nullable
                // reference types after a method is done being generated. If we can determine that the method never
                // returns null, for example, then we can make the signature into a non-null reference type even though
                // the original type was nullable. This allows our code generation to follow our recommendation of only
                // using nullable when necessary. This is done after method generation instead of at analyzer time
                // because it's purely based on the resulting code, which the generator can modify as needed. If return
                // statements are added, the flow analysis could change to indicate something different. It's cleaner to
                // rely on flow analysis of the final resulting code than to try and predict from the analyzer what will
                // happen in the generator. 
                var finalDocument = await UpdateMethodAfterGenerationAsync(callSiteDocument, newMethodDefinition, cancellationToken).ConfigureAwait(false);

                return await PerformFinalTriviaFixupAsync(finalDocument, cancellationToken).ConfigureAwait(false);
            }

            private async Task<SemanticDocument> InsertMethodAndUpdateCallSiteAsync(
                SemanticDocument document, IMethodSymbol newMethodDefinition, CancellationToken cancellationToken)
            {
                var codeGenerationService = document.GetRequiredLanguageService<ICodeGenerationService>();

                // First, update the callsite with the call to the new method.
                var outermostCallSiteContainer = GetOutermostCallSiteContainerToProcess(cancellationToken);

                var rootWithUpdatedCallSite = this.SemanticDocument.Root.ReplaceNode(
                    outermostCallSiteContainer,
                    await GenerateBodyForCallSiteContainerAsync(
                        GetInsertionPoint(document), outermostCallSiteContainer, cancellationToken).ConfigureAwait(false));

                // Then insert the local-function/method into the updated document that contains the updated callsite.
                var documentWithUpdatedCallSite = await this.SemanticDocument.WithSyntaxRootAsync(rootWithUpdatedCallSite, cancellationToken).ConfigureAwait(false);
                var finalRoot = LocalFunction
                    ? InsertLocalFunction()
                    : InsertNormalMethod();

                return await documentWithUpdatedCallSite.WithSyntaxRootAsync(finalRoot, cancellationToken).ConfigureAwait(false);

                SyntaxNode InsertLocalFunction()
                {
                    // Now, insert the local function.
                    var info = codeGenerationService.GetInfo(
                        s_codeGenerationContext.With(generateDefaultAccessibility: false),
                        Options,
                        document.Project.ParseOptions);

                    var localMethod = codeGenerationService.CreateMethodDeclaration(newMethodDefinition, CodeGenerationDestination.Unspecified, info, cancellationToken);

                    // Find the destination for the local function after the callsite has been fixed up.
                    var destination = GetInsertionPoint(documentWithUpdatedCallSite);
                    var updatedDestination = codeGenerationService.AddStatements(destination, [localMethod], info, cancellationToken);

                    var finalRoot = documentWithUpdatedCallSite.Root.ReplaceNode(destination, updatedDestination);
                    return finalRoot;
                }

                SyntaxNode InsertNormalMethod()
                {
                    var syntaxKinds = document.GetRequiredLanguageService<ISyntaxKindsService>();

                    // Find the destination for the new method after the callsite has been fixed up.
                    var mappedMember = GetInsertionPoint(documentWithUpdatedCallSite);
                    mappedMember = mappedMember.Parent?.RawKind == syntaxKinds.GlobalStatement
                        ? mappedMember.Parent
                        : mappedMember;

                    mappedMember = mappedMember.RawKind == syntaxKinds.PrimaryConstructorBaseType
                        ? mappedMember.Parent
                        : mappedMember;

                    // it is possible in a script file case where there is no previous member. in that case, insert new text into top level script
                    var destination = mappedMember.Parent ?? mappedMember;

                    var info = codeGenerationService.GetInfo(
                        s_codeGenerationContext.With(
                            afterThisLocation: mappedMember.GetLocation(),
                            generateDefaultAccessibility: true,
                            generateMethodBodies: true),
                        Options,
                        documentWithUpdatedCallSite.Project.ParseOptions);

                    var newContainer = codeGenerationService.AddMethod(destination, newMethodDefinition, info, cancellationToken);
                    var finalRoot = documentWithUpdatedCallSite.Root.ReplaceNode(destination, newContainer);
                    return finalRoot;
                }
            }

            private SyntaxNode GetOutermostCallSiteContainerToProcess(CancellationToken cancellationToken)
            {
                var callSiteContainer = GetCallSiteContainerFromOutermostMoveInVariable();
                return callSiteContainer ?? this.SelectionResult.GetOutermostCallSiteContainerToProcess(cancellationToken);
            }

            protected VariableInfo GetOutermostVariableToMoveIntoMethodDefinition()
            {
                return this.AnalyzerResult.GetOutermostVariableToMoveIntoMethodDefinition();
            }

            protected ImmutableArray<TStatementSyntax> AddReturnIfUnreachable(
                ImmutableArray<TStatementSyntax> statements, CancellationToken cancellationToken)
            {
                if (AnalyzerResult.EndOfSelectionReachable)
                    return statements;

                var returnType = SelectionResult.GetReturnType(cancellationToken);
                if (returnType != null && returnType.SpecialType != SpecialType.System_Void)
                    return statements;

                // no return type + end of selection not reachable
                if (LastStatementOrHasReturnStatementInReturnableConstruct())
                    return statements;

                return statements.Concat(CreateReturnStatement());
            }

            protected async Task<ImmutableArray<TStatementSyntax>> AddInvocationAtCallSiteAsync(
                ImmutableArray<TStatementSyntax> statements, CancellationToken cancellationToken)
            {
                if (!AnalyzerResult.VariablesToUseAsReturnValue.IsEmpty)
                    return statements;

                Contract.ThrowIfTrue(AnalyzerResult.GetVariablesToSplitOrMoveOutToCallSite().Any(v => v.UseAsReturnValue));

                // add invocation expression
                return statements.Concat(
                    (TStatementSyntax)(SyntaxNode)await GetStatementOrInitializerContainingInvocationToExtractedMethodAsync(cancellationToken).ConfigureAwait(false));
            }

            protected ImmutableArray<TStatementSyntax> AddAssignmentStatementToCallSite(
                ImmutableArray<TStatementSyntax> statements,
                CancellationToken cancellationToken)
            {
                if (AnalyzerResult.VariablesToUseAsReturnValue.IsEmpty)
                    return statements;

                var variables = AnalyzerResult.VariablesToUseAsReturnValue;
                if (variables.Any(v => v.ReturnBehavior == ReturnBehavior.Initialization))
                {
                    var declarationStatement = CreateDeclarationStatement(
                        variables, CreateCallSignature(), cancellationToken);
                    declarationStatement = declarationStatement.WithAdditionalAnnotations(CallSiteAnnotation);

                    return statements.Concat(declarationStatement);
                }

                return statements.Concat(
                    CreateAssignmentExpressionStatement(variables, CreateCallSignature()).WithAdditionalAnnotations(CallSiteAnnotation));
            }

            protected ImmutableArray<TStatementSyntax> CreateDeclarationStatements(
                ImmutableArray<VariableInfo> variables, CancellationToken cancellationToken)
            {
                return variables.SelectAsArray(v => CreateDeclarationStatement([v], initialValue: null, cancellationToken));
            }

            protected ImmutableArray<TStatementSyntax> AddSplitOrMoveDeclarationOutStatementsToCallSite(
                CancellationToken cancellationToken)
            {
                using var _ = ArrayBuilder<TStatementSyntax>.GetInstance(out var list);

                foreach (var variable in AnalyzerResult.GetVariablesToSplitOrMoveOutToCallSite())
                {
                    if (variable.UseAsReturnValue)
                        continue;

                    list.Add(CreateDeclarationStatement(
                        [variable], initialValue: null, cancellationToken: cancellationToken));
                }

                return list.ToImmutableAndClear();
            }

            protected ImmutableArray<TStatementSyntax> AppendReturnStatementIfNeeded(ImmutableArray<TStatementSyntax> statements)
            {
                if (AnalyzerResult.VariablesToUseAsReturnValue.IsEmpty)
                    return statements;

                return statements.Concat(CreateReturnStatement([.. AnalyzerResult.VariablesToUseAsReturnValue.Select(b => b.Name)]));
            }

            protected static HashSet<SyntaxAnnotation> CreateVariableDeclarationToRemoveMap(
                IEnumerable<VariableInfo> variables, CancellationToken cancellationToken)
            {
                var annotations = new MultiDictionary<SyntaxToken, SyntaxAnnotation>();

                foreach (var variable in variables)
                {
                    Contract.ThrowIfFalse(variable.GetDeclarationBehavior() is
                        DeclarationBehavior.MoveOut or
                        DeclarationBehavior.MoveIn);

                    variable.AddIdentifierTokenAnnotationPair(annotations, cancellationToken);
                }

                return [.. annotations.Values.SelectMany(v => v)];
            }

            protected ImmutableArray<ITypeParameterSymbol> CreateMethodTypeParameters()
            {
                if (AnalyzerResult.MethodTypeParametersInDeclaration.IsEmpty)
                    return [];

                var set = new HashSet<ITypeParameterSymbol>(AnalyzerResult.MethodTypeParametersInConstraintList);

                var typeParameters = ArrayBuilder<ITypeParameterSymbol>.GetInstance();
                foreach (var parameter in AnalyzerResult.MethodTypeParametersInDeclaration)
                {
                    if (parameter != null && set.Contains(parameter))
                    {
                        typeParameters.Add(parameter);
                        continue;
                    }

                    typeParameters.Add(CodeGenerationSymbolFactory.CreateTypeParameter(
                        parameter.GetAttributes(), parameter.Variance, parameter.Name, [], parameter.NullableAnnotation,
                        parameter.HasConstructorConstraint, parameter.HasReferenceTypeConstraint, parameter.HasUnmanagedTypeConstraint,
                        parameter.HasValueTypeConstraint, parameter.HasNotNullConstraint, parameter.AllowsRefLikeType, parameter.Ordinal));
                }

                return typeParameters.ToImmutableAndFree();
            }

            protected ImmutableArray<IParameterSymbol> CreateMethodParameters()
            {
                using var _ = ArrayBuilder<IParameterSymbol>.GetInstance(out var parameters);
                var isLocalFunction = LocalFunction && ShouldLocalFunctionCaptureParameter(SemanticDocument.Root);
                foreach (var parameter in AnalyzerResult.MethodParameters)
                {
                    if (!isLocalFunction || !parameter.CanBeCapturedByLocalFunction)
                    {
                        var refKind = GetRefKind(parameter.ParameterModifier);
                        parameters.Add(CodeGenerationSymbolFactory.CreateParameterSymbol(
                            attributes: [],
                            refKind: refKind,
                            isParams: false,
                            type: parameter.SymbolType,
                            name: parameter.Name));
                    }
                }

                return parameters.ToImmutableAndClear();
            }

            private static RefKind GetRefKind(ParameterBehavior parameterBehavior)
                => parameterBehavior switch
                {
                    ParameterBehavior.Ref => RefKind.Ref,
                    ParameterBehavior.Out => RefKind.Out,
                    _ => RefKind.None
                };

            protected TStatementSyntax GetStatementContainingInvocationToExtractedMethodWorker()
            {
                var callSignature = CreateCallSignature();

                var generator = this.SemanticDocument.Document.GetRequiredLanguageService<SyntaxGenerator>();
                return AnalyzerResult.HasReturnType
                    ? (TStatementSyntax)generator.ReturnStatement(callSignature)
                    : (TStatementSyntax)generator.ExpressionStatement(callSignature);
            }
        }
    }
}
