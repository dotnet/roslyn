// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Microsoft.VisualStudio.Settings;
using Moq;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings;

public sealed class NamingSettingsLegacyTests
{
    private const string Key = "Naming";
    private const string FallbackKey = "OldNaming";

    [Fact]
    public void OnlyAbsentLegacyValuesUseDefaults()
    {
        var manager = CreateManager(GetValueResult.Missing, "");
        var xml = "";
        manager.Setup(service => service.TryGetValue(FallbackKey, out xml)).Returns(GetValueResult.Missing);
        Assert.Equal(NamingStylePreferences.Default, NamingSettingsService.ReadLegacyPreferences(manager.Object, Key, FallbackKey));
    }

    [Theory]
    [InlineData(GetValueResult.Corrupt)]
    [InlineData(GetValueResult.IncompatibleType)]
    [InlineData(GetValueResult.ObsoleteFormat)]
    [InlineData(GetValueResult.UnknownError)]
    public void FailedLegacyReadsDoNotBecomeSuccessfulDefaults(GetValueResult result)
    {
        var manager = CreateManager(result, "");
        Assert.Throws<InvalidDataException>(() => NamingSettingsService.ReadLegacyPreferences(manager.Object, Key, FallbackKey));
    }

    [Fact]
    public void ExistingProfileXmlPreservesCustomizations()
    {
        var element = NamingStylePreferences.Default.CreateXElement();
        element.Element("NamingStyles")!.Element("NamingStyle")!.SetAttributeValue("Name", "Custom style");
        var xml = element.ToString();
        var manager = CreateManager(GetValueResult.Success, xml);
        Assert.Equal(NamingStylePreferences.FromXElement(XElement.Parse(xml)), NamingSettingsService.ReadLegacyPreferences(manager.Object, Key, FallbackKey));
    }

    [Fact]
    public void MalformedProfileXmlIsReportedWithoutOverwritingIt()
    {
        var manager = CreateManager(GetValueResult.Success, "<NamingPreferencesInfo");
        Assert.Throws<InvalidDataException>(() => NamingSettingsService.ReadLegacyPreferences(manager.Object, Key, FallbackKey));
        manager.Verify();
        manager.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(GetValueResult.Corrupt)]
    [InlineData(GetValueResult.IncompatibleType)]
    [InlineData(GetValueResult.ObsoleteFormat)]
    [InlineData(GetValueResult.UnknownError)]
    public void FailedFallbackReadsDoNotBecomeSuccessfulDefaults(GetValueResult result)
    {
        var manager = CreateManager(GetValueResult.Missing, "");
        var xml = "";
        manager.Setup(service => service.TryGetValue(FallbackKey, out xml)).Returns(result);

        Assert.Throws<InvalidDataException>(() => NamingSettingsService.ReadLegacyPreferences(manager.Object, Key, FallbackKey));
    }

    [Fact]
    public void MalformedFallbackXmlIsReportedWithoutOverwritingIt()
    {
        var manager = CreateManager(GetValueResult.Missing, "");
        var xml = "<NamingPreferencesInfo";
        manager.Setup(service => service.TryGetValue(FallbackKey, out xml)).Returns(GetValueResult.Success).Verifiable();

        Assert.Throws<InvalidDataException>(() => NamingSettingsService.ReadLegacyPreferences(manager.Object, Key, FallbackKey));
        manager.Verify();
        manager.VerifyNoOtherCalls();
    }

    private static Mock<ISettingsManager> CreateManager(GetValueResult result, string xml)
    {
        var manager = new Mock<ISettingsManager>(MockBehavior.Strict);
        manager.Setup(service => service.TryGetValue(Key, out xml)).Returns(result).Verifiable();
        return manager;
    }
}
