// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class ReferenceCountedDisposableTests
    {
        [Fact]
        [Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestArgumentValidation()
            => Assert.Throws<ArgumentNullException>("instance", () => new ReferenceCountedDisposable<IDisposable>(null));

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestSingleReferenceDispose(int disposeCount)
        {
            var target = new DisposableObject();

            var reference = new ReferenceCountedDisposable<DisposableObject>(target);
            Assert.Same(target, reference.Target);
            Assert.False(target.IsDisposed);
            Assert.Equal(0, target.DisposeCount);

            for (var i = 0; i < disposeCount; i++)
            {
                reference.Dispose();
            }

            Assert.Throws<ObjectDisposedException>(() => reference.Target);
            Assert.True(target.IsDisposed);
            Assert.Equal(1, target.DisposeCount);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestTryAddReferenceFailsAfterDispose()
        {
            var target = new DisposableObject();

            var reference = new ReferenceCountedDisposable<DisposableObject>(target);
            reference.Dispose();

            Assert.Null(reference.TryAddReference());
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestTryAddReferenceFailsAfterDispose2()
        {
            var target = new DisposableObject();

            var reference = new ReferenceCountedDisposable<DisposableObject>(target);

            // TryAddReference succeeds before dispose
            var reference2 = reference.TryAddReference();
            Assert.NotNull(reference2);

            reference.Dispose();

            // TryAddReference fails after dispose, even if another instance is alive
            Assert.Null(reference.TryAddReference());
            Assert.NotNull(reference2.Target);
            Assert.False(target.IsDisposed);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestOutOfOrderDispose()
        {
            var target = new DisposableObject();

            var reference = new ReferenceCountedDisposable<DisposableObject>(target);
            var reference2 = reference.TryAddReference();
            var reference3 = reference2.TryAddReference();

            reference2.Dispose();
            Assert.False(target.IsDisposed);

            reference3.Dispose();
            Assert.False(target.IsDisposed);

            reference.Dispose();
            Assert.True(target.IsDisposed);
            Assert.Equal(1, target.DisposeCount);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestWeakReferenceLifetime()
        {
            var target = new DisposableObject();

            var reference = new ReferenceCountedDisposable<DisposableObject>(target);
            var weakReference = new ReferenceCountedDisposable<DisposableObject>.WeakReference(reference);

            var reference2 = reference.TryAddReference();
            Assert.NotNull(reference2);

            reference.Dispose();

            // TryAddReference fails after dispose for a counted reference
            Assert.Null(reference.TryAddReference());
            Assert.NotNull(reference2.Target);
            Assert.False(target.IsDisposed);

            // However, a WeakReference created from the disposed reference can still add a reference
            var reference3 = weakReference.TryAddReference();
            Assert.NotNull(reference3);

            reference2.Dispose();
            Assert.False(target.IsDisposed);

            reference3.Dispose();
            Assert.True(target.IsDisposed);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestWeakReferenceArgumentValidation()
            => Assert.Throws<ArgumentNullException>("reference", () => new ReferenceCountedDisposable<IDisposable>.WeakReference(null));

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestDefaultWeakReference()
            => Assert.Null(default(ReferenceCountedDisposable<IDisposable>.WeakReference).TryAddReference());

        private sealed class DisposableObject : IDisposable
        {
            public bool IsDisposed
            {
                get;
                private set;
            }

            public int DisposeCount
            {
                get;
                private set;
            }

            public void Dispose()
            {
                IsDisposed = true;
                DisposeCount++;
            }
        }
    }
}
