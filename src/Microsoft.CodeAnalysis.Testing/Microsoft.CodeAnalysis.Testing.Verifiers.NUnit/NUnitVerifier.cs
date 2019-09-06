// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NUnit.Framework;

namespace Microsoft.CodeAnalysis.Testing.Verifiers
{
    public class NUnitVerifier : IVerifier
    {
        public NUnitVerifier()
            : this(ImmutableStack<string>.Empty)
        {
        }

        protected NUnitVerifier(ImmutableStack<string> context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        protected ImmutableStack<string> Context { get; }

        public virtual void Empty<T>(string collectionName, IEnumerable<T> collection)
        {
            Assert.IsEmpty(collection, CreateMessage($"Expected '{collectionName}' to be empty, contains '{collection?.Count()}' elements"));
        }

        public virtual void Equal<T>(T expected, T actual, string? message = null)
        {
            if (message is null && Context.IsEmpty)
            {
                Assert.AreEqual(expected, actual);
            }
            else
            {
                Assert.AreEqual(expected, actual, CreateMessage(message));
            }
        }

        public virtual void True(bool assert, string? message = null)
        {
            if (message is null && Context.IsEmpty)
            {
                Assert.IsTrue(assert);
            }
            else
            {
                Assert.IsTrue(assert, CreateMessage(message));
            }
        }

        public virtual void False(bool assert, string? message = null)
        {
            if (message is null && Context.IsEmpty)
            {
                Assert.IsFalse(assert);
            }
            else
            {
                Assert.IsFalse(assert, CreateMessage(message));
            }
        }

        public virtual void Fail(string? message = null)
        {
            if (message is null && Context.IsEmpty)
            {
                Assert.Fail();
            }
            else
            {
                Assert.Fail(CreateMessage(message));
            }
        }

        public virtual void LanguageIsSupported(string language)
        {
            Assert.IsFalse(language != LanguageNames.CSharp && language != LanguageNames.VisualBasic, CreateMessage($"Unsupported Language: '{language}'"));
        }

        public virtual void NotEmpty<T>(string collectionName, IEnumerable<T> collection)
        {
            Assert.IsNotEmpty(collection, CreateMessage($"expected '{collectionName}' to be non-empty, contains"));
        }

        public virtual void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T>? equalityComparer = null, string? message = null)
        {
            var comparer = new SequenceEqualEnumerableEqualityComparer<T>(equalityComparer);
            var areEqual = comparer.Equals(expected, actual);
            if (!areEqual)
            {
                Assert.Fail(CreateMessage(message));
            }
        }

        public virtual IVerifier PushContext(string context)
        {
            Assert.AreEqual(typeof(NUnitVerifier), GetType());
            return new NUnitVerifier(Context.Push(context));
        }

        protected virtual string CreateMessage(string? message)
        {
            foreach (var frame in Context)
            {
                message = "Context: " + frame + Environment.NewLine + message;
            }

            return message ?? string.Empty;
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
