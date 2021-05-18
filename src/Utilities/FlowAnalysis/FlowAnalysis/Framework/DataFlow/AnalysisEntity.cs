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
    /// <summary>
    /// <para>
    /// Primary entity for which analysis data is tracked by <see cref="DataFlowAnalysis"/>.
    /// </para>
    /// <para>
    /// The entity is based on one or more of the following:
    ///     1. An <see cref="ISymbol"/>.
    ///     2. One or more <see cref="AbstractIndex"/> indices to index into the parent key.
    ///     3. "this" or "Me" instance.
    ///     4. An allocation or an object creation.
    /// </para>
    /// <para>
    /// Each entity has:
    ///     1. An associated non-null <see cref="Type"/> and
    ///     2. A non-null <see cref="InstanceLocation"/> indicating the abstract location at which the entity is located and
    ///     3. An optional parent key if this key has the same <see cref="InstanceLocation"/> as the parent (i.e. parent is a value type).
    /// </para>
    /// </summary>
    public sealed class AnalysisEntity : CacheBasedEquatable<AnalysisEntity>
    {
        private readonly int _ignoringLocationHashCode;

        private AnalysisEntity(
            ISymbol? symbol,
            ImmutableArray<AbstractIndex> indices,
            SyntaxNode? instanceReferenceOperationSyntax,
            InterproceduralCaptureId? captureId,
            PointsToAbstractValue location,
            ITypeSymbol type,
            AnalysisEntity? parent,
            bool isThisOrMeInstance)
        {
            Debug.Assert(!indices.IsDefault);
            Debug.Assert(symbol != null || !indices.IsEmpty || instanceReferenceOperationSyntax != null || captureId.HasValue);
            Debug.Assert(parent == null || parent.Type.HasValueCopySemantics() || !indices.IsEmpty);

            Symbol = symbol;
            Indices = indices;
            InstanceReferenceOperationSyntax = instanceReferenceOperationSyntax;
            CaptureId = captureId;
            InstanceLocation = location;
            Type = type;
            Parent = parent;
            IsThisOrMeInstance = isThisOrMeInstance;

            _ignoringLocationHashCode = ComputeIgnoringLocationHashCode();
            EqualsIgnoringInstanceLocationId = _ignoringLocationHashCode;
        }

        private AnalysisEntity(ISymbol? symbol, ImmutableArray<AbstractIndex> indices, PointsToAbstractValue location, ITypeSymbol type, AnalysisEntity? parent)
            : this(symbol, indices, instanceReferenceOperationSyntax: null, captureId: null, location: location, type: type, parent: parent, isThisOrMeInstance: false)
        {
            Debug.Assert(symbol != null || !indices.IsEmpty);
        }

        private AnalysisEntity(IInstanceReferenceOperation instanceReferenceOperation, PointsToAbstractValue location)
            : this(symbol: null, indices: ImmutableArray<AbstractIndex>.Empty, instanceReferenceOperationSyntax: instanceReferenceOperation.Syntax,
                  captureId: null, location: location, type: instanceReferenceOperation.Type, parent: null, isThisOrMeInstance: false)
        {
            Debug.Assert(instanceReferenceOperation != null);
        }

        private AnalysisEntity(InterproceduralCaptureId captureId, ITypeSymbol capturedType, PointsToAbstractValue location)
            : this(symbol: null, indices: ImmutableArray<AbstractIndex>.Empty, instanceReferenceOperationSyntax: null,
                  captureId: captureId, location: location, type: capturedType, parent: null, isThisOrMeInstance: false)
        {
        }

        private AnalysisEntity(INamedTypeSymbol namedType, PointsToAbstractValue location, bool isThisOrMeInstance)
            : this(symbol: namedType, indices: ImmutableArray<AbstractIndex>.Empty, instanceReferenceOperationSyntax: null,
                  captureId: null, location: location, type: namedType, parent: null, isThisOrMeInstance: isThisOrMeInstance)
        {
        }

        public static AnalysisEntity Create(ISymbol? symbol, ImmutableArray<AbstractIndex> indices,
            ITypeSymbol type, PointsToAbstractValue instanceLocation, AnalysisEntity? parent)
        {
            Debug.Assert(symbol != null || !indices.IsEmpty);
            Debug.Assert(parent == null || parent.InstanceLocation == instanceLocation);

            return new AnalysisEntity(symbol, indices, instanceLocation, type, parent);
        }

        public static AnalysisEntity Create(IInstanceReferenceOperation instanceReferenceOperation, PointsToAbstractValue instanceLocation)
        {
            return new AnalysisEntity(instanceReferenceOperation, instanceLocation);
        }

        public static AnalysisEntity Create(
            InterproceduralCaptureId interproceduralCaptureId,
            ITypeSymbol type,
            PointsToAbstractValue instanceLocation)
        {
            return new AnalysisEntity(interproceduralCaptureId, type, instanceLocation);
        }

        public static AnalysisEntity CreateThisOrMeInstance(INamedTypeSymbol typeSymbol, PointsToAbstractValue instanceLocation)
        {
            Debug.Assert(instanceLocation.Locations.Count == 1);
            Debug.Assert(instanceLocation.Locations.Single().Creation == null);
            Debug.Assert(Equals(instanceLocation.Locations.Single().Symbol, typeSymbol));

            return new AnalysisEntity(typeSymbol, instanceLocation, isThisOrMeInstance: true);
        }

        public AnalysisEntity WithMergedInstanceLocation(AnalysisEntity analysisEntityToMerge)
        {
            Debug.Assert(EqualsIgnoringInstanceLocation(analysisEntityToMerge));
            Debug.Assert(!InstanceLocation.Equals(analysisEntityToMerge.InstanceLocation));

            var mergedInstanceLocation = PointsToAnalysis.PointsToAnalysis.PointsToAbstractValueDomainInstance.Merge(InstanceLocation, analysisEntityToMerge.InstanceLocation);
            return new AnalysisEntity(Symbol, Indices, InstanceReferenceOperationSyntax, CaptureId, mergedInstanceLocation, Type, Parent, IsThisOrMeInstance);
        }

        public bool IsChildOrInstanceMember
        {
            get
            {
                if (IsThisOrMeInstance)
                {
                    return false;
                }

                bool result;
                if (Symbol != null)
                {
                    result = Symbol.Kind != SymbolKind.Parameter &&
                        Symbol.Kind != SymbolKind.Local &&
                        !Symbol.IsStatic;
                }
                else if (!Indices.IsEmpty)
                {
                    result = true;
                }
                else
                {
                    result = false;
                }

                Debug.Assert(Parent == null || result);
                return result;
            }
        }

        internal bool IsChildOrInstanceMemberNeedingCompletePointsToAnalysis()
        {
            if (!IsChildOrInstanceMember)
            {
                return false;
            }

            // PERF: This is the core performance optimization for partial PointsToAnalysisKind.
            // We avoid tracking PointsToValues for all entities that are child or instance members,
            // except when they are fields or members of a value type (for example, tuple elements or struct members).
            return Parent == null || !Parent.Type.HasValueCopySemantics();
        }

        public bool HasConstantValue => Symbol switch
        {
            IFieldSymbol field => field.HasConstantValue,
            ILocalSymbol local => local.HasConstantValue,
            _ => false,
        };

        public ISymbol? Symbol { get; }
        public ImmutableArray<AbstractIndex> Indices { get; }
        public SyntaxNode? InstanceReferenceOperationSyntax { get; }
        public InterproceduralCaptureId? CaptureId { get; }
        public PointsToAbstractValue InstanceLocation { get; }
        public ITypeSymbol Type { get; }
        public AnalysisEntity? Parent { get; }
        public bool IsThisOrMeInstance { get; }

        public bool HasUnknownInstanceLocation => InstanceLocation.Kind switch
        {
            PointsToAbstractValueKind.Unknown
            or PointsToAbstractValueKind.UnknownNull
            or PointsToAbstractValueKind.UnknownNotNull => true,
            _ => false,
        };

        public bool IsLValueFlowCaptureEntity => CaptureId.HasValue && CaptureId.Value.IsLValueFlowCapture;

        public bool EqualsIgnoringInstanceLocation(AnalysisEntity? other)
        {
            // Perform fast equality checks first.
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other == null ||
                EqualsIgnoringInstanceLocationId != other.EqualsIgnoringInstanceLocationId)
            {
                return false;
            }

            // Now perform slow check that compares individual hash code parts sequences.
            return Symbol.GetHashCodeOrDefault() == other.Symbol.GetHashCodeOrDefault()
                && HashUtilities.Combine(Indices) == HashUtilities.Combine(other.Indices)
                && InstanceReferenceOperationSyntax.GetHashCodeOrDefault() == other.InstanceReferenceOperationSyntax.GetHashCodeOrDefault()
                && CaptureId.GetHashCodeOrDefault() == other.CaptureId.GetHashCodeOrDefault()
                && Type.GetHashCodeOrDefault() == other.Type.GetHashCodeOrDefault()
                && Parent.GetHashCodeOrDefault() == other.Parent.GetHashCodeOrDefault()
                && IsThisOrMeInstance.GetHashCode() == other.IsThisOrMeInstance.GetHashCode();
        }

        public int EqualsIgnoringInstanceLocationId { get; private set; }

        protected override void ComputeHashCodeParts(ref RoslynHashCode hashCode)
        {
            hashCode.Add(InstanceLocation.GetHashCode());
            ComputeHashCodePartsIgnoringLocation(ref hashCode);
        }

        protected override bool ComputeEqualsByHashCodeParts(CacheBasedEquatable<AnalysisEntity> obj)
        {
            var other = (AnalysisEntity)obj;
            return InstanceLocation.GetHashCode() == other.InstanceLocation.GetHashCode()
                && EqualsIgnoringInstanceLocation(other);
        }

        private void ComputeHashCodePartsIgnoringLocation(ref RoslynHashCode hashCode)
        {
            hashCode.Add(Symbol.GetHashCodeOrDefault());
            hashCode.Add(HashUtilities.Combine(Indices));
            hashCode.Add(InstanceReferenceOperationSyntax.GetHashCodeOrDefault());
            hashCode.Add(CaptureId.GetHashCodeOrDefault());
            hashCode.Add(Type.GetHashCode());
            hashCode.Add(Parent.GetHashCodeOrDefault());
            hashCode.Add(IsThisOrMeInstance.GetHashCode());
        }

        private int ComputeIgnoringLocationHashCode()
        {
            var hashCode = new RoslynHashCode();
            ComputeHashCodePartsIgnoringLocation(ref hashCode);
            return hashCode.ToHashCode();
        }

        public bool HasAncestor(AnalysisEntity ancestor)
        {
            AnalysisEntity? current = this.Parent;
            while (current != null)
            {
                if (current == ancestor)
                {
                    return true;
                }

                current = current.Parent;
            }

            return false;
        }

        internal bool IsCandidatePredicateEntity()
            => Type.SpecialType == SpecialType.System_Boolean ||
               Type.IsNullableOfBoolean() ||
               Type.Language == LanguageNames.VisualBasic && Type.SpecialType == SpecialType.System_Object;
    }
}