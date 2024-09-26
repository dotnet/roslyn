// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed partial class AnonymousTypeManager
    {
        /// <summary>
        /// Represents an anonymous type 'Equals' method.
        /// </summary>
        private sealed partial class AnonymousTypeEqualsMethodSymbol : SynthesizedMethodBase
        {
            private readonly ImmutableArray<ParameterSymbol> _parameters;

            internal AnonymousTypeEqualsMethodSymbol(NamedTypeSymbol container)
                : base(container, WellKnownMemberNames.ObjectEquals)
            {
                _parameters = ImmutableArray.Create<ParameterSymbol>(
                    SynthesizedParameterSymbol.Create(this, TypeWithAnnotations.Create(this.Manager.System_Object), 0, RefKind.None, "value"));
            }

            public override MethodKind MethodKind
            {
                get { return MethodKind.Ordinary; }
            }

            public override bool ReturnsVoid
            {
                get { return false; }
            }

            public override RefKind RefKind
            {
                get { return RefKind.None; }
            }

            public override TypeWithAnnotations ReturnTypeWithAnnotations
            {
                get { return TypeWithAnnotations.Create(this.Manager.System_Boolean); }
            }

            public override ImmutableArray<ParameterSymbol> Parameters
            {
                get { return _parameters; }
            }

            public override bool IsOverride
            {
                get { return true; }
            }

            internal sealed override bool IsMetadataVirtual(IsMetadataVirtualOption option = IsMetadataVirtualOption.None)
            {
                return true;
            }

            internal override bool IsMetadataFinal
            {
                get
                {
                    return false;
                }
            }
        }
    }
}
