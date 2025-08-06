// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.CopyAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Factory to create <see cref="AnalysisEntity"/> objects for operations, symbol declarations, etc.
    /// This factory also tracks analysis entities that share the same instance location (e.g. value type members).
    /// NOTE: This factory must only be used from within an <see cref="OperationVisitor"/>, as it is tied to the visitor's state tracking via <see cref="_getIsInsideAnonymousObjectInitializer"/> delegate.
    /// </summary>
    public sealed class AnalysisEntityFactory
    {
        private readonly ControlFlowGraph _controlFlowGraph;
        private readonly WellKnownTypeProvider _wellKnownTypeProvider;
        private readonly Dictionary<IOperation, AnalysisEntity?> _analysisEntityMap;
        private readonly Dictionary<ITupleOperation, ImmutableArray<AnalysisEntity>> _tupleElementEntitiesMap;
        private readonly Dictionary<CaptureId, AnalysisEntity> _captureIdEntityMap;
        private readonly Dictionary<CaptureId, CopyAbstractValue> _captureIdCopyValueMap;
        private readonly Dictionary<ISymbol, PointsToAbstractValue> _instanceLocationsForSymbols;
        private readonly Func<IOperation, PointsToAbstractValue>? _getPointsToAbstractValue;
        private readonly Func<bool> _getIsInsideAnonymousObjectInitializer;
        private readonly Func<IFlowCaptureOperation, bool> _getIsLValueFlowCapture;
        private readonly AnalysisEntity? _interproceduralThisOrMeInstanceForCaller;
        private readonly ImmutableStack<IOperation>? _interproceduralCallStack;
        private readonly Func<IOperation, AnalysisEntity?>? _interproceduralGetAnalysisEntityForFlowCapture;
        private readonly Func<ISymbol, ImmutableStack<IOperation>?> _getInterproceduralCallStackForOwningSymbol;

        internal AnalysisEntityFactory(
            ControlFlowGraph controlFlowGraph,
            WellKnownTypeProvider wellKnownTypeProvider,
            Func<IOperation, PointsToAbstractValue>? getPointsToAbstractValue,
            Func<bool> getIsInsideAnonymousObjectInitializer,
            Func<IFlowCaptureOperation, bool> getIsLValueFlowCapture,
            INamedTypeSymbol containingTypeSymbol,
            AnalysisEntity? interproceduralInvocationInstance,
            AnalysisEntity? interproceduralThisOrMeInstanceForCaller,
            ImmutableStack<IOperation>? interproceduralCallStack,
            ImmutableDictionary<ISymbol, PointsToAbstractValue>? interproceduralCapturedVariablesMap,
            Func<IOperation, AnalysisEntity?>? interproceduralGetAnalysisEntityForFlowCapture,
            Func<ISymbol, ImmutableStack<IOperation>?> getInterproceduralCallStackForOwningSymbol)
        {
            _controlFlowGraph = controlFlowGraph;
            _wellKnownTypeProvider = wellKnownTypeProvider;
            _getPointsToAbstractValue = getPointsToAbstractValue;
            _getIsInsideAnonymousObjectInitializer = getIsInsideAnonymousObjectInitializer;
            _getIsLValueFlowCapture = getIsLValueFlowCapture;
            _interproceduralThisOrMeInstanceForCaller = interproceduralThisOrMeInstanceForCaller;
            _interproceduralCallStack = interproceduralCallStack;
            _interproceduralGetAnalysisEntityForFlowCapture = interproceduralGetAnalysisEntityForFlowCapture;
            _getInterproceduralCallStackForOwningSymbol = getInterproceduralCallStackForOwningSymbol;

            _analysisEntityMap = [];
            _tupleElementEntitiesMap = [];
            _captureIdEntityMap = [];
            _captureIdCopyValueMap = [];

            _instanceLocationsForSymbols = [];
            if (interproceduralCapturedVariablesMap != null)
            {
                _instanceLocationsForSymbols.AddRange(interproceduralCapturedVariablesMap);
            }

            if (interproceduralInvocationInstance != null)
            {
                ThisOrMeInstance = interproceduralInvocationInstance;
            }
            else
            {
                var thisOrMeInstanceLocation = AbstractLocation.CreateThisOrMeLocation(containingTypeSymbol, interproceduralCallStack);
                var instanceLocation = PointsToAbstractValue.Create(thisOrMeInstanceLocation, mayBeNull: false);
                ThisOrMeInstance = AnalysisEntity.CreateThisOrMeInstance(containingTypeSymbol, instanceLocation);
            }
        }

        public AnalysisEntity ThisOrMeInstance { get; }

        private static ImmutableArray<AbstractIndex> CreateAbstractIndices<T>(ImmutableArray<T> indices)
            where T : IOperation
        {
            if (!indices.IsEmpty)
            {
                var builder = ArrayBuilder<AbstractIndex>.GetInstance(indices.Length);
                foreach (var index in indices)
                {
                    builder.Add(CreateAbstractIndex(index));
                }

                return builder.ToImmutableAndFree();
            }

            return ImmutableArray<AbstractIndex>.Empty;
        }

        private static AbstractIndex CreateAbstractIndex(IOperation operation)
        {
            if (operation.ConstantValue.HasValue && operation.ConstantValue.Value is int index)
            {
                return AbstractIndex.Create(index);
            }
            // TODO: We need to find the abstract value for the entity to use it for indexing.
            // https://github.com/dotnet/roslyn-analyzers/issues/1577
            //else if (TryCreate(operation, out AnalysisEntity analysisEntity))
            //{
            //    return AbstractIndex.Create(analysisEntity);
            //}

            return AbstractIndex.Create(operation);
        }

        public bool TryCreate(IOperation operation, [NotNullWhen(returnValue: true)] out AnalysisEntity? analysisEntity)
        {
            if (_analysisEntityMap.TryGetValue(operation, out analysisEntity))
            {
                return analysisEntity != null;
            }

            analysisEntity = null;
            ISymbol? symbol = null;
            ImmutableArray<AbstractIndex> indices = ImmutableArray<AbstractIndex>.Empty;
            IOperation? instance = null;
            ITypeSymbol? type = operation.Type;
            switch (operation)
            {
                case ILocalReferenceOperation localReference:
                    symbol = localReference.Local;
                    break;

                case IParameterReferenceOperation parameterReference:
                    symbol = parameterReference.Parameter;
                    break;

                case IMemberReferenceOperation memberReference:
                    instance = memberReference.Instance;
                    GetSymbolAndIndicesForMemberReference(memberReference, ref symbol, ref indices);
                    break;

                case IArrayElementReferenceOperation arrayElementReference:
                    instance = arrayElementReference.ArrayReference;
                    indices = CreateAbstractIndices(arrayElementReference.Indices);
                    break;

                case IDynamicIndexerAccessOperation dynamicIndexerAccess:
                    instance = dynamicIndexerAccess.Operation;
                    indices = CreateAbstractIndices(dynamicIndexerAccess.Arguments);
                    break;

                case IConditionalAccessInstanceOperation conditionalAccessInstance:
                    IConditionalAccessOperation? conditionalAccess = conditionalAccessInstance.GetConditionalAccess();
                    instance = conditionalAccess?.Operation;
                    if (conditionalAccessInstance.Parent is IMemberReferenceOperation memberReferenceParent)
                    {
                        GetSymbolAndIndicesForMemberReference(memberReferenceParent, ref symbol, ref indices);
                    }

                    break;

                case IInstanceReferenceOperation instanceReference:
                    if (_getPointsToAbstractValue != null)
                    {
                        instance = instanceReference.GetInstance(_getIsInsideAnonymousObjectInitializer());
                        if (instance == null)
                        {
                            // Reference to this or base instance.
                            analysisEntity = _interproceduralCallStack != null && _interproceduralCallStack.Peek().DescendantsAndSelf().Contains(instanceReference) ?
                                _interproceduralThisOrMeInstanceForCaller :
                                ThisOrMeInstance;
                        }
                        else
                        {
                            var instanceLocation = _getPointsToAbstractValue(instanceReference);
                            analysisEntity = AnalysisEntity.Create(instanceReference, instanceLocation);
                        }
                    }

                    break;

                case IConversionOperation conversion:
                    return TryCreate(conversion.Operand, out analysisEntity);

                case IParenthesizedOperation parenthesized:
                    return TryCreate(parenthesized.Operand, out analysisEntity);

                case IArgumentOperation argument:
                    return TryCreate(argument.Value, out analysisEntity);

                case IFlowCaptureOperation flowCapture:
                    var isLvalueFlowCapture = _getIsLValueFlowCapture(flowCapture);
                    analysisEntity = GetOrCreateForFlowCapture(flowCapture.Id, flowCapture.Value.Type, flowCapture, isLvalueFlowCapture);

                    // Store flow capture copy values for simple flow captures of non-flow captured entity.
                    // This enables pseudo copy-analysis of values of these two entities in absence of true copy analysis, which is expensive.
                    if (!isLvalueFlowCapture &&
                        TryCreate(flowCapture.Value, out var capturedEntity) &&
                        capturedEntity.CaptureId == null &&
                        !_captureIdCopyValueMap.ContainsKey(flowCapture.Id) &&
                        analysisEntity.Type.IsValueType == capturedEntity.Type.IsValueType)
                    {
                        // Skip flow capture for conversions unless we know the points to value
                        // for conversion and operand is identical.
                        if (flowCapture.Value is IConversionOperation conversion)
                        {
                            if (_getPointsToAbstractValue == null ||
                                _getPointsToAbstractValue(conversion) != _getPointsToAbstractValue(conversion.Operand))
                            {
                                break;
                            }
                        }

                        var kind = capturedEntity.Type.IsValueType ? CopyAbstractValueKind.KnownValueCopy : CopyAbstractValueKind.KnownReferenceCopy;
                        var copyValue = new CopyAbstractValue(ImmutableHashSet.Create(analysisEntity, capturedEntity), kind);
                        _captureIdCopyValueMap.Add(flowCapture.Id, copyValue);
                    }

                    break;

                case IFlowCaptureReferenceOperation flowCaptureReference:
                    analysisEntity = GetOrCreateForFlowCapture(flowCaptureReference.Id, flowCaptureReference.Type, flowCaptureReference, flowCaptureReference.IsLValueFlowCaptureReference());
                    break;

                case IDeclarationExpressionOperation declarationExpression:
                    switch (declarationExpression.Expression)
                    {
                        case ILocalReferenceOperation localReference:
                            return TryCreateForSymbolDeclaration(localReference.Local, out analysisEntity);

                        case ITupleOperation tupleOperation:
                            return TryCreate(tupleOperation, out analysisEntity);
                    }

                    break;

                case IVariableDeclaratorOperation variableDeclarator:
                    symbol = variableDeclarator.Symbol;
                    type = variableDeclarator.Symbol.Type;
                    break;

                case IDeclarationPatternOperation declarationPattern:
                    var declaredLocal = declarationPattern.DeclaredSymbol as ILocalSymbol;
                    symbol = declaredLocal;
                    type = declaredLocal?.Type;
                    break;

                default:
                    break;
            }

            if (symbol != null || !indices.IsEmpty)
            {
                TryCreate(symbol, indices, type!, instance, out analysisEntity);
            }

            _analysisEntityMap[operation] = analysisEntity;
            return analysisEntity != null;
        }

        private static void GetSymbolAndIndicesForMemberReference(IMemberReferenceOperation memberReference, ref ISymbol? symbol, ref ImmutableArray<AbstractIndex> indices)
        {
            switch (memberReference)
            {
                case IFieldReferenceOperation fieldReference:
                    symbol = fieldReference.Field;
                    if (fieldReference.Field.CorrespondingTupleField != null)
                    {
                        // For tuple fields, always use the CorrespondingTupleField (i.e. Item1, Item2, etc.) from the underlying value tuple type.
                        // This allows seamless operation between named tuple elements and use of Item1, Item2, etc. to access tuple elements.
                        var name = fieldReference.Field.CorrespondingTupleField.Name;
                        symbol = fieldReference.Field.ContainingType.GetUnderlyingValueTupleTypeOrThis()?.GetMembers(name).OfType<IFieldSymbol>().FirstOrDefault()
                            ?? symbol;
                    }

                    break;

                case IEventReferenceOperation eventReference:
                    symbol = eventReference.Member;
                    break;

                case IPropertyReferenceOperation propertyReference:
                    // We are only tracking:
                    // 1) Indexers
                    // 2) Read-only properties.
                    // 3) Properties with a backing field (auto-generated properties)
                    if (!propertyReference.Arguments.IsEmpty ||
                        propertyReference.Property.IsReadOnly ||
                        propertyReference.Property.IsPropertyWithBackingField(out _))
                    {
                        symbol = propertyReference.Property;
                        indices = !propertyReference.Arguments.IsEmpty ?
                            CreateAbstractIndices(propertyReference.Arguments.SelectAsArray(a => a.Value)) :
                            ImmutableArray<AbstractIndex>.Empty;
                    }

                    break;
            }
        }

        public bool TryCreateForSymbolDeclaration(ISymbol symbol, [NotNullWhen(returnValue: true)] out AnalysisEntity? analysisEntity)
        {
            Debug.Assert(symbol.Kind is SymbolKind.Local or SymbolKind.Parameter or SymbolKind.Field or SymbolKind.Property);

            var indices = ImmutableArray<AbstractIndex>.Empty;
            IOperation? instance = null;
            var type = symbol.GetMemberOrLocalOrParameterType();
            RoslynDebug.Assert(type != null);

            return TryCreate(symbol, indices, type, instance, out analysisEntity);
        }

        public bool TryCreateForTupleElements(ITupleOperation tupleOperation, [NotNullWhen(returnValue: true)] out ImmutableArray<AnalysisEntity> elementEntities)
        {
            if (_tupleElementEntitiesMap.TryGetValue(tupleOperation, out elementEntities))
            {
                return !elementEntities.IsDefault;
            }

            try
            {
                elementEntities = default;
                if (tupleOperation.Type?.IsTupleType != true ||
                    _getPointsToAbstractValue == null)
                {
                    return false;
                }

                var tupleType = (INamedTypeSymbol)tupleOperation.Type;
                if (tupleType.TupleElements.IsDefault)
                {
                    return false;
                }

                PointsToAbstractValue instanceLocation = _getPointsToAbstractValue(tupleOperation);
                AnalysisEntity? entityForInstanceLocation = null;
                var underlyingValueTupleType = tupleType.GetUnderlyingValueTupleTypeOrThis()!;
                AnalysisEntity? parentEntity = null;
                if (tupleOperation.TryGetParentTupleOperation(out var parentTupleOperationOpt, out var elementOfParentTupleContainingTuple) &&
                    TryCreateForTupleElements(parentTupleOperationOpt, out var parentTupleElementEntities))
                {
                    Debug.Assert(parentTupleOperationOpt.Elements.Length == parentTupleElementEntities.Length);
                    for (int i = 0; i < parentTupleOperationOpt.Elements.Length; i++)
                    {
                        if (parentTupleOperationOpt.Elements[i] == elementOfParentTupleContainingTuple)
                        {
                            parentEntity = parentTupleElementEntities[i];
                            instanceLocation = parentEntity.InstanceLocation;
                            entityForInstanceLocation = parentEntity.EntityForInstanceLocation;
                            break;
                        }
                    }

                    RoslynDebug.Assert(parentEntity != null);
                }
                else
                {
                    parentEntity = AnalysisEntity.Create(underlyingValueTupleType, ImmutableArray<AbstractIndex>.Empty,
                        underlyingValueTupleType, instanceLocation, parent: null, entityForInstanceLocation: null);
                }

                Debug.Assert(parentEntity.InstanceLocation == instanceLocation);

                using var _ = ArrayBuilder<AnalysisEntity>.GetInstance(tupleType.TupleElements.Length, out var builder);
                foreach (var field in tupleType.TupleElements)
                {
                    var tupleFieldName = field.CorrespondingTupleField!.Name;
                    var mappedValueTupleField = underlyingValueTupleType.GetMembers(tupleFieldName).OfType<IFieldSymbol>().FirstOrDefault();
                    if (mappedValueTupleField == null)
                    {
                        return false;
                    }

                    builder.Add(AnalysisEntity.Create(mappedValueTupleField, indices: ImmutableArray<AbstractIndex>.Empty,
                        type: mappedValueTupleField.Type, instanceLocation, parentEntity, entityForInstanceLocation));
                }

                elementEntities = builder.ToImmutable();
                return true;
            }
            finally
            {
                _tupleElementEntitiesMap[tupleOperation] = elementEntities;
            }
        }

        public bool TryCreateForArrayElementInitializer(IArrayCreationOperation arrayCreation, ImmutableArray<AbstractIndex> indices, ITypeSymbol elementType, [NotNullWhen(returnValue: true)] out AnalysisEntity? analysisEntity)
        {
            Debug.Assert(!indices.IsEmpty);

            return TryCreate(symbol: null, indices, elementType, arrayCreation, out analysisEntity);
        }

        public bool TryGetForFlowCapture(CaptureId captureId, out AnalysisEntity analysisEntity)
            => _captureIdEntityMap.TryGetValue(captureId, out analysisEntity);

        public bool TryGetCopyValueForFlowCapture(CaptureId captureId, out CopyAbstractValue copyValue)
            => _captureIdCopyValueMap.TryGetValue(captureId, out copyValue);

        public bool TryGetForInterproceduralAnalysis(IOperation operation, out AnalysisEntity? analysisEntity)
            => _analysisEntityMap.TryGetValue(operation, out analysisEntity);

        private AnalysisEntity GetOrCreateForFlowCapture(CaptureId captureId, ITypeSymbol? type, IOperation flowCaptureOrReference, bool isLValueFlowCapture)
        {
            // Type can be null for capture of operations with OperationKind.None
            type ??= _wellKnownTypeProvider.Compilation.GetSpecialType(SpecialType.System_Object);

            var interproceduralFlowCaptureEntity = _interproceduralGetAnalysisEntityForFlowCapture?.Invoke(flowCaptureOrReference);
            if (interproceduralFlowCaptureEntity != null)
            {
                Debug.Assert(_interproceduralCallStack.Last().Descendants().Contains(flowCaptureOrReference));
                return interproceduralFlowCaptureEntity;
            }

            Debug.Assert(_controlFlowGraph.DescendantOperations().Contains(flowCaptureOrReference));
            if (!_captureIdEntityMap.TryGetValue(captureId, out var entity))
            {
                var interproceduralCaptureId = new InterproceduralCaptureId(captureId, _controlFlowGraph, isLValueFlowCapture);
                var instanceLocation = PointsToAbstractValue.Create(
                    AbstractLocation.CreateFlowCaptureLocation(interproceduralCaptureId, type, _interproceduralCallStack),
                    mayBeNull: false);
                entity = AnalysisEntity.Create(interproceduralCaptureId, type, instanceLocation);
                _captureIdEntityMap.Add(captureId, entity);
            }

            return entity;
        }

        private bool TryCreate(ISymbol? symbol, ImmutableArray<AbstractIndex> indices,
            ITypeSymbol type, IOperation? instance, [NotNullWhen(returnValue: true)] out AnalysisEntity? analysisEntity)
        {
            Debug.Assert(symbol != null || !indices.IsEmpty);

            analysisEntity = null;

            // Only analyze member symbols if we have points to analysis result.
            if (_getPointsToAbstractValue == null &&
                symbol?.Kind != SymbolKind.Local &&
                symbol?.Kind != SymbolKind.Parameter)
            {
                return false;
            }

            PointsToAbstractValue? instanceLocation = null;
            AnalysisEntity? entityForReferenceTypeInstance = null;
            AnalysisEntity? parent = null;
            if (instance?.Type != null)
            {
                if (instance.Type.IsValueType)
                {
                    if (TryCreate(instance, out var instanceEntityOpt) &&
                        instanceEntityOpt.Type.IsValueType)
                    {
                        parent = instanceEntityOpt;
                        instanceLocation = parent.InstanceLocation;
                    }
                    else
                    {
                        // For value type allocations, we store the points to location.
                        var instancePointsToValue = _getPointsToAbstractValue!(instance);
                        if (!ReferenceEquals(instancePointsToValue, PointsToAbstractValue.NoLocation))
                        {
                            instanceLocation = instancePointsToValue;
                        }
                    }

                    if (instanceLocation == null)
                    {
                        return false;
                    }
                }
                else
                {
                    instanceLocation = _getPointsToAbstractValue!(instance);

                    // If the instanceLocation can point to multiple potential locations, then we also store the
                    // entity for the instance location in the analysis entity. This is done to ensure that we
                    // can distinguish this entity from any other entity which can also point to the same set of
                    // potential locations, but the actual runtime location for both these entities can be different.
                    // See https://github.com/dotnet/roslyn-analyzers/issues/6520 for an example.
                    if (instanceLocation.Kind == PointsToAbstractValueKind.KnownLocations &&
                        instanceLocation.Locations.Count > 1)
                    {
                        if (TryCreate(instance, out var instanceEntity))
                        {
                            entityForReferenceTypeInstance = instanceEntity;
                        }
                        else
                        {
                            instanceLocation = instanceLocation.NullState switch
                            {
                                NullAbstractValue.Null => PointsToAbstractValue.UnknownNull,
                                NullAbstractValue.NotNull => PointsToAbstractValue.UnknownNotNull,
                                _ => PointsToAbstractValue.Unknown,
                            };
                        }
                    }
                }
            }

            analysisEntity = Create(symbol, indices, type, instanceLocation, parent, entityForReferenceTypeInstance);
            return true;
        }

        private PointsToAbstractValue? EnsureLocation(PointsToAbstractValue? instanceLocation, ISymbol? symbol, AnalysisEntity? parent)
        {
            if (instanceLocation == null && symbol != null)
            {
                Debug.Assert(symbol.Kind == SymbolKind.Local || symbol.Kind == SymbolKind.Parameter || symbol.IsStatic || symbol.IsLambdaOrLocalFunction());

                if (!_instanceLocationsForSymbols.TryGetValue(symbol, out instanceLocation))
                {
                    if (parent != null)
                    {
                        instanceLocation = parent.InstanceLocation;
                    }
                    else
                    {
                        // Symbol instance location for locals and parameters should also include the interprocedural call stack because
                        // we might have recursive invocations to the same method and the symbol declarations
                        // from both the current and prior invocation of the method in the call stack should be distinct entities.
                        ImmutableStack<IOperation>? interproceduralCallStackForSymbolDeclaration;
                        if (_interproceduralCallStack != null &&
                            (symbol.Kind == SymbolKind.Local || symbol.Kind == SymbolKind.Parameter))
                        {
                            interproceduralCallStackForSymbolDeclaration = _getInterproceduralCallStackForOwningSymbol(symbol.ContainingSymbol);
                        }
                        else
                        {
                            interproceduralCallStackForSymbolDeclaration = ImmutableStack<IOperation>.Empty;
                        }

                        var location = AbstractLocation.CreateSymbolLocation(symbol, interproceduralCallStackForSymbolDeclaration);
                        instanceLocation = PointsToAbstractValue.Create(location, mayBeNull: false);
                    }

                    _instanceLocationsForSymbols.Add(symbol, instanceLocation);
                }
            }

            return instanceLocation;
        }

        private AnalysisEntity Create(ISymbol? symbol, ImmutableArray<AbstractIndex> indices, ITypeSymbol type, PointsToAbstractValue? instanceLocation, AnalysisEntity? parent, AnalysisEntity? entityForInstanceLocation)
        {
            instanceLocation = EnsureLocation(instanceLocation, symbol, parent);
            RoslynDebug.Assert(instanceLocation != null);
            var analysisEntity = AnalysisEntity.Create(symbol, indices, type, instanceLocation, parent, entityForInstanceLocation);
            return analysisEntity;
        }

        public AnalysisEntity CreateWithNewInstanceRoot(AnalysisEntity analysisEntity, AnalysisEntity newRootInstance)
        {
            if (analysisEntity.InstanceLocation == newRootInstance.InstanceLocation &&
                analysisEntity.Parent == newRootInstance.Parent &&
                analysisEntity.EntityForInstanceLocation == newRootInstance.EntityForInstanceLocation)
            {
                return analysisEntity;
            }

            if (analysisEntity.Parent == null)
            {
                return newRootInstance;
            }

            AnalysisEntity parentOpt = CreateWithNewInstanceRoot(analysisEntity.Parent, newRootInstance);
            return Create(analysisEntity.Symbol, analysisEntity.Indices, analysisEntity.Type, newRootInstance.InstanceLocation, parentOpt, newRootInstance.EntityForInstanceLocation);
        }
    }
}
