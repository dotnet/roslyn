// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

                this.SelectionResult = selectionResult;
                _semanticDocument = selectionResult.SemanticDocument;
                this.CancellationToken = cancellationToken;
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
                var variableInfoMap = GenerateVariableInfoMap(model, dataFlowAnalysisData, symbolMap);

                // check whether instance member is used inside of the selection
                var instanceMemberIsUsed = IsInstanceMemberUsedInSelectedCode(dataFlowAnalysisData);

                // check whether end of selection is reachable
                var endOfSelectionReachable = IsEndOfSelectionReachable(model);

                // collects various variable informations
                // extracted code contains return value
                var isInExpressionOrHasReturnStatement = IsInExpressionOrHasReturnStatement(model);
                var signatureTuple = GetSignatureInformation(model, dataFlowAnalysisData, variableInfoMap, isInExpressionOrHasReturnStatement);

                var parameters = signatureTuple.Item1;
                var returnType = signatureTuple.Item2;
                var variableToUseAsReturnValue = signatureTuple.Item3;
                var unsafeAddressTakenUsed = signatureTuple.Item4;

                var returnTypeTuple = AdjustReturnType(model, returnType);

                returnType = returnTypeTuple.Item1;
                bool returnTypeHasAnonymousType = returnTypeTuple.Item2;
                bool awaitTaskReturn = returnTypeTuple.Item3;

                // create new document
                var newDocument = await CreateDocumentWithAnnotationsAsync(_semanticDocument, parameters, CancellationToken).ConfigureAwait(false);

                // collect method type variable used in selected code
                var sortedMap = new SortedDictionary<int, ITypeParameterSymbol>();
                var typeParametersInConstraintList = GetMethodTypeParametersInConstraintList(model, variableInfoMap, symbolMap, sortedMap);
                var typeParametersInDeclaration = GetMethodTypeParametersInDeclaration(returnType, sortedMap);

                // check various error cases
                var operationStatus = GetOperationStatus(model, symbolMap, parameters, unsafeAddressTakenUsed, returnTypeHasAnonymousType);

                return new AnalyzerResult(
                        newDocument,
                        typeParametersInDeclaration, typeParametersInConstraintList,
                        parameters, variableToUseAsReturnValue, returnType, awaitTaskReturn,
                        instanceMemberIsUsed, endOfSelectionReachable, operationStatus);
            }

            private Tuple<ITypeSymbol, bool, bool> AdjustReturnType(SemanticModel model, ITypeSymbol returnType)
            {
                // check whether return type contains anonymous type and if it does, fix it up by making it object
                var returnTypeHasAnonymousType = returnType.ContainsAnonymousType();
                returnType = returnTypeHasAnonymousType ? returnType.RemoveAnonymousTypes(model.Compilation) : returnType;

                // if selection contains await which is not under async lambda or anonymous delegate,
                // change return type to be wrapped in Task
                var shouldPutAsyncModifier = this.SelectionResult.ShouldPutAsyncModifier();
                if (shouldPutAsyncModifier)
                {
                    bool awaitTaskReturn;
                    WrapReturnTypeInTask(model, ref returnType, out awaitTaskReturn);

                    return Tuple.Create(returnType, returnTypeHasAnonymousType, awaitTaskReturn);
                }

                // unwrap task if needed
                UnwrapTaskIfNeeded(model, ref returnType);
                return Tuple.Create(returnType, returnTypeHasAnonymousType, false);
            }

            private void UnwrapTaskIfNeeded(SemanticModel model, ref ITypeSymbol returnType)
            {
                // nothing to unwrap
                if (!this.SelectionResult.ContainingScopeHasAsyncKeyword() ||
                    !this.ContainsReturnStatementInSelectedCode(model))
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

                if (this.SelectionResult.SelectionInExpression)
                {
                    returnType = genericTaskType.Construct(returnType);
                    return;
                }

                if (ContainsReturnStatementInSelectedCode(model))
                {
                    // check whether we will use return type as it is or not.
                    awaitTaskReturn = returnType.Equals(taskType);
                    return;
                }

                // okay, wrap the return type in Task<T>
                returnType = genericTaskType.Construct(returnType);
            }

            private Tuple<IList<VariableInfo>, ITypeSymbol, VariableInfo, bool> GetSignatureInformation(
                SemanticModel model,
                DataFlowAnalysis dataFlowAnalysisData,
                IDictionary<ISymbol, VariableInfo> variableInfoMap,
                bool isInExpressionOrHasReturnStatement)
            {
                if (isInExpressionOrHasReturnStatement)
                {
                    // check whether current selection contains return statement
                    var parameters = GetMethodParameters(variableInfoMap.Values);
                    var returnType = this.SelectionResult.GetContainingScopeType();
                    if (returnType == null)
                    {
                        returnType = model.Compilation.GetSpecialType(SpecialType.System_Object);
                    }

                    var unsafeAddressTakenUsed = ContainsVariableUnsafeAddressTaken(dataFlowAnalysisData, variableInfoMap.Keys);
                    return Tuple.Create(parameters, returnType, default(VariableInfo), unsafeAddressTakenUsed);
                }
                else
                {
                    // no return statement
                    var parameters = MarkVariableInfoToUseAsReturnValueIfPossible(GetMethodParameters(variableInfoMap.Values));
                    var variableToUseAsReturnValue = parameters.FirstOrDefault(v => v.UseAsReturnValue);
                    var returnType = default(ITypeSymbol);
                    if (variableToUseAsReturnValue != null)
                    {
                        returnType = variableToUseAsReturnValue.GetVariableType(_semanticDocument);
                    }
                    else
                    {
                        returnType = model.Compilation.GetSpecialType(SpecialType.System_Void);
                    }

                    var unsafeAddressTakenUsed = ContainsVariableUnsafeAddressTaken(dataFlowAnalysisData, variableInfoMap.Keys);
                    return Tuple.Create(parameters, returnType, variableToUseAsReturnValue, unsafeAddressTakenUsed);
                }
            }

            private bool IsInExpressionOrHasReturnStatement(SemanticModel model)
            {
                var isInExpressionOrHasReturnStatement = this.SelectionResult.SelectionInExpression;
                if (!isInExpressionOrHasReturnStatement)
                {
                    var containsReturnStatement = ContainsReturnStatementInSelectedCode(model);
                    isInExpressionOrHasReturnStatement |= containsReturnStatement;
                }

                return isInExpressionOrHasReturnStatement;
            }

            private OperationStatus GetOperationStatus(
                SemanticModel model, Dictionary<ISymbol, List<SyntaxToken>> symbolMap, IList<VariableInfo> parameters,
                bool unsafeAddressTakenUsed, bool returnTypeHasAnonymousType)
            {
                var readonlyFieldStatus = CheckReadOnlyFields(model, symbolMap);

                var namesWithAnonymousTypes = parameters.Where(v => v.OriginalTypeHadAnonymousTypeOrDelegate).Select(v => v.Name ?? string.Empty);
                if (returnTypeHasAnonymousType)
                {
                    namesWithAnonymousTypes = namesWithAnonymousTypes.Concat("return type");
                }

                var anonymousTypeStatus = namesWithAnonymousTypes.Any() ?
                    new OperationStatus(OperationStatusFlag.BestEffort, string.Format(FeaturesResources.ContainsAnonymousType, string.Join(", ", namesWithAnonymousTypes))) :
                    OperationStatus.Succeeded;

                var unsafeAddressStatus = unsafeAddressTakenUsed ? OperationStatus.UnsafeAddressTaken : OperationStatus.Succeeded;

                var asyncRefOutParameterStatue = CheckAsyncMethodRefOutParameters(parameters);

                return readonlyFieldStatus.With(anonymousTypeStatus).With(unsafeAddressStatus).With(asyncRefOutParameterStatue);
            }

            private OperationStatus CheckAsyncMethodRefOutParameters(IList<VariableInfo> parameters)
            {
                if (this.SelectionResult.ShouldPutAsyncModifier())
                {
                    var names = parameters.Where(v => !v.UseAsReturnValue && (v.ParameterModifier == ParameterBehavior.Out || v.ParameterModifier == ParameterBehavior.Ref))
                                          .Select(p => p.Name ?? string.Empty);

                    if (names.Any())
                    {
                        return new OperationStatus(OperationStatusFlag.BestEffort, string.Format(FeaturesResources.AsyncMethodWithRefOutParameters, string.Join(", ", names)));
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
                var context = this.SelectionResult.GetContainingScope();
                var symbolMap = SymbolMapBuilder.Build(syntaxFactsService, model, context, this.SelectionResult.FinalSpan, CancellationToken);

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
                if (this.SelectionResult.SelectionInExpression)
                {
                    return model.AnalyzeDataFlow(this.SelectionResult.GetContainingScope());
                }

                var pair = GetFlowAnalysisNodeRange();
                return model.AnalyzeDataFlow(pair.Item1, pair.Item2);
            }

            private bool IsEndOfSelectionReachable(SemanticModel model)
            {
                if (this.SelectionResult.SelectionInExpression)
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

                list.Sort(VariableInfo.Compare);

                return list;
            }

            private IDictionary<ISymbol, VariableInfo> GenerateVariableInfoMap(
                SemanticModel model, DataFlowAnalysis dataFlowAnalysisData, Dictionary<ISymbol, List<SyntaxToken>> symbolMap)
            {
                Contract.ThrowIfNull(model);
                Contract.ThrowIfNull(dataFlowAnalysisData);

                var variableInfoMap = new Dictionary<ISymbol, VariableInfo>();

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

                    var variableStyle = GetVariableStyle(symbolMap, symbol, model, type,
                        captured, dataFlowIn, dataFlowOut, alwaysAssigned, variableDeclared,
                        readInside, writtenInside, readOutside, writtenOutside, unsafeAddressTaken);

                    AddVariableToMap(variableInfoMap, symbol, CreateFromSymbol(model.Compilation, symbol, type, variableStyle, variableDeclared));
                }

                return variableInfoMap;
            }

            private void AddVariableToMap(IDictionary<ISymbol, VariableInfo> variableInfoMap, ISymbol localOrParameter, VariableInfo variableInfo)
            {
                variableInfoMap.Add(localOrParameter, variableInfo);
            }

            private VariableStyle GetVariableStyle(
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
                bool unsafeAddressTaken)
            {
                Contract.ThrowIfNull(model);
                Contract.ThrowIfNull(type);

                var style = ExtractMethodMatrix.GetVariableStyle(captured, dataFlowIn, dataFlowOut, alwaysAssigned, variableDeclared,
                                                                 readInside, writtenInside, readOutside, writtenOutside, unsafeAddressTaken);

                if (SelectionContainsOnlyIdentifierWithSameType(type))
                {
                    return style;
                }

                if (UserDefinedValueType(model.Compilation, type) && !this.SelectionResult.DontPutOutOrRefOnStruct)
                {
                    return AlwaysReturn(style);
                }

                // for captured variable, never try to move the decl into extracted method
                if (captured && (style == VariableStyle.MoveIn))
                {
                    return VariableStyle.Out;
                }

                // check special value type cases
                if (type.IsValueType && !IsWrittenInsideForFrameworkValueType(symbolMap, model, symbol, writtenInside))
                {
                    return style;
                }

                // don't blindly always return. make sure there is a write inside of the selection
                if (this.SelectionResult.AllowMovingDeclaration || !writtenInside)
                {
                    return style;
                }

                return AlwaysReturn(style);
            }

            private bool IsWrittenInsideForFrameworkValueType(
                Dictionary<ISymbol, List<SyntaxToken>> symbolMap, SemanticModel model, ISymbol symbol, bool writtenInside)
            {
                List<SyntaxToken> tokens;
                if (!symbolMap.TryGetValue(symbol, out tokens))
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
                if (!this.SelectionResult.SelectionInExpression)
                {
                    return false;
                }

                var firstToken = this.SelectionResult.GetFirstTokenInSelection();
                var lastToken = this.SelectionResult.GetLastTokenInSelection();

                if (!firstToken.Equals(lastToken))
                {
                    return false;
                }

                return type.Equals(this.SelectionResult.GetContainingScopeType());
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

                var cancellationTokenType = compilation.GetTypeByMetadataName("System.Threading.CancellationToken");
                if (cancellationTokenType != null && cancellationTokenType.Equals(type))
                {
                    return true;
                }

                return false;
            }

            private ITypeSymbol GetSymbolType(SemanticModel model, ISymbol symbol)
            {
                var local = symbol as ILocalSymbol;
                if (local != null)
                {
                    return local.Type;
                }

                var parameter = symbol as IParameterSymbol;
                if (parameter != null)
                {
                    return parameter.Type;
                }

                var rangeVariable = symbol as IRangeVariableSymbol;
                if (rangeVariable != null)
                {
                    return GetRangeVariableType(model, rangeVariable);
                }

                return Contract.FailWithReturn<ITypeSymbol>("Shouldn't reach here");
            }

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

            private bool IsParameterUsedOutside(ISymbol localOrParameter)
            {
                var parameter = localOrParameter as IParameterSymbol;
                if (parameter == null)
                {
                    return false;
                }

                return parameter.RefKind != RefKind.None;
            }

            private bool IsParameterAssigned(ISymbol localOrParameter)
            {
                // hack for now.
                var parameter = localOrParameter as IParameterSymbol;
                if (parameter == null)
                {
                    return false;
                }

                return parameter.RefKind != RefKind.Out;
            }

            private bool IsThisParameter(ISymbol localOrParameter)
            {
                var parameter = localOrParameter as IParameterSymbol;
                if (parameter == null)
                {
                    return false;
                }

                return parameter.IsThis;
            }

            private bool IsInteractiveSynthesizedParameter(ISymbol localOrParameter)
            {
                var parameter = localOrParameter as IParameterSymbol;
                if (parameter == null)
                {
                    return false;
                }

                return parameter.IsImplicitlyDeclared &&
                       parameter.ContainingAssembly.IsInteractive &&
                       parameter.ContainingSymbol != null &&
                       parameter.ContainingSymbol.ContainingType != null &&
                       parameter.ContainingSymbol.ContainingType.IsScriptClass;
            }

            private bool ContainsReturnStatementInSelectedCode(SemanticModel model)
            {
                Contract.ThrowIfTrue(this.SelectionResult.SelectionInExpression);

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
                    var parameter = symbol as IParameterSymbol;
                    if (parameter != null)
                    {
                        AddTypeParametersToMap(TypeParameterCollector.Collect(parameter.Type), sortedMap);
                        continue;
                    }

                    var local = symbol as ILocalSymbol;
                    if (local != null)
                    {
                        AddTypeParametersToMap(TypeParameterCollector.Collect(local.Type), sortedMap);
                        continue;
                    }

                    var rangeVariable = symbol as IRangeVariableSymbol;
                    if (rangeVariable != null)
                    {
                        var type = GetRangeVariableType(model, rangeVariable);
                        AddTypeParametersToMap(TypeParameterCollector.Collect(type), sortedMap);
                        continue;
                    }

                    Contract.Fail(FeaturesResources.UnknownSymbolKind);
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
                    var typeParameter = pair.Key as ITypeParameterSymbol;
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

                var constructedType = type as INamedTypeSymbol;
                if (constructedType == null)
                {
                    return SpecializedCollections.EmptyEnumerable<ITypeParameterSymbol>();
                }

                var parameters = constructedType.GetAllTypeParameters().ToList();
                var arguments = constructedType.GetAllTypeArguments().ToList();

                Contract.ThrowIfFalse(parameters.Count == arguments.Count);

                var typeParameters = new List<ITypeParameterSymbol>();
                for (int i = 0; i < parameters.Count; i++)
                {
                    var parameter = parameters[i];

                    var argument = arguments[i] as ITypeParameterSymbol;
                    if (argument != null)
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

                    var candidate = arguments[i] as INamedTypeSymbol;
                    if (candidate == null)
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

                List<string> names = null;
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

                    names = names ?? new List<string>();
                    names.Add(field.Name ?? string.Empty);
                }

                if (names != null)
                {
                    return new OperationStatus(OperationStatusFlag.BestEffort, string.Format(FeaturesResources.AssigningToReadonlyFields, string.Join(", ", names)));
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
                var local = symbol as ILocalSymbol;
                if (local != null)
                {
                    return new VariableInfo(
                        new LocalVariableSymbol<T>(compilation, local, type, nonNoisySyntaxKindSet),
                        style);
                }

                var parameter = symbol as IParameterSymbol;
                if (parameter != null)
                {
                    return new VariableInfo(new ParameterVariableSymbol(compilation, parameter, type), style);
                }

                var rangeVariable = symbol as IRangeVariableSymbol;
                if (rangeVariable != null)
                {
                    return new VariableInfo(new QueryVariableSymbol(compilation, rangeVariable, type), style);
                }

                return Contract.FailWithReturn<VariableInfo>(FeaturesResources.Unknown);
            }
        }
    }
}
