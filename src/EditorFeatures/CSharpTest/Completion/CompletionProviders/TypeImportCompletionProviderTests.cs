// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.Completion)]
public class TypeImportCompletionProviderTests : AbstractCSharpCompletionProviderTests
{
    internal override Type GetCompletionProviderType()
        => typeof(TypeImportCompletionProvider);

    public TypeImportCompletionProviderTests()
    {
        ShowImportCompletionItemsOptionValue = true;
        ForceExpandedCompletionIndexCreation = true;
    }

    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public async Task OptionSetToTrue(bool? optionValue)
    {
        ShowImportCompletionItemsOptionValue = optionValue;

        var markup = @"
class Bar
{
     $$
}";

        if (!optionValue.HasValue || optionValue.Value)
        {
            await VerifyAnyItemExistsAsync(markup);
        }
        else
        {
            await VerifyNoItemsExistAsync(markup);
        }
    }

    #region "CompletionItem tests"

    [InlineData("class", (int)Glyph.ClassPublic)]
    [InlineData("record", (int)Glyph.ClassPublic)]
    [InlineData("struct", (int)Glyph.StructurePublic)]
    [InlineData("enum", (int)Glyph.EnumPublic)]
    [InlineData("interface", (int)Glyph.InterfacePublic)]
    [Theory]
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

    [InlineData("class", (int)Glyph.ClassPublic)]
    [InlineData("record", (int)Glyph.ClassPublic)]
    [InlineData("struct", (int)Glyph.StructurePublic)]
    [InlineData("enum", (int)Glyph.EnumPublic)]
    [InlineData("interface", (int)Glyph.InterfacePublic)]
    [Theory]
    public async Task Show_TopLevelStatement_NoImport_InProject(string typeKind, int glyph)
    {
        var file1 = $@"
namespace Foo
{{
    public {typeKind} Bar
    {{}}
}}";
        var file2 = @"
$$
";
        await VerifyTypeImportItemExistsAsync(
            CreateMarkupForSingleProject(file2, file1, LanguageNames.CSharp),
            "Bar",
            glyph: glyph,
            inlineDescription: "Foo");
    }

    [InlineData("class")]
    [InlineData("record")]
    [InlineData("struct")]
    [InlineData("enum")]
    [InlineData("interface")]
    [Theory]
    public async Task DoNotShow_TopLevel_SameNamespace_InProject(string typeKind)
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
    [InlineData("record", (int)Glyph.ClassPublic)]
    [InlineData("struct", (int)Glyph.StructurePublic)]
    [InlineData("interface", (int)Glyph.InterfacePublic)]
    [Theory]
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
    [InlineData("record")]
    [InlineData("struct")]
    [InlineData("enum")]
    [InlineData("interface")]
    [Theory]
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
    [InlineData("record")]
    [InlineData("struct")]
    [InlineData("enum")]
    [InlineData("interface")]
    [Theory]
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

    [Theory, CombinatorialData]
    public async Task Show_TopLevel_Public_NoImport_InReference(bool isProjectReference)
    {
        var file1 = $@"
namespace Foo
{{
    public class Bar
    {{}}

    public record Bar2
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
        await VerifyTypeImportItemExistsAsync(markup, "Bar2", glyph: (int)Glyph.ClassPublic, inlineDescription: "Foo");
    }

    [Theory, CombinatorialData]
    public async Task DoNotShow_TopLevel_Public_WithImport_InReference(bool isProjectReference)
    {
        var file1 = $@"
namespace Foo
{{
    public class Bar
    {{}}

    public record Bar2
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
        await VerifyTypeImportItemIsAbsentAsync(markup, "Bar2", inlineDescription: "Foo");
    }

    [Theory, CombinatorialData]
    public async Task DoNotShow_TopLevel_Internal_NoImport_InReference(bool isProjectReference)
    {
        var file1 = $@"
namespace Foo
{{
    internal class Bar
    {{}}

    internal record Bar2
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
        await VerifyTypeImportItemIsAbsentAsync(markup, "Bar2", inlineDescription: "Foo");
    }

    [Theory, CombinatorialData]
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

    [Theory, CombinatorialData]
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

    [Theory, CombinatorialData]
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

    [Theory, CombinatorialData]
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

    [Theory, CombinatorialData]
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

    [Theory, CombinatorialData]
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

    [Theory, CombinatorialData]
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

    [Theory, CombinatorialData]
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

    [Fact]
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
        var markup = CreateMarkupForProjecWithVBProjectReference(file2, file1, sourceLanguage: LanguageNames.CSharp, rootNamespace: "Foo");
        await VerifyTypeImportItemExistsAsync(markup, "Barr", glyph: (int)Glyph.ClassPublic, inlineDescription: "Foo.Bar");
    }

    [Fact]
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
        var markup = CreateMarkupForProjecWithVBProjectReference(file2, file1, sourceLanguage: LanguageNames.CSharp, rootNamespace: "");
        await VerifyTypeImportItemExistsAsync(markup, "Bar", glyph: (int)Glyph.ClassPublic, inlineDescription: "Na");
        await VerifyTypeImportItemExistsAsync(markup, "Foo", glyph: (int)Glyph.ClassPublic, inlineDescription: "Na");
        await VerifyTypeImportItemIsAbsentAsync(markup, "Bar", inlineDescription: "na");
        await VerifyTypeImportItemIsAbsentAsync(markup, "Foo", inlineDescription: "na");
    }

    [Fact]
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
        var markup = CreateMarkupForProjecWithVBProjectReference(file2, file1, sourceLanguage: LanguageNames.CSharp, rootNamespace: "");
        await VerifyTypeImportItemIsAbsentAsync(markup, "Bar", inlineDescription: "Na");
        await VerifyTypeImportItemIsAbsentAsync(markup, "Foo", inlineDescription: "Na");
        await VerifyTypeImportItemIsAbsentAsync(markup, "Bar", inlineDescription: "na");
        await VerifyTypeImportItemIsAbsentAsync(markup, "Foo", inlineDescription: "na");
    }

    [Fact]
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
        var markup = CreateMarkupForProjecWithVBProjectReference(file2, file1, sourceLanguage: LanguageNames.CSharp, rootNamespace: "Foo");
        await VerifyTypeImportItemIsAbsentAsync(markup, "Barr", inlineDescription: "Foo.Bar");
    }

    [Fact]
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
        var markup = CreateMarkupForProjecWithVBProjectReference(file2, file1, sourceLanguage: LanguageNames.CSharp, rootNamespace: "Foo");
        await VerifyTypeImportItemIsAbsentAsync(markup, "Barr", inlineDescription: "Foo.Bar");
    }

    [Theory, CombinatorialData]
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

    [Theory, CombinatorialData]
    public async Task TestNoCompletionItemWhenThereIsAlias(bool isProjectReference)
    {
        var file1 = @"
using AliasFoo1 = Foo1.Foo2.Foo3.Foo4;
using AliasFoo2 = Foo1.Foo2.Foo3.Foo4.Foo6;

namespace Bar
{
    using AliasFoo3 = Foo1.Foo2.Foo3.Foo5;
    using AliasFoo4 = Foo1.Foo2.Foo3.Foo5.Foo7;
    public class CC
    {
        public static void Main()
        {    
            F$$
        }
    }
}";
        var file2 = @"
namespace Foo1
{
    namespace Foo2
    {
        namespace Foo3
        {
            public class Foo4
            {
                public class Foo6
                {
                }
            }

            public class Foo5
            {
                public class Foo7
                {
                }
            }
        }
    }
}";

        var markup = GetMarkupWithReference(file1, file2, LanguageNames.CSharp, LanguageNames.CSharp, isProjectReference);
        await VerifyTypeImportItemIsAbsentAsync(markup, "Foo4", "Foo1.Foo2.Foo3");
        await VerifyTypeImportItemIsAbsentAsync(markup, "Foo6", "Foo1.Foo2.Foo3");
        await VerifyTypeImportItemIsAbsentAsync(markup, "Foo5", "Foo1.Foo2.Foo3");
        await VerifyTypeImportItemIsAbsentAsync(markup, "Foo7", "Foo1.Foo2.Foo3");
    }

    [Theory, CombinatorialData]
    public async Task TestAttributesAlias(bool isProjectReference)
    {
        var file1 = @"
using myAlias = Foo.BarAttribute;
using myAlia2 = Foo.BarAttributeDifferentEnding;

namespace Foo2
{
    public class Main
    {
        $$
    }
}";

        var file2 = @"
namespace Foo
{
    public class BarAttribute: System.Attribute
    {
    }

    public class BarAttributeDifferentEnding: System.Attribute
    {
    }
}";

        var markup = GetMarkupWithReference(file1, file2, LanguageNames.CSharp, LanguageNames.CSharp, isProjectReference);
        await VerifyTypeImportItemIsAbsentAsync(markup, "Bar", "Foo");
        await VerifyTypeImportItemIsAbsentAsync(markup, "BarAttribute", "Foo");
        await VerifyTypeImportItemIsAbsentAsync(markup, "BarAttributeDifferentEnding", "Foo");
    }

    [Theory, CombinatorialData]
    public async Task TestGenericsAliasHasNoEffect(bool isProjectReference)
    {
        var file1 = @"
using AliasFoo1 = Foo1.Foo2.Foo3.Foo4<int>;

namespace Bar
{
    using AliasFoo2 = Foo1.Foo2.Foo3.Foo5<string>;
    public class CC
    {
        public static void Main()
        {    
            F$$
        }
    }
}";
        var file2 = @"
namespace Foo1
{
    namespace Foo2
    {
        namespace Foo3
        {
            public class Foo4<T>
            {
            }

            public class Foo5<U>
            {
            }
        }
    }
}";

        var markup = GetMarkupWithReference(file1, file2, LanguageNames.CSharp, LanguageNames.CSharp, isProjectReference);
        await VerifyTypeImportItemExistsAsync(markup, "Foo4", (int)Glyph.ClassPublic, "Foo1.Foo2.Foo3", displayTextSuffix: "<>");
        await VerifyTypeImportItemExistsAsync(markup, "Foo5", (int)Glyph.ClassPublic, "Foo1.Foo2.Foo3", displayTextSuffix: "<>");
    }

    #endregion

    #region "Commit Change Tests"

    [InlineData(SourceCodeKind.Regular)]
    [InlineData(SourceCodeKind.Script)]
    [WpfTheory]
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
    [WpfTheory]
    public async Task Commit_TopLevelStatement_NoImport_InProject(SourceCodeKind kind)
    {
        var file1 = $@"
namespace Foo
{{
    public class Bar
    {{
    }}
}}";

        var file2 = @"
$$
";
        var expectedCodeAfterCommit = @"using Foo;


Bar$$
";
        var markup = CreateMarkupForSingleProject(file2, file1, LanguageNames.CSharp);
        await VerifyCustomCommitProviderAsync(markup, "Bar", expectedCodeAfterCommit, sourceCodeKind: kind);
    }

    [InlineData(SourceCodeKind.Regular)]
    [InlineData(SourceCodeKind.Script)]
    [WpfTheory]
    public async Task Commit_TopLevelStatement_UnrelatedImport_InProject(SourceCodeKind kind)
    {
        var file1 = $@"
namespace Foo
{{
    public class Bar
    {{
    }}
}}";

        var file2 = @"
using System;

$$
";
        var expectedCodeAfterCommit = @"
using System;
using Foo;

Bar$$
";
        var markup = CreateMarkupForSingleProject(file2, file1, LanguageNames.CSharp);
        await VerifyCustomCommitProviderAsync(markup, "Bar", expectedCodeAfterCommit, sourceCodeKind: kind);
    }

    [InlineData(SourceCodeKind.Regular)]
    [InlineData(SourceCodeKind.Script)]
    [WpfTheory]
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
}";
        var expectedCodeAfterCommit = @"
using Foo.Bar;

namespace Baz
{
    class Bat
    {
        Barr$$
    }
}";
        var markup = CreateMarkupForProjecWithVBProjectReference(file2, file1, sourceLanguage: LanguageNames.CSharp, rootNamespace: "Foo");
        await VerifyCustomCommitProviderAsync(markup, "Barr", expectedCodeAfterCommit, sourceCodeKind: kind);
    }

    [InlineData(SourceCodeKind.Regular)]
    [InlineData(SourceCodeKind.Script)]
    [WpfTheory]
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

    [Fact]
    public async Task DoNotShow_TopLevel_Public_NoImport_InNonGlobalAliasedMetadataReference()
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

    [Fact]
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

    [Fact]
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
        var markup = CreateMarkupForProjectWithAliasedProjectReference(file2, "alias1", file1, LanguageNames.CSharp, LanguageNames.CSharp);
        await VerifyTypeImportItemIsAbsentAsync(markup, "Bar", inlineDescription: "Foo");
    }

    [Fact]
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
        AssertRelativeOrder(["SomeType", "SomeTypeWithLongerName"], completionList.ItemsList.ToImmutableArray());
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35540")]
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

        await VerifyTypeImportItemExistsAsync(markup, "My", glyph: (int)Glyph.ClassPublic, inlineDescription: "Foo", expectedDescriptionOrNull: "class Foo.MyAttribute", flags: CompletionItemFlags.Expanded);
        await VerifyTypeImportItemIsAbsentAsync(markup, "MyAttributeWithoutSuffix", inlineDescription: "Foo");  // We intentionally ignore attribute types without proper suffix for perf reason
        await VerifyTypeImportItemIsAbsentAsync(markup, "MyAttribute", inlineDescription: "Foo");
        await VerifyTypeImportItemIsAbsentAsync(markup, "MyClass", inlineDescription: "Foo");
    }

    [InlineData(SourceCodeKind.Regular)]
    [InlineData(SourceCodeKind.Script)]
    [WpfTheory]
    [WorkItem("https://github.com/dotnet/roslyn/issues/35540")]
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

        var expectedCodeAfterCommit = @"
using Foo;

namespace Test
{
    [My$$
    class Program { }
}";

        var markup = CreateMarkupForSingleProject(file2, file1, LanguageNames.CSharp);
        await VerifyCustomCommitProviderAsync(markup, "My", expectedCodeAfterCommit, sourceCodeKind: kind);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35540")]
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

        await VerifyTypeImportItemExistsAsync(markup, "MyAttribute", glyph: (int)Glyph.ClassPublic, inlineDescription: "Foo", expectedDescriptionOrNull: "class Foo.MyAttribute", flags: CompletionItemFlags.CachedAndExpanded);
        await VerifyTypeImportItemExistsAsync(markup, "MyAttributeWithoutSuffix", glyph: (int)Glyph.ClassPublic, inlineDescription: "Foo", expectedDescriptionOrNull: "class Foo.MyAttributeWithoutSuffix", flags: CompletionItemFlags.CachedAndExpanded);
        await VerifyTypeImportItemIsAbsentAsync(markup, "My", inlineDescription: "Foo");
        await VerifyTypeImportItemExistsAsync(markup, "MyClass", glyph: (int)Glyph.ClassPublic, inlineDescription: "Foo", expectedDescriptionOrNull: "class Foo.MyClass", flags: CompletionItemFlags.CachedAndExpanded);
    }

    [InlineData(SourceCodeKind.Regular)]
    [InlineData(SourceCodeKind.Script)]
    [WpfTheory]
    [WorkItem("https://github.com/dotnet/roslyn/issues/35540")]
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

        var expectedCodeAfterCommit = @"
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35540")]
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

        await VerifyTypeImportItemExistsAsync(markup, "Myattribute", glyph: (int)Glyph.ClassPublic, inlineDescription: "Foo", expectedDescriptionOrNull: "class Foo.Myattribute", flags: CompletionItemFlags.CachedAndExpanded);
        await VerifyTypeImportItemIsAbsentAsync(markup, "My", inlineDescription: "Foo");
        await VerifyTypeImportItemIsAbsentAsync(markup, "MyClass", inlineDescription: "Foo");
    }

    [InlineData(SourceCodeKind.Regular)]
    [InlineData(SourceCodeKind.Script)]
    [WpfTheory]
    [WorkItem("https://github.com/dotnet/roslyn/issues/35540")]
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

        var expectedCodeAfterCommit = @"
using Foo;

namespace Test
{
    [Myattribute$$
    class Program { }
}";

        var markup = CreateMarkupForSingleProject(file2, file1, LanguageNames.CSharp);
        await VerifyCustomCommitProviderAsync(markup, "Myattribute", expectedCodeAfterCommit, sourceCodeKind: kind);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35540")]
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

        await VerifyTypeImportItemExistsAsync(markup, "Myattribute", glyph: (int)Glyph.ClassPublic, inlineDescription: "Foo", expectedDescriptionOrNull: "class Foo.Myattribute", flags: CompletionItemFlags.Expanded);
        await VerifyTypeImportItemIsAbsentAsync(markup, "My", inlineDescription: "Foo");
        await VerifyTypeImportItemExistsAsync(markup, "MyClass", glyph: (int)Glyph.ClassPublic, inlineDescription: "Foo", expectedDescriptionOrNull: "class Foo.MyClass", flags: CompletionItemFlags.CachedAndExpanded);
    }

    [InlineData(SourceCodeKind.Regular)]
    [InlineData(SourceCodeKind.Script)]
    [WpfTheory]
    [WorkItem("https://github.com/dotnet/roslyn/issues/35540")]
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

        var expectedCodeAfterCommit = @"
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35540")]
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

        var markup = CreateMarkupForProjectWithProjectReference(file2, file1, LanguageNames.CSharp, LanguageNames.VisualBasic);

        await VerifyTypeImportItemExistsAsync(markup, "Myattribute", glyph: (int)Glyph.ClassPublic, inlineDescription: "Foo", expectedDescriptionOrNull: "class Foo.Myattribute", flags: CompletionItemFlags.Expanded);
        await VerifyTypeImportItemIsAbsentAsync(markup, "My", inlineDescription: "Foo");
        await VerifyTypeImportItemIsAbsentAsync(markup, "MyVBClass", inlineDescription: "Foo");
    }

    [InlineData(SourceCodeKind.Regular)]
    [InlineData(SourceCodeKind.Script)]
    [WpfTheory]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37038")]
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
    [WpfTheory]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37038")]
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
    [WpfTheory]
    [WorkItem("https://github.com/dotnet/roslyn/issues/37038")]
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

    [Fact]
    [Trait(Traits.Feature, Traits.Features.Interactive)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/39027")]
    public async Task TriggerCompletionInSubsequentSubmission()
    {
        var markup = @"
                <Workspace>
                    <Submission Language=""C#"" CommonReferences=""true"">  
                        var x = ""10"";
                    </Submission>
                    <Submission Language=""C#"" CommonReferences=""true"">  
                        var y = $$
                    </Submission>
                </Workspace> ";

        var completionList = await GetCompletionListAsync(markup, workspaceKind: WorkspaceKind.Interactive).ConfigureAwait(false);
        Assert.NotEmpty(completionList.ItemsList);
    }

    [Fact]
    public async Task ShouldNotTriggerInsideTrivia()
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
    /// <summary>
    /// <see cref=""B$$""/>
    /// </summary>
    class Bat
    {
    }
}";
        var markup = CreateMarkupForSingleProject(file2, file1, LanguageNames.CSharp);
        await VerifyTypeImportItemIsAbsentAsync(markup, "Bar", inlineDescription: "Foo");
    }
    private static void AssertRelativeOrder(List<string> expectedTypesInRelativeOrder, ImmutableArray<CompletionItem> allCompletionItems)
    {
        var hashset = new HashSet<string>(expectedTypesInRelativeOrder);
        var actualTypesInRelativeOrder = allCompletionItems.SelectAsArray(item => hashset.Contains(item.DisplayText), item => item.DisplayText);

        Assert.Equal(expectedTypesInRelativeOrder.Count, actualTypesInRelativeOrder.Length);
        for (var i = 0; i < expectedTypesInRelativeOrder.Count; ++i)
        {
            Assert.Equal(expectedTypesInRelativeOrder[i], actualTypesInRelativeOrder[i]);
        }
    }

    [Theory, CombinatorialData]
    public async Task TestBrowsableAwaysFromReferences(bool isProjectReference)
    {
        var srcDoc = @"
class Program
{
    void M()
    {
        $$
    }
}";

        var refDoc = @"
namespace Foo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
    public class Goo
    {
    }
}";

        var markup = isProjectReference switch
        {
            true => CreateMarkupForProjectWithProjectReference(srcDoc, refDoc, LanguageNames.CSharp, LanguageNames.CSharp),
            false => CreateMarkupForProjectWithMetadataReference(srcDoc, refDoc, LanguageNames.CSharp, LanguageNames.CSharp)
        };

        await VerifyTypeImportItemExistsAsync(
                markup,
                "Goo",
                glyph: (int)Glyph.ClassPublic,
                inlineDescription: "Foo");
    }

    [Theory, CombinatorialData]
    public async Task TestBrowsableNeverFromReferences(bool isProjectReference)
    {
        var srcDoc = @"
class Program
{
    void M()
    {
        $$
    }
}";

        var refDoc = @"
namespace Foo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public class Goo
    {
    }
}";

        var (markup, shouldContainItem) = isProjectReference switch
        {
            true => (CreateMarkupForProjectWithProjectReference(srcDoc, refDoc, LanguageNames.CSharp, LanguageNames.CSharp), true),
            false => (CreateMarkupForProjectWithMetadataReference(srcDoc, refDoc, LanguageNames.CSharp, LanguageNames.CSharp), false),
        };

        if (shouldContainItem)
        {
            await VerifyTypeImportItemExistsAsync(
                    markup,
                    "Goo",
                    glyph: (int)Glyph.ClassPublic,
                    inlineDescription: "Foo");
        }
        else
        {
            await VerifyTypeImportItemIsAbsentAsync(
                    markup,
                    "Goo",
                    inlineDescription: "Foo");
        }
    }

    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    [Theory]
    public async Task TestBrowsableAdvancedFromReferences(bool isProjectReference, bool hideAdvancedMembers)
    {
        HideAdvancedMembers = hideAdvancedMembers;

        var srcDoc = @"
class Program
{
    void M()
    {
        $$
    }
}";

        var refDoc = @"
namespace Foo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    public class Goo
    {
    }
}";

        var (markup, shouldContainItem) = isProjectReference switch
        {
            true => (CreateMarkupForProjectWithProjectReference(srcDoc, refDoc, LanguageNames.CSharp, LanguageNames.CSharp), true),
            false => (CreateMarkupForProjectWithMetadataReference(srcDoc, refDoc, LanguageNames.CSharp, LanguageNames.CSharp), !hideAdvancedMembers),
        };

        if (shouldContainItem)
        {
            await VerifyTypeImportItemExistsAsync(
                    markup,
                    "Goo",
                    glyph: (int)Glyph.ClassPublic,
                    inlineDescription: "Foo");
        }
        else
        {
            await VerifyTypeImportItemIsAbsentAsync(
                    markup,
                    "Goo",
                    inlineDescription: "Foo");
        }
    }

    [Theory]
    [InlineData('.')]
    [InlineData(';')]
    public async Task TestCommitWithCustomizedCommitCharForParameterlessConstructor(char commitChar)
    {
        var markup = @"
namespace AA
{
    public class C
    {
    }
}

namespace BB
{
    public class B
    {
        public void M()
        {
            var c = new $$
        }
    }
}";

        var expected = $@"
using AA;

namespace AA
{{
    public class C
    {{
    }}
}}

namespace BB
{{
    public class B
    {{
        public void M()
        {{
            var c = new C(){commitChar}
        }}
    }}
}}";
        await VerifyProviderCommitAsync(markup, "C", expected, commitChar: commitChar, sourceCodeKind: SourceCodeKind.Regular);
    }

    [Theory]
    [InlineData('.')]
    [InlineData(';')]
    public async Task TestCommitWithCustomizedCommitCharUnderNonObjectCreationContext(char commitChar)
    {
        var markup = @"
namespace AA
{
    public class C
    {
    }
}
namespace BB
{
    public class B
    {
        public void M()
        {
            $$
        }
    }
}";

        var expected = $@"
using AA;

namespace AA
{{
    public class C
    {{
    }}
}}
namespace BB
{{
    public class B
    {{
        public void M()
        {{
            C{commitChar}
        }}
    }}
}}";
        await VerifyProviderCommitAsync(markup, "C", expected, commitChar: commitChar, sourceCodeKind: SourceCodeKind.Regular);
    }

    [InlineData(SourceCodeKind.Regular)]
    [InlineData(SourceCodeKind.Script)]
    [WpfTheory]
    [WorkItem("https://github.com/dotnet/roslyn/issues/54493")]
    public async Task CommitInLocalFunctionContext(SourceCodeKind kind)
    {
        var markup = @"
namespace Foo
{
    public class MyClass { }
}

namespace Test
{
    class Program
    {
        public static void Main()
        {
            static $$
        }
    }
}";

        var expectedCodeAfterCommit = @"
using Foo;

namespace Foo
{
    public class MyClass { }
}

namespace Test
{
    class Program
    {
        public static void Main()
        {
            static MyClass
        }
    }
}";

        await VerifyProviderCommitAsync(markup, "MyClass", expectedCodeAfterCommit, commitChar: null, sourceCodeKind: kind);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58473")]
    public async Task TestGlobalUsingsInSdkAutoGeneratedFile()
    {
        var source = @"
using System;
$$";

        var globalUsings = @"
global using global::System;
global using global::System.Collections.Generic;
global using global::System.IO;
global using global::System.Linq;
global using global::System.Net.Http;
global using global::System.Threading;
global using global::System.Threading.Tasks;
";

        var markup = CreateMarkupForSingleProject(source, globalUsings, LanguageNames.CSharp, referencedFileName: "ProjectName.GlobalUsings.g.cs");
        await VerifyTypeImportItemIsAbsentAsync(markup, "Task", inlineDescription: "System.Threading.Tasks");
        await VerifyTypeImportItemIsAbsentAsync(markup, "Console", inlineDescription: "System");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58473")]
    public async Task TestGlobalUsingsInSameFile()
    {
        var source = @"
global using global::System;
global using global::System.Threading.Tasks;

$$";

        var markup = CreateMarkupForSingleProject(source, "", LanguageNames.CSharp);
        await VerifyTypeImportItemIsAbsentAsync(markup, "Console", inlineDescription: "System");
        await VerifyTypeImportItemIsAbsentAsync(markup, "Task", inlineDescription: "System.Threading.Tasks");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58473")]
    public async Task TestGlobalUsingsInUserDocument()
    {
        var source = @"
$$";

        var globalUsings = @"
global using global::System;
global using global::System.Collections.Generic;
global using global::System.IO;
global using global::System.Linq;
global using global::System.Net.Http;
global using global::System.Threading;
global using global::System.Threading.Tasks;
";

        var markup = CreateMarkupForSingleProject(source, globalUsings, LanguageNames.CSharp, referencedFileName: "GlobalUsings.cs");
        await VerifyTypeImportItemIsAbsentAsync(markup, "Task", inlineDescription: "System.Threading.Tasks");
        await VerifyTypeImportItemIsAbsentAsync(markup, "Console", inlineDescription: "System");
    }

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/65339")]
    public async Task TestFileScopedType(bool? isProjectReference)
    {
        var srcDoc = @"
class Program
{
    void M()
    {
        $$goo
    }
}";

        var refDoc = @"
namespace Foo
{
    file class Goo
    {
    }
}";

        string markup;
        if (isProjectReference.HasValue)
        {
            markup = isProjectReference switch
            {
                true => CreateMarkupForProjectWithProjectReference(srcDoc, refDoc, LanguageNames.CSharp, LanguageNames.CSharp),
                false => CreateMarkupForProjectWithMetadataReference(srcDoc, refDoc, LanguageNames.CSharp, LanguageNames.CSharp)
            };
        }
        else
        {
            markup = CreateMarkupForSingleProject(srcDoc, refDoc, LanguageNames.CSharp);
        }
        await VerifyTypeImportItemIsAbsentAsync(
                markup,
                "Goo",
                inlineDescription: "Foo");
    }

    [Fact]
    public async Task TestEnumBaseList1()
    {
        var source = """
            enum E : $$
            """;

        await VerifyTypeImportItemExistsAsync(source, "Byte", glyph: (int)Glyph.StructurePublic, inlineDescription: "System");
        await VerifyTypeImportItemExistsAsync(source, "SByte", glyph: (int)Glyph.StructurePublic, inlineDescription: "System");
        await VerifyTypeImportItemExistsAsync(source, "Int16", glyph: (int)Glyph.StructurePublic, inlineDescription: "System");
        await VerifyTypeImportItemExistsAsync(source, "UInt16", glyph: (int)Glyph.StructurePublic, inlineDescription: "System");
        await VerifyTypeImportItemExistsAsync(source, "Int32", glyph: (int)Glyph.StructurePublic, inlineDescription: "System");
        await VerifyTypeImportItemExistsAsync(source, "UInt32", glyph: (int)Glyph.StructurePublic, inlineDescription: "System");
        await VerifyTypeImportItemExistsAsync(source, "Int64", glyph: (int)Glyph.StructurePublic, inlineDescription: "System");
        await VerifyTypeImportItemExistsAsync(source, "UInt64", glyph: (int)Glyph.StructurePublic, inlineDescription: "System");

        // Verify that other things from `System` namespace are not present
        await VerifyTypeImportItemIsAbsentAsync(source, "Console", inlineDescription: "System");
        await VerifyTypeImportItemIsAbsentAsync(source, "Action", inlineDescription: "System");
        await VerifyTypeImportItemIsAbsentAsync(source, "DateTime", inlineDescription: "System");

        // Verify that things from other namespaces are not present
        await VerifyTypeImportItemIsAbsentAsync(source, "IEnumerable", inlineDescription: "System.Collections");
        await VerifyTypeImportItemIsAbsentAsync(source, "Task", inlineDescription: "System.Threading.Tasks");
        await VerifyTypeImportItemIsAbsentAsync(source, "AssemblyName", inlineDescription: "System.Reflection");
    }

    [Fact]
    public async Task TestEnumBaseList2()
    {
        var source = """
            using System;

            enum E : $$
            """;

        // Everything valid is already in the scope
        await VerifyNoItemsExistAsync(source);
    }

    private Task VerifyTypeImportItemExistsAsync(string markup, string expectedItem, int glyph, string inlineDescription, string displayTextSuffix = null, string expectedDescriptionOrNull = null, CompletionItemFlags? flags = null)
        => VerifyItemExistsAsync(markup, expectedItem, displayTextSuffix: displayTextSuffix, glyph: glyph, inlineDescription: inlineDescription, expectedDescriptionOrNull: expectedDescriptionOrNull, isComplexTextEdit: true, flags: flags);

    private Task VerifyTypeImportItemIsAbsentAsync(string markup, string expectedItem, string inlineDescription, string displayTextSuffix = null)
        => VerifyItemIsAbsentAsync(markup, expectedItem, displayTextSuffix: displayTextSuffix, inlineDescription: inlineDescription);
}
