using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A value source abstracts the source of a value that be accessed either synchronously or
    /// asynchronously. The value may be constant, computed once and stored or computed each time it
    /// is accessed. Stored values might be held as strong references or weak references.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal interface IValueSource<T>
    {
        /// <summary>
        /// Gets the value if it is already computed and/or the reference is still available.
        /// </summary>
        bool TryGetValue(out T value);

        /// <summary>
        /// True if the value is already computed and the reference is still available.
        /// </summary>
        bool HasValue { get; }

        /// <summary>
        /// Get the value, computing it if necessary.
        /// </summary>
        T GetValue(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets the value asynchronously, computing it if necessary.
        /// </summary>
        Task<T> GetValueAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}