// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed partial class AnonymousTypeManager
    {
        /// <summary>
        /// Represents an anonymous type 'ToString' method.
        /// </summary>
        private sealed partial class AnonymousTypeToStringMethodSymbol : SynthesizedMethodBase
        {
            internal AnonymousTypeToStringMethodSymbol(NamedTypeSymbol container)
                : base(container, WellKnownMemberNames.ObjectToString)
            {
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
                get { return TypeWithAnnotations.Create(this.Manager.System_String, NullableAnnotation.NotAnnotated); }
            }

            public override ImmutableArray<ParameterSymbol> Parameters
            {
                get { return ImmutableArray<ParameterSymbol>.Empty; }
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
