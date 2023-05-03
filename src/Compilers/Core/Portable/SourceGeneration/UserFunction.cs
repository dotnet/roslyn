// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal sealed class UserFunctionException : Exception
    {
        public new Exception InnerException => base.InnerException!;

        public UserFunctionException(Exception innerException)
            : base("User provided code threw an exception", innerException)
        {
        }
    }

    internal sealed class WrappedUserComparer<T> : IEqualityComparer<T>
    {
        private readonly IEqualityComparer<T> _inner;

        public WrappedUserComparer(IEqualityComparer<T> inner)
        {
            _inner = inner;
        }

        public bool Equals(T? x, T? y)
        {
            try
            {
                return _inner.Equals(x, y);
            }
            catch (Exception e)
            {
                throw new UserFunctionException(e);
            }
        }

        public int GetHashCode([DisallowNull] T obj)
        {
            try
            {
                return _inner.GetHashCode(obj);
            }
            catch (Exception e)
            {
                throw new UserFunctionException(e);
            }
        }
    }

    internal static class UserFunctionExtensions
    {
        internal static Func<TInput, CancellationToken, TOutput> WrapUserFunction<TInput, TOutput>(this Func<TInput, CancellationToken, TOutput> userFunction)
        {
            return (input, token) =>
            {
                try
                {
                    return userFunction(input, token);
                }
                catch (Exception e) when (!ExceptionUtilities.IsCurrentOperationBeingCancelled(e, token))
                {
                    throw new UserFunctionException(e);
                }
            };
        }

        internal static Func<TInput, CancellationToken, ImmutableArray<TOutput>> WrapUserFunctionAsImmutableArray<TInput, TOutput>(this Func<TInput, CancellationToken, IEnumerable<TOutput>> userFunction)
        {
            var wrappedUserFunction = userFunction.WrapUserFunction();
            return (input, token) => wrappedUserFunction(input, token).ToImmutableArrayOrEmpty();
        }

        internal static Action<TInput, CancellationToken> WrapUserAction<TInput>(this Action<TInput> userAction)
        {
            return (input, token) =>
            {
                try
                {
                    userAction(input);
                }
                catch (Exception e) when (!ExceptionUtilities.IsCurrentOperationBeingCancelled(e, token))
                {
                    throw new UserFunctionException(e);
                }
            };
        }

        internal static Action<TInput1, TInput2, CancellationToken> WrapUserAction<TInput1, TInput2>(this Action<TInput1, TInput2> userAction)
        {
            return (input1, input2, token) =>
            {
                try
                {
                    userAction(input1, input2);
                }
                catch (Exception e) when (!ExceptionUtilities.IsCurrentOperationBeingCancelled(e, token))
                {
                    throw new UserFunctionException(e);
                }
            };
        }

        internal static IEqualityComparer<T> WrapUserComparer<T>(this IEqualityComparer<T> comparer)
        {
            if (comparer is not WrappedUserComparer<T> wrappedComparer)
                wrappedComparer = new WrappedUserComparer<T>(comparer);

            return wrappedComparer;
        }

        internal static Func<TSource, CancellationToken, ImmutableArray<TSource>> WrapPredicateForSelectMany<TSource>(this Func<TSource, bool> predicate)
        {
            return (item, _) => predicate(item) ? ImmutableArray.Create(item) : ImmutableArray<TSource>.Empty;
        }

        internal static Func<TSource, CancellationToken, ImmutableArray<TSource>> WrapPredicateForSelectMany<TSource>(this Func<TSource, CancellationToken, bool> predicate)
        {
            return (item, cancellationToken) => predicate(item, cancellationToken) ? ImmutableArray.Create(item) : ImmutableArray<TSource>.Empty;
        }
    }
}
