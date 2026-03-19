// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Xunit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests;

public sealed class EditAndContinueCapabilitiesTests
{
    [Fact]
    public void ParseCapabilities()
    {
        var capabilities = ImmutableArray.Create("Baseline");

        var service = EditAndContinueCapabilitiesParser.Parse(capabilities);

        Assert.True(service.HasFlag(EditAndContinueCapabilities.Baseline));
        Assert.False(service.HasFlag(EditAndContinueCapabilities.NewTypeDefinition));
    }

    [Fact]
    public void ParseCapabilities_CaseSensitive()
    {
        var capabilities = ImmutableArray.Create("BaseLine");

        var service = EditAndContinueCapabilitiesParser.Parse(capabilities);

        Assert.False(service.HasFlag(EditAndContinueCapabilities.Baseline));
    }

    [Fact]
    public void ParseCapabilities_IgnoreInvalid()
    {
        var capabilities = ImmutableArray.Create("Baseline", "Invalid", "NewTypeDefinition");

        var service = EditAndContinueCapabilitiesParser.Parse(capabilities);

        Assert.True(service.HasFlag(EditAndContinueCapabilities.Baseline));
        Assert.True(service.HasFlag(EditAndContinueCapabilities.NewTypeDefinition));
    }

    [Fact]
    public void ParseCapabilities_IgnoreInvalidNumeric()
    {
        var capabilities = ImmutableArray.Create("Baseline", "90", "NewTypeDefinition");

        var service = EditAndContinueCapabilitiesParser.Parse(capabilities);

        Assert.True(service.HasFlag(EditAndContinueCapabilities.Baseline));
        Assert.True(service.HasFlag(EditAndContinueCapabilities.NewTypeDefinition));
    }

    [Fact]
    public void ParseCapabilities_AllCapabilitiesParsed()
    {
        foreach (var name in Enum.GetNames(typeof(EditAndContinueCapabilities)))
        {
            var capabilities = ImmutableArray.Create(name);

            var service = EditAndContinueCapabilitiesParser.Parse(capabilities);

            var flag = (EditAndContinueCapabilities)Enum.Parse(typeof(EditAndContinueCapabilities), name);
            Assert.True(service.HasFlag(flag), $"Capability '{name}' was not parsed correctly, so it's impossible for a runtime to enable it!");
        }
    }
}
