// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Xunit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    public class EditAndContinueDiagnosticDescriptorsTests
    {
        [Fact]
        public void GetDescriptor()
        {
            var d = EditAndContinueDiagnosticDescriptors.GetDescriptor(RudeEditKind.ActiveStatementUpdate);
            Assert.Equal("ENC0001", d.Id);
            Assert.Equal(DiagnosticCategory.EditAndContinue, d.Category);
            Assert.Equal(new[] { "EditAndContinue", "Telemetry", "NotConfigurable", EnforceOnBuild.Never.ToCustomTag() }, d.CustomTags);
            Assert.Equal("", d.Description);
            Assert.Equal("", d.HelpLinkUri);
            Assert.Equal(new LocalizableResourceString(nameof(FeaturesResources.RudeEdit), FeaturesResources.ResourceManager, typeof(FeaturesResources)), d.Title);
            Assert.Equal(new LocalizableResourceString(nameof(FeaturesResources.Updating_an_active_statement_requires_restarting_the_application),
                FeaturesResources.ResourceManager, typeof(FeaturesResources)), d.MessageFormat);

            Assert.Equal("ENC0087", EditAndContinueDiagnosticDescriptors.GetDescriptor(RudeEditKind.ComplexQueryExpression).Id);

            d = EditAndContinueDiagnosticDescriptors.GetDescriptor(EditAndContinueErrorCode.ErrorReadingFile);
            Assert.Equal("ENC1001", d.Id);
            Assert.Equal(DiagnosticCategory.EditAndContinue, d.Category);
            Assert.Equal(new[] { "EditAndContinue", "Telemetry", "NotConfigurable", EnforceOnBuild.Never.ToCustomTag() }, d.CustomTags);
            Assert.Equal("", d.Description);
            Assert.Equal("", d.HelpLinkUri);
            Assert.Equal(new LocalizableResourceString(nameof(FeaturesResources.EditAndContinue), FeaturesResources.ResourceManager, typeof(FeaturesResources)), d.Title);
            Assert.Equal(new LocalizableResourceString(nameof(FeaturesResources.ErrorReadingFile),
                FeaturesResources.ResourceManager, typeof(FeaturesResources)), d.MessageFormat);

            d = EditAndContinueDiagnosticDescriptors.GetModuleDiagnosticDescriptor(ManagedHotReloadAvailabilityStatus.Optimized);
            Assert.Equal("ENC2012", d.Id);
            Assert.Equal(DiagnosticCategory.EditAndContinue, d.Category);
            Assert.Equal(new[] { "EditAndContinue", "Telemetry", "NotConfigurable", EnforceOnBuild.Never.ToCustomTag() }, d.CustomTags);
            Assert.Equal("", d.Description);
            Assert.Equal("", d.HelpLinkUri);
            Assert.Equal(new LocalizableResourceString(nameof(FeaturesResources.EditAndContinue), FeaturesResources.ResourceManager, typeof(FeaturesResources)), d.Title);
            Assert.Equal(new LocalizableResourceString(nameof(FeaturesResources.EditAndContinueDisallowedByProject), FeaturesResources.ResourceManager, typeof(FeaturesResources)), d.MessageFormat);
        }

        [Fact]
        public void GetDescriptors()
        {
            var descriptors = EditAndContinueDiagnosticDescriptors.GetDescriptors();
            Assert.NotEmpty(descriptors);
            Assert.True(descriptors.All(d => d != null));
        }
    }
}
