// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal abstract partial class MethodExtractor
    {
        protected abstract partial class Analyzer
        {
            private readonly SemanticDocument _semanticDocument;

            protected readonly CancellationToken CancellationToken;
            protected readonly SelectionResult SelectionResult;

            protected Analyzer(SelectionResult selectionResult, CancellationToken cancellationToken)
            {
                Contract.ThrowIfNull(selectionResult);

                SelectionResult = selectionResult;
                _semanticDocument = selectionResult.SemanticDocument;
                CancellationToken = cancellationToken;
            }

            /// <summary>
            /// convert text span to node range for the flow analysis API
            /// </summary>
            protected abstract Tuple<SyntaxNode, SyntaxNode> GetFlowAnalysisNodeRange();

            /// <summary>
            /// check whether selection contains return statement or not
            /// </summary>
            protected abstract bool ContainsReturnStatementInSelectedCode(IEnumerable<SyntaxNode> jumpOutOfRegionStatements);

            /// <summary>
            /// create VariableInfo type
            /// </summary>
            protected abstract VariableInfo CreateFromSymbol(Compilation compilation, ISymbol symbol, ITypeSymbol type, VariableStyle variableStyle, bool variableDeclared);

            /// <summary>
            /// among variables that will be used as parameters at the extracted method, check whether one of the parameter can be used as return
            /// </summary>
            protected abstract int GetIndexOfVariableInfoToUseAsReturnValue(IList<VariableInfo> variableInfo);

            /// <summary>
            /// get type of the range variable symbol
            /// </summary>
            protected abstract ITypeSymbol GetRangeVariableType(SemanticModel model, IRangeVariableSymbol symbol);

            /// <summary>
            /// check whether the selection is at the placed where read-only field is allowed to be extracted out
            /// </summary>
            /// <returns></returns>
            protected abstract bool ReadOnlyFieldAllowed();

            public async Task<AnalyzerResult> AnalyzeAsync()
            {
                // do data flow analysis
                var model = _semanticDocument.SemanticModel;
                var dataFlowAnalysisData = GetDataFlowAnalysisData(model);

                // build symbol map for the identifiers used inside of the selection
                var symbolMap = GetSymbolMap(model);

                // gather initial local or parameter variable info
                GenerateVariableInfoMap(
                    bestEffort: false, model, dataFlowAnalysisData, symbolMap,
                    out var variableInfoMap, out var failedVariables);
                if (failedVariables.Count > 0)
                {
                    // If we weren't able to figure something out, go back and regenerate the map
                    // this time in 'best effort' mode.  We'll give the user a message saying there
                    // was a problem, but we allow them to proceed so they're not unnecessarily
                    // blocked just because we didn't understand something.
                    GenerateVariableInfoMap(
                        bestEffort: true, model, dataFlowAnalysisData, symbolMap,
                        out variableInfoMap, out var unused);
                    Contract.ThrowIfFalse(unused.Count == 0);
                }

                // check whether instance member is used inside of the selection
                var instanceMemberIsUsed = IsInstanceMemberUsedInSelectedCode(dataFlowAnalysisData);

                // check whether end of selection is reachable
                var endOfSelectionReachable = IsEndOfSelectionReachable(model);

                // collects various variable informations
                // extracted code contains return value
                var isInExpressionOrHasReturnStatement = IsInExpressionOrHasReturnStatement(model);
                var (parameters, returnType, variableToUseAsReturnValue, unsafeAddressTakenUsed) =
                    GetSignatureInformation(dataFlowAnalysisData, variableInfoMap, isInExpressionOrHasReturnStatement);

                var returnTypeTuple = AdjustReturnType(model, returnType);

                returnType = returnTypeTuple.typeSymbol;
                var returnTypeHasAnonymousType = returnTypeTuple.hasAnonymouseType;
                var awaitTaskReturn = returnTypeTuple.awaitTaskReturn;

                // create new document
                var newDocument = await CreateDocumentWithAnnotationsAsync(_semanticDocument, parameters, CancellationToken).ConfigureAwait(false);

                // collect method type variable used in selected code
                var sortedMap = new SortedDictionary<int, ITypeParameterSymbol>();
                var typeParametersInConstraintList = GetMethodTypeParametersInConstraintList(model, variableInfoMap, symbolMap, sortedMap);
                var typeParametersInDeclaration = GetMethodTypeParametersInDeclaration(returnType, sortedMap);

                // check various error cases
                var operationStatus = GetOperationStatus(
                    model, symbolMap, parameters, failedVariables, unsafeAddressTakenUsed, returnTypeHasAnonymousType);

                return new AnalyzerResult(
                    newDocument,
                    typeParametersInDeclaration, typeParametersInConstraintList,
                    parameters, variableToUseAsReturnValue, returnType, awaitTaskReturn,
                    instanceMemberIsUsed, endOfSelectionReachable, operationStatus);
            }

            private (ITypeSymbol typeSymbol, bool hasAnonymouseType, bool awaitTaskReturn) AdjustReturnType(SemanticModel model, ITypeSymbol returnType)
            {
                // check whether return type contains anonymous type and if it does, fix it up by making it object
                var returnTypeHasAnonymousType = returnType.ContainsAnonymousType();
                returnType = returnTypeHasAnonymousType ? returnType.RemoveAnonymousTypes(model.Compilation) : returnType;

                // if selection contains await which is not under async lambda or anonymous delegate,
                // change return type to be wrapped in Task
                var shouldPutAsyncModifier = SelectionResult.ShouldPutAsyncModifier();
                if (shouldPutAsyncModifier)
                {
                    WrapReturnTypeInTask(model, ref returnType, out var awaitTaskReturn);

                    return (returnType, returnTypeHasAnonymousType, awaitTaskReturn);
                }

                // unwrap task if needed
                UnwrapTaskIfNeeded(model, ref returnType);
                return (returnType, returnTypeHasAnonymousType, false);
            }

            private void UnwrapTaskIfNeeded(SemanticModel model, ref ITypeSymbol returnType)
            {
                // nothing to unwrap
                if (!SelectionResult.ContainingScopeHasAsyncKeyword() ||
                    !ContainsReturnStatementInSelectedCode(model))
                {
                    return;
                }

                var originalDefinition = returnType.OriginalDefinition;

                // see whether it needs to be unwrapped
                var taskType = model.Compilation.TaskType();
                if (originalDefinition.Equals(taskType))
                {
                    returnType = model.Compilation.GetSpecialType(SpecialType.System_Void);
                    return;
                }

                var genericTaskType = model.Compilation.TaskOfTType();
                if (originalDefinition.Equals(genericTaskType))
                {
                    returnType = ((INamedTypeSymbol)returnType).TypeArguments[0];
                    return;
                }

                // nothing to unwrap
                return;
            }

            private void WrapReturnTypeInTask(SemanticModel model, ref ITypeSymbol returnType, out bool awaitTaskReturn)
            {
                awaitTaskReturn = false;

                var genericTaskType = model.Compilation.TaskOfTType();
                var taskType = model.Compilation.TaskType();

                if (returnType.Equals(model.Compilation.GetSpecialType(SpecialType.System_Void)))
                {
                    // convert void to Task type
                    awaitTaskReturn = true;
                    returnType = taskType;
                    return;
                }

                if (SelectionResult.SelectionInExpression)
                {
                    returnType = genericTaskType.ConstructWithNullability(returnType);
                    return;
                }

                if (ContainsReturnStatementInSelectedCode(model))
                {
                    // check whether we will use return type as it is or not.
                    awaitTaskReturn = returnType.Equals(taskType);
                    return;
                }

                // okay, wrap the return type in Task<T>
                returnType = genericTaskType.ConstructWithNullability(returnType);
            }

            private (IList<VariableInfo> parameters, ITypeSymbol returnType, VariableInfo? variableToUseAsReturnValue, bool unsafeAddressTakenUsed)
                GetSignatureInformation(
                    DataFlowAnalysis dataFlowAnalysisData,
                    IDictionary<ISymbol, VariableInfo> variableInfoMap,
                    bool isInExpressionOrHasReturnStatement)
            {


                var model = _semanticDocument.SemanticModel;
                var compilation = model.Compilation;
                if (isInExpressionOrHasReturnStatement)
                {
                    // check whether current selection contains return statement
                    var parameters = GetMethodParameters(variableInfoMap.Values);
                    var returnType = GetReturnTypeFromStatement(model);

                    var unsafeAddressTakenUsed = ContainsVariableUnsafeAddressTaken(dataFlowAnalysisData, variableInfoMap.Keys);
                    return (parameters, returnType, default(VariableInfo), unsafeAddressTakenUsed);
                }
                else
                {
                    // no return statement
                    var parameters = MarkVariableInfoToUseAsReturnValueIfPossible(GetMethodParameters(variableInfoMap.Values));
                    var variableToUseAsReturnValue = parameters.FirstOrDefault(v => v.UseAsReturnValue);
                    var returnType = variableToUseAsReturnValue != null
                        ? GetReturnTypeFromReturnVariable(_semanticDocument, variableToUseAsReturnValue)
                        : compilation.GetSpecialType(SpecialType.System_Void);

                    var unsafeAddressTakenUsed = ContainsVariableUnsafeAddressTaken(dataFlowAnalysisData, variableInfoMap.Keys);
                    return (parameters, returnType, variableToUseAsReturnValue, unsafeAddressTakenUsed);
                }
            }

            protected virtual ITypeSymbol GetReturnTypeFromStatement(SemanticModel semanticModel)
            {
                var compilation = semanticModel.Compilation;
                return SelectionResult.GetContainingScopeType() ?? compilation.GetSpecialType(SpecialType.System_Object);
            }

            protected virtual ITypeSymbol GetReturnTypeFromReturnVariable(SemanticDocument semanticDocument, VariableInfo variableToUseAsReturnValue)
            => variableToUseAsReturnValue.GetVariableType(semanticDocument);

            private bool IsInExpressionOrHasReturnStatement(SemanticModel model)
            {
                var isInExpressionOrHasReturnStatement = SelectionResult.SelectionInExpression;
                if (!isInExpressionOrHasReturnStatement)
                {
                    var containsReturnStatement = ContainsReturnStatementInSelectedCode(model);
                    isInExpressionOrHasReturnStatement |= containsReturnStatement;
                }

                return isInExpressionOrHasReturnStatement;
            }

            private OperationStatus GetOperationStatus(
                SemanticModel model, Dictionary<ISymbol, List<SyntaxToken>> symbolMap,
                IList<VariableInfo> parameters, IList<ISymbol> failedVariables,
                bool unsafeAddressTakenUsed, bool returnTypeHasAnonymousType)
            {
                var readonlyFieldStatus = CheckReadOnlyFields(model, symbolMap);

                var namesWithAnonymousTypes = parameters.Where(v => v.OriginalTypeHadAnonymousTypeOrDelegate).Select(v => v.Name ?? string.Empty);
                if (returnTypeHasAnonymousType)
                {
                    namesWithAnonymousTypes = namesWithAnonymousTypes.Concat("return type");
                }

                var anonymousTypeStatus = !namesWithAnonymousTypes.Any()
                    ? OperationStatus.Succeeded
                    : new OperationStatus(OperationStatusFlag.BestEffort,
                        string.Format(
                            FeaturesResources.Parameters_type_or_return_type_cannot_be_an_anonymous_type_colon_bracket_0_bracket,
                            string.Join(", ", namesWithAnonymousTypes)));

                var unsafeAddressStatus = unsafeAddressTakenUsed
                    ? OperationStatus.UnsafeAddressTaken
                    : OperationStatus.Succeeded;

                var asyncRefOutParameterStatus = CheckAsyncMethodRefOutParameters(parameters);

                var variableMapStatus = failedVariables.Count == 0
                    ? OperationStatus.Succeeded
                    : new OperationStatus(OperationStatusFlag.BestEffort,
                        string.Format(
                            FeaturesResources.Failed_to_analyze_data_flow_for_0,
                            string.Join(", ", failedVariables.Select(v => v.Name))));

                return readonlyFieldStatus.With(anonymousTypeStatus)
                                          .With(unsafeAddressStatus)
                                          .With(asyncRefOutParameterStatus)
                                          .With(variableMapStatus);
            }

            private OperationStatus CheckAsyncMethodRefOutParameters(IList<VariableInfo> parameters)
            {
                if (SelectionResult.ShouldPutAsyncModifier())
                {
                    var names = parameters.Where(v => !v.UseAsReturnValue && (v.ParameterModifier == ParameterBehavior.Out || v.ParameterModifier == ParameterBehavior.Ref))
                                          .Select(p => p.Name ?? string.Empty);

                    if (names.Any())
                    {
                        return new OperationStatus(OperationStatusFlag.BestEffort, string.Format(FeaturesResources.Asynchronous_method_cannot_have_ref_out_parameters_colon_bracket_0_bracket, string.Join(", ", names)));
                    }
                }

                return OperationStatus.Succeeded;
            }

            private Task<SemanticDocument> CreateDocumentWithAnnotationsAsync(SemanticDocument document, IList<VariableInfo> variables, CancellationToken cancellationToken)
            {
                var annotations = new List<Tuple<SyntaxToken, SyntaxAnnotation>>(variables.Count);
                variables.Do(v => v.AddIdentifierTokenAnnotationPair(annotations, cancellationToken));

                if (annotations.Count == 0)
                {
                    return Task.FromResult(document);
                }

                return document.WithSyntaxRootAsync(document.Root.AddAnnotations(annotations), cancellationToken);
            }

            private Dictionary<ISymbol, List<SyntaxToken>> GetSymbolMap(SemanticModel model)
            {
                var syntaxFactsService = _semanticDocument.Document.Project.LanguageServices.GetService<ISyntaxFactsService>();
                var context = SelectionResult.GetContainingScope();
                var symbolMap = SymbolMapBuilder.Build(syntaxFactsService, model, context, SelectionResult.FinalSpan, CancellationToken);
                return symbolMap;
            }

            private bool ContainsVariableUnsafeAddressTaken(DataFlowAnalysis dataFlowAnalysisData, IEnumerable<ISymbol> symbols)
            {
                // check whether the selection contains "&" over a symbol exist
                var map = new HashSet<ISymbol>(dataFlowAnalysisData.UnsafeAddressTaken);
                return symbols.Any(s => map.Contains(s));
            }

            private DataFlowAnalysis GetDataFlowAnalysisData(SemanticModel model)
            {
                if (SelectionResult.SelectionInExpression)
                {
                    return model.AnalyzeDataFlow(SelectionResult.GetContainingScope());
                }

                var pair = GetFlowAnalysisNodeRange();
                return model.AnalyzeDataFlow(pair.Item1, pair.Item2);
            }

            private bool IsEndOfSelectionReachable(SemanticModel model)
            {
                if (SelectionResult.SelectionInExpression)
                {
                    return true;
                }

                var pair = GetFlowAnalysisNodeRange();
                var analysis = model.AnalyzeControlFlow(pair.Item1, pair.Item2);
                return analysis.EndPointIsReachable;
            }

            private IList<VariableInfo> MarkVariableInfoToUseAsReturnValueIfPossible(IList<VariableInfo> variableInfo)
            {
                var variableToUseAsReturnValueIndex = GetIndexOfVariableInfoToUseAsReturnValue(variableInfo);
                if (variableToUseAsReturnValueIndex >= 0)
                {
                    variableInfo[variableToUseAsReturnValueIndex] = VariableInfo.CreateReturnValue(variableInfo[variableToUseAsReturnValueIndex]);
                }

                return variableInfo;
            }

            private IList<VariableInfo> GetMethodParameters(ICollection<VariableInfo> variableInfo)
            {
                var list = new List<VariableInfo>(variableInfo);
                VariableInfo.SortVariables(_semanticDocument.SemanticModel.Compilation, list);
                return list;
            }

            /// <param name="bestEffort">When false, variables whose data flow is not understood
            /// will be returned in <paramref name="failedVariables"/>. When true, we assume any
            /// variable we don't understand has <see cref="VariableStyle.None"/></param>
            private void GenerateVariableInfoMap(
                bool bestEffort,
                SemanticModel model,
                DataFlowAnalysis dataFlowAnalysisData,
                Dictionary<ISymbol, List<SyntaxToken>> symbolMap,
                out IDictionary<ISymbol, VariableInfo> variableInfoMap,
                out List<ISymbol> failedVariables)
            {
                Contract.ThrowIfNull(model);
                Contract.ThrowIfNull(dataFlowAnalysisData);

                variableInfoMap = new Dictionary<ISymbol, VariableInfo>();
                failedVariables = new List<ISymbol>();

                // create map of each data
                var capturedMap = new HashSet<ISymbol>(dataFlowAnalysisData.Captured);
                var dataFlowInMap = new HashSet<ISymbol>(dataFlowAnalysisData.DataFlowsIn);
                var dataFlowOutMap = new HashSet<ISymbol>(dataFlowAnalysisData.DataFlowsOut);
                var alwaysAssignedMap = new HashSet<ISymbol>(dataFlowAnalysisData.AlwaysAssigned);
                var variableDeclaredMap = new HashSet<ISymbol>(dataFlowAnalysisData.VariablesDeclared);
                var readInsideMap = new HashSet<ISymbol>(dataFlowAnalysisData.ReadInside);
                var writtenInsideMap = new HashSet<ISymbol>(dataFlowAnalysisData.WrittenInside);
                var readOutsideMap = new HashSet<ISymbol>(dataFlowAnalysisData.ReadOutside);
                var writtenOutsideMap = new HashSet<ISymbol>(dataFlowAnalysisData.WrittenOutside);
                var unsafeAddressTakenMap = new HashSet<ISymbol>(dataFlowAnalysisData.UnsafeAddressTaken);

                // gather all meaningful symbols for the span.
                var candidates = new HashSet<ISymbol>(readInsideMap);
                candidates.UnionWith(writtenInsideMap);
                candidates.UnionWith(variableDeclaredMap);

                foreach (var symbol in candidates)
                {
                    if (IsThisParameter(symbol) ||
                        IsInteractiveSynthesizedParameter(symbol))
                    {
                        continue;
                    }

                    var captured = capturedMap.Contains(symbol);
                    var dataFlowIn = dataFlowInMap.Contains(symbol);
                    var dataFlowOut = dataFlowOutMap.Contains(symbol);
                    var alwaysAssigned = alwaysAssignedMap.Contains(symbol);
                    var variableDeclared = variableDeclaredMap.Contains(symbol);
                    var readInside = readInsideMap.Contains(symbol);
                    var writtenInside = writtenInsideMap.Contains(symbol);
                    var readOutside = readOutsideMap.Contains(symbol);
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
                    {
                        continue;
                    }

                    // parameter defined inside of the selection (such as lambda parameter) will be ignored (bug # 10964)
                    if (symbol is IParameterSymbol && variableDeclared)
                    {
                        continue;
                    }

                    var type = GetSymbolType(model, symbol);
                    if (type == null)
                    {
                        continue;
                    }

                    if (!TryGetVariableStyle(
                            bestEffort, symbolMap, symbol, model, type,
                            captured, dataFlowIn, dataFlowOut, alwaysAssigned, variableDeclared,
                            readInside, writtenInside, readOutside, writtenOutside, unsafeAddressTaken,
                            out var variableStyle))
                    {
                        Contract.ThrowIfTrue(bestEffort, "Should never fail if bestEffort is true");
                        failedVariables.Add(symbol);
                        continue;
                    }

                    AddVariableToMap(variableInfoMap, symbol, CreateFromSymbol(model.Compilation, symbol, type, variableStyle, variableDeclared));
                }
            }

            private void AddVariableToMap(IDictionary<ISymbol, VariableInfo> variableInfoMap, ISymbol localOrParameter, VariableInfo variableInfo)
            {
                variableInfoMap.Add(localOrParameter, variableInfo);
            }

            private bool TryGetVariableStyle(
                bool bestEffort,
                Dictionary<ISymbol, List<SyntaxToken>> symbolMap,
                ISymbol symbol,
                SemanticModel model,
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
                Contract.ThrowIfNull(model);
                Contract.ThrowIfNull(type);

                if (!ExtractMethodMatrix.TryGetVariableStyle(
                        bestEffort, captured, dataFlowIn, dataFlowOut, alwaysAssigned, variableDeclared,
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

                if (UserDefinedValueType(model.Compilation, type) && !SelectionResult.DontPutOutOrRefOnStruct)
                {
                    variableStyle = AlwaysReturn(variableStyle);
                    return true;
                }

                // for captured variable, never try to move the decl into extracted method
                if (captured && variableStyle == VariableStyle.MoveIn)
                {
                    variableStyle = VariableStyle.Out;
                    return true;
                }

                // check special value type cases
                if (type.IsValueType && !IsWrittenInsideForFrameworkValueType(symbolMap, model, symbol, writtenInside))
                {
                    return true;
                }

                // don't blindly always return. make sure there is a write inside of the selection
                if (SelectionResult.AllowMovingDeclaration || !writtenInside)
                {
                    return true;
                }

                variableStyle = AlwaysReturn(variableStyle);
                return true;
            }

            private bool IsWrittenInsideForFrameworkValueType(
                Dictionary<ISymbol, List<SyntaxToken>> symbolMap, SemanticModel model, ISymbol symbol, bool writtenInside)
            {
                if (!symbolMap.TryGetValue(symbol, out var tokens))
                {
                    return writtenInside;
                }

                // this relies on the fact that our IsWrittenTo only cares about syntax to figure out whether
                // something is written to or not. but not semantic. 
                // we probably need to move the API to syntaxFact service not semanticFact.
                //
                // if one wants to get result that also considers semantic, he should use data control flow analysis API.
                var semanticFacts = _semanticDocument.Document.GetLanguageService<ISemanticFactsService>();
                return tokens.Any(t => semanticFacts.IsWrittenTo(model, t.Parent, CancellationToken.None));
            }

            private bool SelectionContainsOnlyIdentifierWithSameType(ITypeSymbol type)
            {
                if (!SelectionResult.SelectionInExpression)
                {
                    return false;
                }

                var firstToken = SelectionResult.GetFirstTokenInSelection();
                var lastToken = SelectionResult.GetLastTokenInSelection();

                if (!firstToken.Equals(lastToken))
                {
                    return false;
                }

                return type.Equals(SelectionResult.GetContainingScopeType());
            }

            private bool UserDefinedValueType(Compilation compilation, ITypeSymbol type)
            {
                if (!type.IsValueType || type.IsPointerType() || type.IsEnumType())
                {
                    return false;
                }

                return type.OriginalDefinition.SpecialType == SpecialType.None && !WellKnownFrameworkValueType(compilation, type);
            }

            private bool WellKnownFrameworkValueType(Compilation compilation, ITypeSymbol type)
            {
                if (!type.IsValueType)
                {
                    return false;
                }

                var cancellationTokenType = compilation.GetTypeByMetadataName(typeof(CancellationToken).FullName);
                if (cancellationTokenType != null && cancellationTokenType.Equals(type))
                {
                    return true;
                }

                return false;
            }

            protected virtual ITypeSymbol GetSymbolType(SemanticModel model, ISymbol symbol)
            => symbol switch
            {
                ILocalSymbol local => local.GetTypeWithAnnotatedNullability(),
                IParameterSymbol parameter => parameter.GetTypeWithAnnotatedNullability(),
                IRangeVariableSymbol rangeVariable => GetRangeVariableType(model, rangeVariable),
                _ => Contract.FailWithReturn<ITypeSymbol>("Shouldn't reach here"),
            };

            protected VariableStyle AlwaysReturn(VariableStyle style)
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

            private bool IsThisParameter(ISymbol localOrParameter)
            {
                if (localOrParameter is IParameterSymbol parameter)
                {
                    return parameter.IsThis;
                }

                return false;
            }

            private bool IsInteractiveSynthesizedParameter(ISymbol localOrParameter)
            {
                if (localOrParameter is IParameterSymbol parameter)
                {
                    return parameter.IsImplicitlyDeclared &&
                       parameter.ContainingAssembly.IsInteractive &&
                       parameter.ContainingSymbol != null &&
                       parameter.ContainingSymbol.ContainingType != null &&
                       parameter.ContainingSymbol.ContainingType.IsScriptClass;
                }

                return false;
            }

            private bool ContainsReturnStatementInSelectedCode(SemanticModel model)
            {
                Contract.ThrowIfTrue(SelectionResult.SelectionInExpression);

                var pair = GetFlowAnalysisNodeRange();
                var controlFlowAnalysisData = model.AnalyzeControlFlow(pair.Item1, pair.Item2);

                return ContainsReturnStatementInSelectedCode(controlFlowAnalysisData.ExitPoints);
            }

            private void AddTypeParametersToMap(IEnumerable<ITypeParameterSymbol> typeParameters, IDictionary<int, ITypeParameterSymbol> sortedMap)
            {
                foreach (var typeParameter in typeParameters)
                {
                    AddTypeParameterToMap(typeParameter, sortedMap);
                }
            }

            private void AddTypeParameterToMap(ITypeParameterSymbol typeParameter, IDictionary<int, ITypeParameterSymbol> sortedMap)
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
                SemanticModel model,
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
                            {
                                var type = GetRangeVariableType(model, rangeVariable);
                                AddTypeParametersToMap(TypeParameterCollector.Collect(type), sortedMap);
                                continue;
                            }
                    }

                    Contract.Fail(FeaturesResources.Unknown_symbol_kind);
                }
            }

            private void AppendMethodTypeParameterFromConstraint(SortedDictionary<int, ITypeParameterSymbol> sortedMap)
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

            private void AppendMethodTypeParameterUsedDirectly(IDictionary<ISymbol, List<SyntaxToken>> symbolMap, IDictionary<int, ITypeParameterSymbol> sortedMap)
            {
                foreach (var pair in symbolMap.Where(p => p.Key.Kind == SymbolKind.TypeParameter))
                {
                    var typeParameter = (ITypeParameterSymbol)pair.Key;
                    if (typeParameter.DeclaringMethod == null ||
                        sortedMap.ContainsKey(typeParameter.Ordinal))
                    {
                        continue;
                    }

                    sortedMap[typeParameter.Ordinal] = typeParameter;
                }
            }

            private IEnumerable<ITypeParameterSymbol> GetMethodTypeParametersInConstraintList(
                SemanticModel model,
                IDictionary<ISymbol, VariableInfo> variableInfoMap,
                IDictionary<ISymbol, List<SyntaxToken>> symbolMap,
                SortedDictionary<int, ITypeParameterSymbol> sortedMap)
            {
                // find starting points
                AppendMethodTypeVariableFromDataFlowAnalysis(model, variableInfoMap, sortedMap);
                AppendMethodTypeParameterUsedDirectly(symbolMap, sortedMap);

                // recursively dive into constraints to find all constraints needed
                AppendTypeParametersInConstraintsUsedByConstructedTypeWithItsOwnConstraints(sortedMap);

                return sortedMap.Values.ToList();
            }

            private void AppendTypeParametersInConstraintsUsedByConstructedTypeWithItsOwnConstraints(SortedDictionary<int, ITypeParameterSymbol> sortedMap)
            {
                var visited = new HashSet<ITypeSymbol>();
                var candidates = SpecializedCollections.EmptyEnumerable<ITypeParameterSymbol>();

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
                        candidates = candidates.Concat(AppendTypeParametersInConstraintsUsedByConstructedTypeWithItsOwnConstraints(type, visited));
                    }
                }

                // pick up only valid type parameter and add them to the map
                foreach (var typeParameter in candidates)
                {
                    AddTypeParameterToMap(typeParameter, sortedMap);
                }
            }

            private IEnumerable<ITypeParameterSymbol> AppendTypeParametersInConstraintsUsedByConstructedTypeWithItsOwnConstraints(
                ITypeSymbol type, HashSet<ITypeSymbol> visited)
            {
                if (visited.Contains(type))
                {
                    return SpecializedCollections.EmptyEnumerable<ITypeParameterSymbol>();
                }

                visited.Add(type);

                if (type.OriginalDefinition.Equals(type))
                {
                    return SpecializedCollections.EmptyEnumerable<ITypeParameterSymbol>();
                }

                if (!(type is INamedTypeSymbol constructedType))
                {
                    return SpecializedCollections.EmptyEnumerable<ITypeParameterSymbol>();
                }

                var parameters = constructedType.GetAllTypeParameters().ToList();
                var arguments = constructedType.GetAllTypeArguments().ToList();

                Contract.ThrowIfFalse(parameters.Count == arguments.Count);

                var typeParameters = new List<ITypeParameterSymbol>();
                for (var i = 0; i < parameters.Count; i++)
                {
                    var parameter = parameters[i];

                    if (arguments[i] is ITypeParameterSymbol argument)
                    {
                        // no constraint, nothing to do
                        if (!parameter.HasConstructorConstraint &&
                            !parameter.HasReferenceTypeConstraint &&
                            !parameter.HasValueTypeConstraint &&
                            parameter.ConstraintTypes.IsDefaultOrEmpty)
                        {
                            continue;
                        }

                        typeParameters.Add(argument);
                        continue;
                    }

                    if (!(arguments[i] is INamedTypeSymbol candidate))
                    {
                        continue;
                    }

                    typeParameters.AddRange(AppendTypeParametersInConstraintsUsedByConstructedTypeWithItsOwnConstraints(candidate, visited));
                }

                return typeParameters;
            }

            private IEnumerable<ITypeParameterSymbol> GetMethodTypeParametersInDeclaration(ITypeSymbol returnType, SortedDictionary<int, ITypeParameterSymbol> sortedMap)
            {
                // add return type to the map
                AddTypeParametersToMap(TypeParameterCollector.Collect(returnType), sortedMap);

                AppendMethodTypeParameterFromConstraint(sortedMap);

                return sortedMap.Values.ToList();
            }

            private OperationStatus CheckReadOnlyFields(SemanticModel semanticModel, Dictionary<ISymbol, List<SyntaxToken>> symbolMap)
            {
                if (ReadOnlyFieldAllowed())
                {
                    return OperationStatus.Succeeded;
                }

                List<string>? names = null;
                var semanticFacts = _semanticDocument.Document.GetLanguageService<ISemanticFactsService>();
                foreach (var pair in symbolMap.Where(p => p.Key.Kind == SymbolKind.Field))
                {
                    var field = (IFieldSymbol)pair.Key;
                    if (!field.IsReadOnly)
                    {
                        continue;
                    }

                    var tokens = pair.Value;
                    if (tokens.All(t => !semanticFacts.IsWrittenTo(semanticModel, t.Parent, CancellationToken)))
                    {
                        continue;
                    }

                    names ??= new List<string>();
                    names.Add(field.Name ?? string.Empty);
                }

                if (names != null)
                {
                    return new OperationStatus(OperationStatusFlag.BestEffort, string.Format(FeaturesResources.Assigning_to_readonly_fields_must_be_done_in_a_constructor_colon_bracket_0_bracket, string.Join(", ", names)));
                }

                return OperationStatus.Succeeded;
            }

            private bool IsInstanceMemberUsedInSelectedCode(DataFlowAnalysis dataFlowAnalysisData)
            {
                Contract.ThrowIfNull(dataFlowAnalysisData);

                // "this" can be used as a lvalue in a struct, check WrittenInside as well
                return dataFlowAnalysisData.ReadInside.Any(s => IsThisParameter(s)) ||
                       dataFlowAnalysisData.WrittenInside.Any(s => IsThisParameter(s));
            }

            protected VariableInfo CreateFromSymbolCommon<T>(
                Compilation compilation,
                ISymbol symbol,
                ITypeSymbol type,
                VariableStyle style,
                HashSet<int> nonNoisySyntaxKindSet) where T : SyntaxNode
            {
                return symbol switch
                {
                    ILocalSymbol local => new VariableInfo(
                           new LocalVariableSymbol<T>(compilation, local, type, nonNoisySyntaxKindSet),
                           style),
                    IParameterSymbol parameter => new VariableInfo(new ParameterVariableSymbol(compilation, parameter, type), style),
                    IRangeVariableSymbol rangeVariable => new VariableInfo(new QueryVariableSymbol(compilation, rangeVariable, type), style),

                    _ => Contract.FailWithReturn<VariableInfo>(FeaturesResources.Unknown),
                };
            }
        }
    }
}
