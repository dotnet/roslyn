// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities.Extensions;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.Operations;

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
        private readonly Dictionary<IOperation, AnalysisEntity> _analysisEntityMap;
        private readonly Dictionary<ITupleOperation, ImmutableArray<AnalysisEntity>> _tupleElementEntitiesMap;
        private readonly Dictionary<CaptureId, AnalysisEntity> _captureIdEntityMap;
        private readonly Dictionary<ISymbol, PointsToAbstractValue> _instanceLocationsForSymbols;
        private readonly Func<IOperation, PointsToAbstractValue> _getPointsToAbstractValueOpt;
        private readonly Func<bool> _getIsInsideAnonymousObjectInitializer;
        private readonly Func<IFlowCaptureOperation, bool> _getIsLValueFlowCapture;
        private readonly AnalysisEntity _interproceduralThisOrMeInstanceForCallerOpt;
        private readonly ImmutableStack<IOperation> _interproceduralCallStackOpt;
        private readonly Func<IOperation, AnalysisEntity> _interproceduralGetAnalysisEntityForFlowCaptureOpt;
        private readonly Func<ISymbol, ImmutableStack<IOperation>> _getInterproceduralCallStackForOwningSymbol;

        internal AnalysisEntityFactory(
            ControlFlowGraph controlFlowGraph,
            WellKnownTypeProvider wellKnownTypeProvider,
            Func<IOperation, PointsToAbstractValue> getPointsToAbstractValueOpt,
            Func<bool> getIsInsideAnonymousObjectInitializer,
            Func<IFlowCaptureOperation, bool> getIsLValueFlowCapture,
            INamedTypeSymbol containingTypeSymbol,
            AnalysisEntity interproceduralInvocationInstanceOpt,
            AnalysisEntity interproceduralThisOrMeInstanceForCallerOpt,
            ImmutableStack<IOperation> interproceduralCallStackOpt,
            ImmutableDictionary<ISymbol, PointsToAbstractValue> interproceduralCapturedVariablesMapOpt,
            Func<IOperation, AnalysisEntity> interproceduralGetAnalysisEntityForFlowCaptureOpt,
            Func<ISymbol, ImmutableStack<IOperation>> getInterproceduralCallStackForOwningSymbol)
        {
            _controlFlowGraph = controlFlowGraph;
            _wellKnownTypeProvider = wellKnownTypeProvider;
            _getPointsToAbstractValueOpt = getPointsToAbstractValueOpt;
            _getIsInsideAnonymousObjectInitializer = getIsInsideAnonymousObjectInitializer;
            _getIsLValueFlowCapture = getIsLValueFlowCapture;
            _interproceduralThisOrMeInstanceForCallerOpt = interproceduralThisOrMeInstanceForCallerOpt;
            _interproceduralCallStackOpt = interproceduralCallStackOpt;
            _interproceduralGetAnalysisEntityForFlowCaptureOpt = interproceduralGetAnalysisEntityForFlowCaptureOpt;
            _getInterproceduralCallStackForOwningSymbol = getInterproceduralCallStackForOwningSymbol;

            _analysisEntityMap = new Dictionary<IOperation, AnalysisEntity>();
            _tupleElementEntitiesMap = new Dictionary<ITupleOperation, ImmutableArray<AnalysisEntity>>();
            _captureIdEntityMap = new Dictionary<CaptureId, AnalysisEntity>();

            _instanceLocationsForSymbols = new Dictionary<ISymbol, PointsToAbstractValue>();
            if (interproceduralCapturedVariablesMapOpt != null)
            {
                _instanceLocationsForSymbols.AddRange(interproceduralCapturedVariablesMapOpt);
            }

            if (interproceduralInvocationInstanceOpt != null)
            {
                ThisOrMeInstance = interproceduralInvocationInstanceOpt;
            }
            else
            {
                var thisOrMeInstanceLocation = AbstractLocation.CreateThisOrMeLocation(containingTypeSymbol, interproceduralCallStackOpt);
                var instanceLocation = PointsToAbstractValue.Create(thisOrMeInstanceLocation, mayBeNull: false);
                ThisOrMeInstance = AnalysisEntity.CreateThisOrMeInstance(containingTypeSymbol, instanceLocation);
            }
        }

        public AnalysisEntity ThisOrMeInstance { get; }

        private ImmutableArray<AbstractIndex> CreateAbstractIndices<T>(ImmutableArray<T> indices)
            where T : IOperation
        {
            if (indices.Length > 0)
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

        public bool TryCreate(IOperation operation, out AnalysisEntity analysisEntity)
        {
            if (_analysisEntityMap.TryGetValue(operation, out analysisEntity))
            {
                return analysisEntity != null;
            }

            analysisEntity = null;
            ISymbol symbolOpt = null;
            ImmutableArray<AbstractIndex> indices = ImmutableArray<AbstractIndex>.Empty;
            IOperation instanceOpt = null;
            ITypeSymbol type = operation.Type;
            switch (operation)
            {
                case ILocalReferenceOperation localReference:
                    symbolOpt = localReference.Local;
                    break;

                case IParameterReferenceOperation parameterReference:
                    symbolOpt = parameterReference.Parameter;
                    break;

                case IMemberReferenceOperation memberReference:
                    instanceOpt = memberReference.Instance;
                    GetSymbolAndIndicesForMemberReference(memberReference, ref symbolOpt, ref indices);

                    // Workaround for https://github.com/dotnet/roslyn/issues/22736 (IPropertyReferenceExpressions in IAnonymousObjectCreationExpression are missing a receiver).
                    if (instanceOpt == null &&
                        symbolOpt != null &&
                        memberReference is IPropertyReferenceOperation propertyReference)
                    {
                        instanceOpt = propertyReference.GetAnonymousObjectCreation();
                    }

                    break;

                case IArrayElementReferenceOperation arrayElementReference:
                    instanceOpt = arrayElementReference.ArrayReference;
                    indices = CreateAbstractIndices(arrayElementReference.Indices);
                    break;

                case IDynamicIndexerAccessOperation dynamicIndexerAccess:
                    instanceOpt = dynamicIndexerAccess.Operation;
                    indices = CreateAbstractIndices(dynamicIndexerAccess.Arguments);
                    break;

                case IConditionalAccessInstanceOperation conditionalAccessInstance:
                    IConditionalAccessOperation conditionalAccess = conditionalAccessInstance.GetConditionalAccess();
                    instanceOpt = conditionalAccess.Operation;
                    if (conditionalAccessInstance.Parent is IMemberReferenceOperation memberReferenceParent)
                    {
                        GetSymbolAndIndicesForMemberReference(memberReferenceParent, ref symbolOpt, ref indices);
                    }
                    break;

                case IInstanceReferenceOperation instanceReference:
                    if (_getPointsToAbstractValueOpt != null)
                    {
                        instanceOpt = instanceReference.GetInstance(_getIsInsideAnonymousObjectInitializer());
                        if (instanceOpt == null)
                        {
                            // Reference to this or base instance.
                            analysisEntity = _interproceduralCallStackOpt != null && _interproceduralCallStackOpt.Peek().DescendantsAndSelf().Contains(instanceReference) ?
                                _interproceduralThisOrMeInstanceForCallerOpt :
                                ThisOrMeInstance;
                        }
                        else
                        {
                            var instanceLocation = _getPointsToAbstractValueOpt(instanceReference);
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
                    analysisEntity = GetOrCreateForFlowCapture(flowCapture.Id, flowCapture.Value.Type, flowCapture, _getIsLValueFlowCapture(flowCapture));
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
                    symbolOpt = variableDeclarator.Symbol;
                    type = variableDeclarator.Symbol.Type;
                    break;

                case IDeclarationPatternOperation declarationPattern:
                    var declaredLocal = declarationPattern.DeclaredSymbol as ILocalSymbol;
                    symbolOpt = declaredLocal;
                    type = declaredLocal?.Type;
                    break;

                default:
                    break;
            }

            if (symbolOpt != null || !indices.IsEmpty)
            {
                TryCreate(symbolOpt, indices, type, instanceOpt, out analysisEntity);
            }

            _analysisEntityMap[operation] = analysisEntity;
            return analysisEntity != null;
        }

        private void GetSymbolAndIndicesForMemberReference(IMemberReferenceOperation memberReference, ref ISymbol symbolOpt, ref ImmutableArray<AbstractIndex> indices)
        {
            switch (memberReference)
            {
                case IFieldReferenceOperation fieldReference:
                    symbolOpt = fieldReference.Field;
                    if (fieldReference.Field.CorrespondingTupleField != null)
                    {
                        // For tuple fields, always use the CorrespondingTupleField (i.e. Item1, Item2, etc.) from the underlying value tuple type.
                        // This allows seamless operation between named tuple elements and use of Item1, Item2, etc. to access tuple elements.
                        var name = fieldReference.Field.CorrespondingTupleField.Name;
                        symbolOpt = fieldReference.Field.ContainingType.GetUnderlyingValueTupleTypeOrThis().GetMembers(name).OfType<IFieldSymbol>().FirstOrDefault()
                            ?? symbolOpt;
                    }
                    break;

                case IEventReferenceOperation eventReference:
                    symbolOpt = eventReference.Member;
                    break;

                case IPropertyReferenceOperation propertyReference:
                    // We are only tracking:
                    // 1) Indexers
                    // 2) Read-only properties.
                    // 3) Properties with a backing field (auto-generated properties)
                    if (propertyReference.Arguments.Length > 0 ||
                        propertyReference.Property.IsReadOnly ||
                        propertyReference.Property.IsPropertyWithBackingField())
                    {
                        symbolOpt = propertyReference.Property;
                        indices = propertyReference.Arguments.Length > 0 ?
                            CreateAbstractIndices(propertyReference.Arguments.Select(a => a.Value).ToImmutableArray()) :
                            ImmutableArray<AbstractIndex>.Empty;
                    }
                    break;
            }
        }

        public bool TryCreateForSymbolDeclaration(ISymbol symbol, out AnalysisEntity analysisEntity)
        {
            Debug.Assert(symbol != null);
            Debug.Assert(symbol.Kind == SymbolKind.Local || symbol.Kind == SymbolKind.Parameter || symbol.Kind == SymbolKind.Field || symbol.Kind == SymbolKind.Property);

            var indices = ImmutableArray<AbstractIndex>.Empty;
            IOperation instance = null;
            var type = symbol.GetMemberOrLocalOrParameterType();
            Debug.Assert(type != null);

            return TryCreate(symbol, indices, type, instance, out analysisEntity);
        }

        public bool TryCreateForTupleElements(ITupleOperation tupleOperation, out ImmutableArray<AnalysisEntity> elementEntities)
        {
            if (_tupleElementEntitiesMap.TryGetValue(tupleOperation, out elementEntities))
            {
                return !elementEntities.IsDefault;
            }

            try
            {
                elementEntities = default;
                if (!tupleOperation.Type.IsTupleType)
                {
                    return false;
                }

                var tupleType = (INamedTypeSymbol)tupleOperation.Type;
                if (tupleType.TupleElements.IsDefault)
                {
                    return false;
                }

                PointsToAbstractValue instanceLocation = _getPointsToAbstractValueOpt(tupleOperation);
                var underlyingValueTupleType = tupleType.GetUnderlyingValueTupleTypeOrThis();
                AnalysisEntity parentEntity = null;
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
                            break;
                        }
                    }

                    Debug.Assert(parentEntity != null);
                }
                else
                {
                    parentEntity = AnalysisEntity.Create(underlyingValueTupleType, ImmutableArray<AbstractIndex>.Empty,
                        underlyingValueTupleType, instanceLocation, parentOpt: null);
                }

                Debug.Assert(parentEntity.InstanceLocation == instanceLocation);

                var builder = ArrayBuilder<AnalysisEntity>.GetInstance(tupleType.TupleElements.Length);
                foreach (var field in tupleType.TupleElements)
                {
                    var tupleFieldName = field.CorrespondingTupleField.Name;
                    var mappedValueTupleField = underlyingValueTupleType.GetMembers(tupleFieldName).OfType<IFieldSymbol>().FirstOrDefault();
                    if (mappedValueTupleField == null)
                    {
                        builder.Free();
                        return false;
                    }

                    builder.Add(AnalysisEntity.Create(mappedValueTupleField, indices: ImmutableArray<AbstractIndex>.Empty,
                        type: mappedValueTupleField.Type, instanceLocation, parentEntity));
                }

                elementEntities = builder.ToImmutableAndFree();
                return true;
            }
            finally
            {
                _tupleElementEntitiesMap[tupleOperation] = elementEntities;
            }
        }

        public bool TryCreateForArrayElementInitializer(IArrayCreationOperation arrayCreation, ImmutableArray<AbstractIndex> indices, ITypeSymbol elementType, out AnalysisEntity analysisEntity)
        {
            Debug.Assert(arrayCreation != null);
            Debug.Assert(!indices.IsEmpty);
            Debug.Assert(elementType != null);

            ISymbol symbol = null;
            return TryCreate(symbol, indices, elementType, arrayCreation, out analysisEntity);
        }

        public bool TryGetForFlowCapture(CaptureId captureId, out AnalysisEntity analysisEntity)
            => _captureIdEntityMap.TryGetValue(captureId, out analysisEntity);

        public bool TryGetForInterproceduralAnalysis(IOperation operation, out AnalysisEntity analysisEntity)
            => _analysisEntityMap.TryGetValue(operation, out analysisEntity);

        private AnalysisEntity GetOrCreateForFlowCapture(CaptureId captureId, ITypeSymbol type, IOperation flowCaptureOrReference, bool isLValueFlowCapture)
        {
            // Type can be null for capture of operations with OperationKind.None
            type ??= _wellKnownTypeProvider.Compilation.GetSpecialType(SpecialType.System_Object);

            var interproceduralFlowCaptureEntityOpt = _interproceduralGetAnalysisEntityForFlowCaptureOpt?.Invoke(flowCaptureOrReference);
            if (interproceduralFlowCaptureEntityOpt != null)
            {
                Debug.Assert(_interproceduralCallStackOpt.Last().Descendants().Contains(flowCaptureOrReference));
                return interproceduralFlowCaptureEntityOpt;
            }

            Debug.Assert(_controlFlowGraph.DescendantOperations().Contains(flowCaptureOrReference));
            if (!_captureIdEntityMap.TryGetValue(captureId, out var entity))
            {
                var interproceduralCaptureId = new InterproceduralCaptureId(captureId, _controlFlowGraph, isLValueFlowCapture);
                var instanceLocation = PointsToAbstractValue.Create(
                    AbstractLocation.CreateFlowCaptureLocation(interproceduralCaptureId, type, _interproceduralCallStackOpt),
                    mayBeNull: false);
                entity = AnalysisEntity.Create(interproceduralCaptureId, type, instanceLocation);
                _captureIdEntityMap.Add(captureId, entity);
            }

            return entity;
        }

        private bool TryCreate(ISymbol symbolOpt, ImmutableArray<AbstractIndex> indices,
            ITypeSymbol type, IOperation instanceOpt, out AnalysisEntity analysisEntity)
        {
            Debug.Assert(symbolOpt != null || !indices.IsEmpty);
            Debug.Assert(type != null);

            analysisEntity = null;

            // Only analyze member symbols if we have points to analysis result.
            if (_getPointsToAbstractValueOpt == null &&
                symbolOpt?.Kind != SymbolKind.Local &&
                symbolOpt?.Kind != SymbolKind.Parameter)
            {
                return false;
            }

            // Workaround for https://github.com/dotnet/roslyn-analyzers/issues/1602
            if (instanceOpt != null && instanceOpt.Type == null)
            {
                return false;
            }

            PointsToAbstractValue instanceLocationOpt = null;
            AnalysisEntity parentOpt = null;
            if (instanceOpt?.Type != null)
            {
                if (instanceOpt.Type.IsValueType)
                {
                    if (TryCreate(instanceOpt, out var instanceEntityOpt) &&
                        instanceEntityOpt.Type.IsValueType)
                    {
                        parentOpt = instanceEntityOpt;
                        instanceLocationOpt = parentOpt.InstanceLocation;
                    }
                    else
                    {
                        // For value type allocations, we store the points to location.
                        var instancePointsToValue = _getPointsToAbstractValueOpt(instanceOpt);
                        if (!ReferenceEquals(instancePointsToValue, PointsToAbstractValue.NoLocation))
                        {
                            instanceLocationOpt = instancePointsToValue;
                        }
                    }

                    if (instanceLocationOpt == null)
                    {
                        return false;
                    }
                }
                else
                {
                    instanceLocationOpt = _getPointsToAbstractValueOpt(instanceOpt);
                }
            }

            analysisEntity = Create(symbolOpt, indices, type, instanceLocationOpt, parentOpt);
            return true;
        }

        private PointsToAbstractValue EnsureLocation(PointsToAbstractValue instanceLocationOpt, ISymbol symbolOpt, AnalysisEntity parentOpt)
        {
            if (instanceLocationOpt == null && symbolOpt != null)
            {
                Debug.Assert(symbolOpt.Kind == SymbolKind.Local || symbolOpt.Kind == SymbolKind.Parameter || symbolOpt.IsStatic || symbolOpt.IsLambdaOrLocalFunction());

                if (!_instanceLocationsForSymbols.TryGetValue(symbolOpt, out instanceLocationOpt))
                {
                    if (parentOpt != null)
                    {
                        instanceLocationOpt = parentOpt.InstanceLocation;
                    }
                    else
                    {
                        // Symbol instance location for locals and parameters should also include the interprocedural call stack because
                        // we might have recursive invocations to the same method and the symbol declarations
                        // from both the current and prior invocation of the method in the call stack should be distinct entities.
                        ImmutableStack<IOperation> interproceduralCallStackForSymbolDeclaration;
                        if (_interproceduralCallStackOpt != null &&
                            (symbolOpt.Kind == SymbolKind.Local || symbolOpt.Kind == SymbolKind.Parameter))
                        {
                            interproceduralCallStackForSymbolDeclaration = _getInterproceduralCallStackForOwningSymbol(symbolOpt.ContainingSymbol);
                        }
                        else
                        {
                            interproceduralCallStackForSymbolDeclaration = ImmutableStack<IOperation>.Empty;
                        }

                        var location = AbstractLocation.CreateSymbolLocation(symbolOpt, interproceduralCallStackForSymbolDeclaration);
                        instanceLocationOpt = PointsToAbstractValue.Create(location, mayBeNull: false);
                    }

                    _instanceLocationsForSymbols.Add(symbolOpt, instanceLocationOpt);
                }
            }

            return instanceLocationOpt;
        }

        private AnalysisEntity Create(ISymbol symbolOpt, ImmutableArray<AbstractIndex> indices, ITypeSymbol type, PointsToAbstractValue instanceLocationOpt, AnalysisEntity parentOpt)
        {
            instanceLocationOpt = EnsureLocation(instanceLocationOpt, symbolOpt, parentOpt);
            Debug.Assert(instanceLocationOpt != null);
            var analysisEntity = AnalysisEntity.Create(symbolOpt, indices, type, instanceLocationOpt, parentOpt);
            return analysisEntity;
        }

        public AnalysisEntity CreateWithNewInstanceRoot(AnalysisEntity analysisEntity, AnalysisEntity newRootInstance)
        {
            if (analysisEntity.InstanceLocation == newRootInstance.InstanceLocation &&
                analysisEntity.ParentOpt == newRootInstance.ParentOpt)
            {
                return analysisEntity;
            }

            if (analysisEntity.ParentOpt == null)
            {
                return newRootInstance;
            }

            AnalysisEntity parentOpt = CreateWithNewInstanceRoot(analysisEntity.ParentOpt, newRootInstance);
            return Create(analysisEntity.SymbolOpt, analysisEntity.Indices, analysisEntity.Type, newRootInstance.InstanceLocation, parentOpt);
        }
    }
}