// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// Holds on to the method body data.
    /// </summary>
    internal sealed class MethodBody : Cci.IMethodBody
    {
        private readonly Cci.IMethodDefinition _parent;

        private readonly ImmutableArray<byte> _ilBits;
        private readonly ushort _maxStack;
        private readonly ImmutableArray<Cci.ILocalDefinition> _locals;
        private readonly ImmutableArray<Cci.ExceptionHandlerRegion> _exceptionHandlers;

        // Debug information emitted to Release & Debug PDBs supporting the debugger, EEs and other tools:
        private readonly ImmutableArray<Cci.SequencePoint> _sequencePoints;
        private readonly ImmutableArray<Cci.LocalScope> _localScopes;
        private readonly Cci.IImportScope _importScopeOpt;
        private readonly string _stateMachineTypeNameOpt;
        private readonly ImmutableArray<StateMachineHoistedLocalScope> _stateMachineHoistedLocalScopes;
        private readonly bool _hasDynamicLocalVariables;
        private readonly StateMachineMoveNextBodyDebugInfo _stateMachineMoveNextDebugInfoOpt;

        // Debug information emitted to Debug PDBs supporting EnC:
        private readonly DebugId _methodId;
        private readonly ImmutableArray<EncHoistedLocalInfo> _stateMachineHoistedLocalSlots;
        private readonly ImmutableArray<LambdaDebugInfo> _lambdaDebugInfo;
        private readonly ImmutableArray<ClosureDebugInfo> _closureDebugInfo;

        // Data used when emitting EnC delta:
        private readonly ImmutableArray<Cci.ITypeReference> _stateMachineAwaiterSlots;

        // Data used when emitting Dynamic Analysis resource:
        private readonly DynamicAnalysisMethodBodyData _dynamicAnalysisDataOpt;

        public MethodBody(
            ImmutableArray<byte> ilBits,
            ushort maxStack,
            Cci.IMethodDefinition parent,
            DebugId methodId,
            ImmutableArray<Cci.ILocalDefinition> locals,
            SequencePointList sequencePoints,
            DebugDocumentProvider debugDocumentProvider,
            ImmutableArray<Cci.ExceptionHandlerRegion> exceptionHandlers,
            ImmutableArray<Cci.LocalScope> localScopes,
            bool hasDynamicLocalVariables,
            Cci.IImportScope importScopeOpt,
            ImmutableArray<LambdaDebugInfo> lambdaDebugInfo,
            ImmutableArray<ClosureDebugInfo> closureDebugInfo,
            string stateMachineTypeNameOpt,
            ImmutableArray<StateMachineHoistedLocalScope> stateMachineHoistedLocalScopes,
            ImmutableArray<EncHoistedLocalInfo> stateMachineHoistedLocalSlots,
            ImmutableArray<Cci.ITypeReference> stateMachineAwaiterSlots,
            StateMachineMoveNextBodyDebugInfo stateMachineMoveNextDebugInfoOpt,
            DynamicAnalysisMethodBodyData dynamicAnalysisDataOpt)
        {
            Debug.Assert(!locals.IsDefault);
            Debug.Assert(!exceptionHandlers.IsDefault);
            Debug.Assert(!localScopes.IsDefault);

            _ilBits = ilBits;
            _maxStack = maxStack;
            _parent = parent;
            _methodId = methodId;
            _locals = locals;
            _exceptionHandlers = exceptionHandlers;
            _localScopes = localScopes;
            _hasDynamicLocalVariables = hasDynamicLocalVariables;
            _importScopeOpt = importScopeOpt;
            _lambdaDebugInfo = lambdaDebugInfo;
            _closureDebugInfo = closureDebugInfo;
            _stateMachineTypeNameOpt = stateMachineTypeNameOpt;
            _stateMachineHoistedLocalScopes = stateMachineHoistedLocalScopes;
            _stateMachineHoistedLocalSlots = stateMachineHoistedLocalSlots;
            _stateMachineAwaiterSlots = stateMachineAwaiterSlots;
            _stateMachineMoveNextDebugInfoOpt = stateMachineMoveNextDebugInfoOpt;
            _dynamicAnalysisDataOpt = dynamicAnalysisDataOpt;
            _sequencePoints = GetSequencePoints(sequencePoints, debugDocumentProvider);
        }

        private static ImmutableArray<Cci.SequencePoint> GetSequencePoints(SequencePointList? sequencePoints, DebugDocumentProvider debugDocumentProvider)
        {
            if (sequencePoints == null || sequencePoints.IsEmpty)
            {
                return ImmutableArray<Cci.SequencePoint>.Empty;
            }

            var sequencePointsBuilder = ArrayBuilder<Cci.SequencePoint>.GetInstance();
            sequencePoints.GetSequencePoints(debugDocumentProvider, sequencePointsBuilder);
            return sequencePointsBuilder.ToImmutableAndFree();
        }

        DynamicAnalysisMethodBodyData Cci.IMethodBody.DynamicAnalysisData => _dynamicAnalysisDataOpt;

        ImmutableArray<Cci.ExceptionHandlerRegion> Cci.IMethodBody.ExceptionRegions => _exceptionHandlers;

        bool Cci.IMethodBody.LocalsAreZeroed => true;

        ImmutableArray<Cci.ILocalDefinition> Cci.IMethodBody.LocalVariables => _locals;

        Cci.IMethodDefinition Cci.IMethodBody.MethodDefinition => _parent;

        StateMachineMoveNextBodyDebugInfo Cci.IMethodBody.MoveNextBodyInfo => _stateMachineMoveNextDebugInfoOpt;

        ushort Cci.IMethodBody.MaxStack => _maxStack;

        public ImmutableArray<byte> IL => _ilBits;

        public ImmutableArray<Cci.SequencePoint> SequencePoints => _sequencePoints;

        ImmutableArray<Cci.LocalScope> Cci.IMethodBody.LocalScopes => _localScopes;

        /// <summary>
        /// This is a list of the using directives that were in scope for this method body.
        /// </summary>
        Cci.IImportScope Cci.IMethodBody.ImportScope => _importScopeOpt;

        string Cci.IMethodBody.StateMachineTypeName => _stateMachineTypeNameOpt;

        ImmutableArray<StateMachineHoistedLocalScope> Cci.IMethodBody.StateMachineHoistedLocalScopes
            => _stateMachineHoistedLocalScopes;

        ImmutableArray<EncHoistedLocalInfo> Cci.IMethodBody.StateMachineHoistedLocalSlots
            => _stateMachineHoistedLocalSlots;

        ImmutableArray<Cci.ITypeReference> Cci.IMethodBody.StateMachineAwaiterSlots
            => _stateMachineAwaiterSlots;

        bool Cci.IMethodBody.HasDynamicLocalVariables => _hasDynamicLocalVariables;

        public DebugId MethodId => _methodId;

        public ImmutableArray<LambdaDebugInfo> LambdaDebugInfo => _lambdaDebugInfo;

        public ImmutableArray<ClosureDebugInfo> ClosureDebugInfo => _closureDebugInfo;
    }
}
