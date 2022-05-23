// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Editor.Implementation.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.EditorUtilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Structure
{
    [UseExportProvider]
    public class AbstractStructureTaggerProviderTests
    {
        private static void TextContainsRegionOrUsing(string input, bool expected, string language)
        {
            var exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider();
            var buffer = EditorFactory.CreateBuffer(exportProvider, input.Split(new string[] { Environment.NewLine }, StringSplitOptions.None));
            var textSnapshot = buffer.CurrentSnapshot;

            var actual = AbstractStructureTaggerProvider.ContainsRegionOrImport(textSnapshot, collapseRegions: true, collapseImports: true, language);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void UsingDirective()
        {
            var input = @"
using System;
";

            TextContainsRegionOrUsing(input, true, LanguageNames.CSharp);
        }

        [Fact]
        public void UsingDirectiveInNamespace()
        {
            var input = @"
namespace Goo
{
    using System;
}
";

            TextContainsRegionOrUsing(input, true, LanguageNames.CSharp);
        }

        [Fact]
        public void UsingStaticDirective()
        {
            var input = @"
using static System;
";

            TextContainsRegionOrUsing(input, true, LanguageNames.CSharp);
        }

        [Fact]
        public void UsingAliasDirective()
        {
            var input = @"
using A = System;
";

            TextContainsRegionOrUsing(input, true, LanguageNames.CSharp);
        }

        [Fact]
        public void ExternAlias()
        {
            var input = @"
extern alias Goo;
";

            TextContainsRegionOrUsing(input, true, LanguageNames.CSharp);
        }

        [Fact]
        public void ImportsStatement()
        {
            var input = @"
Imports System
";

            TextContainsRegionOrUsing(input, true, LanguageNames.VisualBasic);
        }

        [Fact]
        public void ImportsAliasStatement()
        {
            var input = @"
Imports A = System
";

            TextContainsRegionOrUsing(input, true, LanguageNames.VisualBasic);
        }

        [Fact]
        public void CSharpRegion1()
        {
            var input = @"
    #region
";

            TextContainsRegionOrUsing(input, true, LanguageNames.CSharp);
        }

        [Fact]
        public void CSharpRegion()
        {
            var input = @"
    #region Goo
";

            TextContainsRegionOrUsing(input, true, LanguageNames.CSharp);
        }

        [Fact]
        public void VisualBasicRegion()
        {
            var input = @"
#Region Goo
";

            TextContainsRegionOrUsing(input, true, LanguageNames.VisualBasic);
        }
    }
}
