// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// The class that represents a translated async method.
    /// </summary>
    internal sealed class AsyncStateMachine : SynthesizedContainer, ISynthesizedMethodBodyImplementationSymbol
    {
        private readonly TypeKind typeKind;
        private readonly MethodSymbol constructor;
        private readonly MethodSymbol asyncMethod;
        private readonly ImmutableArray<NamedTypeSymbol> interfaces;

        public AsyncStateMachine(MethodSymbol asyncMethod, TypeKind typeKind)
            : base(GeneratedNames.MakeIteratorOrAsyncDisplayClassName(asyncMethod.Name, SequenceNumber(asyncMethod)), asyncMethod)
        {
            // TODO: report use-site errors on these types
            this.typeKind = typeKind;
            this.asyncMethod = asyncMethod;
            this.interfaces = ImmutableArray.Create(asyncMethod.DeclaringCompilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_IAsyncStateMachine));
            this.constructor = new AsyncConstructor(this);
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

        public override TypeKind TypeKind
        {
            get { return typeKind; }
        }

        internal override MethodSymbol Constructor
        {
            get { return constructor; }
        }

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics
        {
            get { return interfaces; }
        }

        public override Symbol ContainingSymbol
        {
            get { return asyncMethod.ContainingType; }
        }

        bool ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency
        {
            get
            {
                // MoveNext method contains user code from the async method:
                return true;
            }
        }

        IMethodSymbol ISynthesizedMethodBodyImplementationSymbol.Method
        {
            get { return asyncMethod; }
        }
    }
}