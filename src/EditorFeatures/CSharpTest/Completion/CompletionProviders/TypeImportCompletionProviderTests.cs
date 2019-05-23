﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        protected override void SetWorkspaceOptions(TestWorkspace workspace)
        {
            workspace.Options = workspace.Options
                .WithChangedOption(CompletionOptions.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, ShowImportCompletionItemsOptionValue);
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

        private static string GetMarkupWithReference(string currentFile, string referencedFile, bool isProjectReference, string alias = null)
        {
            return isProjectReference
                ? CreateMarkupForProjecWithProjectReference(currentFile, referencedFile, LanguageNames.CSharp, LanguageNames.CSharp)
                : CreateMarkupForProjectWithMetadataReference(currentFile, referencedFile, LanguageNames.CSharp, LanguageNames.CSharp);
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
            var markup = GetMarkupWithReference(file2, file1, isProjectReference);
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
            var markup = GetMarkupWithReference(file2, file1, isProjectReference);
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
            var markup = GetMarkupWithReference(file2, file1, isProjectReference);
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
            var markup = GetMarkupWithReference(file2, file1, isProjectReference);
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
            var markup = GetMarkupWithReference(file2, file1, isProjectReference);
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
            var markup = GetMarkupWithReference(file2, file1, isProjectReference);
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
            var markup = GetMarkupWithReference(file2, file1, isProjectReference);
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
            var markup = GetMarkupWithReference(file2, file1, isProjectReference);
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
            var markup = GetMarkupWithReference(file2, file1, isProjectReference);
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
            var markup = GetMarkupWithReference(file2, file1, isProjectReference);
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
            var markup = GetMarkupWithReference(file2, file1, isProjectReference);
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
            var markup = GetMarkupWithReference(file2, file1, isProjectReference);
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

        private Task VerifyTypeImportItemExistsAsync(string markup, string expectedItem, int glyph, string inlineDescription, string displayTextSuffix = null)
        {
            return VerifyItemExistsAsync(markup, expectedItem, displayTextSuffix: displayTextSuffix, glyph: glyph, inlineDescription: inlineDescription);
        }


        private Task VerifyTypeImportItemIsAbsentAsync(string markup, string expectedItem, string inlineDescription, string displayTextSuffix = null)
        {
            return VerifyItemIsAbsentAsync(markup, expectedItem, displayTextSuffix: displayTextSuffix, inlineDescription: inlineDescription);
        }
    }
}
