// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
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
    /// canonical type was ambiguous.
    /// </summary>
    internal class NoPiaAmbiguousCanonicalTypeSymbol : ErrorTypeSymbol
    {
        private readonly AssemblySymbol _embeddingAssembly;
        private readonly NamedTypeSymbol _firstCandidate;
        private readonly NamedTypeSymbol _secondCandidate;

        public NoPiaAmbiguousCanonicalTypeSymbol(
            AssemblySymbol embeddingAssembly,
            NamedTypeSymbol firstCandidate,
            NamedTypeSymbol secondCandidate)
        {
            _embeddingAssembly = embeddingAssembly;
            _firstCandidate = firstCandidate;
            _secondCandidate = secondCandidate;
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
                return _embeddingAssembly;
            }
        }

        public NamedTypeSymbol FirstCandidate
        {
            get
            {
                return _firstCandidate;
            }
        }

        public NamedTypeSymbol SecondCandidate
        {
            get
            {
                return _secondCandidate;
            }
        }

        internal override DiagnosticInfo ErrorInfo
        {
            get
            {
                return new CSDiagnosticInfo(ErrorCode.ERR_NoCanonicalView, _firstCandidate);
            }
        }

        public override int GetHashCode()
        {
            return RuntimeHelpers.GetHashCode(this);
        }

        internal override bool Equals(TypeSymbol t2, TypeCompareKind comparison, IReadOnlyDictionary<TypeParameterSymbol, bool> isValueTypeOverrideOpt = null)
        {
            return ReferenceEquals(this, t2);
        }
    }
}
