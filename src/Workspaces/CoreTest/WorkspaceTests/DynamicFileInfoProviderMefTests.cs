// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [UseExportProvider]
    public class DynamicFileInfoProviderMefTests : TestBase
    {
        [Fact]
        public void TestFileExtensionsMetadata()
        {
            var lazy = GetDynamicFileInfoProvider();

            Assert.Equal(2, lazy.Metadata.Extensions.Count());
            AssertEx.SetEqual(new[] { "cshtml", "vbhtml" }, lazy.Metadata.Extensions);
        }

        [Fact]
        public void TestInvalidArgument1()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new ExportDynamicFileInfoProviderAttribute();
            });
        }

        [Fact]
        public void TestInvalidArgument2()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new FileExtensionsMetadata();
            });
        }

        #region Helpers
        internal static Lazy<IDynamicFileInfoProvider, FileExtensionsMetadata> GetDynamicFileInfoProvider()
        {
            var catalog = ExportProviderCache.CreateTypeCatalog(new Type[] { typeof(TestDynamicFileInfoProvider) });
            var factory = ExportProviderCache.GetOrCreateExportProviderFactory(catalog);

            return factory.CreateExportProvider().GetExport<IDynamicFileInfoProvider, FileExtensionsMetadata>();
        }

        [ExportDynamicFileInfoProvider("cshtml", "vbhtml")]
        [Shared]
        [PartNotDiscoverable]
        internal class TestDynamicFileInfoProvider : IDynamicFileInfoProvider
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public TestDynamicFileInfoProvider()
            {
            }

            public event EventHandler<string> Updated;

            public Task<DynamicFileInfo> GetDynamicFileInfoAsync(ProjectId projectId, string projectFilePath, string filePath, CancellationToken cancellationToken)
                => Task.FromResult<DynamicFileInfo>(null);

            public Task RemoveDynamicFileInfoAsync(ProjectId projectId, string projectFilePath, string filePath, CancellationToken cancellationToken)
                => Task.CompletedTask;

            private void OnUpdate()
                => Updated?.Invoke(this, "test");
        }
        #endregion
    }
}
