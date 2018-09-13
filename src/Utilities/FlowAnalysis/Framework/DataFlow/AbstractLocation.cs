// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    using PointsToAnalysisResult = DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue>;

    /// <summary>
    /// <para>
    /// Represents an abstract analysis location.
    /// This is may be used to represent a location where an <see cref="AnalysisEntity"/> resides, i.e. <see cref="AnalysisEntity.InstanceLocation"/> or
    /// a location that is pointed to by a reference type variable, and tracked with <see cref="PointsToAnalysis.PointsToAnalysis"/>.
    /// </para>
    /// <para>
    /// An analysis location can be created for one of the following cases:
    ///     1. An allocation or an object creation operation (<see cref="CreateAllocationLocation(IOperation, ITypeSymbol)"/>).
    ///     2. Location for the implicit 'this' or 'Me' instance being analyzed (<see cref="CreateThisOrMeLocation(INamedTypeSymbol)"/>).
    ///     3. Location created for certain symbols which do not have a declaration in executable code, i.e. no <see cref="IOperation"/> for declaration (such as parameter symbols, member symbols, etc. - <see cref="CreateSymbolLocation(ISymbol)"/>).
    /// </para>
    /// </summary>
    internal sealed class AbstractLocation : CacheBasedEquatable<AbstractLocation>
    {
        private readonly bool _isSpecialSingleton;
        public static readonly AbstractLocation Null = new AbstractLocation(creationOpt: null, creationCallStackOpt: null, analysisEntityOpt: null, symbolOpt: null, locationTypeOpt: null, isSpecialSingleton: true);
        public static readonly AbstractLocation NoLocation = new AbstractLocation(creationOpt: null, creationCallStackOpt: null, analysisEntityOpt: null, symbolOpt: null, locationTypeOpt: null, isSpecialSingleton: true);
        
        private AbstractLocation(IOperation creationOpt, ImmutableStack<IOperation> creationCallStackOpt, AnalysisEntity analysisEntityOpt, ISymbol symbolOpt, ITypeSymbol locationTypeOpt, bool isSpecialSingleton)
        {
            Debug.Assert(isSpecialSingleton ^ (locationTypeOpt != null));

            CreationOpt = creationOpt;
            CreationCallStack = creationCallStackOpt ?? ImmutableStack<IOperation>.Empty;
            AnalysisEntityOpt = analysisEntityOpt;
            SymbolOpt = symbolOpt;
            LocationTypeOpt = locationTypeOpt;
            _isSpecialSingleton = isSpecialSingleton;
        }

        private static AbstractLocation Create(IOperation creationOpt, ImmutableStack<IOperation> creationCallStackOpt, AnalysisEntity analysisEntityOpt, ISymbol symbolOpt, ITypeSymbol locationType)
        {
            Debug.Assert(creationOpt != null ^ symbolOpt != null ^ analysisEntityOpt != null);
            Debug.Assert(locationType != null);

            return new AbstractLocation(creationOpt, creationCallStackOpt, analysisEntityOpt, symbolOpt, locationType, isSpecialSingleton: false);
        }

        public static AbstractLocation CreateAllocationLocation(IOperation creation, ITypeSymbol locationType, PointsToAnalysisContext analysisContext)
            => Create(creation, analysisContext.InterproceduralAnalysisDataOpt?.CallStack, analysisEntityOpt: null, symbolOpt: null, locationType: locationType);
        public static AbstractLocation CreateAnalysisEntityDefaultLocation(AnalysisEntity analysisEntity)
            => Create(creationOpt: null, creationCallStackOpt: null, analysisEntityOpt: analysisEntity, symbolOpt: null, locationType: analysisEntity.Type);
        public static AbstractLocation CreateThisOrMeLocation(INamedTypeSymbol namedTypeSymbol)
            => Create(creationOpt: null, creationCallStackOpt: null, analysisEntityOpt: null, symbolOpt: namedTypeSymbol, locationType: namedTypeSymbol);
        public static AbstractLocation CreateSymbolLocation(ISymbol symbol, ImmutableStack<IOperation> creationCallStackOpt)
            => Create(creationOpt: null, creationCallStackOpt: creationCallStackOpt, analysisEntityOpt: null, symbolOpt: symbol, locationType: symbol.GetMemerOrLocalOrParameterType());

        public IOperation CreationOpt { get; }
        public ImmutableStack<IOperation> CreationCallStack { get; }
        public AnalysisEntity AnalysisEntityOpt { get; }
        public ISymbol SymbolOpt { get; }
        public ITypeSymbol LocationTypeOpt { get; }
        public bool IsNull => ReferenceEquals(this, Null);
        public bool IsNoLocation => ReferenceEquals(this, NoLocation);

        protected override int ComputeHashCode()
            => HashUtilities.Combine(CreationOpt?.GetHashCode() ?? 0,
               HashUtilities.Combine(CreationCallStack,
               HashUtilities.Combine(SymbolOpt?.GetHashCode() ?? 0,
               HashUtilities.Combine(AnalysisEntityOpt?.GetHashCode() ?? 0,
               HashUtilities.Combine(LocationTypeOpt?.GetHashCode() ?? 0,
               HashUtilities.Combine(_isSpecialSingleton.GetHashCode(), IsNull.GetHashCode()))))));

        public SyntaxNode GetNodeToReportDiagnostic(PointsToAnalysisResult pointsToAnalysisResultOpt)
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
                }
            }

            // Fallback to reporting the diagnostic on the allocation location.
            return CreationOpt.Syntax;

            // Local functions.
            SyntaxNode TryGetSyntaxNodeToReportDiagnostic(IOperation creation)
            {
                // If any of the argument to creation points to this location, then use the argument.
                ImmutableArray<IArgumentOperation> arguments;
                switch (creation)
                {
                    case IInvocationOperation invocation:
                        arguments = invocation.Arguments;
                        break;

                    case IObjectCreationOperation objectCreation:
                        arguments = objectCreation.Arguments;
                        break;

                    default:
                        arguments = ImmutableArray<IArgumentOperation>.Empty;
                        break;
                }

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
                    foreach (var location in pointsToValue.Locations)
                    {
                        if (location == this)
                        {
                            return operation.Syntax;
                        }
                    }

                    return null;
                }
            }
        }
    }
}
