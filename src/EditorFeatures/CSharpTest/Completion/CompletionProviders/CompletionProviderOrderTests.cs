// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Completion.SuggestionMode;
using Microsoft.CodeAnalysis.Editor.CSharp.Completion.CompletionProviders;
using Microsoft.CodeAnalysis.Editor.CSharp.Completion.FileSystem;
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
        [Fact]
        public void TestCompletionProviderOrder()
        {
            var completionProviderExports = TestExportProvider.ExportProviderWithCSharpAndVisualBasic.GetExports<CompletionProvider, CompletionProviderMetadata>();
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
                typeof(SpeculativeTCompletionProvider),
                typeof(SymbolCompletionProvider),
                typeof(ExplicitInterfaceMemberCompletionProvider),
                typeof(ExplicitInterfaceTypeCompletionProvider),
                typeof(ObjectCreationCompletionProvider),
                typeof(ObjectInitializerCompletionProvider),
                typeof(CSharpSuggestionModeCompletionProvider),
                typeof(EnumAndCompletionListTagCompletionProvider),
                typeof(CrefCompletionProvider),
                typeof(SnippetCompletionProvider),
                typeof(ExternAliasCompletionProvider),
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

                // Built-in interactive providers
                typeof(LoadDirectiveCompletionProvider),
                typeof(ReferenceDirectiveCompletionProvider),
                typeof(CSharpReplCommandCompletionProvider),

                // Marker for end of built-in completion providers
                typeof(LastBuiltInCompletionProvider),
            };

            AssertEx.EqualOrDiff(
                string.Join(Environment.NewLine, expectedOrder.Select(x => x.FullName)),
                string.Join(Environment.NewLine, actualOrder.Select(x => x.FullName)));
        }

        [Fact]
        public void TestCompletionProviderOrderMetadata()
        {
            var completionProviderExports = TestExportProvider.ExportProviderWithCSharpAndVisualBasic.GetExports<CompletionProvider, CompletionProviderMetadata>();
            var orderedCSharpCompletionProviders = ExtensionOrderer.Order(completionProviderExports.Where(export => export.Metadata.Language == LanguageNames.CSharp));

            for (var i = 0; i < orderedCSharpCompletionProviders.Count; i++)
            {
                if (i == 0)
                {
                    Assert.Empty(orderedCSharpCompletionProviders[i].Metadata.BeforeTyped);
                    Assert.Empty(orderedCSharpCompletionProviders[i].Metadata.AfterTyped);
                    continue;
                }
                else if (i == orderedCSharpCompletionProviders.Count - 1)
                {
                    Assert.Empty(orderedCSharpCompletionProviders[i].Metadata.BeforeTyped);
                    if (!orderedCSharpCompletionProviders[i].Metadata.AfterTyped.Contains(orderedCSharpCompletionProviders[i - 1].Metadata.Name))
                    {
                        // Make sure the last built-in provider comes before the marker
                        Assert.Contains(orderedCSharpCompletionProviders[i].Metadata.Name, orderedCSharpCompletionProviders[i - 1].Metadata.BeforeTyped);
                    }

                    continue;
                }

                if (orderedCSharpCompletionProviders[i].Metadata.BeforeTyped.Any())
                {
                    Assert.Equal(orderedCSharpCompletionProviders.Last().Metadata.Name, Assert.Single(orderedCSharpCompletionProviders[i].Metadata.BeforeTyped));
                }

                var after = Assert.Single(orderedCSharpCompletionProviders[i].Metadata.AfterTyped);
                Assert.Equal(orderedCSharpCompletionProviders[i - 1].Metadata.Name, after);
            }
        }

        [Fact]
        public void TestCompletionProviderFirstNameMetadata()
        {
            var completionProviderExports = TestExportProvider.ExportProviderWithCSharpAndVisualBasic.GetExports<CompletionProvider, CompletionProviderMetadata>();
            var orderedCSharpCompletionProviders = ExtensionOrderer.Order(completionProviderExports.Where(export => export.Metadata.Language == LanguageNames.CSharp));
            var firstCompletionProvider = orderedCSharpCompletionProviders.First();

            Assert.Equal("FirstBuiltInCompletionProvider", firstCompletionProvider.Metadata.Name);
        }

        [Fact]
        public void TestCompletionProviderLastNameMetadata()
        {
            var completionProviderExports = TestExportProvider.ExportProviderWithCSharpAndVisualBasic.GetExports<CompletionProvider, CompletionProviderMetadata>();
            var orderedCSharpCompletionProviders = ExtensionOrderer.Order(completionProviderExports.Where(export => export.Metadata.Language == LanguageNames.CSharp));
            var lastCompletionProvider = orderedCSharpCompletionProviders.Last();

            Assert.Equal("LastBuiltInCompletionProvider", lastCompletionProvider.Metadata.Name);
        }

        [Fact]
        public void TestCompletionProviderNameMetadata()
        {
            var completionProviderExports = TestExportProvider.ExportProviderWithCSharpAndVisualBasic.GetExports<CompletionProvider, CompletionProviderMetadata>();
            var csharpCompletionProviders = completionProviderExports.Where(export => export.Metadata.Language == LanguageNames.CSharp);
            foreach (var export in csharpCompletionProviders)
            {
                Assert.Equal(export.Value.GetType().Name, export.Metadata.Name);
            }
        }
    }
}
