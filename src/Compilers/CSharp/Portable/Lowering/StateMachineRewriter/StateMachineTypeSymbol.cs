// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract class StateMachineTypeSymbol : SynthesizedContainer, ISynthesizedMethodBodyImplementationSymbol
    {
        private ImmutableArray<CSharpAttributeData> _attributes;
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

        IMethodSymbolInternal ISynthesizedMethodBodyImplementationSymbol.Method
        {
            get { return KickoffMethod; }
        }

        public sealed override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            if (_attributes.IsDefault)
            {
                Debug.Assert(base.GetAttributes().Length == 0);

                ArrayBuilder<CSharpAttributeData> builder = null;

                // Inherit some attributes from the container of the kickoff method
                var kickoffType = KickoffMethod.ContainingType;
                foreach (var attribute in kickoffType.GetAttributes())
                {
                    if (attribute.IsTargetAttribute(AttributeDescription.DebuggerNonUserCodeAttribute) ||
                        attribute.IsTargetAttribute(AttributeDescription.DebuggerStepThroughAttribute))
                    {
                        if (builder == null)
                        {
                            builder = ArrayBuilder<CSharpAttributeData>.GetInstance(2); // only 2 different attributes are inherited at the moment
                        }

                        builder.Add(attribute);
                    }
                }

                ImmutableInterlocked.InterlockedCompareExchange(ref _attributes,
                                                                builder == null ? ImmutableArray<CSharpAttributeData>.Empty : builder.ToImmutableAndFree(),
                                                                default(ImmutableArray<CSharpAttributeData>));
            }

            return _attributes;
        }

        public sealed override bool AreLocalsZeroed => KickoffMethod.AreLocalsZeroed;

        internal override bool HasCodeAnalysisEmbeddedAttribute => false;
    }
}
