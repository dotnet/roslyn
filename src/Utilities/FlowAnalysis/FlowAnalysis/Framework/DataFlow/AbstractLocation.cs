// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// <para>
    /// Represents an abstract analysis location.
    /// This is may be used to represent a location where an <see cref="AnalysisEntity"/> resides, i.e. <see cref="AnalysisEntity.InstanceLocation"/> or
    /// a location that is pointed to by a reference type variable, and tracked with <see cref="PointsToAnalysis.PointsToAnalysis"/>.
    /// </para>
    /// <para>
    /// An analysis location can be created for one of the following cases:
    ///     1. An allocation or an object creation operation (<see cref="CreateAllocationLocation(IOperation, ITypeSymbol, PointsToAnalysisContext)"/>).
    ///     2. Location for the implicit 'this' or 'Me' instance being analyzed (<see cref="CreateThisOrMeLocation(INamedTypeSymbol, ImmutableStack{IOperation})"/>).
    ///     3. Location created for certain symbols which do not have a declaration in executable code, i.e. no <see cref="IOperation"/> for declaration (such as parameter symbols, member symbols, etc. - <see cref="CreateSymbolLocation(ISymbol, ImmutableStack{IOperation})"/>/>).
    ///     4. Location created for flow capture entities, i.e. for <see cref="InterproceduralCaptureId"/> created for <see cref="IFlowCaptureOperation"/> or <see cref="IFlowCaptureReferenceOperation"/>.
    ///        See <see cref="CreateFlowCaptureLocation(InterproceduralCaptureId, ITypeSymbol, ImmutableStack{IOperation})"/>
    /// </para>
    /// </summary>
    public sealed class AbstractLocation : CacheBasedEquatable<AbstractLocation>
    {
        private readonly bool _isSpecialSingleton;
        public static readonly AbstractLocation Null = new AbstractLocation(creationOpt: null, creationCallStackOpt: null, analysisEntityOpt: null, symbolOpt: null, captureIdOpt: null, locationTypeOpt: null, isSpecialSingleton: true);
        public static readonly AbstractLocation NoLocation = new AbstractLocation(creationOpt: null, creationCallStackOpt: null, analysisEntityOpt: null, symbolOpt: null, captureIdOpt: null, locationTypeOpt: null, isSpecialSingleton: true);

        private AbstractLocation(IOperation creationOpt, ImmutableStack<IOperation> creationCallStackOpt, AnalysisEntity analysisEntityOpt, ISymbol symbolOpt, InterproceduralCaptureId? captureIdOpt, ITypeSymbol locationTypeOpt, bool isSpecialSingleton)
        {
            Debug.Assert(isSpecialSingleton ^ (locationTypeOpt != null));

            CreationOpt = creationOpt;
            CreationCallStack = creationCallStackOpt ?? ImmutableStack<IOperation>.Empty;
            AnalysisEntityOpt = analysisEntityOpt;
            SymbolOpt = symbolOpt;
            CaptureIdOpt = captureIdOpt;
            LocationTypeOpt = locationTypeOpt;
            _isSpecialSingleton = isSpecialSingleton;
        }

        private static AbstractLocation Create(IOperation creationOpt, ImmutableStack<IOperation> creationCallStackOpt, AnalysisEntity analysisEntityOpt, ISymbol symbolOpt, InterproceduralCaptureId? captureIdOpt, ITypeSymbol locationType)
        {
            Debug.Assert(creationOpt != null ^ symbolOpt != null ^ analysisEntityOpt != null ^ captureIdOpt != null);
            Debug.Assert(locationType != null);

            return new AbstractLocation(creationOpt, creationCallStackOpt, analysisEntityOpt, symbolOpt, captureIdOpt, locationType, isSpecialSingleton: false);
        }

        public static AbstractLocation CreateAllocationLocation(IOperation creation, ITypeSymbol locationType, PointsToAnalysisContext analysisContext)
            => CreateAllocationLocation(creation, locationType, analysisContext.InterproceduralAnalysisDataOpt?.CallStack);
        internal static AbstractLocation CreateAllocationLocation(IOperation creation, ITypeSymbol locationType, ImmutableStack<IOperation> callStackOpt)
            => Create(creation, callStackOpt, analysisEntityOpt: null, symbolOpt: null, captureIdOpt: null, locationType: locationType);
        public static AbstractLocation CreateAnalysisEntityDefaultLocation(AnalysisEntity analysisEntity)
            => Create(creationOpt: null, creationCallStackOpt: null, analysisEntityOpt: analysisEntity, symbolOpt: null, captureIdOpt: null, locationType: analysisEntity.Type);
        public static AbstractLocation CreateThisOrMeLocation(INamedTypeSymbol namedTypeSymbol, ImmutableStack<IOperation> creationCallStackOpt)
            => Create(creationOpt: null, creationCallStackOpt: creationCallStackOpt, analysisEntityOpt: null, symbolOpt: namedTypeSymbol, captureIdOpt: null, locationType: namedTypeSymbol);
        public static AbstractLocation CreateSymbolLocation(ISymbol symbol, ImmutableStack<IOperation> creationCallStackOpt)
            => Create(creationOpt: null, creationCallStackOpt: creationCallStackOpt, analysisEntityOpt: null, symbolOpt: symbol, captureIdOpt: null, locationType: symbol.GetMemberOrLocalOrParameterType());
        public static AbstractLocation CreateFlowCaptureLocation(InterproceduralCaptureId captureId, ITypeSymbol locationType, ImmutableStack<IOperation> creationCallStackOpt)
            => Create(creationOpt: null, creationCallStackOpt: creationCallStackOpt, analysisEntityOpt: null, symbolOpt: null, captureIdOpt: captureId, locationType: locationType);

        public IOperation CreationOpt { get; }
        public ImmutableStack<IOperation> CreationCallStack { get; }

        /// <summary>
        /// Returns the top of <see cref="CreationCallStack"/> if this location was created through an interprocedural method invocation, i.e. <see cref="CreationCallStack"/> is non-empty.
        /// Otherwise, returns <see cref="CreationOpt"/>.
        /// </summary>
        public IOperation GetTopOfCreationCallStackOrCreation()
        {
            if (CreationCallStack.IsEmpty)
            {
                return CreationOpt;
            }

            return CreationCallStack.Peek();
        }

        public AnalysisEntity AnalysisEntityOpt { get; }
        public ISymbol SymbolOpt { get; }
        public InterproceduralCaptureId? CaptureIdOpt { get; }
        public ITypeSymbol LocationTypeOpt { get; }
        public bool IsNull => ReferenceEquals(this, Null);
        public bool IsNoLocation => ReferenceEquals(this, NoLocation);

        /// <summary>
        /// Indicates this represents the initial unknown but distinct location for an analysis entity.
        /// </summary>
        public bool IsAnalysisEntityDefaultLocation => AnalysisEntityOpt != null;

        protected override void ComputeHashCodeParts(Action<int> addPart)
        {
            addPart(CreationOpt.GetHashCodeOrDefault());
            addPart(HashUtilities.Combine(CreationCallStack));
            addPart(SymbolOpt.GetHashCodeOrDefault());
            addPart(CaptureIdOpt.GetHashCodeOrDefault());
            addPart(AnalysisEntityOpt.GetHashCodeOrDefault());
            addPart(LocationTypeOpt.GetHashCodeOrDefault());
            addPart(_isSpecialSingleton.GetHashCode());
            addPart(IsNull.GetHashCode());
        }

        /// <summary>
        /// Attempts to get the syntax node to report diagnostic for this abstract location 
        /// Returns null if the location is owned by another method invoked through interprocedural analysis.
        /// </summary>
        public SyntaxNode TryGetNodeToReportDiagnostic(PointsToAnalysisResult pointsToAnalysisResultOpt)
        {
            Debug.Assert(CreationOpt != null);

            if (pointsToAnalysisResultOpt != null)
            {
                // Attempt to report diagnostic at the bottommost stack frame that owns the location.
                foreach (var creation in CreationCallStack)
                {
                    var syntaxNode = TryGetSyntaxNodeToReportDiagnostic(creation);
                    if (syntaxNode != null)
                    {
                        return syntaxNode;
                    }

                    if (!(creation is IInvocationOperation invocation) ||
                        !invocation.TargetMethod.IsLambdaOrLocalFunctionOrDelegate())
                    {
                        return null;
                    }
                }
            }

            // Fallback to reporting the diagnostic on the allocation location.
            return CreationOpt.Syntax;

            // Local functions.
            SyntaxNode TryGetSyntaxNodeToReportDiagnostic(IOperation creation)
            {
                // If any of the argument to creation points to this location, then use the argument.
                var arguments = creation switch
                {
                    IInvocationOperation invocation => invocation.Arguments,

                    IObjectCreationOperation objectCreation => objectCreation.Arguments,

                    _ => ImmutableArray<IArgumentOperation>.Empty,
                };

                foreach (var argument in arguments)
                {
                    var syntaxNode = TryGetSyntaxNodeToReportDiagnosticCore(argument);
                    if (syntaxNode != null)
                    {
                        return syntaxNode;
                    }
                }

                return TryGetSyntaxNodeToReportDiagnosticCore(creation);

                SyntaxNode TryGetSyntaxNodeToReportDiagnosticCore(IOperation operation)
                {
                    var pointsToValue = pointsToAnalysisResultOpt[operation];
                    return TryGetSyntaxNodeToReportDiagnosticForPointsValue(pointsToValue, operation);

                    SyntaxNode TryGetSyntaxNodeToReportDiagnosticForPointsValue(PointsToAbstractValue pointsToValue, IOperation operation)
                    {
                        foreach (var location in pointsToValue.Locations)
                        {
                            if (location == this)
                            {
                                return operation.Syntax;
                            }
                        }

                        if (pointsToAnalysisResultOpt.TaskWrappedValuesMapOpt != null &&
                            pointsToAnalysisResultOpt.TaskWrappedValuesMapOpt.TryGetValue(pointsToValue, out var wrappedValue))
                        {
                            return TryGetSyntaxNodeToReportDiagnosticForPointsValue(wrappedValue, operation);
                        }

                        return null;
                    }
                }
            }
        }
    }
}
