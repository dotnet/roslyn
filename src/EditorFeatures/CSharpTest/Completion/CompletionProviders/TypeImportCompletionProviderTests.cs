// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Composition;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    [UseExportProvider]
    public class TypeImportCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        public TypeImportCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override CompletionProvider CreateCompletionProvider()
        {
            return new TypeImportCompletionProvider();
        }

        private bool? ShowImportCompletionItemsOptionValue { get; set; } = true;

        // -1 would disable timebox, whereas 0 means always timeout.
        private int TimeoutInMilliseconds { get; set; } = -1;

        protected override void SetWorkspaceOptions(TestWorkspace workspace)
        {
            workspace.Options = workspace.Options
                .WithChangedOption(CompletionOptions.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, ShowImportCompletionItemsOptionValue)
                .WithChangedOption(CompletionServiceOptions.TimeoutInMillisecondsForImportCompletion, TimeoutInMilliseconds);
        }

        protected override ExportProvider GetExportProvider()
        {
            return ExportProviderCache
                .GetOrCreateExportProviderFactory(TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithPart(typeof(TestExperimentationService)))
                .CreateExportProvider();
        }

        #region "Option tests"

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task OptionSetToNull_ExpEnabled()
        {
            SetExperimentOption(WellKnownExperimentNames.TypeImportCompletion, true);

            ShowImportCompletionItemsOptionValue = null;

            var markup = @"
class Bar
{
     $$
}";

            await VerifyAnyItemExistsAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task OptionSetToNull_ExpDisabled()
        {
            ShowImportCompletionItemsOptionValue = null;
            var markup = @"
class Bar
{
     $$
}";

            await VerifyNoItemsExistAsync(markup);
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task OptionSetToFalse(bool isExperimentEnabled)
        {
            SetExperimentOption(WellKnownExperimentNames.TypeImportCompletion, isExperimentEnabled);

            ShowImportCompletionItemsOptionValue = false;

            var markup = @"
class Bar
{
     $$
}";

            await VerifyNoItemsExistAsync(markup);
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task OptionSetToTrue(bool isExperimentEnabled)
        {
            SetExperimentOption(WellKnownExperimentNames.TypeImportCompletion, isExperimentEnabled);

            ShowImportCompletionItemsOptionValue = true;

            var markup = @"
class Bar
{
     $$
}";

            await VerifyAnyItemExistsAsync(markup);
        }

        #endregion

        #region "CompletionItem tests"

        [InlineData("class", (int)Glyph.ClassPublic)]
        [InlineData("struct", (int)Glyph.StructurePublic)]
        [InlineData("enum", (int)Glyph.EnumPublic)]
        [InlineData("interface", (int)Glyph.InterfacePublic)]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task Show_TopLevel_NoImport_InProject(string typeKind, int glyph)
        {
            var file1 = $@"
namespace Foo
{{
    public {typeKind} Bar
    {{}}
}}";
            var file2 = @"
namespace Baz
{
    class Bat
    {
         $$
    }
}";
            await VerifyTypeImportItemExistsAsync(
                CreateMarkupForSingleProject(file2, file1, LanguageNames.CSharp),
                "Bar",
                glyph: glyph,
                inlineDescription: "Foo");
        }

        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("enum")]
        [InlineData("interface")]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DoNotShow_TopLevel_SmaeNamespace_InProject(string typeKind)
        {
            var file1 = $@"
namespace Foo
{{
    public {typeKind} Bar
    {{}}
}}";
            var file2 = @"
namespace Foo
{
    class Bat
    {
         $$
    }
}";
            await VerifyTypeImportItemIsAbsentAsync(
                CreateMarkupForSingleProject(file2, file1, LanguageNames.CSharp),
                "Bar",
                inlineDescription: "Foo");
        }

        [InlineData("class", (int)Glyph.ClassPublic)]
        [InlineData("struct", (int)Glyph.StructurePublic)]
        [InlineData("interface", (int)Glyph.InterfacePublic)]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task Show_TopLevel_MutipleOverrides_NoImport_InProject(string typeKind, int glyph)
        {
            var file1 = $@"
namespace Foo
{{
    public {typeKind} Bar
    {{}} 

    public {typeKind} Bar<T>
    {{}}                   

    public {typeKind} Bar<T1, T2>
    {{}}
}}";

            var file2 = @"
namespace Baz
{
    class Bat
    {
         $$
    }
}";
            var markup = CreateMarkupForSingleProject(file2, file1, LanguageNames.CSharp);
            await VerifyTypeImportItemExistsAsync(markup, "Bar", glyph: glyph, inlineDescription: "Foo");
            await VerifyTypeImportItemExistsAsync(markup, "Bar", displayTextSuffix: "<>", glyph: glyph, inlineDescription: "Foo");
        }

        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("enum")]
        [InlineData("interface")]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DoNotShow_NestedType_NoImport_InProject(string typeKind)
        {
            var file1 = $@"
namespace Foo
{{
    public class Bar
    {{
        public {typeKind} Faz {{}}
    }}
}}";

            var file2 = @"
namespace Baz
{
    class Bat
    {
         $$
    }
}";
            var markup = CreateMarkupForSingleProject(file2, file1, LanguageNames.CSharp);
            await VerifyTypeImportItemIsAbsentAsync(markup, "Faz", inlineDescription: "Foo");
            await VerifyTypeImportItemIsAbsentAsync(markup, "Faz", inlineDescription: "Foo.Bar");
        }

        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("enum")]
        [InlineData("interface")]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DoNotShow_TopLevel_WithImport_InProject(string typeKind)
        {
            var file1 = $@"
namespace Foo
{{
    public {typeKind} Bar
    {{}}
}}";

            var file2 = @"
namespace Baz
{
    using Foo;

    class Bat
    {
         $$
    }
}";
            var markup = CreateMarkupForSingleProject(file2, file1, LanguageNames.CSharp);
            await VerifyTypeImportItemIsAbsentAsync(markup, "Bar", inlineDescription: "Foo");
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task Show_TopLevel_Public_NoImport_InReference(bool isProjectReference)
        {
            var file1 = $@"
namespace Foo
{{
    public class Bar
    {{}}
}}";
            var file2 = @"
namespace Baz
{
    class Bat
    {
         $$
    }
}";
            var markup = GetMarkupWithReference(file2, file1, LanguageNames.CSharp, LanguageNames.CSharp, isProjectReference);
            await VerifyTypeImportItemExistsAsync(markup, "Bar", glyph: (int)Glyph.ClassPublic, inlineDescription: "Foo");
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DoNotShow_TopLevel_Public_WithImport_InReference(bool isProjectReference)
        {
            var file1 = $@"
namespace Foo
{{
    public class Bar
    {{}}
}}";
            var file2 = @"
using Foo;
namespace Baz
{
    class Bat
    {
         $$
    }
}";
            var markup = GetMarkupWithReference(file2, file1, LanguageNames.CSharp, LanguageNames.CSharp, isProjectReference);
            await VerifyTypeImportItemIsAbsentAsync(markup, "Bar", inlineDescription: "Foo");
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DoNotShow_TopLevel_Internal_NoImport_InReference(bool isProjectReference)
        {
            var file1 = $@"
namespace Foo
{{
    internal class Bar
    {{}}
}}";
            var file2 = @"
namespace Baz
{
    class Bat
    {
         $$
    }
}";
            var markup = GetMarkupWithReference(file2, file1, LanguageNames.CSharp, LanguageNames.CSharp, isProjectReference);
            await VerifyTypeImportItemIsAbsentAsync(markup, "Bar", inlineDescription: "Foo");
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TopLevel_OverloadsWithMixedAccessibility_Internal_NoImport_InReference1(bool isProjectReference)
        {
            var file1 = $@"
namespace Foo
{{
    internal class Bar
    {{}}

    public class Bar<T>
    {{}}
}}";
            var file2 = @"
namespace Baz
{
    class Bat
    {
         $$
    }
}";
            var markup = GetMarkupWithReference(file2, file1, LanguageNames.CSharp, LanguageNames.CSharp, isProjectReference);
            await VerifyTypeImportItemIsAbsentAsync(markup, "Bar", displayTextSuffix: "", inlineDescription: "Foo");
            await VerifyTypeImportItemExistsAsync(markup, "Bar", displayTextSuffix: "<>", glyph: (int)Glyph.ClassPublic, inlineDescription: "Foo");
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DoNotShow_TopLevel_OverloadsWithMixedAccessibility_Internal_WithImport_InReference1(bool isProjectReference)
        {
            var file1 = $@"
namespace Foo
{{
    internal class Bar
    {{}}

    public class Bar<T>
    {{}}
}}";
            var file2 = @"
using Foo;
namespace Baz
{
    class Bat
    {
         $$
    }
}";
            var markup = GetMarkupWithReference(file2, file1, LanguageNames.CSharp, LanguageNames.CSharp, isProjectReference);
            await VerifyTypeImportItemIsAbsentAsync(markup, "Bar", displayTextSuffix: "", inlineDescription: "Foo");
            await VerifyTypeImportItemIsAbsentAsync(markup, "Bar", displayTextSuffix: "<>", inlineDescription: "Foo");
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TopLevel_OverloadsWithMixedAccessibility_InternalWithIVT_NoImport_InReference1(bool isProjectReference)
        {
            var file1 = $@"     
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""Project1"")]

namespace Foo
{{
    internal class Bar
    {{}}

    public class Bar<T>
    {{}}
}}";
            var file2 = @"
namespace Baz
{
    class Bat
    {
         $$
    }
}";
            var markup = GetMarkupWithReference(file2, file1, LanguageNames.CSharp, LanguageNames.CSharp, isProjectReference);
            await VerifyTypeImportItemExistsAsync(markup, "Bar", glyph: (int)Glyph.ClassInternal, inlineDescription: "Foo");
            await VerifyTypeImportItemExistsAsync(markup, "Bar", displayTextSuffix: "<>", glyph: (int)Glyph.ClassPublic, inlineDescription: "Foo");
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DoNotShow_TopLevel_OverloadsWithMixedAccessibility_InternalWithIVT_WithImport_InReference1(bool isProjectReference)
        {
            var file1 = $@"     
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""Project1"")]

namespace Foo
{{
    internal class Bar
    {{}}

    public class Bar<T>
    {{}}
}}";
            var file2 = @"
using Foo;
namespace Baz
{
    class Bat
    {
         $$
    }
}";
            var markup = GetMarkupWithReference(file2, file1, LanguageNames.CSharp, LanguageNames.CSharp, isProjectReference);
            await VerifyTypeImportItemIsAbsentAsync(markup, "Bar", inlineDescription: "Foo");
            await VerifyTypeImportItemIsAbsentAsync(markup, "Bar", displayTextSuffix: "<>", inlineDescription: "Foo");
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TopLevel_OverloadsWithMixedAccessibility_Internal_NoImport_InReference2(bool isProjectReference)
        {
            var file1 = $@"
namespace Foo
{{
    public class Bar
    {{}}

    public class Bar<T>
    {{}}    

    internal class Bar<T1, T2>
    {{}}
}}";
            var file2 = @"
namespace Baz
{
    class Bat
    {
         $$
    }
}";
            var markup = GetMarkupWithReference(file2, file1, LanguageNames.CSharp, LanguageNames.CSharp, isProjectReference);
            await VerifyTypeImportItemExistsAsync(markup, "Bar", glyph: (int)Glyph.ClassPublic, inlineDescription: "Foo");
            await VerifyTypeImportItemExistsAsync(markup, "Bar", displayTextSuffix: "<>", glyph: (int)Glyph.ClassPublic, inlineDescription: "Foo");
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DoNotShow_TopLevel_OverloadsWithMixedAccessibility_Internal_SameNamespace_InReference2(bool isProjectReference)
        {
            var file1 = $@"
namespace Foo
{{
    public class Bar
    {{}}

    public class Bar<T>
    {{}}    

    internal class Bar<T1, T2>
    {{}}
}}";
            var file2 = @"
namespace Foo.Baz
{
    class Bat
    {
         $$
    }
}";
            var markup = GetMarkupWithReference(file2, file1, LanguageNames.CSharp, LanguageNames.CSharp, isProjectReference);
            await VerifyTypeImportItemIsAbsentAsync(markup, "Bar", inlineDescription: "Foo");
            await VerifyTypeImportItemIsAbsentAsync(markup, "Bar", displayTextSuffix: "<>", inlineDescription: "Foo");
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TopLevel_OverloadsWithMixedAccessibility_InternalWithIVT_NoImport_InReference2(bool isProjectReference)
        {
            var file1 = $@"   
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""Project1"")]

namespace Foo
{{
    internal class Bar
    {{}}

    internal class Bar<T>
    {{}}    

    internal class Bar<T1, T2>
    {{}}
}}";
            var file2 = @"
namespace Baz
{
    class Bat
    {
         $$
    }
}";
            var markup = GetMarkupWithReference(file2, file1, LanguageNames.CSharp, LanguageNames.CSharp, isProjectReference);
            await VerifyTypeImportItemExistsAsync(markup, "Bar", glyph: (int)Glyph.ClassInternal, inlineDescription: "Foo");
            await VerifyTypeImportItemExistsAsync(markup, "Bar", displayTextSuffix: "<>", glyph: (int)Glyph.ClassInternal, inlineDescription: "Foo");
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task Show_TopLevel_Internal_WithIVT_NoImport_InReference(bool isProjectReference)
        {
            var file1 = $@"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""Project1"")]

namespace Foo
{{
    internal class Bar
    {{}}
}}";
            var file2 = @"
namespace Baz
{
    class Bat
    {
         $$
    }
}";
            var markup = GetMarkupWithReference(file2, file1, LanguageNames.CSharp, LanguageNames.CSharp, isProjectReference);
            await VerifyTypeImportItemExistsAsync(markup, "Bar", glyph: (int)Glyph.ClassInternal, inlineDescription: "Foo");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task Show_TopLevel_NoImport_InVBReference()
        {
            var file1 = $@"
Namespace Bar
    Public Class Barr
    End CLass
End Namespace";
            var file2 = @"
namespace Baz
{
    class Bat
    {
         $$
    }
}";
            var markup = CreateMarkupForProjecWithVBProjectReference(file2, file1, sourceLanguage: LanguageNames.CSharp, rootnamespace: "Foo");
            await VerifyTypeImportItemExistsAsync(markup, "Barr", glyph: (int)Glyph.ClassPublic, inlineDescription: "Foo.Bar");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task VB_MixedCapitalization_Test()
        {
            var file1 = $@"
Namespace Na
    Public Class Foo
    End Class
End Namespace

Namespace na
    Public Class Bar
    End Class
End Namespace
";
            var file2 = @"
namespace Baz
{
    class Bat
    {
         $$
    }
}";
            var markup = CreateMarkupForProjecWithVBProjectReference(file2, file1, sourceLanguage: LanguageNames.CSharp, rootnamespace: "");
            await VerifyTypeImportItemExistsAsync(markup, "Bar", glyph: (int)Glyph.ClassPublic, inlineDescription: "Na");
            await VerifyTypeImportItemExistsAsync(markup, "Foo", glyph: (int)Glyph.ClassPublic, inlineDescription: "Na");
            await VerifyTypeImportItemIsAbsentAsync(markup, "Bar", inlineDescription: "na");
            await VerifyTypeImportItemIsAbsentAsync(markup, "Foo", inlineDescription: "na");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task VB_MixedCapitalization_WithImport_Test()
        {
            var file1 = $@"
Namespace Na
    Public Class Foo
    End Class
End Namespace

Namespace na
    Public Class Bar
    End Class
End Namespace
";
            var file2 = @"
using Na;
namespace Baz
{
    class Bat
    {
         $$
    }
}";
            var markup = CreateMarkupForProjecWithVBProjectReference(file2, file1, sourceLanguage: LanguageNames.CSharp, rootnamespace: "");
            await VerifyTypeImportItemIsAbsentAsync(markup, "Bar", inlineDescription: "Na");
            await VerifyTypeImportItemIsAbsentAsync(markup, "Foo", inlineDescription: "Na");
            await VerifyTypeImportItemIsAbsentAsync(markup, "Bar", inlineDescription: "na");
            await VerifyTypeImportItemIsAbsentAsync(markup, "Foo", inlineDescription: "na");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DoNotShow_TopLevel_Internal_NoImport_InVBReference()
        {
            var file1 = $@"
Namespace Bar
    Friend Class Barr
    End CLass
End Namespace";
            var file2 = @"
namespace Baz
{
    class Bat
    {
         $$
    }
}";
            var markup = CreateMarkupForProjecWithVBProjectReference(file2, file1, sourceLanguage: LanguageNames.CSharp, rootnamespace: "Foo");
            await VerifyTypeImportItemIsAbsentAsync(markup, "Barr", inlineDescription: "Foo.Bar");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DoNotShow_TopLevel_WithImport_InVBReference()
        {
            var file1 = $@"
Namespace Bar
    Public Class Barr
    End CLass
End Namespace";
            var file2 = @"
using Foo.Bar;
namespace Baz
{
    class Bat
    {
         $$
    }
}";
            var markup = CreateMarkupForProjecWithVBProjectReference(file2, file1, sourceLanguage: LanguageNames.CSharp, rootnamespace: "Foo");
            await VerifyTypeImportItemIsAbsentAsync(markup, "Barr", inlineDescription: "Foo.Bar");
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TypesWithIdenticalNameButDifferentNamespaces(bool isProjectReference)
        {
            var file1 = $@"
namespace Foo
{{
    public class Bar
    {{}}

    public class Bar<T>
    {{}}
}}
namespace Baz
{{
    public class Bar<T>
    {{}} 

    public class Bar
    {{}}
}}";
            var file2 = @"
namespace NS
{
    class C
    {
         $$
    }
}";
            var markup = GetMarkupWithReference(file2, file1, LanguageNames.CSharp, LanguageNames.CSharp, isProjectReference);
            await VerifyTypeImportItemExistsAsync(markup, "Bar", glyph: (int)Glyph.ClassPublic, inlineDescription: "Foo");
            await VerifyTypeImportItemExistsAsync(markup, "Bar", displayTextSuffix: "<>", glyph: (int)Glyph.ClassPublic, inlineDescription: "Foo");
            await VerifyTypeImportItemExistsAsync(markup, "Bar", glyph: (int)Glyph.ClassPublic, inlineDescription: "Baz");
            await VerifyTypeImportItemExistsAsync(markup, "Bar", displayTextSuffix: "<>", glyph: (int)Glyph.ClassPublic, inlineDescription: "Baz");
        }

        #endregion

        #region "Commit Change Tests"
        [InlineData(SourceCodeKind.Regular)]
        [InlineData(SourceCodeKind.Script)]
        [WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task Commit_NoImport_InProject(SourceCodeKind kind)
        {
            var file1 = $@"
namespace Foo
{{
    public class Bar
    {{
    }}
}}";

            var file2 = @"
namespace Baz
{
    class Bat
    {
        $$
    }
}";
            var expectedCodeAfterCommit = @"
using Foo;

namespace Baz
{
    class Bat
    {
        Bar$$
    }
}";
            var markup = CreateMarkupForSingleProject(file2, file1, LanguageNames.CSharp);
            await VerifyCustomCommitProviderAsync(markup, "Bar", expectedCodeAfterCommit, sourceCodeKind: kind);
        }


        [InlineData(SourceCodeKind.Regular)]
        [InlineData(SourceCodeKind.Script)]
        [WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task Commit_NoImport_InVBReference(SourceCodeKind kind)
        {
            var file1 = $@"
Namespace Bar
    Public Class Barr
    End CLass
End Namespace";
            var file2 = @"
namespace Baz
{
    class Bat
    {
        $$
    }
}"; var expectedCodeAfterCommit = @"
using Foo.Bar;

namespace Baz
{
    class Bat
    {
        Barr$$
    }
}";
            var markup = CreateMarkupForProjecWithVBProjectReference(file2, file1, sourceLanguage: LanguageNames.CSharp, rootnamespace: "Foo");
            await VerifyCustomCommitProviderAsync(markup, "Barr", expectedCodeAfterCommit, sourceCodeKind: kind);
        }

        [InlineData(SourceCodeKind.Regular)]
        [InlineData(SourceCodeKind.Script)]
        [WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task Commit_NoImport_InPEReference(SourceCodeKind kind)
        {
            var markup = $@"<Workspace>
    <Project Language=""{LanguageNames.CSharp}"" CommonReferences=""true"">
        <Document FilePath=""CSharpDocument"">
class Bar
{{
     $$
}}</Document>
    </Project>    
</Workspace>";
            var expectedCodeAfterCommit = @"
using System;

class Bar
{
     Console$$
}";

            await VerifyCustomCommitProviderAsync(markup, "Console", expectedCodeAfterCommit, sourceCodeKind: kind);
        }

        #endregion

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DoNotShow_TopLevel_Public_NoImport_InNonGLobalAliasedMetadataReference()
        {
            var file1 = $@"
namespace Foo
{{
    public class Bar
    {{}}
}}";
            var file2 = @"
namespace Baz
{
    class Bat
    {
         $$
    }
}";
            var markup = CreateMarkupForProjectWithAliasedMetadataReference(file2, "alias1", file1, LanguageNames.CSharp, LanguageNames.CSharp, hasGlobalAlias: false);
            await VerifyTypeImportItemIsAbsentAsync(markup, "Bar", inlineDescription: "Foo");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task Show_TopLevel_Public_NoImport_InGlobalAliasedMetadataReference()
        {
            var file1 = $@"
namespace Foo
{{
    public class Bar
    {{}}
}}";
            var file2 = @"
namespace Baz
{
    class Bat
    {
         $$
    }
}";
            var markup = CreateMarkupForProjectWithAliasedMetadataReference(file2, "alias1", file1, LanguageNames.CSharp, LanguageNames.CSharp, hasGlobalAlias: true);
            await VerifyTypeImportItemExistsAsync(markup, "Bar", glyph: (int)Glyph.ClassPublic, inlineDescription: "Foo");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DoNotShow_TopLevel_Public_NoImport_InNonGlobalAliasedProjectReference()
        {
            var file1 = $@"
namespace Foo
{{
    public class Bar
    {{}}
}}";
            var file2 = @"
namespace Baz
{
    class Bat
    {
         $$
    }
}";
            var markup = CreateMarkupForProjecWithAliasedProjectReference(file2, "alias1", file1, LanguageNames.CSharp, LanguageNames.CSharp);
            await VerifyTypeImportItemIsAbsentAsync(markup, "Bar", inlineDescription: "Foo");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ShorterTypeNameShouldShowBeforeLongerTypeName()
        {
            var file1 = $@"
namespace Foo
{{
    public class SomeType
    {{}} 
    public class SomeTypeWithLongerName
    {{}}
}}";
            var file2 = @"
namespace Baz
{
    class Bat
    {
         $$
    }
}";
            var markup = CreateMarkupForSingleProject(file2, file1, LanguageNames.CSharp);
            var completionList = await GetCompletionListAsync(markup).ConfigureAwait(false);
            AssertRelativeOrder(new List<string>() { "SomeType", "SomeTypeWithLongerName" }, completionList.Items);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(35540, "https://github.com/dotnet/roslyn/issues/35540")]
        public async Task AttributeTypeInAttributeNameContext()
        {
            var file1 = @"
namespace Foo
{
    public class MyAttribute : System.Attribute { }
    public class MyAttributeWithoutSuffix : System.Attribute { }
    public class MyClass { }
}";

            var file2 = @"
namespace Test
{
    [$$
    class Program { }
}";
            var markup = CreateMarkupForSingleProject(file2, file1, LanguageNames.CSharp);

            await VerifyTypeImportItemExistsAsync(markup, "My", glyph: (int)Glyph.ClassPublic, inlineDescription: "Foo", expectedDescriptionOrNull: "class Foo.MyAttribute");
            await VerifyTypeImportItemIsAbsentAsync(markup, "MyAttributeWithoutSuffix", inlineDescription: "Foo");  // We intentionally ignore attribute types without proper suffix for perf reason
            await VerifyTypeImportItemIsAbsentAsync(markup, "MyAttribute", inlineDescription: "Foo");
            await VerifyTypeImportItemIsAbsentAsync(markup, "MyClass", inlineDescription: "Foo");
        }

        [InlineData(SourceCodeKind.Regular)]
        [InlineData(SourceCodeKind.Script)]
        [WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(35540, "https://github.com/dotnet/roslyn/issues/35540")]
        public async Task CommitAttributeTypeInAttributeNameContext(SourceCodeKind kind)
        {
            var file1 = @"
namespace Foo
{
    public class MyAttribute : System.Attribute { }
}";

            var file2 = @"
namespace Test
{
    [$$
    class Program { }
}";

            string expectedCodeAfterCommit = @"
using Foo;

namespace Test
{
    [My$$
    class Program { }
}";

            var markup = CreateMarkupForSingleProject(file2, file1, LanguageNames.CSharp);
            await VerifyCustomCommitProviderAsync(markup, "My", expectedCodeAfterCommit, sourceCodeKind: kind);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(35540, "https://github.com/dotnet/roslyn/issues/35540")]
        public async Task AttributeTypeInNonAttributeNameContext()
        {
            var file1 = @"
namespace Foo
{
    public class MyAttribute : System.Attribute { }
    public class MyAttributeWithoutSuffix : System.Attribute { }
    public class MyClass { }
}";

            var file2 = @"
namespace Test
{
    class Program 
    {
        $$
    }
}";
            var markup = CreateMarkupForSingleProject(file2, file1, LanguageNames.CSharp);

            await VerifyTypeImportItemExistsAsync(markup, "MyAttribute", glyph: (int)Glyph.ClassPublic, inlineDescription: "Foo", expectedDescriptionOrNull: "class Foo.MyAttribute");
            await VerifyTypeImportItemExistsAsync(markup, "MyAttributeWithoutSuffix", glyph: (int)Glyph.ClassPublic, inlineDescription: "Foo", expectedDescriptionOrNull: "class Foo.MyAttributeWithoutSuffix");
            await VerifyTypeImportItemIsAbsentAsync(markup, "My", inlineDescription: "Foo");
            await VerifyTypeImportItemExistsAsync(markup, "MyClass", glyph: (int)Glyph.ClassPublic, inlineDescription: "Foo", expectedDescriptionOrNull: "class Foo.MyClass");
        }

        [InlineData(SourceCodeKind.Regular)]
        [InlineData(SourceCodeKind.Script)]
        [WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(35540, "https://github.com/dotnet/roslyn/issues/35540")]
        public async Task CommitAttributeTypeInNonAttributeNameContext(SourceCodeKind kind)
        {
            var file1 = @"
namespace Foo
{
    public class MyAttribute : System.Attribute { }
}";

            var file2 = @"
namespace Test
{
    class Program 
    {
        $$
    }
}";

            string expectedCodeAfterCommit = @"
using Foo;

namespace Test
{
    class Program 
    {
        MyAttribute$$
    }
}";
            var markup = CreateMarkupForSingleProject(file2, file1, LanguageNames.CSharp);
            await VerifyCustomCommitProviderAsync(markup, "MyAttribute", expectedCodeAfterCommit, sourceCodeKind: kind);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(35540, "https://github.com/dotnet/roslyn/issues/35540")]
        public async Task AttributeTypeWithoutSuffixInAttributeNameContext()
        {
            // attribute suffix isn't capitalized
            var file1 = @"
namespace Foo
{
    public class Myattribute : System.Attribute { }
    public class MyClass { }
}";

            var file2 = @"
namespace Test
{
    [$$
    class Program { }
}";
            var markup = CreateMarkupForSingleProject(file2, file1, LanguageNames.CSharp);

            await VerifyTypeImportItemExistsAsync(markup, "Myattribute", glyph: (int)Glyph.ClassPublic, inlineDescription: "Foo", expectedDescriptionOrNull: "class Foo.Myattribute");
            await VerifyTypeImportItemIsAbsentAsync(markup, "My", inlineDescription: "Foo");
            await VerifyTypeImportItemIsAbsentAsync(markup, "MyClass", inlineDescription: "Foo");
        }

        [InlineData(SourceCodeKind.Regular)]
        [InlineData(SourceCodeKind.Script)]
        [WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(35540, "https://github.com/dotnet/roslyn/issues/35540")]
        public async Task CommitAttributeTypeWithoutSuffixInAttributeNameContext(SourceCodeKind kind)
        {
            // attribute suffix isn't capitalized
            var file1 = @"
namespace Foo
{
    public class Myattribute : System.Attribute { }
}";

            var file2 = @"
namespace Test
{
    [$$
    class Program { }
}";

            string expectedCodeAfterCommit = @"
using Foo;

namespace Test
{
    [Myattribute$$
    class Program { }
}";

            var markup = CreateMarkupForSingleProject(file2, file1, LanguageNames.CSharp);
            await VerifyCustomCommitProviderAsync(markup, "Myattribute", expectedCodeAfterCommit, sourceCodeKind: kind);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(35540, "https://github.com/dotnet/roslyn/issues/35540")]
        public async Task AttributeTypeWithoutSuffixInNonAttributeNameContext()
        {
            // attribute suffix isn't capitalized
            var file1 = @"
namespace Foo
{
    public class Myattribute : System.Attribute { }
    public class MyClass { }
}";

            var file2 = @"
namespace Test
{
    class Program 
    {
        $$
    }
}";
            var markup = CreateMarkupForSingleProject(file2, file1, LanguageNames.CSharp);

            await VerifyTypeImportItemExistsAsync(markup, "Myattribute", glyph: (int)Glyph.ClassPublic, inlineDescription: "Foo", expectedDescriptionOrNull: "class Foo.Myattribute");
            await VerifyTypeImportItemIsAbsentAsync(markup, "My", inlineDescription: "Foo");
            await VerifyTypeImportItemExistsAsync(markup, "MyClass", glyph: (int)Glyph.ClassPublic, inlineDescription: "Foo", expectedDescriptionOrNull: "class Foo.MyClass");
        }

        [InlineData(SourceCodeKind.Regular)]
        [InlineData(SourceCodeKind.Script)]
        [WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(35540, "https://github.com/dotnet/roslyn/issues/35540")]
        public async Task CommitAttributeTypeWithoutSuffixInNonAttributeNameContext(SourceCodeKind kind)
        {
            // attribute suffix isn't capitalized
            var file1 = @"
namespace Foo
{
    public class Myattribute : System.Attribute { }
}";

            var file2 = @"
namespace Test
{
    class Program 
    {
        $$
    }
}";

            string expectedCodeAfterCommit = @"
using Foo;

namespace Test
{
    class Program 
    {
        Myattribute$$
    }
}";
            var markup = CreateMarkupForSingleProject(file2, file1, LanguageNames.CSharp);
            await VerifyCustomCommitProviderAsync(markup, "Myattribute", expectedCodeAfterCommit, sourceCodeKind: kind);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(35540, "https://github.com/dotnet/roslyn/issues/35540")]
        public async Task VBAttributeTypeWithoutSuffixInAttributeNameContext()
        {
            var file1 = @"
Namespace Foo
    Public Class Myattribute
        Inherits System.Attribute
    End Class
    Public Class MyVBClass
    End Class
End Namespace";

            var file2 = @"
namespace Test
{
    [$$
    class Program 
    {
    }
}";

            var markup = CreateMarkupForProjecWithProjectReference(file2, file1, LanguageNames.CSharp, LanguageNames.VisualBasic);

            await VerifyTypeImportItemExistsAsync(markup, "Myattribute", glyph: (int)Glyph.ClassPublic, inlineDescription: "Foo", expectedDescriptionOrNull: "class Foo.Myattribute");
            await VerifyTypeImportItemIsAbsentAsync(markup, "My", inlineDescription: "Foo");
            await VerifyTypeImportItemIsAbsentAsync(markup, "MyVBClass", inlineDescription: "Foo");
        }

        [InlineData(SourceCodeKind.Regular)]
        [InlineData(SourceCodeKind.Script)]
        [WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37038, "https://github.com/dotnet/roslyn/issues/37038")]
        public async Task CommitTypeInUsingStaticContextShouldUseFullyQualifiedName(SourceCodeKind kind)
        {
            var file1 = @"
namespace Foo
{
    public class MyClass { }
}";

            var file2 = @"
using static $$";

            var expectedCodeAfterCommit = @"
using static Foo.MyClass$$";

            var markup = CreateMarkupForSingleProject(file2, file1, LanguageNames.CSharp);
            await VerifyCustomCommitProviderAsync(markup, "MyClass", expectedCodeAfterCommit, sourceCodeKind: kind);
        }

        [InlineData(SourceCodeKind.Regular)]
        [InlineData(SourceCodeKind.Script)]
        [WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37038, "https://github.com/dotnet/roslyn/issues/37038")]
        public async Task CommitGenericTypeParameterInUsingAliasContextShouldUseFullyQualifiedName(SourceCodeKind kind)
        {
            var file1 = @"
namespace Foo
{
    public class MyClass { }
}";

            var file2 = @"
using CollectionOfStringBuilders = System.Collections.Generic.List<$$>";

            var expectedCodeAfterCommit = @"
using CollectionOfStringBuilders = System.Collections.Generic.List<Foo.MyClass$$>";

            var markup = CreateMarkupForSingleProject(file2, file1, LanguageNames.CSharp);
            await VerifyCustomCommitProviderAsync(markup, "MyClass", expectedCodeAfterCommit, sourceCodeKind: kind);
        }

        [InlineData(SourceCodeKind.Regular)]
        [InlineData(SourceCodeKind.Script)]
        [WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(37038, "https://github.com/dotnet/roslyn/issues/37038")]
        public async Task CommitGenericTypeParameterInUsingAliasContextShouldUseFullyQualifiedName2(SourceCodeKind kind)
        {
            var file1 = @"
namespace Foo.Bar
{
    public class MyClass { }
}";

            var file2 = @"
namespace Foo
{
    using CollectionOfStringBuilders = System.Collections.Generic.List<$$>
}";

            // Completion is not fully qualified
            var expectedCodeAfterCommit = @"
namespace Foo
{
    using CollectionOfStringBuilders = System.Collections.Generic.List<Foo.Bar.MyClass$$>
}";

            var markup = CreateMarkupForSingleProject(file2, file1, LanguageNames.CSharp);
            await VerifyCustomCommitProviderAsync(markup, "MyClass", expectedCodeAfterCommit, sourceCodeKind: kind);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(36624, "https://github.com/dotnet/roslyn/issues/36624")]
        public async Task DoNotShowImportItemsIfTimeout()
        {
            // Set timeout to 0 so it always timeout
            TimeoutInMilliseconds = 0;

            var file1 = $@"
namespace NS1
{{
    public class Bar
    {{}}
}}";
            var file2 = @"
namespace NS2
{
    class C
    {
         $$
    }
}";

            var markup = CreateMarkupForSingleProject(file2, file1, LanguageNames.CSharp);
            await VerifyTypeImportItemIsAbsentAsync(markup, "Bar", inlineDescription: "NS1");
        }
        
        private static void AssertRelativeOrder(List<string> expectedTypesInRelativeOrder, ImmutableArray<CompletionItem> allCompletionItems)
        {
            var hashset = new HashSet<string>(expectedTypesInRelativeOrder);
            var actualTypesInRelativeOrder = allCompletionItems.Where(item => hashset.Contains(item.DisplayText)).Select(item => item.DisplayText).ToImmutableArray();

            Assert.Equal(expectedTypesInRelativeOrder.Count, actualTypesInRelativeOrder.Length);
            for (var i = 0; i < expectedTypesInRelativeOrder.Count; ++i)
            {
                Assert.Equal(expectedTypesInRelativeOrder[i], actualTypesInRelativeOrder[i]);
            }
        }

        private Task VerifyTypeImportItemExistsAsync(string markup, string expectedItem, int glyph, string inlineDescription, string displayTextSuffix = null, string expectedDescriptionOrNull = null)
        {
            return VerifyItemExistsAsync(markup, expectedItem, displayTextSuffix: displayTextSuffix, glyph: glyph, inlineDescription: inlineDescription, expectedDescriptionOrNull: expectedDescriptionOrNull);
        }


        private Task VerifyTypeImportItemIsAbsentAsync(string markup, string expectedItem, string inlineDescription, string displayTextSuffix = null)
        {
            return VerifyItemIsAbsentAsync(markup, expectedItem, displayTextSuffix: displayTextSuffix, inlineDescription: inlineDescription);
        }
    }
}
