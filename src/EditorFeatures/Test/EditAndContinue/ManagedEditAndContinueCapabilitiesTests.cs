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

            var service = EditAndContinueWorkspaceService.ParseCapabilities(capabilities);

            Assert.True(service.HasFlag(ManagedEditAndContinueCapability.Baseline));
            Assert.False(service.HasFlag(ManagedEditAndContinueCapability.RuntimeEdits));
        }

        [Fact]
        public void Parse_CaseSensitive()
        {
            var capabilities = "BaseLine";

            var service = EditAndContinueWorkspaceService.ParseCapabilities(capabilities);

            Assert.False(service.HasFlag(ManagedEditAndContinueCapability.Baseline));
        }

        [Fact]
        public void Parse_IgnoreInvalid()
        {
            var capabilities = "Baseline Invalid RuntimeEdits";

            var service = EditAndContinueWorkspaceService.ParseCapabilities(capabilities);

            Assert.True(service.HasFlag(ManagedEditAndContinueCapability.Baseline));
            Assert.True(service.HasFlag(ManagedEditAndContinueCapability.RuntimeEdits));
        }

        [Fact]
        public void Parse_IgnoreInvalidNumeric()
        {
            var capabilities = "Baseline 90 RuntimeEdits";

            var service = EditAndContinueWorkspaceService.ParseCapabilities(capabilities);

            Assert.True(service.HasFlag(ManagedEditAndContinueCapability.Baseline));
            Assert.True(service.HasFlag(ManagedEditAndContinueCapability.RuntimeEdits));
        }

        [Fact]
        public void Parse_MultipleSpaces()
        {
            var capabilities = "  Baseline      RuntimeEdits   ";

            var service = EditAndContinueWorkspaceService.ParseCapabilities(capabilities);

            Assert.True(service.HasFlag(ManagedEditAndContinueCapability.Baseline));
            Assert.True(service.HasFlag(ManagedEditAndContinueCapability.RuntimeEdits));
        }

        [Fact]
        public void HasFlag_IgnoreInvalid()
        {
            var capabilities = "Baseline";

            var service = EditAndContinueWorkspaceService.ParseCapabilities(capabilities);

            Assert.False(service.HasFlag((ManagedEditAndContinueCapability)999));
        }
    }
}
