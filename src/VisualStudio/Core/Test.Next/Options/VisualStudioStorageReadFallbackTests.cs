// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.Options;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.UnitTests;

[UseExportProvider]
public class VisualStudioStorageReadFallbackTests
{
    [Fact]
    public void SpaceBetweenParentheses()
    {
        var exportProvider = VisualStudioTestCompositions.LanguageServices.ExportProviderFactory.CreateExportProvider();
        var export = exportProvider.GetExports<IVisualStudioStorageReadFallback, OptionNameMetadata>().Single(export => export.Metadata.ConfigName == "csharp_space_between_parentheses");
        string? language = null;

        // if no flags are set the result should be default:
        Assert.Equal(default(Optional<object?>), export.Value.TryRead(language, (storageKey, storageType) => default(Optional<object?>)));

        // all flags set:
        Assert.Equal(export.Value.TryRead(language, (storageKey, storageType) => true).Value, SpacePlacementWithinParentheses.All);
    }

    [Fact]
    public void NewLinesForBraces()
    {
        var exportProvider = VisualStudioTestCompositions.LanguageServices.ExportProviderFactory.CreateExportProvider();
        var export = exportProvider.GetExports<IVisualStudioStorageReadFallback, OptionNameMetadata>().Single(export => export.Metadata.ConfigName == "csharp_new_line_before_open_brace");
        string? language = null;

        // if no flags are set the result should be default:
        Assert.Equal(default(Optional<object?>), export.Value.TryRead(language, (storageKey, storageType) => default(Optional<object?>)));

        // all flags set:
        Assert.Equal(export.Value.TryRead(language, (storageKey, storageType) => true).Value, NewLineBeforeOpenBracePlacement.All);
    }
}
