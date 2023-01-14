// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.ArgumentProviders
{
    [UseExportProvider]
    public class ArgumentProviderOrderTests
    {
        /// <summary>
        /// Verifies the exact order of all built-in argument providers.
        /// </summary>
        [Fact]
        public void TestArgumentProviderOrder()
        {
            var exportProvider = EditorTestCompositions.EditorFeaturesWpf.ExportProviderFactory.CreateExportProvider();
            var argumentProviderExports = exportProvider.GetExports<ArgumentProvider, CompletionProviderMetadata>();
            var orderedCSharpArgumentProviders = ExtensionOrderer.Order(argumentProviderExports.Where(export => export.Metadata.Language == LanguageNames.CSharp));

            var actualOrder = orderedCSharpArgumentProviders.Select(x => x.Value.GetType()).ToArray();
            var expectedOrder = new[]
            {
                // Marker for start of built-in argument providers
                typeof(FirstBuiltInArgumentProvider),

                // Built-in providers
                typeof(ContextVariableArgumentProvider),
                typeof(OutVariableArgumentProvider),
                typeof(DefaultArgumentProvider),

                // Marker for end of built-in argument providers
                typeof(LastBuiltInArgumentProvider),
            };

            AssertEx.EqualOrDiff(
                string.Join(Environment.NewLine, expectedOrder.Select(x => x.FullName)),
                string.Join(Environment.NewLine, actualOrder.Select(x => x.FullName)));
        }

        /// <summary>
        /// Verifies that the order of built-in argument providers is deterministic.
        /// </summary>
        /// <remarks>We ensure that the order is deterministic by the list being explicit: each provider except the first must have
        /// a Before or After attribute that explicitly orders it by the next one in the list. This ensures that if more than
        /// one provider provides the same argument, the provider that provides the winning one is consistent.</remarks>
        [Fact]
        public void TestArgumentProviderOrderMetadata()
        {
            var exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider();
            var argumentProviderExports = exportProvider.GetExports<ArgumentProvider, CompletionProviderMetadata>();
            var orderedCSharpArgumentProviders = ExtensionOrderer.Order(argumentProviderExports.Where(export => export.Metadata.Language == LanguageNames.CSharp));

            for (var i = 0; i < orderedCSharpArgumentProviders.Count; i++)
            {
                if (i == 0)
                {
                    Assert.Empty(orderedCSharpArgumentProviders[i].Metadata.BeforeTyped);
                    Assert.Empty(orderedCSharpArgumentProviders[i].Metadata.AfterTyped);
                }
                else if (i == orderedCSharpArgumentProviders.Count - 1) // last one
                {
                    // The last one isn't before anything else
                    Assert.Empty(orderedCSharpArgumentProviders[i].Metadata.BeforeTyped);

                    // The last argument marker should be last; this is ensured by either the last "real" provider saying it comes before the
                    // marker, or the last argument marker comes after the last "real" provider.
                    if (!orderedCSharpArgumentProviders[i].Metadata.AfterTyped.Contains(orderedCSharpArgumentProviders[i - 1].Metadata.Name))
                    {
                        // Make sure the last built-in provider comes before the marker
                        Assert.Contains(orderedCSharpArgumentProviders[i].Metadata.Name, orderedCSharpArgumentProviders[i - 1].Metadata.BeforeTyped);
                    }
                }
                else
                {
                    if (orderedCSharpArgumentProviders[i].Metadata.BeforeTyped.Any())
                    {
                        Assert.Equal(orderedCSharpArgumentProviders.Last().Metadata.Name, Assert.Single(orderedCSharpArgumentProviders[i].Metadata.BeforeTyped));
                    }

                    var after = Assert.Single(orderedCSharpArgumentProviders[i].Metadata.AfterTyped);
                    Assert.Equal(orderedCSharpArgumentProviders[i - 1].Metadata.Name, after);
                }
            }
        }

        [Fact]
        public void TestArgumentProviderFirstNameMetadata()
        {
            var exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider();
            var argumentProviderExports = exportProvider.GetExports<ArgumentProvider, CompletionProviderMetadata>();
            var orderedCSharpArgumentProviders = ExtensionOrderer.Order(argumentProviderExports.Where(export => export.Metadata.Language == LanguageNames.CSharp));
            var firstArgumentProvider = orderedCSharpArgumentProviders.First();

            Assert.Equal("FirstBuiltInArgumentProvider", firstArgumentProvider.Metadata.Name);
        }

        [Fact]
        public void TestArgumentProviderLastNameMetadata()
        {
            var exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider();
            var argumentProviderExports = exportProvider.GetExports<ArgumentProvider, CompletionProviderMetadata>();
            var orderedCSharpArgumentProviders = ExtensionOrderer.Order(argumentProviderExports.Where(export => export.Metadata.Language == LanguageNames.CSharp));
            var lastArgumentProvider = orderedCSharpArgumentProviders.Last();

            Assert.Equal("LastBuiltInArgumentProvider", lastArgumentProvider.Metadata.Name);
        }

        [Fact]
        public void TestArgumentProviderNameMetadata()
        {
            var exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider();
            var argumentProviderExports = exportProvider.GetExports<ArgumentProvider, CompletionProviderMetadata>();
            var csharpArgumentProviders = argumentProviderExports.Where(export => export.Metadata.Language == LanguageNames.CSharp);
            foreach (var export in csharpArgumentProviders)
            {
                Assert.Equal(export.Value.GetType().Name, export.Metadata.Name);
            }
        }
    }
}
