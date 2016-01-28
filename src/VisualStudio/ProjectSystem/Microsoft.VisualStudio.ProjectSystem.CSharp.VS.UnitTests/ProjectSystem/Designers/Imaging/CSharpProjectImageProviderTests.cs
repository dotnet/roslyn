// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Designers.Imaging
{
    [ProjectSystemTrait]
    public class CSharpProjectImageProviderTests
    {
        [Fact]
        public void Constructor_DoesNotThrow()
        {
            new CSharpProjectImageProvider();
        }

        [Fact]
        public void GetProjectImage_NullAsKey_ThrowsArgumentNull()
        {
            var provider = CreateInstance();

            Assert.Throws<ArgumentNullException>("key", () => {

                provider.GetProjectImage((string)null);
            });
        }


        [Fact]
        public void GetProjectImage_EmptyAsKey_ThrowsArgument()
        {
            var provider = CreateInstance();

            Assert.Throws<ArgumentException>("key", () => {

                provider.GetProjectImage(string.Empty);
            });
        }

        [Fact]
        public void GetProjectImage_UnrecognizedKeyAsKey_ReturnsNull()
        {
            var provider = CreateInstance();

            var result = provider.GetProjectImage("Unrecognized");

            Assert.Null(result);
        }

        [Theory]
        [InlineData(ProjectImageKey.ProjectRoot)]
        [InlineData(ProjectImageKey.AppDesignerFolder)]
        public void GetProjectImage_RecognizedKeyAsKey_ReturnsNonNull(string key)
        {
            var provider = CreateInstance();

            var result = provider.GetProjectImage(key);

            Assert.NotNull(result);
        }

        private static CSharpProjectImageProvider CreateInstance()
        {
            return new CSharpProjectImageProvider();
        }
    }
}
