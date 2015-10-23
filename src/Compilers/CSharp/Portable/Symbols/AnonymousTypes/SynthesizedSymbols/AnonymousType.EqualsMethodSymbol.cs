﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                                      new SynthesizedParameterSymbol(this, this.Manager.System_Object, 0, RefKind.None, "value")
                                  );
            }

            public override MethodKind MethodKind
            {
                get { return MethodKind.Ordinary; }
            }

            public override bool ReturnsVoid
            {
                get { return false; }
            }

            public override TypeSymbol ReturnType
            {
                get { return this.Manager.System_Boolean; }
            }

            public override ImmutableArray<ParameterSymbol> Parameters
            {
                get { return _parameters; }
            }

            public override bool IsOverride
            {
                get { return true; }
            }

            internal sealed override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false)
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
