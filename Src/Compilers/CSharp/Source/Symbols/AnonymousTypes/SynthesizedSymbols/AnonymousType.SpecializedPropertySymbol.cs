//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;

namespace Roslyn.Compilers.CSharp
{
    internal sealed partial class AnonymousTypeManager
    {
        /// <summary>
        /// Represents a specialized anonymous type property symbol. Carries proper property location.
        /// </summary>
        private sealed class AnonymousTypeSpecializedPropertySymbol : SubstitutedPropertySymbol
        {
            private readonly ReadOnlyArray<Location> locations;

            internal AnonymousTypeSpecializedPropertySymbol(SubstitutedNamedTypeSymbol containingType, PropertySymbol originalDefinition, AnonymousTypeField field)
                : base(containingType, originalDefinition)
            {
                this.locations = ReadOnlyArray<Location>.CreateFrom(field.Location);
            }

            public override ReadOnlyArray<Location> Locations
            {
                get { return this.locations; }
            }
        }
    }
}
