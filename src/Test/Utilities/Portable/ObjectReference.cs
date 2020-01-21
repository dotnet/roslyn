// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            ReleaseAndGarbageCollect();

            Assert.False(_weakReference.IsAlive, "Reference should have been released but was not.");
        }

        /// <summary>
        /// Asserts that the underlying object is still being held.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void AssertHeld()
        {
            ReleaseAndGarbageCollect();

            // Since we are asserting it's still held, if it is held we can just recover our strong reference again
            _strongReference = (T)_weakReference.Target;
            Assert.True(_strongReference != null, "Reference should still be held.");
        }

        // Ensure the mention of the field doesn't result in any local temporaries being created in the parent
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ReleaseAndGarbageCollect()
        {
            if (_strongReferenceRetrievedOutsideScopedCall)
            {
                throw new InvalidOperationException($"The strong reference being held by the {nameof(ObjectReference<T>)} was retrieved via a call to {nameof(GetReference)}. Since the CLR might have cached a temporary somewhere in your stack, assertions can no longer be made about the correctness of lifetime.");
            }

            _strongReference = null;

            // We'll loop 1000 times, or until the weak reference disappears. When we're trying to assert that the
            // object is released, once the weak reference goes away, we know we're good. But if we're trying to assert
            // that the object is held, our only real option is to know to do it "enough" times; but if it goes away then
            // we are definitely done.
            for (var i = 0; i < 1000 && _weakReference.IsAlive; i++)
            {
                GC.Collect();
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
