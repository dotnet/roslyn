// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.Options;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.UnitTests;

[UseExportProvider]
public sealed class VisualStudioStorageReadFallbackTests
{
    [Fact]
    public void SpaceBetweenParentheses()
    {
        var exportProvider = VisualStudioTestCompositions.LanguageServices.ExportProviderFactory.CreateExportProvider();
        var fallback = exportProvider.GetExports<IVisualStudioStorageReadFallback, OptionNameMetadata>().Single(export => export.Metadata.ConfigName == "csharp_space_between_parentheses").Value;
        string? language = null;

        // if no flags are set the result should be "missing":
        Assert.Equal(default(Optional<object?>), fallback.TryRead(language, (_, _, _) => default(Optional<object?>)));

        // all flags set:
        Assert.Equal(fallback.TryRead(language, (_, _, _) => true).Value, SpacePlacementWithinParentheses.All);

        // one flag present in storage (false), defaults used for others:
        Assert.Equal(
            fallback.TryRead(language, (storageKey, _, _) => storageKey == "TextEditor.CSharp.Specific.SpaceWithinExpressionParentheses" ? false : default(Optional<object?>)).Value,
            CSharpFormattingOptions2.SpaceBetweenParentheses.DefaultValue & ~SpacePlacementWithinParentheses.Expressions);

        // one flag present in storage (true), defaults used for others:
        Assert.Equal(
            fallback.TryRead(language, (storageKey, _, _) => storageKey == "TextEditor.CSharp.Specific.SpaceWithinExpressionParentheses" ? true : default(Optional<object?>)).Value,
            CSharpFormattingOptions2.SpaceBetweenParentheses.DefaultValue | SpacePlacementWithinParentheses.Expressions);
    }

    [Fact]
    public void NewLinesForBraces()
    {
        var exportProvider = VisualStudioTestCompositions.LanguageServices.ExportProviderFactory.CreateExportProvider();
        var fallback = exportProvider.GetExports<IVisualStudioStorageReadFallback, OptionNameMetadata>().Single(export => export.Metadata.ConfigName == "csharp_new_line_before_open_brace").Value;
        string? language = null;

        // if no flags are set the result should be "missing":
        Assert.Equal(default(Optional<object?>), fallback.TryRead(language, (_, _, _) => default(Optional<object?>)));

        // all flags set:
        Assert.Equal(fallback.TryRead(language, (_, _, _) => true).Value, NewLineBeforeOpenBracePlacement.All);

        // one flag present in storage (false), defaults used for others:
        Assert.Equal(
            fallback.TryRead(language, (storageKey, _, _) => storageKey == "TextEditor.CSharp.Specific.NewLinesForBracesInObjectCollectionArrayInitializers" ? false : default(Optional<object?>)).Value,
            CSharpFormattingOptions2.NewLineBeforeOpenBrace.DefaultValue & ~NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers);

        // one flag present in storage (true), defaults used for others:
        Assert.Equal(
            fallback.TryRead(language, (storageKey, _, _) => storageKey == "TextEditor.CSharp.Specific.NewLinesForBracesInObjectCollectionArrayInitializers" ? true : default(Optional<object?>)).Value,
            CSharpFormattingOptions2.NewLineBeforeOpenBrace.DefaultValue | NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers);
    }
}
