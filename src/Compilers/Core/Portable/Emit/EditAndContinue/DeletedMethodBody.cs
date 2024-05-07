// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Debugging;

namespace Microsoft.CodeAnalysis.Emit.EditAndContinue
{
    internal sealed class DeletedMethodBody(IDeletedMethodDefinition methodDef, ImmutableArray<byte> il) : Cci.IMethodBody
    {
        public ImmutableArray<byte> IL { get; } = il;

#nullable disable

        public ImmutableArray<Cci.ExceptionHandlerRegion> ExceptionRegions => ImmutableArray<Cci.ExceptionHandlerRegion>.Empty;

        public bool AreLocalsZeroed => false;

        public bool HasStackalloc => false;

        public ImmutableArray<Cci.ILocalDefinition> LocalVariables => ImmutableArray<Cci.ILocalDefinition>.Empty;

        public Cci.IMethodDefinition MethodDefinition => methodDef;

        public StateMachineMoveNextBodyDebugInfo MoveNextBodyInfo => null;

        public ushort MaxStack => 8;

        public ImmutableArray<Cci.SequencePoint> SequencePoints => ImmutableArray<Cci.SequencePoint>.Empty;

        public bool HasDynamicLocalVariables => false;

        public ImmutableArray<Cci.LocalScope> LocalScopes => ImmutableArray<Cci.LocalScope>.Empty;

        public Cci.IImportScope ImportScope => null;

        public DebugId MethodId => default;

        public ImmutableArray<StateMachineHoistedLocalScope> StateMachineHoistedLocalScopes => ImmutableArray<StateMachineHoistedLocalScope>.Empty;

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
            var missingMethodExceptionStringStringConstructor = context.Module.CommonCompilation.CommonGetWellKnownTypeMember(WellKnownMember.System_MissingMethodException__ctorString);
            Debug.Assert(missingMethodExceptionStringStringConstructor is not null);

            var builder = new ILBuilder((ITokenDeferral)context.Module, null, OptimizationLevel.Debug, false);

            builder.EmitStringConstant(rudeEdit.HasValue
                ? string.Format(CodeAnalysisResources.EncLambdaRudeEdit, rudeEdit.Value.Message)
                : isLambdaOrLocalFunction
                    ? CodeAnalysisResources.EncDeletedLambdaInvoked
                    : CodeAnalysisResources.EncDeletedMethodInvoked);

            builder.EmitOpCode(ILOpCode.Newobj, 4);
            builder.EmitToken(missingMethodExceptionStringStringConstructor.GetCciAdapter(), context.SyntaxNode!, context.Diagnostics);
            builder.EmitThrow(isRethrow: false);
            builder.Realize();

            return builder.RealizedIL;
        }
    }
}
