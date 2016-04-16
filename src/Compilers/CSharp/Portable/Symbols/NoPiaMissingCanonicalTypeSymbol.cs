// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

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
        private readonly string _guid;
        private readonly string _scope;
        private readonly string _identifier;

        public NoPiaMissingCanonicalTypeSymbol(
            AssemblySymbol embeddingAssembly,
            string fullTypeName,
            string guid,
            string scope,
            string identifier)
        {
            _embeddingAssembly = embeddingAssembly;
            _fullTypeName = fullTypeName;
            _guid = guid;
            _scope = scope;
            _identifier = identifier;
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

        public string Guid
        {
            get
            {
                return _guid;
            }
        }

        public string Scope
        {
            get
            {
                return _scope;
            }
        }

        public string Identifier
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

        internal override bool Equals(TypeSymbol t2, bool ignoreCustomModifiersAndArraySizesAndLowerBounds, bool ignoreDynamic)
        {
            return ReferenceEquals(this, t2);
        }
    }
}
