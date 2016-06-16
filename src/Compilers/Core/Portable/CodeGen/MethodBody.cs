// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

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
        private readonly SequencePointList _sequencePoints;
        private readonly DebugDocumentProvider _debugDocumentProvider;
        private readonly ImmutableArray<Cci.LocalScope> _localScopes;
        private readonly Cci.IImportScope _importScopeOpt;
        private readonly string _stateMachineTypeNameOpt;
        private readonly ImmutableArray<Cci.StateMachineHoistedLocalScope> _stateMachineHoistedLocalScopes;
        private readonly bool _hasDynamicLocalVariables;
        private readonly Cci.AsyncMethodBodyDebugInfo _asyncMethodDebugInfo;

        // Debug information emitted to Debug PDBs supporting EnC:
        private readonly DebugId _methodId;
        private readonly ImmutableArray<EncHoistedLocalInfo> _stateMachineHoistedLocalSlots;
        private readonly ImmutableArray<LambdaDebugInfo> _lambdaDebugInfo;
        private readonly ImmutableArray<ClosureDebugInfo> _closureDebugInfo;

        // Data used when emitting EnC delta:
        private readonly ImmutableArray<Cci.ITypeReference> _stateMachineAwaiterSlots;

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
            ImmutableArray<Cci.StateMachineHoistedLocalScope> stateMachineHoistedLocalScopes,
            ImmutableArray<EncHoistedLocalInfo> stateMachineHoistedLocalSlots,
            ImmutableArray<Cci.ITypeReference> stateMachineAwaiterSlots,
            Cci.AsyncMethodBodyDebugInfo asyncMethodDebugInfo)
        {
            Debug.Assert(!locals.IsDefault);
            Debug.Assert(!exceptionHandlers.IsDefault);
            Debug.Assert(!localScopes.IsDefault);

            _ilBits = ilBits;
            _asyncMethodDebugInfo = asyncMethodDebugInfo;
            _maxStack = maxStack;
            _parent = parent;
            _methodId = methodId;
            _locals = locals;
            _sequencePoints = sequencePoints;
            _debugDocumentProvider = debugDocumentProvider;
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
        }

        void Cci.IMethodBody.Dispatch(Cci.MetadataVisitor visitor)
        {
            throw ExceptionUtilities.Unreachable;
        }

        ImmutableArray<Cci.ExceptionHandlerRegion> Cci.IMethodBody.ExceptionRegions => _exceptionHandlers;

        bool Cci.IMethodBody.LocalsAreZeroed => true;

        ImmutableArray<Cci.ILocalDefinition> Cci.IMethodBody.LocalVariables => _locals;

        Cci.IMethodDefinition Cci.IMethodBody.MethodDefinition => _parent;

        Cci.AsyncMethodBodyDebugInfo Cci.IMethodBody.AsyncDebugInfo => _asyncMethodDebugInfo;

        ushort Cci.IMethodBody.MaxStack => _maxStack;

        public ImmutableArray<byte> IL => _ilBits;

        public void GetSequencePoints(ArrayBuilder<Cci.SequencePoint> builder)
        {
            if (HasAnySequencePoints)
            {
                _sequencePoints.GetSequencePoints(_debugDocumentProvider, builder);
            }
        }

        public bool HasAnySequencePoints
            => _sequencePoints != null && !_sequencePoints.IsEmpty;

        ImmutableArray<Cci.LocalScope> Cci.IMethodBody.LocalScopes => _localScopes;

        /// <summary>
        /// This is a list of the using directives that were in scope for this method body.
        /// </summary>
        Cci.IImportScope Cci.IMethodBody.ImportScope => _importScopeOpt;

        string Cci.IMethodBody.StateMachineTypeName => _stateMachineTypeNameOpt;

        ImmutableArray<Cci.StateMachineHoistedLocalScope> Cci.IMethodBody.StateMachineHoistedLocalScopes
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
