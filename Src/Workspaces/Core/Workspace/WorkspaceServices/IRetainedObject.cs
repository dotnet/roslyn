namespace Roslyn.Services.Host
{
    /// <summary>
    /// Optionally implemented on objects that are intended to be used with a retainer.
    /// </summary>
    public interface IRetainedObject
    {
        /// <summary>
        /// Called when the retainer evicts the object.
        /// </summary>
        void OnEvicted();
    }
}