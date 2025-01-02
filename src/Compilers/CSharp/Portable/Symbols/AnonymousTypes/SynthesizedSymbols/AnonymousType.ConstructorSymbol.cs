// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed partial class AnonymousTypeManager
    {
        /// <summary>
        /// Represents an anonymous type constructor.
        /// </summary>
        private sealed partial class AnonymousTypeConstructorSymbol : SynthesizedMethodBase
        {
            private readonly ImmutableArray<ParameterSymbol> _parameters;

            internal AnonymousTypeConstructorSymbol(NamedTypeSymbol container, ImmutableArray<AnonymousTypePropertySymbol> properties)
                : base(container, WellKnownMemberNames.InstanceConstructorName)
            {
                // Create constructor parameters
                int fieldsCount = properties.Length;
                if (fieldsCount > 0)
                {
                    var paramsArr = ArrayBuilder<ParameterSymbol>.GetInstance(fieldsCount);
                    for (int index = 0; index < fieldsCount; index++)
                    {
                        PropertySymbol property = properties[index];
                        paramsArr.Add(SynthesizedParameterSymbol.Create(this, property.TypeWithAnnotations, index, RefKind.None, property.Name));
                    }
                    _parameters = paramsArr.ToImmutableAndFree();
                }
                else
                {
                    _parameters = ImmutableArray<ParameterSymbol>.Empty;
                }
            }

            public override MethodKind MethodKind
            {
                get { return MethodKind.Constructor; }
            }

            public override bool ReturnsVoid
            {
                get { return true; }
            }

            public override RefKind RefKind
            {
                get { return RefKind.None; }
            }

            public override TypeWithAnnotations ReturnTypeWithAnnotations
            {
                get { return TypeWithAnnotations.Create(this.Manager.System_Void); }
            }

            public override ImmutableArray<ParameterSymbol> Parameters
            {
                get { return _parameters; }
            }

            public override bool IsOverride
            {
                get { return false; }
            }

            internal sealed override bool IsMetadataVirtual(IsMetadataVirtualOption option = IsMetadataVirtualOption.None)
            {
                return false;
            }

            internal override bool IsMetadataFinal
            {
                get
                {
                    return false;
                }
            }

            public override ImmutableArray<Location> Locations
            {
                get
                {
                    // The accessor for an anonymous type constructor has the same location as the type.
                    return this.ContainingSymbol.Locations;
                }
            }
        }
    }
}
