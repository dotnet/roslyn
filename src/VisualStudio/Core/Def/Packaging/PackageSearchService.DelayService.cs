using System;

namespace Microsoft.VisualStudio.LanguageServices.Packaging
{
    internal partial class PackageSearchService
    {
        private class DelayService : IPackageSearchDelayService
        {
            public TimeSpan CachePollDelay { get; } = TimeSpan.FromMinutes(1);
            public TimeSpan FileWriteDelay { get; } = TimeSpan.FromSeconds(10);
            public TimeSpan UpdateFailedDelay { get; } = TimeSpan.FromMinutes(1);
            public TimeSpan UpdateSucceededDelay { get; } = TimeSpan.FromDays(1);
        }
    }
}
