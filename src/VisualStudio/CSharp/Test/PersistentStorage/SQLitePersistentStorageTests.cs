// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.SQLite;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.WorkspaceServices
{
    /// <remarks>
    /// Tests are inherited from <see cref="AbstractPersistentStorageTests"/>.  That way we can
    /// write tests once and have them run against all <see cref="IPersistentStorageService"/>
    /// implementations.
    /// </remarks>
    public class SQLitePersistentStorageTests : AbstractPersistentStorageTests
    {
        internal override IPersistentStorageService GetStorageService(IPersistentStorageFaultInjector faultInjector)
            => new SQLitePersistentStorageService(_persistentEnabledOptionService, faultInjector);

        [Fact]
        public async Task TestNullFilePaths()
        {
            var solution = CreateOrOpenSolution(nullPaths: true);

            var streamName = "stream";

            using (var storage = GetStorage(solution))
            {
                var project = solution.Projects.First();
                var document = project.Documents.First();
                Assert.False(await storage.WriteStreamAsync(project, streamName, EncodeString("")));
                Assert.False(await storage.WriteStreamAsync(document, streamName, EncodeString("")));

                Assert.Null(await storage.ReadStreamAsync(project, streamName));
                Assert.Null(await storage.ReadStreamAsync(document, streamName));
            }
        }

        [Fact]
        public void TestCrashInNewConnection()
        {
            var solution = CreateOrOpenSolution(nullPaths: true);

            var hitInjector = false;
            var faultInjector = new PersistentStorageFaultInjector(
                onNewConnection: () =>
                {
                    hitInjector = true;
                    throw new Exception();
                },
                onFatalError: e => throw e);

            using (var storage = GetStorageService(faultInjector).GetStorage(solution))
            {
                // Because instantiating hte connection will fail, we will not get back
                // a working persistent storage.
                Assert.IsType<NoOpPersistentStorage>(storage);
            }

            Assert.True(hitInjector);

            // Ensure we don't get a crash due to SqlConnection's finalizer running.
            for (var i = 0; i < 10; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        private class PersistentStorageFaultInjector : IPersistentStorageFaultInjector
        {
            private readonly Action _onNewConnection;
            private readonly Action<Exception> _onFatalError;

            public PersistentStorageFaultInjector(
                Action onNewConnection = null,
                Action<Exception> onFatalError = null)
            {
                _onNewConnection = onNewConnection;
                _onFatalError = onFatalError;
            }

            public void OnNewConnection()
                => _onNewConnection?.Invoke();

            public void OnFatalError(Exception ex)
                => _onFatalError?.Invoke(ex);
        }
    }
}
