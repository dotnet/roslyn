// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Test.Utilities;
using System;
using System.Collections.Concurrent;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class EventListenerGuard
    {
        /// <summary>
        /// A unit test that guards against the EventListener race condition:
        ///
        ///     - https://github.com/dotnet/roslyn/issues/8936
        ///     - https://github.com/dotnet/corefx/issues/3793
        ///     
        /// The underlying issue here is EventListener.DisposeOnShutdown has a race
        /// condition if a new EventSource is added during an AppDomain or Process 
        /// exit.  When this occurs there is an unhandled exception during shutdown
        /// due to a modified collection during enumeration that causes xunit to 
        /// falsely fail the run. 
        /// 
        /// The type CDSCollectionETWBCLProvider triggers this bug in our tests.  It
        /// is an EventSource for concurrent collections that is loaded on many 
        /// concurrent collection operations.  These operations are not triggered 
        /// directly in some of our tests and hence lead to the race.  
        /// 
        /// This test guards against them force loading, albeit indirectly, the EventSource 
        /// instance of CDSCollectionETWBCLProvider.  Hence uses during shutdown 
        /// are just re-using this instance and don't trigger the race.
        /// </summary>
        [WorkItem(8936, "https://github.com/dotnet/roslyn/issues/8936")]
        [Fact]
        public void GuardAgainstRace()
        {
            // This code will trigger the load of CDSCollectionETWBCLProvider
            var dictionary = new ConcurrentDictionary<int, int>();
            dictionary.Clear();

            var log = typeof(ConcurrentDictionary<int, int>)
                .Assembly
                .GetType("System.Collections.Concurrent.CDSCollectionETWBCLProvider")
                .GetField("Log", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .GetValue(null);
            Assert.NotNull(log);
        }
    }
}
