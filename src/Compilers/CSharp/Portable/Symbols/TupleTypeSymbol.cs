// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Cci;
using Microsoft.CodeAnalysis.RuntimeMembers;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A TupleTypeSymbol represents a tuple type, such as (int, byte) or (int a, long b).
    /// </summary>
    internal sealed class TupleTypeSymbol : NamedTypeSymbol, ITupleTypeSymbol
    {
        private readonly NamedTypeSymbol _underlyingType;
        private readonly ImmutableArray<TupleFieldSymbol> _fields;

        internal const int RestPosition = 8; // The Rest field is in 8th position

        /// <summary>
        /// Construct a TupleTypeSymbol from it's underlying ValueTuple type and element names.
        ///
        /// Parameter elementsNames has to empty or fully populated.
        /// </summary>
        private TupleTypeSymbol(
            NamedTypeSymbol underlyingType,
            ImmutableArray<string> elementNames,
            AssemblySymbol accessWithin)
        {
            _underlyingType = underlyingType;

            // build the fields
            int approxSize = elementNames.IsDefault ? underlyingType.Arity : elementNames.Length;
            var fieldsBuilder = ArrayBuilder<TupleFieldSymbol>.GetInstance(approxSize);
            NamedTypeSymbol currentLinkType = underlyingType;
            int fieldIndex = 0;

            while (currentLinkType.Arity == RestPosition)
            {
                for (int i = 0; i < RestPosition - 1; i++)
                {
                    string elementName = TupleMemberName(elementNames, fieldIndex + 1);
                    var field = TupleFieldSymbol.Create(elementName, this, currentLinkType, fieldIndex, accessWithin);
                    fieldsBuilder.Add(field);

                    fieldIndex++;
                }

                currentLinkType = (NamedTypeSymbol)currentLinkType.TypeArgumentsNoUseSiteDiagnostics[RestPosition - 1];
            }

            for (int i = 0; i < currentLinkType.Arity; i++)
            {
                string elementName = TupleMemberName(elementNames, fieldIndex + 1);
                var field = TupleFieldSymbol.Create(elementName, this, currentLinkType, fieldIndex, accessWithin);
                fieldsBuilder.Add(field);

                fieldIndex++;
            }

            _fields = fieldsBuilder.ToImmutableAndFree();
        }

        /// <summary>
        /// Helps create a TupleTypeSymbol from source.
        /// </summary>
        internal static TupleTypeSymbol Create(
            ImmutableArray<TypeSymbol> elementTypes,
            ImmutableArray<string> elementNames,
            CSharpSyntaxNode syntax,
            Binder binder,
            DiagnosticBag diagnostics
            )
        {
            Debug.Assert(elementNames.IsDefault || elementTypes.Length == elementNames.Length);

            int numElements = elementTypes.Length;

            if (numElements <= 1)
            {
                throw ExceptionUtilities.Unreachable;
        }
            NamedTypeSymbol underlyingType = GetTupleUnderlyingType(elementTypes, syntax, binder, diagnostics);

            return new TupleTypeSymbol(underlyingType, elementNames, binder.Compilation.Assembly);
        }

        /// <summary>
        /// Copy the original tuple, but modify it to use the new underlying type.
        /// </summary>
        private TupleTypeSymbol(TupleTypeSymbol originalTuple,
                                NamedTypeSymbol newUnderlyingType)
        {
            _underlyingType = newUnderlyingType;

            var fieldsBuilder = ArrayBuilder<TupleFieldSymbol>.GetInstance(originalTuple._fields.Length);

            int fieldIndex = 0;
            int remainder = 0;
            NamedTypeSymbol currentLinkType = newUnderlyingType;

            while (true)
            {
                TupleFieldSymbol originalField = originalTuple._fields[fieldIndex];
                TypeSymbol fieldType = currentLinkType.TypeArgumentsNoUseSiteDiagnostics[remainder];
                fieldsBuilder.Add(originalField.WithType(this, currentLinkType, fieldType));

                fieldIndex++;
                if (fieldIndex >= originalTuple._fields.Length)
                {
                    break;
                }

                remainder = fieldIndex % (RestPosition - 1);
                if (remainder == 0)
                {
                    currentLinkType = (NamedTypeSymbol)currentLinkType.TypeArgumentsNoUseSiteDiagnostics[RestPosition - 1];
                }
            }

            _fields = fieldsBuilder.ToImmutableAndFree();
        }

        /// <summary>
        /// Copy the original tuple, but modify it to use new field names.
        /// </summary>
        private TupleTypeSymbol(TupleTypeSymbol originalTuple,
                                ImmutableArray<string> newElementNames)
        {
            _underlyingType = originalTuple._underlyingType;

            var fieldsBuilder = ArrayBuilder<TupleFieldSymbol>.GetInstance(originalTuple._fields.Length);
            var originalFields = originalTuple._fields;
            
            for (int i = 0; i < originalFields.Length; i++)
            {
                fieldsBuilder.Add(originalFields[i].WithName(this, GetFieldNameFromArrayOrDefaultName(newElementNames, i)));
            }

            _fields = fieldsBuilder.ToImmutableAndFree();
        }

        /// <summary>
        /// Copy this tuple, but modify it to use the new underlying type.
        /// </summary>
        internal TupleTypeSymbol WithUnderlyingType(NamedTypeSymbol newUnderlyingType)
        {
            Debug.Assert((object)newUnderlyingType.OriginalDefinition == (object)UnderlyingTupleType.OriginalDefinition);

            return new TupleTypeSymbol(this, newUnderlyingType);
        }

        /// <summary>
        /// Copy this tuple, but modify it to use the new element names.
        /// </summary>
        internal TupleTypeSymbol WithElementNames(ImmutableArray<string> newElementNames)
        {
            Debug.Assert(newElementNames.IsDefault || this._fields.Length == newElementNames.Length);

            var originalFields = _fields;
            for (int i = 0; i < originalFields.Length; i++)
            {
                var originalField = originalFields[i];
                var originalName = originalField.Name;
                var newName = GetFieldNameFromArrayOrDefaultName(newElementNames, i);

                if (originalName != newName)
                {
                    // at least one name is different
                    return new TupleTypeSymbol(this, newElementNames);
                }
            }

            // all names are the same
            return this;
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
                    currentType = (NamedTypeSymbol)currentType.TypeArgumentsNoUseSiteDiagnostics[TupleTypeSymbol.RestPosition - 1];
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
        internal static void GetElementTypes(NamedTypeSymbol underlyingTupleType, ArrayBuilder<TypeSymbol> tupleElementTypes)
        {
            NamedTypeSymbol currentType = underlyingTupleType;

            while (true)
            {
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

        private NamedTypeSymbol GetTupleUnderlyingTypeAndFields(
            ImmutableArray<TypeSymbol> elementTypes,
            ImmutableArray<string> elementNames,
            CSharpSyntaxNode syntax,
            Binder binder,
            DiagnosticBag diagnostics,
            out ImmutableArray<TupleFieldSymbol> fields
            )
        {
            Debug.Assert(elementNames.IsDefault || elementTypes.Length == elementNames.Length);

            int numElements = elementTypes.Length;

            if (numElements <= 1)
            {
                throw ExceptionUtilities.Unreachable;
            }
            NamedTypeSymbol underlyingType = GetTupleUnderlyingType(elementTypes, syntax, binder, diagnostics);

            // build the fields
            var fieldsBuilder = ArrayBuilder<TupleFieldSymbol>.GetInstance(numElements);
            for (int elementIndex = 0; elementIndex < numElements; elementIndex++)
            {
                FieldSymbol underlyingField = TupleFieldSymbol.GetUnderlyingField(numElements, underlyingType, elementIndex, syntax, binder, diagnostics);

                var field = new TupleFieldSymbol(GetFieldNameFromArrayOrDefaultName(elementNames, elementIndex),
                                           this,
                                           elementTypes[elementIndex],
                                           elementIndex + 1,
                                           underlyingField);
                fieldsBuilder.Add(field);
            }
            fields = fieldsBuilder.ToImmutableAndFree();

            return underlyingType;
        }

        /// <summary>
        /// Given an array of names returns a name at given position or a default name for that position.
        /// </summary>
        private static string GetFieldNameFromArrayOrDefaultName(ImmutableArray<string> elementNames, int elementIndex)
        {
            return elementNames.IsDefault ? TupleMemberName(elementIndex + 1) : elementNames[elementIndex];
        }

        /// <summary>
        /// Returns the nested type at a certain depth.
        ///
        /// For depth=0, just return the tuple type as-is.
        /// For depth=1, returns the nested tuple type at position 8.
        /// </summary>
        internal static NamedTypeSymbol GetNestedTupleType(NamedTypeSymbol tupleType, int depth)
        {
            NamedTypeSymbol found = tupleType;
            for (int i = 0; i < depth; i++)
            {
                found = (NamedTypeSymbol)found.TypeArgumentsNoUseSiteDiagnostics[RestPosition - 1];
            }
            return found;
        }

        /// <summary>
        /// Returns the number of nestings required to represent numElements as nested ValueTuples.
        /// For example, for 8 elements, you need 2 ValueTuples and the remainder (ie the size of the last nested ValueTuple) is 1.
        /// </summary>
        internal static int NumberOfValueTuples(int numElements, out int remainder)
        {
            remainder = (numElements - 1) % (RestPosition - 1) + 1;
            return (numElements - 1) / (RestPosition - 1) + 1;
        }

        /// <summary>
        /// Produces the underlying ValueTuple corresponding to this list of element types.
        /// </summary>
        private static NamedTypeSymbol GetTupleUnderlyingType(ImmutableArray<TypeSymbol> elementTypes, CSharpSyntaxNode syntax, Binder binder, DiagnosticBag diagnostics)
        {
            int numElements = elementTypes.Length;
            int remainder;
            int chainLength = NumberOfValueTuples(numElements, out remainder);

            NamedTypeSymbol currentSymbol = default(NamedTypeSymbol);
            NamedTypeSymbol firstTupleType = binder.GetWellKnownType(GetTupleType(remainder), diagnostics, syntax);
            currentSymbol = firstTupleType.Construct(ImmutableArray.Create(elementTypes, (chainLength - 1) * (RestPosition - 1), remainder));

            int loop = chainLength - 1;
            if (loop > 0)
            {
                NamedTypeSymbol chainedTupleType = binder.GetWellKnownType(GetTupleType(RestPosition), diagnostics, syntax);

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

        /// <summary>
        /// Find the well-known ValueTuple type of a given arity.
        /// For example, for arity=2:
        /// returns WellKnownType.System_ValueTuple_T2
        /// </summary>
        internal static WellKnownType GetTupleType(int arity)
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

        internal static string TupleMemberName(ImmutableArray<string> elementNames, int position)
        {
            return elementNames.IsDefault ? TupleMemberName(position) : elementNames[position - 1];
        }

        /// <summary>
        /// Checks whether the field name is reserved and tells us which position it's reserved for.
        ///
        /// For example:
        /// Returns 3 for "Item3".
        /// Returns 0 for "Rest".
        /// Returns -1 for names that aren't reserved.
        /// </summary>
        internal static int IsMemberNameReserved(string name)
        {
            // PROTOTYPE(tuples): handle others like "ToString"?

            if (String.Equals(name, "Rest", StringComparison.Ordinal))
            {
                return 0;
            }
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
        /// <param name="accessWithin">The assembly referencing the type.</param>
        internal static Symbol GetWellKnownMemberInType(NamedTypeSymbol type, WellKnownMember relativeMember, AssemblySymbol accessWithin)
        {
            Debug.Assert(relativeMember >= 0 && relativeMember < WellKnownMember.Count);
            Debug.Assert(type.IsDefinition);

            MemberDescriptor relativeDescriptor = WellKnownMembers.GetDescriptor(relativeMember);
            return CSharpCompilation.GetRuntimeMember(type, ref relativeDescriptor, CSharpCompilation.SpecialMembersSignatureComparer.Instance, accessWithinOpt: accessWithin);
        }

        /// <summary>
        /// Lookup well-known member declaration in provided type and reports diagnostics.
        /// </summary>
        internal static Symbol GetWellKnownMemberInType(NamedTypeSymbol type, WellKnownMember relativeMember, AssemblySymbol accessWithin, DiagnosticBag diagnostics, SyntaxNode syntax)
        {
            Symbol member = GetWellKnownMemberInType(type, relativeMember, accessWithin);

            if ((object)member == null)
            {
                MemberDescriptor relativeDescriptor = WellKnownMembers.GetDescriptor(relativeMember);
                Binder.Error(diagnostics, ErrorCode.ERR_PredefinedTypeMemberNotFoundInAssembly, syntax, relativeDescriptor.Name, type, accessWithin);
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
        /// </summary>
        internal NamedTypeSymbol UnderlyingTupleType
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

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<Symbol> basesBeingResolved = null)
        {
            return _underlyingType.InterfacesNoUseSiteDiagnostics(basesBeingResolved);
        }

        public override bool IsReferenceType
        {
            get
            {
                return _underlyingType.IsReferenceType;
            }
        }

        public override bool IsValueType
        {
            get
            {
                return _underlyingType.IsValueType;
            }
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

        internal sealed override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                // PROTOTYPE(tuples): need to figure what is the right behavior when underlying is obsolete
                return null;
            }
        }

        public override ImmutableArray<Symbol> GetMembers()
        {
            return ImmutableArray<Symbol>.CastUp(_fields);
        }

        public override ImmutableArray<Symbol> GetMembers(string name)
        {
            // PROTOTYPE(tuples): PERF do we need to have a dictionary here?
            //                    tuples will be typically small 2 or 3 elements only
            return ImmutableArray<Symbol>.CastUp(_fields).WhereAsArray(field => field.Name == name);
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name, int arity)
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
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
                // only classes and structs can have instance fields as tuple requires.
                // we need to have some support for classes, but most common case will be struct
                // in broken scenarios (ErrorType, Enum, Delegate, whatever..) we will just default to struct.
                return _underlyingType.TypeKind == TypeKind.Class ? TypeKind.Class : TypeKind.Struct;
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
                return ImmutableArray<Location>.Empty;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        internal override TResult Accept<TArgument, TResult>(CSharpSymbolVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitTupleType(this, argument);
        }

        public override void Accept(CSharpSymbolVisitor visitor)
        {
            visitor.VisitTupleType(this);
        }

        public override TResult Accept<TResult>(CSharpSymbolVisitor<TResult> visitor)
        {
            return visitor.VisitTupleType(this);
        }

        internal override bool Equals(TypeSymbol t2, bool ignoreCustomModifiers, bool ignoreDynamic)
        {
            if (ignoreDynamic)
            {
                // PROTOTYPE(tuples): rename ignoreDynamic or introduce another "ignoreTuple" flag
                //                    if ignoring dynamic, compare underlying tuple types
                if (t2.IsTupleType)
                {
                    t2 = (t2 as TupleTypeSymbol).UnderlyingTupleType;
                }
                return _underlyingType.Equals(t2, ignoreCustomModifiers, ignoreDynamic);
            }

            return this.Equals(t2 as TupleTypeSymbol, ignoreCustomModifiers, ignoreDynamic);
        }

        internal bool Equals(TupleTypeSymbol other)
        {
            return Equals(other, false, false);
        }

        private bool Equals(TupleTypeSymbol other, bool ignoreCustomModifiers, bool ignoreDynamic)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if ((object)other == null || !other._underlyingType.Equals(_underlyingType, ignoreCustomModifiers, ignoreDynamic))
            {
                return false;
            }

            // Make sure field names are the same.
            if (!ignoreDynamic)
            {
                var fields = this._fields;
                var otherFields = other._fields;
                var count = fields.Length;

                for (int i = 0; i < count; i++)
                {
                    if (fields[i].Name != otherFields[i].Name)
                    {
                        return false;
                    }
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
                return _fields.Length;
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
                return _fields.Select(f => f.Name);
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

        #region ITupleTypeSymbol Members

        #endregion

        #region ISymbol Members

        internal override AttributeUsageInfo GetAttributeUsageInfo()
        {
            return AttributeUsageInfo.Null;
        }

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers()
        {
            return this.GetMembers();
        }

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name)
        {
            return this.GetMembers(name);
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

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit()
        {
            throw ExceptionUtilities.Unreachable;
        }

        #endregion
    }
}
