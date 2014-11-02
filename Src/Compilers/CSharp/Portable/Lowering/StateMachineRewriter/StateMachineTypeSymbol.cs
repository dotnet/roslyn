// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract class StateMachineTypeSymbol : SynthesizedContainer, ISynthesizedMethodBodyImplementationSymbol
    {
        public readonly MethodSymbol KickoffMethod;

        public StateMachineTypeSymbol(VariableSlotAllocator slotAllocatorOpt, MethodSymbol kickoffMethod)
            : base(MakeName(slotAllocatorOpt, kickoffMethod), kickoffMethod)
        {
            Debug.Assert(kickoffMethod != null);
            this.KickoffMethod = kickoffMethod;
        }

        private static string MakeName(VariableSlotAllocator slotAllocatorOpt, MethodSymbol kickoffMethod)
        {
            return slotAllocatorOpt?.PreviousStateMachineTypeName ?? 
                   GeneratedNames.MakeStateMachineTypeName(kickoffMethod.Name, SequenceNumber(kickoffMethod));
        }

        private static int SequenceNumber(MethodSymbol kickoffMethod)
        {
            // return a unique sequence number for the async implementation class that is independent of the compilation state.
            int count = 0;
            foreach (var m in kickoffMethod.ContainingType.GetMembers(kickoffMethod.Name))
            {
                count++;
                if ((object)kickoffMethod == m)
                {
                    return count;
                }
            }

            // It is possible we did not find any such members, e.g. for methods that result from the translation of
            // async lambdas.  In that case the method has already been uniquely named, so there is no need to
            // produce a unique sequence number for the corresponding class, which already includes the (unique) method name.
            return count;
        }

        public override Symbol ContainingSymbol
        {
            get { return KickoffMethod.ContainingType; }
        }

        bool ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency
        {
            get
            {
                // MoveNext method contains user code from the async/iterator method:
                return true;
            }
        }

        IMethodSymbol ISynthesizedMethodBodyImplementationSymbol.Method
        {
            get { return KickoffMethod; }
        }
    }
}
