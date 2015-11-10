// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                    ParameterSymbol[] paramsArr = new ParameterSymbol[fieldsCount];
                    for (int index = 0; index < fieldsCount; index++)
                    {
                        PropertySymbol property = properties[index];
                        paramsArr[index] = new SynthesizedParameterSymbol(this, property.Type, index, RefKind.None, property.Name);
                    }
                    _parameters = paramsArr.AsImmutableOrNull();
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

            public override TypeSymbolWithAnnotations ReturnType
            {
                get { return TypeSymbolWithAnnotations.Create(this.Manager.System_Void); }
            }

            public override ImmutableArray<ParameterSymbol> Parameters
            {
                get { return _parameters; }
            }

            public override bool IsOverride
            {
                get { return false; }
            }

            internal sealed override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false)
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
