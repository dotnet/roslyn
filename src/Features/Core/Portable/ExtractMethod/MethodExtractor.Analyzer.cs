// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod;

internal abstract partial class MethodExtractor<TSelectionResult, TStatementSyntax, TExpressionSyntax>
{
    protected abstract partial class Analyzer
    {
        private readonly SemanticDocument _semanticDocument;

        protected readonly CancellationToken CancellationToken;
        protected readonly TSelectionResult SelectionResult;
        protected readonly bool LocalFunction;

        protected Analyzer(TSelectionResult selectionResult, bool localFunction, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(selectionResult);

            SelectionResult = selectionResult;
            _semanticDocument = selectionResult.SemanticDocument;
            CancellationToken = cancellationToken;
            LocalFunction = localFunction;
        }

        /// <summary>
        /// convert text span to node range for the flow analysis API
        /// </summary>
        private (TStatementSyntax, TStatementSyntax) GetFlowAnalysisNodeRange()
        {
            var first = this.SelectionResult.GetFirstStatement();
            var last = this.SelectionResult.GetLastStatement();

            // single statement case
            if (first == last ||
                first.Span.Contains(last.Span))
            {
                return (first, first);
            }

            // multiple statement case
            var firstUnderContainer = this.SelectionResult.GetFirstStatementUnderContainer();
            var lastUnderContainer = this.SelectionResult.GetLastStatementUnderContainer();
            return (firstUnderContainer, lastUnderContainer);
        }

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
        private int GetIndexOfVariableInfoToUseAsReturnValue(IList<VariableInfo> variableInfo)
        {
            var numberOfOutParameters = 0;
            var numberOfRefParameters = 0;

            var outSymbolIndex = -1;
            var refSymbolIndex = -1;

            for (var i = 0; i < variableInfo.Count; i++)
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

        protected abstract bool TreatOutAsRef { get; }

        /// <summary>
        /// get type of the range variable symbol
        /// </summary>
        protected abstract ITypeSymbol GetRangeVariableType(SemanticModel model, IRangeVariableSymbol symbol);

        /// <summary>
        /// check whether the selection is at the placed where read-only field is allowed to be extracted out
        /// </summary>
        /// <returns></returns>
        protected abstract bool ReadOnlyFieldAllowed();

        public AnalyzerResult Analyze()
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

            var thisParameterBeingRead = (IParameterSymbol?)dataFlowAnalysisData.ReadInside.FirstOrDefault(IsThisParameter);
            var isThisParameterWritten = dataFlowAnalysisData.WrittenInside.Any(static s => IsThisParameter(s));

            var localFunctionCallsNotWithinSpan = symbolMap.Keys.Where(s => s.IsLocalFunction() && !s.Locations.Any(static (l, self) => self.SelectionResult.FinalSpan.Contains(l.SourceSpan), this));

            // Checks to see if selection includes a local function call + if the given local function declaration is not included in the selection.
            var containsAnyLocalFunctionCallNotWithinSpan = localFunctionCallsNotWithinSpan.Any();
            // Checks to see if selection includes a non-static local function call + if the given local function declaration is not included in the selection.
            var containsNonStaticLocalFunctionCallNotWithinSpan = containsAnyLocalFunctionCallNotWithinSpan && localFunctionCallsNotWithinSpan.Where(s => !s.IsStatic).Any();

            var instanceMemberIsUsed = thisParameterBeingRead != null || isThisParameterWritten || containsNonStaticLocalFunctionCallNotWithinSpan;
            var shouldBeReadOnly = !isThisParameterWritten
                && thisParameterBeingRead != null
                && thisParameterBeingRead.Type is { TypeKind: TypeKind.Struct, IsReadOnly: false };

            // check whether end of selection is reachable
            var endOfSelectionReachable = IsEndOfSelectionReachable(model);

            // collects various variable informations
            // extracted code contains return value
            var isInExpressionOrHasReturnStatement = IsInExpressionOrHasReturnStatement(model);
            var (parameters, returnType, returnsByRef, variableToUseAsReturnValue, unsafeAddressTakenUsed) =
                GetSignatureInformation(dataFlowAnalysisData, variableInfoMap, isInExpressionOrHasReturnStatement);

            var returnTypeTuple = AdjustReturnType(model, returnType);

            returnType = returnTypeTuple.typeSymbol;
            var returnTypeHasAnonymousType = returnTypeTuple.hasAnonymousType;
            var awaitTaskReturn = returnTypeTuple.awaitTaskReturn;

            // collect method type variable used in selected code
            var sortedMap = new SortedDictionary<int, ITypeParameterSymbol>();
            var typeParametersInConstraintList = GetMethodTypeParametersInConstraintList(model, variableInfoMap, symbolMap, sortedMap);
            var typeParametersInDeclaration = GetMethodTypeParametersInDeclaration(returnType, sortedMap);

            // check various error cases
            var operationStatus = GetOperationStatus(
                model, symbolMap, parameters, failedVariables, unsafeAddressTakenUsed, returnTypeHasAnonymousType, containsAnyLocalFunctionCallNotWithinSpan);

            return new AnalyzerResult(
                typeParametersInDeclaration,
                typeParametersInConstraintList,
                parameters,
                variableToUseAsReturnValue,
                returnType,
                returnsByRef,
                awaitTaskReturn,
                instanceMemberIsUsed,
                shouldBeReadOnly,
                endOfSelectionReachable,
                operationStatus);
        }

        private (ITypeSymbol typeSymbol, bool hasAnonymousType, bool awaitTaskReturn) AdjustReturnType(SemanticModel model, ITypeSymbol returnType)
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

            var taskType = model.Compilation.TaskType();

            if (taskType is object && returnType.Equals(model.Compilation.GetSpecialType(SpecialType.System_Void)))
            {
                // convert void to Task type
                awaitTaskReturn = true;
                returnType = taskType;
                return;
            }

            if (!SelectionResult.SelectionInExpression && ContainsReturnStatementInSelectedCode(model))
            {
                // check whether we will use return type as it is or not.
                awaitTaskReturn = returnType.Equals(taskType);
                return;
            }

            var genericTaskType = model.Compilation.TaskOfTType();

            if (genericTaskType is object)
            {
                // okay, wrap the return type in Task<T>
                returnType = genericTaskType.Construct(returnType);
            }
        }

        private (ImmutableArray<VariableInfo> parameters, ITypeSymbol returnType, bool returnsByRef, VariableInfo? variableToUseAsReturnValue, bool unsafeAddressTakenUsed)
            GetSignatureInformation(
                DataFlowAnalysis dataFlowAnalysisData,
                Dictionary<ISymbol, VariableInfo> variableInfoMap,
                bool isInExpressionOrHasReturnStatement)
        {
            var model = _semanticDocument.SemanticModel;
            var compilation = model.Compilation;
            if (isInExpressionOrHasReturnStatement)
            {
                // check whether current selection contains return statement
                var parameters = GetMethodParameters(variableInfoMap);
                var (returnType, returnsByRef) = SelectionResult.GetReturnType();
                returnType ??= compilation.GetSpecialType(SpecialType.System_Object);

                var unsafeAddressTakenUsed = ContainsVariableUnsafeAddressTaken(dataFlowAnalysisData, variableInfoMap.Keys);
                return (parameters, returnType, returnsByRef, null, unsafeAddressTakenUsed);
            }
            else
            {
                // no return statement
                var parameters = MarkVariableInfoToUseAsReturnValueIfPossible(GetMethodParameters(variableInfoMap));
                var variableToUseAsReturnValue = parameters.FirstOrDefault(v => v.UseAsReturnValue);
                var returnType = variableToUseAsReturnValue != null
                    ? variableToUseAsReturnValue.GetVariableType()
                    : compilation.GetSpecialType(SpecialType.System_Void);

                var unsafeAddressTakenUsed = ContainsVariableUnsafeAddressTaken(dataFlowAnalysisData, variableInfoMap.Keys);
                return (parameters, returnType, returnsByRef: false, variableToUseAsReturnValue, unsafeAddressTakenUsed);
            }
        }

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
            bool unsafeAddressTakenUsed, bool returnTypeHasAnonymousType,
            bool containsAnyLocalFunctionCallNotWithinSpan)
        {
            var readonlyFieldStatus = CheckReadOnlyFields(model, symbolMap);

            var namesWithAnonymousTypes = parameters.Where(v => v.OriginalTypeHadAnonymousTypeOrDelegate).Select(v => v.Name ?? string.Empty);
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

            var asyncRefOutParameterStatus = CheckAsyncMethodRefOutParameters(parameters);

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
            if (SelectionResult.ShouldPutAsyncModifier())
            {
                var names = parameters.Where(v => v is { UseAsReturnValue: false, ParameterModifier: ParameterBehavior.Out or ParameterBehavior.Ref })
                                      .Select(p => p.Name ?? string.Empty);

                if (names.Any())
                    return new OperationStatus(succeeded: true, string.Format(FeaturesResources.Asynchronous_method_cannot_have_ref_out_parameters_colon_bracket_0_bracket, string.Join(", ", names)));
            }

            return OperationStatus.SucceededStatus;
        }

        private Dictionary<ISymbol, List<SyntaxToken>> GetSymbolMap(SemanticModel model)
        {
            var syntaxFactsService = _semanticDocument.Document.Project.Services.GetService<ISyntaxFactsService>();
            var context = SelectionResult.GetContainingScope();
            var symbolMap = SymbolMapBuilder.Build(syntaxFactsService, model, context, SelectionResult.FinalSpan, CancellationToken);
            return symbolMap;
        }

        private static bool ContainsVariableUnsafeAddressTaken(DataFlowAnalysis dataFlowAnalysisData, IEnumerable<ISymbol> symbols)
        {
            // check whether the selection contains "&" over a symbol exist
            var map = new HashSet<ISymbol>(dataFlowAnalysisData.UnsafeAddressTaken);
            return symbols.Any(map.Contains);
        }

        private DataFlowAnalysis GetDataFlowAnalysisData(SemanticModel model)
        {
            if (SelectionResult.SelectionInExpression)
                return model.AnalyzeDataFlow(SelectionResult.GetNodeForDataFlowAnalysis());

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

        private ImmutableArray<VariableInfo> MarkVariableInfoToUseAsReturnValueIfPossible(ImmutableArray<VariableInfo> variableInfo)
        {
            var index = GetIndexOfVariableInfoToUseAsReturnValue(variableInfo);
            if (index < 0)
                return variableInfo;

            return variableInfo.SetItem(index, VariableInfo.CreateReturnValue(variableInfo[index]));
        }

        private static ImmutableArray<VariableInfo> GetMethodParameters(Dictionary<ISymbol, VariableInfo> variableInfoMap)
        {
            var list = new FixedSizeArrayBuilder<VariableInfo>(variableInfoMap.Count);
            list.AddRange(variableInfoMap.Values);
            list.Sort();
            return list.MoveToImmutable();
        }

        /// <param name="bestEffort">When false, variables whose data flow is not understood
        /// will be returned in <paramref name="failedVariables"/>. When true, we assume any
        /// variable we don't understand has <see cref="VariableStyle.None"/></param>
        private void GenerateVariableInfoMap(
            bool bestEffort,
            SemanticModel model,
            DataFlowAnalysis dataFlowAnalysisData,
            Dictionary<ISymbol, List<SyntaxToken>> symbolMap,
            out Dictionary<ISymbol, VariableInfo> variableInfoMap,
            out List<ISymbol> failedVariables)
        {
            Contract.ThrowIfNull(model);
            Contract.ThrowIfNull(dataFlowAnalysisData);

            variableInfoMap = [];
            failedVariables = [];

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
                if (symbol.IsThisParameter() ||
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

                // If the variable doesn't have a name, it is invalid.
                if (symbol.Name.IsEmpty())
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

        private static void AddVariableToMap(IDictionary<ISymbol, VariableInfo> variableInfoMap, ISymbol localOrParameter, VariableInfo variableInfo)
            => variableInfoMap.Add(localOrParameter, variableInfo);

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

            if (UserDefinedValueType(model.Compilation, type) && !SelectionResult.Options.DoNotPutOutOrRefOnStruct)
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
            if (!writtenInside)
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
            var semanticFacts = _semanticDocument.Document.Project.Services.GetRequiredService<ISemanticFactsService>();
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

        private static bool UserDefinedValueType(Compilation compilation, ITypeSymbol type)
        {
            if (!type.IsValueType || type is IPointerTypeSymbol || type.IsEnumType())
            {
                return false;
            }

            return type.OriginalDefinition.SpecialType == SpecialType.None && !WellKnownFrameworkValueType(compilation, type);
        }

        private static bool WellKnownFrameworkValueType(Compilation compilation, ITypeSymbol type)
        {
            if (!type.IsValueType)
            {
                return false;
            }

            var cancellationTokenType = compilation.GetTypeByMetadataName(typeof(CancellationToken).FullName!);
            if (cancellationTokenType != null && cancellationTokenType.Equals(type))
            {
                return true;
            }

            return false;
        }

        protected virtual ITypeSymbol GetSymbolType(SemanticModel model, ISymbol symbol)
            => symbol switch
            {
                ILocalSymbol local => local.Type,
                IParameterSymbol parameter => parameter.Type,
                IRangeVariableSymbol rangeVariable => GetRangeVariableType(model, rangeVariable),
                _ => throw ExceptionUtilities.UnexpectedValue(symbol)
            };

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

        private bool ContainsReturnStatementInSelectedCode(SemanticModel model)
        {
            Contract.ThrowIfTrue(SelectionResult.SelectionInExpression);

            var pair = GetFlowAnalysisNodeRange();
            var controlFlowAnalysisData = model.AnalyzeControlFlow(pair.Item1, pair.Item2);

            return ContainsReturnStatementInSelectedCode(controlFlowAnalysisData.ExitPoints);
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
                        var type = GetRangeVariableType(model, rangeVariable);
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

        private static void AppendMethodTypeParameterUsedDirectly(IDictionary<ISymbol, List<SyntaxToken>> symbolMap, IDictionary<int, ITypeParameterSymbol> sortedMap)
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

        private static void AppendTypeParametersInConstraintsUsedByConstructedTypeWithItsOwnConstraints(SortedDictionary<int, ITypeParameterSymbol> sortedMap)
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

        private static IEnumerable<ITypeParameterSymbol> AppendTypeParametersInConstraintsUsedByConstructedTypeWithItsOwnConstraints(
            ITypeSymbol type, HashSet<ITypeSymbol> visited)
        {
            if (visited.Contains(type))
                return [];

            visited.Add(type);

            if (type.OriginalDefinition.Equals(type))
                return [];

            if (type is not INamedTypeSymbol constructedType)
                return [];

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
                        !parameter.AllowsRefLikeType &&
                        parameter.ConstraintTypes.IsDefaultOrEmpty)
                    {
                        continue;
                    }

                    typeParameters.Add(argument);
                    continue;
                }

                if (arguments[i] is not INamedTypeSymbol candidate)
                {
                    continue;
                }

                typeParameters.AddRange(AppendTypeParametersInConstraintsUsedByConstructedTypeWithItsOwnConstraints(candidate, visited));
            }

            return typeParameters;
        }

        private static IEnumerable<ITypeParameterSymbol> GetMethodTypeParametersInDeclaration(ITypeSymbol returnType, SortedDictionary<int, ITypeParameterSymbol> sortedMap)
        {
            // add return type to the map
            AddTypeParametersToMap(TypeParameterCollector.Collect(returnType), sortedMap);

            AppendMethodTypeParameterFromConstraint(sortedMap);

            return sortedMap.Values.ToList();
        }

        private OperationStatus CheckReadOnlyFields(SemanticModel semanticModel, Dictionary<ISymbol, List<SyntaxToken>> symbolMap)
        {
            if (ReadOnlyFieldAllowed())
                return OperationStatus.SucceededStatus;

            using var _ = ArrayBuilder<string>.GetInstance(out var names);
            var semanticFacts = _semanticDocument.Document.Project.Services.GetRequiredService<ISemanticFactsService>();
            foreach (var pair in symbolMap.Where(p => p.Key.Kind == SymbolKind.Field))
            {
                var field = (IFieldSymbol)pair.Key;
                if (!field.IsReadOnly)
                    continue;

                var tokens = pair.Value;
                if (tokens.All(t => !semanticFacts.IsWrittenTo(semanticModel, t.Parent, CancellationToken)))
                    continue;

                names.Add(field.Name ?? string.Empty);
            }

            if (names.Count > 0)
                return new OperationStatus(succeeded: true, string.Format(FeaturesResources.Assigning_to_readonly_fields_must_be_done_in_a_constructor_colon_bracket_0_bracket, string.Join(", ", names)));

            return OperationStatus.SucceededStatus;
        }

        protected static VariableInfo CreateFromSymbolCommon<T>(
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
                _ => throw ExceptionUtilities.UnexpectedValue(symbol)
            };
        }
    }
}
