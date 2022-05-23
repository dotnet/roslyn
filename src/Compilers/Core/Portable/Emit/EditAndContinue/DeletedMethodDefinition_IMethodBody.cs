// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Debugging;

namespace Microsoft.CodeAnalysis.Emit.EditAndContinue
{
    internal sealed partial class DeletedMethodDefinition : IMethodBody
    {
        public ImmutableArray<ExceptionHandlerRegion> ExceptionRegions => ImmutableArray<ExceptionHandlerRegion>.Empty;

        public bool AreLocalsZeroed => false;

        public bool HasStackalloc => false;

        public ImmutableArray<ILocalDefinition> LocalVariables => ImmutableArray<ILocalDefinition>.Empty;

        public IMethodDefinition MethodDefinition => this;

        public StateMachineMoveNextBodyDebugInfo MoveNextBodyInfo => null;

        public ushort MaxStack => 8;

        public ImmutableArray<byte> IL
        {
            get
            {
                var builder = new ILBuilder(null, null, OptimizationLevel.Debug, false);

                builder.EmitOpCode(System.Reflection.Metadata.ILOpCode.Ldnull);
                builder.EmitThrow(isRethrow: false);
                builder.Realize();

                return builder.RealizedIL;
            }
        }

        public ImmutableArray<SequencePoint> SequencePoints => ImmutableArray<SequencePoint>.Empty;

        public bool HasDynamicLocalVariables => false;

        public ImmutableArray<LocalScope> LocalScopes => ImmutableArray<LocalScope>.Empty;

        public Cci.IImportScope ImportScope => null;

        public DebugId MethodId => default;

        public ImmutableArray<StateMachineHoistedLocalScope> StateMachineHoistedLocalScopes => ImmutableArray<StateMachineHoistedLocalScope>.Empty;

        public string StateMachineTypeName => null;

        public ImmutableArray<EncHoistedLocalInfo> StateMachineHoistedLocalSlots => default;

        public ImmutableArray<ITypeReference> StateMachineAwaiterSlots => default;

        public ImmutableArray<ClosureDebugInfo> ClosureDebugInfo => ImmutableArray<ClosureDebugInfo>.Empty;

        public ImmutableArray<LambdaDebugInfo> LambdaDebugInfo => ImmutableArray<LambdaDebugInfo>.Empty;

        public DynamicAnalysisMethodBodyData DynamicAnalysisData => null;
    }
}
