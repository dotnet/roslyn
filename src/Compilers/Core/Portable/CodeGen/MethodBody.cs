// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
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
        private readonly byte[] ilBits;
        private readonly Cci.AsyncMethodBodyDebugInfo asyncMethodDebugInfo;
        private readonly ushort maxStack;
        private readonly Cci.IMethodDefinition parent;
        private readonly ImmutableArray<Cci.ILocalDefinition> locals;    // built by someone else
        private readonly SequencePointList sequencePoints;
        private readonly DebugDocumentProvider debugDocumentProvider;
        private readonly ImmutableArray<Cci.ExceptionHandlerRegion> exceptionHandlers;
        private readonly ImmutableArray<Cci.LocalScope> localScopes;
        private readonly ImmutableArray<Cci.NamespaceScope> namespaceScopes;
        private readonly string stateMachineTypeNameOpt;
        private readonly ImmutableArray<Cci.StateMachineHoistedLocalScope> stateMachineHoistedLocalScopes;
        private readonly ImmutableArray<EncHoistedLocalInfo> stateMachineHoistedLocalSlots;
        private readonly ImmutableArray<Cci.ITypeReference> stateMachineAwaiterSlots;
        private readonly Cci.NamespaceScopeEncoding namespaceScopeEncoding;
        private readonly bool hasDynamicLocalVariables;

        public MethodBody(
            byte[] ilBits,
            ushort maxStack,
            Cci.IMethodDefinition parent,
            ImmutableArray<Cci.ILocalDefinition> locals,
            SequencePointList sequencePoints,
            DebugDocumentProvider debugDocumentProvider,
            ImmutableArray<Cci.ExceptionHandlerRegion> exceptionHandlers,
            ImmutableArray<Cci.LocalScope> localScopes,
            bool hasDynamicLocalVariables,
            ImmutableArray<Cci.NamespaceScope> namespaceScopes,
            Cci.NamespaceScopeEncoding namespaceScopeEncoding,
            string stateMachineTypeNameOpt,
            ImmutableArray<Cci.StateMachineHoistedLocalScope> stateMachineHoistedLocalScopes,
            ImmutableArray<EncHoistedLocalInfo> stateMachineHoistedLocalSlots,
            ImmutableArray<Cci.ITypeReference> stateMachineAwaiterSlots,
            Cci.AsyncMethodBodyDebugInfo asyncMethodDebugInfo)
        {
            Debug.Assert(!locals.IsDefault);
            Debug.Assert(!exceptionHandlers.IsDefault);
            Debug.Assert(!localScopes.IsDefault);

            this.ilBits = ilBits;
            this.asyncMethodDebugInfo = asyncMethodDebugInfo;
            this.maxStack = maxStack;
            this.parent = parent;
            this.locals = locals;
            this.sequencePoints = sequencePoints;
            this.debugDocumentProvider = debugDocumentProvider;
            this.exceptionHandlers = exceptionHandlers;
            this.localScopes = localScopes;
            this.namespaceScopeEncoding = namespaceScopeEncoding;
            this.hasDynamicLocalVariables = hasDynamicLocalVariables;
            this.namespaceScopes = namespaceScopes.IsDefault ? ImmutableArray<Cci.NamespaceScope>.Empty : namespaceScopes;
            this.stateMachineTypeNameOpt = stateMachineTypeNameOpt;
            this.stateMachineHoistedLocalScopes = stateMachineHoistedLocalScopes;
            this.stateMachineHoistedLocalSlots = stateMachineHoistedLocalSlots;
            this.stateMachineAwaiterSlots = stateMachineAwaiterSlots;
        }

        void Cci.IMethodBody.Dispatch(Cci.MetadataVisitor visitor)
        {
            throw ExceptionUtilities.Unreachable;
        }

        ImmutableArray<Cci.ExceptionHandlerRegion> Cci.IMethodBody.ExceptionRegions
        {
            get { return this.exceptionHandlers; }
        }

        bool Cci.IMethodBody.LocalsAreZeroed
        {
            get { return true; }
        }

        ImmutableArray<Cci.ILocalDefinition> Cci.IMethodBody.LocalVariables
        {
            get { return this.locals; }
        }

        Cci.IMethodDefinition Cci.IMethodBody.MethodDefinition
        {
            get { return parent; }
        }

        Cci.AsyncMethodBodyDebugInfo Cci.IMethodBody.AsyncDebugInfo
        {
            get { return this.asyncMethodDebugInfo; }
        }

        ushort Cci.IMethodBody.MaxStack
        {
            get { return maxStack; }
        }

        public byte[] IL
        {
            get { return ilBits; }
        }

        public ImmutableArray<Cci.SequencePoint> GetSequencePoints()
        {
            return HasAnySequencePoints ?
                this.sequencePoints.GetSequencePoints(debugDocumentProvider) :
                ImmutableArray<Cci.SequencePoint>.Empty;
        }

        public bool HasAnySequencePoints
        {
            get
            {
                return this.sequencePoints != null && !this.sequencePoints.IsEmpty;
            }
        }

        public ImmutableArray<Cci.SequencePoint> GetLocations()
        {
            return GetSequencePoints();
        }

        ImmutableArray<Cci.LocalScope> Cci.IMethodBody.LocalScopes
        {
            get
            {
                return this.localScopes;
            }
        }

        /// <summary>
        /// This is a list of the using directives that were in scope for this method body.
        /// </summary>
        ImmutableArray<Cci.NamespaceScope> Cci.IMethodBody.NamespaceScopes
        {
            get
            {
                return this.namespaceScopes;
            }
        }

        string Cci.IMethodBody.StateMachineTypeName
        {
            get
            {
                return stateMachineTypeNameOpt;
            }
        }

        ImmutableArray<Cci.StateMachineHoistedLocalScope> Cci.IMethodBody.StateMachineHoistedLocalScopes
        {
            get
            {
                return this.stateMachineHoistedLocalScopes;
            }
        }

        ImmutableArray<EncHoistedLocalInfo> Cci.IMethodBody.StateMachineHoistedLocalSlots
        {
            get
            {
                return this.stateMachineHoistedLocalSlots;
            }
        }

        ImmutableArray<Cci.ITypeReference> Cci.IMethodBody.StateMachineAwaiterSlots
        {
            get
            {
                return stateMachineAwaiterSlots;
            }
        }

        public Cci.NamespaceScopeEncoding NamespaceScopeEncoding
        {
            get
            {
                return this.namespaceScopeEncoding;
            }
        }

        public bool HasDynamicLocalVariables
        {
            get
            {
                return this.hasDynamicLocalVariables;
            }
        }
    }
}
