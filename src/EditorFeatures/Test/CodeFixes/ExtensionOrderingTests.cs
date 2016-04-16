// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.Composition;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeFixes
{
    public class ExtensionOrderingTests
    {
        private readonly ExportProvider _exportProvider = TestExportProvider.ExportProviderWithCSharpAndVisualBasic;

        [ConditionalFact(typeof(x86))]
        public void TestNoCyclesInFixProviders()
        {
            // This test will fail if a cycle is detected in the ordering of our code fix providers.
            // If this test fails, you can break the cycle by inspecting and fixing up the contents of
            // any [ExtensionOrder()] attributes present on our code fix providers.
            var providers = _exportProvider.GetExports<CodeFixProvider, CodeChangeProviderMetadata>();
            var providersPerLanguage = providers.ToPerLanguageMapWithMultipleLanguages();

            var csharpProviders = providersPerLanguage[LanguageNames.CSharp];

            // ExtensionOrderer.CheckForCycles() will throw ArgumentException if cycle is detected.
            ExtensionOrderer.CheckForCycles(csharpProviders);

            // ExtensionOrderer.Order() will not throw even if cycle is detected. However, it will
            // break the cycle and the resulting order will end up being unpredictable.
            var actualOrder = ExtensionOrderer.Order(csharpProviders).ToArray();
            Assert.True(actualOrder.Length > 0);
            Assert.True(actualOrder.IndexOf(p => p.Metadata.Name == PredefinedCodeFixProviderNames.AddUsingOrImport) <
                actualOrder.IndexOf(p => p.Metadata.Name == PredefinedCodeFixProviderNames.RenameTracking));

            var vbProviders = providersPerLanguage[LanguageNames.VisualBasic];
            ExtensionOrderer.CheckForCycles(vbProviders);
            actualOrder = ExtensionOrderer.Order(vbProviders).ToArray();
            Assert.True(actualOrder.Length > 0);
            Assert.True(actualOrder.IndexOf(p => p.Metadata.Name == PredefinedCodeFixProviderNames.AddUsingOrImport) <
                actualOrder.IndexOf(p => p.Metadata.Name == PredefinedCodeFixProviderNames.RenameTracking));
        }

        [Fact]
        public void TestNoCyclesInSuppressionProviders()
        {
            // This test will fail if a cycle is detected in the ordering of our suppression fix providers.
            // If this test fails, you can break the cycle by inspecting and fixing up the contents of
            // any [ExtensionOrder()] attributes present on our suppression fix providers.
            var providers = _exportProvider.GetExports<ISuppressionFixProvider, CodeChangeProviderMetadata>();
            var providersPerLanguage = providers.ToPerLanguageMapWithMultipleLanguages();

            var csharpProviders = providersPerLanguage[LanguageNames.CSharp];

            // ExtensionOrderer.CheckForCycles() will throw ArgumentException if cycle is detected.
            ExtensionOrderer.CheckForCycles(csharpProviders);

            // ExtensionOrderer.Order() will not throw even if cycle is detected. However, it will
            // break the cycle and the resulting order will end up being unpredictable.
            var actualOrder = ExtensionOrderer.Order(csharpProviders).ToArray();
            Assert.Equal(1, actualOrder.Length);

            var vbProviders = providersPerLanguage[LanguageNames.VisualBasic];
            ExtensionOrderer.CheckForCycles(vbProviders);
            actualOrder = ExtensionOrderer.Order(vbProviders).ToArray();
            Assert.Equal(1, actualOrder.Length);
        }

        [Fact]
        public void TestNoCyclesInRefactoringProviders()
        {
            // This test will fail if a cycle is detected in the ordering of our code refactoring providers.
            // If this test fails, you can break the cycle by inspecting and fixing up the contents of
            // any [ExtensionOrder()] attributes present on our code refactoring providers.
            var providers = _exportProvider.GetExports<CodeRefactoringProvider, CodeChangeProviderMetadata>();
            var providersPerLanguage = providers.ToPerLanguageMapWithMultipleLanguages();

            var csharpProviders = providersPerLanguage[LanguageNames.CSharp];

            // ExtensionOrderer.CheckForCycles() will throw ArgumentException if cycle is detected.
            ExtensionOrderer.CheckForCycles(csharpProviders);

            // ExtensionOrderer.Order() will not throw even if cycle is detected. However, it will
            // break the cycle and the resulting order will end up being unpredictable.
            var actualOrder = ExtensionOrderer.Order(csharpProviders).ToArray();
            Assert.True(actualOrder.Length > 0);

            var vbProviders = providersPerLanguage[LanguageNames.VisualBasic];
            ExtensionOrderer.CheckForCycles(vbProviders);
            actualOrder = ExtensionOrderer.Order(vbProviders).ToArray();
            Assert.True(actualOrder.Length > 0);
        }
    }
}
