// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Debugging;

namespace Microsoft.CodeAnalysis.Emit.EditAndContinue
{
    internal sealed class DeletedMethodBody(IDeletedMethodDefinition methodDef, ImmutableArray<byte> il) : Cci.IMethodBody
    {
        private readonly IDeletedMethodDefinition _methodDef = methodDef;

        public ImmutableArray<byte> IL { get; } = il;

#nullable disable

        public ImmutableArray<Cci.ExceptionHandlerRegion> ExceptionRegions => ImmutableArray<Cci.ExceptionHandlerRegion>.Empty;

        public bool AreLocalsZeroed => false;

        public bool HasStackalloc => false;

        public ImmutableArray<Cci.ILocalDefinition> LocalVariables => ImmutableArray<Cci.ILocalDefinition>.Empty;

        public Cci.IMethodDefinition MethodDefinition => _methodDef;

        public StateMachineMoveNextBodyDebugInfo MoveNextBodyInfo => null;

        public ushort MaxStack => 8;

        public ImmutableArray<Cci.SequencePoint> SequencePoints => ImmutableArray<Cci.SequencePoint>.Empty;

        public bool HasDynamicLocalVariables => false;

        public ImmutableArray<Cci.LocalScope> LocalScopes => ImmutableArray<Cci.LocalScope>.Empty;

        public Cci.IImportScope ImportScope => null;

        public DebugId MethodId => default;

        public ImmutableArray<StateMachineHoistedLocalScope> StateMachineHoistedLocalScopes => default;

        public string StateMachineTypeName => null;

        public ImmutableArray<EncHoistedLocalInfo> StateMachineHoistedLocalSlots => default;

        public ImmutableArray<Cci.ITypeReference> StateMachineAwaiterSlots => default;

        public ImmutableArray<EncClosureInfo> ClosureDebugInfo => ImmutableArray<EncClosureInfo>.Empty;

        public ImmutableArray<EncLambdaInfo> LambdaDebugInfo => ImmutableArray<EncLambdaInfo>.Empty;

        public ImmutableArray<LambdaRuntimeRudeEditInfo> OrderedLambdaRuntimeRudeEdits => ImmutableArray<LambdaRuntimeRudeEditInfo>.Empty;

        public ImmutableArray<SourceSpan> CodeCoverageSpans => ImmutableArray<SourceSpan>.Empty;

        public StateMachineStatesDebugInfo StateMachineStatesDebugInfo => default;

        public bool IsPrimaryConstructor => false;

#nullable enable
        public static ImmutableArray<byte> GetIL(EmitContext context, RuntimeRudeEdit? rudeEdit, bool isLambdaOrLocalFunction)
        {
            var hotReloadExceptionCtorDef = context.Module.GetOrCreateHotReloadExceptionConstructorDefinition();

            var builder = new ILBuilder(context.Module, localSlotManager: null, context.Diagnostics, OptimizationLevel.Debug, areLocalsZeroed: false);

            string message;
            int codeValue;
            if (rudeEdit.HasValue)
            {
                message = string.Format(CodeAnalysisResources.EncLambdaRudeEdit, rudeEdit.Value.Message);
                codeValue = rudeEdit.Value.ErrorCode;
            }
            else
            {
                var code = isLambdaOrLocalFunction ? HotReloadExceptionCode.DeletedLambdaInvoked : HotReloadExceptionCode.DeletedMethodInvoked;
                message = code.GetExceptionMessage();
                codeValue = code.GetExceptionCodeValue();
            }

            var syntaxNode = context.SyntaxNode;

            builder.EmitStringConstant(message, syntaxNode);
            builder.EmitIntConstant(codeValue);

            // consumes message and code, pushes the created exception object:
            builder.EmitOpCode(ILOpCode.Newobj, stackAdjustment: -1);
            builder.EmitToken(hotReloadExceptionCtorDef.GetCciAdapter(), syntaxNode);
            builder.EmitThrow(isRethrow: false);
            builder.Realize();

            return builder.RealizedIL;
        }
    }
}
