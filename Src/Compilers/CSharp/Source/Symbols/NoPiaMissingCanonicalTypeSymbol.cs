// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    /// canonocal type couldn't be found.
    /// </summary>
    internal class NoPiaMissingCanonicalTypeSymbol : ErrorTypeSymbol
    // TODO: Should probably inherit from MissingMetadataType.TopLevel, but review TypeOf checks for MissingMetadataType.
    {
        private readonly AssemblySymbol embeddingAssembly;
        private readonly string fullTypeName;
        private readonly string guid;
        private readonly string scope;
        private readonly string identifier;

        public NoPiaMissingCanonicalTypeSymbol(
            AssemblySymbol embeddingAssembly,
            string fullTypeName,
            string guid,
            string scope,
            string identifier)
        {
            this.embeddingAssembly = embeddingAssembly;
            this.fullTypeName = fullTypeName;
            this.guid = guid;
            this.scope = scope;
            this.identifier = identifier;
        }

        public AssemblySymbol EmbeddingAssembly
        {
            get
            {
                return this.embeddingAssembly;
            }
        }

        public string FullTypeName
        {
            get
            {
                return this.fullTypeName;
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
                return this.guid;
            }
        }

        public string Scope
        {
            get
            {
                return this.scope;
            }
        }

        public string Identifier
        {
            get
            {
                return this.identifier;
            }
        }

        internal override DiagnosticInfo ErrorInfo
        {
            get
            {
                return new CSDiagnosticInfo(ErrorCode.ERR_NoCanonicalView, fullTypeName);
            }
        }

        public override int GetHashCode()
        {
            return RuntimeHelpers.GetHashCode(this);
        }

        internal override bool Equals(TypeSymbol t2, bool ignoreCustomModifiers, bool ignoreDynamic)
        {
            return ReferenceEquals(this, t2);
        }
    }
}