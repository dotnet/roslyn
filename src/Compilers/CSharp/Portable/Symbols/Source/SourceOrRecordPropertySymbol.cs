using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
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
