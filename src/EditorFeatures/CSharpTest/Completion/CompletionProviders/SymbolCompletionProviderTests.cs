// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Composition;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionSetSources
{

    [UseExportProvider]
    public partial class SymbolCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        public SymbolCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        internal override CompletionProvider CreateCompletionProvider()
        {
            return new SymbolCompletionProvider();
        }

        protected override ExportProvider GetExportProvider()
        {
            return ExportProviderCache
                .GetOrCreateExportProviderFactory(TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithPart(typeof(TestExperimentationService)))
                .CreateExportProvider();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EmptyFile()
        {
            await VerifyItemIsAbsentAsync(@"$$", @"String", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Regular);
            await VerifyItemIsAbsentAsync(@"$$", @"System", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Regular);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EmptyFile_Interactive()
        {
            await VerifyItemIsAbsentAsync(@"$$", @"String", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
            await VerifyItemExistsAsync(@"$$", @"System", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EmptyFileWithUsing()
        {
            await VerifyItemIsAbsentAsync(@"using System;
$$", @"String", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Regular);
            await VerifyItemIsAbsentAsync(@"using System;
$$", @"System", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Regular);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EmptyFileWithUsing_Interactive()
        {
            await VerifyItemExistsAsync(@"using System;
$$", @"String", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
            await VerifyItemExistsAsync(@"using System;
$$", @"System", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotAfterHashR()
        {
            await VerifyItemIsAbsentAsync(@"#r $$", "@System", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotAfterHashLoad()
        {
            await VerifyItemIsAbsentAsync(@"#load $$", "@System", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task UsingDirective()
        {
            await VerifyItemIsAbsentAsync(@"using $$", @"String");
            await VerifyItemIsAbsentAsync(@"using $$ = System", @"System");
            await VerifyItemExistsAsync(@"using $$", @"System");
            await VerifyItemExistsAsync(@"using T = $$", @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InactiveRegion()
        {
            await VerifyItemIsAbsentAsync(@"class C {
#if false 
$$
#endif", @"String");
            await VerifyItemIsAbsentAsync(@"class C {
#if false 
$$
#endif", @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ActiveRegion()
        {
            await VerifyItemIsAbsentAsync(@"class C {
#if true 
$$
#endif", @"String");
            await VerifyItemExistsAsync(@"class C {
#if true 
$$
#endif", @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InactiveRegionWithUsing()
        {
            await VerifyItemIsAbsentAsync(@"using System;

class C {
#if false 
$$
#endif", @"String");
            await VerifyItemIsAbsentAsync(@"using System;

class C {
#if false 
$$
#endif", @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ActiveRegionWithUsing()
        {
            await VerifyItemExistsAsync(@"using System;

class C {
#if true 
$$
#endif", @"String");
            await VerifyItemExistsAsync(@"using System;

class C {
#if true 
$$
#endif", @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SingleLineComment1()
        {
            await VerifyItemIsAbsentAsync(@"using System;

class C {
// $$", @"String");
            await VerifyItemIsAbsentAsync(@"using System;

class C {
// $$", @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SingleLineComment2()
        {
            await VerifyItemIsAbsentAsync(@"using System;

class C {
// $$
", @"String");
            await VerifyItemIsAbsentAsync(@"using System;

class C {
// $$
", @"System");
            await VerifyItemIsAbsentAsync(@"using System;

class C {
  // $$
", @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MultiLineComment()
        {
            await VerifyItemIsAbsentAsync(@"using System;

class C {
/*  $$", @"String");
            await VerifyItemIsAbsentAsync(@"using System;

class C {
/*  $$", @"System");
            await VerifyItemIsAbsentAsync(@"using System;

class C {
/*  $$   */", @"String");
            await VerifyItemIsAbsentAsync(@"using System;

class C {
/*  $$   */", @"System");
            await VerifyItemExistsAsync(@"using System;

class C {
/*    */$$", @"System");
            await VerifyItemExistsAsync(@"using System;

class C {
/*    */$$
", @"System");
            await VerifyItemExistsAsync(@"using System;

class C {
  /*    */$$
", @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SingleLineXmlComment1()
        {
            await VerifyItemIsAbsentAsync(@"using System;

class C {
/// $$", @"String");
            await VerifyItemIsAbsentAsync(@"using System;

class C {
/// $$", @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SingleLineXmlComment2()
        {
            await VerifyItemIsAbsentAsync(@"using System;

class C {
/// $$
", @"String");
            await VerifyItemIsAbsentAsync(@"using System;

class C {
/// $$
", @"System");
            await VerifyItemIsAbsentAsync(@"using System;

class C {
  /// $$
", @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MultiLineXmlComment()
        {
            await VerifyItemIsAbsentAsync(@"using System;

class C {
/**  $$   */", @"String");
            await VerifyItemIsAbsentAsync(@"using System;

class C {
/**  $$   */", @"System");
            await VerifyItemExistsAsync(@"using System;

class C {
/**     */$$", @"System");
            await VerifyItemExistsAsync(@"using System;

class C {
/**     */$$
", @"System");
            await VerifyItemExistsAsync(@"using System;

class C {
  /**     */$$
", @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task OpenStringLiteral()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", AddInsideMethod("string s = \"$$")), @"String");
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", AddInsideMethod("string s = \"$$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task OpenStringLiteralInDirective()
        {
            await VerifyItemIsAbsentAsync("#r \"$$", "String", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
            await VerifyItemIsAbsentAsync("#r \"$$", "System", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task StringLiteral()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", AddInsideMethod("string s = \"$$\";")), @"System");
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", AddInsideMethod("string s = \"$$\";")), @"String");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task StringLiteralInDirective()
        {
            await VerifyItemIsAbsentAsync("#r \"$$\"", "String", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
            await VerifyItemIsAbsentAsync("#r \"$$\"", "System", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task OpenCharLiteral()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", AddInsideMethod("char c = '$$")), @"System");
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", AddInsideMethod("char c = '$$")), @"String");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AssemblyAttribute1()
        {
            await VerifyItemExistsAsync(@"[assembly: $$]", @"System");
            await VerifyItemIsAbsentAsync(@"[assembly: $$]", @"String");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AssemblyAttribute2()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"[assembly: $$]"), @"System");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"[assembly: $$]"), @"AttributeUsage");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SystemAttributeIsNotAnAttribute()
        {
            var content = @"[$$]
class CL {}";

            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", content), @"Attribute");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TypeAttribute()
        {
            var content = @"[$$]
class CL {}";

            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"AttributeUsage");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TypeParamAttribute()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"class CL<[A$$]T> {}"), @"AttributeUsage");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"class CL<[A$$]T> {}"), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MethodAttribute()
        {
            var content = @"class CL {
    [$$]
    void Method() {}
}";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"AttributeUsage");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MethodTypeParamAttribute()
        {
            var content = @"class CL{
    void Method<[A$$]T> () {}
}";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"AttributeUsage");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MethodParamAttribute()
        {
            var content = @"class CL{
    void Method ([$$]int i) {}
}";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"AttributeUsage");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NamespaceName_EmptyNameSpan_TopLevel()
        {
            var source = @"namespace $$ { }";

            await VerifyItemExistsAsync(source, "System", sourceCodeKind: SourceCodeKind.Regular);
        }

        [WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NamespaceName_EmptyNameSpan_Nested()
        {
            var source = @";
namespace System
{
    namespace $$ { }
}";

            await VerifyItemExistsAsync(source, "Runtime", sourceCodeKind: SourceCodeKind.Regular);
        }

        [WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NamespaceName_Unqualified_TopLevelNoPeers()
        {
            var source = @"using System;

namespace $$";

            await VerifyItemExistsAsync(source, "System", sourceCodeKind: SourceCodeKind.Regular);
            await VerifyItemIsAbsentAsync(source, "String", sourceCodeKind: SourceCodeKind.Regular);
        }

        [WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NamespaceName_Unqualified_TopLevelWithPeer()
        {
            var source = @"
namespace A { }

namespace $$";

            await VerifyItemExistsAsync(source, "A", sourceCodeKind: SourceCodeKind.Regular);
        }

        [WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NamespaceName_Unqualified_NestedWithNoPeers()
        {
            var source = @"
namespace A
{
    namespace $$
}";

            await VerifyNoItemsExistAsync(source, sourceCodeKind: SourceCodeKind.Regular);
        }

        [WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NamespaceName_Unqualified_NestedWithPeer()
        {
            var source = @"
namespace A
{
    namespace B { }

    namespace $$
}";

            await VerifyItemIsAbsentAsync(source, "A", sourceCodeKind: SourceCodeKind.Regular);
            await VerifyItemExistsAsync(source, "B", sourceCodeKind: SourceCodeKind.Regular);
        }

        [WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NamespaceName_Unqualified_ExcludesCurrentDeclaration()
        {
            var source = @"namespace N$$S";

            await VerifyItemIsAbsentAsync(source, "NS", sourceCodeKind: SourceCodeKind.Regular);
        }

        [WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NamespaceName_Unqualified_WithNested()
        {
            var source = @"
namespace A
{
    namespace $$
    {
        namespace B { }
    }
}";

            await VerifyItemIsAbsentAsync(source, "A", sourceCodeKind: SourceCodeKind.Regular);
            await VerifyItemIsAbsentAsync(source, "B", sourceCodeKind: SourceCodeKind.Regular);
        }

        [WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NamespaceName_Unqualified_WithNestedAndMatchingPeer()
        {
            var source = @"
namespace A.B { }

namespace A
{
    namespace $$
    {
        namespace B { }
    }
}";

            await VerifyItemIsAbsentAsync(source, "A", sourceCodeKind: SourceCodeKind.Regular);
            await VerifyItemExistsAsync(source, "B", sourceCodeKind: SourceCodeKind.Regular);
        }

        [WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NamespaceName_Unqualified_InnerCompletionPosition()
        {
            var source = @"namespace Sys$$tem { }";

            await VerifyItemExistsAsync(source, "System", sourceCodeKind: SourceCodeKind.Regular);
            await VerifyItemIsAbsentAsync(source, "Runtime", sourceCodeKind: SourceCodeKind.Regular);
        }

        [WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NamespaceName_Unqualified_IncompleteDeclaration()
        {
            var source = @"
namespace A
{
    namespace B
    {
        namespace $$

        namespace C1 { }
    }

    namespace B.C2 { }
}

namespace A.B.C3 { }";


            // Ideally, all the C* namespaces would be recommended but, because of how the parser
            // recovers from the missing braces, they end up with the following qualified names...
            //
            //     C1 => A.B.?.C1
            //     C2 => A.B.B.C2
            //     C3 => A.A.B.C3
            //
            // ...none of which are found by the current algorithm.
            await VerifyItemIsAbsentAsync(source, "C1", sourceCodeKind: SourceCodeKind.Regular);
            await VerifyItemIsAbsentAsync(source, "C2", sourceCodeKind: SourceCodeKind.Regular);
            await VerifyItemIsAbsentAsync(source, "C3", sourceCodeKind: SourceCodeKind.Regular);

            await VerifyItemIsAbsentAsync(source, "A", sourceCodeKind: SourceCodeKind.Regular);

            // Because of the above, B does end up in the completion list
            // since A.B.B appears to be a peer of the new declaration
            await VerifyItemExistsAsync(source, "B", sourceCodeKind: SourceCodeKind.Regular);
        }

        [WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NamespaceName_Qualified_NoPeers()
        {
            var source = @"namespace A.$$";

            await VerifyNoItemsExistAsync(source, sourceCodeKind: SourceCodeKind.Regular);
        }

        [WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NamespaceName_Qualified_TopLevelWithPeer()
        {
            var source = @"
namespace A.B { }

namespace A.$$";

            await VerifyItemExistsAsync(source, "B", sourceCodeKind: SourceCodeKind.Regular);
        }

        [WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NamespaceName_Qualified_NestedWithPeer()
        {
            var source = @"
namespace A
{
    namespace B.C { }

    namespace B.$$
}";

            await VerifyItemIsAbsentAsync(source, "A", sourceCodeKind: SourceCodeKind.Regular);
            await VerifyItemIsAbsentAsync(source, "B", sourceCodeKind: SourceCodeKind.Regular);
            await VerifyItemExistsAsync(source, "C", sourceCodeKind: SourceCodeKind.Regular);
        }

        [WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NamespaceName_Qualified_WithNested()
        {
            var source = @"
namespace A.$$
{
    namespace B { }
}
";

            await VerifyItemIsAbsentAsync(source, "A", sourceCodeKind: SourceCodeKind.Regular);
            await VerifyItemIsAbsentAsync(source, "B", sourceCodeKind: SourceCodeKind.Regular);
        }

        [WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NamespaceName_Qualified_WithNestedAndMatchingPeer()
        {
            var source = @"
namespace A.B { }

namespace A.$$
{
    namespace B { }
}
";

            await VerifyItemIsAbsentAsync(source, "A", sourceCodeKind: SourceCodeKind.Regular);
            await VerifyItemExistsAsync(source, "B", sourceCodeKind: SourceCodeKind.Regular);
        }

        [WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NamespaceName_Qualified_InnerCompletionPosition()
        {
            var source = @"namespace Sys$$tem.Runtime { }";

            await VerifyItemExistsAsync(source, "System", sourceCodeKind: SourceCodeKind.Regular);
            await VerifyItemIsAbsentAsync(source, "Runtime", sourceCodeKind: SourceCodeKind.Regular);
        }

        [WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NamespaceName_OnKeyword()
        {
            var source = @"name$$space System { }";

            await VerifyItemIsAbsentAsync(source, "System", sourceCodeKind: SourceCodeKind.Regular);
        }

        [WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NamespaceName_OnNestedKeyword()
        {
            var source = @"
namespace System
{
    name$$space Runtime { }
}";

            await VerifyItemIsAbsentAsync(source, "System", sourceCodeKind: SourceCodeKind.Regular);
            await VerifyItemIsAbsentAsync(source, "Runtime", sourceCodeKind: SourceCodeKind.Regular);
        }

        [WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NamespaceName_Qualified_IncompleteDeclaration()
        {
            var source = @"
namespace A
{
    namespace B
    {
        namespace C.$$

        namespace C.D1 { }
    }

    namespace B.C.D2 { }
}

namespace A.B.C.D3 { }";

            await VerifyItemIsAbsentAsync(source, "A", sourceCodeKind: SourceCodeKind.Regular);
            await VerifyItemIsAbsentAsync(source, "B", sourceCodeKind: SourceCodeKind.Regular);
            await VerifyItemIsAbsentAsync(source, "C", sourceCodeKind: SourceCodeKind.Regular);

            // Ideally, all the D* namespaces would be recommended but, because of how the parser
            // recovers from the missing braces, they end up with the following qualified names...
            //
            //     D1 => A.B.C.C.?.D1
            //     D2 => A.B.B.C.D2
            //     D3 => A.A.B.C.D3
            //
            // ...none of which are found by the current algorithm.
            await VerifyItemIsAbsentAsync(source, "D1", sourceCodeKind: SourceCodeKind.Regular);
            await VerifyItemIsAbsentAsync(source, "D2", sourceCodeKind: SourceCodeKind.Regular);
            await VerifyItemIsAbsentAsync(source, "D3", sourceCodeKind: SourceCodeKind.Regular);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task UnderNamespace()
        {
            await VerifyItemIsAbsentAsync(@"namespace NS { $$", @"String");
            await VerifyItemIsAbsentAsync(@"namespace NS { $$", @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task OutsideOfType1()
        {
            await VerifyItemIsAbsentAsync(@"namespace NS {
class CL {}
$$", @"String");
            await VerifyItemIsAbsentAsync(@"namespace NS {
class CL {}
$$", @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task OutsideOfType2()
        {
            var content = @"namespace NS {
class CL {}
$$";
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", content), @"String");
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CompletionInsideProperty()
        {
            var content = @"class C
{
    private string name;
    public string Name
    {
        set
        {
            name = $$";
            await VerifyItemExistsAsync(content, @"value");
            await VerifyItemExistsAsync(content, @"C");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterDot()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"[assembly: A.$$"), @"String");
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"[assembly: A.$$"), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task UsingAlias()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"using MyType = $$"), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"using MyType = $$"), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task IncompleteMember()
        {
            var content = @"class CL {
    $$
";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task IncompleteMemberAccessibility()
        {
            var content = @"class CL {
    public $$
";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task BadStatement()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = $$)c")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = $$)c")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TypeTypeParameter()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class CL<$$"), @"String");
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class CL<$$"), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TypeTypeParameterList()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class CL<T, $$"), @"String");
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class CL<T, $$"), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CastExpressionTypePart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = ($$)c")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = ($$)c")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ObjectCreationExpression()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = new $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = new $$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ArrayCreationExpression()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = new $$ [")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = new $$ [")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task StackAllocArrayCreationExpression()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = stackalloc $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = stackalloc $$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task FromClauseTypeOptPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from $$ c")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from $$ c")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task JoinClause()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C join $$ j")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C join $$ j")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DeclarationStatement()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"$$ i =")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"$$ i =")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task VariableDeclaration()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"fixed($$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"fixed($$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ForEachStatement()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"foreach($$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"foreach($$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ForEachStatementNoToken()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", AddInsideMethod(@"foreach $$")), @"String");
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", AddInsideMethod(@"foreach $$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CatchDeclaration()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"try {} catch($$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"try {} catch($$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task FieldDeclaration()
        {
            var content = @"class CL {
    $$ i";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EventFieldDeclaration()
        {
            var content = @"class CL {
    event $$";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ConversionOperatorDeclaration()
        {
            var content = @"class CL {
    explicit operator $$";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ConversionOperatorDeclarationNoToken()
        {
            var content = @"class CL {
    explicit $$";
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", content), @"String");
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PropertyDeclaration()
        {
            var content = @"class CL {
    $$ Prop {";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EventDeclaration()
        {
            var content = @"class CL {
    event $$ Event {";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task IndexerDeclaration()
        {
            var content = @"class CL {
    $$ this";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task Parameter()
        {
            var content = @"class CL {
    void Method($$";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ArrayType()
        {
            var content = @"class CL {
    $$ [";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PointerType()
        {
            var content = @"class CL {
    $$ *";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NullableType()
        {
            var content = @"class CL {
    $$ ?";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DelegateDeclaration()
        {
            var content = @"class CL {
    delegate $$";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MethodDeclaration()
        {
            var content = @"class CL {
    $$ M(";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task OperatorDeclaration()
        {
            var content = @"class CL {
    $$ operator";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ParenthesizedExpression()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"($$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"($$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InvocationExpression()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"$$(")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"$$(")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ElementAccessExpression()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"$$[")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"$$[")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task Argument()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"i[$$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"i[$$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CastExpressionExpressionPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"(c)$$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"(c)$$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task FromClauseInPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in $$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task LetClauseExpressionPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C let n = $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C let n = $$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task OrderingExpressionPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C orderby $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C orderby $$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SelectClauseExpressionPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C select $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C select $$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ExpressionStatement()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"$$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"$$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ReturnStatement()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"return $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"return $$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ThrowStatement()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"throw $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"throw $$")), @"System");
        }

        [WorkItem(760097, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/760097")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task YieldReturnStatement()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"yield return $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"yield return $$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ForEachStatementExpressionPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"foreach(T t in $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"foreach(T t in $$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task UsingStatementExpressionPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"using($$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"using($$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task LockStatement()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"lock($$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"lock($$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EqualsValueClause()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var i = $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var i = $$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ForStatementInitializersPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"for($$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"for($$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ForStatementConditionOptPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"for(i=0;$$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"for(i=0;$$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ForStatementIncrementorsPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"for(i=0;i>10;$$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"for(i=0;i>10;$$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DoStatementConditionPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"do {} while($$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"do {} while($$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task WhileStatementConditionPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"while($$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"while($$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ArrayRankSpecifierSizesPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"int [$$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"int [$$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PrefixUnaryExpression()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"+$$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"+$$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task PostfixUnaryExpression()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"$$++")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"$$++")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task BinaryExpressionLeftPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"$$ + 1")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"$$ + 1")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task BinaryExpressionRightPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"1 + $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"1 + $$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AssignmentExpressionLeftPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"$$ = 1")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"$$ = 1")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AssignmentExpressionRightPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"1 = $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"1 = $$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ConditionalExpressionConditionPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"$$? 1:")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"$$? 1:")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ConditionalExpressionWhenTruePart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"true? $$:")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"true? $$:")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ConditionalExpressionWhenFalsePart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"true? 1:$$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"true? 1:$$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task JoinClauseInExpressionPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C join p in $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C join p in $$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task JoinClauseLeftExpressionPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C join p in P on $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C join p in P on $$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task JoinClauseRightExpressionPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C join p in P on id equals $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C join p in P on id equals $$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task WhereClauseConditionPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C where $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C where $$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task GroupClauseGroupExpressionPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C group $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C group $$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task GroupClauseByExpressionPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C group g by $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C group g by $$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task IfStatement()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"if ($$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"if ($$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SwitchStatement()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"switch($$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"switch($$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SwitchLabelCase()
        {
            var content = @"switch(i) { case $$";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(content)), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(content)), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SwitchPatternLabelCase()
        {
            var content = @"switch(i) { case $$ when";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(content)), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(content)), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(33915, "https://github.com/dotnet/roslyn/issues/33915")]
        public async Task SwitchExpressionFirstBranch()
        {
            var content = @"i switch { $$";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(content)), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(content)), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(33915, "https://github.com/dotnet/roslyn/issues/33915")]
        public async Task SwitchExpressionSecondBranch()
        {
            var content = @"i switch { 1 => true, $$";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(content)), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(content)), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(33915, "https://github.com/dotnet/roslyn/issues/33915")]
        public async Task PositionalPatternFirstPosition()
        {
            var content = @"i is ($$";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(content)), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(content)), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(33915, "https://github.com/dotnet/roslyn/issues/33915")]
        public async Task PositionalPatternSecondPosition()
        {
            var content = @"i is (1, $$";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(content)), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(content)), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(33915, "https://github.com/dotnet/roslyn/issues/33915")]
        public async Task PropertyPatternFirstPosition()
        {
            var content = @"i is { P: $$";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(content)), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(content)), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(33915, "https://github.com/dotnet/roslyn/issues/33915")]
        public async Task PropertyPatternSecondPosition()
        {
            var content = @"i is { P1: 1, P2: $$";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(content)), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(content)), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InitializerExpression()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = new [] { $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = new [] { $$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TypeParameterConstraintClause()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"class CL<T> where T : $$"), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"class CL<T> where T : $$"), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TypeParameterConstraintClauseList()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"class CL<T> where T : A, $$"), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"class CL<T> where T : A, $$"), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TypeParameterConstraintClauseAnotherWhere()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class CL<T> where T : A where$$"), @"System");
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class CL<T> where T : A where$$"), @"String");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TypeSymbolOfTypeParameterConstraintClause1()
        {
            await VerifyItemExistsAsync(@"class CL<T> where $$", @"T");
            await VerifyItemExistsAsync(@"class CL{ delegate void F<T>() where $$} ", @"T");
            await VerifyItemExistsAsync(@"class CL{ void F<T>() where $$", @"T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TypeSymbolOfTypeParameterConstraintClause2()
        {
            await VerifyItemIsAbsentAsync(@"class CL<T> where $$", @"System");
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class CL<T> where $$"), @"String");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TypeSymbolOfTypeParameterConstraintClause3()
        {
            await VerifyItemIsAbsentAsync(@"class CL<T1> { void M<T2> where $$", @"T1");
            await VerifyItemExistsAsync(@"class CL<T1> { void M<T2>() where $$", @"T2");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task BaseList1()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"class CL : $$"), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"class CL : $$"), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task BaseList2()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"class CL : B, $$"), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"class CL : B, $$"), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task BaseListWhere()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class CL<T> : B where$$"), @"String");
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class CL<T> : B where$$"), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AliasedName()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", AddInsideMethod(@"global::$$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"global::$$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AliasedNamespace()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using S = System;", AddInsideMethod(@"S.$$")), @"String");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AliasedType()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using S = System.String;", AddInsideMethod(@"S.$$")), @"Empty");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ConstructorInitializer()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class C { C() : $$"), @"String");
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class C { C() : $$"), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task Typeof1()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"typeof($$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"typeof($$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task Typeof2()
        {
            await VerifyItemIsAbsentAsync(AddInsideMethod(@"var x = 0; typeof($$"), @"x");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task Sizeof1()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"sizeof($$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"sizeof($$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task Sizeof2()
        {
            await VerifyItemIsAbsentAsync(AddInsideMethod(@"var x = 0; sizeof($$"), @"x");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task Default1()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"default($$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"default($$")), @"System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task Default2()
        {
            await VerifyItemIsAbsentAsync(AddInsideMethod(@"var x = 0; default($$"), @"x");
        }

        [WorkItem(543819, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543819")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task Checked()
        {
            await VerifyItemExistsAsync(AddInsideMethod(@"var x = 0; checked($$"), @"x");
        }

        [WorkItem(543819, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543819")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task Unchecked()
        {
            await VerifyItemExistsAsync(AddInsideMethod(@"var x = 0; unchecked($$"), @"x");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task Locals()
        {
            await VerifyItemExistsAsync(@"class c { void M() { string goo; $$", "goo");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task Parameters()
        {
            await VerifyItemExistsAsync(@"class c { void M(string args) { $$", "args");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CommonTypesInNewExpressionContext()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"class c { void M() { new $$"), "Exception");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoCompletionForUnboundTypes()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class c { void M() { goo.$$"), "Equals");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoParametersInTypeOf()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class c { void M(int x) { typeof($$"), "x");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoParametersInDefault()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class c { void M(int x) { default($$"), "x");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoParametersInSizeOf()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"public class C { void M(int x) { unsafe { sizeof($$"), "x");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoParametersInGenericParameterList()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"public class Generic<T> { void M(int x) { Generic<$$"), "x");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoMembersAfterNullLiteral()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"public class C { void M() { null.$$"), "Equals");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MembersAfterTrueLiteral()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { true.$$"), "Equals");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MembersAfterFalseLiteral()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { false.$$"), "Equals");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MembersAfterCharLiteral()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { 'c'.$$"), "Equals");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MembersAfterStringLiteral()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { """".$$"), "Equals");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MembersAfterVerbatimStringLiteral()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { @"""".$$"), "Equals");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MembersAfterNumericLiteral()
        {
            // NOTE: the Completion command handler will suppress this case if the user types '.',
            // but we still need to show members if the user specifically invokes statement completion here.
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { 2.$$"), "Equals");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoMembersAfterParenthesizedNullLiteral()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"public class C { void M() { (null).$$"), "Equals");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MembersAfterParenthesizedTrueLiteral()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { (true).$$"), "Equals");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MembersAfterParenthesizedFalseLiteral()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { (false).$$"), "Equals");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MembersAfterParenthesizedCharLiteral()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { ('c').$$"), "Equals");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MembersAfterParenthesizedStringLiteral()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { ("""").$$"), "Equals");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MembersAfterParenthesizedVerbatimStringLiteral()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { (@"""").$$"), "Equals");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MembersAfterParenthesizedNumericLiteral()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { (2).$$"), "Equals");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MembersAfterArithmeticExpression()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { (1 + 1).$$"), "Equals");
        }

        [WorkItem(539332, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539332")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InstanceTypesAvailableInUsingAlias()
        {
            await VerifyItemExistsAsync(@"using S = System.$$", "String");
        }

        [WorkItem(539812, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539812")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InheritedMember1()
        {
            var markup = @"
class A
{
    private void Hidden() { }
    protected void Goo() { }
}
class B : A
{
    void Bar()
    {
        $$
    }
}
";
            await VerifyItemIsAbsentAsync(markup, "Hidden");
            await VerifyItemExistsAsync(markup, "Goo");
        }

        [WorkItem(539812, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539812")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InheritedMember2()
        {
            var markup = @"
class A
{
    private void Hidden() { }
    protected void Goo() { }
}
class B : A
{
    void Bar()
    {
        this.$$
    }
}
";
            await VerifyItemIsAbsentAsync(markup, "Hidden");
            await VerifyItemExistsAsync(markup, "Goo");
        }

        [WorkItem(539812, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539812")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InheritedMember3()
        {
            var markup = @"
class A
{
    private void Hidden() { }
    protected void Goo() { }
}
class B : A
{
    void Bar()
    {
        base.$$
    }
}
";
            await VerifyItemIsAbsentAsync(markup, "Hidden");
            await VerifyItemExistsAsync(markup, "Goo");
            await VerifyItemIsAbsentAsync(markup, "Bar");
        }

        [WorkItem(539812, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539812")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InheritedStaticMember1()
        {
            var markup = @"
class A
{
    private static void Hidden() { }
    protected static void Goo() { }
}
class B : A
{
    void Bar()
    {
        $$
    }
}
";
            await VerifyItemIsAbsentAsync(markup, "Hidden");
            await VerifyItemExistsAsync(markup, "Goo");
        }

        [WorkItem(539812, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539812")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InheritedStaticMember2()
        {
            var markup = @"
class A
{
    private static void Hidden() { }
    protected static void Goo() { }
}
class B : A
{
    void Bar()
    {
        B.$$
    }
}
";
            await VerifyItemIsAbsentAsync(markup, "Hidden");
            await VerifyItemExistsAsync(markup, "Goo");
        }

        [WorkItem(539812, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539812")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InheritedStaticMember3()
        {
            var markup = @"
class A
{
     private static void Hidden() { }
     protected static void Goo() { }
}
class B : A
{
    void Bar()
    {
        A.$$
    }
}
";
            await VerifyItemIsAbsentAsync(markup, "Hidden");
            await VerifyItemExistsAsync(markup, "Goo");
        }

        [WorkItem(539812, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539812")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InheritedInstanceAndStaticMembers()
        {
            var markup = @"
class A
{
     private static void HiddenStatic() { }
     protected static void GooStatic() { }

     private void HiddenInstance() { }
     protected void GooInstance() { }
}
class B : A
{
    void Bar()
    {
        $$
    }
}
";
            await VerifyItemIsAbsentAsync(markup, "HiddenStatic");
            await VerifyItemExistsAsync(markup, "GooStatic");
            await VerifyItemIsAbsentAsync(markup, "HiddenInstance");
            await VerifyItemExistsAsync(markup, "GooInstance");
        }

        [WorkItem(540155, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540155")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ForLoopIndexer1()
        {
            var markup = @"
class C
{
    void M()
    {
        for (int i = 0; $$
";
            await VerifyItemExistsAsync(markup, "i");
        }

        [WorkItem(540155, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540155")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ForLoopIndexer2()
        {
            var markup = @"
class C
{
    void M()
    {
        for (int i = 0; i < 10; $$
";
            await VerifyItemExistsAsync(markup, "i");
        }

        [WorkItem(540012, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540012")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoInstanceMembersAfterType1()
        {
            var markup = @"
class C
{
    void M()
    {
        System.IDisposable.$$
";

            await VerifyItemIsAbsentAsync(markup, "Dispose");
        }

        [WorkItem(540012, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540012")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoInstanceMembersAfterType2()
        {
            var markup = @"
class C
{
    void M()
    {
        (System.IDisposable).$$
";
            await VerifyItemIsAbsentAsync(markup, "Dispose");
        }

        [WorkItem(540012, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540012")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoInstanceMembersAfterType3()
        {
            var markup = @"
using System;
class C
{
    void M()
    {
        IDisposable.$$
";

            await VerifyItemIsAbsentAsync(markup, "Dispose");
        }

        [WorkItem(540012, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540012")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoInstanceMembersAfterType4()
        {
            var markup = @"
using System;
class C
{
    void M()
    {
        (IDisposable).$$
";

            await VerifyItemIsAbsentAsync(markup, "Dispose");
        }

        [WorkItem(540012, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540012")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task StaticMembersAfterType1()
        {
            var markup = @"
class C
{
    void M()
    {
        System.IDisposable.$$
";

            await VerifyItemExistsAsync(markup, "ReferenceEquals");
        }

        [WorkItem(540012, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540012")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task StaticMembersAfterType2()
        {
            var markup = @"
class C
{
    void M()
    {
        (System.IDisposable).$$
";
            await VerifyItemIsAbsentAsync(markup, "ReferenceEquals");
        }

        [WorkItem(540012, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540012")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task StaticMembersAfterType3()
        {
            var markup = @"
using System;
class C
{
    void M()
    {
        IDisposable.$$
";

            await VerifyItemExistsAsync(markup, "ReferenceEquals");
        }

        [WorkItem(540012, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540012")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task StaticMembersAfterType4()
        {
            var markup = @"
using System;
class C
{
    void M()
    {
        (IDisposable).$$
";

            await VerifyItemIsAbsentAsync(markup, "ReferenceEquals");
        }

        [WorkItem(540197, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540197")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TypeParametersInClass()
        {
            var markup = @"
class C<T, R>
{
    $$
}
";
            await VerifyItemExistsAsync(markup, "T");
        }

        [WorkItem(540212, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540212")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterRefInLambda_TypeOnly()
        {
            var markup = @"
using System;
class C
{
    void M(String parameter)
    {
        Func<int, int> f = (ref $$
    }
}
";
            await VerifyItemExistsAsync(markup, "String");
            await VerifyItemIsAbsentAsync(markup, "parameter");
        }

        [WorkItem(540212, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540212")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterOutInLambda_TypeOnly()
        {
            var markup = @"
using System;
class C
{
    void M(String parameter)
    {
        Func<int, int> f = (out $$
    }
}
";
            await VerifyItemExistsAsync(markup, "String");
            await VerifyItemIsAbsentAsync(markup, "parameter");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(24326, "https://github.com/dotnet/roslyn/issues/24326")]
        public async Task AfterInInLambda_TypeOnly()
        {
            var markup = @"
using System;
class C
{
    void M(String parameter)
    {
        Func<int, int> f = (in $$
    }
}
";
            await VerifyItemExistsAsync(markup, "String");
            await VerifyItemIsAbsentAsync(markup, "parameter");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterRefInMethodDeclaration_TypeOnly()
        {
            var markup = @"
using System;
class C
{
    String field;
    void M(ref $$)
    {
    }
}
";
            await VerifyItemExistsAsync(markup, "String");
            await VerifyItemIsAbsentAsync(markup, "field");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterOutInMethodDeclaration_TypeOnly()
        {
            var markup = @"
using System;
class C
{
    String field;
    void M(out $$)
    {
    }
}
";
            await VerifyItemExistsAsync(markup, "String");
            await VerifyItemIsAbsentAsync(markup, "field");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(24326, "https://github.com/dotnet/roslyn/issues/24326")]
        public async Task AfterInInMethodDeclaration_TypeOnly()
        {
            var markup = @"
using System;
class C
{
    String field;
    void M(in $$)
    {
    }
}
";
            await VerifyItemExistsAsync(markup, "String");
            await VerifyItemIsAbsentAsync(markup, "field");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterRefInInvocation_TypeAndVariable()
        {
            var markup = @"
using System;
class C
{
    void M(ref String parameter)
    {
        M(ref $$
    }
}
";
            await VerifyItemExistsAsync(markup, "String");
            await VerifyItemExistsAsync(markup, "parameter");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterOutInInvocation_TypeAndVariable()
        {
            var markup = @"
using System;
class C
{
    void M(out String parameter)
    {
        M(out $$
    }
}
";
            await VerifyItemExistsAsync(markup, "String");
            await VerifyItemExistsAsync(markup, "parameter");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(24326, "https://github.com/dotnet/roslyn/issues/24326")]
        public async Task AfterInInInvocation_TypeAndVariable()
        {
            var markup = @"
using System;
class C
{
    void M(in String parameter)
    {
        M(in $$
    }
}
";
            await VerifyItemExistsAsync(markup, "String");
            await VerifyItemExistsAsync(markup, "parameter");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(25569, "https://github.com/dotnet/roslyn/issues/25569")]
        public async Task AfterRefExpression_TypeAndVariable()
        {
            var markup = @"
using System;
class C
{
    void M(String parameter)
    {
        ref var x = ref $$
    }
}
";
            await VerifyItemExistsAsync(markup, "String");
            await VerifyItemExistsAsync(markup, "parameter");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(25569, "https://github.com/dotnet/roslyn/issues/25569")]
        public async Task AfterRefInStatementContext_TypeOnly()
        {
            var markup = @"
using System;
class C
{
    void M(String parameter)
    {
        ref $$
    }
}
";
            await VerifyItemExistsAsync(markup, "String");
            await VerifyItemIsAbsentAsync(markup, "parameter");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(25569, "https://github.com/dotnet/roslyn/issues/25569")]
        public async Task AfterRefReadonlyInStatementContext_TypeOnly()
        {
            var markup = @"
using System;
class C
{
    void M(String parameter)
    {
        ref readonly $$
    }
}
";
            await VerifyItemExistsAsync(markup, "String");
            await VerifyItemIsAbsentAsync(markup, "parameter");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterRefLocalDeclaration_TypeOnly()
        {
            var markup = @"
using System;
class C
{
    void M(String parameter)
    {
        ref $$ int local;
    }
}
";
            await VerifyItemExistsAsync(markup, "String");
            await VerifyItemIsAbsentAsync(markup, "parameter");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterRefReadonlyLocalDeclaration_TypeOnly()
        {
            var markup = @"
using System;
class C
{
    void M(String parameter)
    {
        ref readonly $$ int local;
    }
}
";
            await VerifyItemExistsAsync(markup, "String");
            await VerifyItemIsAbsentAsync(markup, "parameter");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterRefLocalFunction_TypeOnly()
        {
            var markup = @"
using System;
class C
{
    void M(String parameter)
    {
        ref $$ int Function();
    }
}
";
            await VerifyItemExistsAsync(markup, "String");
            await VerifyItemIsAbsentAsync(markup, "parameter");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterRefReadonlyLocalFunction_TypeOnly()
        {
            var markup = @"
using System;
class C
{
    void M(String parameter)
    {
        ref readonly $$ int Function();
    }
}
";
            await VerifyItemExistsAsync(markup, "String");
            await VerifyItemIsAbsentAsync(markup, "parameter");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(25569, "https://github.com/dotnet/roslyn/issues/25569")]
        public async Task AfterRefInMemberContext_TypeOnly()
        {
            var markup = @"
using System;
class C
{
    String field;
    ref $$
}
";
            await VerifyItemExistsAsync(markup, "String");
            await VerifyItemIsAbsentAsync(markup, "field");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(25569, "https://github.com/dotnet/roslyn/issues/25569")]
        public async Task AfterRefReadonlyInMemberContext_TypeOnly()
        {
            var markup = @"
using System;
class C
{
    String field;
    ref readonly $$
}
";
            await VerifyItemExistsAsync(markup, "String");
            await VerifyItemIsAbsentAsync(markup, "field");
        }

        [WorkItem(539217, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539217")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NestedType1()
        {
            var markup = @"
class Q
{
    $$
    class R
    {

    }
}
";
            await VerifyItemExistsAsync(markup, "Q");
            await VerifyItemExistsAsync(markup, "R");
        }

        [WorkItem(539217, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539217")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NestedType2()
        {
            var markup = @"
class Q
{
    class R
    {
        $$
    }
}
";
            await VerifyItemExistsAsync(markup, "Q");
            await VerifyItemExistsAsync(markup, "R");
        }

        [WorkItem(539217, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539217")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NestedType3()
        {
            var markup = @"
class Q
{
    class R
    {
    }
    $$
}
";
            await VerifyItemExistsAsync(markup, "Q");
            await VerifyItemExistsAsync(markup, "R");
        }

        [WorkItem(539217, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539217")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NestedType4_Regular()
        {
            var markup = @"
class Q
{
    class R
    {
    }
}
$$"; // At EOF
            await VerifyItemIsAbsentAsync(markup, "Q", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Regular);
            await VerifyItemIsAbsentAsync(markup, "R", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Regular);
        }

        [WorkItem(539217, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539217")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NestedType4_Script()
        {
            var markup = @"
class Q
{
    class R
    {
    }
}
$$"; // At EOF
            await VerifyItemExistsAsync(markup, "Q", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
            await VerifyItemIsAbsentAsync(markup, "R", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [WorkItem(539217, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539217")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NestedType5()
        {
            var markup = @"
class Q
{
    class R
    {
    }
    $$"; // At EOF
            await VerifyItemExistsAsync(markup, "Q");
            await VerifyItemExistsAsync(markup, "R");
        }

        [WorkItem(539217, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539217")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NestedType6()
        {
            var markup = @"
class Q
{
    class R
    {
        $$"; // At EOF
            await VerifyItemExistsAsync(markup, "Q");
            await VerifyItemExistsAsync(markup, "R");
        }

        [WorkItem(540574, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540574")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AmbiguityBetweenTypeAndLocal()
        {
            var markup = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    public void goo() {
        int i = 5;
        i.$$
        List<string> ml = new List<string>();
    }
}";

            await VerifyItemExistsAsync(markup, "CompareTo");
        }

        [WorkItem(21596, "https://github.com/dotnet/roslyn/issues/21596")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AmbiguityBetweenExpressionAndLocalFunctionReturnType()
        {
            var markup = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        AwaitTest test = new AwaitTest();
        test.Test1().Wait();
    }
}

class AwaitTest
{
    List<string> stringList = new List<string>();

    public async Task<bool> Test1()
    {
        stringList.$$

        await Test2();

        return true;
    }

    public async Task<bool> Test2()
    {
        return true;
    }
}";

            await VerifyItemExistsAsync(markup, "Add");
        }

        [WorkItem(540750, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540750")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CompletionAfterNewInScript()
        {
            var markup = @"
using System;

new $$";

            await VerifyItemExistsAsync(markup, "String", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [WorkItem(540933, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540933")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ExtensionMethodsInScript()
        {
            var markup = @"
using System.Linq;
var a = new int[] { 1, 2 };
a.$$";

            await VerifyItemExistsAsync(markup, "ElementAt", displayTextSuffix: "<>", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [WorkItem(541019, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541019")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ExpressionsInForLoopInitializer()
        {
            var markup = @"
public class C
{
    public void M()
    {
        int count = 0;
        for ($$
";

            await VerifyItemExistsAsync(markup, "count");
        }

        [WorkItem(541108, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541108")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterLambdaExpression1()
        {
            var markup = @"
public class C
{
    public void M()
    {
        System.Func<int, int> f = arg => { arg = 2; return arg; }.$$
    }
}
";

            await VerifyItemIsAbsentAsync(markup, "ToString");
        }

        [WorkItem(541108, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541108")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterLambdaExpression2()
        {
            var markup = @"
public class C
{
    public void M()
    {
        ((System.Func<int, int>)(arg => { arg = 2; return arg; })).$$
    }
}
";

            await VerifyItemExistsAsync(markup, "ToString");
            await VerifyItemExistsAsync(markup, "Invoke");
        }

        [WorkItem(541216, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541216")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InMultiLineCommentAtEndOfFile()
        {
            var markup = @"
using System;
/*$$";

            await VerifyItemIsAbsentAsync(markup, "Console", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [WorkItem(541218, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541218")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TypeParametersAtEndOfFile()
        {
            var markup = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Outer<T>
{
class Inner<U>
{
static void F(T t, U u)
{
return;
}
public static void F(T t)
{
Outer<$$";

            await VerifyItemExistsAsync(markup, "T");
        }

        [WorkItem(552717, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552717")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task LabelInCaseSwitchAbsentForCase()
        {
            var markup = @"
class Program
{
    static void Main()
    {
        int x;
        switch (x)
        {
            case 0:
                goto $$";

            await VerifyItemIsAbsentAsync(markup, "case 0:");
        }

        [WorkItem(552717, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552717")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task LabelInCaseSwitchAbsentForDefaultWhenAbsent()
        {
            var markup = @"
class Program
{
    static void Main()
    {
        int x;
        switch (x)
        {
            case 0:
                goto $$";

            await VerifyItemIsAbsentAsync(markup, "default:");
        }

        [WorkItem(552717, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552717")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task LabelInCaseSwitchPresentForDefault()
        {
            var markup = @"
class Program
{
    static void Main()
    {
        int x;
        switch (x)
        {
            default:
                goto $$";

            await VerifyItemExistsAsync(markup, "default");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task LabelAfterGoto1()
        {
            var markup = @"
class Program
{
    static void Main()
    {
    Goo:
        int Goo;
        goto $$";

            await VerifyItemExistsAsync(markup, "Goo");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task LabelAfterGoto2()
        {
            var markup = @"
class Program
{
    static void Main()
    {
    Goo:
        int Goo;
        goto Goo $$";

            await VerifyItemIsAbsentAsync(markup, "Goo");
        }

        [WorkItem(542225, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542225")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AttributeName()
        {
            var markup = @"
using System;
[$$";

            await VerifyItemExistsAsync(markup, "CLSCompliant");
            await VerifyItemIsAbsentAsync(markup, "CLSCompliantAttribute");
        }

        [WorkItem(542225, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542225")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AttributeNameAfterSpecifier()
        {
            var markup = @"
using System;
[assembly:$$
";

            await VerifyItemExistsAsync(markup, "CLSCompliant");
            await VerifyItemIsAbsentAsync(markup, "CLSCompliantAttribute");
        }

        [WorkItem(542225, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542225")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AttributeNameInAttributeList()
        {
            var markup = @"
using System;
[CLSCompliant, $$";

            await VerifyItemExistsAsync(markup, "CLSCompliant");
            await VerifyItemIsAbsentAsync(markup, "CLSCompliantAttribute");
        }

        [WorkItem(542225, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542225")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AttributeNameBeforeClass()
        {
            var markup = @"
using System;
[$$
class C { }";

            await VerifyItemExistsAsync(markup, "CLSCompliant");
            await VerifyItemIsAbsentAsync(markup, "CLSCompliantAttribute");
        }

        [WorkItem(542225, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542225")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AttributeNameAfterSpecifierBeforeClass()
        {
            var markup = @"
using System;
[assembly:$$
class C { }";

            await VerifyItemExistsAsync(markup, "CLSCompliant");
            await VerifyItemIsAbsentAsync(markup, "CLSCompliantAttribute");
        }

        [WorkItem(542225, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542225")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AttributeNameInAttributeArgumentList()
        {
            var markup = @"
using System;
[CLSCompliant($$
class C { }";

            await VerifyItemExistsAsync(markup, "CLSCompliantAttribute");
            await VerifyItemIsAbsentAsync(markup, "CLSCompliant");
        }

        [WorkItem(542225, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542225")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AttributeNameInsideClass()
        {
            var markup = @"
using System;
class C { $$ }";

            await VerifyItemExistsAsync(markup, "CLSCompliantAttribute");
            await VerifyItemIsAbsentAsync(markup, "CLSCompliant");
        }

        [WorkItem(542954, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542954")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NamespaceAliasInAttributeName1()
        {
            var markup = @"
using Alias = System;

[$$
class C { }";

            await VerifyItemExistsAsync(markup, "Alias");
        }

        [WorkItem(542954, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542954")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NamespaceAliasInAttributeName2()
        {
            var markup = @"
using Alias = Goo;

namespace Goo { }

[$$
class C { }";

            await VerifyItemIsAbsentAsync(markup, "Alias");
        }

        [WorkItem(542954, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542954")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NamespaceAliasInAttributeName3()
        {
            var markup = @"
using Alias = Goo;

namespace Goo { class A : System.Attribute { } }

[$$
class C { }";

            await VerifyItemExistsAsync(markup, "Alias");
        }

        [Fact]
        [WorkItem(545121, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545121")]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AttributeNameAfterNamespace()
        {
            var markup = @"
namespace Test
{
    class MyAttribute : System.Attribute { }
    [Test.$$
    class Program { }
}";
            await VerifyItemExistsAsync(markup, "My");
            await VerifyItemIsAbsentAsync(markup, "MyAttribute");
        }

        [Fact]
        [WorkItem(545121, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545121")]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AttributeNameAfterNamespace2()
        {
            var markup = @"
namespace Test
{
    namespace Two
    {
        class MyAttribute : System.Attribute { }
        [Test.Two.$$
        class Program { }
    }
}";
            await VerifyItemExistsAsync(markup, "My");
            await VerifyItemIsAbsentAsync(markup, "MyAttribute");
        }

        [Fact]
        [WorkItem(545121, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545121")]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AttributeNameWhenSuffixlessFormIsKeyword()
        {
            var markup = @"
namespace Test
{
    class namespaceAttribute : System.Attribute { }
    [$$
    class Program { }
}";
            await VerifyItemExistsAsync(markup, "namespaceAttribute");
            await VerifyItemIsAbsentAsync(markup, "namespace");
            await VerifyItemIsAbsentAsync(markup, "@namespace");
        }

        [Fact]
        [WorkItem(545121, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545121")]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AttributeNameAfterNamespaceWhenSuffixlessFormIsKeyword()
        {
            var markup = @"
namespace Test
{
    class namespaceAttribute : System.Attribute { }
    [Test.$$
    class Program { }
}";
            await VerifyItemExistsAsync(markup, "namespaceAttribute");
            await VerifyItemIsAbsentAsync(markup, "namespace");
            await VerifyItemIsAbsentAsync(markup, "@namespace");
        }

        [Fact]
        [WorkItem(545348, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545348")]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task KeywordsUsedAsLocals()
        {
            var markup = @"
class C
{
    void M()
    {
        var error = 0;
        var method = 0;
        var @int = 0;
        Console.Write($$
    }
}";

            // preprocessor keyword
            await VerifyItemExistsAsync(markup, "error");
            await VerifyItemIsAbsentAsync(markup, "@error");

            // contextual keyword
            await VerifyItemExistsAsync(markup, "method");
            await VerifyItemIsAbsentAsync(markup, "@method");

            // full keyword
            await VerifyItemExistsAsync(markup, "@int");
            await VerifyItemIsAbsentAsync(markup, "int");
        }

        [Fact]
        [WorkItem(545348, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545348")]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task QueryContextualKeywords1()
        {
            var markup = @"
class C
{
    void M()
    {
        var from = new[]{1,2,3};
        var r = from x in $$
    }
}";

            await VerifyItemExistsAsync(markup, "@from");
            await VerifyItemIsAbsentAsync(markup, "from");
        }

        [Fact]
        [WorkItem(545348, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545348")]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task QueryContextualKeywords2()
        {
            var markup = @"
class C
{
    void M()
    {
        var where = new[] { 1, 2, 3 };
        var x = from @from in @where
                where $$ == @where.Length
                select @from;
    }
}";

            await VerifyItemExistsAsync(markup, "@from");
            await VerifyItemIsAbsentAsync(markup, "from");
            await VerifyItemExistsAsync(markup, "@where");
            await VerifyItemIsAbsentAsync(markup, "where");
        }

        [Fact]
        [WorkItem(545348, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545348")]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task QueryContextualKeywords3()
        {
            var markup = @"
class C
{
    void M()
    {
        var where = new[] { 1, 2, 3 };
        var x = from @from in @where
                where @from == @where.Length
                select $$;
    }
}";

            await VerifyItemExistsAsync(markup, "@from");
            await VerifyItemIsAbsentAsync(markup, "from");
            await VerifyItemExistsAsync(markup, "@where");
            await VerifyItemIsAbsentAsync(markup, "where");
        }

        [Fact]
        [WorkItem(545121, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545121")]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AttributeNameAfterGlobalAlias()
        {
            var markup = @"
class MyAttribute : System.Attribute { }
[global::$$
class Program { }";
            await VerifyItemExistsAsync(markup, "My", sourceCodeKind: SourceCodeKind.Regular);
            await VerifyItemIsAbsentAsync(markup, "MyAttribute", sourceCodeKind: SourceCodeKind.Regular);
        }

        [Fact]
        [WorkItem(545121, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545121")]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AttributeNameAfterGlobalAliasWhenSuffixlessFormIsKeyword()
        {
            var markup = @"
class namespaceAttribute : System.Attribute { }
[global::$$
class Program { }";
            await VerifyItemExistsAsync(markup, "namespaceAttribute", sourceCodeKind: SourceCodeKind.Regular);
            await VerifyItemIsAbsentAsync(markup, "namespace", sourceCodeKind: SourceCodeKind.Regular);
            await VerifyItemIsAbsentAsync(markup, "@namespace", sourceCodeKind: SourceCodeKind.Regular);
        }

        [WorkItem(25589, "https://github.com/dotnet/roslyn/issues/25589")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AttributeSearch_NamespaceWithNestedAttribute1()
        {
            var markup = @"
namespace Namespace1
{
    namespace Namespace2 { class NonAttribute { } }
    namespace Namespace3.Namespace4 { class CustomAttribute : System.Attribute { } }
}

[$$]";
            await VerifyItemExistsAsync(markup, "Namespace1");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AttributeSearch_NamespaceWithNestedAttribute2()
        {
            var markup = @"
namespace Namespace1
{
    namespace Namespace2 { class NonAttribute { } }
    namespace Namespace3.Namespace4 { class CustomAttribute : System.Attribute { } }
}

[Namespace1.$$]";
            await VerifyItemIsAbsentAsync(markup, "Namespace2");
            await VerifyItemExistsAsync(markup, "Namespace3");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AttributeSearch_NamespaceWithNestedAttribute3()
        {
            var markup = @"
namespace Namespace1
{
    namespace Namespace2 { class NonAttribute { } }
    namespace Namespace3.Namespace4 { class CustomAttribute : System.Attribute { } }
}

[Namespace1.Namespace3.$$]";
            await VerifyItemExistsAsync(markup, "Namespace4");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AttributeSearch_NamespaceWithNestedAttribute4()
        {
            var markup = @"
namespace Namespace1
{
    namespace Namespace2 { class NonAttribute { } }
    namespace Namespace3.Namespace4 { class CustomAttribute : System.Attribute { } }
}

[Namespace1.Namespace3.Namespace4.$$]";
            await VerifyItemExistsAsync(markup, "Custom");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AttributeSearch_NamespaceWithNestedAttribute_NamespaceAlias()
        {
            var markup = @"
using Namespace1Alias = Namespace1;
using Namespace2Alias = Namespace1.Namespace2;
using Namespace3Alias = Namespace1.Namespace3;
using Namespace4Alias = Namespace1.Namespace3.Namespace4;

namespace Namespace1
{
    namespace Namespace2 { class NonAttribute { } }
    namespace Namespace3.Namespace4 { class CustomAttribute : System.Attribute { } }
}

[$$]";
            await VerifyItemExistsAsync(markup, "Namespace1Alias");
            await VerifyItemIsAbsentAsync(markup, "Namespace2Alias");
            await VerifyItemExistsAsync(markup, "Namespace3Alias");
            await VerifyItemExistsAsync(markup, "Namespace4Alias");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AttributeSearch_NamespaceWithoutNestedAttribute()
        {
            var markup = @"
namespace Namespace1
{
    namespace Namespace2 { class NonAttribute { } }
    namespace Namespace3.Namespace4 { class NonAttribute : System.NonAttribute { } }
}

[$$]";
            await VerifyItemIsAbsentAsync(markup, "Namespace1");
        }

        [WorkItem(542230, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542230")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task RangeVariableInQuerySelect()
        {
            var markup = @"
using System.Linq;
class P
{
    void M()
    {
        var src = new string[] { ""Goo"", ""Bar"" };
        var q = from x in src
                select x.$$";

            await VerifyItemExistsAsync(markup, "Length");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ConstantsInIsExpression()
        {
            var markup = @"
class C
{
    public const int MAX_SIZE = 10;
    void M()
    {
        int i = 10;
        if (i is $$ int"; // 'int' to force this to be parsed as an IsExpression rather than IsPatternExpression

            await VerifyItemExistsAsync(markup, "MAX_SIZE");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ConstantsInIsPatternExpression()
        {
            var markup = @"
class C
{
    public const int MAX_SIZE = 10;
    void M()
    {
        int i = 10;
        if (i is $$ 1";

            await VerifyItemExistsAsync(markup, "MAX_SIZE");
        }

        [WorkItem(542429, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542429")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ConstantsInSwitchCase()
        {
            var markup = @"
class C
{
    public const int MAX_SIZE = 10;
    void M()
    {
        int i = 10;
        switch (i)
        {
            case $$";

            await VerifyItemExistsAsync(markup, "MAX_SIZE");
        }

        [WorkItem(25084, "https://github.com/dotnet/roslyn/issues/25084#issuecomment-370148553")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ConstantsInSwitchPatternCase()
        {
            var markup = @"
class C
{
    public const int MAX_SIZE = 10;
    void M()
    {
        int i = 10;
        switch (i)
        {
            case $$ when";

            await VerifyItemExistsAsync(markup, "MAX_SIZE");
        }

        [WorkItem(542429, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542429")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ConstantsInSwitchGotoCase()
        {
            var markup = @"
class C
{
    public const int MAX_SIZE = 10;
    void M()
    {
        int i = 10;
        switch (i)
        {
            case MAX_SIZE:
                break;
            case GOO:
                goto case $$";

            await VerifyItemExistsAsync(markup, "MAX_SIZE");
        }

        [WorkItem(542429, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542429")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ConstantsInEnumMember()
        {
            var markup = @"
class C
{
    public const int GOO = 0;
    enum E
    {
        A = $$";

            await VerifyItemExistsAsync(markup, "GOO");
        }

        [WorkItem(542429, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542429")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ConstantsInAttribute1()
        {
            var markup = @"
class C
{
    public const int GOO = 0;
    [System.AttributeUsage($$";

            await VerifyItemExistsAsync(markup, "GOO");
        }

        [WorkItem(542429, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542429")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ConstantsInAttribute2()
        {
            var markup = @"
class C
{
    public const int GOO = 0;
    [System.AttributeUsage(GOO, $$";

            await VerifyItemExistsAsync(markup, "GOO");
        }

        [WorkItem(542429, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542429")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ConstantsInAttribute3()
        {
            var markup = @"
class C
{
    public const int GOO = 0;
    [System.AttributeUsage(validOn: $$";

            await VerifyItemExistsAsync(markup, "GOO");
        }

        [WorkItem(542429, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542429")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ConstantsInAttribute4()
        {
            var markup = @"
class C
{
    public const int GOO = 0;
    [System.AttributeUsage(AllowMultiple = $$";

            await VerifyItemExistsAsync(markup, "GOO");
        }

        [WorkItem(542429, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542429")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ConstantsInParameterDefaultValue()
        {
            var markup = @"
class C
{
    public const int GOO = 0;
    void M(int x = $$";

            await VerifyItemExistsAsync(markup, "GOO");
        }

        [WorkItem(542429, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542429")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ConstantsInConstField()
        {
            var markup = @"
class C
{
    public const int GOO = 0;
    const int BAR = $$";

            await VerifyItemExistsAsync(markup, "GOO");
        }

        [WorkItem(542429, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542429")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ConstantsInConstLocal()
        {
            var markup = @"
class C
{
    public const int GOO = 0;
    void M()
    {
        const int BAR = $$";

            await VerifyItemExistsAsync(markup, "GOO");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DescriptionWith1Overload()
        {
            var markup = @"
class C
{
    void M(int i) { }
    void M()
    {
        $$";

            await VerifyItemExistsAsync(markup, "M", expectedDescriptionOrNull: $"void C.M(int i) (+ 1 {FeaturesResources.overload})");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DescriptionWith2Overloads()
        {
            var markup = @"
class C
{
    void M(int i) { }
    void M(out int i) { }
    void M()
    {
        $$";

            await VerifyItemExistsAsync(markup, "M", expectedDescriptionOrNull: $"void C.M(int i) (+ 2 {FeaturesResources.overloads_})");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DescriptionWith1GenericOverload()
        {
            var markup = @"
class C
{
    void M<T>(T i) { }
    void M<T>()
    {
        $$";

            await VerifyItemExistsAsync(markup, "M", displayTextSuffix: "<>", expectedDescriptionOrNull: $"void C.M<T>(T i) (+ 1 {FeaturesResources.generic_overload})");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DescriptionWith2GenericOverloads()
        {
            var markup = @"
class C
{
    void M<T>(int i) { }
    void M<T>(out int i) { }
    void M<T>()
    {
        $$";

            await VerifyItemExistsAsync(markup, "M", displayTextSuffix: "<>", expectedDescriptionOrNull: $"void C.M<T>(int i) (+ 2 {FeaturesResources.generic_overloads})");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DescriptionNamedGenericType()
        {
            var markup = @"
class C<T>
{
    void M()
    {
        $$";

            await VerifyItemExistsAsync(markup, "C", displayTextSuffix: "<>", expectedDescriptionOrNull: "class C<T>");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DescriptionParameter()
        {
            var markup = @"
class C<T>
{
    void M(T goo)
    {
        $$";

            await VerifyItemExistsAsync(markup, "goo", expectedDescriptionOrNull: $"({FeaturesResources.parameter}) T goo");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DescriptionGenericTypeParameter()
        {
            var markup = @"
class C<T>
{
    void M()
    {
        $$";

            await VerifyItemExistsAsync(markup, "T", expectedDescriptionOrNull: $"T {FeaturesResources.in_} C<T>");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DescriptionAnonymousType()
        {
            var markup = @"
class C
{
    void M()
    {
        var a = new { };
        $$
";

            var expectedDescription =
$@"({FeaturesResources.local_variable}) 'a a

{FeaturesResources.Anonymous_Types_colon}
    'a {FeaturesResources.is_} new {{  }}";

            await VerifyItemExistsAsync(markup, "a", expectedDescription);
        }

        [WorkItem(543288, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543288")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterNewInAnonymousType()
        {
            var markup = @"
class Program {
    string field = 0;
    static void Main()     {
        var an = new {  new $$  }; 
    }
}
";

            await VerifyItemExistsAsync(markup, "Program");
        }

        [WorkItem(543601, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543601")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoInstanceFieldsInStaticMethod()
        {
            var markup = @"
class C
{
    int x = 0;
    static void M()
    {
        $$
    }
}
";

            await VerifyItemIsAbsentAsync(markup, "x");
        }

        [WorkItem(543601, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543601")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoInstanceFieldsInStaticFieldInitializer()
        {
            var markup = @"
class C
{
    int x = 0;
    static int y = $$
}
";

            await VerifyItemIsAbsentAsync(markup, "x");
        }

        [WorkItem(543601, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543601")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task StaticFieldsInStaticMethod()
        {
            var markup = @"
class C
{
    static int x = 0;
    static void M()
    {
        $$
    }
}
";

            await VerifyItemExistsAsync(markup, "x");
        }

        [WorkItem(543601, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543601")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task StaticFieldsInStaticFieldInitializer()
        {
            var markup = @"
class C
{
    static int x = 0;
    static int y = $$
}
";

            await VerifyItemExistsAsync(markup, "x");
        }

        [WorkItem(543680, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543680")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoInstanceFieldsFromOuterClassInInstanceMethod()
        {
            var markup = @"
class outer
{
    int i;
    class inner
    {
        void M()
        {
            $$
        }
    }
}
";

            await VerifyItemIsAbsentAsync(markup, "i");
        }

        [WorkItem(543680, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543680")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task StaticFieldsFromOuterClassInInstanceMethod()
        {
            var markup = @"
class outer
{
    static int i;
    class inner
    {
        void M()
        {
            $$
        }
    }
}
";

            await VerifyItemExistsAsync(markup, "i");
        }

        [WorkItem(543104, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543104")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task OnlyEnumMembersInEnumMemberAccess()
        {
            var markup = @"
class C
{
    enum x {a,b,c}
    void M()
    {
        x.$$
    }
}
";

            await VerifyItemExistsAsync(markup, "a");
            await VerifyItemExistsAsync(markup, "b");
            await VerifyItemExistsAsync(markup, "c");
            await VerifyItemIsAbsentAsync(markup, "Equals");
        }

        [WorkItem(543104, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543104")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoEnumMembersInEnumLocalAccess()
        {
            var markup = @"
class C
{
    enum x {a,b,c}
    void M()
    {
        var y = x.a;
        y.$$
    }
}
";

            await VerifyItemIsAbsentAsync(markup, "a");
            await VerifyItemIsAbsentAsync(markup, "b");
            await VerifyItemIsAbsentAsync(markup, "c");
            await VerifyItemExistsAsync(markup, "Equals");
        }

        [WorkItem(529138, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529138")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterLambdaParameterDot()
        {
            var markup = @"
using System;
using System.Linq;
class A
{
    public event Func<String, String> E;
}
 
class Program
{
    static void Main(string[] args)
    {
        new A().E += ss => ss.$$
    }
}
";

            await VerifyItemExistsAsync(markup, "Substring");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ValueNotAtRoot_Interactive()
        {
            await VerifyItemIsAbsentAsync(
@"$$",
"value",
expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ValueNotAfterClass_Interactive()
        {
            await VerifyItemIsAbsentAsync(
@"class C { }
$$",
"value",
expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ValueNotAfterGlobalStatement_Interactive()
        {
            await VerifyItemIsAbsentAsync(
@"System.Console.WriteLine();
$$",
"value",
expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ValueNotAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyItemIsAbsentAsync(
@"int i = 0;
$$",
"value",
expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ValueNotInUsingAlias()
        {
            await VerifyItemIsAbsentAsync(
@"using Goo = $$",
"value");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ValueNotInEmptyStatement()
        {
            await VerifyItemIsAbsentAsync(AddInsideMethod(
@"$$"),
"value");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ValueInsideSetter()
        {
            await VerifyItemExistsAsync(
@"class C {
    int Goo {
      set {
        $$",
"value");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ValueInsideAdder()
        {
            await VerifyItemExistsAsync(
@"class C {
    event int Goo {
      add {
        $$",
"value");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ValueInsideRemover()
        {
            await VerifyItemExistsAsync(
@"class C {
    event int Goo {
      remove {
        $$",
"value");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ValueNotAfterDot()
        {
            await VerifyItemIsAbsentAsync(
@"class C {
    int Goo {
      set {
        this.$$",
"value");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ValueNotAfterArrow()
        {
            await VerifyItemIsAbsentAsync(
@"class C {
    int Goo {
      set {
        a->$$",
"value");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ValueNotAfterColonColon()
        {
            await VerifyItemIsAbsentAsync(
@"class C {
    int Goo {
      set {
        a::$$",
"value");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ValueNotInGetter()
        {
            await VerifyItemIsAbsentAsync(
@"class C {
    int Goo {
      get {
        $$",
"value");
        }

        [WorkItem(544205, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544205")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotAfterNullableType()
        {
            await VerifyItemIsAbsentAsync(
@"class C {
    void M() {
        int goo = 0;
        C? $$",
"goo");
        }

        [WorkItem(544205, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544205")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotAfterNullableTypeAlias()
        {
            await VerifyItemIsAbsentAsync(
@"using A = System.Int32;
class C {
    void M() {
        int goo = 0;
        A? $$",
"goo");
        }

        [WorkItem(544205, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544205")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotAfterNullableTypeAndPartialIdentifier()
        {
            await VerifyItemIsAbsentAsync(
@"class C {
    void M() {
        int goo = 0;
        C? f$$",
"goo");
        }

        [WorkItem(544205, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544205")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterQuestionMarkInConditional()
        {
            await VerifyItemExistsAsync(
@"class C {
    void M() {
        bool b = false;
        int goo = 0;
        b? $$",
"goo");
        }

        [WorkItem(544205, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544205")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterQuestionMarkAndPartialIdentifierInConditional()
        {
            await VerifyItemExistsAsync(
@"class C {
    void M() {
        bool b = false;
        int goo = 0;
        b? f$$",
"goo");
        }

        [WorkItem(544205, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544205")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotAfterPointerType()
        {
            await VerifyItemIsAbsentAsync(
@"class C {
    void M() {
        int goo = 0;
        C* $$",
"goo");
        }

        [WorkItem(544205, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544205")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotAfterPointerTypeAlias()
        {
            await VerifyItemIsAbsentAsync(
@"using A = System.Int32;
class C {
    void M() {
        int goo = 0;
        A* $$",
"goo");
        }

        [WorkItem(544205, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544205")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotAfterPointerTypeAndPartialIdentifier()
        {
            await VerifyItemIsAbsentAsync(
@"class C {
    void M() {
        int goo = 0;
        C* f$$",
"goo");
        }

        [WorkItem(544205, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544205")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterAsteriskInMultiplication()
        {
            await VerifyItemExistsAsync(
@"class C {
    void M() {
        int i = 0;
        int goo = 0;
        i* $$",
"goo");
        }

        [WorkItem(544205, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544205")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterAsteriskAndPartialIdentifierInMultiplication()
        {
            await VerifyItemExistsAsync(
@"class C {
    void M() {
        int i = 0;
        int goo = 0;
        i* f$$",
"goo");
        }

        [WorkItem(543868, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543868")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterEventFieldDeclaredInSameType()
        {
            await VerifyItemExistsAsync(
@"class C {
    public event System.EventHandler E;
    void M() {
        E.$$",
"Invoke");
        }

        [WorkItem(543868, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543868")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotAfterFullEventDeclaredInSameType()
        {
            await VerifyItemIsAbsentAsync(
@"class C {
        public event System.EventHandler E { add { } remove { } }
    void M() {
        E.$$",
"Invoke");
        }

        [WorkItem(543868, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543868")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotAfterEventDeclaredInDifferentType()
        {
            await VerifyItemIsAbsentAsync(
@"class C {
    void M() {
        System.Console.CancelKeyPress.$$",
"Invoke");
        }

        [WorkItem(544219, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544219")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotInObjectInitializerMemberContext()
        {
            await VerifyItemIsAbsentAsync(@"
class C
{
    public int x, y;
    void M()
    {
        var c = new C { x = 2, y = 3, $$",
"x");
        }

        [WorkItem(544219, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544219")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task AfterPointerMemberAccess()
        {
            await VerifyItemExistsAsync(@"
struct MyStruct
{
    public int MyField;
}

class Program
{
    static unsafe void Main(string[] args)
    {
        MyStruct s = new MyStruct();
        MyStruct* ptr = &s;
        ptr->$$
    }}",
"MyField");
        }

        // After @ both X and XAttribute are legal. We think this is an edge case in the language and
        // are not fixing the bug 11931. This test captures that XAttribute doesn't show up indeed.
        [WorkItem(11931, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task VerbatimAttributes()
        {
            var code = @"
using System;
public class X : Attribute
{ }
 
public class XAttribute : Attribute
{ }
 
 
[@X$$]
class Class3 { }
";
            await VerifyItemExistsAsync(code, "X");
            await Assert.ThrowsAsync<Xunit.Sdk.TrueException>(() => VerifyItemExistsAsync(code, "XAttribute"));
        }

        [WorkItem(544928, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544928")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task InForLoopIncrementor1()
        {
            await VerifyItemExistsAsync(@"
using System;
 
class Program
{
    static void Main()
    {
        for (; ; $$
    }
}
", "Console");
        }

        [WorkItem(544928, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544928")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task InForLoopIncrementor2()
        {
            await VerifyItemExistsAsync(@"
using System;
 
class Program
{
    static void Main()
    {
        for (; ; Console.WriteLine(), $$
    }
}
", "Console");
        }

        [WorkItem(544931, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544931")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task InForLoopInitializer1()
        {
            await VerifyItemExistsAsync(@"
using System;
 
class Program
{
    static void Main()
    {
        for ($$
    }
}
", "Console");
        }

        [WorkItem(544931, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544931")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task InForLoopInitializer2()
        {
            await VerifyItemExistsAsync(@"
using System;
 
class Program
{
    static void Main()
    {
        for (Console.WriteLine(), $$
    }
}
", "Console");
        }

        [WorkItem(10572, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task LocalVariableInItsDeclaration()
        {
            // "int goo = goo = 1" is a legal declaration
            await VerifyItemExistsAsync(@"
class Program
{
    void M()
    {
        int goo = $$
    }
}", "goo");
        }

        [WorkItem(10572, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task LocalVariableInItsDeclarator()
        {
            // "int bar = bar = 1" is legal in a declarator
            await VerifyItemExistsAsync(@"
class Program
{
    void M()
    {
        int goo = 0, int bar = $$, int baz = 0;
    }
}", "bar");
        }

        [WorkItem(10572, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task LocalVariableNotBeforeDeclaration()
        {
            await VerifyItemIsAbsentAsync(@"
class Program
{
    void M()
    {
        $$
        int goo = 0;
    }
}", "goo");
        }

        [WorkItem(10572, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task LocalVariableNotBeforeDeclarator()
        {
            await VerifyItemIsAbsentAsync(@"
class Program
{
    void M()
    {
        int goo = $$, bar = 0;
    }
}", "bar");
        }

        [WorkItem(10572, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task LocalVariableAfterDeclarator()
        {
            await VerifyItemExistsAsync(@"
class Program
{
    void M()
    {
        int goo = 0, int bar = $$
    }
}", "goo");
        }

        [WorkItem(10572, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task LocalVariableAsOutArgumentInInitializerExpression()
        {
            await VerifyItemExistsAsync(@"
class Program
{
    void M()
    {
        int goo = Bar(out $$
    }
    int Bar(out int x)
    {
        x = 3;
        return 5;
    }
}", "goo");
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Method_BrowsableStateAlways()
        {
            var markup = @"
class Program
{
    void M()
    {
        Goo.$$
    }
}";

            var referencedCode = @"
public class Goo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
    public static void Bar() 
    {
    }
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Method_BrowsableStateNever()
        {
            var markup = @"
class Program
{
    void M()
    {
        Goo.$$
    }
}";

            var referencedCode = @"
public class Goo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public static void Bar() 
    {
    }
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Method_BrowsableStateAdvanced()
        {
            var markup = @"
class Program
{
    void M()
    {
        Goo.$$
    }
}";

            var referencedCode = @"
public class Goo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    public static void Bar() 
    {
    }
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Method_Overloads_BothBrowsableAlways()
        {
            var markup = @"
class Program
{
    void M()
    {
        Goo.$$
    }
}";

            var referencedCode = @"
public class Goo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
    public static void Bar() 
    {
    }

    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
    public static void Bar(int x) 
    {
    }
}";

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 2,
                expectedSymbolsMetadataReference: 2,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Method_Overloads_OneBrowsableAlways_OneBrowsableNever()
        {
            var markup = @"
class Program
{
    void M()
    {
        Goo.$$
    }
}";

            var referencedCode = @"
public class Goo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
    public static void Bar() 
    {
    }

    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public static void Bar(int x) 
    {
    }
}";

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 2,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Method_Overloads_BothBrowsableNever()
        {
            var markup = @"
class Program
{
    void M()
    {
        Goo.$$
    }
}";

            var referencedCode = @"
public class Goo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public static void Bar() 
    {
    }

    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public static void Bar(int x) 
    {
    }
}";

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 2,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_ExtensionMethod_BrowsableAlways()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Goo().$$
    }
}";

            var referencedCode = @"
public class Goo
{
}

public static class GooExtensions
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
    public static void Bar(this Goo goo, int x)
    {
    }
}";

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_ExtensionMethod_BrowsableNever()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Goo().$$
    }
}";

            var referencedCode = @"
public class Goo
{
}

public static class GooExtensions
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public static void Bar(this Goo goo, int x)
    {
    }
}";

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_ExtensionMethod_BrowsableAdvanced()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Goo().$$
    }
}";

            var referencedCode = @"
public class Goo
{
}

public static class GooExtensions
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    public static void Bar(this Goo goo, int x)
    {
    }
}";

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_ExtensionMethod_BrowsableMixed()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Goo().$$
    }
}";

            var referencedCode = @"
public class Goo
{
}

public static class GooExtensions
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
    public static void Bar(this Goo goo, int x)
    {
    }

    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public static void Bar(this Goo goo, int x, int y)
    {
    }
}";

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 2,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_OverloadExtensionMethodAndMethod_BrowsableAlways()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Goo().$$
    }
}";

            var referencedCode = @"
public class Goo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
    public void Bar(int x)
    {
    }
}

public static class GooExtensions
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
    public static void Bar(this Goo goo, int x, int y)
    {
    }
}";

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 2,
                expectedSymbolsMetadataReference: 2,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_OverloadExtensionMethodAndMethod_BrowsableMixed()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Goo().$$
    }
}";

            var referencedCode = @"
public class Goo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Bar(int x)
    {
    }
}

public static class GooExtensions
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
    public static void Bar(this Goo goo, int x, int y)
    {
    }
}";

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 2,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_SameSigExtensionMethodAndMethod_InstanceMethodBrowsableNever()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Goo().$$
    }
}";

            var referencedCode = @"
public class Goo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Bar(int x)
    {
    }
}

public static class GooExtensions
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
    public static void Bar(this Goo goo, int x)
    {
    }
}";

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 2,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task OverriddenSymbolsFilteredFromCompletionList()
        {
            var markup = @"
class Program
{
    void M()
    {
        D d = new D();
        d.$$
    }
}";

            var referencedCode = @"
public class B
{
    public virtual void Goo(int original) 
    {
    }
}

public class D : B
{
    public override void Goo(int derived) 
    {
    }
}";

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_BrowsableStateAlwaysMethodInBrowsableStateNeverClass()
        {
            var markup = @"
class Program
{
    void M()
    {
        C c = new C();
        c.$$
    }
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
public class C
{
    public void Goo() 
    {
    }
}";

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_BrowsableStateAlwaysMethodInBrowsableStateNeverBaseClass()
        {
            var markup = @"
class Program
{
    void M()
    {
        D d = new D();
        d.$$
    }
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
public class B
{
    public void Goo() 
    {
    }
}

public class D : B
{
    public void Goo(int x)
    {
    }
}";

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 2,
                expectedSymbolsMetadataReference: 2,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_BrowsableStateNeverMethodsInBaseClass()
        {
            var markup = @"
class Program : B
{
    void M()
    {
        $$
    }
}";

            var referencedCode = @"
public class B
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Goo() 
    {
    }
}";

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_GenericTypeCausingMethodSignatureEquality_BothBrowsableAlways()
        {
            var markup = @"
class Program
{
    void M()
    {
        var ci = new C<int>();
        ci.$$
    }
}";

            var referencedCode = @"
public class C<T>
{
    public void Goo(T t) { }
    public void Goo(int i) { }
}";

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 2,
                expectedSymbolsMetadataReference: 2,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_GenericTypeCausingMethodSignatureEquality_BrowsableMixed1()
        {
            var markup = @"
class Program
{
    void M()
    {
        var ci = new C<int>();
        ci.$$
    }
}";

            var referencedCode = @"
public class C<T>
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Goo(T t) { }
    public void Goo(int i) { }
}";

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 2,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_GenericTypeCausingMethodSignatureEquality_BrowsableMixed2()
        {
            var markup = @"
class Program
{
    void M()
    {
        var ci = new C<int>();
        ci.$$
    }
}";

            var referencedCode = @"
public class C<T>
{
    public void Goo(T t) { }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Goo(int i) { }
}";

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 2,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_GenericTypeCausingMethodSignatureEquality_BothBrowsableNever()
        {
            var markup = @"
class Program
{
    void M()
    {
        var ci = new C<int>();
        ci.$$
    }
}";

            var referencedCode = @"
public class C<T>
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Goo(T t) { }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Goo(int i) { }
}";

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 2,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_GenericType2CausingMethodSignatureEquality_BothBrowsableAlways()
        {
            var markup = @"
class Program
{
    void M()
    {
        var cii = new C<int, int>();
        cii.$$
    }
}";

            var referencedCode = @"
public class C<T, U>
{
    public void Goo(T t) { }
    public void Goo(U u) { }
}";

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 2,
                expectedSymbolsMetadataReference: 2,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_GenericType2CausingMethodSignatureEquality_BrowsableMixed()
        {
            var markup = @"
class Program
{
    void M()
    {
        var cii = new C<int, int>();
        cii.$$
    }
}";

            var referencedCode = @"
public class C<T, U>
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Goo(T t) { }
    public void Goo(U u) { }
}";

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 2,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_GenericType2CausingMethodSignatureEquality_BothBrowsableNever()
        {
            var markup = @"
class Program
{
    void M()
    {
        var cii = new C<int, int>();
        cii.$$
    }
}";

            var referencedCode = @"
public class C<T, U>
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Goo(T t) { }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public void Goo(U u) { }
}";

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 2,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Field_BrowsableStateNever()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Goo().$$
    }
}";

            var referencedCode = @"
public class Goo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public int bar;
}";

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Field_BrowsableStateAlways()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Goo().$$
    }
}";

            var referencedCode = @"
public class Goo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
    public int bar;
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Field_BrowsableStateAdvanced()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Goo().$$
    }
}";

            var referencedCode = @"
public class Goo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    public int bar;
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);
        }

        [WorkItem(522440, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/522440")]
        [WorkItem(674611, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674611")]
        [WpfFact(Skip = "674611"), Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Property_BrowsableStateNever()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Goo().$$
    }
}";

            var referencedCode = @"
public class Goo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public int Bar {get; set;}
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Property_IgnoreBrowsabilityOfGetSetMethods()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Goo().$$
    }
}";

            var referencedCode = @"
public class Goo
{
    public int Bar {
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        get { return 5; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        set { }
    }
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Property_BrowsableStateAlways()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Goo().$$
    }
}";

            var referencedCode = @"
public class Goo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
    public int Bar {get; set;}
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Property_BrowsableStateAdvanced()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Goo().$$
    }
}";

            var referencedCode = @"
public class Goo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    public int Bar {get; set;}
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Constructor_BrowsableStateNever()
        {
            var markup = @"
class Program
{
    void M()
    {
        new $$
    }
}";

            var referencedCode = @"
public class Goo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public Goo()
    {
    }
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Constructor_BrowsableStateAlways()
        {
            var markup = @"
class Program
{
    void M()
    {
        new $$
    }
}";

            var referencedCode = @"
public class Goo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
    public Goo()
    {
    }
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Constructor_BrowsableStateAdvanced()
        {
            var markup = @"
class Program
{
    void M()
    {
        new $$
    }
}";

            var referencedCode = @"
public class Goo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    public Goo()
    {
    }
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Constructor_MixedOverloads1()
        {
            var markup = @"
class Program
{
    void M()
    {
        new $$
    }
}";

            var referencedCode = @"
public class Goo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public Goo()
    {
    }

    public Goo(int x)
    {
    }
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Constructor_MixedOverloads2()
        {
            var markup = @"
class Program
{
    void M()
    {
        new $$
    }
}";

            var referencedCode = @"
public class Goo
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public Goo()
    {
    }

    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public Goo(int x)
    {
    }
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Event_BrowsableStateNever()
        {
            var markup = @"
class Program
{
    void M()
    {
        new C().$$
    }
}";

            var referencedCode = @"
public delegate void Handler();

public class C
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public event Handler Changed;
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Changed",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Event_BrowsableStateAlways()
        {
            var markup = @"
class Program
{
    void M()
    {
        new C().$$
    }
}";

            var referencedCode = @"
public delegate void Handler();

public class C
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
    public event Handler Changed;
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Changed",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Event_BrowsableStateAdvanced()
        {
            var markup = @"
class Program
{
    void M()
    {
        new C().$$
    }
}";

            var referencedCode = @"
public delegate void Handler();

public class C
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
    public event Handler Changed;
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Changed",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Changed",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Delegate_BrowsableStateNever()
        {
            var markup = @"
class Program
{
    public event $$
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public delegate void Handler();";

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Handler",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Delegate_BrowsableStateAlways()
        {
            var markup = @"
class Program
{
    public event $$
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
public delegate void Handler();";

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Handler",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Delegate_BrowsableStateAdvanced()
        {
            var markup = @"
class Program
{
    public event $$
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
public delegate void Handler();";

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Handler",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Handler",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Class_BrowsableStateNever_DeclareLocal()
        {
            var markup = @"
class Program
{
    public void M()
    {
        $$    
    }
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public class Goo
{
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Class_BrowsableStateNever_DeriveFrom()
        {
            var markup = @"
class Program : $$
{
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public class Goo
{
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Class_BrowsableStateNever_FullyQualifiedInUsing()
        {
            var markup = @"
class Program
{
    void M()
    {
        using (var x = new NS.$$
    }
}";

            var referencedCode = @"
namespace NS
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class Goo : System.IDisposable
    {
        public void Dispose()
        {
        }
    }
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Class_BrowsableStateAlways_DeclareLocal()
        {
            var markup = @"
class Program
{
    public void M()
    {
        $$    
    }
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
public class Goo
{
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Class_BrowsableStateAlways_DeriveFrom()
        {
            var markup = @"
class Program : $$
{
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
public class Goo
{
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Class_BrowsableStateAlways_FullyQualifiedInUsing()
        {
            var markup = @"
class Program
{
    void M()
    {
        using (var x = new NS.$$
    }
}";

            var referencedCode = @"
namespace NS
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
    public class Goo : System.IDisposable
    {
        public void Dispose()
        {
        }
    }
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Class_BrowsableStateAdvanced_DeclareLocal()
        {
            var markup = @"
class Program
{
    public void M()
    {
        $$    
    }
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
public class Goo
{
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Class_BrowsableStateAdvanced_DeriveFrom()
        {
            var markup = @"
class Program : $$
{
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
public class Goo
{
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Class_BrowsableStateAdvanced_FullyQualifiedInUsing()
        {
            var markup = @"
class Program
{
    void M()
    {
        using (var x = new NS.$$
    }
}";

            var referencedCode = @"
namespace NS
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
    public class Goo : System.IDisposable
    {
        public void Dispose()
        {
        }
    }
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Class_IgnoreBaseClassBrowsableNever()
        {
            var markup = @"
class Program
{
    public void M()
    {
        $$    
    }
}";

            var referencedCode = @"
public class Goo : Bar
{
}

[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public class Bar
{
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Struct_BrowsableStateNever_DeclareLocal()
        {
            var markup = @"
class Program
{
    public void M()
    {
        $$    
    }
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public struct Goo
{
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Struct_BrowsableStateNever_DeriveFrom()
        {
            var markup = @"
class Program : $$
{
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public struct Goo
{
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Struct_BrowsableStateAlways_DeclareLocal()
        {
            var markup = @"
class Program
{
    public void M()
    {
        $$
    }
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
public struct Goo
{
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Struct_BrowsableStateAlways_DeriveFrom()
        {
            var markup = @"
class Program : $$
{
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
public struct Goo
{
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Struct_BrowsableStateAdvanced_DeclareLocal()
        {
            var markup = @"
class Program
{
    public void M()
    {
        $$    
    }
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
public struct Goo
{
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Struct_BrowsableStateAdvanced_DeriveFrom()
        {
            var markup = @"
class Program : $$
{
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
public struct Goo
{
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Enum_BrowsableStateNever()
        {
            var markup = @"
class Program
{
    public void M()
    {
        $$
    }
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public enum Goo
{
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Enum_BrowsableStateAlways()
        {
            var markup = @"
class Program
{
    public void M()
    {
        $$
    }
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
public enum Goo
{
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Enum_BrowsableStateAdvanced()
        {
            var markup = @"
class Program
{
    public void M()
    {
        $$
    }
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
public enum Goo
{
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Interface_BrowsableStateNever_DeclareLocal()
        {
            var markup = @"
class Program
{
    public void M()
    {
        $$    
    }
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public interface Goo
{
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Interface_BrowsableStateNever_DeriveFrom()
        {
            var markup = @"
class Program : $$
{
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public interface Goo
{
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Interface_BrowsableStateAlways_DeclareLocal()
        {
            var markup = @"
class Program
{
    public void M()
    {
        $$
    }
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
public interface Goo
{
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Interface_BrowsableStateAlways_DeriveFrom()
        {
            var markup = @"
class Program : $$
{
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)]
public interface Goo
{
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Interface_BrowsableStateAdvanced_DeclareLocal()
        {
            var markup = @"
class Program
{
    public void M()
    {
        $$    
    }
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
public interface Goo
{
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_Interface_BrowsableStateAdvanced_DeriveFrom()
        {
            var markup = @"
class Program : $$
{
}";

            var referencedCode = @"
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
public interface Goo
{
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: false);

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp,
                hideAdvancedMembers: true);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_CrossLanguage_CStoVB_Always()
        {
            var markup = @"
class Program
{
    void M()
    {
        $$
    }
}";

            var referencedCode = @"
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)>
Public Class Goo
End Class";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.VisualBasic,
                hideAdvancedMembers: false);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_CrossLanguage_CStoVB_Never()
        {
            var markup = @"
class Program
{
    void M()
    {
        $$
    }
}";

            var referencedCode = @"
<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
Public Class Goo
End Class";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 0,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.VisualBasic,
                hideAdvancedMembers: false);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_TypeLibType_NotHidden()
        {
            var markup = @"
class Program
{
    void M()
    {
        new $$
    }
}";

            var referencedCode = @"
[System.Runtime.InteropServices.TypeLibType(System.Runtime.InteropServices.TypeLibTypeFlags.FLicensed)]
public class Goo
{
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_TypeLibType_Hidden()
        {
            var markup = @"
class Program
{
    void M()
    {
        new $$
    }
}";

            var referencedCode = @"
[System.Runtime.InteropServices.TypeLibType(System.Runtime.InteropServices.TypeLibTypeFlags.FHidden)]
public class Goo
{
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_TypeLibType_HiddenAndOtherFlags()
        {
            var markup = @"
class Program
{
    void M()
    {
        new $$
    }
}";

            var referencedCode = @"
[System.Runtime.InteropServices.TypeLibType(System.Runtime.InteropServices.TypeLibTypeFlags.FHidden | System.Runtime.InteropServices.TypeLibTypeFlags.FLicensed)]
public class Goo
{
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_TypeLibType_NotHidden_Int16Constructor()
        {
            var markup = @"
class Program
{
    void M()
    {
        new $$
    }
}";

            var referencedCode = @"
[System.Runtime.InteropServices.TypeLibType((short)System.Runtime.InteropServices.TypeLibTypeFlags.FLicensed)]
public class Goo
{
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_TypeLibType_Hidden_Int16Constructor()
        {
            var markup = @"
class Program
{
    void M()
    {
        new $$
    }
}";

            var referencedCode = @"
[System.Runtime.InteropServices.TypeLibType((short)System.Runtime.InteropServices.TypeLibTypeFlags.FHidden)]
public class Goo
{
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_TypeLibType_HiddenAndOtherFlags_Int16Constructor()
        {
            var markup = @"
class Program
{
    void M()
    {
        new $$
    }
}";

            var referencedCode = @"
[System.Runtime.InteropServices.TypeLibType((short)(System.Runtime.InteropServices.TypeLibTypeFlags.FHidden | System.Runtime.InteropServices.TypeLibTypeFlags.FLicensed))]
public class Goo
{
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_TypeLibFunc_NotHidden()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Goo().$$
    }
}";

            var referencedCode = @"
public class Goo
{
    [System.Runtime.InteropServices.TypeLibFunc(System.Runtime.InteropServices.TypeLibFuncFlags.FReplaceable)]
    public void Bar()
    {
    }
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_TypeLibFunc_Hidden()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Goo().$$
    }
}";

            var referencedCode = @"
public class Goo
{
    [System.Runtime.InteropServices.TypeLibFunc(System.Runtime.InteropServices.TypeLibFuncFlags.FHidden)]
    public void Bar()
    {
    }
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_TypeLibFunc_HiddenAndOtherFlags()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Goo().$$
    }
}";

            var referencedCode = @"
public class Goo
{
    [System.Runtime.InteropServices.TypeLibFunc(System.Runtime.InteropServices.TypeLibFuncFlags.FHidden | System.Runtime.InteropServices.TypeLibFuncFlags.FReplaceable)]
    public void Bar()
    {
    }
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_TypeLibFunc_NotHidden_Int16Constructor()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Goo().$$
    }
}";

            var referencedCode = @"
public class Goo
{
    [System.Runtime.InteropServices.TypeLibFunc((short)System.Runtime.InteropServices.TypeLibFuncFlags.FReplaceable)]
    public void Bar()
    {
    }
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_TypeLibFunc_Hidden_Int16Constructor()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Goo().$$
    }
}";

            var referencedCode = @"
public class Goo
{
    [System.Runtime.InteropServices.TypeLibFunc((short)System.Runtime.InteropServices.TypeLibFuncFlags.FHidden)]
    public void Bar()
    {
    }
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_TypeLibFunc_HiddenAndOtherFlags_Int16Constructor()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Goo().$$
    }
}";

            var referencedCode = @"
public class Goo
{
    [System.Runtime.InteropServices.TypeLibFunc((short)(System.Runtime.InteropServices.TypeLibFuncFlags.FHidden | System.Runtime.InteropServices.TypeLibFuncFlags.FReplaceable))]
    public void Bar()
    {
    }
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_TypeLibVar_NotHidden()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Goo().$$
    }
}";

            var referencedCode = @"
public class Goo
{
    [System.Runtime.InteropServices.TypeLibVar(System.Runtime.InteropServices.TypeLibVarFlags.FReplaceable)]
    public int bar;
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_TypeLibVar_Hidden()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Goo().$$
    }
}";

            var referencedCode = @"
public class Goo
{
    [System.Runtime.InteropServices.TypeLibVar(System.Runtime.InteropServices.TypeLibVarFlags.FHidden)]
    public int bar;
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_TypeLibVar_HiddenAndOtherFlags()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Goo().$$
    }
}";

            var referencedCode = @"
public class Goo
{
    [System.Runtime.InteropServices.TypeLibVar(System.Runtime.InteropServices.TypeLibVarFlags.FHidden | System.Runtime.InteropServices.TypeLibVarFlags.FReplaceable)]
    public int bar;
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_TypeLibVar_NotHidden_Int16Constructor()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Goo().$$
    }
}";

            var referencedCode = @"
public class Goo
{
    [System.Runtime.InteropServices.TypeLibVar((short)System.Runtime.InteropServices.TypeLibVarFlags.FReplaceable)]
    public int bar;
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_TypeLibVar_Hidden_Int16Constructor()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Goo().$$
    }
}";

            var referencedCode = @"
public class Goo
{
    [System.Runtime.InteropServices.TypeLibVar((short)System.Runtime.InteropServices.TypeLibVarFlags.FHidden)]
    public int bar;
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(7336, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EditorBrowsable_TypeLibVar_HiddenAndOtherFlags_Int16Constructor()
        {
            var markup = @"
class Program
{
    void M()
    {
        new Goo().$$
    }
}";

            var referencedCode = @"
public class Goo
{
    [System.Runtime.InteropServices.TypeLibVar((short)(System.Runtime.InteropServices.TypeLibVarFlags.FHidden | System.Runtime.InteropServices.TypeLibVarFlags.FReplaceable))]
    public int bar;
}";
            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem(545557, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545557")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestColorColor1()
        {
            var markup = @"
class A
{
    static void Goo() { }
    void Bar() { }
 
    static void Main()
    {
        A A = new A();
        A.$$
    }
}";

            await VerifyItemExistsAsync(markup, "Goo");
            await VerifyItemExistsAsync(markup, "Bar");
        }

        [WorkItem(545647, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545647")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestLaterLocalHidesType1()
        {
            var markup = @"
using System;
class C
{
    public static void Main()
    {
        $$
        Console.WriteLine();
    }
}";

            await VerifyItemExistsAsync(markup, "Console");
        }

        [WorkItem(545647, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545647")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestLaterLocalHidesType2()
        {
            var markup = @"
using System;
class C
{
    public static void Main()
    {
        C$$
        Console.WriteLine();
    }
}";

            await VerifyItemExistsAsync(markup, "Console");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestIndexedProperty()
        {
            var markup = @"class Program
{
    void M()
    {
            CCC c = new CCC();
            c.$$
    }
}";

            // Note that <COMImport> is required by compiler.  Bug 17013 tracks enabling indexed property for non-COM types.
            var referencedCode = @"Imports System.Runtime.InteropServices

<ComImport()>
<GuidAttribute(CCC.ClassId)>
Public Class CCC

#Region ""COM GUIDs""
    Public Const ClassId As String = ""9d965fd2-1514-44f6-accd-257ce77c46b0""
    Public Const InterfaceId As String = ""a9415060-fdf0-47e3-bc80-9c18f7f39cf6""
    Public Const EventsId As String = ""c6a866a5-5f97-4b53-a5df-3739dc8ff1bb""
# End Region

            ''' <summary>
    ''' An index property from VB
    ''' </summary>
    ''' <param name=""p1"">p1 is an integer index</param>
    ''' <returns>A string</returns>
    Public Property IndexProp(ByVal p1 As Integer, Optional ByVal p2 As Integer = 0) As String
        Get
            Return Nothing
        End Get
        Set(ByVal value As String)

        End Set
    End Property
End Class";

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "IndexProp",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.VisualBasic);
        }

        [WorkItem(546841, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546841")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestDeclarationAmbiguity()
        {
            var markup = @"
using System;

class Program
{
    void Main()
    {
        Environment.$$
        var v;
    }
}";

            await VerifyItemExistsAsync(markup, "CommandLine");
        }

        [WorkItem(12781, "https://github.com/dotnet/roslyn/issues/12781")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestFieldDeclarationAmbiguity()
        {
            var markup = @"
using System;
Environment.$$
var v;
}";

            await VerifyItemExistsAsync(markup, "CommandLine", sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestCursorOnClassCloseBrace()
        {
            var markup = @"
using System;

class Outer
{
    class Inner { }

$$}";

            await VerifyItemExistsAsync(markup, "Inner");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterAsync1()
        {
            var markup = @"
using System.Threading.Tasks;
class Program
{
    async $$
}";

            await VerifyItemExistsAsync(markup, "Task");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task AfterAsync2()
        {
            var markup = @"
using System.Threading.Tasks;
class Program
{
    public async T$$
}";

            await VerifyItemExistsAsync(markup, "Task");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotAfterAsyncInMethodBody()
        {
            var markup = @"
using System.Threading.Tasks;
class Program
{
    void goo()
    {
        var x = async $$
    }
}";

            await VerifyItemIsAbsentAsync(markup, "Task");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotAwaitable1()
        {
            var markup = @"
class Program
{
    void goo()
    {
        $$
    }
}";

            await VerifyItemWithMscorlib45Async(markup, "goo", "void Program.goo()", "C#");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NotAwaitable2()
        {
            var markup = @"
class Program
{
    async void goo()
    {
        $$
    }
}";

            await VerifyItemWithMscorlib45Async(markup, "goo", "void Program.goo()", "C#");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task Awaitable1()
        {
            var markup = @"
using System.Threading;
using System.Threading.Tasks;

class Program
{
    async Task goo()
    {
        $$
    }
}";

            var description = $@"({CSharpFeaturesResources.awaitable}) Task Program.goo()
{WorkspacesResources.Usage_colon}
  {SyntaxFacts.GetText(SyntaxKind.AwaitKeyword)} goo();";

            await VerifyItemWithMscorlib45Async(markup, "goo", description, "C#");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task Awaitable2()
        {
            var markup = @"
using System.Threading.Tasks;

class Program
{
    async Task<int> goo()
    {
        $$
    }
}";

            var description = $@"({CSharpFeaturesResources.awaitable}) Task<int> Program.goo()
{WorkspacesResources.Usage_colon}
  int x = {SyntaxFacts.GetText(SyntaxKind.AwaitKeyword)} goo();";

            await VerifyItemWithMscorlib45Async(markup, "goo", description, "C#");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ObsoleteItem()
        {
            var markup = @"
using System;

class Program
{
    [Obsolete]
    public void goo()
    {
        $$
    }
}";
            await VerifyItemExistsAsync(markup, "goo", $"[{CSharpFeaturesResources.deprecated}] void Program.goo()");
        }

        [WorkItem(568986, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/568986")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoMembersOnDottingIntoUnboundType()
        {
            var markup = @"
class Program
{
    RegistryKey goo;
 
    static void Main(string[] args)
    {
        goo.$$
    }
}";
            await VerifyNoItemsExistAsync(markup);
        }

        [WorkItem(550717, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/550717")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TypeArgumentsInConstraintAfterBaselist()
        {
            var markup = @"
public class Goo<T> : System.Object where $$
{
}";
            await VerifyItemExistsAsync(markup, "T");
        }

        [WorkItem(647175, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/647175")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoDestructor()
        {
            var markup = @"
class C
{
    ~C()
    {
        $$
";
            await VerifyItemIsAbsentAsync(markup, "Finalize");
        }

        [WorkItem(669624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/669624")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ExtensionMethodOnCovariantInterface()
        {
            var markup = @"
class Schema<T> { }

interface ISet<out T> { }

static class SetMethods
{
    public static void ForSchemaSet<T>(this ISet<Schema<T>> set) { }
}

class Context
{
    public ISet<T> Set<T>() { return null; }
}

class CustomSchema : Schema<int> { }

class Program
{
    static void Main(string[] args)
    {
        var set = new Context().Set<CustomSchema>();

        set.$$
";

            await VerifyItemExistsAsync(markup, "ForSchemaSet", displayTextSuffix: "<>", sourceCodeKind: SourceCodeKind.Regular);
        }

        [WorkItem(667752, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/667752")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ForEachInsideParentheses()
        {
            var markup = @"
using System;
class C
{
    void M()
    {
        foreach($$)
";

            await VerifyItemExistsAsync(markup, "String");
        }

        [WorkItem(766869, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/766869")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestFieldInitializerInP2P()
        {
            var markup = @"
class Class
{
    int i = Consts.$$;
}";

            var referencedCode = @"
public static class Consts
{
    public const int C = 1;
}";
            await VerifyItemWithProjectReferenceAsync(markup, referencedCode, "C", 1, LanguageNames.CSharp, LanguageNames.CSharp, false);
        }

        [WorkItem(834605, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/834605")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ShowWithEqualsSign()
        {
            var markup = @"
class c { public int value {set; get; }}

class d
{
    void goo()
    {
       c goo = new c { value$$=
    }
}";

            await VerifyNoItemsExistAsync(markup);
        }

        [WorkItem(825661, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/825661")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NothingAfterThisDotInStaticContext()
        {
            var markup = @"
class C
{
    void M1() { }

    static void M2()
    {
        this.$$
    }
}";

            await VerifyNoItemsExistAsync(markup);
        }

        [WorkItem(825661, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/825661")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NothingAfterBaseDotInStaticContext()
        {
            var markup = @"
class C
{
    void M1() { }

    static void M2()
    {
        base.$$
    }
}";

            await VerifyNoItemsExistAsync(markup);
        }

        [WorkItem(7648, "http://github.com/dotnet/roslyn/issues/7648")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NothingAfterBaseDotInScriptContext()
        {
            await VerifyItemIsAbsentAsync(@"base.$$", @"ToString", sourceCodeKind: SourceCodeKind.Script);
        }

        [WorkItem(858086, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/858086")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoNestedTypeWhenDisplayingInstance()
        {
            var markup = @"
class C
{
    class D
    {
    }

    void M2()
    {
        new C().$$
    }
}";

            await VerifyItemIsAbsentAsync(markup, "D");
        }

        [WorkItem(876031, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/876031")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CatchVariableInExceptionFilter()
        {
            var markup = @"
class C
{
    void M()
    {
        try
        {
        }
        catch (System.Exception myExn) when ($$";

            await VerifyItemExistsAsync(markup, "myExn");
        }

        [WorkItem(849698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/849698")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CompletionAfterExternAlias()
        {
            var markup = @"
class C
{
    void goo()
    {
        global::$$
    }
}";

            await VerifyItemExistsAsync(markup, "System", usePreviousCharAsTrigger: true);
        }

        [WorkItem(849698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/849698")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ExternAliasSuggested()
        {
            var markup = @"
extern alias Bar;
class C
{
    void goo()
    {
        $$
    }
}";
            await VerifyItemWithAliasedMetadataReferencesAsync(markup, "Bar", "Bar", 1, "C#", "C#", false);
        }

        [WorkItem(635957, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/635957")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ClassDestructor()
        {
            var markup = @"
class C
{
    class N
    {
    ~$$
    }
}";
            await VerifyItemExistsAsync(markup, "N");
            await VerifyItemIsAbsentAsync(markup, "C");
        }

        [WorkItem(635957, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/635957")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TildeOutsideClass()
        {
            var markup = @"
class C
{
    class N
    {
    }
}
~$$";
            await VerifyNoItemsExistAsync(markup, SourceCodeKind.Regular);
        }

        [WorkItem(635957, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/635957")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task StructDestructor()
        {
            var markup = @"
struct C
{
   ~$$
}";
            await VerifyItemExistsAsync(markup, "C");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task FieldAvailableInBothLinkedFiles()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
class C
{
    int x;
    void goo()
    {
        $$
    }
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
</Workspace>";

            await VerifyItemInLinkedFilesAsync(markup, "x", $"({FeaturesResources.field}) int C.x");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task FieldUnavailableInOneLinkedFile()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""GOO"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
class C
{
#if GOO
    int x;
#endif
    void goo()
    {
        $$
    }
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
</Workspace>";
            var expectedDescription = $"({FeaturesResources.field}) int C.x\r\n\r\n{string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available)}\r\n{string.Format(FeaturesResources._0_1, "Proj2", FeaturesResources.Not_Available)}\r\n\r\n{FeaturesResources.You_can_use_the_navigation_bar_to_switch_context}";

            await VerifyItemInLinkedFilesAsync(markup, "x", expectedDescription);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task FieldUnavailableInTwoLinkedFiles()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""GOO"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
class C
{
#if GOO
    int x;
#endif
    void goo()
    {
        $$
    }
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj3"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
</Workspace>";
            var expectedDescription = $"({FeaturesResources.field}) int C.x\r\n\r\n{string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available)}\r\n{string.Format(FeaturesResources._0_1, "Proj2", FeaturesResources.Not_Available)}\r\n{string.Format(FeaturesResources._0_1, "Proj3", FeaturesResources.Not_Available)}\r\n\r\n{FeaturesResources.You_can_use_the_navigation_bar_to_switch_context}";

            await VerifyItemInLinkedFilesAsync(markup, "x", expectedDescription);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ExcludeFilesWithInactiveRegions()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""GOO,BAR"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
class C
{
#if GOO
    int x;
#endif

#if BAR
    void goo()
    {
        $$
    }
#endif
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs"" />
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj3"" PreprocessorSymbols=""BAR"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
</Workspace>";
            var expectedDescription = $"({FeaturesResources.field}) int C.x\r\n\r\n{string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available)}\r\n{string.Format(FeaturesResources._0_1, "Proj3", FeaturesResources.Not_Available)}\r\n\r\n{FeaturesResources.You_can_use_the_navigation_bar_to_switch_context}";

            await VerifyItemInLinkedFilesAsync(markup, "x", expectedDescription);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task UnionOfItemsFromBothContexts()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""GOO"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
class C
{
#if GOO
    int x;
#endif

#if BAR
    class G
    {
        public void DoGStuff() {}
    }
#endif
    void goo()
    {
        new G().$$
    }
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"" PreprocessorSymbols=""BAR"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj3"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
</Workspace>";
            var expectedDescription = $"void G.DoGStuff()\r\n\r\n{string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Not_Available)}\r\n{string.Format(FeaturesResources._0_1, "Proj2", FeaturesResources.Available)}\r\n{string.Format(FeaturesResources._0_1, "Proj3", FeaturesResources.Not_Available)}\r\n\r\n{FeaturesResources.You_can_use_the_navigation_bar_to_switch_context}";

            await VerifyItemInLinkedFilesAsync(markup, "DoGStuff", expectedDescription);
        }

        [WorkItem(1020944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1020944")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task LocalsValidInLinkedDocuments()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
class C
{
    void M()
    {
        int xyz;
        $$
    }
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
</Workspace>";
            var expectedDescription = $"({FeaturesResources.local_variable}) int xyz";
            await VerifyItemInLinkedFilesAsync(markup, "xyz", expectedDescription);
        }

        [WorkItem(1020944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1020944")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task LocalWarningInLinkedDocuments()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""PROJ1"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
class C
{
    void M()
    {
#if PROJ1
        int xyz;
#endif
        $$
    }
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
</Workspace>";
            var expectedDescription = $"({FeaturesResources.local_variable}) int xyz\r\n\r\n{string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available)}\r\n{string.Format(FeaturesResources._0_1, "Proj2", FeaturesResources.Not_Available)}\r\n\r\n{FeaturesResources.You_can_use_the_navigation_bar_to_switch_context}";
            await VerifyItemInLinkedFilesAsync(markup, "xyz", expectedDescription);
        }

        [WorkItem(1020944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1020944")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task LabelsValidInLinkedDocuments()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
class C
{
    void M()
    {
LABEL:  int xyz;
        goto $$
    }
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
</Workspace>";
            var expectedDescription = $"({FeaturesResources.label}) LABEL";
            await VerifyItemInLinkedFilesAsync(markup, "LABEL", expectedDescription);
        }

        [WorkItem(1020944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1020944")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task RangeVariablesValidInLinkedDocuments()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
using System.Linq;
class C
{
    void M()
    {
        var x = from y in new[] { 1, 2, 3 } select $$
    }
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
</Workspace>";
            var expectedDescription = $"({FeaturesResources.range_variable}) ? y";
            await VerifyItemInLinkedFilesAsync(markup, "y", expectedDescription);
        }

        [WorkItem(1063403, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1063403")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MethodOverloadDifferencesIgnored()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""ONE"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
class C
{
#if ONE
    void Do(int x){}
#endif
#if TWO
    void Do(string x){}
#endif

    void Shared()
    {
        $$
    }

}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"" PreprocessorSymbols=""TWO"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
</Workspace>";

            var expectedDescription = $"void C.Do(int x)";
            await VerifyItemInLinkedFilesAsync(markup, "Do", expectedDescription);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MethodOverloadDifferencesIgnored_ExtensionMethod()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""ONE"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
class C
{
#if ONE
    void Do(int x){}
#endif

    void Shared()
    {
        this.$$
    }

}

public static class Extensions
{
#if TWO
    public static void Do (this C c, string x)
    {
    }
#endif
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"" PreprocessorSymbols=""TWO"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
</Workspace>";

            var expectedDescription = $"void C.Do(int x)";
            await VerifyItemInLinkedFilesAsync(markup, "Do", expectedDescription);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MethodOverloadDifferencesIgnored_ExtensionMethod2()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""TWO"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
class C
{
#if ONE
    void Do(int x){}
#endif

    void Shared()
    {
        this.$$
    }

}

public static class Extensions
{
#if TWO
    public static void Do (this C c, string x)
    {
    }
#endif
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"" PreprocessorSymbols=""ONE"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
</Workspace>";

            var expectedDescription = $"({CSharpFeaturesResources.extension}) void C.Do(string x)";
            await VerifyItemInLinkedFilesAsync(markup, "Do", expectedDescription);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MethodOverloadDifferencesIgnored_ContainingType()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""ONE"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
class C
{
    void Shared()
    {
        var x = GetThing();
        x.$$
    }

#if ONE
    private Methods1 GetThing()
    {
        return new Methods1();
    }
#endif

#if TWO
    private Methods2 GetThing()
    {
        return new Methods2();
    }
#endif
}

#if ONE
public class Methods1
{
    public void Do(string x) { }
}
#endif

#if TWO
public class Methods2
{
    public void Do(string x) { }
}
#endif
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"" PreprocessorSymbols=""TWO"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
</Workspace>";

            var expectedDescription = $"void Methods1.Do(string x)";
            await VerifyItemInLinkedFilesAsync(markup, "Do", expectedDescription);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SharedProjectFieldAndPropertiesTreatedAsIdentical()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""ONE"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
class C
{
#if ONE
    public int x;
#endif
#if TWO
    public int x {get; set;}
#endif
    void goo()
    {
        x$$
    }
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"" PreprocessorSymbols=""TWO"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
</Workspace>";

            var expectedDescription = $"({ FeaturesResources.field }) int C.x";
            await VerifyItemInLinkedFilesAsync(markup, "x", expectedDescription);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SharedProjectFieldAndPropertiesTreatedAsIdentical2()
        {
            var markup = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" PreprocessorSymbols=""ONE"">
        <Document FilePath=""CurrentDocument.cs""><![CDATA[
class C
{
#if TWO
    public int x;
#endif
#if ONE
    public int x {get; set;}
#endif
    void goo()
    {
        x$$
    }
}
]]>
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"" PreprocessorSymbols=""TWO"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
</Workspace>";

            var expectedDescription = "int C.x { get; set; }";
            await VerifyItemInLinkedFilesAsync(markup, "x", expectedDescription);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ConditionalAccessWalkUp()
        {
            var markup = @"
public class B
{
    public A BA;
    public B BB;
}

class A
{
    public A AA;
    public A AB;
    public int? x;

    public void goo()
    {
        A a = null;
        var q = a?.$$AB.BA.AB.BA;
    }
}";
            await VerifyItemExistsAsync(markup, "AA");
            await VerifyItemExistsAsync(markup, "AB");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ConditionalAccessNullableIsUnwrapped()
        {
            var markup = @"
public struct S
{
    public int? i;
}

class A
{
    public S? s;

    public void goo()
    {
        A a = null;
        var q = a?.s?.$$;
    }
}";
            await VerifyItemExistsAsync(markup, "i");
            await VerifyItemIsAbsentAsync(markup, "value");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ConditionalAccessNullableIsUnwrapped2()
        {
            var markup = @"
public struct S
{
    public int? i;
}

class A
{
    public S? s;

    public void goo()
    {
        var q = s?.$$i?.ToString();
    }
}";
            await VerifyItemExistsAsync(markup, "i");
            await VerifyItemIsAbsentAsync(markup, "value");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CompletionAfterConditionalIndexing()
        {
            var markup = @"
public struct S
{
    public int? i;
}

class A
{
    public S[] s;

    public void goo()
    {
        A a = null;
        var q = a?.s?[$$;
    }
}";
            await VerifyItemExistsAsync(markup, "System");
        }

        [WorkItem(1109319, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1109319")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task WithinChainOfConditionalAccesses()
        {
            var markup = @"
class Program
{
    static void Main(string[] args)
    {
        A a;
        var x = a?.$$b?.c?.d.e;
    }
}

class A { public B b; }
class B { public C c; }
class C { public D d; }
class D { public int e; }";
            await VerifyItemExistsAsync(markup, "b");
        }

        [WorkItem(843466, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/843466")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NestedAttributeAccessibleOnSelf()
        {
            var markup = @"using System;
[My]
class X
{
    [My$$]
    class MyAttribute : Attribute
    {

    }
}";
            await VerifyItemExistsAsync(markup, "My");
        }

        [WorkItem(843466, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/843466")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NestedAttributeAccessibleOnOuterType()
        {
            var markup = @"using System;

[My]
class Y
{

}

[$$]
class X
{
    [My]
    class MyAttribute : Attribute
    {

    }
}";
            await VerifyItemExistsAsync(markup, "My");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InstanceMembersFromBaseOuterType()
        {
            var markup = @"abstract class Test
{
  private int _field;

  public sealed class InnerTest : Test 
  {
    
    public void SomeTest() 
    {
        $$
    }
  }
}";
            await VerifyItemExistsAsync(markup, "_field");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InstanceMembersFromBaseOuterType2()
        {
            var markup = @"class C<T>
{
    void M() { }
    class N : C<int>
    {
        void Test()
        {
            $$ // M recommended and accessible
        }

        class NN
        {
            void Test2()
            {
                // M inaccessible and not recommended
            }
        }
    }
}";
            await VerifyItemExistsAsync(markup, "M");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InstanceMembersFromBaseOuterType3()
        {
            var markup = @"class C<T>
{
    void M() { }
    class N : C<int>
    {
        void Test()
        {
            M(); // M recommended and accessible
        }

        class NN
        {
            void Test2()
            {
                $$ // M inaccessible and not recommended
            }
        }
    }
}";
            await VerifyItemIsAbsentAsync(markup, "M");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InstanceMembersFromBaseOuterType4()
        {
            var markup = @"class C<T>
{
    void M() { }
    class N : C<int>
    {
        void Test()
        {
            M(); // M recommended and accessible
        }

        class NN : N
        {
            void Test2()
            {
                $$ // M accessible and recommended.
            }
        }
    }
}";
            await VerifyItemExistsAsync(markup, "M");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InstanceMembersFromBaseOuterType5()
        {
            var markup = @"
class D
{
    public void Q() { }
}
class C<T> : D
{
    class N
    {
        void Test()
        {
            $$
        }
    }
}";
            await VerifyItemIsAbsentAsync(markup, "Q");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InstanceMembersFromBaseOuterType6()
        {
            var markup = @"
class Base<T>
{
    public int X;
}

class Derived : Base<int>
{
    class Nested
    {
        void Test()
        {
            $$
        }
    }
}";
            await VerifyItemIsAbsentAsync(markup, "X");
        }

        [WorkItem(983367, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/983367")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoTypeParametersDefinedInCrefs()
        {
            var markup = @"using System;

/// <see cref=""Program{T$$}""/>
class Program<T> { }";
            await VerifyItemIsAbsentAsync(markup, "T");
        }

        [WorkItem(988025, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/988025")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ShowTypesInGenericMethodTypeParameterList1()
        {
            var markup = @"
class Class1<T, D>
{
    public static Class1<T, D> Create() { return null; }
}
static class Class2
{
    public static void Test<T,D>(this Class1<T, D> arg)
    {
    }
}
class Program
{
    static void Main(string[] args)
    {
        Class1<string, int>.Create().Test<$$
    }
}
";
            await VerifyItemExistsAsync(markup, "Class1", displayTextSuffix: "<>", sourceCodeKind: SourceCodeKind.Regular);
        }

        [WorkItem(988025, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/988025")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ShowTypesInGenericMethodTypeParameterList2()
        {
            var markup = @"
class Class1<T, D>
{
    public static Class1<T, D> Create() { return null; }
}
static class Class2
{
    public static void Test<T,D>(this Class1<T, D> arg)
    {
    }
}
class Program
{
    static void Main(string[] args)
    {
        Class1<string, int>.Create().Test<string,$$
    }
}
";
            await VerifyItemExistsAsync(markup, "Class1", displayTextSuffix: "<>", sourceCodeKind: SourceCodeKind.Regular);
        }

        [WorkItem(991466, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/991466")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task DescriptionInAliasedType()
        {
            var markup = @"
using IAlias = IGoo;
///<summary>summary for interface IGoo</summary>
interface IGoo {  }
class C 
{ 
    I$$
}
";
            await VerifyItemExistsAsync(markup, "IAlias", expectedDescriptionOrNull: "interface IGoo\r\nsummary for interface IGoo");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task WithinNameOf()
        {
            var markup = @"
class C 
{ 
    void goo()
    {
        var x = nameof($$)
    }
}
";
            await VerifyAnyItemExistsAsync(markup);
        }

        [WorkItem(997410, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/997410")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InstanceMemberInNameOfInStaticContext()
        {
            var markup = @"
class C
{
  int y1 = 15;
  static int y2 = 1;
  static string x = nameof($$
";
            await VerifyItemExistsAsync(markup, "y1");
        }

        [WorkItem(997410, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/997410")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task StaticMemberInNameOfInStaticContext()
        {
            var markup = @"
class C
{
  int y1 = 15;
  static int y2 = 1;
  static string x = nameof($$
";
            await VerifyItemExistsAsync(markup, "y2");
        }

        [WorkItem(883293, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/883293")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task IncompleteDeclarationExpressionType()
        {
            var markup = @"
using System;
class C
{
  void goo()
    {
        var x = Console.$$
        var y = 3;
    }
}
";
            await VerifyItemExistsAsync(markup, "WriteLine");
        }

        [WorkItem(1024380, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1024380")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task StaticAndInstanceInNameOf()
        {
            var markup = @"
using System;
class C
{
    class D
    {
        public int x;
        public static int y;   
    }

  void goo()
    {
        var z = nameof(C.D.$$
    }
}
";
            await VerifyItemExistsAsync(markup, "x");
            await VerifyItemExistsAsync(markup, "y");
        }

        [WorkItem(1663, "https://github.com/dotnet/roslyn/issues/1663")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NameOfMembersListedForLocals()
        {
            var markup = @"class C
{
    void M()
    {
        var x = nameof(T.z.$$)
    }
}
 
public class T
{
    public U z; 
}
 
public class U
{
    public int nope;
}
";
            await VerifyItemExistsAsync(markup, "nope");
        }

        [WorkItem(1029522, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1029522")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NameOfMembersListedForNamespacesAndTypes2()
        {
            var markup = @"class C
{
    void M()
    {
        var x = nameof(U.$$)
    }
}
 
public class T
{
    public U z; 
}
 
public class U
{
    public int nope;
}
";
            await VerifyItemExistsAsync(markup, "nope");
        }

        [WorkItem(1029522, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1029522")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NameOfMembersListedForNamespacesAndTypes3()
        {
            var markup = @"class C
{
    void M()
    {
        var x = nameof(N.$$)
    }
}

namespace N
{
public class U
{
    public int nope;
}
} ";
            await VerifyItemExistsAsync(markup, "U");
        }

        [WorkItem(1029522, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1029522")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NameOfMembersListedForNamespacesAndTypes4()
        {
            var markup = @"
using z = System;
class C
{
    void M()
    {
        var x = nameof(z.$$)
    }
}
";
            await VerifyItemExistsAsync(markup, "Console");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InterpolatedStrings1()
        {
            var markup = @"
class C
{
    void M()
    {
        var a = ""Hello"";
        var b = ""World"";
        var c = $""{$$
";
            await VerifyItemExistsAsync(markup, "a");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InterpolatedStrings2()
        {
            var markup = @"
class C
{
    void M()
    {
        var a = ""Hello"";
        var b = ""World"";
        var c = $""{$$}"";
    }
}";
            await VerifyItemExistsAsync(markup, "a");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InterpolatedStrings3()
        {
            var markup = @"
class C
{
    void M()
    {
        var a = ""Hello"";
        var b = ""World"";
        var c = $""{a}, {$$
";
            await VerifyItemExistsAsync(markup, "b");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InterpolatedStrings4()
        {
            var markup = @"
class C
{
    void M()
    {
        var a = ""Hello"";
        var b = ""World"";
        var c = $""{a}, {$$}"";
    }
}";
            await VerifyItemExistsAsync(markup, "b");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InterpolatedStrings5()
        {
            var markup = @"
class C
{
    void M()
    {
        var a = ""Hello"";
        var b = ""World"";
        var c = $@""{a}, {$$
";
            await VerifyItemExistsAsync(markup, "b");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InterpolatedStrings6()
        {
            var markup = @"
class C
{
    void M()
    {
        var a = ""Hello"";
        var b = ""World"";
        var c = $@""{a}, {$$}"";
    }
}";
            await VerifyItemExistsAsync(markup, "b");
        }

        [WorkItem(1064811, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064811")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotBeforeFirstStringHole()
        {
            await VerifyNoItemsExistAsync(AddInsideMethod(
@"var x = ""\{0}$$\{1}\{2}"""));
        }

        [WorkItem(1064811, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064811")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotBetweenStringHoles()
        {
            await VerifyNoItemsExistAsync(AddInsideMethod(
@"var x = ""\{0}\{1}$$\{2}"""));
        }

        [WorkItem(1064811, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064811")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotAfterStringHoles()
        {
            await VerifyNoItemsExistAsync(AddInsideMethod(
@"var x = ""\{0}\{1}\{2}$$"""));
        }

        [WorkItem(1087171, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1087171")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task CompletionAfterTypeOfGetType()
        {
            await VerifyItemExistsAsync(AddInsideMethod(
"typeof(int).GetType().$$"), "GUID");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task UsingDirectives1()
        {
            var markup = @"
using $$

class A { }
static class B { }

namespace N
{
    class C { }
    static class D { }

    namespace M { }
}";

            await VerifyItemIsAbsentAsync(markup, "A");
            await VerifyItemIsAbsentAsync(markup, "B");
            await VerifyItemExistsAsync(markup, "N");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task UsingDirectives2()
        {
            var markup = @"
using N.$$

class A { }
static class B { }

namespace N
{
    class C { }
    static class D { }

    namespace M { }
}";

            await VerifyItemIsAbsentAsync(markup, "C");
            await VerifyItemIsAbsentAsync(markup, "D");
            await VerifyItemExistsAsync(markup, "M");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task UsingDirectives3()
        {
            var markup = @"
using G = $$

class A { }
static class B { }

namespace N
{
    class C { }
    static class D { }

    namespace M { }
}";

            await VerifyItemExistsAsync(markup, "A");
            await VerifyItemExistsAsync(markup, "B");
            await VerifyItemExistsAsync(markup, "N");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task UsingDirectives4()
        {
            var markup = @"
using G = N.$$

class A { }
static class B { }

namespace N
{
    class C { }
    static class D { }

    namespace M { }
}";

            await VerifyItemExistsAsync(markup, "C");
            await VerifyItemExistsAsync(markup, "D");
            await VerifyItemExistsAsync(markup, "M");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task UsingDirectives5()
        {
            var markup = @"
using static $$

class A { }
static class B { }

namespace N
{
    class C { }
    static class D { }

    namespace M { }
}";

            await VerifyItemExistsAsync(markup, "A");
            await VerifyItemExistsAsync(markup, "B");
            await VerifyItemExistsAsync(markup, "N");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task UsingDirectives6()
        {
            var markup = @"
using static N.$$

class A { }
static class B { }

namespace N
{
    class C { }
    static class D { }

    namespace M { }
}";

            await VerifyItemExistsAsync(markup, "C");
            await VerifyItemExistsAsync(markup, "D");
            await VerifyItemExistsAsync(markup, "M");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task UsingStaticDoesNotShowDelegates1()
        {
            var markup = @"
using static $$

class A { }
delegate void B();

namespace N
{
    class C { }
    static class D { }

    namespace M { }
}";

            await VerifyItemExistsAsync(markup, "A");
            await VerifyItemIsAbsentAsync(markup, "B");
            await VerifyItemExistsAsync(markup, "N");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task UsingStaticDoesNotShowDelegates2()
        {
            var markup = @"
using static N.$$

class A { }
static class B { }

namespace N
{
    class C { }
    delegate void D();

    namespace M { }
}";

            await VerifyItemExistsAsync(markup, "C");
            await VerifyItemIsAbsentAsync(markup, "D");
            await VerifyItemExistsAsync(markup, "M");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task UsingStaticDoesNotShowInterfaces1()
        {
            var markup = @"
using static N.$$

class A { }
static class B { }

namespace N
{
    class C { }
    interface I { }

    namespace M { }
}";

            await VerifyItemExistsAsync(markup, "C");
            await VerifyItemIsAbsentAsync(markup, "I");
            await VerifyItemExistsAsync(markup, "M");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task UsingStaticDoesNotShowInterfaces2()
        {
            var markup = @"
using static $$

class A { }
interface I { }

namespace N
{
    class C { }
    static class D { }

    namespace M { }
}";

            await VerifyItemExistsAsync(markup, "A");
            await VerifyItemIsAbsentAsync(markup, "I");
            await VerifyItemExistsAsync(markup, "N");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task UsingStaticAndExtensionMethods1()
        {
            var markup = @"
using static A;
using static B;

static class A
{
    public static void Goo(this string s) { }
}

static class B
{
    public static void Bar(this string s) { }
}

class C
{
    void M()
    {
        $$
    }
}
";

            await VerifyItemIsAbsentAsync(markup, "Goo");
            await VerifyItemIsAbsentAsync(markup, "Bar");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task UsingStaticAndExtensionMethods2()
        {
            var markup = @"
using N;

namespace N
{
    static class A
    {
        public static void Goo(this string s) { }
    }

    static class B
    {
        public static void Bar(this string s) { }
    }
}

class C
{
    void M()
    {
        $$
    }
}
";

            await VerifyItemIsAbsentAsync(markup, "Goo");
            await VerifyItemIsAbsentAsync(markup, "Bar");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task UsingStaticAndExtensionMethods3()
        {
            var markup = @"
using N;

namespace N
{
    static class A
    {
        public static void Goo(this string s) { }
    }

    static class B
    {
        public static void Bar(this string s) { }
    }
}

class C
{
    void M()
    {
        string s;
        s.$$
    }
}
";

            await VerifyItemExistsAsync(markup, "Goo");
            await VerifyItemExistsAsync(markup, "Bar");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task UsingStaticAndExtensionMethods4()
        {
            var markup = @"
using static N.A;
using static N.B;

namespace N
{
    static class A
    {
        public static void Goo(this string s) { }
    }

    static class B
    {
        public static void Bar(this string s) { }
    }
}

class C
{
    void M()
    {
        string s;
        s.$$
    }
}
";

            await VerifyItemExistsAsync(markup, "Goo");
            await VerifyItemExistsAsync(markup, "Bar");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task UsingStaticAndExtensionMethods5()
        {
            var markup = @"
using static N.A;

namespace N
{
    static class A
    {
        public static void Goo(this string s) { }
    }

    static class B
    {
        public static void Bar(this string s) { }
    }
}

class C
{
    void M()
    {
        string s;
        s.$$
    }
}
";

            await VerifyItemExistsAsync(markup, "Goo");
            await VerifyItemIsAbsentAsync(markup, "Bar");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task UsingStaticAndExtensionMethods6()
        {
            var markup = @"
using static N.B;

namespace N
{
    static class A
    {
        public static void Goo(this string s) { }
    }

    static class B
    {
        public static void Bar(this string s) { }
    }
}

class C
{
    void M()
    {
        string s;
        s.$$
    }
}
";

            await VerifyItemIsAbsentAsync(markup, "Goo");
            await VerifyItemExistsAsync(markup, "Bar");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task UsingStaticAndExtensionMethods7()
        {
            var markup = @"
using N;
using static N.B;

namespace N
{
    static class A
    {
        public static void Goo(this string s) { }
    }

    static class B
    {
        public static void Bar(this string s) { }
    }
}

class C
{
    void M()
    {
        string s;
        s.$$;
    }
}
";

            await VerifyItemExistsAsync(markup, "Goo");
            await VerifyItemExistsAsync(markup, "Bar");
        }

        [WorkItem(7932, "https://github.com/dotnet/roslyn/issues/7932")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ExtensionMethodWithinSameClassOfferedForCompletion()
        {
            var markup = @"
public static class Test
{
    static void TestB()
    {
        $$
    }
    static void TestA(this string s) { }
}
";
            await VerifyItemExistsAsync(markup, "TestA");
        }

        [WorkItem(7932, "https://github.com/dotnet/roslyn/issues/7932")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ExtensionMethodWithinParentClassOfferedForCompletion()
        {
            var markup = @"
public static class Parent
{
    static void TestA(this string s) { }
    static void TestC(string s) { }
    public static class Test
    {
        static void TestB()
        {
            $$
        }
    }
}
";
            await VerifyItemExistsAsync(markup, "TestA");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ExceptionFilter1()
        {
            var markup = @"
using System;

class C
{
    void M(bool x)
    {
        try
        {
        }
        catch when ($$
";

            await VerifyItemExistsAsync(markup, "x");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ExceptionFilter1_NotBeforeOpenParen()
        {
            var markup = @"
using System;

class C
{
    void M(bool x)
    {
        try
        {
        }
        catch when $$
";

            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ExceptionFilter2()
        {
            var markup = @"
using System;

class C
{
    void M(bool x)
    {
        try
        {
        }
        catch (Exception ex) when ($$
";

            await VerifyItemExistsAsync(markup, "x");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ExceptionFilter2_NotBeforeOpenParen()
        {
            var markup = @"
using System;

class C
{
    void M(bool x)
    {
        try
        {
        }
        catch (Exception ex) when $$
";

            await VerifyNoItemsExistAsync(markup);
        }

        [WorkItem(25084, "https://github.com/dotnet/roslyn/issues/25084")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SwitchCaseWhenClause1()
        {
            var markup = @"
class C
{
    void M(bool x)
    {
        switch (1)
        {
            case 1 when $$
";

            await VerifyItemExistsAsync(markup, "x");
        }

        [WorkItem(25084, "https://github.com/dotnet/roslyn/issues/25084")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task SwitchCaseWhenClause2()
        {
            var markup = @"
class C
{
    void M(bool x)
    {
        switch (1)
        {
            case int i when $$
";

            await VerifyItemExistsAsync(markup, "x");
        }

        [WorkItem(717, "https://github.com/dotnet/roslyn/issues/717")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ExpressionContextCompletionWithinCast()
        {
            var markup = @"
class Program
{
    void M()
    {
        for (int i = 0; i < 5; i++)
        {
            var x = ($$)
            var y = 1;
        }
    }
}
";
            await VerifyItemExistsAsync(markup, "i");
        }

        [WorkItem(1277, "https://github.com/dotnet/roslyn/issues/1277")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoInstanceMembersInPropertyInitializer()
        {
            var markup = @"
class A {
    int abc;
    int B { get; } = $$
}
";
            await VerifyItemIsAbsentAsync(markup, "abc");
        }

        [WorkItem(1277, "https://github.com/dotnet/roslyn/issues/1277")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task StaticMembersInPropertyInitializer()
        {
            var markup = @"
class A {
    static Action s_abc;
    event Action B = $$
}
";
            await VerifyItemExistsAsync(markup, "s_abc");
        }


        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoInstanceMembersInFieldLikeEventInitializer()
        {
            var markup = @"
class A {
    Action abc;
    event Action B = $$
}
";
            await VerifyItemIsAbsentAsync(markup, "abc");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task StaticMembersInFieldLikeEventInitializer()
        {
            var markup = @"
class A {
    static Action s_abc;
    event Action B = $$
}
";
            await VerifyItemExistsAsync(markup, "s_abc");
        }

        [WorkItem(5069, "https://github.com/dotnet/roslyn/issues/5069")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InstanceMembersInTopLevelFieldInitializer()
        {
            var markup = @"
int aaa = 1;
int bbb = $$
";
            await VerifyItemExistsAsync(markup, "aaa", sourceCodeKind: SourceCodeKind.Script);
        }

        [WorkItem(5069, "https://github.com/dotnet/roslyn/issues/5069")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task InstanceMembersInTopLevelFieldLikeEventInitializer()
        {
            var markup = @"
Action aaa = null;
event Action bbb = $$
";
            await VerifyItemExistsAsync(markup, "aaa", sourceCodeKind: SourceCodeKind.Script);
        }

        [WorkItem(33, "https://github.com/dotnet/roslyn/issues/33")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoConditionalAccessCompletionOnTypes1()
        {
            var markup = @"
using A = System
class C
{
    A?.$$
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [WorkItem(33, "https://github.com/dotnet/roslyn/issues/33")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoConditionalAccessCompletionOnTypes2()
        {
            var markup = @"
class C
{
    System?.$$
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [WorkItem(33, "https://github.com/dotnet/roslyn/issues/33")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoConditionalAccessCompletionOnTypes3()
        {
            var markup = @"
class C
{
    System.Console?.$$
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CompletionInIncompletePropertyDeclaration()
        {
            var markup = @"
class Class1
{
    public string Property1 { get; set; }
}

class Class2
{
    public string Property { get { return this.Source.$$
    public Class1 Source { get; set; }
}";
            await VerifyItemExistsAsync(markup, "Property1");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task NoCompletionInShebangComments()
        {
            await VerifyNoItemsExistAsync("#!$$", sourceCodeKind: SourceCodeKind.Script);
            await VerifyNoItemsExistAsync("#! S$$", sourceCodeKind: SourceCodeKind.Script, usePreviousCharAsTrigger: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CompoundNameTargetTypePreselection()
        {
            var markup = @"
class Class1
{
    void goo()
    {
        int x = 3;
        string y = x.$$
    }
}";
            await VerifyItemExistsAsync(markup, "ToString", matchPriority: SymbolMatchPriority.PreferEventOrMethod);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TargetTypeInCollectionInitializer1()
        {
            var markup = @"
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        int z;
        string q;
        List<int> x = new List<int>() { $$  }
    }
}";
            await VerifyItemExistsAsync(markup, "z", matchPriority: SymbolMatchPriority.PreferLocalOrParameterOrRangeVariable);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TargetTypeInCollectionInitializer2()
        {
            var markup = @"
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        int z;
        string q;
        List<int> x = new List<int>() { 1, $$  }
    }
}";
            await VerifyItemExistsAsync(markup, "z", matchPriority: SymbolMatchPriority.PreferLocalOrParameterOrRangeVariable);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TargeTypeInObjectInitializer1()
        {
            var markup = @"
class C
{
    public int X { get; set; }
    public int Y { get; set; }

    void goo()
    {
        int i;
        var c = new C() { X = $$ }
    }
}";
            await VerifyItemExistsAsync(markup, "i", matchPriority: SymbolMatchPriority.PreferLocalOrParameterOrRangeVariable);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TargeTypeInObjectInitializer2()
        {
            var markup = @"
class C
{
    public int X { get; set; }
    public int Y { get; set; }

    void goo()
    {
        int i;
        var c = new C() { X = 1, Y = $$ }
    }
}";
            await VerifyItemExistsAsync(markup, "i", matchPriority: SymbolMatchPriority.PreferLocalOrParameterOrRangeVariable);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TupleElements()
        {
            var markup = @"
class C
{
    void goo()
    {
        var t = (Alice: 1, Item2: 2, ITEM3: 3, 4, 5, 6, 7, 8, Bob: 9);
        t.$$
    }
}" + TestResources.NetFX.ValueTuple.tuplelib_cs;

            await VerifyItemExistsAsync(markup, "Alice");
            await VerifyItemExistsAsync(markup, "Bob");
            await VerifyItemExistsAsync(markup, "CompareTo");
            await VerifyItemExistsAsync(markup, "Equals");
            await VerifyItemExistsAsync(markup, "GetHashCode");
            await VerifyItemExistsAsync(markup, "GetType");
            await VerifyItemExistsAsync(markup, "Item2");
            await VerifyItemExistsAsync(markup, "ITEM3");
            for (var i = 4; i <= 8; i++)
            {
                await VerifyItemExistsAsync(markup, "Item" + i);
            }
            await VerifyItemExistsAsync(markup, "ToString");

            await VerifyItemIsAbsentAsync(markup, "Item1");
            await VerifyItemIsAbsentAsync(markup, "Item9");
            await VerifyItemIsAbsentAsync(markup, "Rest");
            await VerifyItemIsAbsentAsync(markup, "Item3");
        }

        [WorkItem(14546, "https://github.com/dotnet/roslyn/issues/14546")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TupleElementsCompletionOffMethodGroup()
        {
            var markup = @"
class C
{
    void goo()
    {
        new object().ToString.$$
    }
}" + TestResources.NetFX.ValueTuple.tuplelib_cs;

            // should not crash
            await VerifyItemExistsAsync(markup, "ToString");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.LocalFunctions)]
        [WorkItem(13480, "https://github.com/dotnet/roslyn/issues/13480")]
        public async Task NoCompletionInLocalFuncGenericParamList()
        {
            var markup = @"
class C
{
    void M()
    {
        int Local<$$";

            await VerifyNoItemsExistAsync(markup);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.LocalFunctions)]
        [WorkItem(13480, "https://github.com/dotnet/roslyn/issues/13480")]
        public async Task CompletionForAwaitWithoutAsync()
        {
            var markup = @"
class C
{
    void M()
    {
        await Local<$$";

            await VerifyAnyItemExistsAsync(markup);
        }

        [WorkItem(14127, "https://github.com/dotnet/roslyn/issues/14127")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TupleTypeAtMemberLevel1()
        {
            await VerifyItemExistsAsync(@"
class C
{
    ($$
}", "C");
        }

        [WorkItem(14127, "https://github.com/dotnet/roslyn/issues/14127")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TupleTypeAtMemberLevel2()
        {
            await VerifyItemExistsAsync(@"
class C
{
    ($$)
}", "C");
        }

        [WorkItem(14127, "https://github.com/dotnet/roslyn/issues/14127")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TupleTypeAtMemberLevel3()
        {
            await VerifyItemExistsAsync(@"
class C
{
    (C, $$
}", "C");
        }

        [WorkItem(14127, "https://github.com/dotnet/roslyn/issues/14127")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TupleTypeAtMemberLevel4()
        {
            await VerifyItemExistsAsync(@"
class C
{
    (C, $$)
}", "C");
        }


        [WorkItem(14127, "https://github.com/dotnet/roslyn/issues/14127")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TupleTypeInForeach()
        {
            await VerifyItemExistsAsync(@"
class C
{
    void M()
    {
        foreach ((C, $$
    }
}", "C");
        }


        [WorkItem(14127, "https://github.com/dotnet/roslyn/issues/14127")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TupleTypeInParameterList()
        {
            await VerifyItemExistsAsync(@"
class C
{
    void M((C, $$)
    {
    }
}", "C");
        }

        [WorkItem(14127, "https://github.com/dotnet/roslyn/issues/14127")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TupleTypeInNameOf()
        {
            await VerifyItemExistsAsync(@"
class C
{
    void M()
    {
        var x = nameof((C, $$
    }
}", "C");
        }

        [WorkItem(14163, "https://github.com/dotnet/roslyn/issues/14163")]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.LocalFunctions)]
        public async Task LocalFunctionDescription()
        {
            await VerifyItemExistsAsync(@"
class C
{
    void M()
    {
        void Local() { }
        
        $$
    }
}", "Local", "void Local()");
        }

        [WorkItem(14163, "https://github.com/dotnet/roslyn/issues/14163")]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.LocalFunctions)]
        public async Task LocalFunctionDescription2()
        {
            await VerifyItemExistsAsync(@"
using System;
class C
{
    class var { }
    void M()
    {
        Action<int> Local(string x, ref var @class, params Func<int, string> f)
        {
            return () => 0;
        }

        $$
    }
}", "Local", "Action<int> Local(string x, ref var @class, params Func<int, string> f)");
        }

        [WorkItem(18359, "https://github.com/dotnet/roslyn/issues/18359")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task EnumMemberAfterDot()
        {
            var markup =
@"namespace ConsoleApplication253
{
    class Program
    {
        static void Main(string[] args)
        {
            M(E.$$)
        }

        static void M(E e) { }
    }

    enum E
    {
        A,
        B,
    }
}
";
            // VerifyItemExistsAsync also tests with the item typed.
            await VerifyItemExistsAsync(markup, "A");
            await VerifyItemExistsAsync(markup, "B");
        }

        [WorkItem(8321, "https://github.com/dotnet/roslyn/issues/8321")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotOnMethodGroup1()
        {
            var markup =
@"namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Main.$$
        }
    }
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [WorkItem(8321, "https://github.com/dotnet/roslyn/issues/8321")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotOnMethodGroup2()
        {
            var markup =
@"class C {
    void M<T>() {M<C>.$$ }
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [WorkItem(8321, "https://github.com/dotnet/roslyn/issues/8321")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task NotOnMethodGroup3()
        {
            var markup =
@"class C {
    void M() {M.$$}
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [WorkItem(420697, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=420697&_a=edit")]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/21766"), Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task DoNotCrashInExtensionMethoWithExpressionBodiedMember()
        {
            var markup =
@"public static class Extensions { public static T Get<T>(this object o) => $$}
";
            await VerifyItemExistsAsync(markup, "o");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task EnumConstraint()
        {
            var markup =
@"public class X<T> where T : System.$$
";
            await VerifyItemExistsAsync(markup, "Enum");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task DelegateConstraint()
        {
            var markup =
@"public class X<T> where T : System.$$
";
            await VerifyItemExistsAsync(markup, "Delegate");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task MulticastDelegateConstraint()
        {
            var markup =
@"public class X<T> where T : System.$$
";
            await VerifyItemExistsAsync(markup, "MulticastDelegate");
        }

        private static string CreateThenIncludeTestCode(string lambdaExpressionString, string methodDeclarationString)
        {
            var template = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ThenIncludeIntellisenseBug
{
    class Program
    {
        static void Main(string[] args)
        {
            var registrations = new List<Registration>().AsQueryable();
            var reg = registrations.Include(r => r.Activities).ThenInclude([1]);
        }
    }

    internal class Registration
    {
        public ICollection<Activity> Activities { get; set; }
    }

    public class Activity
    {
        public Task Task { get; set; }
    }

    public class Task
    {
        public string Name { get; set; }
    }

    public interface IIncludableQueryable<out TEntity, out TProperty> : IQueryable<TEntity>
    {
    }

    public static class EntityFrameworkQuerybleExtensions
    {
        public static IIncludableQueryable<TEntity, TProperty> Include<TEntity, TProperty>(
            this IQueryable<TEntity> source,
            Expression<Func<TEntity, TProperty>> navigationPropertyPath)
            where TEntity : class
        {
            return default(IIncludableQueryable<TEntity, TProperty>);
        }

        [2]
    }
}";

            return template.Replace("[1]", lambdaExpressionString).Replace("[2]", methodDeclarationString);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ThenInclude()
        {
            var markup = CreateThenIncludeTestCode("b => b.$$",
@"
    public static IIncludableQueryable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
        this IIncludableQueryable<TEntity, ICollection<TPreviousProperty>> source,
        Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath) where TEntity : class
    {
        return default(IIncludableQueryable<TEntity, TProperty>);
    }

    public static IIncludableQueryable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
        this IIncludableQueryable<TEntity, TPreviousProperty> source,
        Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath) where TEntity : class
    {
        return default(IIncludableQueryable<TEntity, TProperty>);
    }");

            await VerifyItemExistsAsync(markup, "Task");
            await VerifyItemExistsAsync(markup, "FirstOrDefault", displayTextSuffix: "<>");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ThenIncludeNoExpression()
        {
            var markup = CreateThenIncludeTestCode("b => b.$$",
@"
    public static IIncludableQueryable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
        this IIncludableQueryable<TEntity, ICollection<TPreviousProperty>> source,
        Func<TPreviousProperty, TProperty> navigationPropertyPath) where TEntity : class
    {
        return default(IIncludableQueryable<TEntity, TProperty>);
    }

    public static IIncludableQueryable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
        this IIncludableQueryable<TEntity, TPreviousProperty> source,
        Func<TPreviousProperty, TProperty> navigationPropertyPath) where TEntity : class
    {
        return default(IIncludableQueryable<TEntity, TProperty>);
    }");

            await VerifyItemExistsAsync(markup, "Task");
            await VerifyItemExistsAsync(markup, "FirstOrDefault", displayTextSuffix: "<>");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ThenIncludeSecondArgument()
        {
            var markup = CreateThenIncludeTestCode("0, b => b.$$",
@"
    public static IIncludableQueryable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
        this IIncludableQueryable<TEntity, ICollection<TPreviousProperty>> source,
        int a,
        Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath) where TEntity : class
    {
        return default(IIncludableQueryable<TEntity, TProperty>);
    }

    public static IIncludableQueryable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
        this IIncludableQueryable<TEntity, TPreviousProperty> source,
        int a,
        Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath) where TEntity : class
    {
        return default(IIncludableQueryable<TEntity, TProperty>);
    }");

            await VerifyItemExistsAsync(markup, "Task");
            await VerifyItemExistsAsync(markup, "FirstOrDefault", displayTextSuffix: "<>");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ThenIncludeSecondArgumentAndMultiArgumentLambda()
        {
            var markup = CreateThenIncludeTestCode("0, (a,b,c) => c.$$)",
@"
    public static IIncludableQueryable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
        this IIncludableQueryable<TEntity, ICollection<TPreviousProperty>> source,
        int a,
        Expression<Func<string, string, TPreviousProperty, TProperty>> navigationPropertyPath) where TEntity : class
    {
        return default(IIncludableQueryable<TEntity, TProperty>);
    }

    public static IIncludableQueryable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
        this IIncludableQueryable<TEntity, TPreviousProperty> source,
        int a,
        Expression<Func<string, string, TPreviousProperty, TProperty>> navigationPropertyPath) where TEntity : class
    {
        return default(IIncludableQueryable<TEntity, TProperty>);
    }");

            await VerifyItemExistsAsync(markup, "Task");
            await VerifyItemExistsAsync(markup, "FirstOrDefault", displayTextSuffix: "<>");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ThenIncludeSecondArgumentNoOverlap()
        {
            var markup = CreateThenIncludeTestCode("b => b.Task, b =>b.$$",
@"
    public static IIncludableQueryable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
        this IIncludableQueryable<TEntity, ICollection<TPreviousProperty>> source,
        Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath,
        Expression<Func<TPreviousProperty, TProperty>> anotherNavigationPropertyPath) where TEntity : class
        {
            return default(IIncludableQueryable<TEntity, TProperty>);
        }

        public static IIncludableQueryable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
           this IIncludableQueryable<TEntity, TPreviousProperty> source,
           Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath) where TEntity : class
        {
            return default(IIncludableQueryable<TEntity, TProperty>);
        }
");

            await VerifyItemExistsAsync(markup, "Task");
            await VerifyItemIsAbsentAsync(markup, "FirstOrDefault", displayTextSuffix: "<>");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ThenIncludeSecondArgumentAndMultiArgumentLambdaWithNoLambdaOverlap()
        {
            var markup = CreateThenIncludeTestCode("0, (a,b,c) => c.$$",
@"
    public static IIncludableQueryable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
        this IIncludableQueryable<TEntity, ICollection<TPreviousProperty>> source,
        int a,
        Expression<Func<string, TPreviousProperty, TProperty>> navigationPropertyPath) where TEntity : class
    {
        return default(IIncludableQueryable<TEntity, TProperty>);
    }

    public static IIncludableQueryable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
        this IIncludableQueryable<TEntity, TPreviousProperty> source,
        int a,
        Expression<Func<string, string, TPreviousProperty, TProperty>> navigationPropertyPath) where TEntity : class
    {
        return default(IIncludableQueryable<TEntity, TProperty>);
    }
");

            await VerifyItemIsAbsentAsync(markup, "Task");
            await VerifyItemExistsAsync(markup, "FirstOrDefault", displayTextSuffix: "<>");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/35100"), Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ThenIncludeGenericAndNoGenericOverloads()
        {
            var markup = CreateThenIncludeTestCode("c => c.$$",
@"
        public static IIncludableQueryable<Registration, Task> ThenInclude(
                   this IIncludableQueryable<Registration, ICollection<Activity>> source,
                   Func<Activity, Task> navigationPropertyPath)
        {
            return default(IIncludableQueryable<Registration, Task>);
        }

        public static IIncludableQueryable<TEntity, TProperty> ThenInclude<TEntity, TPreviousProperty, TProperty>(
            this IIncludableQueryable<TEntity, TPreviousProperty> source,
            Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath) where TEntity : class
        {
            return default(IIncludableQueryable<TEntity, TProperty>);
        }
");

            await VerifyItemExistsAsync(markup, "Task");
            await VerifyItemExistsAsync(markup, "FirstOrDefault", displayTextSuffix: "<>");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ThenIncludeNoGenericOverloads()
        {
            var markup = CreateThenIncludeTestCode("c => c.$$",
@"
        public static IIncludableQueryable<Registration, Task> ThenInclude(
            this IIncludableQueryable<Registration, ICollection<Activity>> source,
            Func<Activity, Task> navigationPropertyPath)
        {
            return default(IIncludableQueryable<Registration, Task>);
        }

        public static IIncludableQueryable<Registration, Activity> ThenInclude(
            this IIncludableQueryable<Registration, ICollection<Activity>> source,
            Func<ICollection<Activity>, Activity> navigationPropertyPath) 
        {
            return default(IIncludableQueryable<Registration, Activity>);
        }
");

            await VerifyItemExistsAsync(markup, "Task");
            await VerifyItemExistsAsync(markup, "FirstOrDefault", displayTextSuffix: "<>");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CompletionInsideMethodsWithNonFunctionsAsArguments()
        {
            var markup = @"
using System;
class c
{
    void M()
    {
        Goo(builder =>
        {
            builder.$$
        });
    }

    void Goo(Action<Builder> configure)
    {
        var builder = new Builder();
        configure(builder);
    }
}
class Builder
{
    public int Something { get; set; }
}";

            await VerifyItemExistsAsync(markup, "Something");
            await VerifyItemIsAbsentAsync(markup, "BeginInvoke");
            await VerifyItemIsAbsentAsync(markup, "Clone");
            await VerifyItemIsAbsentAsync(markup, "Method");
            await VerifyItemIsAbsentAsync(markup, "Target");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CompletionInsideMethodsWithDelegatesAsArguments()
        {
            var markup = @"
using System;

class Program
{
    public delegate void Delegate1(Uri u);
    public delegate void Delegate2(Guid g);

    public void M(Delegate1 d) { }
    public void M(Delegate2 d) { }

    public void Test()
    {
        M(d => d.$$)
    }
}";

            // Guid
            await VerifyItemExistsAsync(markup, "ToByteArray");

            // Uri
            await VerifyItemExistsAsync(markup, "AbsoluteUri");
            await VerifyItemExistsAsync(markup, "Fragment");
            await VerifyItemExistsAsync(markup, "Query");

            // Should not appear for Delegate
            await VerifyItemIsAbsentAsync(markup, "BeginInvoke");
            await VerifyItemIsAbsentAsync(markup, "Clone");
            await VerifyItemIsAbsentAsync(markup, "Method");
            await VerifyItemIsAbsentAsync(markup, "Target");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CompletionInsideMethodsWithDelegatesAndReversingArguments()
        {
            var markup = @"
using System;

class Program
{
    public delegate void Delegate1<T1,T2>(T2 t2, T1 t1);
    public delegate void Delegate2<T1,T2>(T2 t2, int g, T1 t1);

    public void M(Delegate1<Uri,Guid> d) { }
    public void M(Delegate2<Uri,Guid> d) { }

    public void Test()
    {
        M(d => d.$$)
    }
}";

            // Guid
            await VerifyItemExistsAsync(markup, "ToByteArray");

            // Should not appear for  Uri
            await VerifyItemIsAbsentAsync(markup, "AbsoluteUri");
            await VerifyItemIsAbsentAsync(markup, "Fragment");
            await VerifyItemIsAbsentAsync(markup, "Query");

            // Should not appear for Delegate
            await VerifyItemIsAbsentAsync(markup, "BeginInvoke");
            await VerifyItemIsAbsentAsync(markup, "Clone");
            await VerifyItemIsAbsentAsync(markup, "Method");
            await VerifyItemIsAbsentAsync(markup, "Target");
        }

        [WorkItem(36029, "https://github.com/dotnet/roslyn/issues/36029")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CompletionInsideMethodWithParamsBeforeParams()
        {
            var markup = @"
using System;
class C
{
    void M()
    {
        Goo(builder =>
        {
            builder.$$
        });
    }

    void Goo(Action<Builder> action, params Action<AnotherBuilder>[] otherActions)
    {
    }
}
class Builder
{
    public int Something { get; set; }
};

class AnotherBuilder
{
    public int AnotherSomething { get; set; }
}";

            await VerifyItemIsAbsentAsync(markup, "AnotherSomething");
            await VerifyItemIsAbsentAsync(markup, "FirstOrDefault");
            await VerifyItemExistsAsync(markup, "Something");
        }

        [WorkItem(36029, "https://github.com/dotnet/roslyn/issues/36029")]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CompletionInsideMethodWithParamsInParams()
        {
            var markup = @"
using System;
class C
{
    void M()
    {
        Goo(b0 => { }, b1 => {}, b2 => { b2.$$ });
    }

    void Goo(Action<Builder> action, params Action<AnotherBuilder>[] otherActions)
    {
    }
}
class Builder
{
    public int Something { get; set; }
};

class AnotherBuilder
{
    public int AnotherSomething { get; set; }
}";

            await VerifyItemIsAbsentAsync(markup, "Something");
            await VerifyItemIsAbsentAsync(markup, "FirstOrDefault");
            await VerifyItemExistsAsync(markup, "AnotherSomething");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TargetTypedCompletion)]
        public async Task TestTargetTypeFilterWithExperimentEnabled()
        {
            SetExperimentOption(WellKnownExperimentNames.TargetTypedCompletionFilter, true);

            var markup =
@"public class C
{
    int intField;
    void M(int x)
    {
        M($$);
    }
}";
            await VerifyItemExistsAsync(
                markup, "intField",
                matchingFilters: new List<CompletionItemFilter> { CompletionItemFilter.FieldFilter, CompletionItemFilter.TargetTypedFilter });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TargetTypedCompletion)]
        public async Task TestNoTargetTypeFilterWithExperimentDisabled()
        {
            SetExperimentOption(WellKnownExperimentNames.TargetTypedCompletionFilter, false);

            var markup =
@"public class C
{
    int intField;
    void M(int x)
    {
        M($$);
    }
}";
            await VerifyItemExistsAsync(
                markup, "intField",
                matchingFilters: new List<CompletionItemFilter> { CompletionItemFilter.FieldFilter });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TargetTypedCompletion)]
        public async Task TestTargetTypeFilter_NotOnObjectMembers()
        {
            SetExperimentOption(WellKnownExperimentNames.TargetTypedCompletionFilter, true);

            var markup =
@"public class C
{
    void M(int x)
    {
        M($$);
    }
}";
            await VerifyItemExistsAsync(
                markup, "GetHashCode",
                matchingFilters: new List<CompletionItemFilter> { CompletionItemFilter.MethodFilter });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TargetTypedCompletion)]
        public async Task TestTargetTypeFilter_NotNamedTypes()
        {
            SetExperimentOption(WellKnownExperimentNames.TargetTypedCompletionFilter, true);

            var markup =
@"public class C
{
    void M(C c)
    {
        M($$);
    }
}";
            await VerifyItemExistsAsync(
                markup, "c",
                matchingFilters: new List<CompletionItemFilter> { CompletionItemFilter.LocalAndParameterFilter, CompletionItemFilter.TargetTypedFilter });

            await VerifyItemExistsAsync(
                markup, "C",
                matchingFilters: new List<CompletionItemFilter> { CompletionItemFilter.ClassFilter });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CompletionShouldNotProvideExtensionMethodsIfTypeConstraintDoesNotMatch()
        {
            var markup = @"
public static class Ext
{
    public static void DoSomething<T>(this T thing, string s) where T : class, I
    { 
    }
}

public interface I 
{
}

public class C
{
    public void M(string s)
    {
        this.$$
    }
}";

            await VerifyItemExistsAsync(markup, "M");
            await VerifyItemExistsAsync(markup, "Equals");
            await VerifyItemIsAbsentAsync(markup, "DoSomething", displayTextSuffix: "<>");
        }

        [WorkItem(38074, "https://github.com/dotnet/roslyn/issues/38074")]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.Completion)]
        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.LocalFunctions)]
        public async Task LocalFunctionInStaticMethod()
        {
            await VerifyItemExistsAsync(@"
class C
{
    static void M()
    {
        void Local() { }

        $$
    }
}", "Local");
        }
    }
}
