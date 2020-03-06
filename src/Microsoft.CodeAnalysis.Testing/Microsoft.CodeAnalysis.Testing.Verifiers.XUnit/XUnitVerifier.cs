// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.Testing.Verifiers
{
    public class XUnitVerifier : IVerifier
    {
        public XUnitVerifier()
            : this(ImmutableStack<string>.Empty)
        {
        }

        protected XUnitVerifier(ImmutableStack<string> context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        protected ImmutableStack<string> Context { get; }

        public virtual void Empty<T>(string collectionName, IEnumerable<T> collection)
        {
            using (var enumerator = collection.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    throw new EmptyWithMessageException(collection, CreateMessage($"'{collectionName}' is not empty"));
                }
            }
        }

        public virtual void Equal<T>(T expected, T actual, string? message = null)
        {
            if (message is null && Context.IsEmpty)
            {
                Assert.Equal(expected, actual);
            }
            else
            {
                if (!EqualityComparer<T>.Default.Equals(expected, actual))
                {
                    throw new EqualWithMessageException(expected, actual, CreateMessage(message));
                }
            }
        }

        public virtual void True([DoesNotReturnIf(false)] bool assert, string? message = null)
        {
            if (message is null && Context.IsEmpty)
            {
                Assert.True(assert);
            }
            else
            {
                Assert.True(assert, CreateMessage(message));
            }
        }

        public virtual void False([DoesNotReturnIf(true)] bool assert, string? message = null)
        {
            if (message is null && Context.IsEmpty)
            {
                Assert.False(assert);
            }
            else
            {
                Assert.False(assert, CreateMessage(message));
            }
        }

        [DoesNotReturn]
        public virtual void Fail(string? message = null)
        {
            if (message is null && Context.IsEmpty)
            {
                Assert.True(false);
            }
            else
            {
                Assert.True(false, CreateMessage(message));
            }

            throw ExceptionUtilities.Unreachable;
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
                    throw new NotEmptyWithMessageException(CreateMessage($"'{collectionName}' is empty"));
                }
            }
        }

        public virtual void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T>? equalityComparer = null, string? message = null)
        {
            if (message is null && Context.IsEmpty)
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
                    throw new EqualWithMessageException(expected, actual, CreateMessage(message));
                }
            }
        }

        public virtual IVerifier PushContext(string context)
        {
            Assert.IsType<XUnitVerifier>(this);
            return new XUnitVerifier(Context.Push(context));
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
