using System;

namespace Roslyn.Utilities
{
#if !COMPILERCORE
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    // Mimics Dev11 implementation, except Dev11 doesn't derive from WeakReference, so we shouldn't use IsAlive and Target properties:
    internal class WeakReference<T> : WeakReference
        where T : class
    {
        /// <summary>
        /// Create a weak reference to an object.
        /// </summary>
        public WeakReference(T target)
            : base(target, trackResurrection: false)
        {
        }

        [Obsolete("This API is not available in Dev11, use TryGetTaget method, or IsNull extension instead", error: true)]
        public new bool IsAlive { get { throw Contract.Unreachable; } }

        [Obsolete("This API is not available in Dev11, use TryGetTaget method, or GetTarget extension instead", error: true)]
        public new object Target { get { throw Contract.Unreachable; } set { throw Contract.Unreachable; } }

        public bool TryGetTarget(out T target)
        {
            object obj = base.Target;
            if (obj != null)
            {
                target = (T)obj;
                return true;
            }
            else
            {
                target = null;
                return false;
            }
        }
    }
}