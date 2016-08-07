// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a field of a tuple type (such as (int, byte).Item1)
    /// that doesn't have a corresponding backing field within the tuple underlying type.
    /// Created in response to an error condition.
    /// </summary>
    internal sealed class TupleErrorFieldSymbol : SynthesizedFieldSymbolBase
    {
        private readonly TypeSymbol _type;

        /// <summary>
        /// If this field represents a tuple element (including the name match), 
        /// id is an index of the element (zero-based).
        /// Otherwise, (-1 - [index in members array]);
        /// </summary>
        private readonly int _tupleFieldId;

        private readonly ImmutableArray<Location> _locations;
        private readonly DiagnosticInfo _useSiteDiagnosticInfo;

        public TupleErrorFieldSymbol(NamedTypeSymbol container, string name, int tupleFieldId, Location location, TypeSymbol type, DiagnosticInfo useSiteDiagnosticInfo)
            : base(container, name, isPublic:true, isReadOnly:false, isStatic:false)
        {
            Debug.Assert(name != null);
            _type = type;
            _locations = location == null ? ImmutableArray<Location>.Empty : ImmutableArray.Create(location);
            _useSiteDiagnosticInfo = useSiteDiagnosticInfo;
            _tupleFieldId = tupleFieldId;
        }

        public override bool IsTupleField
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// If this field represents a tuple element (including the name match), 
        /// id is an index of the element (zero-based).
        /// Otherwise, (-1 - [index in members array]);
        /// </summary>
        public override int TupleElementIndex
        {
            get
            {
                return _tupleFieldId;
            }
        }

        public override FieldSymbol TupleUnderlyingField
        {
            get
            {
                // Failed to find one
                return null;
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

        public override bool IsImplicitlyDeclared
        {
            get
            {
                return false;
            }
        }

        internal override bool SuppressDynamicAttribute
        {
            get
            {
                return true;
            }
        }

        internal override TypeSymbol GetFieldType(ConsList<FieldSymbol> fieldsBeingBound)
        {
            return _type;
        }

        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            return _useSiteDiagnosticInfo;
        }

        public override sealed int GetHashCode()
        {
            return Hash.Combine(ContainingType.GetHashCode(), _tupleFieldId.GetHashCode());
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TupleErrorFieldSymbol);
        }

        public bool Equals(TupleErrorFieldSymbol other)
        {
            if ((object)other == this)
            {
                return true;
            }

            return (object)other != null && _tupleFieldId == other._tupleFieldId && ContainingType == other.ContainingType;
        }
    }
}