// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Roslyn.Test.Utilities
{
    public static class ObjectReference
    {
        // We want to ensure this isn't inlined, because we need to ensure that any temporaries
        // on the stack in this method or targetFactory get cleaned up. Otherwise, they might still
        // be alive when we want to make later assertions.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ObjectReference<T> CreateFromFactory<T, TArg>(Func<TArg, T> targetFactory, TArg arg)
            where T : class
        {
            return new ObjectReference<T>(targetFactory(arg));
        }

        // We want to ensure this isn't inlined, because we need to ensure that any temporaries
        // on the stack in this method or targetFactory get cleaned up. Otherwise, they might still
        // be alive when we want to make later assertions.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ObjectReference<T> CreateFromFactory<T>(Func<T> targetFactory) where T : class
        {
            return new ObjectReference<T>(targetFactory());
        }

        public static ObjectReference<T> Create<T>(T target) where T : class
        {
            return new ObjectReference<T>(target);
        }
    }

    /// <summary>
    /// A wrapper to hold onto an object that you wish to make assertions about the lifetime of. This type has specific protections
    /// to ensure the best possible patterns to avoid "gotchas" with these sorts of tests.
    /// </summary>
    /// <remarks>
    /// Specifically, consider this common pattern:
    /// 
    /// <code>
    /// var weakReference = new WeakReference(strongReference);
    /// strongReference = null;
    /// GC.Collect(); // often a few times...
    /// Assert.Null(weakReference.Target);
    /// </code>
    /// 
    /// This code has a bug: it presumes that when strongReference = null is assigned, there are no other references anywhere.
    /// But that line only tells the JIT to null out the place that's holding the active value. The JIT could have spilled a copy
    /// at some point to the stack, which it now considers unused and isn't worth cleaning up. Or another register might still be
    /// holding it, etc.
    /// 
    /// What this class does is it holds the only active reference in the heap, and any use of that reference is put in a method
    /// that is marked NoInline; this ensures that when the uses are done, any temporaries still floating around are understood
    /// by the JIT/GC to actually be unused.
    /// </remarks>
    public sealed class ObjectReference<T> where T : class
    {
        private T _strongReference;

        /// <summary>
        /// Tracks if <see cref="GetReference"/> was called, which means it's no longer safe to do lifetime assertions.
        /// </summary>
        private bool _strongReferenceRetrievedOutsideScopedCall;

        private readonly WeakReference _weakReference;

        public ObjectReference(T target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            _strongReference = target;
            _weakReference = new WeakReference(target);
        }

        /// <summary>
        /// Asserts that the underlying object has been released.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void AssertReleased()
        {
            ReleaseAndGarbageCollect(expectReleased: true);

            Assert.False(_weakReference.IsAlive, "Reference should have been released but was not.");
        }

        /// <summary>
        /// Asserts that the underlying object is still being held.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void AssertHeld()
        {
            ReleaseAndGarbageCollect(expectReleased: false);

            // Since we are asserting it's still held, if it is held we can just recover our strong reference again
            _strongReference = (T)_weakReference.Target;
            Assert.True(_strongReference != null, "Reference should still be held.");
        }

        /// <summary>
        /// Releases the strong reference without making any assertions.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ReleaseStrongReference()
        {
            ReleaseAndGarbageCollect(expectReleased: false);
        }

        // Ensure the mention of the field doesn't result in any local temporaries being created in the parent
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ReleaseAndGarbageCollect(bool expectReleased)
        {
            if (_strongReferenceRetrievedOutsideScopedCall)
            {
                throw new InvalidOperationException($"The strong reference being held by the {nameof(ObjectReference<T>)} was retrieved via a call to {nameof(GetReference)}. Since the CLR might have cached a temporary somewhere in your stack, assertions can no longer be made about the correctness of lifetime.");
            }

            _strongReference = null;

            // The maximum number of iterations is determined by the expected outcome. If we expect the reference to be
            // released, we loop many more times to avoid flaky test failures. Otherwise, we loop a few times knowing
            // that the test will probably catch the failure on any given run. This strategy trades produces a few false
            // negatives in testing to gain a significant performance advantage for the majority case.
            var loopCount = expectReleased ? 1000 : 10;

            // We'll loop until the iteration count is reached, or until the weak reference disappears. When we're
            // trying to assert that the object is released, once the weak reference goes away, we know we're good. But
            // if we're trying to assert that the object is held, our only real option is to know to do it "enough"
            // times; but if it goes away then we are definitely done.
            for (var i = 0; i < loopCount && _weakReference.IsAlive; i++)
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
                GC.WaitForPendingFinalizers();
            }
        }

        /// <summary>
        /// Provides the underlying strong reference to the given action. This method is marked not be inlined, to ensure that no temporaries are left
        /// on the stack that might still root the strong reference. The caller must not "leak" the object out of the given action for any lifetime
        /// assertions to be safe.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void UseReference(Action<T> action)
        {
            action(GetReferenceWithChecks());
        }

        /// <summary>
        /// Provides the underlying strong reference to the given function. This method is marked not be inlined, to ensure that no temporaries are left
        /// on the stack that might still root the strong reference. The caller must not "leak" the object out of the given action for any lifetime
        /// assertions to be safe.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public U UseReference<U>(Func<T, U> function)
        {
            return function(GetReferenceWithChecks());
        }

        /// <summary>
        /// Provides the underlying strong reference to the given function, lets a function extract some value, and then returns a new ObjectReference.
        /// This method is marked not be inlined, to ensure that no temporaries are left on the stack that might still root the strong reference. The
        /// caller must not "leak" the object out of the given action for any lifetime assertions to be safe.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public ObjectReference<TResult> GetObjectReference<TResult>(Func<T, TResult> function) where TResult : class
        {
            var newValue = function(GetReferenceWithChecks());
            return ObjectReference.Create(newValue);
        }

        /// <summary>
        /// Provides the underlying strong reference to the given function, lets a function extract some value, and then returns a new ObjectReference.
        /// This method is marked not be inlined, to ensure that no temporaries are left on the stack that might still root the strong reference. The
        /// caller must not "leak" the object out of the given action for any lifetime assertions to be safe.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public ObjectReference<TResult> GetObjectReference<TResult, TArg>(Func<T, TArg, TResult> function, TArg argument) where TResult : class
        {
            var newValue = function(GetReferenceWithChecks(), argument);
            return ObjectReference.Create(newValue);
        }

        /// <summary>
        /// Fetches the object strongly being held from this. Because the value returned might be cached in a local temporary from
        /// the caller of this function, no further calls to <see cref="AssertHeld"/> or <see cref="AssertReleased"/> may be called
        /// on this object as the test is not valid either way. If you need to operate with the object without invalidating
        /// the ability to reference the object, see <see cref="UseReference"/>.
        /// </summary>
        public T GetReference()
        {
            _strongReferenceRetrievedOutsideScopedCall = true;
            return GetReferenceWithChecks();
        }

        private T GetReferenceWithChecks()
        {
            if (_strongReference == null)
            {
                throw new InvalidOperationException($"The type has already been released due to a call to {nameof(AssertReleased)}.");
            }

            return _strongReference;
        }
    }
}
