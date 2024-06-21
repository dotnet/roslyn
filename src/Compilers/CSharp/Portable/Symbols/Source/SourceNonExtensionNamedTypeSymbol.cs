// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SourceNonExtensionNamedTypeSymbol : SourceNamedTypeSymbol
    {
        internal SourceNonExtensionNamedTypeSymbol(NamespaceOrTypeSymbol containingSymbol, MergedTypeDeclaration declaration, BindingDiagnosticBag diagnostics, TupleExtraData? tupleData = null)
            : base(containingSymbol, declaration, diagnostics, tupleData)
        {
        }

        internal override bool IsExtension => false;
        internal override bool IsExplicitExtension => false;

        internal override TypeSymbol? GetExtendedTypeNoUseSiteDiagnostics(ConsList<TypeSymbol>? basesBeingResolved) => null;

        internal override TypeSymbol? GetDeclaredExtensionUnderlyingType()
            => throw ExceptionUtilities.Unreachable();

        internal override Symbol? TryGetCorrespondingStaticMetadataExtensionMember(Symbol member) => null;

        protected override void CheckUnderlyingType(BindingDiagnosticBag diagnostics)
            => throw ExceptionUtilities.Unreachable();
    }
}
