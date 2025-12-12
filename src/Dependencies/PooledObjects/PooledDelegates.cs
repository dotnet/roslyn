// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.PooledObjects
{
    /// <summary>
    /// Provides pooled delegate instances to help avoid closure allocations for delegates that require a state argument
    /// with APIs that do not provide appropriate overloads with state arguments.
    /// </summary>
    internal static class PooledDelegates
    {
        private static class DefaultDelegatePool<T>
            where T : class, new()
        {
            public static readonly ObjectPool<T> Instance = new(() => new T(), 20);
        }

        private static Releaser GetPooledDelegate<TPooled, TArg, TUnboundDelegate, TBoundDelegate>(TUnboundDelegate unboundDelegate, TArg argument, out TBoundDelegate boundDelegate)
            where TPooled : AbstractDelegateWithBoundArgument<TPooled, TArg, TUnboundDelegate, TBoundDelegate>, new()
            where TUnboundDelegate : Delegate
            where TBoundDelegate : Delegate
        {
            var obj = DefaultDelegatePool<TPooled>.Instance.Allocate();
            obj.Initialize(unboundDelegate, argument);
            boundDelegate = obj.BoundDelegate;

            return new Releaser(obj);
        }

        /// <summary>
        /// Gets an <see cref="Action"/> delegate, which calls <paramref name="unboundAction"/> with the specified
        /// <paramref name="argument"/>. The resulting <paramref name="boundAction"/> may be called any number of times
        /// until the returned <see cref="Releaser"/> is disposed.
        /// </summary>
        /// <example>
        /// <para>The following example shows the use of a capturing delegate for a callback action that requires an
        /// argument:</para>
        ///
        /// <code>
        /// int x = 3;
        /// RunWithActionCallback(() => this.DoSomething(x));
        /// </code>
        ///
        /// <para>The following example shows the use of a pooled delegate to avoid capturing allocations for the same
        /// callback action:</para>
        ///
        /// <code>
        /// int x = 3;
        /// using var _ = GetPooledAction(arg => arg.self.DoSomething(arg.x), (self: this, x), out Action action);
        /// RunWithActionCallback(action);
        /// </code>
        /// </example>
        /// <typeparam name="TArg">The type of argument to pass to <paramref name="unboundAction"/>.</typeparam>
        /// <param name="unboundAction">The unbound action delegate.</param>
        /// <param name="argument">The argument to pass to the unbound action delegate.</param>
        /// <param name="boundAction">A delegate which calls <paramref name="unboundAction"/> with the specified
        /// <paramref name="argument"/>.</param>
        /// <returns>A disposable <see cref="Releaser"/> which returns the object to the delegate pool.</returns>
        public static Releaser GetPooledAction<TArg>(Action<TArg> unboundAction, TArg argument, out Action boundAction)
            => GetPooledDelegate<ActionWithBoundArgument<TArg>, TArg, Action<TArg>, Action>(unboundAction, argument, out boundAction);

        /// <summary>
        /// Gets an <see cref="Action{T}"/> delegate, which calls <paramref name="unboundAction"/> with the specified
        /// <paramref name="argument"/>. The resulting <paramref name="boundAction"/> may be called any number of times
        /// until the returned <see cref="Releaser"/> is disposed.
        /// </summary>
        /// <example>
        /// <para>The following example shows the use of a capturing delegate for a callback action that requires an
        /// argument:</para>
        ///
        /// <code>
        /// int x = 3;
        /// RunWithActionCallback(a => this.DoSomething(a, x));
        /// </code>
        ///
        /// <para>The following example shows the use of a pooled delegate to avoid capturing allocations for the same
        /// callback action:</para>
        ///
        /// <code>
        /// int x = 3;
        /// using var _ = GetPooledAction((a, arg) => arg.self.DoSomething(a, arg.x), (self: this, x), out Action&lt;int&gt; action);
        /// RunWithActionCallback(action);
        /// </code>
        /// </example>
        /// <typeparam name="T1">The type of the first parameter of the bound action.</typeparam>
        /// <typeparam name="TArg">The type of argument to pass to <paramref name="unboundAction"/>.</typeparam>
        /// <param name="unboundAction">The unbound action delegate.</param>
        /// <param name="argument">The argument to pass to the unbound action delegate.</param>
        /// <param name="boundAction">A delegate which calls <paramref name="unboundAction"/> with the specified
        /// <paramref name="argument"/>.</param>
        /// <returns>A disposable <see cref="Releaser"/> which returns the object to the delegate pool.</returns>
        public static Releaser GetPooledAction<T1, TArg>(Action<T1, TArg> unboundAction, TArg argument, out Action<T1> boundAction)
            => GetPooledDelegate<ActionWithBoundArgument<T1, TArg>, TArg, Action<T1, TArg>, Action<T1>>(unboundAction, argument, out boundAction);

        /// <summary>
        /// Gets an <see cref="Action{T1, T2}"/> delegate, which calls <paramref name="unboundAction"/> with the specified
        /// <paramref name="argument"/>. The resulting <paramref name="boundAction"/> may be called any number of times
        /// until the returned <see cref="Releaser"/> is disposed.
        /// </summary>
        /// <example>
        /// <para>The following example shows the use of a capturing delegate for a callback action that requires an
        /// argument:</para>
        ///
        /// <code>
        /// int x = 3;
        /// RunWithActionCallback((a, b) => this.DoSomething(a, b, x));
        /// </code>
        ///
        /// <para>The following example shows the use of a pooled delegate to avoid capturing allocations for the same
        /// callback action:</para>
        ///
        /// <code>
        /// int x = 3;
        /// using var _ = GetPooledAction((a, b, arg) => arg.self.DoSomething(a, b, arg.x), (self: this, x), out Action&lt;int, int&gt; action);
        /// RunWithActionCallback(action);
        /// </code>
        /// </example>
        /// <typeparam name="T1">The type of the first parameter of the bound action.</typeparam>
        /// <typeparam name="T2">The type of the second parameter of the bound action.</typeparam>
        /// <typeparam name="TArg">The type of argument to pass to <paramref name="unboundAction"/>.</typeparam>
        /// <param name="unboundAction">The unbound action delegate.</param>
        /// <param name="argument">The argument to pass to the unbound action delegate.</param>
        /// <param name="boundAction">A delegate which calls <paramref name="unboundAction"/> with the specified
        /// <paramref name="argument"/>.</param>
        /// <returns>A disposable <see cref="Releaser"/> which returns the object to the delegate pool.</returns>
        public static Releaser GetPooledAction<T1, T2, TArg>(Action<T1, T2, TArg> unboundAction, TArg argument, out Action<T1, T2> boundAction)
            => GetPooledDelegate<ActionWithBoundArgument<T1, T2, TArg>, TArg, Action<T1, T2, TArg>, Action<T1, T2>>(unboundAction, argument, out boundAction);

        /// <summary>
        /// Gets an <see cref="Action{T1, T2, T3}"/> delegate, which calls <paramref name="unboundAction"/> with the specified
        /// <paramref name="argument"/>. The resulting <paramref name="boundAction"/> may be called any number of times
        /// until the returned <see cref="Releaser"/> is disposed.
        /// </summary>
        /// <example>
        /// <para>The following example shows the use of a capturing delegate for a callback action that requires an
        /// argument:</para>
        ///
        /// <code>
        /// int x = 3;
        /// RunWithActionCallback((a, b, c) => this.DoSomething(a, b, c, x));
        /// </code>
        ///
        /// <para>The following example shows the use of a pooled delegate to avoid capturing allocations for the same
        /// callback action:</para>
        ///
        /// <code>
        /// int x = 3;
        /// using var _ = GetPooledAction((a, b, c, arg) => arg.self.DoSomething(a, b, c, arg.x), (self: this, x), out Action&lt;int, int, int&gt; action);
        /// RunWithActionCallback(action);
        /// </code>
        /// </example>
        /// <typeparam name="T1">The type of the first parameter of the bound action.</typeparam>
        /// <typeparam name="T2">The type of the second parameter of the bound action.</typeparam>
        /// <typeparam name="T3">The type of the third parameter of the bound action.</typeparam>
        /// <typeparam name="TArg">The type of argument to pass to <paramref name="unboundAction"/>.</typeparam>
        /// <param name="unboundAction">The unbound action delegate.</param>
        /// <param name="argument">The argument to pass to the unbound action delegate.</param>
        /// <param name="boundAction">A delegate which calls <paramref name="unboundAction"/> with the specified
        /// <paramref name="argument"/>.</param>
        /// <returns>A disposable <see cref="Releaser"/> which returns the object to the delegate pool.</returns>
        public static Releaser GetPooledAction<T1, T2, T3, TArg>(Action<T1, T2, T3, TArg> unboundAction, TArg argument, out Action<T1, T2, T3> boundAction)
            => GetPooledDelegate<ActionWithBoundArgument<T1, T2, T3, TArg>, TArg, Action<T1, T2, T3, TArg>, Action<T1, T2, T3>>(unboundAction, argument, out boundAction);

        /// <summary>
        /// Gets a <see cref="Func{TResult}"/> delegate, which calls <paramref name="unboundFunction"/> with the
        /// specified <paramref name="argument"/>. The resulting <paramref name="boundFunction"/> may be called any
        /// number of times until the returned <see cref="Releaser"/> is disposed.
        /// </summary>
        /// <example>
        /// <para>The following example shows the use of a capturing delegate for a predicate that requires an
        /// argument:</para>
        ///
        /// <code>
        /// int x = 3;
        /// RunWithPredicate(() => this.IsSomething(x));
        /// </code>
        ///
        /// <para>The following example shows the use of a pooled delegate to avoid capturing allocations for the same
        /// predicate:</para>
        ///
        /// <code>
        /// int x = 3;
        /// using var _ = GetPooledFunction(arg => arg.self.IsSomething(arg.x), (self: this, x), out Func&lt;bool&gt; predicate);
        /// RunWithPredicate(predicate);
        /// </code>
        /// </example>
        /// <typeparam name="TArg">The type of argument to pass to <paramref name="unboundFunction"/>.</typeparam>
        /// <typeparam name="TResult">The type of the return value of the function.</typeparam>
        /// <param name="unboundFunction">The unbound function delegate.</param>
        /// <param name="argument">The argument to pass to the unbound function delegate.</param>
        /// <param name="boundFunction">A delegate which calls <paramref name="unboundFunction"/> with the specified
        /// <paramref name="argument"/>.</param>
        /// <returns>A disposable <see cref="Releaser"/> which returns the object to the delegate pool.</returns>
        public static Releaser GetPooledFunction<TArg, TResult>(Func<TArg, TResult> unboundFunction, TArg argument, out Func<TResult> boundFunction)
            => GetPooledDelegate<FuncWithBoundArgument<TArg, TResult>, TArg, Func<TArg, TResult>, Func<TResult>>(unboundFunction, argument, out boundFunction);

        /// <summary>
        /// Equivalent to <see cref="GetPooledFunction{TArg, TResult}(Func{TArg, TResult}, TArg, out Func{TResult})"/>,
        /// except typed such that it can be used to create a pooled <see cref="ConditionalWeakTable{TKey,
        /// TValue}.CreateValueCallback"/>.
        /// </summary>
        public static Releaser GetPooledCreateValueCallback<TKey, TArg, TValue>(
            Func<TKey, TArg, TValue> unboundFunction, TArg argument,
            out ConditionalWeakTable<TKey, TValue>.CreateValueCallback boundFunction) where TKey : class where TValue : class

        {
            return GetPooledDelegate<CreateValueCallbackWithBoundArgument<TKey, TArg, TValue>, TArg, Func<TKey, TArg, TValue>, ConditionalWeakTable<TKey, TValue>.CreateValueCallback>(unboundFunction, argument, out boundFunction);
        }
        /// <summary>
        /// Gets a <see cref="Func{T, TResult}"/> delegate, which calls <paramref name="unboundFunction"/> with the
        /// specified <paramref name="argument"/>. The resulting <paramref name="boundFunction"/> may be called any
        /// number of times until the returned <see cref="Releaser"/> is disposed.
        /// </summary>
        /// <example>
        /// <para>The following example shows the use of a capturing delegate for a predicate that requires an
        /// argument:</para>
        ///
        /// <code>
        /// int x = 3;
        /// RunWithPredicate(a => this.IsSomething(a, x));
        /// </code>
        ///
        /// <para>The following example shows the use of a pooled delegate to avoid capturing allocations for the same
        /// predicate:</para>
        ///
        /// <code>
        /// int x = 3;
        /// using var _ = GetPooledFunction((a, arg) => arg.self.IsSomething(a, arg.x), (self: this, x), out Func&lt;int, bool&gt; predicate);
        /// RunWithPredicate(predicate);
        /// </code>
        /// </example>
        /// <typeparam name="T1">The type of the first parameter of the bound function.</typeparam>
        /// <typeparam name="TArg">The type of argument to pass to <paramref name="unboundFunction"/>.</typeparam>
        /// <typeparam name="TResult">The type of the return value of the function.</typeparam>
        /// <param name="unboundFunction">The unbound function delegate.</param>
        /// <param name="argument">The argument to pass to the unbound function delegate.</param>
        /// <param name="boundFunction">A delegate which calls <paramref name="unboundFunction"/> with the specified
        /// <paramref name="argument"/>.</param>
        /// <returns>A disposable <see cref="Releaser"/> which returns the object to the delegate pool.</returns>
        public static Releaser GetPooledFunction<T1, TArg, TResult>(Func<T1, TArg, TResult> unboundFunction, TArg argument, out Func<T1, TResult> boundFunction)
            => GetPooledDelegate<FuncWithBoundArgument<T1, TArg, TResult>, TArg, Func<T1, TArg, TResult>, Func<T1, TResult>>(unboundFunction, argument, out boundFunction);

        /// <summary>
        /// Gets a <see cref="Func{T1, T2, TResult}"/> delegate, which calls <paramref name="unboundFunction"/> with the
        /// specified <paramref name="argument"/>. The resulting <paramref name="boundFunction"/> may be called any
        /// number of times until the returned <see cref="Releaser"/> is disposed.
        /// </summary>
        /// <example>
        /// <para>The following example shows the use of a capturing delegate for a predicate that requires an
        /// argument:</para>
        ///
        /// <code>
        /// int x = 3;
        /// RunWithPredicate((a, b) => this.IsSomething(a, b, x));
        /// </code>
        ///
        /// <para>The following example shows the use of a pooled delegate to avoid capturing allocations for the same
        /// predicate:</para>
        ///
        /// <code>
        /// int x = 3;
        /// using var _ = GetPooledFunction((a, b, arg) => arg.self.IsSomething(a, b, arg.x), (self: this, x), out Func&lt;int, int, bool&gt; predicate);
        /// RunWithPredicate(predicate);
        /// </code>
        /// </example>
        /// <typeparam name="T1">The type of the first parameter of the bound function.</typeparam>
        /// <typeparam name="T2">The type of the second parameter of the bound function.</typeparam>
        /// <typeparam name="TArg">The type of argument to pass to <paramref name="unboundFunction"/>.</typeparam>
        /// <typeparam name="TResult">The type of the return value of the function.</typeparam>
        /// <param name="unboundFunction">The unbound function delegate.</param>
        /// <param name="argument">The argument to pass to the unbound function delegate.</param>
        /// <param name="boundFunction">A delegate which calls <paramref name="unboundFunction"/> with the specified
        /// <paramref name="argument"/>.</param>
        /// <returns>A disposable <see cref="Releaser"/> which returns the object to the delegate pool.</returns>
        public static Releaser GetPooledFunction<T1, T2, TArg, TResult>(Func<T1, T2, TArg, TResult> unboundFunction, TArg argument, out Func<T1, T2, TResult> boundFunction)
            => GetPooledDelegate<FuncWithBoundArgument<T1, T2, TArg, TResult>, TArg, Func<T1, T2, TArg, TResult>, Func<T1, T2, TResult>>(unboundFunction, argument, out boundFunction);

        /// <summary>
        /// Gets a <see cref="Func{T1, T2, T3, TResult}"/> delegate, which calls <paramref name="unboundFunction"/> with the
        /// specified <paramref name="argument"/>. The resulting <paramref name="boundFunction"/> may be called any
        /// number of times until the returned <see cref="Releaser"/> is disposed.
        /// </summary>
        /// <example>
        /// <para>The following example shows the use of a capturing delegate for a predicate that requires an
        /// argument:</para>
        ///
        /// <code>
        /// int x = 3;
        /// RunWithPredicate((a, b, c) => this.IsSomething(a, b, c, x));
        /// </code>
        ///
        /// <para>The following example shows the use of a pooled delegate to avoid capturing allocations for the same
        /// predicate:</para>
        ///
        /// <code>
        /// int x = 3;
        /// using var _ = GetPooledFunction((a, b, c, arg) => arg.self.IsSomething(a, b, c, arg.x), (self: this, x), out Func&lt;int, int, int, bool&gt; predicate);
        /// RunWithPredicate(predicate);
        /// </code>
        /// </example>
        /// <typeparam name="T1">The type of the first parameter of the bound function.</typeparam>
        /// <typeparam name="T2">The type of the second parameter of the bound function.</typeparam>
        /// <typeparam name="T3">The type of the third parameter of the bound function.</typeparam>
        /// <typeparam name="TArg">The type of argument to pass to <paramref name="unboundFunction"/>.</typeparam>
        /// <typeparam name="TResult">The type of the return value of the function.</typeparam>
        /// <param name="unboundFunction">The unbound function delegate.</param>
        /// <param name="argument">The argument to pass to the unbound function delegate.</param>
        /// <param name="boundFunction">A delegate which calls <paramref name="unboundFunction"/> with the specified
        /// <paramref name="argument"/>.</param>
        /// <returns>A disposable <see cref="Releaser"/> which returns the object to the delegate pool.</returns>
        public static Releaser GetPooledFunction<T1, T2, T3, TArg, TResult>(Func<T1, T2, T3, TArg, TResult> unboundFunction, TArg argument, out Func<T1, T2, T3, TResult> boundFunction)
            => GetPooledDelegate<FuncWithBoundArgument<T1, T2, T3, TArg, TResult>, TArg, Func<T1, T2, T3, TArg, TResult>, Func<T1, T2, T3, TResult>>(unboundFunction, argument, out boundFunction);

        /// <summary>
        /// A releaser for a pooled delegate.
        /// </summary>
        /// <remarks>
        /// <para>This type is intended for use as the resource of a <c>using</c> statement. When used in this manner,
        /// <see cref="Dispose"/> should not be called explicitly.</para>
        ///
        /// <para>If used without a <c>using</c> statement, calling <see cref="Dispose"/> is optional. If the call is
        /// omitted, the object will not be returned to the pool. The behavior of this type if <see cref="Dispose"/> is
        /// called multiple times is undefined.</para>
        /// </remarks>
        [NonCopyable]
        public readonly struct Releaser : IDisposable
        {
            private readonly Poolable _pooledObject;

            internal Releaser(Poolable pooledObject)
            {
                _pooledObject = pooledObject;
            }

            public void Dispose() => _pooledObject.ClearAndFree();
        }

        internal abstract class Poolable
        {
            public abstract void ClearAndFree();
        }

        private abstract class AbstractDelegateWithBoundArgument<TSelf, TArg, TUnboundDelegate, TBoundDelegate> : Poolable
            where TSelf : AbstractDelegateWithBoundArgument<TSelf, TArg, TUnboundDelegate, TBoundDelegate>, new()
            where TUnboundDelegate : Delegate
            where TBoundDelegate : Delegate
        {
            protected AbstractDelegateWithBoundArgument()
            {
                BoundDelegate = Bind();

                UnboundDelegate = null!;
                Argument = default!;
            }

            public TBoundDelegate BoundDelegate { get; }

            public TUnboundDelegate UnboundDelegate { get; private set; }
            public TArg Argument { get; private set; }

            public void Initialize(TUnboundDelegate unboundDelegate, TArg argument)
            {
                UnboundDelegate = unboundDelegate;
                Argument = argument;
            }

            public sealed override void ClearAndFree()
            {
                Argument = default!;
                UnboundDelegate = null!;
                DefaultDelegatePool<TSelf>.Instance.Free((TSelf)this);
            }

            protected abstract TBoundDelegate Bind();
        }

        private sealed class ActionWithBoundArgument<TArg>
            : AbstractDelegateWithBoundArgument<ActionWithBoundArgument<TArg>, TArg, Action<TArg>, Action>
        {
            protected override Action Bind()
                => () => UnboundDelegate(Argument);
        }

        private sealed class ActionWithBoundArgument<T1, TArg>
            : AbstractDelegateWithBoundArgument<ActionWithBoundArgument<T1, TArg>, TArg, Action<T1, TArg>, Action<T1>>
        {
            protected override Action<T1> Bind()
                => arg1 => UnboundDelegate(arg1, Argument);
        }

        private sealed class ActionWithBoundArgument<T1, T2, TArg>
            : AbstractDelegateWithBoundArgument<ActionWithBoundArgument<T1, T2, TArg>, TArg, Action<T1, T2, TArg>, Action<T1, T2>>
        {
            protected override Action<T1, T2> Bind()
                => (arg1, arg2) => UnboundDelegate(arg1, arg2, Argument);
        }

        private sealed class ActionWithBoundArgument<T1, T2, T3, TArg>
            : AbstractDelegateWithBoundArgument<ActionWithBoundArgument<T1, T2, T3, TArg>, TArg, Action<T1, T2, T3, TArg>, Action<T1, T2, T3>>
        {
            protected override Action<T1, T2, T3> Bind()
                => (arg1, arg2, arg3) => UnboundDelegate(arg1, arg2, arg3, Argument);
        }

        private sealed class FuncWithBoundArgument<TArg, TResult>
            : AbstractDelegateWithBoundArgument<FuncWithBoundArgument<TArg, TResult>, TArg, Func<TArg, TResult>, Func<TResult>>
        {
            protected override Func<TResult> Bind()
                => () => UnboundDelegate(Argument);
        }

        private sealed class CreateValueCallbackWithBoundArgument<TKey, TArg, TValue>
            : AbstractDelegateWithBoundArgument<
                CreateValueCallbackWithBoundArgument<TKey, TArg, TValue>,
                TArg,
                Func<TKey, TArg, TValue>,
                ConditionalWeakTable<TKey, TValue>.CreateValueCallback>
            where TKey : class
            where TValue : class
        {
            protected override ConditionalWeakTable<TKey, TValue>.CreateValueCallback Bind()
                => key => UnboundDelegate(key, Argument);
        }

        private sealed class FuncWithBoundArgument<T1, TArg, TResult>
            : AbstractDelegateWithBoundArgument<FuncWithBoundArgument<T1, TArg, TResult>, TArg, Func<T1, TArg, TResult>, Func<T1, TResult>>
        {
            protected override Func<T1, TResult> Bind()
                => arg1 => UnboundDelegate(arg1, Argument);
        }

        private sealed class FuncWithBoundArgument<T1, T2, TArg, TResult>
            : AbstractDelegateWithBoundArgument<FuncWithBoundArgument<T1, T2, TArg, TResult>, TArg, Func<T1, T2, TArg, TResult>, Func<T1, T2, TResult>>
        {
            protected override Func<T1, T2, TResult> Bind()
                => (arg1, arg2) => UnboundDelegate(arg1, arg2, Argument);
        }

        private sealed class FuncWithBoundArgument<T1, T2, T3, TArg, TResult>
            : AbstractDelegateWithBoundArgument<FuncWithBoundArgument<T1, T2, T3, TArg, TResult>, TArg, Func<T1, T2, T3, TArg, TResult>, Func<T1, T2, T3, TResult>>
        {
            protected override Func<T1, T2, T3, TResult> Bind()
                => (arg1, arg2, arg3) => UnboundDelegate(arg1, arg2, arg3, Argument);
        }

        [AttributeUsage(AttributeTargets.Struct)]
        private sealed class NonCopyableAttribute : Attribute
        {
        }
    }
}
