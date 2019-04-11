// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
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
                .WithChangedOption(CompletionOptions.ShowImportCompletionItems, LanguageNames.CSharp, ShowImportCompletionItemsOptionValue);
        }

        #region "Option tests"

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task OptionSetToNull()
        {
            ShowImportCompletionItemsOptionValue = null;
            var markup = @"
class Bar
{
     $$
}";

            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task OptionSetToFalse()
        {
            ShowImportCompletionItemsOptionValue = false;
            var markup = @"
class Bar
{
     $$
}";

            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task OptionSetToTrue()
        {
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
            await VerifyItemExistsAsync(
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
            await VerifyItemIsAbsentAsync(
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
            await VerifyItemExistsAsync(markup, "Bar", glyph: glyph, inlineDescription: "Foo");
            await VerifyItemExistsAsync(markup, "Bar", displayTextSuffix: "<>", glyph: glyph, inlineDescription: "Foo");
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
            await VerifyItemIsAbsentAsync(markup, "Faz");
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
            await VerifyItemIsAbsentAsync(markup, "Bar", inlineDescription: "Foo");
        }

        private static string GetMarkupWithReference(string currentFile, string referencedFile, bool isProjectReference)
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
            await VerifyItemExistsAsync(markup, "Bar", glyph: (int)Glyph.ClassPublic, inlineDescription: "Foo");
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
            await VerifyItemIsAbsentAsync(markup, "Bar", inlineDescription: "Foo");
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
            await VerifyItemIsAbsentAsync(markup, "Bar", inlineDescription: "Foo");
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
            await VerifyItemIsAbsentAsync(markup, "Bar", displayTextSuffix: "", inlineDescription: "Foo");
            await VerifyItemExistsAsync(markup, "Bar", displayTextSuffix: "<>", glyph: (int)Glyph.ClassPublic, inlineDescription: "Foo");
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
            await VerifyItemIsAbsentAsync(markup, "Bar", displayTextSuffix: "", inlineDescription: "Foo");
            await VerifyItemIsAbsentAsync(markup, "Bar", displayTextSuffix: "<>", inlineDescription: "Foo");
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
            await VerifyItemExistsAsync(markup, "Bar", glyph: (int)Glyph.ClassInternal, inlineDescription: "Foo");
            await VerifyItemExistsAsync(markup, "Bar", displayTextSuffix: "<>", glyph: (int)Glyph.ClassPublic, inlineDescription: "Foo");
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
            await VerifyItemIsAbsentAsync(markup, "Bar", inlineDescription: "Foo");
            await VerifyItemIsAbsentAsync(markup, "Bar", displayTextSuffix: "<>", inlineDescription: "Foo");
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
            await VerifyItemExistsAsync(markup, "Bar", glyph: (int)Glyph.ClassPublic, inlineDescription: "Foo");
            await VerifyItemExistsAsync(markup, "Bar", displayTextSuffix: "<>", glyph: (int)Glyph.ClassPublic, inlineDescription: "Foo");
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
            await VerifyItemIsAbsentAsync(markup, "Bar", inlineDescription: "Foo");
            await VerifyItemIsAbsentAsync(markup, "Bar", displayTextSuffix: "<>", inlineDescription: "Foo");
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
            await VerifyItemExistsAsync(markup, "Bar", glyph: (int)Glyph.ClassInternal, inlineDescription: "Foo");
            await VerifyItemExistsAsync(markup, "Bar", displayTextSuffix: "<>", glyph: (int)Glyph.ClassInternal, inlineDescription: "Foo");
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
            await VerifyItemExistsAsync(markup, "Bar", glyph: (int)Glyph.ClassInternal, inlineDescription: "Foo");
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
            await VerifyItemExistsAsync(markup, "Barr", glyph: (int)Glyph.ClassPublic, inlineDescription: "Foo.Bar");
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
            await VerifyItemExistsAsync(markup, "Bar", glyph: (int)Glyph.ClassPublic, inlineDescription: "Na");
            await VerifyItemExistsAsync(markup, "Foo", glyph: (int)Glyph.ClassPublic, inlineDescription: "Na");
            await VerifyItemIsAbsentAsync(markup, "Bar", inlineDescription: "na");
            await VerifyItemIsAbsentAsync(markup, "Foo", inlineDescription: "na");
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
            await VerifyItemIsAbsentAsync(markup, "Bar", inlineDescription: "Na");
            await VerifyItemIsAbsentAsync(markup, "Foo", inlineDescription: "Na");
            await VerifyItemIsAbsentAsync(markup, "Bar", inlineDescription: "na");
            await VerifyItemIsAbsentAsync(markup, "Foo", inlineDescription: "na");
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
            await VerifyItemIsAbsentAsync(markup, "Barr", inlineDescription: "Foo.Bar");
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
            await VerifyItemIsAbsentAsync(markup, "Barr", inlineDescription: "Foo.Bar");
        }

        #endregion

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
    }
}
