// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal class InjectedNamespaceSymbol : SourceNamespaceSymbol
    {
        internal InjectedNamespaceSymbol(SourceModuleSymbol module, Symbol container, MergedNamespaceDeclaration mergedDeclaration, DiagnosticBag diagnostics)
            : base(module, container, mergedDeclaration, diagnostics)
        {
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;
        public override ImmutableArray<Location> Locations => ImmutableArray.Create(Location.None);
        public override bool IsImplicitlyDeclared => true;
    }
}
