// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class AssemblyIdentityTests : AssemblyIdentityTestBase
    {
        [Fact]
        public void FullKeyAndToken()
        {
            string displayPkt = "Foo, Version=1.0.0.0, Culture=neutral, PublicKeyToken=" + StrPublicKeyToken1;
            string displayPk = "Foo, Version=1.0.0.0, Culture=neutral, PublicKey=" + StrPublicKey1;

            bool equivalent;
            FusionAssemblyIdentityComparer.AssemblyComparisonResult result;
            int hr = FusionAssemblyIdentityComparer.DefaultModelCompareAssemblyIdentity(displayPkt, false, displayPk, false, out equivalent, out result, IntPtr.Zero);

            Assert.Equal(0, hr);
            Assert.True(equivalent, "Expected equivalent");
            Assert.Equal(FusionAssemblyIdentityComparer.AssemblyComparisonResult.EquivalentFullMatch, result);

            hr = FusionAssemblyIdentityComparer.DefaultModelCompareAssemblyIdentity(displayPk, false, displayPkt, false, out equivalent, out result, IntPtr.Zero);

            Assert.Equal(0, hr);
            Assert.True(equivalent, "Expected equivalent");
            Assert.Equal(FusionAssemblyIdentityComparer.AssemblyComparisonResult.EquivalentFullMatch, result);
        }
    }
}
