// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.Testing
{
    public class DefaultVerifier : IVerifier
    {
        public DefaultVerifier()
            : this(ImmutableStack<string>.Empty)
        {
        }

        private DefaultVerifier(ImmutableStack<string> context)
        {
            Context = context;
        }

        private ImmutableStack<string> Context { get; }

        public virtual void Empty<T>(string collectionName, IEnumerable<T> collection)
        {
            if (collection?.Any() == true)
            {
                throw new InvalidOperationException(CreateMessage($"'{collectionName}' is not empty"));
            }
        }

        public virtual void NotEmpty<T>(string collectionName, IEnumerable<T> collection)
        {
            if (collection?.Any() == false)
            {
                throw new InvalidOperationException(CreateMessage($"'{collectionName}' is empty"));
            }
        }

        public virtual void LanguageIsSupported(string language)
        {
            if (language != LanguageNames.CSharp && language != LanguageNames.VisualBasic)
            {
                throw new InvalidOperationException(CreateMessage($"Unsupported Language: '{language}'"));
            }
        }

        public virtual void Equal<T>(T expected, T actual, string message = null)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(CreateMessage(message ?? $"items not equal.  expected:'{expected}' actual:'{actual}'"));
            }
        }

        public virtual void True(bool assert, string message = null)
        {
            if (!assert)
            {
                throw new InvalidOperationException(CreateMessage(message ?? $"Expected value to be 'true' but was 'false'"));
            }
        }

        public virtual void False(bool assert, string message = null)
        {
            if (assert)
            {
                throw new InvalidOperationException(CreateMessage(message ?? $"Expected value to be 'false' but was 'true'"));
            }
        }

        public virtual void Fail(string message = null)
        {
            throw new InvalidOperationException(CreateMessage(message ?? "Verification failed for an unspecified reason."));
        }

        public virtual void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T> equalityComparer = null, string message = null)
        {
            var comparer = new SequenceEqualEnumerableEqualityComparer<T>(equalityComparer);
            var areEqual = comparer.Equals(expected, actual);
            if (!areEqual)
            {
                throw new InvalidOperationException(CreateMessage(message ?? $"Sequences are not equal"));
            }
        }

        public virtual IVerifier PushContext(string context)
        {
            return new DefaultVerifier(Context.Push(context));
        }

        private string CreateMessage(string message)
        {
            foreach (var frame in Context)
            {
                message = "Context: " + frame + Environment.NewLine + message;
            }

            return message;
        }

        private sealed class SequenceEqualEnumerableEqualityComparer<T> : IEqualityComparer<IEnumerable<T>>
        {
            private readonly IEqualityComparer<T> _itemEqualityComparer;

            public SequenceEqualEnumerableEqualityComparer(IEqualityComparer<T> itemEqualityComparer)
            {
                _itemEqualityComparer = itemEqualityComparer ?? EqualityComparer<T>.Default;
            }

            public bool Equals(IEnumerable<T> x, IEnumerable<T> y)
            {
                if (ReferenceEquals(x, y)) { return true; }
                if (x is null || y is null) { return false; }

                return x.SequenceEqual(y, _itemEqualityComparer);
            }

            public int GetHashCode(IEnumerable<T> obj)
            {
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
