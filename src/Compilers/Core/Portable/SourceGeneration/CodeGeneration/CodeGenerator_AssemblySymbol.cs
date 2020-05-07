// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.SourceGeneration
{
    internal static partial class CodeGenerator
    {
        public static IAssemblySymbol Assembly(IModuleSymbol module, ImmutableArray<AttributeData> attributes = default)
            => new AssemblySymbol(module, attributes);

        public static IAssemblySymbol With(
            this IAssemblySymbol assembly,
            Optional<IModuleSymbol> module = default,
            Optional<ImmutableArray<AttributeData>> attributes = default)
        {
            return new AssemblySymbol(
                module.GetValueOr(assembly.Modules.First()),
                attributes.GetValueOr(assembly.GetAttributes()));
        }

        private class AssemblySymbol : Symbol, IAssemblySymbol
        {
            private readonly ImmutableArray<IModuleSymbol> _modules;
            private readonly ImmutableArray<AttributeData> _attributes;

            public AssemblySymbol(
                IModuleSymbol module,
                ImmutableArray<AttributeData> attributes)
            {
                _modules = ImmutableArray.Create(module.With(containingSymbol: this));
                _attributes = attributes;
            }

            public override SymbolKind Kind => SymbolKind.Assembly;

            public IEnumerable<IModuleSymbol> Modules
                => _modules;

            public override ImmutableArray<AttributeData> GetAttributes()
                => _attributes;

            public override void Accept(SymbolVisitor visitor)
                => visitor.VisitAssembly(this);

            public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
                => visitor.VisitAssembly(this);

            #region default implementation

            public AssemblyIdentity Identity => throw new NotImplementedException();
            public AssemblyMetadata GetMetadata() => throw new NotImplementedException();
            public bool GivesAccessTo(IAssemblySymbol toAssembly) => throw new NotImplementedException();
            public bool IsInteractive => throw new NotImplementedException();
            public bool MightContainExtensionMethods => throw new NotImplementedException();
            public ICollection<string> NamespaceNames => throw new NotImplementedException();
            public ICollection<string> TypeNames => throw new NotImplementedException();
            public INamedTypeSymbol GetTypeByMetadataName(string fullyQualifiedMetadataName) => throw new NotImplementedException();
            public INamedTypeSymbol ResolveForwardedType(string fullyQualifiedMetadataName) => throw new NotImplementedException();
            public INamespaceSymbol GlobalNamespace => throw new NotImplementedException();

            #endregion
        }
    }
}
