// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Cci = Microsoft.Cci;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// When indexer overload resolution fails, we have two options:
    ///   1) Create a BoundBadExpression with the candidates as child nodes;
    ///   2) Create a BoundIndexerAccess with the error flag set.
    ///   
    /// Option 2 is preferable, because it retains information about the arguments
    /// (names, ref kind, etc), and results in better output from flow analysis.
    /// However, we can't create a BoundIndexerAccess with a null indexer symbol,
    /// so we create an ErrorPropertySymbol to fill the gap.
    /// </summary>
    internal sealed class ErrorPropertySymbol : PropertySymbol
    {
        private readonly Symbol _containingSymbol;
        private readonly TypeWithAnnotations _typeWithAnnotations;
        private readonly string _name;
        private readonly bool _isIndexer;
        private readonly bool _isIndexedProperty;

        public ErrorPropertySymbol(Symbol containingSymbol, TypeSymbol type, string name, bool isIndexer, bool isIndexedProperty)
        {
            _containingSymbol = containingSymbol;
            _typeWithAnnotations = TypeWithAnnotations.Create(type);
            _name = name;
            _isIndexer = isIndexer;
            _isIndexedProperty = isIndexedProperty;
        }

        public override Symbol ContainingSymbol { get { return _containingSymbol; } }

        public override RefKind RefKind { get { return RefKind.None; } }

        public override TypeWithAnnotations TypeWithAnnotations { get { return _typeWithAnnotations; } }

        public override string Name { get { return _name; } }

        internal override bool HasSpecialName { get { return false; } }

        public override bool IsIndexer { get { return _isIndexer; } }

        public override bool IsIndexedProperty { get { return _isIndexedProperty; } }

        // CONSIDER: could create an ErrorMethodSymbol
        public override MethodSymbol GetMethod { get { return null; } }

        // CONSIDER: could create an ErrorMethodSymbol
        public override MethodSymbol SetMethod { get { return null; } }

        public override ImmutableArray<Location> Locations { get { return ImmutableArray<Location>.Empty; } }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences { get { return ImmutableArray<SyntaxReference>.Empty; } }

        public override Accessibility DeclaredAccessibility { get { return Accessibility.NotApplicable; } }

        public override bool IsStatic { get { return false; } }

        public override bool IsVirtual { get { return false; } }

        public override bool IsOverride { get { return false; } }

        public override bool IsAbstract { get { return false; } }

        public override bool IsSealed { get { return false; } }

        public override bool IsExtern { get { return false; } }

        internal override bool IsRequired => false;

        internal sealed override bool HasUnscopedRefAttribute => false;

        internal sealed override bool IsCallerUnsafe => false;

        internal sealed override ObsoleteAttributeData ObsoleteAttributeData { get { return null; } }

        public override ImmutableArray<ParameterSymbol> Parameters { get { return ImmutableArray<ParameterSymbol>.Empty; } }

        internal override Cci.CallingConvention CallingConvention { get { return Cci.CallingConvention.Default; } }

        internal override bool MustCallMethodsDirectly { get { return false; } }

        public override ImmutableArray<PropertySymbol> ExplicitInterfaceImplementations { get { return ImmutableArray<PropertySymbol>.Empty; } }

        public override ImmutableArray<CustomModifier> RefCustomModifiers { get { return ImmutableArray<CustomModifier>.Empty; } }

        internal override int TryGetOverloadResolutionPriority() => 0;
    }
}
