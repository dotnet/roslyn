using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis.Host.Esent;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// A service that enables storing and retrieving of information associated with solutions,
    /// projects or documents across runtime sessions.
    /// </summary>
    internal partial class PersistentStorageService : IPersistentStorageService
    {
        private readonly IOptionService optionService;
        private readonly object lookupAccessLock = new object();
        private Dictionary<string, FileStorage> lookup;

        public PersistentStorageService(IOptionService optionService)
        {
            this.optionService = optionService;
            this.lookup = new Dictionary<string, FileStorage>();
        }

        public IPersistentStorage GetStorage(Solution solution)
        {
            if (solution.FilePath == null || !File.Exists(solution.FilePath))
            {
                return NoOpPersistentStorageInstance;
            }

            lock (lookupAccessLock)
            {
                var storage = lookup.GetOrAdd(solution.FilePath, path => new FileStorage(solution, optionService, Release));

                storage.AddRefUnsafe();

                return storage;
            }
        }

        private void Release(FileStorage storage)
        {
            lock (lookupAccessLock)
            {
                if (storage.ReleaseRefUnsafe())
                {
                    lookup.Remove(storage.Solution.FilePath);
                }
            }
        }
    }
}