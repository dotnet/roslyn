// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionSetSources
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.Completion)]
    public partial class SymbolCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        internal override Type GetCompletionProviderType()
            => typeof(SymbolCompletionProvider);

        [Theory]
        [InlineData(SourceCodeKind.Regular)]
        [InlineData(SourceCodeKind.Script)]
        public async Task EmptyFile(SourceCodeKind sourceCodeKind)
        {
            await VerifyItemIsAbsentAsync(@"$$", @"String", expectedDescriptionOrNull: null, sourceCodeKind: sourceCodeKind);
            await VerifyItemExistsAsync(@"$$", @"System", expectedDescriptionOrNull: null, sourceCodeKind: sourceCodeKind);
        }

        [Theory]
        [InlineData(SourceCodeKind.Regular)]
        [InlineData(SourceCodeKind.Script)]
        public async Task EmptyFileWithUsing(SourceCodeKind sourceCodeKind)
        {
            await VerifyItemExistsAsync(@"using System;
$$", @"String", expectedDescriptionOrNull: null, sourceCodeKind: sourceCodeKind);
            await VerifyItemExistsAsync(@"using System;
$$", @"System", expectedDescriptionOrNull: null, sourceCodeKind: sourceCodeKind);
        }

        [Fact]
        public async Task NotAfterHashR()
            => await VerifyItemIsAbsentAsync(@"#r $$", "@System", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);

        [Fact]
        public async Task NotAfterHashLoad()
            => await VerifyItemIsAbsentAsync(@"#load $$", "@System", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);

        [Fact]
        public async Task UsingDirective()
        {
            await VerifyItemIsAbsentAsync(@"using $$", @"String");
            await VerifyItemIsAbsentAsync(@"using $$ = System", @"System");
            await VerifyItemExistsAsync(@"using $$", @"System");
            await VerifyItemExistsAsync(@"using T = $$", @"System");
        }

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
        public async Task SingleLineComment1()
        {
            await VerifyItemIsAbsentAsync(@"using System;

class C {
// $$", @"String");
            await VerifyItemIsAbsentAsync(@"using System;

class C {
// $$", @"System");
        }

        [Fact]
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

        [Fact]
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

        [Fact]
        public async Task SingleLineXmlComment1()
        {
            await VerifyItemIsAbsentAsync(@"using System;

class C {
/// $$", @"String");
            await VerifyItemIsAbsentAsync(@"using System;

class C {
/// $$", @"System");
        }

        [Fact]
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

        [Fact]
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

        [Fact]
        public async Task OpenStringLiteral()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", AddInsideMethod("string s = \"$$")), @"String");
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", AddInsideMethod("string s = \"$$")), @"System");
        }

        [Fact]
        public async Task OpenStringLiteralInDirective()
        {
            await VerifyItemIsAbsentAsync("#r \"$$", "String", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
            await VerifyItemIsAbsentAsync("#r \"$$", "System", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact]
        public async Task StringLiteral()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", AddInsideMethod("string s = \"$$\";")), @"System");
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", AddInsideMethod("string s = \"$$\";")), @"String");
        }

        [Fact]
        public async Task StringLiteralInDirective()
        {
            await VerifyItemIsAbsentAsync("#r \"$$\"", "String", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
            await VerifyItemIsAbsentAsync("#r \"$$\"", "System", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact]
        public async Task OpenCharLiteral()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", AddInsideMethod("char c = '$$")), @"System");
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", AddInsideMethod("char c = '$$")), @"String");
        }

        [Fact]
        public async Task AssemblyAttribute1()
        {
            await VerifyItemExistsAsync(@"[assembly: $$]", @"System");
            await VerifyItemIsAbsentAsync(@"[assembly: $$]", @"String");
        }

        [Fact]
        public async Task AssemblyAttribute2()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"[assembly: $$]"), @"System");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"[assembly: $$]"), @"AttributeUsage");
        }

        [Fact]
        public async Task SystemAttributeIsNotAnAttribute()
        {
            var content = @"[$$]
class CL {}";

            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", content), @"Attribute");
        }

        [Fact]
        public async Task TypeAttribute()
        {
            var content = @"[$$]
class CL {}";

            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"AttributeUsage");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact]
        public async Task TypeParamAttribute()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"class CL<[A$$]T> {}"), @"AttributeUsage");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"class CL<[A$$]T> {}"), @"System");
        }

        [Fact]
        public async Task MethodAttribute()
        {
            var content = @"class CL {
    [$$]
    void Method() {}
}";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"AttributeUsage");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact]
        public async Task MethodTypeParamAttribute()
        {
            var content = @"class CL{
    void Method<[A$$]T> () {}
}";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"AttributeUsage");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact]
        public async Task MethodParamAttribute()
        {
            var content = @"class CL{
    void Method ([$$]int i) {}
}";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"AttributeUsage");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
        public async Task NamespaceName_EmptyNameSpan_TopLevel()
        {
            var source = @"namespace $$ { }";

            await VerifyItemExistsAsync(source, "System", sourceCodeKind: SourceCodeKind.Regular);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
        public async Task NamespaceName_EmptyNameSpan_Nested()
        {
            var source = @";
namespace System
{
    namespace $$ { }
}";

            await VerifyItemExistsAsync(source, "Runtime", sourceCodeKind: SourceCodeKind.Regular);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
        public async Task NamespaceName_Unqualified_TopLevelNoPeers()
        {
            var source = @"using System;

namespace $$";

            await VerifyItemExistsAsync(source, "System", sourceCodeKind: SourceCodeKind.Regular);
            await VerifyItemIsAbsentAsync(source, "String", sourceCodeKind: SourceCodeKind.Regular);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
        public async Task NamespaceName_Unqualified_TopLevelNoPeers_FileScopedNamespace()
        {
            var source = @"using System;

namespace $$;";

            await VerifyItemExistsAsync(source, "System", sourceCodeKind: SourceCodeKind.Regular);
            await VerifyItemIsAbsentAsync(source, "String", sourceCodeKind: SourceCodeKind.Regular);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
        public async Task NamespaceName_Unqualified_TopLevelWithPeer()
        {
            var source = @"
namespace A { }

namespace $$";

            await VerifyItemExistsAsync(source, "A", sourceCodeKind: SourceCodeKind.Regular);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
        public async Task NamespaceName_Unqualified_NestedWithNoPeers()
        {
            var source = @"
namespace A
{
    namespace $$
}";

            await VerifyNoItemsExistAsync(source, sourceCodeKind: SourceCodeKind.Regular);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
        public async Task NamespaceName_Unqualified_ExcludesCurrentDeclaration()
        {
            var source = @"namespace N$$S";

            await VerifyItemIsAbsentAsync(source, "NS", sourceCodeKind: SourceCodeKind.Regular);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
        public async Task NamespaceName_Unqualified_InnerCompletionPosition()
        {
            var source = @"namespace Sys$$tem { }";

            await VerifyItemExistsAsync(source, "System", sourceCodeKind: SourceCodeKind.Regular);
            await VerifyItemIsAbsentAsync(source, "Runtime", sourceCodeKind: SourceCodeKind.Regular);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
        public async Task NamespaceName_Qualified_NoPeers()
        {
            var source = @"namespace A.$$";

            await VerifyNoItemsExistAsync(source, sourceCodeKind: SourceCodeKind.Regular);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
        public async Task NamespaceName_Qualified_TopLevelWithPeer()
        {
            var source = @"
namespace A.B { }

namespace A.$$";

            await VerifyItemExistsAsync(source, "B", sourceCodeKind: SourceCodeKind.Regular);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
        public async Task NamespaceName_Qualified_TopLevelWithPeer_FileScopedNamespace()
        {
            var source = @"
namespace A.B { }

namespace A.$$;";

            await VerifyItemExistsAsync(source, "B", sourceCodeKind: SourceCodeKind.Regular);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
        public async Task NamespaceName_Qualified_InnerCompletionPosition()
        {
            var source = @"namespace Sys$$tem.Runtime { }";

            await VerifyItemExistsAsync(source, "System", sourceCodeKind: SourceCodeKind.Regular);
            await VerifyItemIsAbsentAsync(source, "Runtime", sourceCodeKind: SourceCodeKind.Regular);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
        public async Task NamespaceName_OnKeyword()
        {
            var source = @"name$$space System { }";

            await VerifyItemExistsAsync(source, "System", sourceCodeKind: SourceCodeKind.Regular);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7213")]
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

        [Fact]
        public async Task UnderNamespace()
        {
            await VerifyItemIsAbsentAsync(@"namespace NS { $$", @"String");
            await VerifyItemIsAbsentAsync(@"namespace NS { $$", @"System");
        }

        [Fact]
        public async Task OutsideOfType1()
        {
            await VerifyItemIsAbsentAsync(@"namespace NS {
class CL {}
$$", @"String");
            await VerifyItemIsAbsentAsync(@"namespace NS {
class CL {}
$$", @"System");
        }

        [Fact]
        public async Task OutsideOfType2()
        {
            var content = @"namespace NS {
class CL {}
$$";
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", content), @"String");
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact]
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

        [Fact]
        public async Task AfterDot()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"[assembly: A.$$"), @"String");
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"[assembly: A.$$"), @"System");
        }

        [Fact]
        public async Task UsingAlias()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"using MyType = $$"), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"using MyType = $$"), @"System");
        }

        [Fact]
        public async Task IncompleteMember()
        {
            var content = @"class CL {
    $$
";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact]
        public async Task IncompleteMemberAccessibility()
        {
            var content = @"class CL {
    public $$
";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact]
        public async Task BadStatement()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = $$)c")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = $$)c")), @"System");
        }

        [Fact]
        public async Task TypeTypeParameter()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class CL<$$"), @"String");
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class CL<$$"), @"System");
        }

        [Fact]
        public async Task TypeTypeParameterList()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class CL<T, $$"), @"String");
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class CL<T, $$"), @"System");
        }

        [Fact]
        public async Task CastExpressionTypePart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = ($$)c")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = ($$)c")), @"System");
        }

        [Fact]
        public async Task ObjectCreationExpression()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = new $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = new $$")), @"System");
        }

        [Fact]
        public async Task ArrayCreationExpression()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = new $$ [")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = new $$ [")), @"System");
        }

        [Fact]
        public async Task StackAllocArrayCreationExpression()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = stackalloc $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = stackalloc $$")), @"System");
        }

        [Fact]
        public async Task FromClauseTypeOptPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from $$ c")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from $$ c")), @"System");
        }

        [Fact]
        public async Task JoinClause()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C join $$ j")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C join $$ j")), @"System");
        }

        [Fact]
        public async Task DeclarationStatement()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"$$ i =")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"$$ i =")), @"System");
        }

        [Fact]
        public async Task VariableDeclaration()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"fixed($$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"fixed($$")), @"System");
        }

        [Fact]
        public async Task ForEachStatement()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"foreach($$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"foreach($$")), @"System");
        }

        [Fact]
        public async Task ForEachStatementNoToken()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", AddInsideMethod(@"foreach $$")), @"String");
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", AddInsideMethod(@"foreach $$")), @"System");
        }

        [Fact]
        public async Task CatchDeclaration()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"try {} catch($$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"try {} catch($$")), @"System");
        }

        [Fact]
        public async Task FieldDeclaration()
        {
            var content = @"class CL {
    $$ i";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact]
        public async Task EventFieldDeclaration()
        {
            var content = @"class CL {
    event $$";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact]
        public async Task ConversionOperatorDeclaration()
        {
            var content = @"class CL {
    explicit operator $$";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact]
        public async Task ConversionOperatorDeclarationNoToken()
        {
            var content = @"class CL {
    explicit $$";
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", content), @"String");
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact]
        public async Task PropertyDeclaration()
        {
            var content = @"class CL {
    $$ Prop {";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact]
        public async Task EventDeclaration()
        {
            var content = @"class CL {
    event $$ Event {";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact]
        public async Task IndexerDeclaration()
        {
            var content = @"class CL {
    $$ this";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact]
        public async Task Parameter()
        {
            var content = @"class CL {
    void Method($$";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact]
        public async Task ArrayType()
        {
            var content = @"class CL {
    $$ [";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact]
        public async Task PointerType()
        {
            var content = @"class CL {
    $$ *";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact]
        public async Task NullableType()
        {
            var content = @"class CL {
    $$ ?";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact]
        public async Task DelegateDeclaration()
        {
            var content = @"class CL {
    delegate $$";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact]
        public async Task MethodDeclaration()
        {
            var content = @"class CL {
    $$ M(";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact]
        public async Task OperatorDeclaration()
        {
            var content = @"class CL {
    $$ operator";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", content), @"System");
        }

        [Fact]
        public async Task ParenthesizedExpression()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"($$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"($$")), @"System");
        }

        [Fact]
        public async Task InvocationExpression()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"$$(")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"$$(")), @"System");
        }

        [Fact]
        public async Task ElementAccessExpression()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"$$[")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"$$[")), @"System");
        }

        [Fact]
        public async Task Argument()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"i[$$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"i[$$")), @"System");
        }

        [Fact]
        public async Task CastExpressionExpressionPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"(c)$$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"(c)$$")), @"System");
        }

        [Fact]
        public async Task FromClauseInPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in $$")), @"System");
        }

        [Fact]
        public async Task LetClauseExpressionPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C let n = $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C let n = $$")), @"System");
        }

        [Fact]
        public async Task OrderingExpressionPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C orderby $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C orderby $$")), @"System");
        }

        [Fact]
        public async Task SelectClauseExpressionPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C select $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C select $$")), @"System");
        }

        [Fact]
        public async Task ExpressionStatement()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"$$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"$$")), @"System");
        }

        [Fact]
        public async Task ReturnStatement()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"return $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"return $$")), @"System");
        }

        [Fact]
        public async Task ThrowStatement()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"throw $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"throw $$")), @"System");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/760097")]
        public async Task YieldReturnStatement()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"yield return $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"yield return $$")), @"System");
        }

        [Fact]
        public async Task ForEachStatementExpressionPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"foreach(T t in $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"foreach(T t in $$")), @"System");
        }

        [Fact]
        public async Task UsingStatementExpressionPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"using($$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"using($$")), @"System");
        }

        [Fact]
        public async Task LockStatement()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"lock($$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"lock($$")), @"System");
        }

        [Fact]
        public async Task EqualsValueClause()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var i = $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var i = $$")), @"System");
        }

        [Fact]
        public async Task ForStatementInitializersPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"for($$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"for($$")), @"System");
        }

        [Fact]
        public async Task ForStatementConditionOptPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"for(i=0;$$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"for(i=0;$$")), @"System");
        }

        [Fact]
        public async Task ForStatementIncrementorsPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"for(i=0;i>10;$$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"for(i=0;i>10;$$")), @"System");
        }

        [Fact]
        public async Task DoStatementConditionPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"do {} while($$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"do {} while($$")), @"System");
        }

        [Fact]
        public async Task WhileStatementConditionPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"while($$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"while($$")), @"System");
        }

        [Fact]
        public async Task ArrayRankSpecifierSizesPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"int [$$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"int [$$")), @"System");
        }

        [Fact]
        public async Task PrefixUnaryExpression()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"+$$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"+$$")), @"System");
        }

        [Fact]
        public async Task PostfixUnaryExpression()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"$$++")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"$$++")), @"System");
        }

        [Fact]
        public async Task BinaryExpressionLeftPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"$$ + 1")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"$$ + 1")), @"System");
        }

        [Fact]
        public async Task BinaryExpressionRightPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"1 + $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"1 + $$")), @"System");
        }

        [Fact]
        public async Task AssignmentExpressionLeftPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"$$ = 1")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"$$ = 1")), @"System");
        }

        [Fact]
        public async Task AssignmentExpressionRightPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"1 = $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"1 = $$")), @"System");
        }

        [Fact]
        public async Task ConditionalExpressionConditionPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"$$? 1:")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"$$? 1:")), @"System");
        }

        [Fact]
        public async Task ConditionalExpressionWhenTruePart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"true? $$:")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"true? $$:")), @"System");
        }

        [Fact]
        public async Task ConditionalExpressionWhenFalsePart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"true? 1:$$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"true? 1:$$")), @"System");
        }

        [Fact]
        public async Task JoinClauseInExpressionPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C join p in $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C join p in $$")), @"System");
        }

        [Fact]
        public async Task JoinClauseLeftExpressionPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C join p in P on $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C join p in P on $$")), @"System");
        }

        [Fact]
        public async Task JoinClauseRightExpressionPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C join p in P on id equals $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C join p in P on id equals $$")), @"System");
        }

        [Fact]
        public async Task WhereClauseConditionPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C where $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C where $$")), @"System");
        }

        [Fact]
        public async Task GroupClauseGroupExpressionPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C group $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C group $$")), @"System");
        }

        [Fact]
        public async Task GroupClauseByExpressionPart()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C group g by $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = from c in C group g by $$")), @"System");
        }

        [Fact]
        public async Task IfStatement()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"if ($$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"if ($$")), @"System");
        }

        [Fact]
        public async Task SwitchStatement()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"switch($$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"switch($$")), @"System");
        }

        [Fact]
        public async Task SwitchLabelCase()
        {
            var content = @"switch(i) { case $$";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(content)), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(content)), @"System");
        }

        [Fact]
        public async Task SwitchPatternLabelCase()
        {
            var content = @"switch(i) { case $$ when";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(content)), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(content)), @"System");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33915")]
        public async Task SwitchExpressionFirstBranch()
        {
            var content = @"i switch { $$";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(content)), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(content)), @"System");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33915")]
        public async Task SwitchExpressionSecondBranch()
        {
            var content = @"i switch { 1 => true, $$";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(content)), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(content)), @"System");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33915")]
        public async Task PositionalPatternFirstPosition()
        {
            var content = @"i is ($$";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(content)), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(content)), @"System");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33915")]
        public async Task PositionalPatternSecondPosition()
        {
            var content = @"i is (1, $$";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(content)), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(content)), @"System");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33915")]
        public async Task PropertyPatternFirstPosition()
        {
            var content = @"i is { P: $$";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(content)), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(content)), @"System");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33915")]
        public async Task PropertyPatternSecondPosition()
        {
            var content = @"i is { P1: 1, P2: $$";
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(content)), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(content)), @"System");
        }

        [Fact]
        public async Task InitializerExpression()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = new [] { $$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"var t = new [] { $$")), @"System");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
        public async Task TypeParameterConstraintClause()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"class CL<T> where T : $$"), @"System");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
        public async Task TypeParameterConstraintClause_NotStaticClass()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class CL<T> where T : $$"), @"Console");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
        public async Task TypeParameterConstraintClause_StillShowStaticClassWhenHaveInternalType()
        {
            await VerifyItemExistsAsync(
@"static class Test
{
    public interface IInterface {}
}

class CL<T> where T : $$", @"Test");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
        public async Task TypeParameterConstraintClause_NotSealedClass()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class CL<T> where T : $$"), @"String");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
        public async Task TypeParameterConstraintClause_StillShowSealedClassWhenHaveInternalType()
        {
            await VerifyItemExistsAsync(
@"sealed class Test
{
    public interface IInterface {}
}

class CL<T> where T : $$", @"Test");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
        public async Task TypeParameterConstraintClause_StillShowStaticAndSealedTypesNotDirectlyInConstraint()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"class CL<T> where T : IList<$$"), @"System");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"class CL<T> where T : IList<$$"), @"String");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
        public async Task TypeParameterConstraintClauseList()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"class CL<T> where T : A, $$"), @"System");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
        public async Task TypeParameterConstraintClauseList_NotStaticClass()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class CL<T> where T : A, $$"), @"Console");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
        public async Task TypeParameterConstraintClauseList_StillShowStaticClassWhenHaveInternalType()
        {
            await VerifyItemExistsAsync(
@"static class Test
{
    public interface IInterface {}
}

class CL<T> where T : A, $$", @"Test");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
        public async Task TypeParameterConstraintClauseList_NotSealedClass()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class CL<T> where T : A, $$"), @"String");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
        public async Task TypeParameterConstraintClauseList_StillShowSealedClassWhenHaveInternalType()
        {
            await VerifyItemExistsAsync(
@"sealed class Test
{
    public interface IInterface {}
}

class CL<T> where T : A, $$", @"Test");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30784")]
        public async Task TypeParameterConstraintClauseList_StillShowStaticAndSealedTypesNotDirectlyInConstraint()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"class CL<T> where T : A, IList<$$"), @"System");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"class CL<T> where T : A, IList<$$"), @"String");
        }

        [Fact]
        public async Task TypeParameterConstraintClauseAnotherWhere()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class CL<T> where T : A where$$"), @"System");
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class CL<T> where T : A where$$"), @"String");
        }

        [Fact]
        public async Task TypeSymbolOfTypeParameterConstraintClause1()
        {
            await VerifyItemExistsAsync(@"class CL<T> where $$", @"T");
            await VerifyItemExistsAsync(@"class CL{ delegate void F<T>() where $$} ", @"T");
            await VerifyItemExistsAsync(@"class CL{ void F<T>() where $$", @"T");
        }

        [Fact]
        public async Task TypeSymbolOfTypeParameterConstraintClause2()
        {
            await VerifyItemIsAbsentAsync(@"class CL<T> where $$", @"System");
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class CL<T> where $$"), @"String");
        }

        [Fact]
        public async Task TypeSymbolOfTypeParameterConstraintClause3()
        {
            await VerifyItemIsAbsentAsync(@"class CL<T1> { void M<T2> where $$", @"T1");
            await VerifyItemExistsAsync(@"class CL<T1> { void M<T2>() where $$", @"T2");
        }

        [Fact]
        public async Task BaseList1()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"class CL : $$"), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"class CL : $$"), @"System");
        }

        [Fact]
        public async Task BaseList2()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"class CL : B, $$"), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"class CL : B, $$"), @"System");
        }

        [Fact]
        public async Task BaseListWhere()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class CL<T> : B where$$"), @"String");
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class CL<T> : B where$$"), @"System");
        }

        [Fact]
        public async Task AliasedName()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", AddInsideMethod(@"global::$$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"global::$$")), @"System");
        }

        [Fact]
        public async Task AliasedNamespace()
            => await VerifyItemExistsAsync(AddUsingDirectives("using S = System;", AddInsideMethod(@"S.$$")), @"String");

        [Fact]
        public async Task AliasedType()
            => await VerifyItemExistsAsync(AddUsingDirectives("using S = System.String;", AddInsideMethod(@"S.$$")), @"Empty");

        [Fact]
        public async Task ConstructorInitializer()
        {
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class C { C() : $$"), @"String");
            await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class C { C() : $$"), @"System");
        }

        [Fact]
        public async Task Typeof1()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"typeof($$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"typeof($$")), @"System");
        }

        [Fact]
        public async Task Typeof2()
            => await VerifyItemIsAbsentAsync(AddInsideMethod(@"var x = 0; typeof($$"), @"x");

        [Fact]
        public async Task Sizeof1()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"sizeof($$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"sizeof($$")), @"System");
        }

        [Fact]
        public async Task Sizeof2()
            => await VerifyItemIsAbsentAsync(AddInsideMethod(@"var x = 0; sizeof($$"), @"x");

        [Fact]
        public async Task Default1()
        {
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"default($$")), @"String");
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", AddInsideMethod(@"default($$")), @"System");
        }

        [Fact]
        public async Task Default2()
            => await VerifyItemIsAbsentAsync(AddInsideMethod(@"var x = 0; default($$"), @"x");

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543819")]
        public async Task Checked()
            => await VerifyItemExistsAsync(AddInsideMethod(@"var x = 0; checked($$"), @"x");

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543819")]
        public async Task Unchecked()
            => await VerifyItemExistsAsync(AddInsideMethod(@"var x = 0; unchecked($$"), @"x");

        [Fact]
        public async Task Locals()
            => await VerifyItemExistsAsync(@"class c { void M() { string goo; $$", "goo");

        [Fact]
        public async Task Parameters_01()
            => await VerifyItemExistsAsync(@"class c { void M(string args) { $$", "args");

        [Theory]
        [InlineData("a")]
        [InlineData("ar")]
        [InlineData("arg")]
        [InlineData("args")]
        public async Task Parameters_02(string prefix)
        {
            await VerifyItemExistsAsync(prefix + "$$", "args", sourceCodeKind: SourceCodeKind.Regular);
        }

        [Theory]
        [InlineData("a")]
        [InlineData("ar")]
        [InlineData("arg")]
        [InlineData("args")]
        public async Task Parameters_03(string prefix)
        {
            await VerifyItemIsAbsentAsync(prefix + "$$", "args", sourceCodeKind: SourceCodeKind.Script);
        }

        [Theory]
        [InlineData("a")]
        [InlineData("ar")]
        [InlineData("arg")]
        [InlineData("args")]
        public async Task Parameters_04(string prefix)
        {
            await VerifyItemExistsAsync(prefix + @"$$
Systen.Console.WriteLine();
", "args", sourceCodeKind: SourceCodeKind.Regular);
        }

        [Theory]
        [InlineData("a")]
        [InlineData("ar")]
        [InlineData("arg")]
        [InlineData("args")]
        public async Task Parameters_05(string prefix)
        {
            await VerifyItemExistsAsync(@"
Systen.Console.WriteLine();
" + prefix + "$$", "args", sourceCodeKind: SourceCodeKind.Regular);
        }

        [Theory]
        [InlineData("a")]
        [InlineData("ar")]
        [InlineData("arg")]
        [InlineData("args")]
        public async Task Parameters_06(string prefix)
        {
            await VerifyItemExistsAsync(@"
Systen.Console.WriteLine();
" + prefix + @"$$
Systen.Console.WriteLine();
", "args", sourceCodeKind: SourceCodeKind.Regular);
        }

        [Theory]
        [InlineData("a")]
        [InlineData("ar")]
        [InlineData("arg")]
        [InlineData("args")]
        public async Task Parameters_07(string prefix)
        {
            await VerifyItemExistsAsync("call(" + prefix + "$$)", "args", sourceCodeKind: SourceCodeKind.Regular);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/55969")]
        public async Task Parameters_TopLevelStatement_1()
            => await VerifyItemIsAbsentAsync(@"$$", "args", sourceCodeKind: SourceCodeKind.Regular);

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/55969")]
        public async Task Parameters_TopLevelStatement_2()
            => await VerifyItemExistsAsync(
                @"using System;
Console.WriteLine();
$$", "args", sourceCodeKind: SourceCodeKind.Regular);

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/55969")]
        public async Task Parameters_TopLevelStatement_3()
            => await VerifyItemIsAbsentAsync(
                @"using System;
$$", "args", sourceCodeKind: SourceCodeKind.Regular);

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55969")]
        public async Task Parameters_TopLevelStatement_4()
            => await VerifyItemExistsAsync(@"string first = $$", "args", sourceCodeKind: SourceCodeKind.Regular);

        [Fact]
        public async Task LambdaDiscardParameters()
            => await VerifyItemIsAbsentAsync(@"class C { void M() { System.Func<int, string, int> f = (int _, string _) => 1 + $$", "_");

        [Fact]
        public async Task AnonymousMethodDiscardParameters()
            => await VerifyItemIsAbsentAsync(@"class C { void M() { System.Func<int, string, int> f = delegate(int _, string _) { return 1 + $$ }; } }", "_");

        [Fact]
        public async Task CommonTypesInNewExpressionContext()
            => await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"class c { void M() { new $$"), "Exception");

        [Fact]
        public async Task NoCompletionForUnboundTypes()
            => await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class c { void M() { goo.$$"), "Equals");

        [Fact]
        public async Task NoParametersInTypeOf()
            => await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class c { void M(int x) { typeof($$"), "x");

        [Fact]
        public async Task NoParametersInDefault()
            => await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"class c { void M(int x) { default($$"), "x");

        [Fact]
        public async Task NoParametersInSizeOf()
            => await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"public class C { void M(int x) { unsafe { sizeof($$"), "x");

        [Fact]
        public async Task NoParametersInGenericParameterList()
            => await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"public class Generic<T> { void M(int x) { Generic<$$"), "x");

        [Fact]
        public async Task NoMembersAfterNullLiteral()
            => await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"public class C { void M() { null.$$"), "Equals");

        [Fact]
        public async Task MembersAfterTrueLiteral()
            => await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { true.$$"), "Equals");

        [Fact]
        public async Task MembersAfterFalseLiteral()
            => await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { false.$$"), "Equals");

        [Fact]
        public async Task MembersAfterCharLiteral()
            => await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { 'c'.$$"), "Equals");

        [Fact]
        public async Task MembersAfterStringLiteral()
            => await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { """".$$"), "Equals");

        [Fact]
        public async Task MembersAfterVerbatimStringLiteral()
            => await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { @"""".$$"), "Equals");

        [Fact]
        public async Task MembersAfterNumericLiteral()
        {
            // NOTE: the Completion command handler will suppress this case if the user types '.',
            // but we still need to show members if the user specifically invokes statement completion here.
            await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { 2.$$"), "Equals");
        }

        [Fact]
        public async Task NoMembersAfterParenthesizedNullLiteral()
            => await VerifyItemIsAbsentAsync(AddUsingDirectives("using System;", @"public class C { void M() { (null).$$"), "Equals");

        [Fact]
        public async Task MembersAfterParenthesizedTrueLiteral()
            => await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { (true).$$"), "Equals");

        [Fact]
        public async Task MembersAfterParenthesizedFalseLiteral()
            => await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { (false).$$"), "Equals");

        [Fact]
        public async Task MembersAfterParenthesizedCharLiteral()
            => await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { ('c').$$"), "Equals");

        [Fact]
        public async Task MembersAfterParenthesizedStringLiteral()
            => await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { ("""").$$"), "Equals");

        [Fact]
        public async Task MembersAfterParenthesizedVerbatimStringLiteral()
            => await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { (@"""").$$"), "Equals");

        [Fact]
        public async Task MembersAfterParenthesizedNumericLiteral()
            => await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { (2).$$"), "Equals");

        [Fact]
        public async Task MembersAfterArithmeticExpression()
            => await VerifyItemExistsAsync(AddUsingDirectives("using System;", @"public class C { void M() { (1 + 1).$$"), "Equals");

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539332")]
        public async Task InstanceTypesAvailableInUsingAlias()
            => await VerifyItemExistsAsync(@"using S = System.$$", "String");

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539812")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539812")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539812")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539812")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539812")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539812")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539812")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540155")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540155")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540012")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540012")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540012")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540012")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540012")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540012")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540012")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540012")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540197")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540212")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540212")]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24326")]
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

        [Fact]
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

        [Fact]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24326")]
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

        [Fact]
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

        [Fact]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24326")]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25569")]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25569")]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25569")]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35178")]
        public async Task RefStructMembersEmptyByDefault()
        {
            var markup = @"
ref struct Test {}
class C
{
    void M()
    {
        var test = new Test();
        test.$$
    }
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35178")]
        public async Task RefStructMembersHasMethodIfItWasOverriden()
        {
            var markup = @"
ref struct Test
{
    public override string ToString() => string.Empty;
}
class C
{
    void M()
    {
        var test = new Test();
        test.$$
    }
}
";
            await VerifyItemExistsAsync(markup, "ToString");
            await VerifyItemIsAbsentAsync(markup, "GetType");
            await VerifyItemIsAbsentAsync(markup, "Equals");
            await VerifyItemIsAbsentAsync(markup, "GetHashCode");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35178")]
        public async Task RefStructMembersHasMethodsForNameof()
        {
            var markup = @"
ref struct Test {}
class C
{
    void M()
    {
        var test = new Test();
        _ = nameof(test.$$);
    }
}
";
            await VerifyItemExistsAsync(markup, "ToString");
            await VerifyItemExistsAsync(markup, "GetType");
            await VerifyItemExistsAsync(markup, "Equals");
            await VerifyItemExistsAsync(markup, "GetHashCode");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53585")]
        public async Task AfterStaticLocalFunction_TypeOnly()
        {
            var markup = @"
using System;
class C
{
    void M(String parameter)
    {
        static $$
    }
}
";
            await VerifyItemExistsAsync(markup, "String");
            await VerifyItemIsAbsentAsync(markup, "parameter");
        }

        [Theory]
        [WorkItem("https://github.com/dotnet/roslyn/issues/53585")]
        [InlineData("extern")]
        [InlineData("static extern")]
        [InlineData("extern static")]
        [InlineData("unsafe")]
        [InlineData("static unsafe")]
        [InlineData("unsafe static")]
        [InlineData("unsafe extern")]
        [InlineData("extern unsafe")]
        public async Task AfterLocalFunction_TypeOnly(string keyword)
        {
            var markup = $@"
using System;
class C
{{
    void M(String parameter)
    {{
        {keyword} $$
    }}
}}
";
            await VerifyItemExistsAsync(markup, "String");
            await VerifyItemIsAbsentAsync(markup, "parameter");
        }

        [Theory]
        [WorkItem("https://github.com/dotnet/roslyn/issues/60341")]
        [InlineData("async")]
        [InlineData("static async")]
        [InlineData("async static")]
        [InlineData("async unsafe")]
        [InlineData("unsafe async")]
        [InlineData("extern unsafe async static")]
        public async Task AfterLocalFunction_TypeOnly_Async(string keyword)
        {
            var markup = $@"
using System;
class C
{{
    void M(String parameter)
    {{
        {keyword} $$
    }}
}}
";
            await VerifyItemIsAbsentAsync(markup, "String");
            await VerifyItemIsAbsentAsync(markup, "parameter");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60341")]
        public async Task AfterAsyncLocalFunctionWithTwoAsyncs()
        {
            var markup = @"
using System;
class C
{
    void M(string parameter)
    {
        async async $$
    }
}
";
            await VerifyItemIsAbsentAsync(markup, "String");
            await VerifyItemIsAbsentAsync(markup, "parameter");
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/53585")]
        [InlineData("void")]
        [InlineData("string")]
        [InlineData("String")]
        [InlineData("(int, int)")]
        [InlineData("async void")]
        [InlineData("async System.Threading.Tasks.Task")]
        [InlineData("int Function")]
        public async Task NotAfterReturnTypeInLocalFunction(string returnType)
        {
            var markup = @$"
using System;
class C
{{
    void M(String parameter)
    {{
        static {returnType} $$
    }}
}}
";
            await VerifyItemIsAbsentAsync(markup, "String");
            await VerifyItemIsAbsentAsync(markup, "parameter");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25569")]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25569")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539217")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539217")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539217")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539217")]
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

            // Top-level statements are not allowed to follow classes, but we still offer it in completion for a few
            // reasons:
            //
            // 1. The code is simpler
            // 2. It's a relatively rare coding practice to define types outside of namespaces
            // 3. It allows the compiler to produce a better error message when users type things in the wrong order
            await VerifyItemExistsAsync(markup, "Q", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Regular);
            await VerifyItemIsAbsentAsync(markup, "R", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Regular);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539217")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539217")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539217")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540574")]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21596")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540750")]
        public async Task CompletionAfterNewInScript()
        {
            var markup = @"
using System;

new $$";

            await VerifyItemExistsAsync(markup, "String", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540933")]
        public async Task ExtensionMethodsInScript()
        {
            var markup = @"
using System.Linq;
var a = new int[] { 1, 2 };
a.$$";

            await VerifyItemExistsAsync(markup, "ElementAt", displayTextSuffix: "<>", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541019")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541108")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541108")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541216")]
        public async Task InMultiLineCommentAtEndOfFile()
        {
            var markup = @"
using System;
/*$$";

            await VerifyItemIsAbsentAsync(markup, "Console", expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541218")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552717")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552717")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552717")]
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

        [Fact]
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

        [Fact]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542225")]
        public async Task AttributeName()
        {
            var markup = @"
using System;
[$$";

            await VerifyItemExistsAsync(markup, "CLSCompliant");
            await VerifyItemIsAbsentAsync(markup, "CLSCompliantAttribute");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542225")]
        public async Task AttributeNameAfterSpecifier()
        {
            var markup = @"
using System;
[assembly:$$
";

            await VerifyItemExistsAsync(markup, "CLSCompliant");
            await VerifyItemIsAbsentAsync(markup, "CLSCompliantAttribute");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542225")]
        public async Task AttributeNameInAttributeList()
        {
            var markup = @"
using System;
[CLSCompliant, $$";

            await VerifyItemExistsAsync(markup, "CLSCompliant");
            await VerifyItemIsAbsentAsync(markup, "CLSCompliantAttribute");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542225")]
        public async Task AttributeNameBeforeClass()
        {
            var markup = @"
using System;
[$$
class C { }";

            await VerifyItemExistsAsync(markup, "CLSCompliant");
            await VerifyItemIsAbsentAsync(markup, "CLSCompliantAttribute");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542225")]
        public async Task AttributeNameAfterSpecifierBeforeClass()
        {
            var markup = @"
using System;
[assembly:$$
class C { }";

            await VerifyItemExistsAsync(markup, "CLSCompliant");
            await VerifyItemIsAbsentAsync(markup, "CLSCompliantAttribute");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542225")]
        public async Task AttributeNameInAttributeArgumentList()
        {
            var markup = @"
using System;
[CLSCompliant($$
class C { }";

            await VerifyItemExistsAsync(markup, "CLSCompliantAttribute");
            await VerifyItemIsAbsentAsync(markup, "CLSCompliant");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542225")]
        public async Task AttributeNameInsideClass()
        {
            var markup = @"
using System;
class C { $$ }";

            await VerifyItemExistsAsync(markup, "CLSCompliantAttribute");
            await VerifyItemIsAbsentAsync(markup, "CLSCompliant");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542954")]
        public async Task NamespaceAliasInAttributeName1()
        {
            var markup = @"
using Alias = System;

[$$
class C { }";

            await VerifyItemExistsAsync(markup, "Alias");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542954")]
        public async Task NamespaceAliasInAttributeName2()
        {
            var markup = @"
using Alias = Goo;

namespace Goo { }

[$$
class C { }";

            await VerifyItemIsAbsentAsync(markup, "Alias");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542954")]
        public async Task NamespaceAliasInAttributeName3()
        {
            var markup = @"
using Alias = Goo;

namespace Goo { class A : System.Attribute { } }

[$$
class C { }";

            await VerifyItemExistsAsync(markup, "Alias");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545121")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545121")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545121")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545121")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545348")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545348")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545348")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545348")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545121")]
        public async Task AttributeNameAfterGlobalAlias()
        {
            var markup = @"
class MyAttribute : System.Attribute { }
[global::$$
class Program { }";
            await VerifyItemExistsAsync(markup, "My", sourceCodeKind: SourceCodeKind.Regular);
            await VerifyItemIsAbsentAsync(markup, "MyAttribute", sourceCodeKind: SourceCodeKind.Regular);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545121")]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25589")]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542230")]
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

        [Fact]
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

        [Fact]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542429")]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25084#issuecomment-370148553")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542429")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542429")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542429")]
        public async Task ConstantsInAttribute1()
        {
            var markup = @"
class C
{
    public const int GOO = 0;
    [System.AttributeUsage($$";

            await VerifyItemExistsAsync(markup, "GOO");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542429")]
        public async Task ConstantsInAttribute2()
        {
            var markup = @"
class C
{
    public const int GOO = 0;
    [System.AttributeUsage(GOO, $$";

            await VerifyItemExistsAsync(markup, "GOO");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542429")]
        public async Task ConstantsInAttribute3()
        {
            var markup = @"
class C
{
    public const int GOO = 0;
    [System.AttributeUsage(validOn: $$";

            await VerifyItemExistsAsync(markup, "GOO");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542429")]
        public async Task ConstantsInAttribute4()
        {
            var markup = @"
class C
{
    public const int GOO = 0;
    [System.AttributeUsage(AllowMultiple = $$";

            await VerifyItemExistsAsync(markup, "GOO");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542429")]
        public async Task ConstantsInParameterDefaultValue()
        {
            var markup = @"
class C
{
    public const int GOO = 0;
    void M(int x = $$";

            await VerifyItemExistsAsync(markup, "GOO");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542429")]
        public async Task ConstantsInConstField()
        {
            var markup = @"
class C
{
    public const int GOO = 0;
    const int BAR = $$";

            await VerifyItemExistsAsync(markup, "GOO");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542429")]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

{FeaturesResources.Types_colon}
    'a {FeaturesResources.is_} new {{  }}";

            await VerifyItemExistsAsync(markup, "a", expectedDescription);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543288")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543601")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543601")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543601")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543601")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543680")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543680")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543104")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543104")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529138")]
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

        [Fact, WorkItem(61343, "https://github.com/dotnet/roslyn/issues/61343")]
        public async Task LambdaParameterMemberAccessOverloads()
        {
            var markup = @"
using System.Linq;

public class C
{
    public void M() { }
    public void M(int i) { }
    public int P { get; }

    void Test()
    {
        new C[0].Select(x => x.$$)
    }
}";

            await VerifyItemExistsAsync(markup, "M", expectedDescriptionOrNull: $"void C.M() (+ 1 {FeaturesResources.overload})");
            await VerifyItemExistsAsync(markup, "P", expectedDescriptionOrNull: "int C.P { get; }");
        }

        [Fact]
        public async Task ValueNotAtRoot_Interactive()
        {
            await VerifyItemIsAbsentAsync(
@"$$",
"value",
expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact]
        public async Task ValueNotAfterClass_Interactive()
        {
            await VerifyItemIsAbsentAsync(
@"class C { }
$$",
"value",
expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact]
        public async Task ValueNotAfterGlobalStatement_Interactive()
        {
            await VerifyItemIsAbsentAsync(
@"System.Console.WriteLine();
$$",
"value",
expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact]
        public async Task ValueNotAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyItemIsAbsentAsync(
@"int i = 0;
$$",
"value",
expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact]
        public async Task ValueNotInUsingAlias()
        {
            await VerifyItemIsAbsentAsync(
@"using Goo = $$",
"value");
        }

        [Fact]
        public async Task ValueNotInEmptyStatement()
        {
            await VerifyItemIsAbsentAsync(AddInsideMethod(
@"$$"),
"value");
        }

        [Fact]
        public async Task ValueInsideSetter()
        {
            await VerifyItemExistsAsync(
@"class C {
    int Goo {
      set {
        $$",
"value");
        }

        [Fact]
        public async Task ValueInsideAdder()
        {
            await VerifyItemExistsAsync(
@"class C {
    event int Goo {
      add {
        $$",
"value");
        }

        [Fact]
        public async Task ValueInsideRemover()
        {
            await VerifyItemExistsAsync(
@"class C {
    event int Goo {
      remove {
        $$",
"value");
        }

        [Fact]
        public async Task ValueNotAfterDot()
        {
            await VerifyItemIsAbsentAsync(
@"class C {
    int Goo {
      set {
        this.$$",
"value");
        }

        [Fact]
        public async Task ValueNotAfterArrow()
        {
            await VerifyItemIsAbsentAsync(
@"class C {
    int Goo {
      set {
        a->$$",
"value");
        }

        [Fact]
        public async Task ValueNotAfterColonColon()
        {
            await VerifyItemIsAbsentAsync(
@"class C {
    int Goo {
      set {
        a::$$",
"value");
        }

        [Fact]
        public async Task ValueNotInGetter()
        {
            await VerifyItemIsAbsentAsync(
@"class C {
    int Goo {
      get {
        $$",
"value");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544205")]
        public async Task NotAfterNullableType()
        {
            await VerifyItemIsAbsentAsync(
@"class C {
    void M() {
        int goo = 0;
        C? $$",
"goo");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544205")]
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544205")]
        public async Task NotAfterNullableTypeAndPartialIdentifier()
        {
            await VerifyItemIsAbsentAsync(
@"class C {
    void M() {
        int goo = 0;
        C? f$$",
"goo");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544205")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544205")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544205")]
        public async Task NotAfterPointerType()
        {
            await VerifyItemIsAbsentAsync(
@"class C {
    void M() {
        int goo = 0;
        C* $$",
"goo");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544205")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544205")]
        public async Task NotAfterPointerTypeAndPartialIdentifier()
        {
            await VerifyItemIsAbsentAsync(
@"class C {
    void M() {
        int goo = 0;
        C* f$$",
"goo");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544205")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544205")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543868")]
        public async Task AfterEventFieldDeclaredInSameType()
        {
            await VerifyItemExistsAsync(
@"class C {
    public event System.EventHandler E;
    void M() {
        E.$$",
"Invoke");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543868")]
        public async Task NotAfterFullEventDeclaredInSameType()
        {
            await VerifyItemIsAbsentAsync(
@"class C {
        public event System.EventHandler E { add { } remove { } }
    void M() {
        E.$$",
"Invoke");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543868")]
        public async Task NotAfterEventDeclaredInDifferentType()
        {
            await VerifyItemIsAbsentAsync(
@"class C {
    void M() {
        System.Console.CancelKeyPress.$$",
"Invoke");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544219")]
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544219")]
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
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(11931, "DevDiv_Projects/Roslyn")]
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544928")]
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544928")]
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544931")]
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544931")]
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

        [Fact, WorkItem(10572, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(10572, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(10572, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(10572, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(10572, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(10572, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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
            HideAdvancedMembers = false;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);

            HideAdvancedMembers = true;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

            HideAdvancedMembers = false;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);

            HideAdvancedMembers = true;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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
            HideAdvancedMembers = true;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);

            HideAdvancedMembers = false;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/522440")]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674611")]
        [WpfFact(Skip = "674611")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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
            HideAdvancedMembers = true;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);

            HideAdvancedMembers = false;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Bar",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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
            HideAdvancedMembers = true;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);

            HideAdvancedMembers = false;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

            HideAdvancedMembers = false;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Changed",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);

            HideAdvancedMembers = true;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Changed",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

            HideAdvancedMembers = false;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Handler",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);

            HideAdvancedMembers = true;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Handler",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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
            HideAdvancedMembers = false;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);

            HideAdvancedMembers = true;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

            HideAdvancedMembers = false;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);

            HideAdvancedMembers = true;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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
            HideAdvancedMembers = false;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);

            HideAdvancedMembers = true;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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
            HideAdvancedMembers = false;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);

            HideAdvancedMembers = true;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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
            HideAdvancedMembers = false;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);

            HideAdvancedMembers = true;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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
            HideAdvancedMembers = false;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);

            HideAdvancedMembers = true;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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
            HideAdvancedMembers = false;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);

            HideAdvancedMembers = true;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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
            HideAdvancedMembers = false;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 1,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);

            HideAdvancedMembers = true;

            await VerifyItemInEditorBrowsableContextsAsync(
                markup: markup,
                referencedCode: referencedCode,
                item: "Goo",
                expectedSymbolsSameSolution: 1,
                expectedSymbolsMetadataReference: 0,
                sourceLanguage: LanguageNames.CSharp,
                referencedLanguage: LanguageNames.CSharp);
        }

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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
                referencedLanguage: LanguageNames.VisualBasic);
        }

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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
                referencedLanguage: LanguageNames.VisualBasic);
        }

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem(7336, "DevDiv_Projects/Roslyn")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545557")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545647")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545647")]
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

        [Fact]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546841")]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/12781")]
        public async Task TestFieldDeclarationAmbiguity()
        {
            var markup = @"
using System;
Environment.$$
var v;
}";

            await VerifyItemExistsAsync(markup, "CommandLine", sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60341")]
        public async Task AfterAsync3()
        {
            var markup = @"
using System.Threading.Tasks;
class Program
{
    public async $$
    
    public void M() {}
}";

            await VerifyItemExistsAsync(markup, "Task");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60341")]
        public async Task AfterAsync4()
        {
            var markup = @"
using System;
using System.Threading.Tasks;
class Program
{
    public async $$
}";

            await VerifyItemExistsAsync(markup, "Task");
            await VerifyItemIsAbsentAsync(markup, "Console");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60341")]
        public async Task AfterAsync5()
        {
            var markup = @"
using System.Threading.Tasks;
class Program
{
    public async $$
}

class Test {}";

            await VerifyItemExistsAsync(markup, "Task");
            await VerifyItemIsAbsentAsync(markup, "Test");
        }

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

            var description = $@"({CSharpFeaturesResources.awaitable}) Task Program.goo()";

            await VerifyItemWithMscorlib45Async(markup, "goo", description, "C#");
        }

        [Fact]
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

            var description = $@"({CSharpFeaturesResources.awaitable}) Task<int> Program.goo()";

            await VerifyItemWithMscorlib45Async(markup, "goo", description, "C#");
        }

        [Fact]
        public async Task AwaitableDotsLikeRangeExpression()
        {
            var markup = @"
using System.IO;
using System.Threading.Tasks;

namespace N
{
    class C
    {
        async Task M()
        {
            var request = new Request();
            var m = await request.$$.ReadAsStreamAsync();
        }
    }

    class Request
    {
        public Task<Stream> ReadAsStreamAsync() => null;
    }
}";

            await VerifyItemExistsAsync(markup, "ReadAsStreamAsync");
        }

        [Fact]
        public async Task AwaitableDotsLikeRangeExpressionWithParentheses()
        {
            var markup = @"
using System.IO;
using System.Threading.Tasks;

namespace N
{
    class C
    {
        async Task M()
        {
            var request = new Request();
            var m = (await request).$$.ReadAsStreamAsync();
        }
    }

    class Request
    {
        public Task<Stream> ReadAsStreamAsync() => null;
    }
}";
            // Nothing should be found: no awaiter for request.
            await VerifyItemIsAbsentAsync(markup, "Result");
            await VerifyItemIsAbsentAsync(markup, "ReadAsStreamAsync");
        }

        [Fact]
        public async Task AwaitableDotsLikeRangeExpressionWithTaskAndParentheses()
        {
            var markup = @"
using System.IO;
using System.Threading.Tasks;

namespace N
{
    class C
    {
        async Task M()
        {
            var request = new Task<Request>();
            var m = (await request).$$.ReadAsStreamAsync();
        }
    }

    class Request
    {
        public Task<Stream> ReadAsStreamAsync() => null;
    }
}";

            await VerifyItemIsAbsentAsync(markup, "Result");
            await VerifyItemExistsAsync(markup, "ReadAsStreamAsync");
        }

        [Fact]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/568986")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/550717")]
        public async Task TypeArgumentsInConstraintAfterBaselist()
        {
            var markup = @"
public class Goo<T> : System.Object where $$
{
}";
            await VerifyItemExistsAsync(markup, "T");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/647175")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/669624")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/667752")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/766869")]
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
            await VerifyItemWithProjectReferenceAsync(markup, referencedCode, "C", 1, LanguageNames.CSharp, LanguageNames.CSharp);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/834605")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/825661")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/825661")]
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

        [Fact, WorkItem("http://github.com/dotnet/roslyn/issues/7648")]
        public async Task NothingAfterBaseDotInScriptContext()
            => await VerifyItemIsAbsentAsync(@"base.$$", @"ToString", sourceCodeKind: SourceCodeKind.Script);

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/858086")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/876031")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/849698")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/849698")]
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
            await VerifyItemWithAliasedMetadataReferencesAsync(markup, "Bar", "Bar", 1, "C#", "C#");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/635957")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/635957")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/44423")]
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
            await VerifyItemExistsAsync(markup, "C");
            await VerifyItemIsAbsentAsync(markup, "N");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/635957")]
        public async Task StructDestructor()
        {
            var markup = @"
struct C
{
   ~$$
}";
            await VerifyItemIsAbsentAsync(markup, "C");
        }

        [Theory]
        [InlineData("record")]
        [InlineData("record class")]
        public async Task RecordDestructor(string record)
        {
            var markup = $@"
{record} C
{{
   ~$$
}}";
            await VerifyItemExistsAsync(markup, "C");
        }

        [Fact]
        public async Task RecordStructDestructor()
        {
            var markup = $@"
record struct C
{{
   ~$$
}}";
            await VerifyItemIsAbsentAsync(markup, "C");
        }

        [Fact]
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

        [Fact]
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
            var expectedDescription = $"({FeaturesResources.field}) int C.x\r\n\r\n{string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available)}\r\n{string.Format(FeaturesResources._0_1, "Proj2", FeaturesResources.Not_Available)}\r\n\r\n{FeaturesResources.You_can_use_the_navigation_bar_to_switch_contexts}";

            await VerifyItemInLinkedFilesAsync(markup, "x", expectedDescription);
        }

        [Fact]
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
            var expectedDescription = $"({FeaturesResources.field}) int C.x\r\n\r\n{string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available)}\r\n{string.Format(FeaturesResources._0_1, "Proj2", FeaturesResources.Not_Available)}\r\n{string.Format(FeaturesResources._0_1, "Proj3", FeaturesResources.Not_Available)}\r\n\r\n{FeaturesResources.You_can_use_the_navigation_bar_to_switch_contexts}";

            await VerifyItemInLinkedFilesAsync(markup, "x", expectedDescription);
        }

        [Fact]
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
            var expectedDescription = $"({FeaturesResources.field}) int C.x\r\n\r\n{string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available)}\r\n{string.Format(FeaturesResources._0_1, "Proj3", FeaturesResources.Not_Available)}\r\n\r\n{FeaturesResources.You_can_use_the_navigation_bar_to_switch_contexts}";

            await VerifyItemInLinkedFilesAsync(markup, "x", expectedDescription);
        }

        [Fact]
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
            var expectedDescription = $"void G.DoGStuff()\r\n\r\n{string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Not_Available)}\r\n{string.Format(FeaturesResources._0_1, "Proj2", FeaturesResources.Available)}\r\n{string.Format(FeaturesResources._0_1, "Proj3", FeaturesResources.Not_Available)}\r\n\r\n{FeaturesResources.You_can_use_the_navigation_bar_to_switch_contexts}";

            await VerifyItemInLinkedFilesAsync(markup, "DoGStuff", expectedDescription);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1020944")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1020944")]
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
            var expectedDescription = $"({FeaturesResources.local_variable}) int xyz\r\n\r\n{string.Format(FeaturesResources._0_1, "Proj1", FeaturesResources.Available)}\r\n{string.Format(FeaturesResources._0_1, "Proj2", FeaturesResources.Not_Available)}\r\n\r\n{FeaturesResources.You_can_use_the_navigation_bar_to_switch_contexts}";
            await VerifyItemInLinkedFilesAsync(markup, "xyz", expectedDescription);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1020944")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1020944")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1063403")]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

            var expectedDescription = $"({FeaturesResources.field}) int C.x";
            await VerifyItemInLinkedFilesAsync(markup, "x", expectedDescription);
        }

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/54361")]
        public async Task ConditionalAccessNullableIsUnwrappedOnParameter()
        {
            var markup = @"
class A
{
    void M(System.DateTime? dt)
    {
        dt?.$$
    }
}
";
            await VerifyItemExistsAsync(markup, "Day");
            await VerifyItemIsAbsentAsync(markup, "Value");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/54361")]
        public async Task NullableIsNotUnwrappedOnParameter()
        {
            var markup = @"
class A
{
    void M(System.DateTime? dt)
    {
        dt.$$
    }
}
";
            await VerifyItemExistsAsync(markup, "Value");
            await VerifyItemIsAbsentAsync(markup, "Day");
        }

        [Fact]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1109319")]
        public async Task WithinChainOfConditionalAccesses1()
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1109319")]
        public async Task WithinChainOfConditionalAccesses2()
        {
            var markup = @"
class Program
{
    static void Main(string[] args)
    {
        A a;
        var x = a?.b?.$$c?.d.e;
    }
}

class A { public B b; }
class B { public C c; }
class C { public D d; }
class D { public int e; }";
            await VerifyItemExistsAsync(markup, "c");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1109319")]
        public async Task WithinChainOfConditionalAccesses3()
        {
            var markup = @"
class Program
{
    static void Main(string[] args)
    {
        A a;
        var x = a?.b?.c?.$$d.e;
    }
}

class A { public B b; }
class B { public C c; }
class C { public D d; }
class D { public int e; }";
            await VerifyItemExistsAsync(markup, "d");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/843466")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/843466")]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/983367")]
        public async Task NoTypeParametersDefinedInCrefs()
        {
            var markup = @"using System;

/// <see cref=""Program{T$$}""/>
class Program<T> { }";
            await VerifyItemIsAbsentAsync(markup, "T");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/988025")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/988025")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/991466")]
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

        [Fact]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/997410")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/997410")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/883293")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1024380")]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1663")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1029522")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1029522")]
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1029522")]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064811")]
        public async Task NotBeforeFirstStringHole()
        {
            await VerifyNoItemsExistAsync(AddInsideMethod(
@"var x = ""\{0}$$\{1}\{2}"""));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064811")]
        public async Task NotBetweenStringHoles()
        {
            await VerifyNoItemsExistAsync(AddInsideMethod(
@"var x = ""\{0}\{1}$$\{2}"""));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064811")]
        public async Task NotAfterStringHoles()
        {
            await VerifyNoItemsExistAsync(AddInsideMethod(
@"var x = ""\{0}\{1}\{2}$$"""));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1087171")]
        public async Task CompletionAfterTypeOfGetType()
        {
            await VerifyItemExistsAsync(AddInsideMethod(
"typeof(int).GetType().$$"), "GUID");
        }

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [WorkItem("https://github.com/dotnet/roslyn/issues/7932")]
        [WpfFact]
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

        [WorkItem("https://github.com/dotnet/roslyn/issues/7932")]
        [WpfFact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25084")]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25084")]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/717")]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1277")]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1277")]
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

        [Fact]
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

        [Fact]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/5069")]
        public async Task InstanceMembersInTopLevelFieldInitializer()
        {
            var markup = @"
int aaa = 1;
int bbb = $$
";
            await VerifyItemExistsAsync(markup, "aaa", sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/5069")]
        public async Task InstanceMembersInTopLevelFieldLikeEventInitializer()
        {
            var markup = @"
Action aaa = null;
event Action bbb = $$
";
            await VerifyItemExistsAsync(markup, "aaa", sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33")]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33")]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33")]
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

        [Fact]
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

        [Fact]
        public async Task NoCompletionInShebangComments()
        {
            await VerifyNoItemsExistAsync("#!$$", sourceCodeKind: SourceCodeKind.Script);
            await VerifyNoItemsExistAsync("#! S$$", sourceCodeKind: SourceCodeKind.Script, usePreviousCharAsTrigger: true);
        }

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14546")]
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
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/13480")]
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
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/13480")]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14127")]
        public async Task TupleTypeAtMemberLevel1()
        {
            await VerifyItemExistsAsync(@"
class C
{
    ($$
}", "C");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14127")]
        public async Task TupleTypeAtMemberLevel2()
        {
            await VerifyItemExistsAsync(@"
class C
{
    ($$)
}", "C");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14127")]
        public async Task TupleTypeAtMemberLevel3()
        {
            await VerifyItemExistsAsync(@"
class C
{
    (C, $$
}", "C");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14127")]
        public async Task TupleTypeAtMemberLevel4()
        {
            await VerifyItemExistsAsync(@"
class C
{
    (C, $$)
}", "C");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14127")]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14127")]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14127")]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14163")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14163")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18359")]
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/8321")]
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/8321")]
        public async Task NotOnMethodGroup2()
        {
            var markup =
@"class C {
    void M<T>() {M<C>.$$ }
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/8321")]
        public async Task NotOnMethodGroup3()
        {
            var markup =
@"class C {
    void M() {M.$$}
}
";
            await VerifyNoItemsExistAsync(markup);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/21766"), Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem("https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=420697&_a=edit")]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/35100")]
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

        [Fact]
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

        [Fact]
        public async Task CompletionForLambdaWithOverloads()
        {
            var markup = @"
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ClassLibrary1
{
    class SomeItem
    {
        public string A;
        public int B;
    }
    class SomeCollection<T> : List<T>
    {
        public virtual SomeCollection<T> Include(string path) => null;
    }

    static class Extensions
    {
        public static IList<T> Include<T, TProperty>(this IList<T> source, Expression<Func<T, TProperty>> path)
            => null;

        public static IList Include(this IList source, string path) => null;

        public static IList<T> Include<T>(this IList<T> source, string path) => null;
    }

    class Program 
    {
        void M(SomeCollection<SomeItem> c)
        {
            var a = from m in c.Include(t => t.$$);
        }
    }
}";

            await VerifyItemIsAbsentAsync(markup, "Substring");
            await VerifyItemExistsAsync(markup, "A");
            await VerifyItemExistsAsync(markup, "B");
        }

        [Fact, WorkItem("https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1056325")]
        public async Task CompletionForLambdaWithOverloads2()
        {
            var markup = @"
using System;

class C
{
    void M(Action<int> a) { }
    void M(string s) { }

    void Test()
    {
        M(p => p.$$);
    }
}";

            await VerifyItemIsAbsentAsync(markup, "Substring");
            await VerifyItemExistsAsync(markup, "GetTypeCode");
        }

        [Fact, WorkItem("https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1056325")]
        public async Task CompletionForLambdaWithOverloads3()
        {
            var markup = @"
using System;

class C
{
    void M(Action<int> a) { }
    void M(Action<string> a) { }

    void Test()
    {
        M((int p) => p.$$);
    }
}";

            await VerifyItemIsAbsentAsync(markup, "Substring");
            await VerifyItemExistsAsync(markup, "GetTypeCode");
        }

        [Fact, WorkItem("https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1056325")]
        public async Task CompletionForLambdaWithOverloads4()
        {
            var markup = @"
using System;

class C
{
    void M(Action<int> a) { }
    void M(Action<string> a) { }

    void Test()
    {
        M(p => p.$$);
    }
}";

            await VerifyItemExistsAsync(markup, "Substring");
            await VerifyItemExistsAsync(markup, "GetTypeCode");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42997")]
        public async Task CompletionForLambdaWithTypeParameters()
        {
            var markup = @"
using System;
using System.Collections.Generic;

class Program
{
    static void M()
    {
        Create(new List<Product>(), arg => arg.$$);
    }

    static void Create<T>(List<T> list, Action<T> expression) { }
}

class Product { public void MyProperty() { } }";

            await VerifyItemExistsAsync(markup, "MyProperty");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42997")]
        public async Task CompletionForLambdaWithTypeParametersAndOverloads()
        {
            var markup = @"
using System;
using System.Collections.Generic;

class Program
{
    static void M()
    {
        Create(new Dictionary<Product1, Product2>(), arg => arg.$$);
    }

    static void Create<T, U>(Dictionary<T, U> list, Action<T> expression) { }
    static void Create<T, U>(Dictionary<U, T> list, Action<T> expression) { }
}

class Product1 { public void MyProperty1() { } }
class Product2 { public void MyProperty2() { } }";

            await VerifyItemExistsAsync(markup, "MyProperty1");
            await VerifyItemExistsAsync(markup, "MyProperty2");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42997")]
        public async Task CompletionForLambdaWithTypeParametersAndOverloads2()
        {
            var markup = @"
using System;
using System.Collections.Generic;

class Program
{
    static void M()
    {
        Create(new Dictionary<Product1,Product2>(),arg => arg.$$);
    }

    static void Create<T, U>(Dictionary<T, U> list, Action<T> expression) { }
    static void Create<T, U>(Dictionary<U, T> list, Action<T> expression) { }
    static void Create(Dictionary<Product1, Product2> list, Action<Product3> expression) { }
}

class Product1 { public void MyProperty1() { } }
class Product2 { public void MyProperty2() { } }
class Product3 { public void MyProperty3() { } }";

            await VerifyItemExistsAsync(markup, "MyProperty1");
            await VerifyItemExistsAsync(markup, "MyProperty2");
            await VerifyItemExistsAsync(markup, "MyProperty3");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42997")]
        public async Task CompletionForLambdaWithTypeParametersFromClass()
        {
            var markup = @"
using System;

class Program<T>
{
    static void M()
    {
        Create(arg => arg.$$);
    }

    static void Create(Action<T> expression) { }
}

class Product { public void MyProperty() { } }";

            await VerifyItemExistsAsync(markup, "GetHashCode");
            await VerifyItemIsAbsentAsync(markup, "MyProperty");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42997")]
        public async Task CompletionForLambdaWithTypeParametersFromClassWithConstraintOnType()
        {
            var markup = @"
using System;

class Program<T> where T : Product
{
    static void M()
    {
        Create(arg => arg.$$);
    }

    static void Create(Action<T> expression) { }
}

class Product { public void MyProperty() { } }";

            await VerifyItemExistsAsync(markup, "GetHashCode");
            await VerifyItemExistsAsync(markup, "MyProperty");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42997")]
        public async Task CompletionForLambdaWithTypeParametersFromClassWithConstraintOnMethod()
        {
            var markup = @"
using System;

class Program
{
    static void M()
    {
        Create(arg => arg.$$);
    }

    static void Create<T>(Action<T> expression) where T : Product { }
}

class Product { public void MyProperty() { } }";

            await VerifyItemExistsAsync(markup, "GetHashCode");
            await VerifyItemExistsAsync(markup, "MyProperty");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40216")]
        public async Task CompletionForLambdaPassedAsNamedArgumentAtDifferentPositionFromCorrespondingParameter1()
        {
            var markup = @"
using System;

class C
{
    void Test()
    {
        X(y: t => Console.WriteLine(t.$$));
    }

    void X(int x = 7, Action<string> y = null) { }
}
";

            await VerifyItemExistsAsync(markup, "Length");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40216")]
        public async Task CompletionForLambdaPassedAsNamedArgumentAtDifferentPositionFromCorrespondingParameter2()
        {
            var markup = @"
using System;

class C
{
    void Test()
    {
        X(y: t => Console.WriteLine(t.$$));
    }

    void X(int x, int z, Action<string> y) { }
}
";

            await VerifyItemExistsAsync(markup, "Length");
        }

        [Fact]
        public async Task CompletionForLambdaPassedAsArgumentInReducedExtensionMethod_NonInteractive()
        {
            var markup = @"
using System;

static class CExtensions
{
    public static void X(this C x, Action<string> y) { }
}

class C
{
    void Test()
    {
        new C().X(t => Console.WriteLine(t.$$));
    }
}
";
            await VerifyItemExistsAsync(markup, "Length", sourceCodeKind: SourceCodeKind.Regular);
        }

        [Fact]
        public async Task CompletionForLambdaPassedAsArgumentInReducedExtensionMethod_Interactive()
        {
            var markup = @"
using System;

public static void X(this C x, Action<string> y) { }

public class C
{
    void Test()
    {
        new C().X(t => Console.WriteLine(t.$$));
    }
}
";
            await VerifyItemExistsAsync(markup, "Length", sourceCodeKind: SourceCodeKind.Script);
        }

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36029")]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36029")]
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
            ShowTargetTypedCompletionFilter = true;

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
                matchingFilters: new List<CompletionFilter> { FilterSet.FieldFilter, FilterSet.TargetTypedFilter });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TargetTypedCompletion)]
        public async Task TestNoTargetTypeFilterWithExperimentDisabled()
        {
            ShowTargetTypedCompletionFilter = false;

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
                matchingFilters: new List<CompletionFilter> { FilterSet.FieldFilter });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TargetTypedCompletion)]
        public async Task TestTargetTypeFilter_NotOnObjectMembers()
        {
            ShowTargetTypedCompletionFilter = true;

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
                matchingFilters: new List<CompletionFilter> { FilterSet.MethodFilter });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TargetTypedCompletion)]
        public async Task TestTargetTypeFilter_NotNamedTypes()
        {
            ShowTargetTypedCompletionFilter = true;

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
                matchingFilters: new List<CompletionFilter> { FilterSet.LocalAndParameterFilter, FilterSet.TargetTypedFilter });

            await VerifyItemExistsAsync(
                markup, "C",
                matchingFilters: new List<CompletionFilter> { FilterSet.ClassFilter });
        }

        [Fact]
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38074")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
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

        [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1152109")]
        public async Task NoItemWithEmptyDisplayName()
        {
            var markup = @"
class C
{
    static void M()
    {
        int$$
    }
}
";
            await VerifyItemIsAbsentAsync(
                markup, "",
                matchingFilters: new List<CompletionFilter> { FilterSet.LocalAndParameterFilter });
        }

        [Theory]
        [InlineData('.')]
        [InlineData(';')]
        public async Task CompletionWithCustomizedCommitCharForMethod(char commitChar)
        {
            var markup = @"
class Program
{
    private void Bar()
    {
        F$$
    }
    
    private void Foo(int i)
    {
    }

    private void Foo(int i, int c)
    {
    }
}";
            var expected = $@"
class Program
{{
    private void Bar()
    {{
        Foo(){commitChar}
    }}
    
    private void Foo(int i)
    {{
    }}

    private void Foo(int i, int c)
    {{
    }}
}}";
            await VerifyProviderCommitAsync(markup, "Foo", expected, commitChar: commitChar);
        }

        [Theory]
        [InlineData('.')]
        [InlineData(';')]
        public async Task CompletionWithSemicolonInNestedMethod(char commitChar)
        {
            var markup = @"
class Program
{
    private void Bar()
    {
        Foo(F$$);
    }
    
    private int Foo(int i)
    {
        return 1;
    }
}";
            var expected = $@"
class Program
{{
    private void Bar()
    {{
        Foo(Foo(){commitChar});
    }}
    
    private int Foo(int i)
    {{
        return 1;
    }}
}}";
            await VerifyProviderCommitAsync(markup, "Foo", expected, commitChar: commitChar);
        }

        [Theory]
        [InlineData('.')]
        [InlineData(';')]
        public async Task CompletionWithCustomizedCommitCharForDelegateInferredType(char commitChar)
        {
            var markup = @"
using System;
class Program
{
    private void Bar()
    {
        Bar2(F$$);
    }
    
    private void Foo()
    {
    }

    void Bar2(Action t) { }
}";
            var expected = $@"
using System;
class Program
{{
    private void Bar()
    {{
        Bar2(Foo{commitChar});
    }}
    
    private void Foo()
    {{
    }}

    void Bar2(Action t) {{ }}
}}";
            await VerifyProviderCommitAsync(markup, "Foo", expected, commitChar: commitChar);
        }

        [Theory]
        [InlineData('.')]
        [InlineData(';')]
        public async Task CompletionWithCustomizedCommitCharForConstructor(char commitChar)
        {
            var markup = @"
class Program
{
    private static void Bar()
    {
        var o = new P$$
    }
}";
            var expected = $@"
class Program
{{
    private static void Bar()
    {{
        var o = new Program(){commitChar}
    }}
}}";
            await VerifyProviderCommitAsync(markup, "Program", expected, commitChar: commitChar);
        }

        [Theory]
        [InlineData('.')]
        [InlineData(';')]
        public async Task CompletionWithCustomizedCharForTypeUnderNonObjectCreationContext(char commitChar)
        {
            var markup = @"
class Program
{
    private static void Bar()
    {
        var o = P$$
    }
}";
            var expected = $@"
class Program
{{
    private static void Bar()
    {{
        var o = Program{commitChar}
    }}
}}";
            await VerifyProviderCommitAsync(markup, "Program", expected, commitChar: commitChar);
        }

        [Theory]
        [InlineData('.')]
        [InlineData(';')]
        public async Task CompletionWithCustomizedCommitCharForAliasConstructor(char commitChar)
        {
            var markup = @"
using String2 = System.String;
namespace Bar1
{
    class Program
    {
        private static void Bar()
        {
            var o = new S$$
        }
    }
}";
            var expected = $@"
using String2 = System.String;
namespace Bar1
{{
    class Program
    {{
        private static void Bar()
        {{
            var o = new String2(){commitChar}
        }}
    }}
}}";
            await VerifyProviderCommitAsync(markup, "String2", expected, commitChar: commitChar);
        }

        [Fact]
        public async Task CompletionWithSemicolonUnderNameofContext()
        {
            var markup = @"
namespace Bar1
{
    class Program
    {
        private static void Bar()
        {
            var o = nameof(B$$)
        }
    }
}";
            var expected = @"
namespace Bar1
{
    class Program
    {
        private static void Bar()
        {
            var o = nameof(Bar;)
        }
    }
}";
            await VerifyProviderCommitAsync(markup, "Bar", expected, commitChar: ';');
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49072")]
        public async Task EnumMemberAfterPatternMatch()
        {
            var markup =
@"namespace N
{
	enum RankedMusicians
	{
		BillyJoel,
		EveryoneElse
	}

	class C
	{
		void M(RankedMusicians m)
		{
			if (m is RankedMusicians.$$
		}
	}
}";
            // VerifyItemExistsAsync also tests with the item typed.
            await VerifyItemExistsAsync(markup, "BillyJoel");
            await VerifyItemExistsAsync(markup, "EveryoneElse");
            await VerifyItemIsAbsentAsync(markup, "Equals");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49072")]
        public async Task EnumMemberAfterPatternMatchWithDeclaration()
        {
            var markup =
@"namespace N
{
	enum RankedMusicians
	{
		BillyJoel,
		EveryoneElse
	}

	class C
	{
		void M(RankedMusicians m)
		{
			if (m is RankedMusicians.$$ r)
            {
            }
		}
	}
}";
            // VerifyItemExistsAsync also tests with the item typed.
            await VerifyItemExistsAsync(markup, "BillyJoel");
            await VerifyItemExistsAsync(markup, "EveryoneElse");
            await VerifyItemIsAbsentAsync(markup, "Equals");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49072")]
        public async Task EnumMemberAfterPropertyPatternMatch()
        {
            var markup =
@"namespace N
{
	enum RankedMusicians
	{
		BillyJoel,
		EveryoneElse
	}

	class C
	{
        public RankedMusicians R;

		void M(C m)
		{
			if (m is { R: RankedMusicians.$$
		}
	}
}";
            // VerifyItemExistsAsync also tests with the item typed.
            await VerifyItemExistsAsync(markup, "BillyJoel");
            await VerifyItemExistsAsync(markup, "EveryoneElse");
            await VerifyItemIsAbsentAsync(markup, "Equals");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49072")]
        public async Task ChildClassAfterPatternMatch()
        {
            var markup =
@"namespace N
{
	public class D { public class E { } }

	class C
	{
		void M(object m)
		{
			if (m is D.$$
		}
	}
}";
            // VerifyItemExistsAsync also tests with the item typed.
            await VerifyItemExistsAsync(markup, "E");
            await VerifyItemIsAbsentAsync(markup, "Equals");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49072")]
        public async Task EnumMemberAfterBinaryExpression()
        {
            var markup =
@"namespace N
{
	enum RankedMusicians
	{
		BillyJoel,
		EveryoneElse
	}

	class C
	{
		void M(RankedMusicians m)
		{
			if (m == RankedMusicians.$$
		}
	}
}";
            // VerifyItemExistsAsync also tests with the item typed.
            await VerifyItemExistsAsync(markup, "BillyJoel");
            await VerifyItemExistsAsync(markup, "EveryoneElse");
            await VerifyItemIsAbsentAsync(markup, "Equals");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49072")]
        public async Task EnumMemberAfterBinaryExpressionWithDeclaration()
        {
            var markup =
@"namespace N
{
	enum RankedMusicians
	{
		BillyJoel,
		EveryoneElse
	}

	class C
	{
		void M(RankedMusicians m)
		{
			if (m == RankedMusicians.$$ r)
            {
            }
		}
	}
}";
            // VerifyItemExistsAsync also tests with the item typed.
            await VerifyItemExistsAsync(markup, "BillyJoel");
            await VerifyItemExistsAsync(markup, "EveryoneElse");
            await VerifyItemIsAbsentAsync(markup, "Equals");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49609")]
        public async Task ObsoleteOverloadsAreSkippedIfNonObsoleteOverloadIsAvailable()
        {
            var markup =
@"
public class C
{
    [System.Obsolete]
    public void M() { }

    public void M(int i) { }
    
    public void Test()
    {
        this.$$
    }
}
";
            await VerifyItemExistsAsync(markup, "M", expectedDescriptionOrNull: $"void C.M(int i) (+ 1 {FeaturesResources.overload})");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49609")]
        public async Task FirstObsoleteOverloadIsUsedIfAllOverloadsAreObsolete()
        {
            var markup =
@"
public class C
{
    [System.Obsolete]
    public void M() { }

    [System.Obsolete]
    public void M(int i) { }
    
    public void Test()
    {
        this.$$
    }
}
";
            await VerifyItemExistsAsync(markup, "M", expectedDescriptionOrNull: $"[{CSharpFeaturesResources.deprecated}] void C.M() (+ 1 {FeaturesResources.overload})");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49609")]
        public async Task IgnoreCustomObsoleteAttribute()
        {
            var markup =
@"
public class ObsoleteAttribute: System.Attribute
{
}

public class C
{
    [Obsolete]
    public void M() { }

    public void M(int i) { }
    
    public void Test()
    {
        this.$$
    }
}
";
            await VerifyItemExistsAsync(markup, "M", expectedDescriptionOrNull: $"void C.M() (+ 1 {FeaturesResources.overload})");
        }

        [InlineData("int", "")]
        [InlineData("int[]", "int a")]
        [Theory, Trait(Traits.Feature, Traits.Features.TargetTypedCompletion)]
        public async Task TestTargetTypeCompletionDescription(string targetType, string expectedParameterList)
        {
            // Check the description displayed is based on symbol matches targeted type
            ShowTargetTypedCompletionFilter = true;

            var markup =
$@"public class C
{{
    bool Bar(int a, int b) => false;
    int Bar() => 0;
    int[] Bar(int a) => null;

    bool N({targetType} x) => true;

    void M(C c)
    {{
        N(c.$$);
    }}
}}";
            await VerifyItemExistsAsync(
                markup, "Bar",
                expectedDescriptionOrNull: $"{targetType} C.Bar({expectedParameterList}) (+{NonBreakingSpaceString}2{NonBreakingSpaceString}{FeaturesResources.overloads_})",
                matchingFilters: new List<CompletionFilter> { FilterSet.MethodFilter, FilterSet.TargetTypedFilter });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestTypesNotSuggestedInDeclarationDeconstruction()
        {
            await VerifyItemIsAbsentAsync(@"
class C
{
    int M()
    {
        var (x, $$) = (0, 0);
    }
}", "C");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestTypesSuggestedInMixedDeclarationAndAssignmentInDeconstruction()
        {
            await VerifyItemExistsAsync(@"
class C
{
    int M()
    {
        (x, $$) = (0, 0);
    }
}", "C");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestLocalDeclaredBeforeDeconstructionSuggestedInMixedDeclarationAndAssignmentInDeconstruction()
        {
            await VerifyItemExistsAsync(@"
class C
{
    int M()
    {
        int y;
        (var x, $$) = (0, 0);
    }
}", "y");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/53930")]
        public async Task TestTypeParameterConstraintedToInterfaceWithStatics()
        {
            var source = @"
interface I1
{
    static void M0();
    static abstract void M1();
    abstract static int P1 { get; set; }
    abstract static event System.Action E1;
}

interface I2
{
    static abstract void M2();
    static virtual void M3() { }
}

class Test
{
    void M<T>(T x) where T : I1, I2
    {
        T.$$
    }
}
";
            await VerifyItemExistsAsync(source, "M0");
            await VerifyItemExistsAsync(source, "M1");
            await VerifyItemExistsAsync(source, "M2");
            await VerifyItemExistsAsync(source, "M3");
            await VerifyItemExistsAsync(source, "P1");
            await VerifyItemExistsAsync(source, "E1");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58081")]
        public async Task CompletionOnPointerParameter()
        {
            var source = @"
struct TestStruct
{
    public int X;
    public int Y { get; }
    public void Method() { }
}

unsafe class Test
{
    void TestMethod(TestStruct* a)
    {
        a->$$
    }
}
";
            await VerifyItemExistsAsync(source, "X");
            await VerifyItemExistsAsync(source, "Y");
            await VerifyItemExistsAsync(source, "Method");
            await VerifyItemExistsAsync(source, "ToString");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58081")]
        public async Task CompletionOnAwaitedPointerParameter()
        {
            var source = @"
struct TestStruct
{
    public int X;
    public int Y { get; }
    public void Method() { }
}

unsafe class Test
{
    async void TestMethod(TestStruct* a)
    {
        await a->$$
    }
}
";
            await VerifyItemExistsAsync(source, "X");
            await VerifyItemExistsAsync(source, "Y");
            await VerifyItemExistsAsync(source, "Method");
            await VerifyItemExistsAsync(source, "ToString");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58081")]
        public async Task CompletionOnLambdaPointerParameter()
        {
            var source = @"
struct TestStruct
{
    public int X;
    public int Y { get; }
    public void Method() { }
}

unsafe class Test
{
    delegate void TestLambda(TestStruct* a);

    TestLambda TestMethod()
    {
        return a => a->$$
    }
}
";
            await VerifyItemExistsAsync(source, "X");
            await VerifyItemExistsAsync(source, "Y");
            await VerifyItemExistsAsync(source, "Method");
            await VerifyItemExistsAsync(source, "ToString");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58081")]
        public async Task CompletionOnOverloadedLambdaPointerParameter()
        {

            var source = @"
struct TestStruct1
{
    public int X;
}

struct TestStruct2
{
    public int Y;
}

unsafe class Test
{
    delegate void TestLambda1(TestStruct1* a);
    delegate void TestLambda2(TestStruct2* a);

    void Overloaded(TestLambda1 lambda)
    {
    }

    void Overloaded(TestLambda2 lambda)
    {
    }

    void TestMethod()
        => Overloaded(a => a->$$);
}
";
            await VerifyItemExistsAsync(source, "X");
            await VerifyItemExistsAsync(source, "Y");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58081")]
        public async Task CompletionOnOverloadedLambdaPointerParameterWithExplicitType()
        {

            var source = @"
struct TestStruct1
{
    public int X;
}

struct TestStruct2
{
    public int Y;
}

unsafe class Test
{
    delegate void TestLambda1(TestStruct1* a);
    delegate void TestLambda2(TestStruct2* a);

    void Overloaded(TestLambda1 lambda)
    {
    }

    void Overloaded(TestLambda2 lambda)
    {
    }

    void TestMethod()
        => Overloaded((TestStruct1* a) => a->$$);
}
";
            await VerifyItemExistsAsync(source, "X");
            await VerifyItemIsAbsentAsync(source, "Y");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58081")]
        public async Task CompletionOnPointerParameterWithSimpleMemberAccess()
        {
            var source = @"
struct TestStruct
{
    public int X;
    public int Y { get; }
    public void Method() { }
}

unsafe class Test
{
    void TestMethod(TestStruct* a)
    {
        a.$$
    }
}
";
            await VerifyItemIsAbsentAsync(source, "X");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58081")]
        public async Task CompletionOnOverloadedLambdaPointerParameterWithSimpleMemberAccess()
        {

            var source = @"
struct TestStruct1
{
    public int X;
}

struct TestStruct2
{
    public int Y;
}

unsafe class Test
{
    delegate void TestLambda1(TestStruct1* a);
    delegate void TestLambda2(TestStruct2* a);

    void Overloaded(TestLambda1 lambda)
    {
    }

    void Overloaded(TestLambda2 lambda)
    {
    }

    void TestMethod()
        => Overloaded(a => a.$$);
}
";
            await VerifyItemIsAbsentAsync(source, "X");
            await VerifyItemIsAbsentAsync(source, "Y");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58081")]
        public async Task CompletionOnOverloadedLambdaPointerParameterWithSimpleMemberAccessAndExplicitType()
        {

            var source = @"
struct TestStruct1
{
    public int X;
}

struct TestStruct2
{
    public int Y;
}

unsafe class Test
{
    delegate void TestLambda1(TestStruct1* a);
    delegate void TestLambda2(TestStruct2* a);

    void Overloaded(TestLambda1 lambda)
    {
    }

    void Overloaded(TestLambda2 lambda)
    {
    }

    void TestMethod()
        => Overloaded((TestStruct1* a) => a.$$);
}
";
            await VerifyItemIsAbsentAsync(source, "X");
            await VerifyItemIsAbsentAsync(source, "Y");
        }

        [InlineData("m.MyObject?.$$MyValue!!()")]
        [InlineData("m.MyObject?.$$MyObject!.MyValue!!()")]
        [InlineData("m.MyObject?.MyObject!.$$MyValue!!()")]
        [Theory]
        [WorkItem("https://github.com/dotnet/roslyn/issues/59714")]
        public async Task OptionalExclamationsAfterConditionalAccessShouldBeHandled(string conditionalAccessExpression)
        {
            var source = $@"
class MyClass
{{
    public MyClass? MyObject {{ get; set; }}
    public MyClass? MyValue() => null;

    public static void F()
    {{
        var m = new MyClass();
        {conditionalAccessExpression};
    }}
}}";
            await VerifyItemExistsAsync(source, "MyValue");
        }

        [Fact]
        public async Task TopLevelSymbolsAvailableAtTopLevel()
        {
            var source = $@"
int goo;

void Bar()
{{
}}

$$

class MyClass
{{
    public static void F()
    {{
    }}
}}";
            await VerifyItemExistsAsync(source, "goo");
            await VerifyItemExistsAsync(source, "Bar");
        }

        [Fact]
        public async Task TopLevelSymbolsAvailableInsideTopLevelFunction()
        {
            var source = $@"
int goo;

void Bar()
{{
    $$
}}

class MyClass
{{
    public static void F()
    {{
    }}
}}";
            await VerifyItemExistsAsync(source, "goo");
            await VerifyItemExistsAsync(source, "Bar");
        }

        [Fact]
        public async Task TopLevelSymbolsNotAvailableInOtherTypes()
        {
            var source = $@"
int goo;

void Bar()
{{
}}

class MyClass
{{
    public static void F()
    {{
        $$
    }}
}}";
            await VerifyItemIsAbsentAsync(source, "goo");
            await VerifyItemIsAbsentAsync(source, "Bar");
        }

        [Fact]
        public async Task ParameterAvailableInMethodAttributeNameof()
        {
            var source = @"
class C
{
    [Some(nameof(p$$))]
    void M(int parameter) { }
}
";
            await VerifyItemExistsAsync(MakeMarkup(source), "parameter");

            await VerifyItemExistsAsync(MakeMarkup(source, languageVersion: "10"), "parameter");
        }

        [Fact]
        public async Task ParameterNotAvailableInMethodAttributeNameofWithNoArgument()
        {
            var source = @"
class C
{
    [Some(nameof($$))]
    void M(int parameter) { }
}
";
            // Tracked by https://github.com/dotnet/roslyn/issues/60812
            await VerifyItemIsAbsentAsync(MakeMarkup(source), "parameter");
        }

        [Fact]
        public async Task ParameterAvailableInMethodParameterAttributeNameof()
        {
            var source = @"
class C
{
    void M([Some(nameof(p$$))] int parameter) { }
}
";
            await VerifyItemExistsAsync(MakeMarkup(source), "parameter");
        }

        [Fact]
        public async Task ParameterAvailableInLocalFunctionAttributeNameof()
        {
            var source = @"
class C
{
    void M()
    {
        [Some(nameof(p$$))]
        void local(int parameter) { }
    }
}
";
            await VerifyItemExistsAsync(MakeMarkup(source), "parameter");

            await VerifyItemExistsAsync(MakeMarkup(source, languageVersion: "10"), "parameter");
        }

        [Fact]
        public async Task ParameterAvailableInLocalFunctionParameterAttributeNameof()
        {
            var source = @"
class C
{
    void M()
    {
        void local([Some(nameof(p$$))] int parameter) { }
    }
}
";
            await VerifyItemExistsAsync(MakeMarkup(source), "parameter");

            await VerifyItemExistsAsync(MakeMarkup(source, languageVersion: "10"), "parameter");
        }

        [Fact]
        public async Task ParameterAvailableInLambdaAttributeNameof()
        {
            var source = @"
class C
{
    void M()
    {
        _ = [Some(nameof(p$$))] void(int parameter) => { };
    }
}
";
            await VerifyItemExistsAsync(MakeMarkup(source), "parameter");

            await VerifyItemExistsAsync(MakeMarkup(source, languageVersion: "10"), "parameter");
        }

        [Fact]
        public async Task ParameterAvailableInLambdaParameterAttributeNameof()
        {
            var source = @"
class C
{
    void M()
    {
        _ = void([Some(nameof(p$$))] int parameter) => { };
    }
}
";
            await VerifyItemExistsAsync(MakeMarkup(source), "parameter");

            await VerifyItemExistsAsync(MakeMarkup(source, languageVersion: "10"), "parameter");
        }

        [Fact]
        public async Task ParameterAvailableInDelegateAttributeNameof()
        {
            var source = @"
[Some(nameof(p$$))]
delegate void MyDelegate(int parameter);
";
            await VerifyItemExistsAsync(MakeMarkup(source), "parameter");

            await VerifyItemExistsAsync(MakeMarkup(source, languageVersion: "10"), "parameter");
        }

        [Fact]
        public async Task ParameterAvailableInDelegateParameterAttributeNameof()
        {
            var source = @"
delegate void MyDelegate([Some(nameof(p$$))] int parameter);
";
            await VerifyItemExistsAsync(MakeMarkup(source), "parameter");

            await VerifyItemExistsAsync(MakeMarkup(source, languageVersion: "10"), "parameter");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64585")]
        public async Task AfterRequired()
        {
            var source = @"
class C
{
    required $$
}";
            await VerifyAnyItemExistsAsync(source);
        }

        [Fact]
        public async Task AfterScopedInsideMethod()
        {
            var source = @"
class C
{
    void M()
    {
        scoped $$
    }
}

ref struct MyRefStruct { }
";
            await VerifyItemExistsAsync(MakeMarkup(source), "MyRefStruct");
        }

        [Fact]
        public async Task AfterScopedGlobalStatement_FollowedByType()
        {
            var source = @"
scoped $$

ref struct MyRefStruct { }
";
            await VerifyItemExistsAsync(MakeMarkup(source), "MyRefStruct");
        }

        [Fact]
        public async Task AfterScopedGlobalStatement_NotFollowedByType()
        {
            var source = """
                using System;

                scoped $$
                """;

            await VerifyItemExistsAsync(MakeMarkup(source), "ReadOnlySpan", displayTextSuffix: "<>");
        }

        [Fact]
        public async Task AfterScopedInParameter()
        {
            var source = @"
class C
{
    void M(scoped $$)
    {
    }
}

ref struct MyRefStruct { }
";
            await VerifyItemExistsAsync(MakeMarkup(source), "MyRefStruct");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65020")]
        public async Task DoNotProvideMemberOnSystemVoid()
        {
            var source = @"
class C
{
    void M1(){}
    void M2()
    {
        this.M1().$$
    }
}

public static class Extension
{
    public static bool ExtMethod(this object x) => false;
}
";
            await VerifyItemIsAbsentAsync(MakeMarkup(source), "ExtMethod");
        }

        [Theory, MemberData(nameof(ValidEnumUnderlyingTypeNames))]
        public async Task EnumBaseList1(string underlyingType)
        {
            var source = "enum E : $$";

            await VerifyItemExistsAsync(source, "System");

            // Not accessible in the given context
            await VerifyItemIsAbsentAsync(source, underlyingType);
        }

        [Theory, MemberData(nameof(ValidEnumUnderlyingTypeNames))]
        public async Task EnumBaseList2(string underlyingType)
        {
            var source = """
                enum E : $$

                class System
                {
                }
                """;

            // class `System` shadows the namespace in regular source
            await VerifyItemIsAbsentAsync(source, "System", sourceCodeKind: SourceCodeKind.Regular);

            // Not accessible in the given context
            await VerifyItemIsAbsentAsync(source, underlyingType);
        }

        [Theory, MemberData(nameof(ValidEnumUnderlyingTypeNames))]
        public async Task EnumBaseList3(string underlyingType)
        {
            var source = """
                using System;

                enum E : $$
                """;

            await VerifyItemExistsAsync(source, "System");
            await VerifyItemExistsAsync(source, underlyingType);

            // Verify that other things from `System` namespace are not present
            await VerifyItemIsAbsentAsync(source, "Console");
            await VerifyItemIsAbsentAsync(source, "Action");
            await VerifyItemIsAbsentAsync(source, "DateTime");
        }

        [Theory, MemberData(nameof(ValidEnumUnderlyingTypeNames))]
        public async Task EnumBaseList4(string underlyingType)
        {
            var source = """
                namespace MyNamespace
                {
                }

                enum E : global::$$
                """;

            await VerifyItemIsAbsentAsync(source, "E");

            await VerifyItemExistsAsync(source, "System");
            await VerifyItemIsAbsentAsync(source, "MyNamespace");

            // Not accessible in the given context
            await VerifyItemIsAbsentAsync(source, underlyingType);
        }

        [Theory, MemberData(nameof(ValidEnumUnderlyingTypeNames))]
        public async Task EnumBaseList5(string underlyingType)
        {
            var source = "enum E : System.$$";

            await VerifyItemIsAbsentAsync(source, "System");

            await VerifyItemExistsAsync(source, underlyingType);

            // Verify that other things from `System` namespace are not present
            await VerifyItemIsAbsentAsync(source, "Console");
            await VerifyItemIsAbsentAsync(source, "Action");
            await VerifyItemIsAbsentAsync(source, "DateTime");
        }

        [Theory, MemberData(nameof(ValidEnumUnderlyingTypeNames))]
        public async Task EnumBaseList6(string underlyingType)
        {
            var source = "enum E : global::System.$$";

            await VerifyItemIsAbsentAsync(source, "System");

            await VerifyItemExistsAsync(source, underlyingType);

            // Verify that other things from `System` namespace are not present
            await VerifyItemIsAbsentAsync(source, "Console");
            await VerifyItemIsAbsentAsync(source, "Action");
            await VerifyItemIsAbsentAsync(source, "DateTime");
        }

        [Fact]
        public async Task EnumBaseList7()
        {
            var source = "enum E : System.Collections.Generic.$$";

            await VerifyNoItemsExistAsync(source);
        }

        [Fact]
        public async Task EnumBaseList8()
        {
            var source = """
                namespace MyNamespace
                {
                    namespace System {}
                    public struct Byte {}
                    public struct SByte {}
                    public struct Int16 {}
                    public struct UInt16 {}
                    public struct Int32 {}
                    public struct UInt32 {}
                    public struct Int64 {}
                    public struct UInt64 {}
                }

                enum E : MyNamespace.$$
                """;

            await VerifyNoItemsExistAsync(source);
        }

        [Fact]
        public async Task EnumBaseList9()
        {
            var source = """
                using MySystem = System;

                enum E : $$
                """;

            await VerifyItemExistsAsync(source, "MySystem");
        }

        [Fact]
        public async Task EnumBaseList10()
        {
            var source = """
                using MySystem = System;

                enum E : global::$$
                """;

            await VerifyItemIsAbsentAsync(source, "MySystem");
        }

        [Theory, MemberData(nameof(ValidEnumUnderlyingTypeNames))]
        public async Task EnumBaseList11(string underlyingType)
        {
            var source = """
                using MySystem = System;

                enum E : MySystem.$$
                """;

            await VerifyItemIsAbsentAsync(source, "System");
            await VerifyItemIsAbsentAsync(source, "MySystem");

            await VerifyItemExistsAsync(source, underlyingType);

            // Verify that other things from `System` namespace are not present
            await VerifyItemIsAbsentAsync(source, "Console");
            await VerifyItemIsAbsentAsync(source, "Action");
            await VerifyItemIsAbsentAsync(source, "DateTime");
        }

        [Fact]
        public async Task EnumBaseList12()
        {
            var source = """
                using MySystem = System;

                enum E : global::MySystem.$$
                """;

            await VerifyNoItemsExistAsync(source);
        }

        [Theory, MemberData(nameof(ValidEnumUnderlyingTypeNames))]
        public async Task EnumBaseList13(string underlyingType)
        {
            var source = $"""
                using My{underlyingType} = System.{underlyingType};

                enum E : $$
                """;

            await VerifyItemExistsAsync(source, $"My{underlyingType}");
        }

        [Theory, MemberData(nameof(ValidEnumUnderlyingTypeNames))]
        public async Task EnumBaseList14(string underlyingType)
        {
            var source = $"""
                using My{underlyingType} = System.{underlyingType};

                enum E : global::$$
                """;

            await VerifyItemIsAbsentAsync(source, $"My{underlyingType}");
        }

        [Theory, MemberData(nameof(ValidEnumUnderlyingTypeNames))]
        public async Task EnumBaseList15(string underlyingType)
        {
            var source = $"""
                using My{underlyingType} = System.{underlyingType};

                enum E : System.$$
                """;

            await VerifyItemIsAbsentAsync(source, $"My{underlyingType}");

        }

        [Theory, MemberData(nameof(ValidEnumUnderlyingTypeNames))]
        public async Task EnumBaseList16(string underlyingType)
        {
            var source = $"""
                using MySystem = System;
                using My{underlyingType} = System.{underlyingType};

                enum E : MySystem.$$
                """;

            await VerifyItemIsAbsentAsync(source, $"My{underlyingType}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66903")]
        public async Task InRangeExpression()
        {
            var source = """
                class C
                {
                    const int Test = 1;

                    void M(string s)
                    {
                        var endIndex = 1;
                        var substr = s[1..$$];
                    }
                }
                """;

            await VerifyItemExistsAsync(source, "endIndex");
            await VerifyItemExistsAsync(source, "Test");
            await VerifyItemExistsAsync(source, "C");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66903")]
        public async Task InRangeExpression_WhitespaceAfterDotDotToken()
        {
            var source = """
                class C
                {
                    const int Test = 1;

                    void M(string s)
                    {
                        var endIndex = 1;
                        var substr = s[1.. $$];
                    }
                }
                """;

            await VerifyItemExistsAsync(source, "endIndex");
            await VerifyItemExistsAsync(source, "Test");
            await VerifyItemExistsAsync(source, "C");
        }

        private static string MakeMarkup(string source, string languageVersion = "Preview")
        {
            return $$"""
<Workspace>
    <Project Language="C#" AssemblyName="Assembly" CommonReferencesNet6="true" LanguageVersion="{{languageVersion}}">
        <Document FilePath="Test.cs">
{{source}}
        </Document>
    </Project>
</Workspace>
""";
        }

        public static IEnumerable<object[]> ValidEnumUnderlyingTypeNames()
        {
            yield return new object[] { "Byte" };
            yield return new object[] { "SByte" };
            yield return new object[] { "Int16" };
            yield return new object[] { "UInt16" };
            yield return new object[] { "Int32" };
            yield return new object[] { "UInt32" };
            yield return new object[] { "Int64" };
            yield return new object[] { "UInt64" };
        }
    }
}
