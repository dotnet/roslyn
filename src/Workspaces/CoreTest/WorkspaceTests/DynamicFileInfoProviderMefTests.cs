// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
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

        internal static Lazy<IDynamicFileInfoProvider, FileExtensionsMetadata> GetDynamicFileInfoProvider()
        {
            var composition = TestComposition.Empty.AddParts(typeof(TestDynamicFileInfoProviderThatProducesNoFiles));
            return composition.ExportProviderFactory.CreateExportProvider().GetExport<IDynamicFileInfoProvider, FileExtensionsMetadata>();
        }
    }
}
