// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#nullable enable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.RuntimeMembers;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal abstract partial class NamedTypeSymbol
    {
        internal const int RestPosition = 8; // The Rest field is in 8th position
        internal const int RestIndex = RestPosition - 1;
        internal const string TupleTypeName = "ValueTuple";
        internal const string RestFieldName = "Rest";

        protected TupleUncommonData? _lazyTupleData; // Could be marked as initonly once we have such a feature

        /// <summary>
        /// Helps create a tuple type from source.
        /// </summary>
        internal static NamedTypeSymbol CreateTuple(
            Location? locationOpt,
            ImmutableArray<TypeWithAnnotations> elementTypesWithAnnotations,
            ImmutableArray<Location> elementLocations,
            ImmutableArray<string> elementNames,
            CSharpCompilation compilation,
            bool shouldCheckConstraints,
            bool includeNullability,
            ImmutableArray<bool> errorPositions,
            CSharpSyntaxNode? syntax = null,
            DiagnosticBag? diagnostics = null)
        {
            Debug.Assert(!shouldCheckConstraints || syntax is object);
            Debug.Assert(elementNames.IsDefault || elementTypesWithAnnotations.Length == elementNames.Length);
            Debug.Assert(!includeNullability || shouldCheckConstraints);

            int numElements = elementTypesWithAnnotations.Length;

            if (numElements <= 1)
            {
                throw ExceptionUtilities.Unreachable;
            }

            NamedTypeSymbol underlyingType = GetTupleUnderlyingType(elementTypesWithAnnotations, syntax, compilation, diagnostics);

            if (numElements >= RestPosition && diagnostics != null && !underlyingType.IsErrorType())
            {
                WellKnownMember wellKnownTupleRest = GetTupleTypeMember(RestPosition, RestPosition);
                _ = GetWellKnownMemberInType(underlyingType.OriginalDefinition, wellKnownTupleRest, diagnostics, syntax);
            }

            if (diagnostics != null && ((SourceModuleSymbol)compilation.SourceModule).AnyReferencedAssembliesAreLinked)
            {
                // Complain about unembeddable types from linked assemblies.
                Emit.NoPia.EmbeddedTypesManager.IsValidEmbeddableType(underlyingType, syntax, diagnostics);
            }

            var locations = locationOpt is null ? ImmutableArray<Location>.Empty : ImmutableArray.Create(locationOpt);
            var constructedType = CreateTuple(underlyingType, elementNames, errorPositions, elementLocations, locations);
            if (shouldCheckConstraints && diagnostics != null)
            {
                constructedType.CheckConstraints(compilation.Conversions, includeNullability, syntax, elementLocations, compilation, diagnostics, diagnostics);
            }

            return constructedType;
        }

        public static NamedTypeSymbol CreateTuple(
            NamedTypeSymbol tupleCompatibleType,
            ImmutableArray<string> elementNames = default,
            ImmutableArray<bool> errorPositions = default,
            ImmutableArray<Location> elementLocations = default,
            ImmutableArray<Location> locations = default)
        {
            Debug.Assert(tupleCompatibleType.IsTupleType);
            return tupleCompatibleType.WithElementNames(elementNames, elementLocations, errorPositions, locations);
        }

        internal NamedTypeSymbol WithTupleDataFrom(NamedTypeSymbol original)
        {
            if (!IsTupleType || (original._lazyTupleData == null && this._lazyTupleData == null) || TupleData!.EqualsIgnoringTupleUnderlyingType(original.TupleData))
            {
                return this;
            }

            return WithElementNames(original.TupleElementNames, original.TupleElementLocations, original.TupleErrorPositions, original.Locations);
        }

        internal NamedTypeSymbol? TupleUnderlyingType
            => this._lazyTupleData != null ? this.TupleData!.TupleUnderlyingType : (this.IsTupleType ? this : null);

        /// <summary>
        /// Adjust the <paramref name="tupleCompatibleType"/> in such a way so that
        /// all types used by Rest fields are tuples. Throughout the entire nesting chain.
        /// </summary>
        private static NamedTypeSymbol EnsureRestExtensionsAreTuples(NamedTypeSymbol tupleCompatibleType)
        {
            if (!tupleCompatibleType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[RestPosition - 1].Type.IsTupleType)
            {
                var nonTupleTypeChain = ArrayBuilder<NamedTypeSymbol>.GetInstance();

                NamedTypeSymbol currentType = tupleCompatibleType;
                do
                {
                    nonTupleTypeChain.Add(currentType);
                    currentType = (NamedTypeSymbol)currentType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[RestPosition - 1].Type;
                }
                while (currentType.Arity == RestPosition);

                if (!currentType.IsTupleType)
                {
                    nonTupleTypeChain.Add(currentType);
                }

                Debug.Assert(nonTupleTypeChain.Count > 1);
                tupleCompatibleType = nonTupleTypeChain.Pop();

                do
                {
                    var extensionTuple = CreateTuple(tupleCompatibleType);
                    tupleCompatibleType = nonTupleTypeChain.Pop();

                    tupleCompatibleType = ReplaceRestExtensionType(tupleCompatibleType, extensionTuple);
                }
                while (nonTupleTypeChain.Count != 0);

                nonTupleTypeChain.Free();
            }

            return tupleCompatibleType;
        }

        private static NamedTypeSymbol ReplaceRestExtensionType(NamedTypeSymbol tupleCompatibleType, NamedTypeSymbol extensionTuple)
        {
            Debug.Assert(extensionTuple.IsTupleType);
            var typeArgumentsBuilder = ArrayBuilder<TypeWithAnnotations>.GetInstance(RestPosition);
            var arguments = tupleCompatibleType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics;
            typeArgumentsBuilder.AddRange(arguments, RestPosition - 1);
            typeArgumentsBuilder.Add(TypeWithAnnotations.Create(extensionTuple, customModifiers: arguments[RestPosition - 1].CustomModifiers));

            tupleCompatibleType = tupleCompatibleType.ConstructedFrom.Construct(typeArgumentsBuilder.ToImmutableAndFree(), unbound: false);
            return tupleCompatibleType;
        }

        /// <summary>
        /// Copy this tuple, but modify it to use the new element types.
        /// </summary>
        internal NamedTypeSymbol WithElementTypes(ImmutableArray<TypeWithAnnotations> newElementTypes)
        {
            Debug.Assert(TupleElementTypesWithAnnotations.Length == newElementTypes.Length);
            Debug.Assert(newElementTypes.All(t => t.HasType));

            NamedTypeSymbol firstTupleType;
            NamedTypeSymbol? chainedTupleType;
            if (Arity < NamedTypeSymbol.RestPosition)
            {
                firstTupleType = OriginalDefinition;
                chainedTupleType = null;
            }
            else
            {
                chainedTupleType = OriginalDefinition;
                var underlyingType = this;
                do
                {
                    underlyingType = ((NamedTypeSymbol)underlyingType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[NamedTypeSymbol.RestIndex].Type);
                }
                while (underlyingType.Arity >= NamedTypeSymbol.RestPosition);

                firstTupleType = underlyingType.OriginalDefinition;
            }
            return CreateTuple(
                ConstructTupleUnderlyingType(firstTupleType, chainedTupleType, newElementTypes),
                elementNames: TupleElementNames, elementLocations: TupleElementLocations, errorPositions: TupleErrorPositions, locations: Locations);
        }

        /// <summary>
        /// Copy this tuple, but modify it to use the new element names.
        /// Also applies new location of the whole tuple as well as each element.
        /// Drops the inferred positions.
        /// </summary>
        internal NamedTypeSymbol WithElementNames(ImmutableArray<string> newElementNames,
                                                  ImmutableArray<Location> newElementLocations,
                                                  ImmutableArray<bool> errorPositions,
                                                  ImmutableArray<Location> locations)
        {
            Debug.Assert(IsTupleType);
            Debug.Assert(newElementNames.IsDefault || this.TupleElementTypesWithAnnotations.Length == newElementNames.Length);
            return WithTupleData(new TupleUncommonData(this.TupleUnderlyingType!, newElementNames, newElementLocations, errorPositions, locations));
        }

        private NamedTypeSymbol WithTupleData(TupleUncommonData newData)
        {
            Debug.Assert(IsTupleType);

            if (newData.EqualsIgnoringTupleUnderlyingType(TupleData))
            {
                return this;
            }

            if (this.IsDefinition)
            {
                if (newData.ElementNames.IsDefault)
                {
                    // We don't want to modify the definition unless we're adding names
                    return this;
                }

                // This is for the rare case of making a tuple with names inside the definition of a ValueTuple type
                return this.Construct(this.GetTypeParametersAsTypeArguments(), unbound: false, constructWithTypeParameters: true).WithTupleData(newData);
            }

            return WithTupleDataCore(newData);
        }

        protected abstract NamedTypeSymbol WithTupleDataCore(TupleUncommonData newData);

        /// <summary>
        /// Decompose the underlying tuple type into its links and store them into the underlyingTupleTypeChain.
        ///
        /// For instance, ValueTuple&lt;..., ValueTuple&lt; int >> (the underlying type for an 8-tuple)
        /// will be decomposed into two links: the first one is the entire thing, and the second one is the ValueTuple&lt; int >
        /// </summary>
        internal static void GetUnderlyingTypeChain(NamedTypeSymbol underlyingTupleType, ArrayBuilder<NamedTypeSymbol> underlyingTupleTypeChain)
        {
            NamedTypeSymbol currentType = underlyingTupleType;

            while (true)
            {
                underlyingTupleTypeChain.Add(currentType);
                if (currentType.Arity == NamedTypeSymbol.RestPosition)
                {
                    currentType = (NamedTypeSymbol)currentType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[NamedTypeSymbol.RestPosition - 1].Type;
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Gets flattened type arguments of the underlying type
        /// which correspond to the types of the tuple elements left-to-right
        /// </summary>
        internal static void AddElementTypes(NamedTypeSymbol underlyingTupleType, ArrayBuilder<TypeWithAnnotations> tupleElementTypes)
        {
            NamedTypeSymbol currentType = underlyingTupleType;

            while (true)
            {
                if (currentType.IsTupleType)
                {
                    tupleElementTypes.AddRange(currentType.TupleElementTypesWithAnnotations);
                    break;
                }

                var regularElements = Math.Min(currentType.Arity, NamedTypeSymbol.RestPosition - 1);
                tupleElementTypes.AddRange(currentType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics, regularElements);

                if (currentType.Arity == NamedTypeSymbol.RestPosition)
                {
                    currentType = (NamedTypeSymbol)currentType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[NamedTypeSymbol.RestPosition - 1].Type;
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Returns the nested type at a certain depth.
        ///
        /// For depth=0, just return the tuple type as-is.
        /// For depth=1, returns the nested tuple type at position 8.
        /// </summary>
        private static NamedTypeSymbol GetNestedTupleUnderlyingType(NamedTypeSymbol topLevelUnderlyingType, int depth)
        {
            NamedTypeSymbol found = topLevelUnderlyingType;
            for (int i = 0; i < depth; i++)
            {
                found = (NamedTypeSymbol)found.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[RestPosition - 1].Type;
            }

            return found;
        }

        /// <summary>
        /// Returns the number of nestings required to represent numElements as nested ValueTuples.
        /// For example, for 8 elements, you need 2 ValueTuples and the remainder (ie the size of the last nested ValueTuple) is 1.
        /// </summary>
        private static int NumberOfValueTuples(int numElements, out int remainder)
        {
            remainder = (numElements - 1) % (RestPosition - 1) + 1;
            return (numElements - 1) / (RestPosition - 1) + 1;
        }

        /// <summary>
        /// Produces the underlying ValueTuple corresponding to this list of element types.
        ///
        /// Pass a null diagnostic bag and syntax node if you don't care about diagnostics.
        /// </summary>
        private static NamedTypeSymbol GetTupleUnderlyingType(ImmutableArray<TypeWithAnnotations> elementTypes, CSharpSyntaxNode? syntax, CSharpCompilation compilation, DiagnosticBag? diagnostics)
        {
            int numElements = elementTypes.Length;
            int remainder;
            int chainLength = NumberOfValueTuples(numElements, out remainder);

            NamedTypeSymbol firstTupleType = compilation.GetWellKnownType(GetTupleType(remainder));
            if (diagnostics is object && syntax is object)
            {
                ReportUseSiteAndObsoleteDiagnostics(syntax, diagnostics, firstTupleType);
            }

            NamedTypeSymbol? chainedTupleType = null;
            if (chainLength > 1)
            {
                chainedTupleType = compilation.GetWellKnownType(GetTupleType(RestPosition));
                if (diagnostics is object && syntax is object)
                {
                    ReportUseSiteAndObsoleteDiagnostics(syntax, diagnostics, chainedTupleType);
                }
            }

            return ConstructTupleUnderlyingType(firstTupleType, chainedTupleType, elementTypes);
        }

        private static NamedTypeSymbol ConstructTupleUnderlyingType(NamedTypeSymbol firstTupleType, NamedTypeSymbol? chainedTupleTypeOpt, ImmutableArray<TypeWithAnnotations> elementTypes)
        {
            Debug.Assert(chainedTupleTypeOpt is null == elementTypes.Length < RestPosition);

            int numElements = elementTypes.Length;
            int remainder;
            int chainLength = NumberOfValueTuples(numElements, out remainder);

            NamedTypeSymbol currentSymbol = firstTupleType.Construct(ImmutableArray.Create(elementTypes, (chainLength - 1) * (RestPosition - 1), remainder));
            int loop = chainLength - 1;
            while (loop > 0)
            {
                var chainedTypes = ImmutableArray.Create(elementTypes, (loop - 1) * (RestPosition - 1), RestPosition - 1).Add(TypeWithAnnotations.Create(currentSymbol));
                currentSymbol = chainedTupleTypeOpt!.Construct(chainedTypes);
                loop--;
            }

            return currentSymbol;
        }

        private static void ReportUseSiteAndObsoleteDiagnostics(CSharpSyntaxNode? syntax, DiagnosticBag? diagnostics, NamedTypeSymbol firstTupleType)
        {
            Binder.ReportUseSiteDiagnostics(firstTupleType, diagnostics, syntax);
            Binder.ReportDiagnosticsIfObsoleteInternal(diagnostics, firstTupleType, syntax, firstTupleType.ContainingType, BinderFlags.None);
        }

        /// <summary>
        /// For tuples with no natural type, we still need to verify that an underlying type of proper arity exists, and report if otherwise.
        /// </summary>
        internal static void VerifyTupleTypePresent(int cardinality, CSharpSyntaxNode? syntax, CSharpCompilation compilation, DiagnosticBag? diagnostics)
        {
            Debug.Assert(diagnostics is object && syntax is object);

            int remainder;
            int chainLength = NumberOfValueTuples(cardinality, out remainder);

            NamedTypeSymbol firstTupleType = compilation.GetWellKnownType(GetTupleType(remainder));
            ReportUseSiteAndObsoleteDiagnostics(syntax, diagnostics, firstTupleType);

            if (chainLength > 1)
            {
                NamedTypeSymbol chainedTupleType = compilation.GetWellKnownType(GetTupleType(RestPosition));
                ReportUseSiteAndObsoleteDiagnostics(syntax, diagnostics, chainedTupleType);
            }
        }

        internal static void ReportTupleNamesMismatchesIfAny(TypeSymbol destination, BoundTupleLiteral literal, DiagnosticBag diagnostics)
        {
            var sourceNames = literal.ArgumentNamesOpt;
            if (sourceNames.IsDefault)
            {
                return;
            }

            ImmutableArray<bool> inferredNames = literal.InferredNamesOpt;
            bool noInferredNames = inferredNames.IsDefault;
            ImmutableArray<string> destinationNames = destination.TupleElementNames;
            int sourceLength = sourceNames.Length;
            bool allMissing = destinationNames.IsDefault;
            Debug.Assert(allMissing || destinationNames.Length == sourceLength);

            for (int i = 0; i < sourceLength; i++)
            {
                var sourceName = sourceNames[i];
                var wasInferred = noInferredNames ? false : inferredNames[i];

                if (sourceName != null && !wasInferred && (allMissing || string.CompareOrdinal(destinationNames[i], sourceName) != 0))
                {
                    diagnostics.Add(ErrorCode.WRN_TupleLiteralNameMismatch, literal.Arguments[i].Syntax.Parent.Location, sourceName, destination);
                }
            }
        }

        /// <summary>
        /// Find the well-known ValueTuple type of a given arity.
        /// For example, for arity=2:
        /// returns WellKnownType.System_ValueTuple_T2
        /// </summary>
        private static WellKnownType GetTupleType(int arity)
        {
            if (arity > RestPosition)
            {
                throw ExceptionUtilities.Unreachable;
            }
            return tupleTypes[arity - 1];
        }

        private static readonly WellKnownType[] tupleTypes = {
                                                                    WellKnownType.System_ValueTuple_T1,
                                                                    WellKnownType.System_ValueTuple_T2,
                                                                    WellKnownType.System_ValueTuple_T3,
                                                                    WellKnownType.System_ValueTuple_T4,
                                                                    WellKnownType.System_ValueTuple_T5,
                                                                    WellKnownType.System_ValueTuple_T6,
                                                                    WellKnownType.System_ValueTuple_T7,
                                                                    WellKnownType.System_ValueTuple_TRest };

        /// <summary>
        /// Find the constructor for a well-known ValueTuple type of a given arity.
        ///
        /// For example, for arity=2:
        /// returns WellKnownMember.System_ValueTuple_T2__ctor
        ///
        /// For arity=12:
        /// return System_ValueTuple_TRest__ctor
        /// </summary>
        internal static WellKnownMember GetTupleCtor(int arity)
        {
            if (arity > 8)
            {
                throw ExceptionUtilities.Unreachable;
            }
            return tupleCtors[arity - 1];
        }

        private static readonly WellKnownMember[] tupleCtors = {
                                                                    WellKnownMember.System_ValueTuple_T1__ctor,
                                                                    WellKnownMember.System_ValueTuple_T2__ctor,
                                                                    WellKnownMember.System_ValueTuple_T3__ctor,
                                                                    WellKnownMember.System_ValueTuple_T4__ctor,
                                                                    WellKnownMember.System_ValueTuple_T5__ctor,
                                                                    WellKnownMember.System_ValueTuple_T6__ctor,
                                                                    WellKnownMember.System_ValueTuple_T7__ctor,
                                                                    WellKnownMember.System_ValueTuple_TRest__ctor };

        /// <summary>
        /// Find the well-known members to the ValueTuple type of a given arity and position.
        /// For example, for arity=3 and position=1:
        /// returns WellKnownMember.System_ValueTuple_T3__Item1
        /// </summary>
        internal static WellKnownMember GetTupleTypeMember(int arity, int position)
        {
            return tupleMembers[arity - 1][position - 1];
        }

        private static readonly WellKnownMember[][] tupleMembers = new[]{
                                                                new[]{
                                                                    WellKnownMember.System_ValueTuple_T1__Item1 },

                                                                new[]{
                                                                    WellKnownMember.System_ValueTuple_T2__Item1,
                                                                    WellKnownMember.System_ValueTuple_T2__Item2 },

                                                                new[]{
                                                                    WellKnownMember.System_ValueTuple_T3__Item1,
                                                                    WellKnownMember.System_ValueTuple_T3__Item2,
                                                                    WellKnownMember.System_ValueTuple_T3__Item3 },

                                                                new[]{
                                                                    WellKnownMember.System_ValueTuple_T4__Item1,
                                                                    WellKnownMember.System_ValueTuple_T4__Item2,
                                                                    WellKnownMember.System_ValueTuple_T4__Item3,
                                                                    WellKnownMember.System_ValueTuple_T4__Item4 },

                                                                new[]{
                                                                    WellKnownMember.System_ValueTuple_T5__Item1,
                                                                    WellKnownMember.System_ValueTuple_T5__Item2,
                                                                    WellKnownMember.System_ValueTuple_T5__Item3,
                                                                    WellKnownMember.System_ValueTuple_T5__Item4,
                                                                    WellKnownMember.System_ValueTuple_T5__Item5 },

                                                                new[]{
                                                                    WellKnownMember.System_ValueTuple_T6__Item1,
                                                                    WellKnownMember.System_ValueTuple_T6__Item2,
                                                                    WellKnownMember.System_ValueTuple_T6__Item3,
                                                                    WellKnownMember.System_ValueTuple_T6__Item4,
                                                                    WellKnownMember.System_ValueTuple_T6__Item5,
                                                                    WellKnownMember.System_ValueTuple_T6__Item6 },

                                                                new[]{
                                                                    WellKnownMember.System_ValueTuple_T7__Item1,
                                                                    WellKnownMember.System_ValueTuple_T7__Item2,
                                                                    WellKnownMember.System_ValueTuple_T7__Item3,
                                                                    WellKnownMember.System_ValueTuple_T7__Item4,
                                                                    WellKnownMember.System_ValueTuple_T7__Item5,
                                                                    WellKnownMember.System_ValueTuple_T7__Item6,
                                                                    WellKnownMember.System_ValueTuple_T7__Item7 },

                                                                new[]{
                                                                    WellKnownMember.System_ValueTuple_TRest__Item1,
                                                                    WellKnownMember.System_ValueTuple_TRest__Item2,
                                                                    WellKnownMember.System_ValueTuple_TRest__Item3,
                                                                    WellKnownMember.System_ValueTuple_TRest__Item4,
                                                                    WellKnownMember.System_ValueTuple_TRest__Item5,
                                                                    WellKnownMember.System_ValueTuple_TRest__Item6,
                                                                    WellKnownMember.System_ValueTuple_TRest__Item7,
                                                                    WellKnownMember.System_ValueTuple_TRest__Rest }
                };

        /// <summary>
        /// Returns "Item1" for position=1
        /// Returns "Item12" for position=12
        /// </summary>
        internal static string TupleMemberName(int position)
        {
            return "Item" + position;
        }

        private static bool IsElementNameForbidden(string name)
        {
            switch (name)
            {
                case "CompareTo":
                case WellKnownMemberNames.DeconstructMethodName:
                case "Equals":
                case "GetHashCode":
                case "Rest":
                case "ToString":
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Checks whether the field name is reserved and tells us which position it's reserved for.
        ///
        /// For example:
        /// Returns 3 for "Item3".
        /// Returns 0 for "Rest", "ToString" and other members of System.ValueTuple.
        /// Returns -1 for names that aren't reserved.
        /// </summary>
        internal static int IsTupleElementNameReserved(string name)
        {
            if (IsElementNameForbidden(name))
            {
                return 0;
            }

            return MatchesCanonicalElementName(name);
        }

        /// <summary>
        /// Returns 3 for "Item3".
        /// Returns -1 otherwise.
        /// </summary>
        private static int MatchesCanonicalElementName(string name)
        {
            if (name.StartsWith("Item", StringComparison.Ordinal))
            {
                string tail = name.Substring(4);
                int number;
                if (int.TryParse(tail, out number))
                {
                    if (number > 0 && String.Equals(name, TupleMemberName(number), StringComparison.Ordinal))
                    {
                        return number;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// Lookup well-known member declaration in provided type.
        ///
        /// If a well-known member of a generic type instantiation is needed use this method to get the corresponding generic definition and
        /// <see cref="MethodSymbol.AsMember"/> to construct an instantiation.
        /// </summary>
        /// <param name="type">Type that we'll try to find member in.</param>
        /// <param name="relativeMember">A reference to a well-known member type descriptor. Note however that the type in that descriptor is ignored here.</param>
        private static Symbol GetWellKnownMemberInType(NamedTypeSymbol type, WellKnownMember relativeMember)
        {
            Debug.Assert(relativeMember >= WellKnownMember.System_ValueTuple_T1__Item1 && relativeMember <= WellKnownMember.System_ValueTuple_TRest__ctor);
            Debug.Assert(type.IsDefinition);

            MemberDescriptor relativeDescriptor = WellKnownMembers.GetDescriptor(relativeMember);
            var members = type.GetMembers(relativeDescriptor.Name);

            return CSharpCompilation.GetRuntimeMember(members, relativeDescriptor, CSharpCompilation.SpecialMembersSignatureComparer.Instance,
                                                      accessWithinOpt: null); // force lookup of public members only
        }

        private static Symbol GetWellKnownMemberInType(ImmutableArray<Symbol> members, WellKnownMember relativeMember)
        {
            Debug.Assert(relativeMember >= WellKnownMember.System_ValueTuple_T1__Item1 && relativeMember <= WellKnownMember.System_ValueTuple_TRest__ctor);

            MemberDescriptor relativeDescriptor = WellKnownMembers.GetDescriptor(relativeMember);
            return CSharpCompilation.GetRuntimeMember(members, relativeDescriptor, CSharpCompilation.SpecialMembersSignatureComparer.Instance,
                                                      accessWithinOpt: null); // force lookup of public members only
        }

        /// <summary>
        /// Lookup well-known member declaration in provided type and reports diagnostics.
        /// </summary>
        internal static Symbol? GetWellKnownMemberInType(NamedTypeSymbol type, WellKnownMember relativeMember, DiagnosticBag diagnostics, SyntaxNode? syntax)
        {
            Symbol? member = GetWellKnownMemberInType(type, relativeMember);

            if (member is null)
            {
                MemberDescriptor relativeDescriptor = WellKnownMembers.GetDescriptor(relativeMember);
                Binder.Error(diagnostics, ErrorCode.ERR_PredefinedTypeMemberNotFoundInAssembly, syntax, relativeDescriptor.Name, type, type.ContainingAssembly);
            }
            else
            {
                DiagnosticInfo useSiteDiag = member.GetUseSiteDiagnostic();
                if (useSiteDiag is object && useSiteDiag.Severity == DiagnosticSeverity.Error)
                {
                    diagnostics.Add(useSiteDiag, syntax?.GetLocation() ?? Location.None);
                }
            }

            return member;
        }

        public sealed override bool IsTupleType
            => IsTupleTypeOfCardinality(tupleCardinality: out _);

        internal TupleUncommonData? TupleData
        {
            get
            {
                if (!IsTupleType)
                {
                    return null;
                }

                if (_lazyTupleData is null)
                {
                    Interlocked.CompareExchange(ref _lazyTupleData, new TupleUncommonData(this), null);
                }

                return _lazyTupleData;
            }
        }

        public sealed override ImmutableArray<string> TupleElementNames
            => _lazyTupleData is null ? default : _lazyTupleData.ElementNames;

        private ImmutableArray<bool> TupleErrorPositions
            => _lazyTupleData is null ? default : _lazyTupleData.ErrorPositions;

        private ImmutableArray<Location> TupleElementLocations
            => _lazyTupleData is null ? default : _lazyTupleData.ElementLocations;

        public sealed override ImmutableArray<TypeWithAnnotations> TupleElementTypesWithAnnotations
            => IsTupleType ? TupleData!.TupleElementTypesWithAnnotations(this) : default;

        public sealed override ImmutableArray<FieldSymbol> TupleElements
            => IsTupleType ? TupleData!.TupleElements(this) : default;

        public TMember? GetTupleMemberSymbolForUnderlyingMember<TMember>(TMember underlyingMemberOpt) where TMember : Symbol
        {
            return IsTupleType ? TupleData!.GetTupleMemberSymbolForUnderlyingMember(underlyingMemberOpt) : default;
        }

        protected ArrayBuilder<Symbol>? AddOrWrapTupleMembers(ImmutableArray<Symbol> currentMembers)
        {
            Debug.Assert(IsTupleType);
            Debug.Assert(currentMembers.All(m => !(m is TupleVirtualElementFieldSymbol)));

            var elementTypes = TupleElementTypesWithAnnotations;
            var elementsMatchedByFields = ArrayBuilder<bool>.GetInstance(elementTypes.Length, fillWithValue: false);
            var members = ArrayBuilder<Symbol>.GetInstance(currentMembers.Length);

            NamedTypeSymbol currentValueTuple = this;
            int currentNestingLevel = 0;

            var currentFieldsForElements = ArrayBuilder<FieldSymbol>.GetInstance(currentValueTuple.Arity);

            // Lookup field definitions that we are interested in
            collectTargetTupleFields(currentValueTuple.Arity, getOriginalFields(currentMembers), currentFieldsForElements);

            var elementNames = TupleElementNames;
            var elementLocations = TupleData!.ElementLocations;
            while (true)
            {
                foreach (Symbol member in currentMembers)
                {
                    switch (member.Kind)
                    {
                        case SymbolKind.Field:
                            var field = (FieldSymbol)member;
                            if (field is TupleVirtualElementFieldSymbol)
                            {
                                continue;
                            }

                            var underlyingField = field is TupleElementFieldSymbol tupleElement ? tupleElement.UnderlyingField.OriginalDefinition : field.OriginalDefinition;
                            int tupleFieldIndex = currentFieldsForElements.IndexOf(underlyingField, ReferenceEqualityComparer.Instance);
                            if (underlyingField is TupleErrorFieldSymbol)
                            {
                                // We will re-add all necessary error field symbols below.
                                continue;
                            }
                            else if (tupleFieldIndex >= 0)
                            {
                                // This is a tuple backing field (ie. real)

                                // adjust tuple index for nesting
                                if (currentNestingLevel != 0)
                                {
                                    tupleFieldIndex += (RestPosition - 1) * currentNestingLevel;
                                }

                                var providedName = elementNames.IsDefault ? null : elementNames[tupleFieldIndex];

                                ImmutableArray<Location> locations;
                                if (this.IsDefinition)
                                {
                                    locations = member.Locations;
                                }
                                else if (elementLocations.IsDefault)
                                {
                                    locations = ImmutableArray<Location>.Empty;
                                }
                                else
                                {
                                    var elementLocation = elementLocations[tupleFieldIndex];
                                    locations = elementLocation == null ? ImmutableArray<Location>.Empty : ImmutableArray.Create(elementLocation);
                                }

                                var defaultName = TupleMemberName(tupleFieldIndex + 1);
                                // if provided name does not match the default one,
                                // then default element is declared implicitly
                                var defaultImplicitlyDeclared = providedName != defaultName;

                                // Add a field with default name. It should be present regardless.
                                TupleElementFieldSymbol defaultTupleField;
                                var fieldSymbol = underlyingField.AsMember(currentValueTuple);
                                if (currentNestingLevel != 0)
                                {
                                    // This is a matching field, but it is in the extension tuple
                                    // Make it virtual since we are not at the top level
                                    defaultTupleField = new TupleVirtualElementFieldSymbol(this,
                                                                                            fieldSymbol,
                                                                                            defaultName,
                                                                                            tupleFieldIndex,
                                                                                            locations,
                                                                                            cannotUse: false,
                                                                                            isImplicitlyDeclared: defaultImplicitlyDeclared,
                                                                                            correspondingDefaultFieldOpt: null);
                                }
                                else
                                {
                                    Debug.Assert(fieldSymbol.Name == defaultName, "top level underlying field must match default name");

                                    // Add the underlying/real field as an element (wrapping mainly to capture location). It should have the default name.
                                    defaultTupleField = new TupleElementFieldSymbol(this,
                                                                                    fieldSymbol,
                                                                                    tupleFieldIndex,
                                                                                    locations,
                                                                                    isImplicitlyDeclared: defaultImplicitlyDeclared,
                                                                                    correspondingDefaultFieldOpt: null);
                                }

                                members.Add(defaultTupleField);

                                if (defaultImplicitlyDeclared && !string.IsNullOrEmpty(providedName))
                                {
                                    var errorPositions = TupleErrorPositions;
                                    var isError = errorPositions.IsDefault ? false : errorPositions[tupleFieldIndex];

                                    // The name given doesn't match the default name Item8, etc.
                                    // Add a virtual field with the given name
                                    members.Add(new TupleVirtualElementFieldSymbol(this,
                                        fieldSymbol,
                                        providedName!,
                                        tupleFieldIndex,
                                        locations,
                                        cannotUse: isError,
                                        isImplicitlyDeclared: false,
                                        correspondingDefaultFieldOpt: defaultTupleField));
                                }

                                elementsMatchedByFields[tupleFieldIndex] = true; // mark as handled
                            }
                            else if (currentNestingLevel == 0)
                            {
                                // field at the top level didn't match a tuple backing field, simply add.
                                members.Add(new TupleFieldSymbol(this, field, -members.Count - 1));
                            }
                            break;

                        case SymbolKind.NamedType:
                            // We are dropping nested types, if any. Pending real need.
                            break;

                        case SymbolKind.Method:
                        case SymbolKind.Property:
                        case SymbolKind.Event:
                            if (currentNestingLevel == 0)
                            {
                                members.Add(member);
                            }
                            break;

                        default:
                            if (currentNestingLevel == 0)
                            {
                                throw ExceptionUtilities.UnexpectedValue(member.Kind);
                            }
                            break;
                    }
                }

                if (currentValueTuple.Arity != RestPosition)
                {
                    break;
                }

                var oldUnderlying = currentValueTuple;
                currentValueTuple = (NamedTypeSymbol)oldUnderlying.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[RestIndex].Type;
                currentNestingLevel++;

                if (currentValueTuple.Arity != RestPosition)
                {
                    // refresh members and target fields
                    currentMembers = currentValueTuple.GetMembers();
                    currentFieldsForElements.Clear();
                    collectTargetTupleFields(currentValueTuple.Arity, getOriginalFields(currentMembers), currentFieldsForElements);
                }
                else
                {
                    Debug.Assert((object)oldUnderlying.OriginalDefinition == currentValueTuple.OriginalDefinition);
                }
            }

            currentFieldsForElements.Free();

            // At the end, add unmatched fields as error symbols
            for (int i = 0; i < elementsMatchedByFields.Count; i++)
            {
                if (!elementsMatchedByFields[i])
                {
                    // We couldn't find a backing field for this element. It will be an error to access it.
                    int fieldRemainder; // one-based
                    int fieldChainLength = NumberOfValueTuples(i + 1, out fieldRemainder);
                    NamedTypeSymbol container = GetNestedTupleUnderlyingType(this, fieldChainLength - 1).OriginalDefinition;

                    var diagnosticInfo = container.IsErrorType() ?
                                                          null :
                                                          new CSDiagnosticInfo(ErrorCode.ERR_PredefinedTypeMemberNotFoundInAssembly,
                                                                               TupleMemberName(fieldRemainder),
                                                                               container,
                                                                               container.ContainingAssembly);

                    var providedName = elementNames.IsDefault ? null : elementNames[i];
                    var location = elementLocations.IsDefault ? null : elementLocations[i];
                    var defaultName = TupleMemberName(i + 1);
                    // if provided name does not match the default one,
                    // then default element is declared implicitly
                    var defaultImplicitlyDeclared = providedName != defaultName;

                    // Add a field with default name. It should be present regardless.
                    TupleErrorFieldSymbol defaultTupleField = new TupleErrorFieldSymbol(this,
                                                                                        defaultName,
                                                                                        i,
                                                                                        defaultImplicitlyDeclared ? null : location,
                                                                                        elementTypes[i],
                                                                                        diagnosticInfo,
                                                                                        defaultImplicitlyDeclared,
                                                                                        correspondingDefaultFieldOpt: null);

                    members.Add(defaultTupleField);

                    if (defaultImplicitlyDeclared && !String.IsNullOrEmpty(providedName))
                    {
                        // Add friendly named element field.
                        members.Add(new TupleErrorFieldSymbol(this,
                                                              providedName,
                                                              i,
                                                              location,
                                                              elementTypes[i],
                                                              diagnosticInfo,
                                                              isImplicitlyDeclared: false,
                                                              correspondingDefaultFieldOpt: defaultTupleField));
                    }
                }
            }

            elementsMatchedByFields.Free();
            return members;

            static void collectTargetTupleFields(int arity, ImmutableArray<Symbol> members, ArrayBuilder<FieldSymbol> fieldsForElements)
            {
                int fieldsPerType = Math.Min(arity, RestPosition - 1);

                for (int i = 0; i < fieldsPerType; i++)
                {
                    WellKnownMember wellKnownTupleField = GetTupleTypeMember(arity, i + 1);
                    fieldsForElements.Add((FieldSymbol)GetWellKnownMemberInType(members, wellKnownTupleField));
                }
            }

            static ImmutableArray<Symbol> getOriginalFields(ImmutableArray<Symbol> members)
            {
                var fields = ArrayBuilder<Symbol>.GetInstance();
                foreach (var member in members)
                {
                    if (member is TupleVirtualElementFieldSymbol)
                    {
                        continue;
                    }
                    else if (member is TupleElementFieldSymbol tupleField)
                    {
                        fields.Add(tupleField.UnderlyingField.OriginalDefinition);
                    }
                    else if (member is FieldSymbol field)
                    {
                        fields.Add(field.OriginalDefinition);
                    }
                }

                Debug.Assert(fields.All(f => f is object));
                return fields.ToImmutableAndFree();
            }
        }

        private TypeSymbol MergeTupleNames(NamedTypeSymbol other, NamedTypeSymbol mergedType)
        {
            // Merge tuple element names, if any
            ImmutableArray<string> names1 = TupleElementNames;
            ImmutableArray<string> names2 = other.TupleElementNames;
            ImmutableArray<string> mergedNames;
            if (names1.IsDefault || names2.IsDefault)
            {
                mergedNames = default;
            }
            else
            {
                Debug.Assert(names1.Length == names2.Length);
                mergedNames = names1.ZipAsArray(names2, (n1, n2) => string.CompareOrdinal(n1, n2) == 0 ? n1 : null)!;

                if (mergedNames.All(n => n is null))
                {
                    mergedNames = default;
                }
            }

            bool namesUnchanged = mergedNames.IsDefault ? TupleElementNames.IsDefault : mergedNames.SequenceEqual(TupleElementNames);
            return (namesUnchanged && this.Equals(mergedType, TypeCompareKind.ConsiderEverything))
                ? this
                : CreateTuple(mergedType, mergedNames, this.TupleErrorPositions, this.TupleElementLocations, this.Locations);
        }

        /// <summary>
        /// The main purpose of this type is to store element names and also cache some information related to tuples.
        /// </summary>
        internal sealed class TupleUncommonData
        {
            /// <summary>
            /// Element names, if provided.
            /// </summary>
            internal ImmutableArray<string> ElementNames { get; }

            /// <summary>
            /// Declaration locations for individual elements, if provided.
            /// Declaration location for this tuple type symbol
            /// </summary>
            internal ImmutableArray<Location> ElementLocations { get; }

            /// <summary>
            /// Which element names were inferred and therefore cannot be used.
            /// If none of the element names were inferred, or inferred names can be used (no tracking necessary), leave as default.
            /// This information is ignored in type equality and comparison.
            /// </summary>
            internal ImmutableArray<bool> ErrorPositions { get; }

            internal ImmutableArray<Location> Locations { get; }

            /// <summary>
            /// Element types.
            /// </summary>
            private ImmutableArray<TypeWithAnnotations> _lazyElementTypes;

            private ImmutableArray<FieldSymbol> _lazyDefaultElementFields;
            private SmallDictionary<Symbol, Symbol>? _lazyUnderlyingDefinitionToMemberMap;

            /// <summary>
            /// The same named type, but without element names.
            /// </summary>
            internal NamedTypeSymbol TupleUnderlyingType { get; }

            internal TupleUncommonData(NamedTypeSymbol underlyingType)
            {
                Debug.Assert(underlyingType is object);
                Debug.Assert(underlyingType.IsTupleType);
                Debug.Assert(underlyingType.TupleElementNames.IsDefault);

                TupleUnderlyingType = underlyingType;
                Locations = ImmutableArray<Location>.Empty;
            }

            internal TupleUncommonData(NamedTypeSymbol underlyingType, ImmutableArray<string> elementNames,
                ImmutableArray<Location> elementLocations, ImmutableArray<bool> errorPositions, ImmutableArray<Location> locations)
                : this(underlyingType)
            {
                ElementNames = elementNames;
                ElementLocations = elementLocations;
                ErrorPositions = errorPositions;
                Locations = locations.NullToEmpty();
            }

            internal bool EqualsIgnoringTupleUnderlyingType(TupleUncommonData? other)
            {
                if (other is null && this.ElementNames.IsDefault && this.ElementLocations.IsDefault && this.ErrorPositions.IsDefault)
                {
                    return true;
                }

                return other is object &&
                    this.ElementNames == other.ElementNames
                    && compareElementLocations(other)
                    && this.ErrorPositions == other.ErrorPositions;

                bool compareElementLocations(TupleUncommonData other)
                {
                    if (this.ElementLocations.IsDefault && other.ElementLocations.IsDefault)
                    {
                        return true;
                    }

                    if (this.ElementLocations.IsDefault != other.ElementLocations.IsDefault)
                    {
                        return false;
                    }

                    return this.ElementLocations.SequenceEqual(other.ElementLocations, (l1, l2) => object.Equals(l1, l2));
                }
            }


            public ImmutableArray<TypeWithAnnotations> TupleElementTypesWithAnnotations(NamedTypeSymbol tuple)
            {
                if (_lazyElementTypes.IsDefault)
                {
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyElementTypes, collectTupleElementTypesWithAnnotations(tuple));
                }

                return _lazyElementTypes;

                static ImmutableArray<TypeWithAnnotations> collectTupleElementTypesWithAnnotations(NamedTypeSymbol tuple)
                {
                    ImmutableArray<TypeWithAnnotations> elementTypes;

                    if (tuple.Arity == RestPosition)
                    {
                        // Ensure all Rest extensions are tuples
                        var tupleCompatibleType = EnsureRestExtensionsAreTuples(tuple);

                        var extensionTupleElementTypes = tupleCompatibleType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[RestPosition - 1].Type.TupleElementTypesWithAnnotations;
                        var typesBuilder = ArrayBuilder<TypeWithAnnotations>.GetInstance(RestPosition - 1 + extensionTupleElementTypes.Length);
                        typesBuilder.AddRange(tupleCompatibleType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics, RestPosition - 1);
                        typesBuilder.AddRange(extensionTupleElementTypes);
                        elementTypes = typesBuilder.ToImmutableAndFree();
                    }
                    else
                    {
                        elementTypes = tuple.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics;
                    }

                    return elementTypes;
                }
            }

            public ImmutableArray<FieldSymbol> TupleElements(NamedTypeSymbol tuple)
            {
                if (_lazyDefaultElementFields.IsDefault)
                {
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyDefaultElementFields, CollectTupleElementFields(tuple));
                }

                return _lazyDefaultElementFields;
            }

            private ImmutableArray<FieldSymbol> CollectTupleElementFields(NamedTypeSymbol tuple)
            {
                var builder = ArrayBuilder<FieldSymbol>.GetInstance(TupleElementTypesWithAnnotations(tuple).Length, fillWithValue: null!);

                foreach (var member in tuple.GetMembers())
                {
                    if (member.Kind != SymbolKind.Field)
                    {
                        continue;
                    }

                    var candidate = (FieldSymbol)member;
                    var index = candidate.TupleElementIndex;

                    if (index >= 0)
                    {
                        if (builder[index]?.IsDefaultTupleElement != false)
                        {
                            builder[index] = candidate;
                        }
                        else
                        {
                            // there is a better field in the slot
                            // that can only happen if the candidate is default.
                            Debug.Assert(candidate.IsDefaultTupleElement);
                        }
                    }
                }

                Debug.Assert(builder.All(f => f is object));

                return builder.ToImmutableAndFree();
            }


            internal SmallDictionary<Symbol, Symbol> UnderlyingDefinitionToMemberMap
            {
                get
                {
                    return _lazyUnderlyingDefinitionToMemberMap ??
                        (_lazyUnderlyingDefinitionToMemberMap = ComputeDefinitionToMemberMap());
                }
            }

            private SmallDictionary<Symbol, Symbol> ComputeDefinitionToMemberMap()
            {
                var map = new SmallDictionary<Symbol, Symbol>(ReferenceEqualityComparer.Instance);
                var members = TupleUnderlyingType.GetMembers();

                // Go in reverse because we want members with default name, which precede the ones with
                // friendly names, to be in the map.
                for (int i = members.Length - 1; i >= 0; i--)
                {
                    var member = members[i];
                    switch (member.Kind)
                    {
                        case SymbolKind.Method:
                        case SymbolKind.Property:
                        case SymbolKind.NamedType:
                            map.Add(member.OriginalDefinition, member);
                            break;

                        case SymbolKind.Field:
                            var tupleUnderlyingField = ((FieldSymbol)member).TupleUnderlyingField;
                            if (tupleUnderlyingField is object)
                            {
                                map[tupleUnderlyingField.OriginalDefinition] = member;
                            }
                            break;

                        case SymbolKind.Event:
                            var underlyingEvent = (EventSymbol)member;
                            var underlyingAssociatedField = underlyingEvent.AssociatedField;
                            // The field is not part of the members list
                            if ((object)underlyingAssociatedField != null)
                            {
                                Debug.Assert((object)underlyingAssociatedField.ContainingSymbol == TupleUnderlyingType);
                                Debug.Assert(TupleUnderlyingType.GetMembers(underlyingAssociatedField.Name).IndexOf(underlyingAssociatedField) < 0);
                                map.Add(underlyingAssociatedField.OriginalDefinition, new TupleFieldSymbol(TupleUnderlyingType, underlyingAssociatedField, -i - 1));
                            }

                            map.Add(underlyingEvent.OriginalDefinition, member);
                            break;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(member.Kind);
                    }
                }

                return map;
            }

            public TMember? GetTupleMemberSymbolForUnderlyingMember<TMember>(TMember underlyingMemberOpt) where TMember : Symbol
            {
                if ((object)underlyingMemberOpt == null)
                {
                    return null;
                }

                Symbol underlyingMemberDefinition = underlyingMemberOpt.OriginalDefinition;
                if (underlyingMemberDefinition is TupleFieldSymbol tupleField)
                {
                    underlyingMemberDefinition = tupleField.UnderlyingField;
                }

                if (TypeSymbol.Equals(underlyingMemberDefinition.ContainingType, TupleUnderlyingType.OriginalDefinition, TypeCompareKind.ConsiderEverything))
                {
                    if (UnderlyingDefinitionToMemberMap.TryGetValue(underlyingMemberDefinition, out Symbol result))
                    {
                        return (TMember)result;
                    }
                }

                return null;
            }
        }
    }
}
