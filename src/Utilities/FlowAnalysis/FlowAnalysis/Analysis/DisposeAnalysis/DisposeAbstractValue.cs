// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Analyzer.Utilities;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.DisposeAnalysis
{
    /// <summary>
    /// Abstract dispose data tracked by <see cref="DisposeAnalysis"/>.
    /// It contains the set of <see cref="IOperation"/>s that dispose an associated disposable <see cref="AbstractLocation"/> and
    /// the dispose <see cref="Kind"/>.
    /// </summary>
    public class DisposeAbstractValue : CacheBasedEquatable<DisposeAbstractValue>
    {
        public static readonly DisposeAbstractValue NotDisposable = new DisposeAbstractValue(DisposeAbstractValueKind.NotDisposable);
        public static readonly DisposeAbstractValue Invalid = new DisposeAbstractValue(DisposeAbstractValueKind.Invalid);
        public static readonly DisposeAbstractValue NotDisposed = new DisposeAbstractValue(DisposeAbstractValueKind.NotDisposed);
        public static readonly DisposeAbstractValue Unknown = new DisposeAbstractValue(DisposeAbstractValueKind.Unknown);

        private DisposeAbstractValue(DisposeAbstractValueKind kind)
            : this(ImmutableHashSet<IOperation>.Empty, kind)
        {
            Debug.Assert(kind != DisposeAbstractValueKind.Disposed);
        }

        internal DisposeAbstractValue(ImmutableHashSet<IOperation> disposingOrEscapingOperations, DisposeAbstractValueKind kind)
        {
            VerifyArguments(disposingOrEscapingOperations, kind);
            DisposingOrEscapingOperations = disposingOrEscapingOperations;
            Kind = kind;
        }

        internal DisposeAbstractValue WithNewDisposingOperation(IOperation disposingOperation)
        {
            Debug.Assert(Kind != DisposeAbstractValueKind.NotDisposable);

            return new DisposeAbstractValue(DisposingOrEscapingOperations.Add(disposingOperation), DisposeAbstractValueKind.Disposed);
        }

        internal DisposeAbstractValue WithNewEscapingOperation(IOperation escapingOperation)
        {
            Debug.Assert(Kind != DisposeAbstractValueKind.NotDisposable);
            Debug.Assert(Kind != DisposeAbstractValueKind.Unknown);

            return new DisposeAbstractValue(ImmutableHashSet.Create(escapingOperation), DisposeAbstractValueKind.Escaped);
        }

        [Conditional("DEBUG")]
        private static void VerifyArguments(ImmutableHashSet<IOperation> disposingOrEscapingOperations, DisposeAbstractValueKind kind)
        {
            switch (kind)
            {
                case DisposeAbstractValueKind.NotDisposable:
                case DisposeAbstractValueKind.NotDisposed:
                case DisposeAbstractValueKind.Invalid:
                case DisposeAbstractValueKind.Unknown:
                    Debug.Assert(disposingOrEscapingOperations.Count == 0);
                    break;

                case DisposeAbstractValueKind.Escaped:
                case DisposeAbstractValueKind.Disposed:
                case DisposeAbstractValueKind.MaybeDisposed:
                    Debug.Assert(disposingOrEscapingOperations.Count > 0);
                    break;
            }
        }

        public ImmutableHashSet<IOperation> DisposingOrEscapingOperations { get; }
        public DisposeAbstractValueKind Kind { get; }

        protected override void ComputeHashCodeParts(Action<int> addPart)
        {
            addPart(HashUtilities.Combine(DisposingOrEscapingOperations));
            addPart(Kind.GetHashCode());
        }
    }
}
