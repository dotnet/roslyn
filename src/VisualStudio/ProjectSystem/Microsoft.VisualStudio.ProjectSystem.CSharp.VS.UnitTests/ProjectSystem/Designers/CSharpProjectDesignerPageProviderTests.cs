// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Designers
{
    [UnitTestTrait]
    public class CSharpProjectDesignerPageProviderTests
    {
        [Fact]
        public void Constructor_DoesNotThrow()
        {
            new CSharpProjectDesignerPageProvider();
        }

        [Fact]
        public async Task GetPagesAsync_ReturnsPagesInOrder()
        {
            var provider = CreateInstance();

            var pages = await provider.GetPagesAsync();

            Assert.Equal(pages.Count(), 1);
            Assert.Same(pages.ElementAt(0), CSharpProjectDesignerPage.Application);            
        }

        private static CSharpProjectDesignerPageProvider CreateInstance()
        {
            return new CSharpProjectDesignerPageProvider();
        }
    }
}
