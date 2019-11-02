// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.SolutionSize;
using Microsoft.CodeAnalysis.SQLite;
using Microsoft.CodeAnalysis.Storage;
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
        internal override AbstractPersistentStorageService GetStorageService(IPersistentStorageLocationService locationService, ISolutionSizeTracker solutionSizeTracker, IPersistentStorageFaultInjector faultInjector)
            => new SQLitePersistentStorageService(_persistentEnabledOptionService, locationService, solutionSizeTracker, faultInjector);

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

            using (var storage = GetStorage(solution, faultInjector))
            {
                // Because instantiating the connection will fail, we will not get back
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
