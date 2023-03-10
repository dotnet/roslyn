// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    /// <summary>
    /// A display class field representing a local, exposed
    /// as a local on the original method.
    /// </summary>
    internal sealed class EEDisplayClassFieldLocalSymbol : EELocalSymbolBase
    {
        private readonly DisplayClassVariable _variable;

        public EEDisplayClassFieldLocalSymbol(DisplayClassVariable variable)
        {
            _variable = variable;

            // Verify all type parameters are substituted.
            Debug.Assert(this.ContainingSymbol.IsContainingSymbolOfAllTypeParameters(this.Type));
        }

        internal override EELocalSymbolBase ToOtherMethod(MethodSymbol method, TypeMap typeMap)
        {
            return new EEDisplayClassFieldLocalSymbol(_variable.ToOtherMethod(method, typeMap));
        }

        public override string Name
        {
            get { return _variable.Name; }
        }

        internal override LocalDeclarationKind DeclarationKind
        {
            get { return LocalDeclarationKind.RegularVariable; }
        }

        internal override SyntaxToken IdentifierToken
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        public override Symbol ContainingSymbol
        {
            get { return _variable.ContainingSymbol; }
        }

        public override TypeWithAnnotations TypeWithAnnotations
        {
            get { return TypeWithAnnotations.Create(_variable.Type); }
        }

        internal override bool IsPinned
        {
            get { return false; }
        }

        internal override bool IsKnownToReferToTempIfReferenceType
        {
            get { return false; }
        }

        internal override bool IsCompilerGenerated
        {
            get { return false; }
        }

        public override RefKind RefKind
        {
            get { return RefKind.None; }
        }

        public override ImmutableArray<Location> Locations
        {
            get { return NoLocations; }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get { return ImmutableArray<SyntaxReference>.Empty; }
        }
    }
}
