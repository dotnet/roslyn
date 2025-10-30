// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public static readonly DisposeAbstractValue NotDisposable = new(DisposeAbstractValueKind.NotDisposable);
        public static readonly DisposeAbstractValue Invalid = new(DisposeAbstractValueKind.Invalid);
        public static readonly DisposeAbstractValue NotDisposed = new(DisposeAbstractValueKind.NotDisposed);
        public static readonly DisposeAbstractValue Unknown = new(DisposeAbstractValueKind.Unknown);

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
                    Debug.Assert(disposingOrEscapingOperations.IsEmpty);
                    break;

                case DisposeAbstractValueKind.Escaped:
                case DisposeAbstractValueKind.Disposed:
                case DisposeAbstractValueKind.MaybeDisposed:
                    Debug.Assert(!disposingOrEscapingOperations.IsEmpty);
                    break;
            }
        }

        public ImmutableHashSet<IOperation> DisposingOrEscapingOperations { get; }
        public DisposeAbstractValueKind Kind { get; }

        protected override void ComputeHashCodeParts(ref RoslynHashCode hashCode)
        {
            hashCode.Add(HashUtilities.Combine(DisposingOrEscapingOperations));
            hashCode.Add(((int)Kind).GetHashCode());
        }

        protected override bool ComputeEqualsByHashCodeParts(CacheBasedEquatable<DisposeAbstractValue> obj)
        {
            var other = (DisposeAbstractValue)obj;
            return HashUtilities.Combine(DisposingOrEscapingOperations) == HashUtilities.Combine(other.DisposingOrEscapingOperations)
                && ((int)Kind).GetHashCode() == ((int)other.Kind).GetHashCode();
        }
    }
}
