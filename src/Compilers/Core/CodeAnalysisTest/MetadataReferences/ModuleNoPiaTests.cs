// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class NoPia : TestBase
    {
        [Fact]
        public void ContainsNoPiaLocalTypes()
        {
            using (AssemblyMetadata piaMetadata = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.Pia1),
                                    metadata1 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.LocalTypes1),
                                    metadata2 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.LocalTypes2))
            {
                var pia1 = piaMetadata.GetAssembly().Modules[0];
                var localTypes1 = metadata1.GetAssembly().Modules[0];
                var localTypes2 = metadata2.GetAssembly().Modules[0];

                Assert.False(pia1.ContainsNoPiaLocalTypes());
                Assert.False(pia1.ContainsNoPiaLocalTypes());

                Assert.True(localTypes1.ContainsNoPiaLocalTypes());
                Assert.True(localTypes1.ContainsNoPiaLocalTypes());

                Assert.True(localTypes2.ContainsNoPiaLocalTypes());
                Assert.True(localTypes2.ContainsNoPiaLocalTypes());
            }
        }
    }
}
