// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Completion.CompletionProviders.Snippets;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    [UseExportProvider]
    public class CompletionProviderOrderTests
    {
        /// <summary>
        /// Verifies the exact order of all built-in completion providers.
        /// </summary>
        [Fact]
        public void TestCompletionProviderOrder()
        {
            var exportProvider = EditorTestCompositions.EditorFeaturesWpf.ExportProviderFactory.CreateExportProvider();
            var completionProviderExports = exportProvider.GetExports<CompletionProvider, CompletionProviderMetadata>();
            var orderedCSharpCompletionProviders = ExtensionOrderer.Order(completionProviderExports.Where(export => export.Metadata.Language == LanguageNames.CSharp));

            var actualOrder = orderedCSharpCompletionProviders.Select(x => x.Value.GetType()).ToArray();
            var expectedOrder = new[]
            {
                // Marker for start of built-in completion providers
                typeof(FirstBuiltInCompletionProvider),

                // Built-in providers
                typeof(AttributeNamedParameterCompletionProvider),
                typeof(NamedParameterCompletionProvider),
                typeof(KeywordCompletionProvider),
                typeof(AwaitCompletionProvider),
                typeof(SpeculativeTCompletionProvider),
                typeof(SymbolCompletionProvider),
                typeof(UnnamedSymbolCompletionProvider),
                typeof(ExplicitInterfaceMemberCompletionProvider),
                typeof(ExplicitInterfaceTypeCompletionProvider),
                typeof(ObjectCreationCompletionProvider),
                typeof(ObjectAndWithInitializerCompletionProvider),
                typeof(CSharpSuggestionModeCompletionProvider),
                typeof(EnumAndCompletionListTagCompletionProvider),
                typeof(CrefCompletionProvider),
                typeof(SnippetCompletionProvider),
                typeof(ExternAliasCompletionProvider),
                typeof(PreprocessorCompletionProvider),
                typeof(OverrideCompletionProvider),
                typeof(PartialMethodCompletionProvider),
                typeof(PartialTypeCompletionProvider),
                typeof(XmlDocCommentCompletionProvider),
                typeof(TupleNameCompletionProvider),
                typeof(DeclarationNameCompletionProvider),
                typeof(InternalsVisibleToCompletionProvider),
                typeof(PropertySubpatternCompletionProvider),
                typeof(TypeImportCompletionProvider),
                typeof(ExtensionMethodImportCompletionProvider),
                typeof(AggregateEmbeddedLanguageCompletionProvider),
                typeof(FunctionPointerUnmanagedCallingConventionCompletionProvider),
                typeof(CSharpSnippetCompletionProvider),

                // Built-in interactive providers
                typeof(LoadDirectiveCompletionProvider),
                typeof(ReferenceDirectiveCompletionProvider),

                // Marker for end of built-in completion providers
                typeof(LastBuiltInCompletionProvider),
            };

            AssertEx.EqualOrDiff(
                string.Join(Environment.NewLine, expectedOrder.Select(x => x.FullName)),
                string.Join(Environment.NewLine, actualOrder.Select(x => x.FullName)));
        }

        /// <summary>
        /// Verifies that the order of built-in completion providers is deterministic.
        /// </summary>
        /// <remarks>We ensure that the order is deterministic by the list being explicit: each provider except the first must have
        /// a Before or After attribute that explicitly orders it by the next one in the list. This ensures that if more than
        /// one provider provides the same completion item, the provider that provides the winning one is consistent.</remarks>
        [Fact]
        public void TestCompletionProviderOrderMetadata()
        {
            var exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider();
            var completionProviderExports = exportProvider.GetExports<CompletionProvider, CompletionProviderMetadata>();
            var orderedCSharpCompletionProviders = ExtensionOrderer.Order(completionProviderExports.Where(export => export.Metadata.Language == LanguageNames.CSharp));

            for (var i = 0; i < orderedCSharpCompletionProviders.Count; i++)
            {
                if (i == 0)
                {
                    Assert.Empty(orderedCSharpCompletionProviders[i].Metadata.BeforeTyped);
                    Assert.Empty(orderedCSharpCompletionProviders[i].Metadata.AfterTyped);
                }
                else if (i == orderedCSharpCompletionProviders.Count - 1) // last one
                {
                    // The last one isn't before anything else
                    Assert.Empty(orderedCSharpCompletionProviders[i].Metadata.BeforeTyped);

                    // The last completion marker should be last; this is ensured by either the last "real" provider saying it comes before the
                    // marker, or the last completion marker comes after the last "real" provider.
                    if (!orderedCSharpCompletionProviders[i].Metadata.AfterTyped.Contains(orderedCSharpCompletionProviders[i - 1].Metadata.Name))
                    {
                        // Make sure the last built-in provider comes before the marker
                        Assert.Contains(orderedCSharpCompletionProviders[i].Metadata.Name, orderedCSharpCompletionProviders[i - 1].Metadata.BeforeTyped);
                    }
                }
                else
                {
                    if (orderedCSharpCompletionProviders[i].Metadata.BeforeTyped.Any())
                    {
                        Assert.Equal(orderedCSharpCompletionProviders.Last().Metadata.Name, Assert.Single(orderedCSharpCompletionProviders[i].Metadata.BeforeTyped));
                    }

                    var after = Assert.Single(orderedCSharpCompletionProviders[i].Metadata.AfterTyped);
                    Assert.Equal(orderedCSharpCompletionProviders[i - 1].Metadata.Name, after);
                }
            }
        }

        [Fact]
        public void TestCompletionProviderFirstNameMetadata()
        {
            var exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider();
            var completionProviderExports = exportProvider.GetExports<CompletionProvider, CompletionProviderMetadata>();
            var orderedCSharpCompletionProviders = ExtensionOrderer.Order(completionProviderExports.Where(export => export.Metadata.Language == LanguageNames.CSharp));
            var firstCompletionProvider = orderedCSharpCompletionProviders.First();

            Assert.Equal("FirstBuiltInCompletionProvider", firstCompletionProvider.Metadata.Name);
        }

        [Fact]
        public void TestCompletionProviderLastNameMetadata()
        {
            var exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider();
            var completionProviderExports = exportProvider.GetExports<CompletionProvider, CompletionProviderMetadata>();
            var orderedCSharpCompletionProviders = ExtensionOrderer.Order(completionProviderExports.Where(export => export.Metadata.Language == LanguageNames.CSharp));
            var lastCompletionProvider = orderedCSharpCompletionProviders.Last();

            Assert.Equal("LastBuiltInCompletionProvider", lastCompletionProvider.Metadata.Name);
        }

        [Fact]
        public void TestCompletionProviderNameMetadata()
        {
            var exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider();
            var completionProviderExports = exportProvider.GetExports<CompletionProvider, CompletionProviderMetadata>();
            var csharpCompletionProviders = completionProviderExports.Where(export => export.Metadata.Language == LanguageNames.CSharp);
            foreach (var export in csharpCompletionProviders)
            {
                Assert.Equal(export.Value.GetType().Name, export.Metadata.Name);
            }
        }
    }
}
