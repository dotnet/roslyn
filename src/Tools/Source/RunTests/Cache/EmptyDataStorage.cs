using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RunTests.Cache
{
    internal sealed class EmptyDataStorage : IDataStorage
    {
        internal static readonly EmptyDataStorage Instance = new EmptyDataStorage();

        public string Name => "none";

        public Task AddCachedTestResult(string assemblyName, ContentFile conentFile, CachedTestResult testResult)
        {
            var source = new TaskCompletionSource<bool>();
            source.SetResult(true);
            return source.Task;
        }

        public Task<CachedTestResult?> TryGetCachedTestResult(string checksum)
        {
            return Task.FromResult<CachedTestResult?>(null);
        }
    }
}
