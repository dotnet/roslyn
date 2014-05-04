// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
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
        private readonly ImmutableArray<LocalDefinition> locals;    // built by someone else
        private readonly SequencePointList sequencePoints;
        private readonly DebugDocumentProvider debugDocumentProvider;
        private readonly ImmutableArray<Cci.ExceptionHandlerRegion> exceptionHandlers;
        private readonly ImmutableArray<Cci.LocalScope> localScopes;
        private readonly ImmutableArray<Cci.NamespaceScope> namespaceScopes;
        private readonly string iteratorClassName;
        private readonly ImmutableArray<Cci.LocalScope> iteratorScopes;
        private readonly Cci.CustomDebugInfoKind customDebugInfoKind;
        private readonly bool hasDynamicLocalVariables;

        public MethodBody(
            byte[] ilBits,
            ushort maxStack,
            Cci.IMethodDefinition parent,
            ImmutableArray<LocalDefinition> locals,
            SequencePointList sequencePoints,
            DebugDocumentProvider debugDocumentProvider,
            ImmutableArray<Cci.ExceptionHandlerRegion> exceptionHandlers,
            ImmutableArray<Cci.LocalScope> localScopes,
            Cci.CustomDebugInfoKind customDebugInfoKind,
            bool hasDynamicLocalVariables,
            ImmutableArray<Cci.NamespaceScope> namespaceScopes = default(ImmutableArray<Cci.NamespaceScope>),
            string iteratorClassName = null,
            ImmutableArray<Cci.LocalScope> iteratorScopes = default(ImmutableArray<Cci.LocalScope>),
            Cci.AsyncMethodBodyDebugInfo asyncMethodDebugInfo = null)
        {
            this.ilBits = ilBits;
            this.asyncMethodDebugInfo = asyncMethodDebugInfo;
            this.maxStack = maxStack;
            this.parent = parent;
            this.locals = locals;
            this.sequencePoints = sequencePoints;
            this.debugDocumentProvider = debugDocumentProvider;
            this.exceptionHandlers = exceptionHandlers;
            this.localScopes = localScopes;
            this.customDebugInfoKind = customDebugInfoKind;
            this.hasDynamicLocalVariables = hasDynamicLocalVariables;
            this.namespaceScopes = namespaceScopes.IsDefault ? ImmutableArray<Cci.NamespaceScope>.Empty : namespaceScopes;
            this.iteratorClassName = iteratorClassName;
            this.iteratorScopes = iteratorScopes.IsDefault ? ImmutableArray<Cci.LocalScope>.Empty : iteratorScopes;
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
            get { return StaticCast<Cci.ILocalDefinition>.From(this.locals); }
        }

        Cci.IMethodDefinition Cci.IMethodBody.MethodDefinition
        {
            get { return parent; }
        }

        Cci.AsyncMethodBodyDebugInfo Cci.IMethodBody.AsyncMethodDebugInfo
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

        public bool HasAnyLocations
        {
            get
            {
                return this.HasAnySequencePoints;
            }
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

        string Cci.IMethodBody.IteratorClassName
        {
            get
            {
                return iteratorClassName;
            }
        }

        ImmutableArray<Cci.LocalScope> Cci.IMethodBody.IteratorScopes
        {
            get
            {
                return this.iteratorScopes;
            }
        }

        public Cci.CustomDebugInfoKind CustomDebugInfoKind
        {
            get
            {
                return this.customDebugInfoKind;
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
