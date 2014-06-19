//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Roslyn.Compilers.CSharp
{
    internal sealed partial class AnonymousTypeManager
    {
        /// <summary>
        /// Represents a constructed instance of an anonymous type 'template'.
        /// </summary>
        private sealed class ConstructedAnonymousTypeSymbol : ConstructedNamedTypeSymbol
        {
            internal ConstructedAnonymousTypeSymbol(NamedTypeSymbol constructedFrom, ReadOnlyArray<TypeSymbol> typeArguments, AnonymousTypeDescriptor typeDescr)
                : base(constructedFrom, typeArguments)
            {
                this.TypeDescr = typeDescr;
            }

            internal readonly AnonymousTypeDescriptor TypeDescr;

            public override bool IsImplicitlyDeclared
            {
                get { return false; }
            }

            public override bool IsAnonymousType
            {
                get { return true; }
            }

            public override ReadOnlyArray<Location> Locations
            {
                get { return ReadOnlyArray<Location>.CreateFrom(this.TypeDescr.Location); }
            }

            public override ReadOnlyArray<SyntaxNode> DeclaringSyntaxNodes
            {
                get
                {
                    return GetDeclaringSyntaxNodeHelper<AnonymousObjectCreationExpressionSyntax>(this.Locations);
                }
            }

            private AnonymousTypeTemplateSymbol Template
            {
                get { return (AnonymousTypeTemplateSymbol)this.ConstructedFrom; }
            }

            public override string Name
            {
                get { return string.Empty; }
            }

            public override string MetadataName
            {
                get { return string.Empty; }
            }
            
            /// <summary> Adjust the smallest location in the 
            /// anonymous type template with this type's location </summary>
            internal void AdjustSmallestLocationInTemplate()
            {
                Debug.Assert(this.TypeDescr.Location.IsInSource);
                Template.AdjustLocation(this.TypeDescr.Location);
            }
        }
    }
}
