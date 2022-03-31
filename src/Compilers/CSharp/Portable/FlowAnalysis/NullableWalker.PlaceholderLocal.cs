// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class NullableWalker
    {
        /// <summary>
        /// A symbol to be used as a placeholder for an instance being constructed by
        /// <see cref="BoundObjectCreationExpression"/>, or the input expression of a pattern-matching operation.
        /// It is used to track the state of an expression, such as members being initialized.
        /// </summary>
        private sealed class PlaceholderLocal : LocalSymbol
        {
            private readonly Symbol _containingSymbol;
            private readonly TypeWithAnnotations _type;
            private readonly object _identifier;

            public PlaceholderLocal(Symbol containingSymbol, object identifier, TypeWithAnnotations type)
            {
                Debug.Assert(identifier != null);
                Debug.Assert(containingSymbol is null || containingSymbol.DeclaringCompilation is not null);
                _containingSymbol = containingSymbol;
                _type = type;
                _identifier = identifier;
            }

            public override bool Equals(Symbol obj, TypeCompareKind compareKind)
            {
                if ((object)this == obj)
                {
                    return true;
                }

                return obj is PlaceholderLocal other && _identifier.Equals(other._identifier);
            }

            public override int GetHashCode() => _identifier.GetHashCode();
            internal override SyntaxNode ScopeDesignatorOpt => null;
            public override Symbol ContainingSymbol => _containingSymbol;
            public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;
            public override ImmutableArray<Location> Locations => ImmutableArray<Location>.Empty;
            public override TypeWithAnnotations TypeWithAnnotations => _type;
            internal override LocalDeclarationKind DeclarationKind => LocalDeclarationKind.None;
            internal override SyntaxToken IdentifierToken => throw ExceptionUtilities.Unreachable;
            internal override bool IsCompilerGenerated => true;
            internal override bool IsImportedFromMetadata => false;
            internal override bool IsPinned => false;
            public override RefKind RefKind => RefKind.None;
            internal override SynthesizedLocalKind SynthesizedKind => throw ExceptionUtilities.Unreachable;
            internal override ConstantValue GetConstantValue(SyntaxNode node, LocalSymbol inProgress, BindingDiagnosticBag diagnostics = null) => null;
            internal override ImmutableBindingDiagnostic<AssemblySymbol> GetConstantValueDiagnostics(BoundExpression boundInitValue) => ImmutableBindingDiagnostic<AssemblySymbol>.Empty;
            internal override SyntaxNode GetDeclaratorSyntax() => throw ExceptionUtilities.Unreachable;
            internal override LocalSymbol WithSynthesizedLocalKindAndSyntax(SynthesizedLocalKind kind, SyntaxNode syntax) => throw ExceptionUtilities.Unreachable;
            internal override uint ValEscapeScope => throw ExceptionUtilities.Unreachable;
            internal override uint RefEscapeScope => throw ExceptionUtilities.Unreachable;
        }
    }
}
