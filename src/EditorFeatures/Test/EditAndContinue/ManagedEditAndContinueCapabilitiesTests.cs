// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    public class ManagedEditAndContinueCapabilitiesTests
    {
        [Fact]
        public void Parse()
        {
            var capabilities = "Baseline";

            var service = new ManagedEditAndContinueCapabilities(capabilities);

            Assert.True(service.HasCapability(ManagedEditAndContinueCapability.Baseline));
            Assert.False(service.HasCapability(ManagedEditAndContinueCapability.RuntimeEdits));
        }

        [Fact]
        public void Parse_CaseSensitive()
        {
            var capabilities = "BaseLine";

            var service = new ManagedEditAndContinueCapabilities(capabilities);

            Assert.False(service.HasCapability(ManagedEditAndContinueCapability.Baseline));
        }

        [Fact]
        public void Parse_IgnoreInvalid()
        {
            var capabilities = "Baseline Invalid RuntimeEdits";

            var service = new ManagedEditAndContinueCapabilities(capabilities);

            Assert.True(service.HasCapability(ManagedEditAndContinueCapability.Baseline));
            Assert.True(service.HasCapability(ManagedEditAndContinueCapability.RuntimeEdits));
        }

        [Fact]
        public void Parse_IgnoreInvalidNumeric()
        {
            var capabilities = "Baseline 90 RuntimeEdits";

            var service = new ManagedEditAndContinueCapabilities(capabilities);

            Assert.True(service.HasCapability(ManagedEditAndContinueCapability.Baseline));
            Assert.True(service.HasCapability(ManagedEditAndContinueCapability.RuntimeEdits));
        }

        [Fact]
        public void Parse_MultipleSpaces()
        {
            var capabilities = "  Baseline      RuntimeEdits   ";

            var service = new ManagedEditAndContinueCapabilities(capabilities);

            Assert.True(service.HasCapability(ManagedEditAndContinueCapability.Baseline));
            Assert.True(service.HasCapability(ManagedEditAndContinueCapability.RuntimeEdits));
        }

        [Fact]
        public void HasCapability_IgnoreInvalid()
        {
            var capabilities = "Baseline";

            var service = new ManagedEditAndContinueCapabilities(capabilities);

            Assert.False(service.HasCapability((ManagedEditAndContinueCapability)999));
        }
    }
}
