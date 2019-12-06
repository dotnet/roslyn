// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
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
            Assert.Equal(FeaturesResources.EditAndContinue, d.Category);
            Assert.Equal(new[] { "EditAndContinue", "Telemetry", "NotConfigurable" }, d.CustomTags);
            Assert.Equal("", d.Description);
            Assert.Equal("", d.HelpLinkUri);
            Assert.Equal(new LocalizableResourceString(nameof(FeaturesResources.RudeEdit), FeaturesResources.ResourceManager, typeof(FeaturesResources)), d.Title);
            Assert.Equal(new LocalizableResourceString(nameof(FeaturesResources.Updating_an_active_statement_will_prevent_the_debug_session_from_continuing),
                FeaturesResources.ResourceManager, typeof(FeaturesResources)), d.MessageFormat);

            Assert.Equal("ENC0082", EditAndContinueDiagnosticDescriptors.GetDescriptor(RudeEditKind.ComplexQueryExpression).Id);

            d = EditAndContinueDiagnosticDescriptors.GetDescriptor(EditAndContinueErrorCode.ErrorReadingFile);
            Assert.Equal("ENC1001", d.Id);
            Assert.Equal(FeaturesResources.EditAndContinue, d.Category);
            Assert.Equal(new[] { "EditAndContinue", "Telemetry", "NotConfigurable" }, d.CustomTags);
            Assert.Equal("", d.Description);
            Assert.Equal("", d.HelpLinkUri);
            Assert.Equal(new LocalizableResourceString(nameof(FeaturesResources.EditAndContinue), FeaturesResources.ResourceManager, typeof(FeaturesResources)), d.Title);
            Assert.Equal(new LocalizableResourceString(nameof(FeaturesResources.ErrorReadingFile),
                FeaturesResources.ResourceManager, typeof(FeaturesResources)), d.MessageFormat);

            d = EditAndContinueDiagnosticDescriptors.GetModuleDiagnosticDescriptor(12);
            Assert.Equal("ENC2012", d.Id);
            Assert.Equal(FeaturesResources.EditAndContinue, d.Category);
            Assert.Equal(new[] { "EditAndContinue", "Telemetry", "NotConfigurable" }, d.CustomTags);
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
