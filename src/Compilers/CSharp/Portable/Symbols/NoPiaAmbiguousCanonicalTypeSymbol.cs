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
            NamedTypeSymbol secondCandidate,
            TupleExtraData? tupleData = null)
            : base(tupleData)
        {
            _embeddingAssembly = embeddingAssembly;
            _firstCandidate = firstCandidate;
            _secondCandidate = secondCandidate;
        }

        protected override NamedTypeSymbol WithTupleDataCore(TupleExtraData newData)
        {
            return new NoPiaAmbiguousCanonicalTypeSymbol(_embeddingAssembly, _firstCandidate, _secondCandidate, newData);
        }

        internal override bool MangleName
        {
            get
            {
                Debug.Assert(Arity == 0);
                return false;
            }
        }

        internal sealed override bool IsFileLocal => false;
        internal sealed override FileIdentifier? AssociatedFileIdentifier => null;

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

        internal override bool Equals(TypeSymbol t2, TypeCompareKind comparison)
        {
            return ReferenceEquals(this, t2);
        }
    }
}
