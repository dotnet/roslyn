// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal abstract partial class MethodExtractor
    {
        protected abstract partial class CodeGenerator<TStatement, TExpression, TNodeUnderContainer>
            where TStatement : SyntaxNode
            where TExpression : SyntaxNode
            where TNodeUnderContainer : SyntaxNode
        {
            protected readonly SyntaxAnnotation MethodNameAnnotation;
            protected readonly SyntaxAnnotation MethodDefinitionAnnotation;
            protected readonly SyntaxAnnotation CallSiteAnnotation;

            protected readonly InsertionPoint InsertionPoint;
            protected readonly SemanticDocument SemanticDocument;
            protected readonly SelectionResult SelectionResult;
            protected readonly AnalyzerResult AnalyzerResult;

            protected CodeGenerator(InsertionPoint insertionPoint, SelectionResult selectionResult, AnalyzerResult analyzerResult)
            {
                Contract.ThrowIfFalse(insertionPoint.SemanticDocument == analyzerResult.SemanticDocument);

                InsertionPoint = insertionPoint;
                SemanticDocument = insertionPoint.SemanticDocument;

                SelectionResult = selectionResult;
                AnalyzerResult = analyzerResult;

                MethodNameAnnotation = new SyntaxAnnotation();
                CallSiteAnnotation = new SyntaxAnnotation();
                MethodDefinitionAnnotation = new SyntaxAnnotation();
            }

            #region method to be implemented in sub classes

            protected abstract SyntaxNode GetOutermostCallSiteContainerToProcess(CancellationToken cancellationToken);
            protected abstract Task<SyntaxNode> GenerateBodyForCallSiteContainerAsync(CancellationToken cancellationToken);
            protected abstract SyntaxNode GetPreviousMember(SemanticDocument document);
            protected abstract OperationStatus<IMethodSymbol> GenerateMethodDefinition(CancellationToken cancellationToken);

            protected abstract SyntaxToken CreateIdentifier(string name);
            protected abstract SyntaxToken CreateMethodName();
            protected abstract bool LastStatementOrHasReturnStatementInReturnableConstruct();

            protected abstract TNodeUnderContainer GetFirstStatementOrInitializerSelectedAtCallSite();
            protected abstract TNodeUnderContainer GetLastStatementOrInitializerSelectedAtCallSite();
            protected abstract Task<TNodeUnderContainer> GetStatementOrInitializerContainingInvocationToExtractedMethodAsync(SyntaxAnnotation callsiteAnnotation, CancellationToken cancellationToken);

            protected abstract TExpression CreateCallSignature();
            protected abstract TStatement CreateDeclarationStatement(VariableInfo variable, TExpression initialValue, CancellationToken cancellationToken);
            protected abstract TStatement CreateAssignmentExpressionStatement(SyntaxToken identifier, TExpression rvalue);
            protected abstract TStatement CreateReturnStatement(string identifierName = null);

            protected abstract IEnumerable<TStatement> GetInitialStatementsForMethodDefinitions();
            #endregion

            public async Task<GeneratedCode> GenerateAsync(CancellationToken cancellationToken)
            {
                var root = SemanticDocument.Root;

                // should I check venus hidden position check here as well?
                root = root.ReplaceNode(GetOutermostCallSiteContainerToProcess(cancellationToken), await GenerateBodyForCallSiteContainerAsync(cancellationToken).ConfigureAwait(false));
                var callSiteDocument = await SemanticDocument.WithSyntaxRootAsync(root, cancellationToken).ConfigureAwait(false);

                var newCallSiteRoot = callSiteDocument.Root;
                var previousMemberNode = GetPreviousMember(callSiteDocument);

                // it is possible in a script file case where there is no previous member. in that case, insert new text into top level script
                var destination = (previousMemberNode.Parent == null) ? previousMemberNode : previousMemberNode.Parent;

                var codeGenerationService = SemanticDocument.Document.GetLanguageService<ICodeGenerationService>();

                var result = GenerateMethodDefinition(cancellationToken);
                var newContainer = codeGenerationService.AddMethod(
                    destination, result.Data,
                    new CodeGenerationOptions(afterThisLocation: previousMemberNode.GetLocation(), generateDefaultAccessibility: true, generateMethodBodies: true),
                    cancellationToken);

                var newDocument = callSiteDocument.Document.WithSyntaxRoot(newCallSiteRoot.ReplaceNode(destination, newContainer));
                newDocument = await Simplifier.ReduceAsync(newDocument, Simplifier.Annotation, null, cancellationToken).ConfigureAwait(false);

                var finalDocument = await SemanticDocument.CreateAsync(newDocument, cancellationToken).ConfigureAwait(false);
                var finalRoot = finalDocument.Root;

                var methodDefinition = finalRoot.GetAnnotatedNodesAndTokens(MethodDefinitionAnnotation).FirstOrDefault();
                if (!methodDefinition.IsNode || methodDefinition.AsNode() == null)
                {
                    return await CreateGeneratedCodeAsync(
                        result.Status.With(OperationStatus.FailedWithUnknownReason), finalDocument, cancellationToken).ConfigureAwait(false);
                }

                if (methodDefinition.SyntaxTree.IsHiddenPosition(methodDefinition.AsNode().SpanStart, cancellationToken) ||
                    methodDefinition.SyntaxTree.IsHiddenPosition(methodDefinition.AsNode().Span.End, cancellationToken))
                {
                    return await CreateGeneratedCodeAsync(
                        result.Status.With(OperationStatus.OverlapsHiddenPosition), finalDocument, cancellationToken).ConfigureAwait(false);
                }

                return await CreateGeneratedCodeAsync(result.Status, finalDocument, cancellationToken).ConfigureAwait(false);
            }

            protected virtual Task<GeneratedCode> CreateGeneratedCodeAsync(OperationStatus status, SemanticDocument newDocument, CancellationToken cancellationToken)
            {
                return Task.FromResult(new GeneratedCode(
                    status,
                    newDocument,
                    MethodNameAnnotation,
                    CallSiteAnnotation,
                    MethodDefinitionAnnotation));
            }

            protected VariableInfo GetOutermostVariableToMoveIntoMethodDefinition(CancellationToken cancellationToken)
            {
                var variables = new List<VariableInfo>(AnalyzerResult.GetVariablesToMoveIntoMethodDefinition(cancellationToken));
                if (variables.Count <= 0)
                {
                    return null;
                }

                VariableInfo.SortVariables(SemanticDocument.SemanticModel.Compilation, variables);
                return variables[0];
            }

            protected IEnumerable<TStatement> AddReturnIfUnreachable(
                IEnumerable<TStatement> statements, CancellationToken cancellationToken)
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

            protected async Task<IEnumerable<TStatement>> AddInvocationAtCallSiteAsync(
                IEnumerable<TStatement> statements, CancellationToken cancellationToken)
            {
                if (AnalyzerResult.HasVariableToUseAsReturnValue)
                {
                    return statements;
                }

                Contract.ThrowIfTrue(AnalyzerResult.GetVariablesToSplitOrMoveOutToCallSite(cancellationToken).Any(v => v.UseAsReturnValue));

                // add invocation expression
                return statements.Concat(
                    (TStatement)(SyntaxNode)await GetStatementOrInitializerContainingInvocationToExtractedMethodAsync(CallSiteAnnotation, cancellationToken).ConfigureAwait(false));
            }

            protected IEnumerable<TStatement> AddAssignmentStatementToCallSite(
                IEnumerable<TStatement> statements,
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

            protected IEnumerable<TStatement> CreateDeclarationStatements(
                IEnumerable<VariableInfo> variables, CancellationToken cancellationToken)
            {
                var list = new List<TStatement>();

                foreach (var variable in variables)
                {
                    var declaration = CreateDeclarationStatement(
                        variable, initialValue: null, cancellationToken: cancellationToken);
                    list.Add(declaration);
                }

                return list;
            }

            protected IEnumerable<TStatement> AddSplitOrMoveDeclarationOutStatementsToCallSite(
                IEnumerable<TStatement> statements, CancellationToken cancellationToken)
            {
                var list = new List<TStatement>();

                foreach (var variable in AnalyzerResult.GetVariablesToSplitOrMoveOutToCallSite(cancellationToken))
                {
                    if (variable.UseAsReturnValue)
                    {
                        continue;
                    }

                    var declaration = CreateDeclarationStatement(
                        variable, initialValue: null, cancellationToken: cancellationToken);
                    list.Add(declaration);
                }

                return list;
            }

            protected IEnumerable<TStatement> AppendReturnStatementIfNeeded(IEnumerable<TStatement> statements)
            {
                if (!AnalyzerResult.HasVariableToUseAsReturnValue)
                {
                    return statements;
                }

                var variableToUseAsReturnValue = AnalyzerResult.VariableToUseAsReturnValue;

                Contract.ThrowIfFalse(variableToUseAsReturnValue.ReturnBehavior == ReturnBehavior.Assignment ||
                                      variableToUseAsReturnValue.ReturnBehavior == ReturnBehavior.Initialization);

                return statements.Concat(CreateReturnStatement(AnalyzerResult.VariableToUseAsReturnValue.Name));
            }

            protected HashSet<SyntaxAnnotation> CreateVariableDeclarationToRemoveMap(
                IEnumerable<VariableInfo> variables, CancellationToken cancellationToken)
            {
                var annotations = new List<Tuple<SyntaxToken, SyntaxAnnotation>>();

                foreach (var variable in variables)
                {
                    Contract.ThrowIfFalse(variable.GetDeclarationBehavior(cancellationToken) == DeclarationBehavior.MoveOut ||
                                          variable.GetDeclarationBehavior(cancellationToken) == DeclarationBehavior.MoveIn ||
                                          variable.GetDeclarationBehavior(cancellationToken) == DeclarationBehavior.Delete);

                    variable.AddIdentifierTokenAnnotationPair(annotations, cancellationToken);
                }

                return new HashSet<SyntaxAnnotation>(annotations.Select(t => t.Item2));
            }

            protected ImmutableArray<ITypeParameterSymbol> CreateMethodTypeParameters(CancellationToken cancellationToken)
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
                        parameter.GetAttributes(), parameter.Variance, parameter.Name, ImmutableArray.Create<ITypeSymbol>(),
                        parameter.HasConstructorConstraint, parameter.HasReferenceTypeConstraint, parameter.HasUnmanagedTypeConstraint,
                        parameter.HasValueTypeConstraint, parameter.HasNotNullConstraint, parameter.Ordinal));
                }

                return typeParameters.ToImmutableAndFree();
            }

            protected ImmutableArray<IParameterSymbol> CreateMethodParameters()
            {
                var parameters = ArrayBuilder<IParameterSymbol>.GetInstance();

                foreach (var parameter in AnalyzerResult.MethodParameters)
                {
                    var refKind = GetRefKind(parameter.ParameterModifier);
                    var type = parameter.GetVariableType(SemanticDocument);

                    parameters.Add(
                        CodeGenerationSymbolFactory.CreateParameterSymbol(
                            attributes: ImmutableArray<AttributeData>.Empty,
                            refKind: refKind,
                            isParams: false,
                            type: type,
                            name: parameter.Name));
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
