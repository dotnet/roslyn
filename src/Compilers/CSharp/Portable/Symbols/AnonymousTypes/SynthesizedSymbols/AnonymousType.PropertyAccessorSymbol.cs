// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Emit;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed partial class AnonymousTypeManager
    {
        /// <summary>
        /// Represents a getter for anonymous type property.
        /// </summary>
        private sealed partial class AnonymousTypePropertyGetAccessorSymbol : SynthesizedMethodBase
        {
            private readonly AnonymousTypePropertySymbol _property;

            internal AnonymousTypePropertyGetAccessorSymbol(AnonymousTypePropertySymbol property)
                // winmdobj output only effects setters, so we can always set this to false
                : base(property.ContainingType, SourcePropertyAccessorSymbol.GetAccessorName(property.Name, getNotSet: true, isWinMdOutput: false))
            {
                _property = property;
            }

            public override MethodKind MethodKind
            {
                get { return MethodKind.PropertyGet; }
            }

            public override bool ReturnsVoid
            {
                get { return false; }
            }

            public override TypeSymbolWithAnnotations ReturnType
            {
                get { return _property.Type; }
            }

            public override ImmutableArray<ParameterSymbol> Parameters
            {
                get { return ImmutableArray<ParameterSymbol>.Empty; }
            }

            public override Symbol AssociatedSymbol
            {
                get { return _property; }
            }

            public override ImmutableArray<Location> Locations
            {
                get
                {
                    // The accessor for a anonymous type property has the same location as the property.
                    return _property.Locations;
                }
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

            internal override void AddSynthesizedAttributes(ModuleCompilationState compilationState, ref ArrayBuilder<SynthesizedAttributeData> attributes)
            {
                // Do not call base.AddSynthesizedAttributes.
                // Dev11 does not emit DebuggerHiddenAttribute in property accessors
            }
        }
    }
}
