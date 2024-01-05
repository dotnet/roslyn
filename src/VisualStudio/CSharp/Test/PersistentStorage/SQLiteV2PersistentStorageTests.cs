// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SQLite.v2;
using Microsoft.CodeAnalysis.Storage;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.WorkspaceServices
{
    /// <remarks>
    /// Tests are inherited from <see cref="AbstractPersistentStorageTests"/>.  That way we can
    /// write tests once and have them run against all <see cref="IPersistentStorageService"/>
    /// implementations.
    /// </remarks>
    public class SQLiteV2PersistentStorageTests : AbstractPersistentStorageTests
    {
        internal override AbstractPersistentStorageService GetStorageService(IMefHostExportProvider exportProvider, IPersistentStorageConfiguration configuration, IPersistentStorageFaultInjector? faultInjector, string relativePathBase)
            => new SQLitePersistentStorageService(
                exportProvider.GetExports<SQLiteConnectionPoolService>().Single().Value,
                configuration,
                exportProvider.GetExports<IAsynchronousOperationListenerProvider>().Single().Value.GetListener(FeatureAttribute.PersistentStorage),
                faultInjector);

        [Fact]
        public async Task TestCrashInNewConnection()
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

            // Because instantiating the connection will fail, we will not get back
            // a working persistent storage. We are testing a fault recovery code path.
            await using (var storage = await GetStorageAsync(solution, faultInjector: faultInjector, throwOnFailure: false))
            using (var memStream = new MemoryStream())
            using (var streamWriter = new StreamWriter(memStream))
            {
                streamWriter.WriteLine("contents");
                streamWriter.Flush();

                memStream.Position = 0;
                await storage.WriteStreamAsync("temp", memStream);
                var readStream = await storage.ReadStreamAsync("temp");

                // Because we don't have a real storage service, we should get back
                // null even when trying to read something we just wrote.
                Assert.Null(readStream);
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
            private readonly Action? _onNewConnection;
            private readonly Action<Exception>? _onFatalError;

            public PersistentStorageFaultInjector(
                Action? onNewConnection = null,
                Action<Exception>? onFatalError = null)
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
