// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.RuntimeMembers;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A TupleTypeSymbol represents a tuple type, such as (int, byte) or (int a, long b).
    /// </summary>
    internal sealed class TupleTypeSymbol : WrappedNamedTypeSymbol
    {
        /// <summary>
        /// Declaration location for this tuple type symbol
        /// </summary>
        private readonly ImmutableArray<Location> _locations;

        /// <summary>
        /// Declaration locations for individual elements, if provided.
        /// </summary>
        private readonly ImmutableArray<Location> _elementLocations;

        /// <summary>
        /// Element names, if provided. 
        /// </summary>
        private readonly ImmutableArray<string> _elementNames;

        /// <summary>
        /// Element types.
        /// </summary>
        private readonly ImmutableArray<TypeSymbol> _elementTypes;

        private ImmutableArray<Symbol> _lazyMembers;
        private ImmutableArray<FieldSymbol> _lazyDefaultElementFields;
        private SmallDictionary<Symbol, Symbol> _lazyUnderlyingDefinitionToMemberMap;

        internal const int RestPosition = 8; // The Rest field is in 8th position
        internal const string TupleTypeName = "ValueTuple";
        internal const string RestFieldName = "Rest";

        private TupleTypeSymbol(Location locationOpt, NamedTypeSymbol underlyingType, ImmutableArray<Location> elementLocations, ImmutableArray<string> elementNames, ImmutableArray<TypeSymbol> elementTypes)
            : this(locationOpt == null ? ImmutableArray<Location>.Empty : ImmutableArray.Create(locationOpt),
                  underlyingType, elementLocations, elementNames, elementTypes)
        {
        }

        private TupleTypeSymbol(ImmutableArray<Location> locations, NamedTypeSymbol underlyingType, ImmutableArray<Location> elementLocations, ImmutableArray<string> elementNames, ImmutableArray<TypeSymbol> elementTypes)
            : base(underlyingType)
        {
            Debug.Assert(elementLocations.IsDefault || elementLocations.Length == elementTypes.Length);
            Debug.Assert(elementNames.IsDefault || elementNames.Length == elementTypes.Length);
            Debug.Assert(!underlyingType.IsTupleType);

            _elementLocations = elementLocations;
            _elementNames = elementNames;
            _elementTypes = elementTypes;
            _locations = locations;
        }

        /// <summary>
        /// Helps create a TupleTypeSymbol from source.
        /// </summary>
        internal static NamedTypeSymbol Create(
            Location locationOpt,
            ImmutableArray<TypeSymbol> elementTypes,
            ImmutableArray<Location> elementLocations,
            ImmutableArray<string> elementNames,
            CSharpCompilation compilation,
            CSharpSyntaxNode syntax = null,
            DiagnosticBag diagnostics = null
            )
        {
            Debug.Assert(elementNames.IsDefault || elementTypes.Length == elementNames.Length);

            int numElements = elementTypes.Length;

            if (numElements <= 1)
            {
                throw ExceptionUtilities.Unreachable;
            }

            NamedTypeSymbol underlyingType = GetTupleUnderlyingType(elementTypes, syntax, compilation, diagnostics);

            if (((SourceModuleSymbol)compilation.SourceModule).AnyReferencedAssembliesAreLinked)
            {
                // Complain about unembeddable types from linked assemblies.
                Emit.NoPia.EmbeddedTypesManager.IsValidEmbeddableType(underlyingType, syntax, diagnostics);
            }

            return Create(underlyingType, elementNames, locationOpt, elementLocations);
        }

        public static TupleTypeSymbol Create(NamedTypeSymbol tupleCompatibleType,
                                             ImmutableArray<string> elementNames = default(ImmutableArray<string>),
                                             Location locationOpt = null,
                                             ImmutableArray<Location> elementLocations = default(ImmutableArray<Location>))
        {
            return Create(locationOpt == null ? ImmutableArray<Location>.Empty : ImmutableArray.Create(locationOpt),
                          tupleCompatibleType,
                          elementLocations,
                          elementNames);
        }

        public static TupleTypeSymbol Create(ImmutableArray<Location> locations, NamedTypeSymbol tupleCompatibleType, ImmutableArray<Location> elementLocations, ImmutableArray<string> elementNames)
        {
            Debug.Assert(tupleCompatibleType.IsTupleCompatible());

            ImmutableArray<TypeSymbol> elementTypes;

            if (tupleCompatibleType.Arity == RestPosition)
            {
                // Ensure all Rest extensions are tuples
                tupleCompatibleType = EnsureRestExtensionsAreTuples(tupleCompatibleType);

                var extensionTupleElementTypes = tupleCompatibleType.TypeArgumentsNoUseSiteDiagnostics[RestPosition - 1].TupleElementTypes;
                var typesBuilder = ArrayBuilder<TypeSymbol>.GetInstance(RestPosition - 1 + extensionTupleElementTypes.Length);
                typesBuilder.AddRange(tupleCompatibleType.TypeArgumentsNoUseSiteDiagnostics, RestPosition - 1);
                typesBuilder.AddRange(extensionTupleElementTypes);
                elementTypes = typesBuilder.ToImmutableAndFree();
            }
            else
            {
                elementTypes = tupleCompatibleType.TypeArgumentsNoUseSiteDiagnostics;
            }

            return new TupleTypeSymbol(locations, tupleCompatibleType, elementLocations, elementNames, elementTypes);
        }

        /// <summary>
        /// Adjust the <paramref name="tupleCompatibleType"/> in such a way so that 
        /// all types used by Rest fields are tuples. Throughout the entire nesting chain.
        /// </summary>
        private static NamedTypeSymbol EnsureRestExtensionsAreTuples(NamedTypeSymbol tupleCompatibleType)
        {
            if (!tupleCompatibleType.TypeArgumentsNoUseSiteDiagnostics[RestPosition - 1].IsTupleType)
            {
                var nonTupleTypeChain = ArrayBuilder<NamedTypeSymbol>.GetInstance();

                NamedTypeSymbol currentType = tupleCompatibleType;
                do
                {
                    nonTupleTypeChain.Add(currentType);
                    currentType = (NamedTypeSymbol)currentType.TypeArgumentsNoUseSiteDiagnostics[RestPosition - 1];
                }
                while (currentType.Arity == RestPosition);

                if (!currentType.IsTupleType)
                {
                    nonTupleTypeChain.Add(currentType);
                }

                Debug.Assert(nonTupleTypeChain.Count > 1);
                tupleCompatibleType = nonTupleTypeChain.Pop();

                var typeArgumentsBuilder = ArrayBuilder<TypeWithModifiers>.GetInstance(RestPosition);

                do
                {
                    var extensionTuple = Create(tupleCompatibleType);
                    tupleCompatibleType = nonTupleTypeChain.Pop();

                    tupleCompatibleType = ReplaceRestExtensionType(tupleCompatibleType, typeArgumentsBuilder, extensionTuple);
                }
                while (nonTupleTypeChain.Count != 0);

                typeArgumentsBuilder.Free();
                nonTupleTypeChain.Free();
            }

            return tupleCompatibleType;
        }

        private static NamedTypeSymbol ReplaceRestExtensionType(NamedTypeSymbol tupleCompatibleType, ArrayBuilder<TypeWithModifiers> typeArgumentsBuilder, TupleTypeSymbol extensionTuple)
        {
            var modifiers = default(ImmutableArray<ImmutableArray<CustomModifier>>);

            if (tupleCompatibleType.HasTypeArgumentsCustomModifiers)
            {
                modifiers = tupleCompatibleType.TypeArgumentsCustomModifiers;
            }

            var arguments = tupleCompatibleType.TypeArgumentsNoUseSiteDiagnostics;
            typeArgumentsBuilder.Clear();

            for (int i = 0; i < RestPosition - 1; i++)
            {
                typeArgumentsBuilder.Add(new TypeWithModifiers(arguments[i], GetModifiers(modifiers, i)));
            }

            typeArgumentsBuilder.Add(new TypeWithModifiers(extensionTuple, GetModifiers(modifiers, RestPosition - 1)));

            tupleCompatibleType = tupleCompatibleType.ConstructedFrom.Construct(typeArgumentsBuilder.ToImmutable(), unbound: false);
            return tupleCompatibleType;
        }

        private static ImmutableArray<CustomModifier> GetModifiers(ImmutableArray<ImmutableArray<CustomModifier>> modifiers, int i)
        {
            return modifiers.IsDefaultOrEmpty ? ImmutableArray<CustomModifier>.Empty : modifiers[i];
        }

        /// <summary>
        /// Copy this tuple, but modify it to use the new underlying type.
        /// </summary>
        internal TupleTypeSymbol WithUnderlyingType(NamedTypeSymbol newUnderlyingType)
        {
            Debug.Assert(!newUnderlyingType.IsTupleType && newUnderlyingType.IsTupleOrCompatibleWithTupleOfCardinality(_elementTypes.Length));

            return Create(_locations, newUnderlyingType, _elementLocations, _elementNames);
        }

        /// <summary>
        /// Copy this tuple, but modify it to use the new element names.
        /// </summary>
        internal TupleTypeSymbol WithElementNames(ImmutableArray<string> newElementNames)
        {
            Debug.Assert(newElementNames.IsDefault || this._elementTypes.Length == newElementNames.Length);

            if (this._elementNames.IsDefault)
            {
                if (newElementNames.IsDefault)
                {
                    return this;
                }
            }
            else if (!newElementNames.IsDefault && this._elementNames.SequenceEqual(newElementNames))
            {
                return this;
            }

            return new TupleTypeSymbol(null, _underlyingType, default(ImmutableArray<Location>), newElementNames, _elementTypes);
        }

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
                if (currentType.Arity == TupleTypeSymbol.RestPosition)
                {
                    currentType = (NamedTypeSymbol)currentType.TypeArgumentsNoUseSiteDiagnostics[TupleTypeSymbol.RestPosition - 1].TupleUnderlyingType;
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
        internal static void AddElementTypes(NamedTypeSymbol underlyingTupleType, ArrayBuilder<TypeSymbol> tupleElementTypes)
        {
            NamedTypeSymbol currentType = underlyingTupleType;

            while (true)
            {
                if (currentType.IsTupleType)
                {
                    tupleElementTypes.AddRange(currentType.TupleElementTypes);
                    break;
                }

                var regularElements = Math.Min(currentType.Arity, TupleTypeSymbol.RestPosition - 1);
                tupleElementTypes.AddRange(currentType.TypeArguments, regularElements);

                if (currentType.Arity == TupleTypeSymbol.RestPosition)
                {
                    currentType = (NamedTypeSymbol)currentType.TypeArgumentsNoUseSiteDiagnostics[TupleTypeSymbol.RestPosition - 1];
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
                found = (NamedTypeSymbol)found.TypeArgumentsNoUseSiteDiagnostics[RestPosition - 1].TupleUnderlyingType;
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
        private static NamedTypeSymbol GetTupleUnderlyingType(ImmutableArray<TypeSymbol> elementTypes, CSharpSyntaxNode syntax, CSharpCompilation compilation, DiagnosticBag diagnostics)
        {
            int numElements = elementTypes.Length;
            int remainder;
            int chainLength = NumberOfValueTuples(numElements, out remainder);

            NamedTypeSymbol currentSymbol = default(NamedTypeSymbol);
            NamedTypeSymbol firstTupleType = compilation.GetWellKnownType(GetTupleType(remainder));

            if ((object)diagnostics != null && (object)syntax != null)
            {
                ReportUseSiteAndObsoleteDiagnostics(syntax, diagnostics, firstTupleType);
            }

            currentSymbol = firstTupleType.Construct(ImmutableArray.Create(elementTypes, (chainLength - 1) * (RestPosition - 1), remainder));

            int loop = chainLength - 1;
            if (loop > 0)
            {
                NamedTypeSymbol chainedTupleType = compilation.GetWellKnownType(GetTupleType(RestPosition));

                if ((object)diagnostics != null && (object)syntax != null)
                {
                    ReportUseSiteAndObsoleteDiagnostics(syntax, diagnostics, chainedTupleType);
                }

                do
                {
                    ImmutableArray<TypeSymbol> chainedTypes = ImmutableArray.Create(elementTypes, (loop - 1) * (RestPosition - 1), RestPosition - 1).Add(currentSymbol);

                    currentSymbol = chainedTupleType.Construct(chainedTypes);
                    loop--;
                }
                while (loop > 0);
            }

            return currentSymbol;
        }

        private static void ReportUseSiteAndObsoleteDiagnostics(CSharpSyntaxNode syntax, DiagnosticBag diagnostics, NamedTypeSymbol firstTupleType)
        {
            Binder.ReportUseSiteDiagnostics(firstTupleType, diagnostics, syntax);
            Binder.ReportDiagnosticsIfObsoleteInternal(diagnostics, firstTupleType, syntax, firstTupleType.ContainingType, BinderFlags.None);
        }

        /// <summary>
        /// For tuples with no natural type, we still need to verify that an underlying type of proper arity exists, and report if otherwise.
        /// </summary>
        internal static void VerifyTupleTypePresent(int cardinality, CSharpSyntaxNode syntax, CSharpCompilation compilation, DiagnosticBag diagnostics)
        {
            Debug.Assert((object)diagnostics != null && (object)syntax != null);

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

        internal static void ReportNamesMismatchesIfAny(TypeSymbol destination, BoundTupleLiteral literal, DiagnosticBag diagnostics)
        {
            var sourceNames = literal.ArgumentNamesOpt;
            if (sourceNames.IsDefault)
            {
                return;
            }

            ImmutableArray<string> destinationNames = destination.TupleElementNames;
            int sourceLength = sourceNames.Length;
            bool allMissing = destinationNames.IsDefault;
            Debug.Assert(allMissing || destinationNames.Length == sourceLength);

            for (int i = 0; i < sourceLength; i++)
            {
                var sourceName = sourceNames[i];
                if (sourceName != null && (allMissing || string.CompareOrdinal(destinationNames[i], sourceName) != 0))
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
                case "Deconstruct":
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
        internal static int IsElementNameReserved(string name)
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
        internal static Symbol GetWellKnownMemberInType(NamedTypeSymbol type, WellKnownMember relativeMember)
        {
            Debug.Assert(relativeMember >= WellKnownMember.System_ValueTuple_T1__Item1 && relativeMember <= WellKnownMember.System_ValueTuple_TRest__ctor);
            Debug.Assert(type.IsDefinition);

            MemberDescriptor relativeDescriptor = WellKnownMembers.GetDescriptor(relativeMember);
            return CSharpCompilation.GetRuntimeMember(type, ref relativeDescriptor, CSharpCompilation.SpecialMembersSignatureComparer.Instance,
                                                      accessWithinOpt: null); // force lookup of public members only
        }

        /// <summary>
        /// Lookup well-known member declaration in provided type and reports diagnostics.
        /// </summary>
        internal static Symbol GetWellKnownMemberInType(NamedTypeSymbol type, WellKnownMember relativeMember, DiagnosticBag diagnostics, SyntaxNode syntax)
        {
            Symbol member = GetWellKnownMemberInType(type, relativeMember);

            if ((object)member == null)
            {
                MemberDescriptor relativeDescriptor = WellKnownMembers.GetDescriptor(relativeMember);
                Binder.Error(diagnostics, ErrorCode.ERR_PredefinedTypeMemberNotFoundInAssembly, syntax, relativeDescriptor.Name, type, type.ContainingAssembly);
            }
            else
            {
                DiagnosticInfo useSiteDiag = member.GetUseSiteDiagnostic();
                if ((object)useSiteDiag != null && useSiteDiag.Severity == DiagnosticSeverity.Error)
                {
                    diagnostics.Add(useSiteDiag, syntax.GetLocation());
                }
            }

            return member;
        }

        /// <summary>
        /// The ValueTuple type for this tuple.
        /// The type argument corresponding to the type of the extension field (VT[8].Rest),
        /// which is at the 8th (one based) position is always a symbol for another tuple, 
        /// rather than its underlying type.
        /// </summary>
        public override NamedTypeSymbol TupleUnderlyingType
        {
            get
            {
                return _underlyingType;
            }
        }

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics
        {
            get
            {
                return _underlyingType.BaseTypeNoUseSiteDiagnostics;
            }
        }

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<Symbol> basesBeingResolved)
        {
            return _underlyingType.InterfacesNoUseSiteDiagnostics(basesBeingResolved);
        }

        internal sealed override bool IsManagedType
        {
            get
            {
                return _underlyingType.IsManagedType;
            }
        }

        public override bool IsTupleType
        {
            get
            {
                return true;
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get
            {
                return false;
            }
        }

        public override ImmutableArray<TypeSymbol> TupleElementTypes
        {
            get
            {
                return _elementTypes;
            }
        }

        public override ImmutableArray<string> TupleElementNames
        {
            get
            {
                return _elementNames;
            }
        }

        /// <summary>
        /// Get the default fields for the tuple's elements (in order and cached).
        /// </summary>
        public override ImmutableArray<FieldSymbol> TupleDefaultElementFields
        {
            get
            {
                if (_lazyDefaultElementFields.IsDefault)
                {
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyDefaultElementFields, CollectTupleElementFields());
                }

                return _lazyDefaultElementFields;
            }
        }

        private ImmutableArray<FieldSymbol> CollectTupleElementFields()
        {
            var builder = ArrayBuilder<FieldSymbol>.GetInstance(_elementTypes.Length, fillWithValue: null);

            foreach (var member in GetMembers())
            {
                if (member.Kind != SymbolKind.Field)
                {
                    continue;
                }

                var field = (FieldSymbol)member;

                if (field.IsDefaultTupleElement)
                {
                    var index = field.TupleElementIndex;
                    Debug.Assert((object)builder[index] == null);
                    builder[index] = field;
                }
            }

            Debug.Assert(builder.All(symbol => (object)symbol != null));

            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Returns all members of the tuple type - a combination of members from the underlying type 
        /// and synthesized fields for tuple elements.
        /// </summary>
        public override ImmutableArray<Symbol> GetMembers()
        {
            if (_lazyMembers.IsDefault)
            {
                ImmutableInterlocked.InterlockedInitialize(ref _lazyMembers, CreateMembers());
            }

            return _lazyMembers;
        }

        private ImmutableArray<Symbol> CreateMembers()
        {
            var elementsMatchedByFields = ArrayBuilder<bool>.GetInstance(_elementTypes.Length, fillWithValue: false);
            var members = ArrayBuilder<Symbol>.GetInstance(Math.Max(_elementTypes.Length, _underlyingType.OriginalDefinition.GetMembers().Length));

            NamedTypeSymbol currentUnderlying = _underlyingType;
            int currentNestingLevel = 0;

            var currentFieldsForElements = ArrayBuilder<FieldSymbol>.GetInstance(currentUnderlying.Arity);

            // Lookup field definitions that we are interested in
            CollectTargetTupleFields(currentUnderlying, currentFieldsForElements);

            ImmutableArray<Symbol> underlyingMembers = currentUnderlying.OriginalDefinition.GetMembers();

            while (true)
            {
                foreach (Symbol member in underlyingMembers)
                {
                    switch (member.Kind)
                    {
                        case SymbolKind.Method:
                            if (currentNestingLevel == 0)
                            {
                                members.Add(new TupleMethodSymbol(this, ((MethodSymbol)member).AsMember(currentUnderlying)));
                            }
                            break;

                        case SymbolKind.Field:
                            var field = (FieldSymbol)member;

                            int tupleFieldIndex = currentFieldsForElements.IndexOf(field, ReferenceEqualityComparer.Instance);
                            if (tupleFieldIndex >= 0)
                            {
                                // This is a tuple backing field

                                // adjust tuple index for nesting
                                if (currentNestingLevel != 0)
                                {
                                    tupleFieldIndex += (RestPosition - 1) * currentNestingLevel;
                                }

                                var providedName = _elementNames.IsDefault ? null : _elementNames[tupleFieldIndex];
                                var location = _elementLocations.IsDefault ? null : _elementLocations[tupleFieldIndex];
                                var defaultName = TupleMemberName(tupleFieldIndex + 1);
                                // if provided name does not match the default one, 
                                // then default element is declared implicitly
                                var defaultImplicitlyDeclared = providedName != defaultName;

                                var fieldSymbol = field.AsMember(currentUnderlying);

                                // Add a field with default name. It should be present regardless.
                                if (currentNestingLevel != 0)
                                {
                                    // This is a matching field, but it is in the extension tuple
                                    // Make it virtual since we are not at the top level
                                    // tupleFieldIndex << 1 because this is a default element
                                    members.Add(new TupleVirtualElementFieldSymbol(this, fieldSymbol, defaultName, tupleFieldIndex << 1, location, defaultImplicitlyDeclared));
                                }
                                else
                                {
                                    Debug.Assert(fieldSymbol.Name == defaultName, "top level underlying field must match default name");

                                    // Add the underlying field as an element. It should have the default name.
                                    // tupleFieldIndex << 1 because this is a default element
                                    members.Add(new TupleElementFieldSymbol(this, fieldSymbol, tupleFieldIndex << 1, location, defaultImplicitlyDeclared));
                                }

                                if (defaultImplicitlyDeclared && providedName != null)
                                {
                                    // The name given doesn't match the default name Item8, etc.
                                    // Add a virtual field with the given name
                                    // tupleFieldIndex << 1 + 1, because this is not a default element
                                    members.Add(new TupleVirtualElementFieldSymbol(this, fieldSymbol, providedName, (tupleFieldIndex << 1) + 1, location, isImplicitlyDeclared: false));
                                }

                                elementsMatchedByFields[tupleFieldIndex] = true; // mark as handled
                            }
                            else if (currentNestingLevel == 0)
                            {
                                // field at the top level didn't match a tuple backing field, simply add.
                                members.Add(new TupleFieldSymbol(this, field.AsMember(currentUnderlying), -members.Count - 1));
                            }
                            break;

                        case SymbolKind.NamedType:
                            // We are dropping nested types, if any. Pending real need.
                            break;

                        case SymbolKind.Property:
                            if (currentNestingLevel == 0)
                            {
                                members.Add(new TuplePropertySymbol(this, ((PropertySymbol)member).AsMember(currentUnderlying)));
                            }
                            break;

                        case SymbolKind.Event:
                            if (currentNestingLevel == 0)
                            {
                                members.Add(new TupleEventSymbol(this, ((EventSymbol)member).AsMember(currentUnderlying)));
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

                if (currentUnderlying.Arity != RestPosition)
                {
                    break;
                }

                var oldUnderlying = currentUnderlying;
                currentUnderlying = oldUnderlying.TypeArgumentsNoUseSiteDiagnostics[RestPosition - 1].TupleUnderlyingType;
                currentNestingLevel++;

                if (currentUnderlying.Arity != RestPosition)
                {
                    // refresh members and target fields
                    underlyingMembers = currentUnderlying.OriginalDefinition.GetMembers();
                    currentFieldsForElements.Clear();
                    CollectTargetTupleFields(currentUnderlying, currentFieldsForElements);
                }
                else
                {
                    Debug.Assert((object)oldUnderlying.OriginalDefinition == currentUnderlying.OriginalDefinition);
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
                    NamedTypeSymbol container = GetNestedTupleUnderlyingType(_underlyingType, fieldChainLength - 1).OriginalDefinition;

                    var diagnosticInfo = container.IsErrorType() ?
                                                          null :
                                                          new CSDiagnosticInfo(ErrorCode.ERR_PredefinedTypeMemberNotFoundInAssembly,
                                                                               TupleMemberName(fieldRemainder),
                                                                               container,
                                                                               container.ContainingAssembly);

                    var providedName = _elementNames.IsDefault ? null : _elementNames[i];
                    var location = _elementLocations.IsDefault ? null : _elementLocations[i];
                    var defaultName = TupleMemberName(i + 1);
                    // if provided name does not match the default one, 
                    // then default element is declared implicitly
                    var defaultImplicitlyDeclared = providedName != defaultName;

                    // Add default element field. 
                    // i << 1 because this is a default element
                    members.Add(new TupleErrorFieldSymbol(this, defaultName, i << 1, defaultImplicitlyDeclared ? null : location, _elementTypes[i], diagnosticInfo, defaultImplicitlyDeclared));


                    if (defaultImplicitlyDeclared && providedName != null)
                    {
                        // Add friendly named element field. 
                        // (i << 1) + 1, because this is not a default element
                        members.Add(new TupleErrorFieldSymbol(this, providedName, (i << 1) + 1, location, _elementTypes[i], diagnosticInfo, isImplicitlyDeclared: false));
                    }
                }
            }

            elementsMatchedByFields.Free();
            return members.ToImmutableAndFree();
        }

        private static void CollectTargetTupleFields(NamedTypeSymbol underlying, ArrayBuilder<FieldSymbol> fieldsForElements)
        {
            underlying = underlying.OriginalDefinition;
            int fieldsPerType = Math.Min(underlying.Arity, RestPosition - 1);

            for (int i = 0; i < fieldsPerType; i++)
            {
                WellKnownMember wellKnownTupleField = GetTupleTypeMember(underlying.Arity, i + 1);
                fieldsForElements.Add((FieldSymbol)GetWellKnownMemberInType(underlying, wellKnownTupleField));
            }
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

            var underlyingDefinition = _underlyingType.OriginalDefinition;
            var members = GetMembers();

            // Go in reverse because we want members with default name, which precede the ones with
            // friendly names, to be in the map.  
            for (int i = members.Length - 1; i >= 0; i--)
            {
                var member = members[i];
                switch (member.Kind)
                {
                    case SymbolKind.Method:
                        map.Add(((MethodSymbol)member).TupleUnderlyingMethod.OriginalDefinition, member);
                        break;

                    case SymbolKind.Field:
                        var tupleUnderlyingField = ((FieldSymbol)member).TupleUnderlyingField;
                        if ((object)tupleUnderlyingField != null)
                        {
                            map[tupleUnderlyingField.OriginalDefinition] = member;
                        }
                        break;

                    case SymbolKind.Property:
                        map.Add(((PropertySymbol)member).TupleUnderlyingProperty.OriginalDefinition, member);
                        break;

                    case SymbolKind.Event:
                        var underlyingEvent = ((EventSymbol)member).TupleUnderlyingEvent;
                        var underlyingAssociatedField = underlyingEvent.AssociatedField;
                        // The field is not part of the members list
                        if ((object)underlyingAssociatedField != null)
                        {
                            Debug.Assert((object)underlyingAssociatedField.ContainingSymbol == _underlyingType);
                            Debug.Assert(_underlyingType.GetMembers(underlyingAssociatedField.Name).IndexOf(underlyingAssociatedField) < 0);
                            map.Add(underlyingAssociatedField.OriginalDefinition, new TupleFieldSymbol(this, underlyingAssociatedField, -i - 1));
                        }

                        map.Add(underlyingEvent.OriginalDefinition, member);
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(member.Kind);
                }
            }

            return map;
        }

        public TMember GetTupleMemberSymbolForUnderlyingMember<TMember>(TMember underlyingMemberOpt) where TMember : Symbol
        {
            if ((object)underlyingMemberOpt == null)
            {
                return null;
            }

            Symbol underlyingMemberDefinition = underlyingMemberOpt.OriginalDefinition;
            if (underlyingMemberDefinition.ContainingType == _underlyingType.OriginalDefinition)
            {
                Symbol result;
                if (UnderlyingDefinitionToMemberMap.TryGetValue(underlyingMemberDefinition, out result))
                {
                    return (TMember)result;
                }
            }

            return null;
        }

        public override ImmutableArray<Symbol> GetMembers(string name)
        {
            return GetMembers().WhereAsArray(m => m.Name == name);
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            // do not support nested types at the moment
            Debug.Assert(!GetMembers().Any(m => m.Kind == SymbolKind.NamedType));
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
        {
            // do not support nested types at the moment
            Debug.Assert(!GetMembers().Any(m => m.Kind == SymbolKind.NamedType));
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name, int arity)
        {
            // do not support nested types at the moment
            Debug.Assert(!GetMembers().Any(m => m.Kind == SymbolKind.NamedType));
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public override NamedTypeSymbol EnumUnderlyingType
        {
            get
            {
                return _underlyingType.EnumUnderlyingType;
            }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return _underlyingType.GetAttributes();
        }

        public override SymbolKind Kind
        {
            get
            {
                return SymbolKind.NamedType;
            }
        }

        public override TypeKind TypeKind
        {
            get
            {
                // From the language perspective tuple is a value type 
                // composed of its underlying elements
                return TypeKind.Struct;
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return _underlyingType.ContainingSymbol;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return _locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return GetDeclaringSyntaxReferenceHelper<CSharpSyntaxNode>(_locations);
            }
        }

        internal override bool Equals(TypeSymbol t2, TypeCompareKind comparison)
        {
            if ((comparison & TypeCompareKind.IgnoreTupleNames) != 0)
            {
                if (t2?.IsTupleType == true)
                {
                    t2 = t2.TupleUnderlyingType;
                }

                return _underlyingType.Equals(t2, comparison);
            }

            return this.Equals(t2 as TupleTypeSymbol, comparison);
        }

        internal bool Equals(TupleTypeSymbol other)
        {
            return Equals(other, TypeCompareKind.ConsiderEverything);
        }

        private bool Equals(TupleTypeSymbol other, TypeCompareKind comparison)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if ((object)other == null || !other._underlyingType.Equals(_underlyingType, comparison))
            {
                return false;
            }

            // Make sure field names are the same.
            if ((comparison & TypeCompareKind.IgnoreTupleNames) == 0)
            {
                if (this._elementNames.IsDefault)
                {
                    if (!other._elementNames.IsDefault)
                    {
                        return false;
                    }
                }
                else if (other._elementNames.IsDefault)
                {
                    Debug.Assert(!this._elementNames.IsDefault);
                    return false;
                }
                else if (!this._elementNames.SequenceEqual(other._elementNames))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            return _underlyingType.GetHashCode();
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                if (_underlyingType.IsErrorType())
                {
                    return Accessibility.Public;
                }
                else
                {
                    return _underlyingType.DeclaredAccessibility;
                }
            }
        }

        public override bool IsStatic
        {
            get
            {
                return false;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                return false;
            }
        }

        public override bool IsSealed
        {
            get
            {
                return true;
            }
        }

        public override int Arity
        {
            get
            {
                return 0;
            }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get
            {
                return ImmutableArray<TypeParameterSymbol>.Empty;
            }
        }

        internal override ImmutableArray<ImmutableArray<CustomModifier>> TypeArgumentsCustomModifiers
        {
            get
            {
                return ImmutableArray<ImmutableArray<CustomModifier>>.Empty;
            }
        }

        internal override bool HasTypeArgumentsCustomModifiers
        {
            get
            {
                return false;
            }
        }

        internal override ImmutableArray<TypeSymbol> TypeArgumentsNoUseSiteDiagnostics
        {
            get
            {
                return ImmutableArray<TypeSymbol>.Empty;
            }
        }

        public override NamedTypeSymbol ConstructedFrom
        {
            get
            {
                return this;
            }
        }

        public override bool MightContainExtensionMethods
        {
            get
            {
                return false;
            }
        }

        public override string Name
        {
            get
            {
                return string.Empty;
            }
        }

        internal override bool MangleName
        {
            get
            {
                return false;
            }
        }

        public override IEnumerable<string> MemberNames
        {
            get
            {
                var set = PooledHashSet<string>.GetInstance();
                foreach (var member in GetMembers())
                {
                    var name = member.Name;
                    if (set.Add(name))
                    {
                        yield return name;
                    }
                }

                set.Free();
            }
        }

        internal override bool HasSpecialName
        {
            get
            {
                return false;
            }
        }

        internal override bool IsComImport
        {
            get
            {
                return false;
            }
        }

        internal override NamedTypeSymbol ComImportCoClass
        {
            get
            {
                return null;
            }
        }

        internal override bool IsWindowsRuntimeImport
        {
            get
            {
                return false;
            }
        }

        internal override bool ShouldAddWinRTMembers
        {
            get
            {
                return false;
            }
        }

        internal override bool IsSerializable
        {
            get
            {
                return _underlyingType.IsSerializable;
            }
        }

        internal override TypeLayout Layout
        {
            get
            {
                return _underlyingType.Layout;
            }
        }

        internal override CharSet MarshallingCharSet
        {
            get
            {
                return _underlyingType.MarshallingCharSet;
            }
        }

        internal override bool HasDeclarativeSecurity
        {
            get
            {
                return _underlyingType.HasDeclarativeSecurity;
            }
        }

        internal override bool IsInterface
        {
            get
            {
                return false;
            }
        }

        #region Use-Site Diagnostics

        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            return _underlyingType.GetUseSiteDiagnostic();
        }

        internal override bool GetUnificationUseSiteDiagnosticRecursive(ref DiagnosticInfo result, Symbol owner, ref HashSet<TypeSymbol> checkedTypes)
        {
            return _underlyingType.GetUnificationUseSiteDiagnosticRecursive(ref result, owner, ref checkedTypes);
        }

        #endregion

        #region ISymbol Members

        internal override AttributeUsageInfo GetAttributeUsageInfo()
        {
            return AttributeUsageInfo.Null;
        }

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers()
        {
            var underlying = _underlyingType.GetEarlyAttributeDecodingMembers();
            if (underlying.IsEmpty)
            {
                return underlying;
            }

            return underlying.SelectAsArray((u, tuple) => tuple.GetTupleMemberSymbolForUnderlyingMember(u), this).WhereAsArray(m => (object)m != null);
        }

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name)
        {
            var underlying = _underlyingType.GetEarlyAttributeDecodingMembers(name);
            if (underlying.IsEmpty)
            {
                return underlying;
            }

            return underlying.SelectAsArray((u, tuple) => tuple.GetTupleMemberSymbolForUnderlyingMember(u), this).WhereAsArray(m => (object)m != null);
        }

        internal override NamedTypeSymbol GetDeclaredBaseType(ConsList<Symbol> basesBeingResolved)
        {
            return _underlyingType.GetDeclaredBaseType(basesBeingResolved);
        }

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<Symbol> basesBeingResolved)
        {
            return _underlyingType.GetDeclaredInterfaces(basesBeingResolved);
        }

        internal override IEnumerable<SecurityAttribute> GetSecurityInformation()
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return ImmutableArray<string>.Empty;
        }

        internal override IEnumerable<FieldSymbol> GetFieldsToEmit()
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override IEnumerable<EventSymbol> GetEventsToEmit()
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override IEnumerable<MethodSymbol> GetMethodsToEmit()
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override IEnumerable<PropertySymbol> GetPropertiesToEmit()
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit()
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(ModuleCompilationState compilationState)
        {
            throw ExceptionUtilities.Unreachable;
        }

        #endregion

        public static TypeSymbol TransformToTupleIfCompatible(TypeSymbol target)
        {
            if (!target.IsErrorType() && target.IsTupleCompatible())
            {
                return TupleTypeSymbol.Create((NamedTypeSymbol)target);
            }

            return target;
        }
    }
}
