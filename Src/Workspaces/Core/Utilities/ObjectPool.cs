namespace Roslyn.Utilities
{
    // nongeneric base class to hold nongeneric helper
    internal class ObjectPool
    {
        // this wrapper is needed to avoid typechecks when passing array elements to ref parameters.
        protected struct Element
        {
            // oddly, generic types regardless of constraints are noticeably more expensive than
            // object when comparing with null so we want this to be object and want the whole thing
            // to live outside of ObjectPool<T>
            internal object Value;
        }
    }
}