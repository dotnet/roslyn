// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#nullable enable

using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class UpdatedContainingSymbolAndNullableAnnotationLocal : LocalSymbol
    {
        internal UpdatedContainingSymbolAndNullableAnnotationLocal(SourceLocalSymbol underlyingLocal, Symbol updatedContainingSymbol, TypeWithAnnotations updatedType)
        {
            Debug.Assert(underlyingLocal is object);
            Debug.Assert(updatedContainingSymbol is object);
            UnderlyingLocal = underlyingLocal;
            ContainingSymbol = updatedContainingSymbol;
            TypeWithAnnotations = updatedType;
        }

        private SourceLocalSymbol UnderlyingLocal { get; }
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
                UpdatedContainingSymbolAndNullableAnnotationLocal updated => updated.UnderlyingLocal,
                SourceLocalSymbol source => source,
                _ => null
            };

            if (otherSource is null || !UnderlyingLocal.Equals(otherSource, compareKind))
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
        public override int GetHashCode() => UnderlyingLocal.GetHashCode();

        #region Forwards
        public override RefKind RefKind => UnderlyingLocal.RefKind;
        public override ImmutableArray<Location> Locations => UnderlyingLocal.Locations;
        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => UnderlyingLocal.DeclaringSyntaxReferences;
        public override string Name => UnderlyingLocal.Name;
        public override bool IsImplicitlyDeclared => UnderlyingLocal.IsImplicitlyDeclared;
        internal override LocalDeclarationKind DeclarationKind => UnderlyingLocal.DeclarationKind;
        internal override SynthesizedLocalKind SynthesizedKind => UnderlyingLocal.SynthesizedKind;
        internal override SyntaxNode ScopeDesignatorOpt => UnderlyingLocal.ScopeDesignatorOpt;
        internal override bool IsImportedFromMetadata => UnderlyingLocal.IsImportedFromMetadata;
        internal override SyntaxToken IdentifierToken => UnderlyingLocal.IdentifierToken;
        internal override bool IsPinned => UnderlyingLocal.IsPinned;
        internal override bool IsCompilerGenerated => UnderlyingLocal.IsCompilerGenerated;
        internal override uint RefEscapeScope => UnderlyingLocal.RefEscapeScope;
        internal override uint ValEscapeScope => UnderlyingLocal.ValEscapeScope;
        internal override ConstantValue GetConstantValue(SyntaxNode node, LocalSymbol inProgress, DiagnosticBag? diagnostics = null) =>
            UnderlyingLocal.GetConstantValue(node, inProgress, diagnostics);
        internal override ImmutableArray<Diagnostic> GetConstantValueDiagnostics(BoundExpression boundInitValue) =>
            UnderlyingLocal.GetConstantValueDiagnostics(boundInitValue);
        internal override SyntaxNode GetDeclaratorSyntax() =>
            UnderlyingLocal.GetDeclaratorSyntax();
        internal override LocalSymbol WithSynthesizedLocalKindAndSyntax(SynthesizedLocalKind kind, SyntaxNode syntax) =>
            throw ExceptionUtilities.Unreachable;
        #endregion
    }
}
