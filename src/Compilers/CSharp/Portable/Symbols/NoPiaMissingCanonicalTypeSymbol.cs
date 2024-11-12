// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A NoPiaMissingCanonicalTypeSymbol is a special kind of ErrorSymbol that represents a NoPia
    /// embedded type symbol that was attempted to be substituted with canonical type, but the
    /// canonical type couldn't be found.
    /// </summary>
    internal class NoPiaMissingCanonicalTypeSymbol : ErrorTypeSymbol
    // TODO: Should probably inherit from MissingMetadataType.TopLevel, but review TypeOf checks for MissingMetadataType.
    {
        private readonly AssemblySymbol _embeddingAssembly;
        private readonly string _fullTypeName;
        private readonly string? _guid;
        private readonly string? _scope;
        private readonly string? _identifier;

        public NoPiaMissingCanonicalTypeSymbol(
            AssemblySymbol embeddingAssembly,
            string fullTypeName,
            string? guid,
            string? scope,
            string? identifier,
            TupleExtraData? tupleData = null)
            : base(tupleData)
        {
            _embeddingAssembly = embeddingAssembly;
            _fullTypeName = fullTypeName;
            _guid = guid;
            _scope = scope;
            _identifier = identifier;
        }

        protected override NamedTypeSymbol WithTupleDataCore(TupleExtraData newData)
        {
            return new NoPiaMissingCanonicalTypeSymbol(_embeddingAssembly, _fullTypeName, _guid, _scope, _identifier, newData);
        }

        public AssemblySymbol EmbeddingAssembly
        {
            get
            {
                return _embeddingAssembly;
            }
        }

        public string FullTypeName
        {
            get
            {
                return _fullTypeName;
            }
        }

        internal override bool MangleName
        {
            get
            {
                // Cannot be generic.
                Debug.Assert(Arity == 0);
                return false;
            }
        }

        internal sealed override bool IsFileLocal => false;
        internal sealed override FileIdentifier? AssociatedFileIdentifier => null;

        public string? Guid
        {
            get
            {
                return _guid;
            }
        }

        public string? Scope
        {
            get
            {
                return _scope;
            }
        }

        public string? Identifier
        {
            get
            {
                return _identifier;
            }
        }

        internal override DiagnosticInfo ErrorInfo
        {
            get
            {
                return new CSDiagnosticInfo(ErrorCode.ERR_NoCanonicalView, _fullTypeName);
            }
        }

        public override int GetHashCode()
        {
            return RuntimeHelpers.GetHashCode(this);
        }

        internal override bool Equals(TypeSymbol t2, TypeCompareKind comparison)
        {
            return ReferenceEquals(this, t2);
        }
    }
}
