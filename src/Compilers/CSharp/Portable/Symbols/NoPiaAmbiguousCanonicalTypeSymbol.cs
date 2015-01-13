// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A NoPiaAmbiguousCanonicalTypeSymbol is a special kind of ErrorSymbol that represents a NoPia
    /// embedded type symbol that was attempted to be substituted with canonical type, but the
    /// canonocal type was ambiguous.
    /// </summary>
    internal class NoPiaAmbiguousCanonicalTypeSymbol : ErrorTypeSymbol
    {
        private readonly AssemblySymbol embeddingAssembly;
        private readonly NamedTypeSymbol firstCandidate;
        private readonly NamedTypeSymbol secondCandidate;

        public NoPiaAmbiguousCanonicalTypeSymbol(
            AssemblySymbol embeddingAssembly,
            NamedTypeSymbol firstCandidate,
            NamedTypeSymbol secondCandidate)
        {
            this.embeddingAssembly = embeddingAssembly;
            this.firstCandidate = firstCandidate;
            this.secondCandidate = secondCandidate;
        }

        internal override bool MangleName
        {
            get
            {
                Debug.Assert(Arity == 0);
                return false;
            }
        }

        public AssemblySymbol EmbeddingAssembly
        {
            get
            {
                return this.embeddingAssembly;
            }
        }

        public NamedTypeSymbol FirstCandidate
        {
            get
            {
                return this.firstCandidate;
            }
        }

        public NamedTypeSymbol SecondCandidate
        {
            get
            {
                return this.secondCandidate;
            }
        }

        internal override DiagnosticInfo ErrorInfo
        {
            get
            {
                return new CSDiagnosticInfo(ErrorCode.ERR_NoCanonicalView, firstCandidate);
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