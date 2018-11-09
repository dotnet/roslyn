// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.UtilityTest
{
    public class AsyncLazyInitializerTests
    {
        private object _value;

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AsyncLazy)]
        public async Task EnsureInitialized()
        {
            var expected = new object();

            _value = null;
            var initializedValue = await AsyncLazyInitializer.EnsureInitializedAsync(
                () => ref _value,
                async () =>
                {
                    await Task.Yield();
                    return expected;
                });
            Assert.Same(expected, _value);
            Assert.Same(expected, initializedValue);

            _value = null;
            initializedValue = await AsyncLazyInitializer.EnsureInitializedAsync(
                state => ref state._value,
                async _ =>
                {
                    await Task.Yield();
                    return expected;
                },
                this);
            Assert.Same(expected, _value);
            Assert.Same(expected, initializedValue);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AsyncLazy)]
        public async Task EnsureInitializedTakesFirstCompleted()
        {
            var expected = new object();
            var notExpected = new object();

            var firstInValueFactory = new TaskCompletionSource<object>();
            var secondCompleted = new TaskCompletionSource<object>();

            var firstInitializedValueTask = AsyncLazyInitializer.EnsureInitializedAsync(
                () => ref _value,
                async () =>
                {
                    firstInValueFactory.SetResult(null);
                    await secondCompleted.Task;
                    return notExpected;
                });
            var secondInitializedValueTask = AsyncLazyInitializer.EnsureInitializedAsync(
                () => ref _value,
                async () =>
                {
                    await firstInValueFactory.Task;
                    return expected;
                });

            var secondInitializedValue = await secondInitializedValueTask;
            secondCompleted.SetResult(null);

            var firstInitializedValue = await firstInitializedValueTask;

            Assert.Same(expected, _value);
            Assert.Same(expected, firstInitializedValue);
            Assert.Same(expected, secondInitializedValue);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AsyncLazy)]
        public async Task EnsureInitializedWithStateTakesFirstCompleted()
        {
            var expected = new object();
            var notExpected = new object();

            var firstInValueFactory = new TaskCompletionSource<object>();
            var secondCompleted = new TaskCompletionSource<object>();

            var firstInitializedValueTask = AsyncLazyInitializer.EnsureInitializedAsync(
                state => ref state._value,
                async _ =>
                {
                    firstInValueFactory.SetResult(null);
                    await secondCompleted.Task;
                    return notExpected;
                },
                this);
            var secondInitializedValueTask = AsyncLazyInitializer.EnsureInitializedAsync(
                state => ref state._value,
                async _ =>
                {
                    await firstInValueFactory.Task;
                    return expected;
                },
                this);

            var secondInitializedValue = await secondInitializedValueTask;
            secondCompleted.SetResult(null);

            var firstInitializedValue = await firstInitializedValueTask;

            Assert.Same(expected, _value);
            Assert.Same(expected, firstInitializedValue);
            Assert.Same(expected, secondInitializedValue);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AsyncLazy)]
        public async Task ValueFactoryNotUsedIfAlreadyInitialized()
        {
            _value = new object();
            var expected = _value;

            var initializedValue = await AsyncLazyInitializer.EnsureInitializedAsync(() => ref _value, null);
            Assert.Same(expected, _value);
            Assert.Same(expected, initializedValue);

            initializedValue = await AsyncLazyInitializer.EnsureInitializedAsync(state => ref state._value, null, this);
            Assert.Same(expected, _value);
            Assert.Same(expected, initializedValue);
        }

        /// <summary>
        /// Verifies that an exception thrown synchronously by <c>targetAccessor</c> is directly rethrown by
        /// <see cref="AsyncLazyInitializer"/>, as opposed to being captured in the <see cref="ValueTask{TResult}"/> it
        /// returns.
        /// </summary>
        [Fact]
        [Trait(Traits.Feature, Traits.Features.AsyncLazy)]
        public void TargetAccessorExceptionThrownSynchronously()
        {
            var expected = new FormatException();

            // These intentionally used `Assert.Throws` instead of `Assert.ThrowsAsync`
            Assert.Same(expected, Assert.Throws<FormatException>(() => AsyncLazyInitializer.EnsureInitializedAsync(() => throw expected, () => new ValueTask<object>(new object()))));
            Assert.Same(expected, Assert.Throws<FormatException>(() => AsyncLazyInitializer.EnsureInitializedAsync(_ => throw expected, _ => new ValueTask<object>(new object()), this)));

            Assert.Null(_value);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AsyncLazy)]
        public async Task TargetAccessorExceptionForNull()
        {
            Assert.Null(_value);

            await Assert.ThrowsAsync<NullReferenceException>(async () => await AsyncLazyInitializer.EnsureInitializedAsync(null, () => new ValueTask<object>(new object())));
            await Assert.ThrowsAsync<NullReferenceException>(async () => await AsyncLazyInitializer.EnsureInitializedAsync(null, _ => new ValueTask<object>(new object()), this));

            Assert.Null(_value);
        }

        /// <summary>
        /// Verifies that an exception thrown synchronously by <c>valueFactory</c> is captured by the
        /// <see cref="ValueTask{TResult}"/> returned from <see cref="AsyncLazyInitializer"/>.
        /// </summary>
        [Fact]
        [Trait(Traits.Feature, Traits.Features.AsyncLazy)]
        public async Task ValueFactoryExceptionThrownAsynchronously()
        {
            var expected = new FormatException();

            var task = AsyncLazyInitializer.EnsureInitializedAsync(() => ref _value, () => throw expected);
            Assert.True(task.IsCompleted);
            Assert.True(task.IsFaulted);
            Assert.Same(expected, await Assert.ThrowsAsync<FormatException>(async () => await task));

            task = AsyncLazyInitializer.EnsureInitializedAsync(state => ref state._value, _ => throw expected, this);
            Assert.True(task.IsCompleted);
            Assert.True(task.IsFaulted);
            Assert.Same(expected, await Assert.ThrowsAsync<FormatException>(async () => await task));

            Assert.Null(_value);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AsyncLazy)]
        public async Task ValueFactoryExceptionForNull()
        {
            Assert.Null(_value);

            await Assert.ThrowsAsync<NullReferenceException>(async () => await AsyncLazyInitializer.EnsureInitializedAsync(() => ref _value, null));
            await Assert.ThrowsAsync<NullReferenceException>(async () => await AsyncLazyInitializer.EnsureInitializedAsync(state => ref state._value, null, this));

            Assert.Null(_value);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.AsyncLazy)]
        public async Task ValueFactoryExceptionForNullValue()
        {
            Assert.Null(_value);

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await AsyncLazyInitializer.EnsureInitializedAsync(() => ref _value, () => new ValueTask<object>(default(object))));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await AsyncLazyInitializer.EnsureInitializedAsync(state => ref state._value, _ => new ValueTask<object>(default(object)), this));

            Assert.Null(_value);
        }
    }
}
