// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal abstract class SourceOrRecordPropertySymbol : PropertySymbol, IAttributeTargetSymbol
    {
        public Location Location { get; }

        public SourceOrRecordPropertySymbol(Location location)
        {
            Location = location;
        }

        internal abstract SynthesizedBackingFieldSymbol BackingField { get; }

        internal abstract bool IsAutoProperty { get; }

        internal abstract bool HasPointerType { get; }

        public abstract SyntaxList<AttributeListSyntax> AttributeDeclarationSyntaxList { get; }

        protected abstract IAttributeTargetSymbol AttributesOwner { get; }

        protected abstract AttributeLocation AllowedAttributeLocations { get; }

        protected abstract AttributeLocation DefaultAttributeLocation { get; }

        IAttributeTargetSymbol IAttributeTargetSymbol.AttributesOwner => AttributesOwner;

        AttributeLocation IAttributeTargetSymbol.AllowedAttributeLocations => AllowedAttributeLocations;

        AttributeLocation IAttributeTargetSymbol.DefaultAttributeLocation => DefaultAttributeLocation;
    }
}
