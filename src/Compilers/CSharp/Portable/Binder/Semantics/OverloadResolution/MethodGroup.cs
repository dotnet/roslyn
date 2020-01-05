// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    // pooled class used for input to overload resolution
    internal sealed class MethodGroup
    {
        internal BoundExpression Receiver { get; private set; }
        internal ArrayBuilder<MethodSymbol> Methods { get; }
        internal ArrayBuilder<TypeWithAnnotations> TypeArguments { get; }
        internal bool IsExtensionMethodGroup { get; private set; }
        internal DiagnosticInfo Error { get; private set; }
        internal LookupResultKind ResultKind { get; private set; }

        private MethodGroup()
        {
            this.Methods = new ArrayBuilder<MethodSymbol>();
            this.TypeArguments = new ArrayBuilder<TypeWithAnnotations>();
        }

        internal void PopulateWithSingleMethod(
            BoundExpression receiverOpt,
            MethodSymbol method,
            LookupResultKind resultKind = LookupResultKind.Viable,
            DiagnosticInfo error = null)
        {
            this.PopulateHelper(receiverOpt, resultKind, error);
            this.Methods.Add(method);
        }

        internal void PopulateWithExtensionMethods(
            BoundExpression receiverOpt,
            ArrayBuilder<Symbol> members,
            ImmutableArray<TypeWithAnnotations> typeArguments,
            LookupResultKind resultKind = LookupResultKind.Viable,
            DiagnosticInfo error = null)
        {
            this.PopulateHelper(receiverOpt, resultKind, error);
            this.IsExtensionMethodGroup = true;
            foreach (var member in members)
            {
                this.Methods.Add((MethodSymbol)member);
            }
            if (!typeArguments.IsDefault)
            {
                this.TypeArguments.AddRange(typeArguments);
            }
        }

        internal void PopulateWithNonExtensionMethods(
            BoundExpression receiverOpt,
            ImmutableArray<MethodSymbol> methods,
            ImmutableArray<TypeWithAnnotations> typeArguments,
            LookupResultKind resultKind = LookupResultKind.Viable,
            DiagnosticInfo error = null)
        {
            this.PopulateHelper(receiverOpt, resultKind, error);
            this.Methods.AddRange(methods);
            if (!typeArguments.IsDefault)
            {
                this.TypeArguments.AddRange(typeArguments);
            }
        }

        private void PopulateHelper(BoundExpression receiverOpt, LookupResultKind resultKind, DiagnosticInfo error)
        {
            VerifyClear();
            this.Receiver = receiverOpt;
            this.Error = error;
            this.ResultKind = resultKind;
        }

        public void Clear()
        {
            this.Receiver = null;
            this.Methods.Clear();
            this.TypeArguments.Clear();
            this.IsExtensionMethodGroup = false;
            this.Error = null;
            this.ResultKind = LookupResultKind.Empty;

            VerifyClear();
        }

        public string Name
        {
            get
            {
                return this.Methods.Count > 0 ? this.Methods[0].Name : null;
            }
        }

        public BoundExpression InstanceOpt
        {
            get
            {
                if (this.Receiver == null)
                {
                    return null;
                }

                if (this.Receiver.Kind == BoundKind.TypeExpression)
                {
                    return null;
                }

                return this.Receiver;
            }
        }

        [Conditional("DEBUG")]
        private void VerifyClear()
        {
            Debug.Assert(this.Receiver == null);
            Debug.Assert(this.Methods.Count == 0);
            Debug.Assert(this.TypeArguments.Count == 0);
            Debug.Assert(!this.IsExtensionMethodGroup);
            Debug.Assert(this.Error == null);
            Debug.Assert(this.ResultKind == LookupResultKind.Empty);
        }

        #region "Poolable"

        public static MethodGroup GetInstance()
        {
            return Pool.Allocate();
        }

        public void Free()
        {
            this.Clear();
            Pool.Free(this);
        }

        //2) Expose the pool or the way to create a pool or the way to get an instance.
        //       for now we will expose both and figure which way works better
        public static readonly ObjectPool<MethodGroup> Pool = CreatePool();

        private static ObjectPool<MethodGroup> CreatePool()
        {
            ObjectPool<MethodGroup> pool = null;
            pool = new ObjectPool<MethodGroup>(() => new MethodGroup(), 10);
            return pool;
        }

        #endregion
    }
}
