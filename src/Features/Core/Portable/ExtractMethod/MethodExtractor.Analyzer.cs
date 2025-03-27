﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod;

internal abstract partial class AbstractExtractMethodService<
    TStatementSyntax,
    TExecutableStatementSyntax,
    TExpressionSyntax>
{
    internal abstract partial class MethodExtractor
    {
        protected abstract partial class Analyzer
        {
            protected readonly CancellationToken CancellationToken;
            protected readonly SelectionResult SelectionResult;
            protected readonly bool LocalFunction;

            private SemanticDocument SemanticDocument => SelectionResult.SemanticDocument;
            protected SemanticModel SemanticModel => SemanticDocument.SemanticModel;

            protected ISemanticFactsService SemanticFacts => this.SemanticDocument.Document.GetRequiredLanguageService<ISemanticFactsService>();
            protected ISyntaxFactsService SyntaxFacts => this.SemanticDocument.Document.GetRequiredLanguageService<ISyntaxFactsService>();

            protected Analyzer(SelectionResult selectionResult, bool localFunction, CancellationToken cancellationToken)
            {
                Contract.ThrowIfNull(selectionResult);

                SelectionResult = selectionResult;
                CancellationToken = cancellationToken;
                LocalFunction = localFunction;
            }

            protected abstract bool IsInPrimaryConstructorBaseType();

            /// <summary>
            /// check whether selection contains return statement or not
            /// </summary>
            protected abstract bool ContainsReturnStatementInSelectedCode(ImmutableArray<SyntaxNode> exitPoints);

            protected virtual bool IsReadOutside(ISymbol symbol, HashSet<ISymbol> readOutsideMap)
                => readOutsideMap.Contains(symbol);

            protected abstract bool TreatOutAsRef { get; }

            /// <summary>
            /// get type of the range variable symbol
            /// </summary>
            protected abstract ITypeSymbol? GetRangeVariableType(IRangeVariableSymbol symbol);

            /// <summary>
            /// check whether the selection is at the placed where read-only field is allowed to be extracted out
            /// </summary>
            /// <returns></returns>
            protected abstract bool ReadOnlyFieldAllowed();

            public AnalyzerResult Analyze()
            {
                // do data flow analysis
                var model = this.SemanticDocument.SemanticModel;
                var dataFlowAnalysisData = this.SelectionResult.GetDataFlowAnalysis();

                // build symbol map for the identifiers used inside of the selection
                var symbolMap = GetSymbolMap();

                var isInPrimaryConstructorBaseType = this.IsInPrimaryConstructorBaseType();

                // gather initial local or parameter variable info
                GenerateVariableInfoMap(
                    bestEffort: false, dataFlowAnalysisData, symbolMap, isInPrimaryConstructorBaseType, out var variableInfoMap, out var failedVariables);
                if (failedVariables.Count > 0)
                {
                    // If we weren't able to figure something out, go back and regenerate the map
                    // this time in 'best effort' mode.  We'll give the user a message saying there
                    // was a problem, but we allow them to proceed so they're not unnecessarily
                    // blocked just because we didn't understand something.
                    GenerateVariableInfoMap(
                        bestEffort: true, dataFlowAnalysisData, symbolMap, isInPrimaryConstructorBaseType, out variableInfoMap, out var unused);
                    Contract.ThrowIfFalse(unused.Count == 0);
                }

                var thisParameterBeingRead = (IParameterSymbol?)dataFlowAnalysisData.ReadInside.FirstOrDefault(IsThisParameter);
                var isThisParameterWritten = dataFlowAnalysisData.WrittenInside.Any(static s => IsThisParameter(s));

                // Need to generate an instance method if any primary constructor parameter is read or written inside the
                // selection.  This does not apply if we're in the base-type-list as that will still need a static method.
                var primaryConstructorParameterReadOrWritten = !isInPrimaryConstructorBaseType && dataFlowAnalysisData.ReadInside
                    .Concat(dataFlowAnalysisData.WrittenInside)
                    .OfType<IParameterSymbol>()
                    .FirstOrDefault(s => s.IsPrimaryConstructor(this.CancellationToken)) != null;

                var localFunctionCallsNotWithinSpan = symbolMap.Keys.Where(s => s.IsLocalFunction() && !s.Locations.Any(static (l, self) => self.SelectionResult.FinalSpan.Contains(l.SourceSpan), this));

                // Checks to see if selection includes a local function call + if the given local function declaration is not included in the selection.
                var containsAnyLocalFunctionCallNotWithinSpan = localFunctionCallsNotWithinSpan.Any();

                // Checks to see if selection includes a non-static local function call + if the given local function declaration is not included in the selection.
                var containsNonStaticLocalFunctionCallNotWithinSpan = containsAnyLocalFunctionCallNotWithinSpan && localFunctionCallsNotWithinSpan.Where(s => !s.IsStatic).Any();

                var instanceMemberIsUsed = thisParameterBeingRead != null
                    || isThisParameterWritten
                    || containsNonStaticLocalFunctionCallNotWithinSpan
                    || primaryConstructorParameterReadOrWritten;

                var shouldBeReadOnly = !isThisParameterWritten
                    && thisParameterBeingRead is { Type: { TypeKind: TypeKind.Struct, IsReadOnly: false } };

                // check whether end of selection is reachable
                var endOfSelectionReachable = this.SelectionResult.IsEndOfSelectionReachable();

                // check whether the selection contains "&" over a symbol exist
                var unsafeAddressTakenUsed = dataFlowAnalysisData.UnsafeAddressTaken.Intersect(variableInfoMap.Keys).Any();

                var (variables, returnType, returnsByRef) = GetSignatureInformation(variableInfoMap);

                (returnType, var awaitTaskReturn) = AdjustReturnType(returnType);

                // collect method type variable used in selected code
                var sortedMap = new SortedDictionary<int, ITypeParameterSymbol>();
                var typeParametersInConstraintList = GetMethodTypeParametersInConstraintList(variableInfoMap, symbolMap, sortedMap);
                var typeParametersInDeclaration = GetMethodTypeParametersInDeclaration(returnType, sortedMap);

                // check various error cases
                var operationStatus = GetOperationStatus(
                    symbolMap, variables, failedVariables, unsafeAddressTakenUsed, returnType.ContainsAnonymousType(), containsAnyLocalFunctionCallNotWithinSpan);

                return new AnalyzerResult(
                    typeParametersInDeclaration,
                    typeParametersInConstraintList,
                    variables,
                    returnType,
                    returnsByRef,
                    awaitTaskReturn,
                    instanceMemberIsUsed,
                    shouldBeReadOnly,
                    endOfSelectionReachable,
                    operationStatus);
            }

            private (ITypeSymbol typeSymbol, bool awaitTaskReturn) AdjustReturnType(ITypeSymbol returnType)
            {
                // if selection contains await which is not under async lambda or anonymous delegate,
                // change return type to be wrapped in Task
                var shouldPutAsyncModifier = SelectionResult.CreateAsyncMethod();
                if (shouldPutAsyncModifier)
                    return WrapReturnTypeInTask(returnType);

                // unwrap task if needed
                return (UnwrapTaskIfNeeded(returnType), awaitTaskReturn: false);
            }

            private ITypeSymbol UnwrapTaskIfNeeded(ITypeSymbol returnType)
            {
                // nothing to unwrap
                if (SelectionResult.ContainingScopeHasAsyncKeyword() &&
                    ContainsReturnStatementInSelectedCode())
                {
                    var originalDefinition = returnType.OriginalDefinition;

                    // see whether it needs to be unwrapped
                    var model = this.SemanticDocument.SemanticModel;
                    var taskType = model.Compilation.TaskType();
                    if (originalDefinition.Equals(taskType))
                        return model.Compilation.GetSpecialType(SpecialType.System_Void);

                    var genericTaskType = model.Compilation.TaskOfTType();
                    if (originalDefinition.Equals(genericTaskType))
                        return ((INamedTypeSymbol)returnType).TypeArguments[0];
                }

                // nothing to unwrap
                return returnType;
            }

            private (ITypeSymbol returnType, bool awaitTaskReturn) WrapReturnTypeInTask(ITypeSymbol returnType)
            {
                var compilation = this.SemanticModel.Compilation;
                var taskType = compilation.TaskType();

                // convert void to Task type
                if (taskType is object && returnType.Equals(compilation.GetSpecialType(SpecialType.System_Void)))
                    return (taskType, awaitTaskReturn: true);

                if (!SelectionResult.IsExtractMethodOnExpression && ContainsReturnStatementInSelectedCode())
                    return (returnType, awaitTaskReturn: false);

                var genericTaskType = compilation.TaskOfTType();

                // okay, wrap the return type in Task<T>
                if (genericTaskType is object)
                    returnType = genericTaskType.Construct(returnType);

                return (returnType, awaitTaskReturn: false);
            }

            private (ImmutableArray<VariableInfo> finalOrderedVariableInfos, ITypeSymbol returnType, bool returnsByRef)
                GetSignatureInformation(Dictionary<ISymbol, VariableInfo> symbolMap)
            {
                var allVariableInfos = symbolMap.Values.Order().ToImmutableArray();

                if (this.IsInExpressionOrHasReturnStatement())
                {
                    // check whether current selection contains return statement
                    var (returnType, returnsByRef) = SelectionResult.GetReturnTypeInfo(this.CancellationToken);

                    return (allVariableInfos, returnType, returnsByRef);
                }
                else
                {
                    // no return statement
                    var finalOrderedVariableInfos = MarkVariableInfosToUseAsReturnValueIfPossible(allVariableInfos);
                    var variablesToUseAsReturnValue = finalOrderedVariableInfos.WhereAsArray(v => v.UseAsReturnValue);

                    var returnType = GetReturnType(variablesToUseAsReturnValue);

                    return (finalOrderedVariableInfos, returnType, returnsByRef: false);
                }

                ITypeSymbol GetReturnType(ImmutableArray<VariableInfo> variablesToUseAsReturnValue)
                {
                    var compilation = this.SemanticDocument.SemanticModel.Compilation;

                    if (variablesToUseAsReturnValue.IsEmpty)
                        return compilation.GetSpecialType(SpecialType.System_Void);

                    if (variablesToUseAsReturnValue is [var info])
                        return info.SymbolType;

                    return compilation.CreateTupleTypeSymbol(
                        variablesToUseAsReturnValue.SelectAsArray(v => v.SymbolType),
                        variablesToUseAsReturnValue.SelectAsArray(v => v.Name)!);
                }
            }

            private bool IsInExpressionOrHasReturnStatement()
                => SelectionResult.IsExtractMethodOnExpression || ContainsReturnStatementInSelectedCode();

            private OperationStatus GetOperationStatus(
                MultiDictionary<ISymbol, SyntaxToken> symbolMap,
                ImmutableArray<VariableInfo> variables,
                IList<ISymbol> failedVariables,
                bool unsafeAddressTakenUsed,
                bool returnTypeHasAnonymousType,
                bool containsAnyLocalFunctionCallNotWithinSpan)
            {
                var readonlyFieldStatus = CheckReadOnlyFields(symbolMap);

                var namesWithAnonymousTypes = variables.Where(v => v.OriginalTypeHadAnonymousTypeOrDelegate).Select(v => v.Name ?? string.Empty);
                if (returnTypeHasAnonymousType)
                {
                    namesWithAnonymousTypes = namesWithAnonymousTypes.Concat("return type");
                }

                var anonymousTypeStatus = !namesWithAnonymousTypes.Any()
                    ? OperationStatus.SucceededStatus
                    : new OperationStatus(succeeded: true,
                        string.Format(
                            FeaturesResources.Parameters_type_or_return_type_cannot_be_an_anonymous_type_colon_bracket_0_bracket,
                            string.Join(", ", namesWithAnonymousTypes)));

                var unsafeAddressStatus = unsafeAddressTakenUsed
                    ? OperationStatus.UnsafeAddressTaken
                    : OperationStatus.SucceededStatus;

                var asyncRefOutParameterStatus = CheckAsyncMethodRefOutParameters(variables);

                var variableMapStatus = failedVariables.Count == 0
                    ? OperationStatus.SucceededStatus
                    : new OperationStatus(succeeded: true,
                        string.Format(
                            FeaturesResources.Failed_to_analyze_data_flow_for_0,
                            string.Join(", ", failedVariables.Select(v => v.Name))));

                var localFunctionStatus = (containsAnyLocalFunctionCallNotWithinSpan && !LocalFunction)
                    ? OperationStatus.LocalFunctionCallWithoutDeclaration
                    : OperationStatus.SucceededStatus;

                return readonlyFieldStatus.With(anonymousTypeStatus)
                                          .With(unsafeAddressStatus)
                                          .With(asyncRefOutParameterStatus)
                                          .With(variableMapStatus)
                                          .With(localFunctionStatus);
            }

            private OperationStatus CheckAsyncMethodRefOutParameters(IList<VariableInfo> parameters)
            {
                if (SelectionResult.CreateAsyncMethod())
                {
                    var names = parameters.Where(v => v is { UseAsReturnValue: false, ParameterModifier: ParameterBehavior.Out or ParameterBehavior.Ref })
                                          .Select(p => p.Name ?? string.Empty);

                    if (names.Any())
                        return new OperationStatus(succeeded: true, string.Format(FeaturesResources.Asynchronous_method_cannot_have_ref_out_parameters_colon_bracket_0_bracket, string.Join(", ", names)));
                }

                return OperationStatus.SucceededStatus;
            }

            private MultiDictionary<ISymbol, SyntaxToken> GetSymbolMap()
            {
                var symbolMap = new MultiDictionary<ISymbol, SyntaxToken>();

                var semanticModel = this.SemanticModel;
                var syntaxFacts = this.SyntaxFacts;
                var context = SelectionResult.GetContainingScope();

                foreach (var token in context.DescendantTokens())
                {
                    if (token.IsMissing ||
                        token.Width() <= 0 ||
                        !this.SelectionResult.FinalSpan.Contains(token.Span) ||
                        !syntaxFacts.IsIdentifier(token) ||
                        syntaxFacts.IsNameOfNamedArgument(token.Parent))
                    {
                        continue;
                    }

                    var symbolInfo = semanticModel.GetSymbolInfo(token, this.CancellationToken);
                    foreach (var sym in symbolInfo.GetAllSymbols())
                        symbolMap.Add(sym, token);
                }

                return symbolMap;
            }

            private ImmutableArray<VariableInfo> MarkVariableInfosToUseAsReturnValueIfPossible(ImmutableArray<VariableInfo> variableInfo)
            {
                var index = GetIndexOfVariableInfoToUseAsReturnValue(variableInfo, out var numberOfOutParameters, out var numberOfRefParameters);

                // If there are any variables we'd make out/ref and this is async, then we need to make these the
                // return values of the method since we can't actually have out/ref with an async method.
                var outRefCount = numberOfOutParameters + numberOfRefParameters;
                if (outRefCount > 0 &&
                    this.SelectionResult.CreateAsyncMethod() &&
                    this.SyntaxFacts.SupportsTupleDeconstruction(this.SemanticDocument.Document.Project.ParseOptions!))
                {
                    var result = new FixedSizeArrayBuilder<VariableInfo>(variableInfo.Length);
                    foreach (var info in variableInfo)
                    {
                        result.Add(info.CanBeUsedAsReturnValue && info.ParameterModifier is ParameterBehavior.Out or ParameterBehavior.Ref
                            ? VariableInfo.CreateReturnValue(info)
                            : info);
                    }

                    return result.MoveToImmutable();
                }

                // If there's just one variable that would be ref/out, then make that the return value of the final method.
                if (index >= 0)
                    return variableInfo.SetItem(index, VariableInfo.CreateReturnValue(variableInfo[index]));

                return variableInfo;
            }

            /// <summary>
            /// among variables that will be used as parameters at the extracted method, check whether one of the parameter can be used as return
            /// </summary>
            private int GetIndexOfVariableInfoToUseAsReturnValue(
                ImmutableArray<VariableInfo> variableInfo,
                out int numberOfOutParameters,
                out int numberOfRefParameters)
            {
                numberOfOutParameters = 0;
                numberOfRefParameters = 0;

                var outSymbolIndex = -1;
                var refSymbolIndex = -1;

                for (var i = 0; i < variableInfo.Length; i++)
                {
                    var variable = variableInfo[i];

                    // there should be no-one set as return value yet
                    Contract.ThrowIfTrue(variable.UseAsReturnValue);

                    if (!variable.CanBeUsedAsReturnValue)
                    {
                        continue;
                    }

                    // check modifier
                    if (variable.ParameterModifier == ParameterBehavior.Ref ||
                        (variable.ParameterModifier == ParameterBehavior.Out && TreatOutAsRef))
                    {
                        numberOfRefParameters++;
                        refSymbolIndex = i;
                    }
                    else if (variable.ParameterModifier == ParameterBehavior.Out)
                    {
                        numberOfOutParameters++;
                        outSymbolIndex = i;
                    }
                }

                // if there is only one "out" or "ref", that will be converted to return statement.
                if (numberOfOutParameters == 1)
                {
                    return outSymbolIndex;
                }

                if (numberOfRefParameters == 1)
                {
                    return refSymbolIndex;
                }

                return -1;
            }

            /// <param name="bestEffort">When false, variables whose data flow is not understood
            /// will be returned in <paramref name="failedVariables"/>. When true, we assume any
            /// variable we don't understand has <see cref="VariableStyle.None"/></param>
            private void GenerateVariableInfoMap(
                bool bestEffort,
                DataFlowAnalysis dataFlowAnalysisData,
                MultiDictionary<ISymbol, SyntaxToken> symbolMap,
                bool isInPrimaryConstructorBaseType,
                out Dictionary<ISymbol, VariableInfo> variableInfoMap,
                out List<ISymbol> failedVariables)
            {
                Contract.ThrowIfNull(dataFlowAnalysisData);

                variableInfoMap = [];
                failedVariables = [];

                // create map of each data
                using var _0 = GetPooledSymbolSet(dataFlowAnalysisData.Captured, out var capturedMap);
                using var _1 = GetPooledSymbolSet(dataFlowAnalysisData.DataFlowsIn, out var dataFlowInMap);
                using var _2 = GetPooledSymbolSet(dataFlowAnalysisData.DataFlowsOut, out var dataFlowOutMap);
                using var _3 = GetPooledSymbolSet(dataFlowAnalysisData.AlwaysAssigned, out var alwaysAssignedMap);
                using var _4 = GetPooledSymbolSet(dataFlowAnalysisData.VariablesDeclared, out var variableDeclaredMap);
                using var _5 = GetPooledSymbolSet(dataFlowAnalysisData.ReadInside, out var readInsideMap);
                using var _6 = GetPooledSymbolSet(dataFlowAnalysisData.WrittenInside, out var writtenInsideMap);
                using var _7 = GetPooledSymbolSet(dataFlowAnalysisData.ReadOutside, out var readOutsideMap);
                using var _8 = GetPooledSymbolSet(dataFlowAnalysisData.WrittenOutside, out var writtenOutsideMap);
                using var _9 = GetPooledSymbolSet(dataFlowAnalysisData.UnsafeAddressTaken, out var unsafeAddressTakenMap);

                // gather all meaningful symbols for the span.
                var candidates = new HashSet<ISymbol>(readInsideMap);
                candidates.UnionWith(writtenInsideMap);
                candidates.UnionWith(variableDeclaredMap);

                // Need to analyze from the start of what we're extracting to the end of the scope that this variable could
                // have been referenced in.
                var containingScope = SelectionResult.GetContainingScope();
                var analysisRange = TextSpan.FromBounds(SelectionResult.FinalSpan.Start, containingScope.Span.End);
                var selectionOperation = this.SemanticModel.GetOperation(containingScope);

                foreach (var symbol in candidates)
                {
                    // We don't care about the 'this' parameter.  It will be available to an extracted method already.
                    if (symbol.IsThisParameter())
                        continue;

                    // Primary constructor parameters will be in scope for any instance extracted method.  No need to do
                    // anything special with them.  They won't be in scope for a static method generated in a primary
                    // constructor base type list.
                    if (!isInPrimaryConstructorBaseType && symbol is IParameterSymbol parameter && parameter.IsPrimaryConstructor(this.CancellationToken))
                        continue;

                    if (IsInteractiveSynthesizedParameter(symbol))
                        continue;

                    var captured = capturedMap.Contains(symbol);
                    var dataFlowIn = dataFlowInMap.Contains(symbol);
                    var dataFlowOut = dataFlowOutMap.Contains(symbol);
                    var alwaysAssigned = alwaysAssignedMap.Contains(symbol);
                    var variableDeclared = variableDeclaredMap.Contains(symbol);
                    var readInside = readInsideMap.Contains(symbol);
                    var writtenInside = writtenInsideMap.Contains(symbol);
                    var readOutside = IsReadOutside(symbol, readOutsideMap);
                    var writtenOutside = writtenOutsideMap.Contains(symbol);
                    var unsafeAddressTaken = unsafeAddressTakenMap.Contains(symbol);

                    // if it is static local, make sure it is not defined inside
                    if (symbol.IsStatic)
                    {
                        dataFlowIn = dataFlowIn && !variableDeclared;
                    }

                    // make sure readoutside is true when dataflowout is true (bug #3790)
                    // when a variable is only used inside of loop, a situation where dataflowout == true and readOutside == false
                    // can happen. but for extract method's point of view, this is not an information that would affect output.
                    // so, here we adjust flags to follow predefined assumption.
                    readOutside = readOutside || dataFlowOut;

                    // make sure data flow out is true when declared inside/written inside/read outside/not written outside are true (bug #6277)
                    dataFlowOut = dataFlowOut || (variableDeclared && writtenInside && readOutside && !writtenOutside);

                    // variable that is declared inside but never referenced outside. just ignore it and move to next one.
                    if (variableDeclared && !dataFlowOut && !readOutside && !writtenOutside)
                        continue;

                    // parameter defined inside of the selection (such as lambda parameter) will be ignored (bug # 10964)
                    if (symbol is IParameterSymbol && variableDeclared)
                        continue;

                    var type = GetSymbolType(symbol);
                    if (type == null)
                        continue;

                    // If the variable doesn't have a name, it is invalid.
                    if (symbol.Name.IsEmpty())
                        continue;

                    if (!TryGetVariableStyle(
                            bestEffort, symbolMap, symbol, type,
                            captured, dataFlowIn, dataFlowOut, alwaysAssigned, variableDeclared,
                            readInside, writtenInside, readOutside, writtenOutside, unsafeAddressTaken,
                            out var variableStyle))
                    {
                        Contract.ThrowIfTrue(bestEffort, "Should never fail if bestEffort is true");
                        failedVariables.Add(symbol);
                        continue;
                    }

                    // An assignment to a VB 'function value'.  (e.g. `MethodName = value`) needs to be treated as a
                    // return value from the inner function which the outer function then still assigns to its function
                    // value.
                    if (symbol is ILocalSymbol { IsFunctionValue: true } &&
                        variableStyle.ParameterStyle.DeclarationBehavior != DeclarationBehavior.None)
                    {
                        Contract.ThrowIfFalse(variableStyle.ParameterStyle.DeclarationBehavior == DeclarationBehavior.MoveIn || variableStyle.ParameterStyle.DeclarationBehavior == DeclarationBehavior.SplitIn);
                        variableStyle = AlwaysReturn(variableStyle);
                    }

                    AddVariableToMap(
                        variableInfoMap,
                        symbol,
                        CreateFromSymbol(symbol, type, variableStyle));
                }

                return;

                PooledDisposer<PooledHashSet<ISymbol>> GetPooledSymbolSet(ImmutableArray<ISymbol> symbols, out PooledHashSet<ISymbol> symbolSet)
                {
                    var disposer = PooledHashSet<ISymbol>.GetInstance(out symbolSet);
                    symbolSet.AddRange(symbols);
                    return disposer;
                }

                ITypeSymbol? GetSymbolType(ISymbol symbol)
                {
                    var type = symbol switch
                    {
                        ILocalSymbol local => local.Type,
                        IParameterSymbol parameter => parameter.Type,
                        IRangeVariableSymbol rangeVariable => GetRangeVariableType(rangeVariable),
                        _ => throw ExceptionUtilities.UnexpectedValue(symbol)
                    };

                    if (type is null)
                        return type;

                    // Check if null is possibly assigned to the symbol. If it is, leave nullable annotation as is, otherwise we
                    // can modify the annotation to be NotAnnotated to code that more likely matches the user's intent.

                    if (type.NullableAnnotation is not NullableAnnotation.Annotated)
                        return type;

                    // For Extract-Method we don't care about analyzing the declaration of this variable. For example, even if
                    // it was initially assigned 'null' for the purposes of determining the type of it for a return value, all
                    // we care is if it is null at the end of the selection.  If it is only assigned non-null values, for
                    // example, we want to treat it as non-null.
                    if (selectionOperation is not null &&
                        NullableHelpers.IsSymbolAssignedPossiblyNullValue(
                            this.SemanticFacts, this.SemanticModel, selectionOperation, symbol, analysisRange, includeDeclaration: false, this.CancellationToken) == false)
                    {
                        return type.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
                    }

                    return type;
                }

                static VariableInfo CreateFromSymbol(
                   ISymbol symbol,
                   ITypeSymbol type,
                   VariableStyle style)
                {
                    return symbol switch
                    {
                        ILocalSymbol local => new VariableInfo(
                            new LocalVariableSymbol(local, type),
                            style,
                            useAsReturnValue: false),
                        IParameterSymbol parameter => new VariableInfo(new ParameterVariableSymbol(parameter, type), style, useAsReturnValue: false),
                        IRangeVariableSymbol rangeVariable => new VariableInfo(new QueryVariableSymbol(rangeVariable, type), style, useAsReturnValue: false),
                        _ => throw ExceptionUtilities.UnexpectedValue(symbol)
                    };
                }
            }

            private static void AddVariableToMap(IDictionary<ISymbol, VariableInfo> variableInfoMap, ISymbol localOrParameter, VariableInfo variableInfo)
                => variableInfoMap.Add(localOrParameter, variableInfo);

            private bool TryGetVariableStyle(
                bool bestEffort,
                MultiDictionary<ISymbol, SyntaxToken> symbolMap,
                ISymbol symbol,
                ITypeSymbol type,
                bool captured,
                bool dataFlowIn,
                bool dataFlowOut,
                bool alwaysAssigned,
                bool variableDeclared,
                bool readInside,
                bool writtenInside,
                bool readOutside,
                bool writtenOutside,
                bool unsafeAddressTaken,
                out VariableStyle variableStyle)
            {
                Contract.ThrowIfNull(type);

                if (!ExtractMethodMatrix.TryGetVariableStyle(
                        bestEffort, dataFlowIn, dataFlowOut, alwaysAssigned, variableDeclared,
                        readInside, writtenInside, readOutside, writtenOutside, unsafeAddressTaken,
                        out variableStyle))
                {
                    Contract.ThrowIfTrue(bestEffort, "Should never fail if bestEffort is true");
                    return false;
                }

                if (SelectionContainsOnlyIdentifierWithSameType(type))
                {
                    return true;
                }

                // for captured variable, never try to move the decl into extracted method
                if (captured && variableStyle == VariableStyle.MoveIn)
                {
                    variableStyle = VariableStyle.Out;
                    return true;
                }

                // check special value type cases
                if (type.IsValueType && !IsWrittenInsideForFrameworkValueType(symbolMap, symbol, writtenInside))
                {
                    return true;
                }

                // don't blindly always return. make sure there is a write inside of the selection
                if (!writtenInside)
                {
                    return true;
                }

                variableStyle = AlwaysReturn(variableStyle);
                return true;
            }

            private bool IsWrittenInsideForFrameworkValueType(
                MultiDictionary<ISymbol, SyntaxToken> symbolMap, ISymbol symbol, bool writtenInside)
            {
                var tokens = symbolMap[symbol];
                if (tokens.Count == 0)
                    return writtenInside;

                var semanticFacts = this.SemanticFacts;
                return tokens.Any(t => semanticFacts.IsWrittenTo(this.SemanticModel, t.Parent, this.CancellationToken));
            }

            private bool SelectionContainsOnlyIdentifierWithSameType(ITypeSymbol type)
            {
                if (!SelectionResult.IsExtractMethodOnExpression)
                {
                    return false;
                }

                var firstToken = SelectionResult.GetFirstTokenInSelection();
                var lastToken = SelectionResult.GetLastTokenInSelection();

                if (!firstToken.Equals(lastToken))
                {
                    return false;
                }

                return type.Equals(SelectionResult.GetReturnType(this.CancellationToken));
            }

            protected static VariableStyle AlwaysReturn(VariableStyle style)
            {
                if (style == VariableStyle.InputOnly)
                {
                    return VariableStyle.Ref;
                }

                if (style == VariableStyle.MoveIn)
                {
                    return VariableStyle.Out;
                }

                if (style == VariableStyle.SplitIn)
                {
                    return VariableStyle.Out;
                }

                if (style == VariableStyle.SplitOut)
                {
                    return VariableStyle.OutWithMoveOut;
                }

                return style;
            }

            private static bool IsThisParameter(ISymbol localOrParameter)
            {
                if (localOrParameter is not IParameterSymbol parameter)
                {
                    return false;
                }

                return parameter.IsThis;
            }

            private static bool IsInteractiveSynthesizedParameter(ISymbol localOrParameter)
            {
                if (localOrParameter is not IParameterSymbol parameter)
                {
                    return false;
                }

                return parameter.IsImplicitlyDeclared &&
                       parameter.ContainingAssembly.IsInteractive &&
                       parameter.ContainingSymbol != null &&
                       parameter.ContainingSymbol.ContainingType != null &&
                       parameter.ContainingSymbol.ContainingType.IsScriptClass;
            }

            private bool ContainsReturnStatementInSelectedCode()
            {
                Contract.ThrowIfTrue(SelectionResult.IsExtractMethodOnExpression);
                return ContainsReturnStatementInSelectedCode(this.SelectionResult.GetStatementControlFlowAnalysis().ExitPoints);
            }

            private static void AddTypeParametersToMap(IEnumerable<ITypeParameterSymbol> typeParameters, IDictionary<int, ITypeParameterSymbol> sortedMap)
            {
                foreach (var typeParameter in typeParameters)
                {
                    AddTypeParameterToMap(typeParameter, sortedMap);
                }
            }

            private static void AddTypeParameterToMap(ITypeParameterSymbol typeParameter, IDictionary<int, ITypeParameterSymbol> sortedMap)
            {
                if (typeParameter == null ||
                    typeParameter.DeclaringMethod == null ||
                    sortedMap.ContainsKey(typeParameter.Ordinal))
                {
                    return;
                }

                sortedMap[typeParameter.Ordinal] = typeParameter;
            }

            private void AppendMethodTypeVariableFromDataFlowAnalysis(
                IDictionary<ISymbol, VariableInfo> variableInfoMap,
                IDictionary<int, ITypeParameterSymbol> sortedMap)
            {
                foreach (var symbol in variableInfoMap.Keys)
                {
                    switch (symbol)
                    {
                        case IParameterSymbol parameter:
                            AddTypeParametersToMap(TypeParameterCollector.Collect(parameter.Type), sortedMap);
                            continue;

                        case ILocalSymbol local:
                            AddTypeParametersToMap(TypeParameterCollector.Collect(local.Type), sortedMap);
                            continue;

                        case IRangeVariableSymbol rangeVariable:
                            var type = GetRangeVariableType(rangeVariable);
                            AddTypeParametersToMap(TypeParameterCollector.Collect(type), sortedMap);
                            continue;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(symbol);
                    }
                }
            }

            private static void AppendMethodTypeParameterFromConstraint(SortedDictionary<int, ITypeParameterSymbol> sortedMap)
            {
                var typeParametersInConstraint = new List<ITypeParameterSymbol>();

                // collect all type parameter appears in constraint
                foreach (var typeParameter in sortedMap.Values)
                {
                    var constraintTypes = typeParameter.ConstraintTypes;
                    if (constraintTypes.IsDefaultOrEmpty)
                    {
                        continue;
                    }

                    foreach (var type in constraintTypes)
                    {
                        // constraint itself is type parameter
                        typeParametersInConstraint.AddRange(TypeParameterCollector.Collect(type));
                    }
                }

                // pick up only valid type parameter and add them to the map
                foreach (var typeParameter in typeParametersInConstraint)
                {
                    AddTypeParameterToMap(typeParameter, sortedMap);
                }
            }

            private static void AppendMethodTypeParameterUsedDirectly(MultiDictionary<ISymbol, SyntaxToken> symbolMap, IDictionary<int, ITypeParameterSymbol> sortedMap)
            {
                foreach (var typeParameter in symbolMap.Keys.OfType<ITypeParameterSymbol>())
                {
                    if (typeParameter.DeclaringMethod != null &&
                        !sortedMap.ContainsKey(typeParameter.Ordinal))
                    {
                        sortedMap[typeParameter.Ordinal] = typeParameter;
                    }
                }
            }

            private ImmutableArray<ITypeParameterSymbol> GetMethodTypeParametersInConstraintList(
                IDictionary<ISymbol, VariableInfo> variableInfoMap,
                MultiDictionary<ISymbol, SyntaxToken> symbolMap,
                SortedDictionary<int, ITypeParameterSymbol> sortedMap)
            {
                // find starting points
                AppendMethodTypeVariableFromDataFlowAnalysis(variableInfoMap, sortedMap);
                AppendMethodTypeParameterUsedDirectly(symbolMap, sortedMap);

                // recursively dive into constraints to find all constraints needed
                AppendTypeParametersInConstraintsUsedByConstructedTypeWithItsOwnConstraints(sortedMap);

                return [.. sortedMap.Values];
            }

            private static void AppendTypeParametersInConstraintsUsedByConstructedTypeWithItsOwnConstraints(SortedDictionary<int, ITypeParameterSymbol> sortedMap)
            {
                using var _1 = PooledHashSet<ITypeSymbol>.GetInstance(out var visited);
                using var _2 = PooledHashSet<ITypeParameterSymbol>.GetInstance(out var candidates);

                // collect all type parameter appears in constraint
                foreach (var typeParameter in sortedMap.Values)
                {
                    var constraintTypes = typeParameter.ConstraintTypes;
                    if (constraintTypes.IsDefaultOrEmpty)
                        continue;

                    foreach (var type in constraintTypes)
                        AddTypeParametersInConstraintsUsedByConstructedTypeWithItsOwnConstraints(type, visited, candidates);
                }

                // pick up only valid type parameter and add them to the map
                foreach (var typeParameter in candidates)
                    AddTypeParameterToMap(typeParameter, sortedMap);
            }

            private static void AddTypeParametersInConstraintsUsedByConstructedTypeWithItsOwnConstraints(
                ITypeSymbol type, HashSet<ITypeSymbol> visited, HashSet<ITypeParameterSymbol> typeParameters)
            {
                if (!visited.Add(type))
                    return;

                if (type.OriginalDefinition.Equals(type))
                    return;

                if (type is not INamedTypeSymbol constructedType)
                    return;

                var parameters = constructedType.GetAllTypeParameters();
                var arguments = constructedType.GetAllTypeArguments();

                Contract.ThrowIfFalse(parameters.Length == arguments.Length);

                for (var i = 0; i < parameters.Length; i++)
                {
                    if (arguments[i] is ITypeParameterSymbol argument)
                    {
                        if (parameters[i] is not
                            {
                                HasConstructorConstraint: false,
                                HasReferenceTypeConstraint: false,
                                HasValueTypeConstraint: false,
                                AllowsRefLikeType: false,
                                ConstraintTypes.IsDefaultOrEmpty: true
                            })
                        {
                            typeParameters.Add(argument);
                        }
                    }
                    else if (arguments[i] is INamedTypeSymbol candidate)
                    {
                        AddTypeParametersInConstraintsUsedByConstructedTypeWithItsOwnConstraints(candidate, visited, typeParameters);
                    }
                }
            }

            private static ImmutableArray<ITypeParameterSymbol> GetMethodTypeParametersInDeclaration(ITypeSymbol returnType, SortedDictionary<int, ITypeParameterSymbol> sortedMap)
            {
                // add return type to the map
                AddTypeParametersToMap(TypeParameterCollector.Collect(returnType), sortedMap);

                AppendMethodTypeParameterFromConstraint(sortedMap);

                return [.. sortedMap.Values];
            }

            private OperationStatus CheckReadOnlyFields(MultiDictionary<ISymbol, SyntaxToken> symbolMap)
            {
                if (ReadOnlyFieldAllowed())
                    return OperationStatus.SucceededStatus;

                using var _ = ArrayBuilder<string>.GetInstance(out var names);
                var semanticFacts = this.SemanticFacts;
                foreach (var pair in symbolMap.Where(p => p.Key.Kind == SymbolKind.Field))
                {
                    var field = (IFieldSymbol)pair.Key;
                    if (!field.IsReadOnly)
                        continue;

                    var tokens = pair.Value;
                    if (tokens.All(t => !semanticFacts.IsWrittenTo(this.SemanticModel, t.Parent, CancellationToken)))
                        continue;

                    names.Add(field.Name ?? string.Empty);
                }

                if (names.Count > 0)
                    return new OperationStatus(succeeded: true, string.Format(FeaturesResources.Assigning_to_readonly_fields_must_be_done_in_a_constructor_colon_bracket_0_bracket, string.Join(", ", names)));

                return OperationStatus.SucceededStatus;
            }
        }
    }
}
