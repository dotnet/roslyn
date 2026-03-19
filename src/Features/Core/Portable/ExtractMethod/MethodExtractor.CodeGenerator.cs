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
        public static readonly SyntaxAnnotation InsertionPointAnnotation = new();

        /// <summary>
        /// Marks nodes that cause control flow to leave the extracted selection.  This is commonly constructs like <see
        /// langword="return"/>, <see langword="break"/>, <see langword="continue"/> and the like.  We mark these with
        /// annotations at the start of the extraction process so that we can find these nodes again later after they
        /// have been extracted to rewrite them as needed.  Specifically, constructs like <see langword="break"/>, <see
        /// langword="continue"/> cannot cross a method boundary.  As such, they must be translated to a <see
        /// langword="return"/> statement that returns a value indicating the flow control construct that should be
        /// executed at the callsite after the extracted method is called.
        /// </summary>
        public static readonly SyntaxAnnotation ExitPointAnnotation = new();

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

            // TODO: Check if these namesare already in scope and if so, generate non-colliding ones.
            protected const string FlowControlName = "flowControl";
            protected const string ReturnValueName = "value";

            protected readonly SelectionResult SelectionResult;
            protected readonly AnalyzerResult AnalyzerResult;

            protected readonly ExtractMethodGenerationOptions ExtractMethodGenerationOptions;
            protected readonly TCodeGenerationOptions Options;

            protected readonly bool LocalFunction;

            private ITypeSymbol _finalReturnType;

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

            protected abstract SyntaxToken CreateMethodName();
            protected abstract bool LastStatementOrHasReturnStatementInReturnableConstruct();

            protected abstract TNodeUnderContainer GetFirstStatementOrInitializerSelectedAtCallSite();
            protected abstract TNodeUnderContainer GetLastStatementOrInitializerSelectedAtCallSite();
            protected abstract Task<TNodeUnderContainer> GetStatementOrInitializerContainingInvocationToExtractedMethodAsync(CancellationToken cancellationToken);

            protected abstract TExpressionSyntax CreateCallSignature();

            /// <summary>
            /// Statement we create when we are assigning variables and at least one of the variables in a new
            /// declaration that is being created.  <paramref name="variables"/> can be empty.  This can happen
            /// if we are creating a new declaration for a flow control variable.
            /// </summary>
            protected abstract TStatementSyntax CreateDeclarationStatement(
                ImmutableArray<VariableInfo> variables, TExpressionSyntax initialValue, ExtractMethodFlowControlInformation flowControlInformation, CancellationToken cancellationToken);

            /// <summary>
            /// Statement we create when we are assigning variables and all of the variables already exist and are just
            /// being assigned to. <paramref name="variables"/> must be non-empty.
            /// </summary>
            protected abstract TStatementSyntax CreateAssignmentExpressionStatement(
                ImmutableArray<VariableInfo> variables, TExpressionSyntax right);

            protected abstract TExecutableStatementSyntax CreateBreakStatement();
            protected abstract TExecutableStatementSyntax CreateContinueStatement();

            protected abstract TExpressionSyntax CreateFlowControlReturnExpression(
                ExtractMethodFlowControlInformation flowControlInformation, object flowValue);

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
                if (AnalyzerResult.FlowControlInformation.EndPointIsReachable)
                    return statements;

                // All the flow control in the analyzed block is the same (for example, all breaks/continues/returns).
                // In this case add a specific instance of that same flow control construct after the call to the new
                // method to ensure we preserve original control flow.
                if (AnalyzerResult.FlowControlInformation.HasUniformControlFlow())
                {
                    if (AnalyzerResult.FlowControlInformation.BreakStatementCount > 0)
                        return statements.Concat(this.CreateBreakStatement());
                    else if (AnalyzerResult.FlowControlInformation.ContinueStatementCount > 0)
                        return statements.Concat(this.CreateContinueStatement());
                }

                var returnType = SelectionResult.GetReturnType(cancellationToken);
                if (returnType != null && returnType.SpecialType != SpecialType.System_Void)
                    return statements;

                // no return type + end of selection not reachable
                if (LastStatementOrHasReturnStatementInReturnableConstruct())
                    return statements;

                return statements.Concat(CreateReturnStatement([]));
            }

            private TExecutableStatementSyntax CreateReturnStatement(
                ImmutableArray<TExpressionSyntax> expressions)
            {
                var generator = this.SemanticDocument.GetRequiredLanguageService<SyntaxGenerator>();
                return (TExecutableStatementSyntax)generator.ReturnStatement(CreateReturnExpression(expressions));
            }

            private TExpressionSyntax CreateReturnExpression(ImmutableArray<TExpressionSyntax> expressions)
            {
                var generator = this.SemanticDocument.GetRequiredLanguageService<SyntaxGenerator>();
                return
                    expressions.Length == 0 ? null :
                    expressions.Length == 1 ? expressions[0] :
                    (TExpressionSyntax)generator.TupleExpression(expressions.Select(generator.Argument));
            }

            protected async Task<ImmutableArray<TStatementSyntax>> AddInvocationAtCallSiteAsync(
                ImmutableArray<TStatementSyntax> statements, CancellationToken cancellationToken)
            {
                // If the newly extracted method isn't returning any data, and doesn't have complex flow control, then
                // we want to handle that here.  The case where we do need to pass data out is in AddAssignmentStatementToCallSite.
                if (AnalyzerResult.VariablesToUseAsReturnValue.IsEmpty &&
                    !AnalyzerResult.FlowControlInformation.NeedsControlFlowValue())
                {
                    Contract.ThrowIfTrue(AnalyzerResult.GetVariablesToSplitOrMoveOutToCallSite().Any(v => v.UseAsReturnValue));

                    // add invocation expression
                    return statements.Concat(
                        (TStatementSyntax)(SyntaxNode)await GetStatementOrInitializerContainingInvocationToExtractedMethodAsync(cancellationToken).ConfigureAwait(false));
                }

                return statements;
            }

            protected ImmutableArray<TStatementSyntax> AddAssignmentStatementToCallSite(
                ImmutableArray<TStatementSyntax> statements,
                CancellationToken cancellationToken)
            {
                if (AnalyzerResult.VariablesToUseAsReturnValue.IsEmpty &&
                    !AnalyzerResult.FlowControlInformation.NeedsControlFlowValue())
                {
                    return statements;
                }

                var flowControlInformation = AnalyzerResult.FlowControlInformation;
                var variables = AnalyzerResult.VariablesToUseAsReturnValue;
                if (variables.Any(v => v.ReturnBehavior == ReturnBehavior.Initialization) ||
                    flowControlInformation.NeedsControlFlowValue())
                {
                    var declarationStatement = CreateDeclarationStatement(
                        variables, CreateCallSignature(), flowControlInformation, cancellationToken);

                    return statements.Concat(declarationStatement.WithAdditionalAnnotations(CallSiteAnnotation));
                }

                return statements.Concat(
                    CreateAssignmentExpressionStatement(variables, CreateCallSignature()).WithAdditionalAnnotations(CallSiteAnnotation));
            }

            protected ImmutableArray<TStatementSyntax> CreateDeclarationStatements(
                ImmutableArray<VariableInfo> variables, CancellationToken cancellationToken)
            {
                return variables.SelectAsArray(
                    v => CreateDeclarationStatement([v], initialValue: null, flowControlInformation: null, cancellationToken));
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
                        [variable], initialValue: null, flowControlInformation: null, cancellationToken));
                }

                return list.ToImmutableAndClear();
            }

            protected ImmutableArray<TStatementSyntax> AppendReturnStatementIfNeeded(ImmutableArray<TStatementSyntax> statements)
            {
                // No need to add a return statement if we already have one.
                var syntaxFacts = this.SemanticDocument.GetRequiredLanguageService<ISyntaxFactsService>();
                if (statements is [.., var lastStatement] &&
                    syntaxFacts.IsReturnStatement(lastStatement))
                {
                    return statements;
                }

                var generator = this.SemanticDocument.GetRequiredLanguageService<SyntaxGenerator>();

                if (this.AnalyzerResult.FlowControlInformation.TryGetFallThroughFlowValue(out var fallthroughValue))
                {
                    return statements.Concat(CreateReturnStatement([CreateFlowControlReturnExpression(this.AnalyzerResult.FlowControlInformation, fallthroughValue)]));
                }
                else if (!this.AnalyzerResult.VariablesToUseAsReturnValue.IsEmpty)
                {
                    return statements.Concat(CreateReturnStatement([
                        CreateReturnExpression(AnalyzerResult.VariablesToUseAsReturnValue.SelectAsArray(
                            static (v, generator) => (TExpressionSyntax)generator.IdentifierName(v.Name),
                            generator))]));
                }
                else
                {
                    return statements;
                }
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

            protected TExecutableStatementSyntax GetStatementContainingInvocationToExtractedMethodWorker()
            {
                var callSignature = CreateCallSignature();

                var generator = this.SemanticDocument.Document.GetRequiredLanguageService<SyntaxGenerator>();
                return AnalyzerResult.CoreReturnType.SpecialType != SpecialType.System_Void
                    ? (TExecutableStatementSyntax)generator.ReturnStatement(callSignature)
                    : (TExecutableStatementSyntax)generator.ExpressionStatement(callSignature);
            }

            public ITypeSymbol GetFinalReturnType()
            {
                return _finalReturnType ??= WrapWithTaskIfNecessary(AddFlowControlTypeIfNecessary(this.AnalyzerResult.CoreReturnType));

                ITypeSymbol AddFlowControlTypeIfNecessary(ITypeSymbol coreReturnType)
                {
                    var controlFlowValueType = this.AnalyzerResult.FlowControlInformation.ControlFlowValueType;

                    // If don't need to report complex flow control to the caller.  Just return whatever the inner method wanted to iriginally return.
                    if (controlFlowValueType.SpecialType == SpecialType.System_Void)
                        return coreReturnType;

                    // We need to report complex flow control to the caller.

                    // If the method wasn't going to return any values to begin with, then all we have to do is
                    // return the control value value to the caller to indicate what flow control path to take.
                    if (coreReturnType.SpecialType == SpecialType.System_Void)
                        return controlFlowValueType;

                    // We need to report both the control flow data and the original data.
                    var compilation = this.SemanticDocument.SemanticModel.Compilation;
                    return compilation.CreateTupleTypeSymbol(
                        [controlFlowValueType, coreReturnType],
                        [FlowControlName, ReturnValueName]);
                }

                ITypeSymbol WrapWithTaskIfNecessary(ITypeSymbol type)
                {
                    if (!this.SelectionResult.ContainsAwaitExpression())
                        return type;

                    // If we're awaiting, then we're going to be returning a task of some sort.  Convert `void` to
                    // `Task` and any other T to `Task<T>`.
                    var compilation = this.SemanticDocument.SemanticModel.Compilation;
                    return type.SpecialType == SpecialType.System_Void
                        ? compilation.TaskType()
                        : compilation.TaskOfTType().Construct(type);
                }
            }
        }
    }
}
