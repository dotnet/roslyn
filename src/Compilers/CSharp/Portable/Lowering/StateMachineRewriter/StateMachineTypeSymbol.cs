// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract class StateMachineTypeSymbol : SynthesizedContainer, ISynthesizedMethodBodyImplementationSymbol
    {
        public readonly MethodSymbol KickoffMethod;

        public StateMachineTypeSymbol(VariableSlotAllocator slotAllocatorOpt, TypeCompilationState compilationState, MethodSymbol kickoffMethod, int kickoffMethodOrdinal)
            : base(MakeName(slotAllocatorOpt, compilationState, kickoffMethod, kickoffMethodOrdinal), kickoffMethod)
        {
            Debug.Assert(kickoffMethod != null);
            this.KickoffMethod = kickoffMethod;
        }

        private static string MakeName(VariableSlotAllocator slotAllocatorOpt, TypeCompilationState compilationState, MethodSymbol kickoffMethod, int kickoffMethodOrdinal)
        {
            return slotAllocatorOpt?.PreviousStateMachineTypeName ??
                   GeneratedNames.MakeStateMachineTypeName(kickoffMethod.Name, kickoffMethodOrdinal, compilationState.ModuleBuilderOpt.CurrentGenerationOrdinal);
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
