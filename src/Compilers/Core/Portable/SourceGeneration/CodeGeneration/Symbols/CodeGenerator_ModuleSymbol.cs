// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.SourceGeneration
{
    internal static partial class CodeGenerator
    {
        public static IModuleSymbol Module(
            INamespaceSymbol globalNamespace,
            ImmutableArray<AttributeData> attributes = default,
            ISymbol containingSymbol = null)
        {
            return new ModuleSymbol(globalNamespace, attributes, containingSymbol);
        }

        private static IModuleSymbol WithGlobalNamespaces(this IModuleSymbol symbol, INamespaceSymbol globalNamespace)
            => With(symbol, globalNamespace: ToOptional(globalNamespace));

        public static IModuleSymbol WithAttributes(this IModuleSymbol symbol, params AttributeData[] attributes)
            => WithAttributes(symbol, (IEnumerable<AttributeData>)attributes);

        public static IModuleSymbol WithAttributes(this IModuleSymbol symbol, IEnumerable<AttributeData> attributes)
            => WithAttributes(symbol, attributes.ToImmutableArray());

        public static IModuleSymbol WithAttributes(this IModuleSymbol symbol, ImmutableArray<AttributeData> attributes)
            => With(symbol, attributes: ToOptional(attributes));

        public static IModuleSymbol WithContainingSymbol(this IModuleSymbol symbol, ISymbol containingSymbol)
            => With(symbol, containingSymbol: ToOptional(containingSymbol));

        private static IModuleSymbol With(
            this IModuleSymbol module,
            Optional<INamespaceSymbol> globalNamespace = default,
            Optional<ImmutableArray<AttributeData>> attributes = default,
            Optional<ISymbol> containingSymbol = default)
        {
            return new ModuleSymbol(
                globalNamespace.GetValueOr(module.GlobalNamespace),
                attributes.GetValueOr(module.GetAttributes()),
                containingSymbol.GetValueOr(module.ContainingSymbol));
        }

        private class ModuleSymbol : Symbol, IModuleSymbol
        {
            private readonly ImmutableArray<AttributeData> _attributes;

            public ModuleSymbol(
                INamespaceSymbol globalNamespace,
                ImmutableArray<AttributeData> attributes,
                ISymbol containingSymbol)
            {
                GlobalNamespace = globalNamespace.With(containingSymbol: this);
                _attributes = attributes;
                ContainingSymbol = containingSymbol;
            }

            public override ISymbol ContainingSymbol { get; }
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
            public INamespaceSymbol GetModuleNamespace(INamespaceSymbol namespaceSymbol) => throw new NotImplementedException();
            public ModuleMetadata GetMetadata() => throw new NotImplementedException();

            #endregion
        }
    }
}
