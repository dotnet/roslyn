// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    /// <summary>
    /// Extensions to <see cref="CancellationToken"/>.
    /// </summary>
    internal static class CancellationTokenExtensions
    {
        /// <summary>
        /// Creates a new <see cref="CancellationToken"/> that is canceled when any of a set of other tokens are canceled.
        /// </summary>
        /// <param name="original">The first token.</param>
        /// <param name="other">The second token.</param>
        /// <returns>A struct that contains the combined <see cref="CancellationToken"/> and a means to release memory when you're done using it.</returns>
        public static CombinedCancellationToken CombineWith(this CancellationToken original, CancellationToken other)
        {
            if (original.IsCancellationRequested || !other.CanBeCanceled)
            {
                return new CombinedCancellationToken(original);
            }

            if (other.IsCancellationRequested || !original.CanBeCanceled)
            {
                return new CombinedCancellationToken(other);
            }

            // This is the most expensive path to take since it involves allocating memory and requiring disposal.
            // Before this point we've checked every condition that would allow us to avoid it.
            return new CombinedCancellationToken(CancellationTokenSource.CreateLinkedTokenSource(original, other));
        }

        /// <summary>
        /// Creates a new <see cref="CancellationToken"/> that is canceled when any of a set of other tokens are canceled.
        /// </summary>
        /// <param name="original">The first token.</param>
        /// <param name="others">The additional tokens.</param>
        /// <returns>A struct that contains the combined <see cref="CancellationToken"/> and a means to release memory when you're done using it.</returns>
        public static CombinedCancellationToken CombineWith(this CancellationToken original, params CancellationToken[] others)
        {
            Contract.ThrowIfNull(others, nameof(others));

            if (original.IsCancellationRequested)
            {
                return new CombinedCancellationToken(original);
            }

            var cancelableTokensCount = original.CanBeCanceled ? 1 : 0;
            foreach (var other in others)
            {
                if (other.IsCancellationRequested)
                {
                    return new CombinedCancellationToken(other);
                }

                if (other.CanBeCanceled)
                {
                    cancelableTokensCount++;
                }
            }

            switch (cancelableTokensCount)
            {
                case 0:
                    return new CombinedCancellationToken(CancellationToken.None);

                case 1:
                    if (original.CanBeCanceled)
                    {
                        return new CombinedCancellationToken(original);
                    }

                    foreach (var other in others)
                    {
                        if (other.CanBeCanceled)
                        {
                            return new CombinedCancellationToken(other);
                        }
                    }

                    throw ExceptionUtilities.Unreachable;

                case 2:
                    var first = CancellationToken.None;
                    var second = CancellationToken.None;

                    if (original.CanBeCanceled)
                    {
                        first = original;
                    }

                    foreach (var other in others)
                    {
                        if (other.CanBeCanceled)
                        {
                            if (first.CanBeCanceled)
                            {
                                second = other;
                            }
                            else
                            {
                                first = other;
                            }
                        }
                    }

                    Debug.Assert(first.CanBeCanceled && second.CanBeCanceled);

                    // Call the overload that takes two CancellationTokens explicitly to avoid an array allocation.
                    return new CombinedCancellationToken(CancellationTokenSource.CreateLinkedTokenSource(first, second));

                default:
                    // This is the most expensive path to take since it involves allocating memory and requiring disposal.
                    // Before this point we've checked every condition that would allow us to avoid it.
                    var cancelableTokens = new CancellationToken[cancelableTokensCount];
                    var i = 0;
                    foreach (var other in others)
                    {
                        if (other.CanBeCanceled)
                        {
                            cancelableTokens[i++] = other;
                        }
                    }

                    return new CombinedCancellationToken(CancellationTokenSource.CreateLinkedTokenSource(cancelableTokens));
            }
        }

        /// <summary>
        /// Provides access to a <see cref="System.Threading.CancellationToken"/> that combines multiple other tokens,
        /// and allows convenient disposal of any applicable <see cref="CancellationTokenSource"/>.
        /// </summary>
        public readonly struct CombinedCancellationToken : IDisposable, IEquatable<CombinedCancellationToken>
        {
            /// <summary>
            /// The object to dispose when this struct is disposed.
            /// </summary>
            private readonly CancellationTokenSource _cancellationTokenSource;

            /// <summary>
            /// Initializes a new instance of the <see cref="CombinedCancellationToken"/> struct
            /// that contains an aggregate <see cref="System.Threading.CancellationToken"/> whose source must be disposed.
            /// </summary>
            /// <param name="cancellationTokenSource">The cancellation token source.</param>
            public CombinedCancellationToken(CancellationTokenSource cancellationTokenSource)
            {
                _cancellationTokenSource = cancellationTokenSource;
                Token = cancellationTokenSource.Token;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="CombinedCancellationToken"/> struct
            /// that represents just a single, non-disposable <see cref="System.Threading.CancellationToken"/>.
            /// </summary>
            /// <param name="cancellationToken">The cancellation token</param>
            public CombinedCancellationToken(CancellationToken cancellationToken)
            {
                _cancellationTokenSource = null;
                Token = cancellationToken;
            }

            /// <summary>
            /// Checks whether two instances of <see cref="CombinedCancellationToken"/> are equal.
            /// </summary>
            /// <param name="left">The left operand.</param>
            /// <param name="right">The right operand.</param>
            /// <returns><c>true</c> if they are equal; <c>false</c> otherwise.</returns>
            public static bool operator ==(CombinedCancellationToken left, CombinedCancellationToken right)
                => left.Equals(right);

            /// <summary>
            /// Checks whether two instances of <see cref="CombinedCancellationToken"/> are not equal.
            /// </summary>
            /// <param name="left">The left operand.</param>
            /// <param name="right">The right operand.</param>
            /// <returns><c>true</c> if they are not equal; <c>false</c> if they are equal.</returns>
            public static bool operator !=(CombinedCancellationToken left, CombinedCancellationToken right)
                => !(left == right);

            /// <summary>
            /// Gets the combined cancellation token.
            /// </summary>
            public CancellationToken Token { get; }

            /// <summary>
            /// Disposes the <see cref="CancellationTokenSource"/> behind this combined token, if any.
            /// </summary>
            public void Dispose()
            {
                _cancellationTokenSource?.Dispose();
            }

            /// <inheritdoc />
            public override bool Equals(object obj)
                => obj is CombinedCancellationToken other && Equals(other);

            /// <inheritdoc />
            public bool Equals(CombinedCancellationToken other)
                => _cancellationTokenSource == other._cancellationTokenSource && Token.Equals(other.Token);

            /// <inheritdoc />
            public override int GetHashCode()
                => (_cancellationTokenSource?.GetHashCode() ?? 0) + Token.GetHashCode();
        }
    }
}
