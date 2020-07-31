// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Composition;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeFixes
{
    [UseExportProvider]
    public class ExtensionOrderingTests
    {
        private static ExportProvider ExportProvider => EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider();

        [ConditionalFact(typeof(x86))]
        public void TestNoCyclesInFixProviders()
        {
            // This test will fail if a cycle is detected in the ordering of our code fix providers.
            // If this test fails, you can break the cycle by inspecting and fixing up the contents of
            // any [ExtensionOrder()] attributes present on our code fix providers.
            var providers = ExportProvider.GetExports<CodeFixProvider, CodeChangeProviderMetadata>();
            var providersPerLanguage = providers.ToPerLanguageMapWithMultipleLanguages();

            var csharpProviders = providersPerLanguage[LanguageNames.CSharp];

            // ExtensionOrderer.TestAccessor.CheckForCycles() will throw ArgumentException if cycle is detected.
            ExtensionOrderer.TestAccessor.CheckForCycles(csharpProviders);

            // ExtensionOrderer.Order() will not throw even if cycle is detected. However, it will
            // break the cycle and the resulting order will end up being unpredictable.
            var actualOrder = ExtensionOrderer.Order(csharpProviders).ToArray();
            Assert.True(actualOrder.Length > 0);
            Assert.True(actualOrder.IndexOf(p => p.Metadata.Name == PredefinedCodeFixProviderNames.AddImport) <
                actualOrder.IndexOf(p => p.Metadata.Name == PredefinedCodeFixProviderNames.FullyQualify));

            var vbProviders = providersPerLanguage[LanguageNames.VisualBasic];
            ExtensionOrderer.TestAccessor.CheckForCycles(vbProviders);
            actualOrder = ExtensionOrderer.Order(vbProviders).ToArray();
            Assert.True(actualOrder.Length > 0);
            Assert.True(actualOrder.IndexOf(p => p.Metadata.Name == PredefinedCodeFixProviderNames.AddImport) <
                actualOrder.IndexOf(p => p.Metadata.Name == PredefinedCodeFixProviderNames.FullyQualify));
        }

        [Fact]
        public void TestNoCyclesInSuppressionProviders()
        {
            // This test will fail if a cycle is detected in the ordering of our suppression fix providers.
            // If this test fails, you can break the cycle by inspecting and fixing up the contents of
            // any [ExtensionOrder()] attributes present on our suppression fix providers.
            var providers = ExportProvider.GetExports<IConfigurationFixProvider, CodeChangeProviderMetadata>();
            var providersPerLanguage = providers.ToPerLanguageMapWithMultipleLanguages();

            TestCore(LanguageNames.CSharp);
            TestCore(LanguageNames.VisualBasic);
            return;

            // Local functions.
            void TestCore(string language)
            {
                var providers = providersPerLanguage[language];

                // ExtensionOrderer.TestAccessor.CheckForCycles() will throw ArgumentException if cycle is detected.
                ExtensionOrderer.TestAccessor.CheckForCycles(providers);

                // ExtensionOrderer.Order() will not throw even if cycle is detected. However, it will
                // break the cycle and the resulting order will end up being unpredictable.
                var actualOrder = ExtensionOrderer.Order(providers).ToArray();
                Assert.Equal(3, actualOrder.Length);
                Assert.Equal(PredefinedCodeFixProviderNames.Suppression, actualOrder[0].Metadata.Name);
                Assert.Equal(PredefinedCodeFixProviderNames.ConfigureCodeStyleOption, actualOrder[1].Metadata.Name);
                Assert.Equal(PredefinedCodeFixProviderNames.ConfigureSeverity, actualOrder[2].Metadata.Name);
            }
        }

        [Fact]
        public void TestNoCyclesInRefactoringProviders()
        {
            // This test will fail if a cycle is detected in the ordering of our code refactoring providers.
            // If this test fails, you can break the cycle by inspecting and fixing up the contents of
            // any [ExtensionOrder()] attributes present on our code refactoring providers.
            var providers = ExportProvider.GetExports<CodeRefactoringProvider, CodeChangeProviderMetadata>();
            var providersPerLanguage = providers.ToPerLanguageMapWithMultipleLanguages();

            var csharpProviders = providersPerLanguage[LanguageNames.CSharp];

            // ExtensionOrderer.TestAccessor.CheckForCycles() will throw ArgumentException if cycle is detected.
            ExtensionOrderer.TestAccessor.CheckForCycles(csharpProviders);

            // ExtensionOrderer.Order() will not throw even if cycle is detected. However, it will
            // break the cycle and the resulting order will end up being unpredictable.
            var actualOrder = ExtensionOrderer.Order(csharpProviders).ToArray();
            Assert.True(actualOrder.Length > 0);

            var vbProviders = providersPerLanguage[LanguageNames.VisualBasic];
            ExtensionOrderer.TestAccessor.CheckForCycles(vbProviders);
            actualOrder = ExtensionOrderer.Order(vbProviders).ToArray();
            Assert.True(actualOrder.Length > 0);
        }
    }
}
