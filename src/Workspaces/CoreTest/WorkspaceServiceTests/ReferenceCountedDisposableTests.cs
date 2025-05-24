// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Reflection;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

[Trait(Traits.Feature, Traits.Features.Workspace)]
public sealed class ReferenceCountedDisposableTests
{
    [Fact]
    public void TestArgumentValidation()
        => Assert.Throws<ArgumentNullException>("instance", () => new ReferenceCountedDisposable<IDisposable>(null));

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
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
    public void TestTryAddReferenceFailsAfterDispose()
    {
        var target = new DisposableObject();

        var reference = new ReferenceCountedDisposable<DisposableObject>(target);
        reference.Dispose();

        Assert.Null(reference.TryAddReference());
    }

    [Fact]
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
    public void TestWeakReferenceArgumentValidation()
        => Assert.Throws<ArgumentNullException>("reference", () => new ReferenceCountedDisposable<IDisposable>.WeakReference(null));

    [Fact]
    public void TestDefaultWeakReference()
        => Assert.Null(default(ReferenceCountedDisposable<IDisposable>.WeakReference).TryAddReference());

    /// <summary>
    /// This test verifies that a weak reference cannot be created from a disposed reference, even if another strong
    /// reference to the same object is still alive. It specifically covers the case where a weak reference HAS NOT
    /// been created prior to the assertion.
    /// </summary>
    [Fact]
    public void TestWeakReferenceCannotBeCreatedFromDisposedReference_NoPriorWeakReference()
    {
        var target = new DisposableObject();
        var reference = new ReferenceCountedDisposable<DisposableObject>(target);

        var secondReference = reference.TryAddReference();
        Assert.NotNull(secondReference);

        reference.Dispose();

        var weakReference = new ReferenceCountedDisposable<DisposableObject>.WeakReference(reference);
        Assert.Null(weakReference.TryAddReference());
    }

    /// <summary>
    /// This test verifies that a weak reference cannot be created from a disposed reference, even if another strong
    /// reference to the same object is still alive. It specifically covers the case where a weak reference HAS been
    /// created prior to the assertion.
    /// </summary>
    [Fact]
    public void TestWeakReferenceCannotBeCreatedFromDisposedReference_WithPriorWeakReference()
    {
        var target = new DisposableObject();
        var reference = new ReferenceCountedDisposable<DisposableObject>(target);

        // Create an initial weak reference at a point where the reference is alive. This ensures the internal
        // shared WeakReference<T> is initialized.
        var weakReference = new ReferenceCountedDisposable<DisposableObject>.WeakReference(reference);
        Assert.NotNull(weakReference.TryAddReference());

        var secondReference = reference.TryAddReference();
        Assert.NotNull(secondReference);

        reference.Dispose();

        var secondWeakReference = new ReferenceCountedDisposable<DisposableObject>.WeakReference(reference);
        Assert.Null(secondWeakReference.TryAddReference());
    }

    [Fact]
    public void TestWeakReferenceCannotTear()
    {
        // WeakReference contains a single field which is a reference type, so reads/writes cannot tear
        var field = Assert.Single(typeof(ReferenceCountedDisposable<>.WeakReference)
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));

        Assert.True(field.FieldType.IsClass);
    }

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
