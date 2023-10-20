// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal abstract partial class MethodExtractor<TSelectionResult, TStatementSyntax, TExpressionSyntax>
    {
        protected abstract class CodeGenerator
        {
            public abstract OperationStatus<ImmutableArray<SyntaxNode>> GetNewMethodStatements(
                SyntaxNode insertionPointNode, CancellationToken cancellationToken);
        }

        protected abstract partial class CodeGenerator<TStatement, TExpression, TNodeUnderContainer, TCodeGenerationOptions>
            : CodeGenerator
            where TStatement : SyntaxNode
            where TExpression : SyntaxNode
            where TNodeUnderContainer : SyntaxNode
            where TCodeGenerationOptions : CodeGenerationOptions
        {
            protected readonly SyntaxAnnotation MethodNameAnnotation;
            protected readonly SyntaxAnnotation MethodDefinitionAnnotation;
            protected readonly SyntaxAnnotation CallSiteAnnotation;

            // protected readonly SyntaxNode InsertionPointNode;
            protected readonly SelectionResult<TStatementSyntax> SelectionResult;
            protected readonly AnalyzerResult AnalyzerResult;

            protected readonly TCodeGenerationOptions Options;
            protected readonly bool LocalFunction;

            protected CodeGenerator(SelectionResult<TStatementSyntax> selectionResult, AnalyzerResult analyzerResult, TCodeGenerationOptions options, bool localFunction)
            {
                // InsertionPointNode = insertionPointNode;

                SelectionResult = selectionResult;
                AnalyzerResult = analyzerResult;

                Options = options;
                LocalFunction = localFunction;

                MethodNameAnnotation = new SyntaxAnnotation();
                CallSiteAnnotation = new SyntaxAnnotation();
                MethodDefinitionAnnotation = new SyntaxAnnotation();
            }

            protected SemanticDocument SemanticDocument => SelectionResult.SemanticDocument;

            #region method to be implemented in sub classes

            protected abstract SyntaxNode GetOutermostCallSiteContainerToProcess(CancellationToken cancellationToken);
            protected abstract Task<SyntaxNode> GenerateBodyForCallSiteContainerAsync(SyntaxNode insertionPointNode, SyntaxNode outermostCallSiteContainer, CancellationToken cancellationToken);
            protected abstract IMethodSymbol GenerateMethodDefinition(SyntaxNode insertionPointNode, CancellationToken cancellationToken);
            protected abstract bool ShouldLocalFunctionCaptureParameter(SyntaxNode node);

            protected abstract SyntaxToken CreateIdentifier(string name);
            protected abstract SyntaxToken CreateMethodName();
            protected abstract bool LastStatementOrHasReturnStatementInReturnableConstruct();

            protected abstract TNodeUnderContainer GetFirstStatementOrInitializerSelectedAtCallSite();
            protected abstract TNodeUnderContainer GetLastStatementOrInitializerSelectedAtCallSite();
            protected abstract Task<TNodeUnderContainer> GetStatementOrInitializerContainingInvocationToExtractedMethodAsync(CancellationToken cancellationToken);

            protected abstract TExpression CreateCallSignature();
            protected abstract TStatement CreateDeclarationStatement(VariableInfo variable, TExpression initialValue, CancellationToken cancellationToken);
            protected abstract TStatement CreateAssignmentExpressionStatement(SyntaxToken identifier, TExpression rvalue);
            protected abstract TStatement CreateReturnStatement(string identifierName = null);

            protected abstract ImmutableArray<TStatement> GetInitialStatementsForMethodDefinitions();

            protected abstract Task<SemanticDocument> UpdateMethodAfterGenerationAsync(
                SemanticDocument originalDocument, IMethodSymbol methodSymbolResult, CancellationToken cancellationToken);

            #endregion

            public async Task<GeneratedCode> GenerateAsync(InsertionPoint insertionPoint, CancellationToken cancellationToken)
            {
                var newMethodDefinition = GenerateMethodDefinition(insertionPoint.GetContext(), cancellationToken);
                var callSiteDocument = await InsertMethodAndUpdateCallSiteAsync(insertionPoint, newMethodDefinition, cancellationToken).ConfigureAwait(false);

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

                return await CreateGeneratedCodeAsync(finalDocument, cancellationToken).ConfigureAwait(false);
            }

            private async Task<SemanticDocument> InsertMethodAndUpdateCallSiteAsync(
                InsertionPoint insertionPoint, IMethodSymbol newMethodDefinition, CancellationToken cancellationToken)
            {
                var document = this.SemanticDocument.Document;
                var codeGenerationService = document.GetLanguageService<ICodeGenerationService>();

                // First, update the callsite with the call to the new method.
                var outermostCallSiteContainer = GetOutermostCallSiteContainerToProcess(cancellationToken);

                var rootWithUpdatedCallSite = this.SemanticDocument.Root.ReplaceNode(
                    outermostCallSiteContainer,
                    await GenerateBodyForCallSiteContainerAsync(
                        insertionPoint.GetContext(), outermostCallSiteContainer, cancellationToken).ConfigureAwait(false));

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
                        new CodeGenerationContext(generateDefaultAccessibility: false),
                        Options,
                        document.Project.ParseOptions);

                    var localMethod = codeGenerationService.CreateMethodDeclaration(newMethodDefinition, CodeGenerationDestination.Unspecified, info, cancellationToken);

                    // Find the destination for the local function after the callsite has been fixed up.
                    var destination = insertionPoint.With(documentWithUpdatedCallSite).GetContext();
                    var updatedDestination = codeGenerationService.AddStatements(destination, new[] { localMethod }, info, cancellationToken);

                    var finalRoot = documentWithUpdatedCallSite.Root.ReplaceNode(destination, updatedDestination);
                    return finalRoot;
                }

                SyntaxNode InsertNormalMethod()
                {
                    var syntaxKinds = document.GetLanguageService<ISyntaxKindsService>();

                    // Find the destination for the new method after the callsite has been fixed up.
                    var mappedMember = insertionPoint.With(documentWithUpdatedCallSite).GetContext();
                    mappedMember = mappedMember.Parent?.RawKind == syntaxKinds.GlobalStatement
                        ? mappedMember.Parent
                        : mappedMember;

                    // it is possible in a script file case where there is no previous member. in that case, insert new text into top level script
                    var destination = mappedMember.Parent ?? mappedMember;

                    var info = codeGenerationService.GetInfo(
                        new CodeGenerationContext(
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

            protected virtual Task<GeneratedCode> CreateGeneratedCodeAsync(SemanticDocument newDocument, CancellationToken cancellationToken)
            {
                return Task.FromResult(new GeneratedCode(
                    newDocument,
                    MethodNameAnnotation,
                    CallSiteAnnotation,
                    MethodDefinitionAnnotation));
            }

            protected VariableInfo GetOutermostVariableToMoveIntoMethodDefinition(CancellationToken cancellationToken)
            {
                return this.AnalyzerResult.GetOutermostVariableToMoveIntoMethodDefinition(cancellationToken);
            }

            protected ImmutableArray<TStatement> AddReturnIfUnreachable(ImmutableArray<TStatement> statements)
            {
                if (AnalyzerResult.EndOfSelectionReachable)
                {
                    return statements;
                }

                var type = SelectionResult.GetContainingScopeType();
                if (type != null && type.SpecialType != SpecialType.System_Void)
                {
                    return statements;
                }

                // no return type + end of selection not reachable
                if (LastStatementOrHasReturnStatementInReturnableConstruct())
                {
                    return statements;
                }

                return statements.Concat(CreateReturnStatement());
            }

            protected async Task<ImmutableArray<TStatement>> AddInvocationAtCallSiteAsync(
                ImmutableArray<TStatement> statements, CancellationToken cancellationToken)
            {
                if (AnalyzerResult.HasVariableToUseAsReturnValue)
                {
                    return statements;
                }

                Contract.ThrowIfTrue(AnalyzerResult.GetVariablesToSplitOrMoveOutToCallSite(cancellationToken).Any(v => v.UseAsReturnValue));

                // add invocation expression
                return statements.Concat(
                    (TStatement)(SyntaxNode)await GetStatementOrInitializerContainingInvocationToExtractedMethodAsync(cancellationToken).ConfigureAwait(false));
            }

            protected ImmutableArray<TStatement> AddAssignmentStatementToCallSite(
                ImmutableArray<TStatement> statements,
                CancellationToken cancellationToken)
            {
                if (!AnalyzerResult.HasVariableToUseAsReturnValue)
                {
                    return statements;
                }

                var variable = AnalyzerResult.VariableToUseAsReturnValue;
                if (variable.ReturnBehavior == ReturnBehavior.Initialization)
                {
                    // there must be one decl behavior when there is "return value and initialize" variable
                    Contract.ThrowIfFalse(AnalyzerResult.GetVariablesToSplitOrMoveOutToCallSite(cancellationToken).Single(v => v.ReturnBehavior == ReturnBehavior.Initialization) != null);

                    var declarationStatement = CreateDeclarationStatement(
                        variable, CreateCallSignature(), cancellationToken);
                    declarationStatement = declarationStatement.WithAdditionalAnnotations(CallSiteAnnotation);

                    return statements.Concat(declarationStatement);
                }

                Contract.ThrowIfFalse(variable.ReturnBehavior == ReturnBehavior.Assignment);
                return statements.Concat(
                    CreateAssignmentExpressionStatement(CreateIdentifier(variable.Name), CreateCallSignature()).WithAdditionalAnnotations(CallSiteAnnotation));
            }

            protected ImmutableArray<TStatement> CreateDeclarationStatements(
                ImmutableArray<VariableInfo> variables, CancellationToken cancellationToken)
            {
                return variables.SelectAsArray(v => CreateDeclarationStatement(v, initialValue: null, cancellationToken));
            }

            protected ImmutableArray<TStatement> AddSplitOrMoveDeclarationOutStatementsToCallSite(
                CancellationToken cancellationToken)
            {
                using var _ = ArrayBuilder<TStatement>.GetInstance(out var list);

                foreach (var variable in AnalyzerResult.GetVariablesToSplitOrMoveOutToCallSite(cancellationToken))
                {
                    if (variable.UseAsReturnValue)
                        continue;

                    var declaration = CreateDeclarationStatement(
                        variable, initialValue: null, cancellationToken: cancellationToken);
                    list.Add(declaration);
                }

                return list.ToImmutable();
            }

            protected ImmutableArray<TStatement> AppendReturnStatementIfNeeded(ImmutableArray<TStatement> statements)
            {
                if (!AnalyzerResult.HasVariableToUseAsReturnValue)
                {
                    return statements;
                }

                var variableToUseAsReturnValue = AnalyzerResult.VariableToUseAsReturnValue;

                Contract.ThrowIfFalse(variableToUseAsReturnValue.ReturnBehavior is ReturnBehavior.Assignment or
                                      ReturnBehavior.Initialization);

                return statements.Concat(CreateReturnStatement(AnalyzerResult.VariableToUseAsReturnValue.Name));
            }

            protected static HashSet<SyntaxAnnotation> CreateVariableDeclarationToRemoveMap(
                IEnumerable<VariableInfo> variables, CancellationToken cancellationToken)
            {
                var annotations = new List<Tuple<SyntaxToken, SyntaxAnnotation>>();

                foreach (var variable in variables)
                {
                    Contract.ThrowIfFalse(variable.GetDeclarationBehavior(cancellationToken) is DeclarationBehavior.MoveOut or
                                          DeclarationBehavior.MoveIn or
                                          DeclarationBehavior.Delete);

                    variable.AddIdentifierTokenAnnotationPair(annotations, cancellationToken);
                }

                return new HashSet<SyntaxAnnotation>(annotations.Select(t => t.Item2));
            }

            protected ImmutableArray<ITypeParameterSymbol> CreateMethodTypeParameters()
            {
                if (AnalyzerResult.MethodTypeParametersInDeclaration.Count == 0)
                {
                    return ImmutableArray<ITypeParameterSymbol>.Empty;
                }

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
                        parameter.GetAttributes(), parameter.Variance, parameter.Name, ImmutableArray.Create<ITypeSymbol>(), parameter.NullableAnnotation,
                        parameter.HasConstructorConstraint, parameter.HasReferenceTypeConstraint, parameter.HasUnmanagedTypeConstraint,
                        parameter.HasValueTypeConstraint, parameter.HasNotNullConstraint, parameter.Ordinal));
                }

                return typeParameters.ToImmutableAndFree();
            }

            protected ImmutableArray<IParameterSymbol> CreateMethodParameters()
            {
                var parameters = ArrayBuilder<IParameterSymbol>.GetInstance();
                var isLocalFunction = LocalFunction && ShouldLocalFunctionCaptureParameter(SemanticDocument.Root);
                foreach (var parameter in AnalyzerResult.MethodParameters)
                {
                    if (!isLocalFunction || !parameter.CanBeCapturedByLocalFunction)
                    {
                        var refKind = GetRefKind(parameter.ParameterModifier);
                        var type = parameter.GetVariableType();

                        parameters.Add(
                            CodeGenerationSymbolFactory.CreateParameterSymbol(
                                attributes: ImmutableArray<AttributeData>.Empty,
                                refKind: refKind,
                                isParams: false,
                                type: type,
                                name: parameter.Name));
                    }
                }

                return parameters.ToImmutableAndFree();
            }

            private static RefKind GetRefKind(ParameterBehavior parameterBehavior)
            {
                return parameterBehavior == ParameterBehavior.Ref ? RefKind.Ref :
                            parameterBehavior == ParameterBehavior.Out ? RefKind.Out : RefKind.None;
            }
        }
    }
}
