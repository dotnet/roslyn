// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.Cci;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class AsyncRewriter2
    {
        /// <summary>
        /// The class that represents a translated async method.
        /// </summary>
        sealed internal class AsyncStruct : SynthesizedContainer
        {
            private readonly MethodSymbol constructor;
            private readonly ImmutableArray<NamedTypeSymbol> interfaces;

            public AsyncStruct(MethodSymbol method)
                : base(method, GeneratedNames.MakeIteratorOrAsyncDisplayClassName(method.Name, SequenceNumber(method)), TypeKind.Struct)
            {
                // TODO: report use-site errors on these types
                this.interfaces = ImmutableArray.Create<NamedTypeSymbol>(method.DeclaringCompilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_IAsyncStateMachine));
                this.constructor = new SynthesizedInstanceConstructor(this);
            }

            private static int SequenceNumber(MethodSymbol method)
            {
                // return a unique sequence number for the async implementation class that is independent of the compilation state.
                int count = 0;
                foreach (var m in method.ContainingNamespaceOrType().GetMembers(method.Name))
                {
                    count++;
                    if (method == m) return count;
                }

                // It is possible we did not find any such members, e.g. for methods that result from the translation of
                // async lambdas.  In that case the method has already been uniquely named, so there is no need to
                // produce a unique sequence number for the corresponding class, which already includes the (unique) method name.
                return count;
            }

            internal override MethodSymbol Constructor
            {
                get { return constructor; }
            }

            internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics
            {
                get { return interfaces; }
            }
        }
    }
}