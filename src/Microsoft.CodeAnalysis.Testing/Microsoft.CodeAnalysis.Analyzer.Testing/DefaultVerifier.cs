// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// Provides a default implementation of <see cref="IVerifier"/>.
    /// </summary>
    /// <remarks>
    /// This verifier is not dependent on any particular test framework. Each verification method throws
    /// <see cref="InvalidOperationException"/> on failure.
    /// </remarks>
    public class DefaultVerifier : IVerifier
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultVerifier"/> class.
        /// </summary>
        public DefaultVerifier()
            : this(ImmutableStack<string>.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultVerifier"/> class with the specified context.
        /// </summary>
        /// <param name="context">The verification context, with the innermost verification context label at the top of
        /// the stack.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="context"/> is <see langword="null"/>.</exception>
        protected DefaultVerifier(ImmutableStack<string> context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Gets the current verification context. The innermost verification context label is the top item on the
        /// stack.
        /// </summary>
        protected ImmutableStack<string> Context { get; }

        /// <inheritdoc/>
        public virtual void Empty<T>(string collectionName, IEnumerable<T> collection)
        {
            if (collection?.Any() == true)
            {
                throw new InvalidOperationException(CreateMessage($"'{collectionName}' is not empty"));
            }
        }

        /// <inheritdoc/>
        public virtual void NotEmpty<T>(string collectionName, IEnumerable<T> collection)
        {
            if (collection?.Any() == false)
            {
                throw new InvalidOperationException(CreateMessage($"'{collectionName}' is empty"));
            }
        }

        /// <inheritdoc/>
        public virtual void LanguageIsSupported(string language)
        {
            if (language != LanguageNames.CSharp && language != LanguageNames.VisualBasic)
            {
                throw new InvalidOperationException(CreateMessage($"Unsupported Language: '{language}'"));
            }
        }

        /// <inheritdoc/>
        public virtual void Equal<T>(T expected, T actual, string? message = null)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(CreateMessage(message ?? $"items not equal.  expected:'{expected}' actual:'{actual}'"));
            }
        }

        /// <inheritdoc/>
        public virtual void True(bool assert, string? message = null)
        {
            if (!assert)
            {
                throw new InvalidOperationException(CreateMessage(message ?? $"Expected value to be 'true' but was 'false'"));
            }
        }

        /// <inheritdoc/>
        public virtual void False(bool assert, string? message = null)
        {
            if (assert)
            {
                throw new InvalidOperationException(CreateMessage(message ?? $"Expected value to be 'false' but was 'true'"));
            }
        }

        /// <inheritdoc/>
        public virtual void Fail(string? message = null)
        {
            throw new InvalidOperationException(CreateMessage(message ?? "Verification failed for an unspecified reason."));
        }

        /// <inheritdoc/>
        public virtual void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T>? equalityComparer = null, string? message = null)
        {
            var comparer = new SequenceEqualEnumerableEqualityComparer<T>(equalityComparer);
            var areEqual = comparer.Equals(expected, actual);
            if (!areEqual)
            {
                throw new InvalidOperationException(CreateMessage(message ?? $"Sequences are not equal"));
            }
        }

        /// <inheritdoc/>
        public virtual IVerifier PushContext(string context)
        {
            if (GetType() != typeof(DefaultVerifier))
            {
                throw new InvalidOperationException($"Custom verifier types must override {nameof(PushContext)}");
            }

            return new DefaultVerifier(Context.Push(context));
        }

        /// <summary>
        /// Creates a full message for a verifier failure combining the current verification <see cref="Context"/> with
        /// the <paramref name="message"/> for the current verification.
        /// </summary>
        /// <param name="message">The failure message to report.</param>
        /// <returns>A full failure message containing both the verification context and the failure message for the current test.</returns>
        protected virtual string CreateMessage(string message)
        {
            foreach (var frame in Context)
            {
                message = "Context: " + frame + Environment.NewLine + message;
            }

            return message;
        }

        private sealed class SequenceEqualEnumerableEqualityComparer<T> : IEqualityComparer<IEnumerable<T>?>
        {
            private readonly IEqualityComparer<T> _itemEqualityComparer;

            public SequenceEqualEnumerableEqualityComparer(IEqualityComparer<T>? itemEqualityComparer)
            {
                _itemEqualityComparer = itemEqualityComparer ?? EqualityComparer<T>.Default;
            }

            public bool Equals(IEnumerable<T>? x, IEnumerable<T>? y)
            {
                if (ReferenceEquals(x, y)) { return true; }
                if (x is null || y is null) { return false; }

                return x.SequenceEqual(y, _itemEqualityComparer);
            }

            public int GetHashCode(IEnumerable<T>? obj)
            {
                if (obj is null)
                {
                    return 0;
                }

                // From System.Tuple
                return obj
                    .Select(item => _itemEqualityComparer.GetHashCode(item))
                    .Aggregate(
                        0,
                        (aggHash, nextHash) => ((aggHash << 5) + aggHash) ^ nextHash);
            }
        }
    }
}
