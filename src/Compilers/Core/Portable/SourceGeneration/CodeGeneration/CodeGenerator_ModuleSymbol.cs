// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.SourceGeneration
{
    internal static partial class CodeGenerator
    {
        public static IModuleSymbol Module(INamespaceSymbol globalNamespace, ImmutableArray<AttributeData> attributes = default)
            => new ModuleSymbol(globalNamespace, attributes);

        public static IModuleSymbol With(
            this IModuleSymbol module,
            Optional<INamespaceSymbol> globalNamespace = default,
            Optional<ImmutableArray<AttributeData>> attributes = default)
        {
            return new ModuleSymbol(
                globalNamespace.GetValueOr(module.GlobalNamespace),
                attributes.GetValueOr(module.GetAttributes()));
        }

        private class ModuleSymbol : Symbol, IModuleSymbol
        {
            private readonly ImmutableArray<AttributeData> _attributes;

            public ModuleSymbol(
                INamespaceSymbol globalNamespace,
                ImmutableArray<AttributeData> attributes)
            {
                GlobalNamespace = globalNamespace;
                _attributes = attributes;
            }

            public override SymbolKind Kind => SymbolKind.NetModule;
            public INamespaceSymbol GlobalNamespace { get; }

            public override ImmutableArray<AttributeData> GetAttributes()
                => _attributes;

            public override void Accept(SymbolVisitor visitor)
                => visitor.VisitModule(this);

            public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
                => visitor.VisitModule(this);

            #region default implementation

            public ImmutableArray<AssemblyIdentity> ReferencedAssemblies => throw new NotImplementedException();
            public ImmutableArray<IAssemblySymbol> ReferencedAssemblySymbols => throw new NotImplementedException();
            public ModuleMetadata GetMetadata() => throw new NotImplementedException();
            public INamespaceSymbol GetModuleNamespace(INamespaceSymbol namespaceSymbol) => throw new NotImplementedException();

            #endregion
        }
    }
}
