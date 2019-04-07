// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.CodeAnalysis.Testing.Verifiers
{
    public class XUnitVerifier : IVerifier
    {
        private readonly ImmutableStack<string> _context;

        public XUnitVerifier()
            : this(ImmutableStack<string>.Empty)
        {
        }

        private XUnitVerifier(ImmutableStack<string> context)
        {
            _context = context;
        }

        public virtual void Empty<T>(string collectionName, IEnumerable<T> collection)
        {
            using (var enumerator = collection.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    throw new XunitException(CreateMessage($"'{collectionName}' is not empty"));
                }
            }
        }

        public virtual void Equal<T>(T expected, T actual, string message = null)
        {
            if (message is null && _context.IsEmpty)
            {
                Assert.Equal(expected, actual);
            }
            else
            {
                if (!EqualityComparer<T>.Default.Equals(expected, actual))
                {
                    Assert.True(false, CreateMessage(message));
                }
            }
        }

        public virtual void True(bool assert, string message = null)
        {
            if (message is null && _context.IsEmpty)
            {
                Assert.True(assert);
            }
            else
            {
                Assert.True(assert, CreateMessage(message));
            }
        }

        public virtual void False(bool assert, string message = null)
        {
            if (message is null && _context.IsEmpty)
            {
                Assert.False(assert);
            }
            else
            {
                Assert.False(assert, CreateMessage(message));
            }
        }

        public virtual void Fail(string message = null)
        {
            if (message is null && _context.IsEmpty)
            {
                Assert.True(false);
            }
            else
            {
                Assert.True(false, CreateMessage(message));
            }
        }

        public virtual void LanguageIsSupported(string language)
        {
            Assert.False(language != LanguageNames.CSharp && language != LanguageNames.VisualBasic, CreateMessage($"Unsupported Language: '{language}'"));
        }

        public virtual void NotEmpty<T>(string collectionName, IEnumerable<T> collection)
        {
            using (var enumerator = collection.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                {
                    throw new XunitException(CreateMessage($"'{collectionName}' is empty"));
                }
            }
        }

        public virtual void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T> equalityComparer = null, string message = null)
        {
            if (message is null && _context.IsEmpty)
            {
                if (equalityComparer is null)
                {
                    Assert.Equal(expected, actual);
                }
                else
                {
                    Assert.Equal(expected, actual, equalityComparer);
                }
            }
            else
            {
                var comparer = new SequenceEqualEnumerableEqualityComparer<T>(equalityComparer);
                var areEqual = comparer.Equals(expected, actual);
                if (!areEqual)
                {
                    Assert.True(false, CreateMessage(message));
                }
            }
        }

        public virtual IVerifier PushContext(string context)
        {
            return new XUnitVerifier(_context.Push(context));
        }

        private string CreateMessage(string message)
        {
            foreach (var frame in _context)
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
