// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class UpdatedContainingSymbolAndNullableAnnotationLocal : LocalSymbol
    {
        /// <summary>
        /// Creates a new <see cref="UpdatedContainingSymbolAndNullableAnnotationLocal"/> for testing purposes,
        /// which does not verify that the containing symbol matches the original containing symbol.
        /// </summary>
        internal static UpdatedContainingSymbolAndNullableAnnotationLocal CreateForTest(SourceLocalSymbol underlyingLocal, Symbol updatedContainingSymbol, TypeWithAnnotations updatedType)
        {
            return new UpdatedContainingSymbolAndNullableAnnotationLocal(underlyingLocal, updatedContainingSymbol, updatedType, assertContaining: false);
        }

        private UpdatedContainingSymbolAndNullableAnnotationLocal(SourceLocalSymbol underlyingLocal, Symbol updatedContainingSymbol, TypeWithAnnotations updatedType, bool assertContaining)
        {
            RoslynDebug.Assert(underlyingLocal is object);
            RoslynDebug.Assert(updatedContainingSymbol is object);
            Debug.Assert(updatedContainingSymbol.DeclaringCompilation is not null);
            Debug.Assert(!assertContaining || updatedContainingSymbol.Equals(underlyingLocal.ContainingSymbol, TypeCompareKind.AllNullableIgnoreOptions));
            ContainingSymbol = updatedContainingSymbol;
            TypeWithAnnotations = updatedType;
            _underlyingLocal = underlyingLocal;
        }

        internal UpdatedContainingSymbolAndNullableAnnotationLocal(SourceLocalSymbol underlyingLocal, Symbol updatedContainingSymbol, TypeWithAnnotations updatedType)
            : this(underlyingLocal, updatedContainingSymbol, updatedType, assertContaining: true)
        {
        }

        private readonly SourceLocalSymbol _underlyingLocal;
        public override Symbol ContainingSymbol { get; }
        public override TypeWithAnnotations TypeWithAnnotations { get; }

        public override bool Equals(Symbol other, TypeCompareKind compareKind)
        {
            if (other == (object)this)
            {
                return true;
            }

            if (!(other is LocalSymbol otherLocal))
            {
                return false;
            }

            SourceLocalSymbol? otherSource = otherLocal switch
            {
                UpdatedContainingSymbolAndNullableAnnotationLocal updated => updated._underlyingLocal,
                SourceLocalSymbol source => source,
                _ => null
            };

            if (otherSource is null || !_underlyingLocal.Equals(otherSource, compareKind))
            {
                return false;
            }

            var ignoreNullable = (compareKind & TypeCompareKind.AllNullableIgnoreOptions) != 0;
            return ignoreNullable ||
                (TypeWithAnnotations.Equals(otherLocal.TypeWithAnnotations, compareKind) &&
                 ContainingSymbol.Equals(otherLocal.ContainingSymbol, compareKind));
        }

        // The default equality for symbols does not include nullability, so we directly
        // delegate to the underlying local for its hashcode, as neither TypeWithAnnotations
        // nor ContainingSymbol will differ from UnderlyingLocal by more than nullability.
        public override int GetHashCode() => _underlyingLocal.GetHashCode();

        #region Forwards
        public override RefKind RefKind => _underlyingLocal.RefKind;
        public override ImmutableArray<Location> Locations => _underlyingLocal.Locations;
        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => _underlyingLocal.DeclaringSyntaxReferences;
        public override string Name => _underlyingLocal.Name;
        public override bool IsImplicitlyDeclared => _underlyingLocal.IsImplicitlyDeclared;
        internal override LocalDeclarationKind DeclarationKind => _underlyingLocal.DeclarationKind;
        internal override SynthesizedLocalKind SynthesizedKind => _underlyingLocal.SynthesizedKind;
        internal override SyntaxNode ScopeDesignatorOpt => _underlyingLocal.ScopeDesignatorOpt;
        internal override bool IsImportedFromMetadata => _underlyingLocal.IsImportedFromMetadata;
        internal override SyntaxToken IdentifierToken => _underlyingLocal.IdentifierToken;
        internal override bool IsPinned => _underlyingLocal.IsPinned;
        internal override bool IsKnownToReferToTempIfReferenceType => _underlyingLocal.IsKnownToReferToTempIfReferenceType;
        internal override bool IsCompilerGenerated => _underlyingLocal.IsCompilerGenerated;
        internal override ScopedKind Scope => _underlyingLocal.Scope;
        internal override ConstantValue GetConstantValue(SyntaxNode node, LocalSymbol inProgress, BindingDiagnosticBag? diagnostics = null) =>
            _underlyingLocal.GetConstantValue(node, inProgress, diagnostics);
        internal override ImmutableBindingDiagnostic<AssemblySymbol> GetConstantValueDiagnostics(BoundExpression boundInitValue) =>
            _underlyingLocal.GetConstantValueDiagnostics(boundInitValue);
        internal override SyntaxNode GetDeclaratorSyntax() =>
            _underlyingLocal.GetDeclaratorSyntax();
        internal override bool HasSourceLocation
            => _underlyingLocal.HasSourceLocation;
        internal override LocalSymbol WithSynthesizedLocalKindAndSyntax(
            SynthesizedLocalKind kind, SyntaxNode syntax
#if DEBUG
            ,
            [CallerLineNumber] int createdAtLineNumber = 0,
            [CallerFilePath] string? createdAtFilePath = null
#endif
            ) => throw ExceptionUtilities.Unreachable();
        #endregion
    }
}
