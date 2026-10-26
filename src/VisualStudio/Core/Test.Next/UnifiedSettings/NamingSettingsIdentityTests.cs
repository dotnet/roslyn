// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.VisualStudio.LanguageServices.Options;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings;

public sealed class NamingSettingsIdentityTests
{
    [Fact]
    public void EveryBuiltInHasAnInvariantId()
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var specification in NamingStylePreferences.Default.SymbolSpecifications)
        {
            Assert.True(NamingSettingsIdentity.TryGetBuiltInId(specification.ID, out var id));
            Assert.True(NamingSettingsIdentity.IsValidId(id!));
            Assert.True(ids.Add(id!));
        }

        ids.Clear();
        foreach (var style in NamingStylePreferences.Default.NamingStyles)
        {
            Assert.True(NamingSettingsIdentity.TryGetBuiltInId(style.ID, out var id));
            Assert.True(NamingSettingsIdentity.IsValidId(id!));
            Assert.True(ids.Add(id!));
        }
    }

    [Fact]
    public void BuiltInIdDoesNotDependOnDisplayName()
    {
        var legacyId = new Guid("23d856b4-5089-4405-83ce-749aada99153");
        Assert.Equal("interface", NamingSettingsIdentity.CreateId(legacyId, "Renamed interface", new HashSet<string>(StringComparer.Ordinal)));
        Assert.Equal("interface", NamingSettingsIdentity.CreateId(legacyId, "Other display name", new HashSet<string>(StringComparer.Ordinal)));
    }

    [Theory]
    [InlineData("Private fields", "private_fields")]
    [InlineData("Private--fields!", "private_fields")]
    [InlineData("123 fields", "item_123_fields")]
    [InlineData("", "item")]
    [InlineData("!!!", "item")]
    public void CustomIdsAreReadable(string name, string expected)
        => Assert.Equal(expected, NamingSettingsIdentity.CreateId(Guid.NewGuid(), name, new HashSet<string>(StringComparer.Ordinal)));

    [Fact]
    public void CollisionsReceiveReadableSuffixes()
    {
        var usedIds = new HashSet<string>(StringComparer.Ordinal) { "private_fields", "private_fields_2" };
        Assert.Equal("private_fields_3", NamingSettingsIdentity.CreateId(Guid.NewGuid(), "Private fields", usedIds));
    }

    [Fact]
    public void NormalizationIsCultureInvariant()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            Assert.Equal("interface", NamingSettingsIdentity.CreateId(Guid.NewGuid(), "INTERFACE", new HashSet<string>(StringComparer.Ordinal)));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Theory]
    [InlineData("private_fields", true)]
    [InlineData("private_fields_2", true)]
    [InlineData("Private fields", false)]
    [InlineData("1_field", false)]
    [InlineData("", false)]
    public void ValidateIds(string id, bool expected)
        => Assert.Equal(expected, NamingSettingsIdentity.IsValidId(id));
}
