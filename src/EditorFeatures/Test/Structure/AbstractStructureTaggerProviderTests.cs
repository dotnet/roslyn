// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Editor.Implementation.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.EditorUtilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Structure;

[UseExportProvider]
public sealed class AbstractStructureTaggerProviderTests
{
    private static void TextContainsRegionOrUsing(string input, bool expected, string language)
    {
        var exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider();
        var buffer = EditorFactory.CreateBuffer(exportProvider, input.Split([Environment.NewLine], StringSplitOptions.None));
        var textSnapshot = buffer.CurrentSnapshot;

        var actual = AbstractStructureTaggerProvider.ContainsRegionOrImport(textSnapshot, collapseRegions: true, collapseImports: true, language);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UsingDirective()
        => TextContainsRegionOrUsing("""
            using System;
            """, true, LanguageNames.CSharp);

    [Fact]
    public void UsingDirectiveInNamespace()
        => TextContainsRegionOrUsing("""
            namespace Goo
            {
                using System;
            }
            """, true, LanguageNames.CSharp);

    [Fact]
    public void UsingStaticDirective()
        => TextContainsRegionOrUsing("""
            using static System;
            """, true, LanguageNames.CSharp);

    [Fact]
    public void UsingAliasDirective()
        => TextContainsRegionOrUsing("""
            using A = System;
            """, true, LanguageNames.CSharp);

    [Fact]
    public void ExternAlias()
        => TextContainsRegionOrUsing("""
            extern alias Goo;
            """, true, LanguageNames.CSharp);

    [Fact]
    public void ImportsStatement()
        => TextContainsRegionOrUsing("""
            Imports System
            """, true, LanguageNames.VisualBasic);

    [Fact]
    public void ImportsAliasStatement()
        => TextContainsRegionOrUsing("""
            Imports A = System
            """, true, LanguageNames.VisualBasic);

    [Fact]
    public void CSharpRegion1()
        => TextContainsRegionOrUsing("""
            #region
            """, true, LanguageNames.CSharp);

    [Fact]
    public void CSharpRegion()
        => TextContainsRegionOrUsing("""
            #region Goo
            """, true, LanguageNames.CSharp);

    [Fact]
    public void VisualBasicRegion()
        => TextContainsRegionOrUsing("""
            #Region Goo
            """, true, LanguageNames.VisualBasic);
}
