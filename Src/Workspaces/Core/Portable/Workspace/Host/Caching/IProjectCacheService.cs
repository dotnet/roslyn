using System;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// Service used to enable recoverable object caches for a given <see cref="ProjectId"/>
    /// </summary>
    internal interface IProjectCacheService : IWorkspaceService
    {
        IDisposable EnableCaching(ProjectId key);
    }

    /// <summary>
    /// Caches recoverable objects
    /// 
    /// Compilations are put into a conditional weak table.
    /// 
    /// Recoverable SyntaxTrees implement <see cref="ICachedObjectOwner"/> since they are numerous
    /// and putting them into a conditional weak table greatly increases GC costs in
    /// clr.dll!PromoteDependentHandle.
    /// </summary>
    internal interface IProjectCacheHostService : IProjectCacheService
    {
        /// <summary>
        /// If caching is enabled for <see cref="ProjectId"/> key, the instance is added to 
        /// a condtional weak table.  
        /// 
        /// It will not be collected until either caching is disabled for the project
        /// or the owner object is collected.
        /// </summary>
        /// <returns>The instance passed in is always returned</returns>
        T CreateStrongReference<T>(ProjectId key, object owner, T instance) where T : class;

        /// <summary>
        /// If caching is enabled for <see cref="ProjectId"/> key, <see cref="ICachedObjectOwner.CachedObject"/>
        /// will be set to instance.
        /// </summary>
        /// <returns>The instance passed in is always returned</returns>
        T CacheObject<T>(ProjectId key, ICachedObjectOwner owner, T instance) where T : class;
    }
}