// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.IntroduceVariable;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.IntroduceVariable
{
    public class IntroduceVariableTests : AbstractCSharpCodeActionTest
    {
        protected override object CreateCodeRefactoringProvider(Workspace workspace)
        {
            return new IntroduceVariableCodeRefactoringProvider();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMethodFix1()
        {
            await TestAsync(
                @"class C { void Foo() { Bar([|1 + 1|]); Bar(1 + 1); } }",
                @"class C { void Foo() { const int {|Rename:V|} = 1 + 1; Bar(V); Bar(1 + 1); } }",
                index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMethodFix2()
        {
            await TestAsync(
                @"class C { void Foo() { Bar([|1 + 1|]); Bar(1 + 1); } }",
                @"class C { void Foo() { const int {|Rename:V|} = 1 + 1; Bar(V); Bar(V); } }",
                index: 3);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMethodFix3()
        {
            var code =
@"class C
{
    void Foo()
    {
        Bar(([|1 + 1|]));
        Bar((1 + 1));
    }
}";

            var expected =
@"class C
{
    void Foo()
    {
        const int {|Rename:V|} = 1 + 1;
        Bar(V);
        Bar((1 + 1));
    }
}";

            await TestAsync(code, expected, index: 2, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMethodFix4()
        {
            var code =
@"class C
{
    void Foo()
    {
        Bar(([|1 + 1|]));
        Bar((1 + 1));
    }
}";

            var expected =
@"class C
{
    void Foo()
    {
        const int {|Rename:V|} = 1 + 1;
        Bar(V);
        Bar(V);
    }
}";

            await TestAsync(code, expected, index: 3, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestFieldFix1()
        {
            var code =
@"class C
{
    int i = ([|1 + 1|]) + (1 + 1);
}";

            var expected =
@"class C
{
    private const int {|Rename:V|} = 1 + 1;
    int i = V + (1 + 1);
}";

            await TestAsync(code, expected, index: 0, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestFieldFix2()
        {
            var code =
@"class C
{
    int i = ([|1 + 1|]) + (1 + 1);
}";

            var expected =
@"class C
{
    private const int {|Rename:V|} = 1 + 1;
    int i = V + V;
}";

            await TestAsync(code, expected, index: 1, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestConstFieldFix1()
        {
            var code =
@"class C
{
    const int i = ([|1 + 1|]) + (1 + 1);
}";

            var expected =
@"class C
{
    private const int {|Rename:V|} = 1 + 1;
    const int i = V + (1 + 1);
}";

            await TestAsync(code, expected, index: 0, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestConstFieldFix2()
        {
            var code =
@"class C
{
    const int i = ([|1 + 1|]) + (1 + 1);
}";

            var expected =
@"class C
{
    private const int {|Rename:V|} = 1 + 1;
    const int i = V + V;
}";

            await TestAsync(code, expected, index: 1, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestConstructorFix1()
        {
            await TestAsync(
                @"class C { public C() : this([|1 + 1|], 1 + 1) { } }",
                @"class C { private const int {|Rename:V|} = 1 + 1; public C() : this(V, 1 + 1) { } }",
                index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestConstructorFix2()
        {
            await TestAsync(
                @"class C { public C() : this([|1 + 1|], 1 + 1) { } }",
                @"class C { private const int {|Rename:V|} = 1 + 1; public C() : this(V, V) { } }",
                index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestParameterFix1()
        {
            await TestAsync(
                @"class C { void Bar(int i = [|1 + 1|], int j = 1 + 1) { } }",
                @"class C { private const int {|Rename:V|} = 1 + 1; void Bar(int i = V, int j = 1 + 1) { } }",
                index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestParameterFix2()
        {
            await TestAsync(
                @"class C { void Bar(int i = [|1 + 1|], int j = 1 + 1) { } }",
                @"class C { private const int {|Rename:V|} = 1 + 1; void Bar(int i = V, int j = V) { } }",
                index: 1);
        }

        [Fact]
        public async Task TestAttributeFix1()
        {
            await TestAsync(
                @"class C { [Foo([|1 + 1|], 1 + 1)]void Bar() { } }",
                @"class C { private const int {|Rename:V|} = 1 + 1; [Foo(V, 1 + 1)]void Bar() { } }",
                index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestAttributeFix2()
        {
            await TestAsync(
                @"class C { [Foo([|1 + 1|], 1 + 1)]void Bar() { } }",
                @"class C { private const int {|Rename:V|} = 1 + 1; [Foo(V, V)]void Bar() { } }",
                index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMethodFixExistingName1()
        {
            await TestAsync(
                @"class C { void Foo() { int V = 0; Bar([|1 + 1|]); Bar(1 + 1); } }",
                @"class C { void Foo() { int V = 0; const int {|Rename:V1|} = 1 + 1; Bar(V1); Bar(1 + 1); } }",
                index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestFieldExistingName1()
        {
            var code =
@"class C
{
    int V;
    int V1;
    int i = ([|1 + 1|]) + (1 + 1);
}";

            var expected =
@"class C
{
    private const int {|Rename:V2|} = 1 + 1;
    int V;
    int V1;
    int i = V2 + (1 + 1);
}";

            await TestAsync(
                code,
                expected,
                index: 0,
                compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMethodFixComplexName1()
        {
            await TestAsync(
                @"class C { static int Baz; void Foo() { Bar([|C.Baz|]); Bar(1 + 1); } }",
                @"class C { static int Baz; void Foo() { var {|Rename:baz|} = C.Baz; Bar(baz); Bar(1 + 1); } }",
                index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMethodFixComplexName1NotVar()
        {
            await TestAsync(
                @"class C { static int Baz; void Foo() { Bar([|C.Baz|]); Bar(1 + 1); } }",
                @"class C { static int Baz; void Foo() { int {|Rename:baz|} = C.Baz; Bar(baz); Bar(1 + 1); } }",
                index: 0,
                options: new Dictionary<OptionKey, object> { { new OptionKey(CSharpCodeStyleOptions.UseVarWhenDeclaringLocals), false } });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNameConflict1()
        {
            await TestAsync(
                @"class C { public C(int V) : this([|1 + 1|]) { } }",
                @"class C { private const int {|Rename:V|} = 1 + 1; public C(int V) : this(C.V) { } }",
                index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNameConflict2()
        {
            await TestAsync(
@"using System ; class Program { private static int v = 5 ; static void Main ( string [ ] args ) { Func < int , int > d = ( x ) => { return [|x * v|] ; } ; d . Invoke ( v ) ; } } ",
@"using System ; class Program { private static int v = 5 ; static void Main ( string [ ] args ) { Func < int , int > d = ( x ) => { var {|Rename:v1|} = x * v; return v1 ; } ; d . Invoke ( v ) ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNameConflict2NotVar()
        {
            await TestAsync(
@"using System ; class Program { private static int v = 5 ; static void Main ( string [ ] args ) { Func < int , int > d = ( x ) => { return [|x * v|] ; } ; d . Invoke ( v ) ; } } ",
@"using System ; class Program { private static int v = 5 ; static void Main ( string [ ] args ) { Func < int , int > d = ( x ) => { int {|Rename:v1|} = x * v; return v1 ; } ; d . Invoke ( v ) ; } } ",
index: 0,
options: new Dictionary<OptionKey, object> { { new OptionKey(CSharpCodeStyleOptions.UseVarWhenDeclaringLocals), false } });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNameVerbatimIdentifier1()
        {
            await TestAsync(
@"static class G<T> { public class @class { } public static void Add(object t) { } } class Program { static void Main() { G<int>.Add([|new G<int>.@class()|]); } }",
@"static class G<T> { public class @class { } public static void Add(object t) { } } class Program { static void Main() { var {|Rename:@class|} = new G<int>.@class(); G<int>.Add(@class); } }",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNameVerbatimIdentifier1NoVar()
        {
            await TestAsync(
@"static class G<T> { public class @class { } public static void Add(object t) { } } class Program { static void Main() { G<int>.Add([|new G<int>.@class()|]); } }",
@"static class G<T> { public class @class { } public static void Add(object t) { } } class Program { static void Main() { G<int>.@class {|Rename:@class|} = new G<int>.@class(); G<int>.Add(@class); } }",
index: 0,
options: new Dictionary<OptionKey, object> { { new OptionKey(CSharpCodeStyleOptions.UseVarWhenDeclaringLocals), false } });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNameVerbatimIdentifier2()
        {
            await TestAsync(
@"static class G<T> { public class @class { } public static void Add(object t) { } static void Main() { G<int>.Add([|new G<int>.@class()|]); } }",
@"static class G<T> { public class @class { } public static void Add(object t) { } static void Main() { var {|Rename:class1|} = new G<int>.@class(); G<int>.Add(class1); } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNameVerbatimIdentifier2NoVar()
        {
            await TestAsync(
@"static class G<T> { public class @class { } public static void Add(object t) { } static void Main() { G<int>.Add([|new G<int>.@class()|]); } }",
@"static class G<T> { public class @class { } public static void Add(object t) { } static void Main() { G<int>.@class {|Rename:class1|} = new G<int>.@class(); G<int>.Add(class1); } }",
options: new Dictionary<OptionKey, object> { { new OptionKey(CSharpCodeStyleOptions.UseVarWhenDeclaringLocals), false } });
        }

        [WorkItem(540078, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540078")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestConstantField1()
        {
            await TestAsync(
@"class C { int [ ] array = new int [ [|10|] ] ; } ",
@"class C { private const int {|Rename:V|} = 10 ; int [ ] array = new int [ V ] ; } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        [WorkItem(540079, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540079")]
        public async Task TestFormattingOfReplacedExpression1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int i = [|1 + 2|] + 3;
    }
}",
@"class C
{
    void M()
    {
        const int {|Rename:V|} = 1 + 2;
        int i = V + 3;
    }
}",
index: 2,
compareTokens: false);
        }

        [WorkItem(540468, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540468")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestCantExtractMethodTypeParameterToField()
        {
            await TestAsync(
@"using System ; using System . Collections . Generic ; using System . Linq ; class Program { static void Main < T > ( string [ ] args ) { Foo ( [|( T ) 2 . ToString ( )|] ) ; } } ",
@"using System ; using System . Collections . Generic ; using System . Linq ; class Program { static void Main < T > ( string [ ] args ) { var {|Rename:t|} = ( T ) 2 . ToString ( ) ; Foo ( t ) ; } } ");
        }

        [WorkItem(540468, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540468")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestCantExtractMethodTypeParameterToFieldCount()
        {
            await TestActionCountAsync(
@"using System ; using System . Collections . Generic ; using System . Linq ; class Program { static void Main < T > ( string [ ] args ) { Foo ( [|( T ) 2 . ToString ( )|] ) ; } } ",
count: 2);
        }

        [WorkItem(552389, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552389")]
        [WorkItem(540482, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540482")]
        [WpfFact(Skip = "552389"), Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestConstantForFixedBufferInitializer()
        {
            await TestAsync(
@"unsafe struct S { fixed int buffer [ [|10|] ] ; } ",
@"unsafe struct S { private const int p = 10 ; fixed int buffer [ p ] ; } ",
index: 0);
        }

        [WorkItem(540486, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540486")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestFormattingOfIntroduceLocal()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int i = [|1 + 2|] + 3;
    }
}",
@"class C
{
    void M()
    {
        const int {|Rename:V|} = 1 + 2;
        int i = V + 3;
    }
}",
index: 2,
compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestLocalConstant()
        {
            await TestAsync(
@"class Program { static void Main ( string [ ] args ) { const int i = [|1|] + 1 ; } } ",
@"class Program { static void Main ( string [ ] args ) { const int {|Rename:V|} = 1 ; const int i = V + 1 ; } } ",
index: 2);
        }

        [WorkItem(542699, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542699")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestFieldConstant()
        {
            await TestAsync(
@"[ Foo ( 2 + 3 + 4 ) ] class Program { int x = [|2 + 3|] + 4 ; } internal class FooAttribute : System . Attribute { public FooAttribute ( int x ) { } } ",
@"[ Foo ( V + 4 ) ] class Program { private const int {|Rename:V|} = 2 + 3 ; int x = V + 4 ; } internal class FooAttribute : System . Attribute { public FooAttribute ( int x ) { } } ",
index: 1);
        }

        [WorkItem(542781, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542781")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMissingOnExpressionStatement()
        {
            await TestMissingAsync(
@"class Program
{
    static void Main(string[] args)
    {
        int i; [|i = 2|]; i = 3;
    }
}
");
        }

        [WorkItem(542780, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542780")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestSingleQueryClause()
        {
            await TestAsync(
@"using System . Linq ; class Program { void Main ( ) { var query = from i in new int [ ] { 1 } from j in new int [ ] { 2 } select [|i + j|] ; } } ",
@"using System . Linq ; class Program { void Main ( ) { var query = from i in new int [ ] { 1 } from j in new int [ ] { 2 } let {|Rename:v|} = i + j select v ; } } ",
index: 0);
        }

        [WorkItem(542780, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542780")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestSingleQuerySelectOrGroupByClause()
        {
            await TestAsync(
@"using System . Linq ; class Program { void Main ( ) { var query = from i in new int [ ] { 1 } from j in new int [ ] { 2 } where [|i + j|] > 5 select i + j ; } } ",
@"using System . Linq ; class Program { void Main ( ) { var query = from i in new int [ ] { 1 } from j in new int [ ] { 2 } let {|Rename:v|} = i + j where v > 5 select i + j ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestLinqQuery()
        {
            await TestAsync(
@"using System . Linq ; class Program { void Main ( ) { var query = from i in new int [ ] { 1 } from j in new int [ ] { 2 } where [|i + j|] > 5 let x = j + i select i + j ; } } ",
@"using System . Linq ; class Program { void Main ( ) { var query = from i in new int [ ] { 1 } from j in new int [ ] { 2 } let {|Rename:v|} = i + j where v > 5 let x = j + i select v ; } } ",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestSingleQueryReplaceAll()
        {
            await TestAsync(
@"using System . Linq ; class Program { void Main ( ) { var query = from i in new int [ ] { 1 } from j in new int [ ] { 2 } where i + j > 5 let x = j + i select [|i + j|] ; } } ",
@"using System . Linq ; class Program { void Main ( ) { var query = from i in new int [ ] { 1 } from j in new int [ ] { 2 } let {|Rename:v|} = i + j where v > 5 let x = j + i select v ; } } ",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNestedQueryReplaceOne1()
        {
            await TestAsync(
@"using System . Linq ; class Program { void Main ( ) { var query = from i in new int [ ] { 1 } from j in new int [ ] { 2 } where i > ( from k in new int [ ] { 3 } select [|i + j|] ) . Max ( ) where j > ( from m in new int [ ] { 4 } select i + j ) . Max ( ) let x = j + i select i + j ; } } ",
@"using System . Linq ; class Program { void Main ( ) { var query = from i in new int [ ] { 1 } from j in new int [ ] { 2 } where i > ( from k in new int [ ] { 3 } let {|Rename:v|} = i + j select v ) . Max ( ) where j > ( from m in new int [ ] { 4 } select i + j ) . Max ( ) let x = j + i select i + j ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNestedQueryReplaceAll1()
        {
            await TestAsync(
@"using System . Linq ; class Program { void Main ( ) { var query = from i in new int [ ] { 1 } from j in new int [ ] { 2 } where i > ( from k in new int [ ] { 3 } select [|i + j|] ) . Max ( ) where j > ( from m in new int [ ] { 4 } select i + j ) . Max ( ) let x = j + i select i + j ; } } ",
@"using System . Linq ; class Program { void Main ( ) { var query = from i in new int [ ] { 1 } from j in new int [ ] { 2 } let {|Rename:v|} = i + j where i > ( from k in new int [ ] { 3 } select v ) . Max ( ) where j > ( from m in new int [ ] { 4 } select v ) . Max ( ) let x = j + i select v ; } } ",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNestedQueryReplaceOne2()
        {
            await TestAsync(
@"using System . Linq ; class Program { void Main ( ) { var query = from i in new int [ ] { 1 } from j in new int [ ] { 2 } where i > ( from k in new int [ ] { 3 } select i + j ) . Max ( ) where j > ( from m in new int [ ] { 4 } select [|i + j|] ) . Max ( ) let x = j + i select i + j ; } } ",
@"using System . Linq ; class Program { void Main ( ) { var query = from i in new int [ ] { 1 } from j in new int [ ] { 2 } where i > ( from k in new int [ ] { 3 } select i + j ) . Max ( ) where j > ( from m in new int [ ] { 4 } let {|Rename:v|} = i + j select v ) . Max ( ) let x = j + i select i + j ; } } ",
index: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNestedQueryReplaceAll2()
        {
            await TestAsync(
@"using System . Linq ; class Program { void Main ( ) { var query = from i in new int [ ] { 1 } from j in new int [ ] { 2 } where i > ( from k in new int [ ] { 3 } select i + j ) . Max ( ) where j > ( from m in new int [ ] { 4 } select [|i + j|] ) . Max ( ) let x = j + i select i + j ; } } ",
@"using System . Linq ; class Program { void Main ( ) { var query = from i in new int [ ] { 1 } from j in new int [ ] { 2 } let {|Rename:v|} = i + j where i > ( from k in new int [ ] { 3 } select v ) . Max ( ) where j > ( from m in new int [ ] { 4 } select v ) . Max ( ) let x = j + i select v ; } } ",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        [WorkItem(10742, "DevDiv_Projects/Roslyn")]
        public async Task TestAnonymousTypeMemberAssignment()
        {
            await TestMissingAsync(
@"class C { void M ( ) { var a = new { [|A = 0|] } ; } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        [WorkItem(10743, "DevDiv_Projects/Roslyn")]
        public async Task TestAnonymousTypeBody()
        {
            await TestMissingAsync(
@"class C { void M ( ) { var a = new [|{ A = 0 }|] ; } } ");
        }

        [WorkItem(543477, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543477")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestImplicitlyTypedArraysUsedInCheckedExpression()
        {
            await TestAsync(
@"class Program { static void Main ( string [ ] args ) { int [ ] a = null ; int [ ] temp = checked ( [|a = new [ ] { 1 , 2 , 3 }|] ) ; } } ",
@"class Program { static void Main ( string [ ] args ) { int [ ] a = null ; var {|Rename:v|} = a = new [ ] { 1 , 2 , 3 } ; int [ ] temp = checked ( v ) ; } } ");
        }

        [WorkItem(543832, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543832")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMissingOnGenericTypeParameter()
        {
            await TestMissingAsync(
@"class C { void M() { F<[|int?|], int?>(3); } R F<T, R>(T arg1) { return default(R); } }");
        }

        [WorkItem(543941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543941")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestAnonymousType1()
        {
            await TestAsync(
@"class Program { void Main ( ) { WriteLine ( [|new { X = 1 }|] ) ; } } ",
@"class Program { void Main ( ) { var {|Rename:p|} = new { X = 1 }; WriteLine(p); } } ");
        }

        [WorkItem(544099, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544099")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMissingOnAttributeNameEquals()
        {
            await TestMissingAsync(
@"using System;
using System.Runtime.InteropServices; 

class M
{
    [DllImport(""user32.dll"", [|CharSet|] = CharSet.Auto)]
    public static extern IntPtr FindWindow(string className, string windowTitle);
}");
        }

        [WorkItem(544162, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544162")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMissingOnRightOfDot()
        {
            await TestMissingAsync(
@"using System ; using System . Runtime . InteropServices ; class M { [ DllImport ( ""user32.dll"" , CharSet = CharSet . [|Auto|] ) ] public static extern IntPtr FindWindow ( string className , string windowTitle ) ; } ");
        }

        [WorkItem(544209, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544209")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMissingOnAttributeNamedParameter()
        {
            await TestMissingAsync(
@"using System ; class TestAttribute : Attribute { public TestAttribute ( int a = 42 ) { } } [ Test ( [|a|] : 1 ) ] class Foo { } ");
        }

        [WorkItem(544264, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544264")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMissingOnVariableWrite()
        {
            await TestMissingAsync(
@"class Program { void Main ( ) { var x = new int [ 3 ] ; [|x [ 1 ]|] = 2 ; } } ");
        }

        [WorkItem(544577, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544577")]
        [WorkItem(909152, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/909152")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestExpressionTLambda()
        {
            await TestMissingAsync(
@"using System ; using System . Linq . Expressions ; class Program { static Expression < Func < int ? , char ? > > e1 = c => [|null|] ; } ");
        }

        [WorkItem(544915, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544915")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMissingOnTypeSyntax()
        {
            await TestMissingAsync(
@"using System ; class Program { void Main ( ) { int [ , ] array2Da = new [|int [ 1 , 2 ]|] { { 1 , 2 } } ; } } ");
        }

        [WorkItem(544610, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544610")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task ParenthesizeIfParseChanges()
        {
            var code = @"
class C
{
    static void M()
    {
        int x = 2;
        Bar(x < [|1|], x > (2 + 3));
    }
}";

            var expected = @"
class C
{
    static void M()
    {
        int x = 2;
        const int {|Rename:V|} = 1;
        Bar(x < V, (x > (2 + 3)));
    }
}";

            await TestAsync(code, expected, index: 2, compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMissingInPartiallyHiddenMethod()
        {
            await TestMissingAsync(
@"class Program
{
#line hidden
    void Main()
    {
#line default
        Foo([|1 + 1|]);
    }
}", parseOptions: Options.Regular);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestInVisibleMethod()
        {
            await TestAsync(
@"#line hidden
class Program
{
#line default
    void Main()
    {
        Foo([|1 + 1|]);
    }
#line hidden
}
#line default",
@"#line hidden
class Program
{
#line default
    void Main()
    {
        const int {|Rename:V|} = 1 + 1;
        Foo(V);
    }
#line hidden
}
#line default",
compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMissingInFieldInPartiallyHiddenType()
        {
            await TestMissingAsync(
@"class Program
{
    int i = [|1 + 1|];

#line hidden
}
#line default", parseOptions: Options.Regular);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMissingInAttributeInPartiallyHiddenType()
        {
            await TestMissingAsync(
@"[Foo([|1 + 1|])]
class Program
{
#line hidden
}
#line default", parseOptions: Options.Regular);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMissingInConstructorInitializerInPartiallyHiddenType()
        {
            await TestMissingAsync(
@"class Program
{
    public Program() : this([|1 + 1|])
    {
    }

#line hidden
}
#line default", parseOptions: Options.Regular);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMissingInParameterInPartiallyHiddenType()
        {
            await TestMissingAsync(
@"class Program
{
    public Program(int i = [|1 + 1|])
    {
    }

#line hidden
}
#line default", parseOptions: Options.Regular);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMissingInQueryInPartiallyHiddenType()
        {
            await TestMissingAsync(
@"
using System.Linq;

class Program
{
    public Program(string[] args)
    {
        var q = from x in args
                #line hidden
                let z = 1
                #line default
                select [|x + x|];
    }
}", parseOptions: Options.Regular);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestInVisibleQueryInHiddenType()
        {
            await TestAsync(
@"#line hidden
using System.Linq;

class Program
{
    public Program(string[] args)
    {
        var q =
#line default
            from x in args
            let z = 1
            select [|x + x|];
#line hidden
    }
}
#line default",
@"#line hidden
using System.Linq;

class Program
{
    public Program(string[] args)
    {
        var q =
#line default
            from x in args
            let z = 1
            let {|Rename:v|} = x + x
            select v;
#line hidden
    }
}
#line default",
compareTokens: false,
parseOptions: Options.Regular);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMissingOnNamespace()
        {
            await TestMissingAsync(
@"class Program { void Main ( ) { [|System|] . Console . WriteLine ( 4 ) ; } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMissingOnType()
        {
            await TestMissingAsync(
@"class Program { void Main ( ) { [|System . Console|] . WriteLine ( 4 ) ; } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMissingOnBase()
        {
            await TestMissingAsync(
@"class Program { void Main ( ) { [|base|] . ToString ( ) ; } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestVenusGeneration1()
        {
            await TestMissingAsync(
@"
class Program
{
    void Main ( )
    {
#line 1 ""foo""
        Console.WriteLine([|5|]);
#line default
#line hidden
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestVenusGeneration2()
        {
            var code =
@"
class Program
{
    void Main ( )
    {
#line 1 ""foo""
        if (true)
        {
            Console.WriteLine([|5|]);
        }
#line default
#line hidden
    }
}";

            await TestExactActionSetOfferedAsync(code, new[] { string.Format(FeaturesResources.IntroduceLocalConstantFor, "5") });

            await TestAsync(code,
@"
class Program
{
    void Main ( )
    {
#line 1 ""foo""
        if (true)
        {
            const int {|Rename:V|} = 5;
            Console.WriteLine(V);
        }
#line default
#line hidden
    }
}", compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestVenusGeneration3()
        {
            var code =
@"
class Program
{
#line 1 ""foo""
    void Main ( )
    {
        if (true)
        {
            Console.WriteLine([|5|]);
        }
    }
#line default
#line hidden
}";

            await TestExactActionSetOfferedAsync(code,
                new[] { string.Format(FeaturesResources.IntroduceLocalConstantFor, "5"), string.Format(FeaturesResources.IntroduceLocalConstantForAll, "5") });
        }

        [WorkItem(529795, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529795")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMissingOnNegatedLiteral()
        {
            await TestMissingAsync(
@"class A { void Main ( ) { long x = - [|9223372036854775808|] ; } } ");
        }

        [WorkItem(546091, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546091")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNotOnInterfaceAttribute()
        {
            await TestMissingAsync(
@"[ GuidAttribute ( [|""1A585C4D-3371-48dc-AF8A-AFFECC1B0967""|] ) ] public interface I { } ");
        }

        [WorkItem(546095, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546095")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNotOnTypeOfInAttribute()
        {
            await TestMissingAsync(
@"using System . Runtime . InteropServices ; [ ComSourceInterfaces ( [|typeof ( GuidAttribute )|] ) ] public class Button { } ");
        }

        [WorkItem(530109, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530109")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestPreferGenerateConstantField1()
        {
            await TestAsync(
@"class C { void foo ( string s = ""Hello"" ) { var s2 = [|""Hello""|] + ""World"" ; } } ",
@"class C { private const string {|Rename:V|} = ""Hello"" ; void foo ( string s = ""Hello"" ) { var s2 = V + ""World"" ; } } ");
        }

        [WorkItem(530109, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530109")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestPreferGenerateConstantField2()
        {
            await TestAsync(
@"class C { void foo ( string s = ""Hello"" ) { var s2 = [|""Hello""|] + ""World"" ; } } ",
@"class C { private const string {|Rename:V|} = ""Hello"" ; void foo ( string s = V ) { var s2 = V + ""World"" ; } } ",
index: 1);
        }

        [WorkItem(530109, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530109")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestPreferGenerateConstantField3()
        {
            await TestAsync(
@"class C { void foo ( string s = ""Hello"" ) { var s2 = [|""Hello""|] + ""World"" ; } } ",
@"class C { void foo ( string s = ""Hello"" ) { const string {|Rename:V|} = ""Hello"" ; var s2 = V + ""World"" ; } } ",
index: 2);
        }

        [WorkItem(530109, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530109")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestPreferGenerateConstantField4()
        {
            await TestAsync(
@"class C { void foo ( string s = ""Hello"" ) { var s2 = [|""Hello""|] + ""World"" ; } } ",
@"class C { void foo ( string s = ""Hello"" ) { const string {|Rename:V|} = ""Hello"" ; var s2 = V + ""World"" ; } } ",
index: 3);
        }

        [WorkItem(530109, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530109")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNoGenerateConstantFieldIfAccessingLocal1()
        {
            await TestAsync(
@"class C { void foo ( string s = ""Hello"" ) { const string s1 = ""World"" ; var s2 = [|""Hello"" + s1|] ; } } ",
@"class C { void foo ( string s = ""Hello"" ) { const string s1 = ""World"" ; const string {|Rename:V|} = ""Hello"" + s1 ; var s2 = V ; } } ",
index: 0);
        }

        [WorkItem(530109, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530109")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNoGenerateConstantFieldIfAccessingLocal2()
        {
            await TestAsync(
@"class C { void foo ( string s = ""Hello"" ) { const string s1 = ""World"" ; var s2 = [|""Hello"" + s1|] ; } } ",
@"class C { void foo ( string s = ""Hello"" ) { const string s1 = ""World"" ; const string {|Rename:V|} = ""Hello"" + s1 ; var s2 = V ; } } ",
index: 1);
        }

        [WorkItem(530109, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530109")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNoGenerateConstantFieldIfNotAccessingLocal1()
        {
            await TestAsync(
@"class C { const string s1 = ""World"" ; void foo ( string s = ""Hello"" ) { var s2 = [|""Hello"" + s1|] ; } } ",
@"class C { const string s1 = ""World"" ; private const string {|Rename:V|} = ""Hello"" + s1 ; void foo ( string s = ""Hello"" ) { var s2 = V ; } } ");
        }

        [WorkItem(530109, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530109")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNoGenerateConstantFieldIfNotAccessingLocal2()
        {
            await TestAsync(
@"class C { const string s1 = ""World"" ; void foo ( string s = ""Hello"" ) { var s2 = [|""Hello"" + s1|] ; } } ",
@"class C { const string s1 = ""World"" ; private const string {|Rename:V|} = ""Hello"" + s1 ; void foo ( string s = ""Hello"" ) { var s2 = V ; } } ",
index: 1);
        }

        [WorkItem(530109, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530109")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNoGenerateConstantFieldIfNotAccessingLocal3()
        {
            await TestAsync(
@"class C { const string s1 = ""World"" ; void foo ( string s = ""Hello"" ) { var s2 = [|""Hello"" + s1|] ; } } ",
@"class C { const string s1 = ""World"" ; void foo ( string s = ""Hello"" ) { const string {|Rename:V|} = ""Hello"" + s1 ; var s2 = V ; } } ",
index: 2);
        }

        [WorkItem(530109, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530109")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNoGenerateConstantFieldIfNotAccessingLocal4()
        {
            await TestAsync(
@"class C { const string s1 = ""World"" ; void foo ( string s = ""Hello"" ) { var s2 = [|""Hello"" + s1|] ; } } ",
@"class C { const string s1 = ""World"" ; void foo ( string s = ""Hello"" ) { const string {|Rename:V|} = ""Hello"" + s1 ; var s2 = V ; } } ",
index: 3);
        }

        [WorkItem(606347, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/606347")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task InsertNeededCast1()
        {
            await TestAsync(
@"using System;

static class C
{
    static void Inner(Action<string> x, string y) { }
    static void Inner(Action<string> x, int y) { }
    static void Inner(Action<int> x, int y) { }

    static void Outer(Action<string> x, object y) { Console.WriteLine(1); }
    static void Outer(Action<int> x, string y) { Console.WriteLine(2); }

    static T Foo<T>(T x) { return x; }

    static void Main()
    {
        Outer(y => Inner(x => { [|Foo(x)|].ToString(); }, y), null);
    }
}",

@"using System;

static class C
{
    static void Inner(Action<string> x, string y) { }
    static void Inner(Action<string> x, int y) { }
    static void Inner(Action<int> x, int y) { }

    static void Outer(Action<string> x, object y) { Console.WriteLine(1); }
    static void Outer(Action<int> x, string y) { Console.WriteLine(2); }

    static T Foo<T>(T x) { return x; }

    static void Main()
    {
        Outer(y => Inner(x => { var {|Rename:v|} = Foo(x); v.ToString(); }, y), null);
    }
}",

compareTokens: false);
        }

        [WorkItem(606347, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/606347")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task InsertNeededCast1NotVar()
        {
            await TestAsync(
@"using System;

static class C
{
    static void Inner(Action<string> x, string y) { }
    static void Inner(Action<string> x, int y) { }
    static void Inner(Action<int> x, int y) { }

    static void Outer(Action<string> x, object y) { Console.WriteLine(1); }
    static void Outer(Action<int> x, string y) { Console.WriteLine(2); }

    static T Foo<T>(T x) { return x; }

    static void Main()
    {
        Outer(y => Inner(x => { [|Foo(x)|].ToString(); }, y), null);
    }
}",

@"using System;

static class C
{
    static void Inner(Action<string> x, string y) { }
    static void Inner(Action<string> x, int y) { }
    static void Inner(Action<int> x, int y) { }

    static void Outer(Action<string> x, object y) { Console.WriteLine(1); }
    static void Outer(Action<int> x, string y) { Console.WriteLine(2); }

    static T Foo<T>(T x) { return x; }

    static void Main()
    {
        Outer(y => Inner(x => { string {|Rename:v|} = Foo(x); v.ToString(); }, y), (object)null);
    }
}",
compareTokens: false,
options: new Dictionary<OptionKey, object> { { new OptionKey(CSharpCodeStyleOptions.UseVarWhenDeclaringLocals), false } });
        }

        [WorkItem(606347, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/606347"), WorkItem(714632, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/714632")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task InsertNeededCast2()
        {
            await TestAsync(
@"using System;

class Program
{
    static void Main()
    {
        byte z = 0;
        Foo([|x => 0|], y => 0, z, z);
    }

    static void Foo<T, S>(Func<S, T> p, Func<T, S> q, T r, S s) { Console.WriteLine(1); }
    static void Foo(Func<byte, byte> p, Func<byte, byte> q, int r, int s) { Console.WriteLine(2); }
}",

@"using System;

class Program
{
    static void Main()
    {
        byte z = 0;
        Func<byte, byte> {|Rename:p|} = x => 0;
        Foo<byte, byte>(p, y => 0, z, z);
    }

    static void Foo<T, S>(Func<S, T> p, Func<T, S> q, T r, S s) { Console.WriteLine(1); }
    static void Foo(Func<byte, byte> p, Func<byte, byte> q, int r, int s) { Console.WriteLine(2); }
}",

compareTokens: false);
        }

        [WorkItem(546512, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546512")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestInSwitchSection()
        {
            await TestAsync(
@"class Program { int Main ( int i ) { switch ( 1 ) { case 0 : var f = Main ( [|1 + 1|] ) ; Console . WriteLine ( f ) ; } } } ",
@"class Program { int Main ( int i ) { switch ( 1 ) { case 0 : const int {|Rename:V|} = 1 + 1 ; var f = Main ( V ) ; Console . WriteLine ( f ) ; } } } ",
index: 2);
        }

        [WorkItem(530480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530480")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestLambdaParameter1()
        {
            await TestAsync(
@"using System ; class Program { static void Main ( string [ ] args ) { Func < int , int > f = x => [|x + 1|] ; } } ",
@"using System ; class Program { static void Main ( string [ ] args ) { Func < int , int > f = x => { var {|Rename:v|} = x + 1 ; return v; }; } } ");
        }

        [WorkItem(530480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530480")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestLambdaParameter2()
        {
            await TestAsync(
@"using System ; class Program { static void Main ( string [ ] args ) { Func < int , Func < int , int > > f = x => y => [|x + 1|] ; } } ",
@"using System ; class Program { static void Main ( string [ ] args ) { Func < int , Func < int , int > > f = x => { var {|Rename:v|} = x + 1 ; return y => v; }; } } ");
        }

        [WorkItem(530480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530480")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestLambdaParameter3()
        {
            await TestAsync(
@"using System ; class Program { static void Main ( string [ ] args ) { Func < int , Func < int , int > > f = x => y => [|y + 1|] ; } } ",
@"using System ; class Program { static void Main ( string [ ] args ) { Func < int , Func < int , int > > f = x => y =>{ var {|Rename:v|} =  y + 1 ; return v; }; } } ");
        }

        [WorkItem(530480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530480")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestLambdaParameter4()
        {
            await TestAsync(
@"using System ; class Program { static void Main ( string [ ] args ) { Func < int , Func < int , int > > f = x => [|y => y + 1|] ; } } ",
            @"using System ; class Program { static void Main ( string [ ] args ) { Func < int , int > {|Rename:p|} = y => y + 1; Func < int , Func < int , int > > f = x => p ; } } ");
        }

        [WorkItem(530480, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530480")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestLambdaParameter5()
        {
            await TestAsync(
@"using System ; class Program { static void Main ( string [ ] args ) { Func < int , Func < int , int > > f = x => [|y => x + 1|] ; } } ",
@"using System ; class Program { static void Main ( string [ ] args ) { Func < int , Func < int , int > > f = x => { Func<int,int> {|Rename:p|} = y => x + 1 ; return p; }; } } ");
        }

        [WorkItem(530721, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530721")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroVarInAction1()
        {
            await TestAsync(
@"using System ; class Program { void M ( ) { Action < int > foo = x => [|x . Foo|] ; } } ",
@"using System ; class Program { void M ( ) { Action < int > foo = x => { object {|Rename:foo1|} = x . Foo ; } ; } } ");
        }

        [WorkItem(530919, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530919")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNullableOfPointerType()
        {
            await TestAsync(
@"using System ; class Program { static void Main ( ) { [|new Nullable < int * > ( )|] . GetValueOrDefault ( ) ; } } ",
@"using System ; class Program { static void Main ( ) { var {|Rename:v|} = new Nullable < int * > ( ) ; v . GetValueOrDefault ( ) ; } } ");
        }

        [WorkItem(530919, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530919")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNullableOfPointerTypeNotVar()
        {
            await TestAsync(
@"using System ; class Program { static void Main ( ) { [|new Nullable < int * > ( )|] . GetValueOrDefault ( ) ; } } ",
@"using System ; class Program { static void Main ( ) { Nullable < int * > {|Rename:v|} = new Nullable < int * > ( ) ; v . GetValueOrDefault ( ) ; } } ",
options: new Dictionary<OptionKey, object> { { new OptionKey(CSharpCodeStyleOptions.UseVarWhenDeclaringLocals), false } });
        }

        [WorkItem(830885, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/830885")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalRemovesUnnecessaryCast()
        {
            await TestAsync(
@"using System.Collections.Generic; class C { static void Main(string[] args) { var set = new HashSet<string>(); set.Add([|set.ToString()|]); } } ",
@"using System.Collections.Generic; class C { static void Main(string[] args) { var set = new HashSet<string>(); var {|Rename:v|} = set.ToString(); set.Add(v); } } ");
        }

        [WorkItem(655498, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/655498")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task HandleParenthesizedExpression()
        {
            await TestAsync(
@"using System;

class C
{
    void Foo()
    {
        ([|(C.Bar)|].Invoke)();
    }

    static Action Bar;
}",

@"using System;

class C
{
    void Foo()
    {
        Action {|Rename:bar|} = (C.Bar);
        bar.Invoke();
    }

    static Action Bar;
}",

compareTokens: false);
        }

        [WorkItem(682683, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/682683")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task DontRemoveParenthesesIfOperatorPrecedenceWouldBeBroken()
        {
            await TestAsync(
@"using System;
 
class Program
{
    static void Main()
    {
        Console.WriteLine(5 - ([|1|] + 2));
    }
}",

@"using System;
 
class Program
{
    static void Main()
    {
        const int {|Rename:V|} = 1;
        Console.WriteLine(5 - (V + 2));
    }
}",
index: 2,
compareTokens: false);
        }

        [WorkItem(828108, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/828108")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task UseNewSemanticModelForSimplification()
        {
            await TestAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        var d = new Dictionary<string, Exception>();
        d.Add(""a"", [|new Exception()|]);
    }
}",

@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        var d = new Dictionary<string, Exception>();
        var {|Rename:exception|} = new Exception();
        d.Add(""a"", exception);
    }
}",
compareTokens: false);
        }

        [WorkItem(884961, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/884961")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestInCollectionInitializer()
        {
            await TestAsync(
@"using System;
using System.Collections.Generic;
class C
{
    void M()
    {
        var l = new List<int>() { [|Environment.TickCount|] };
    }
}",
@"using System;
using System.Collections.Generic;
class C
{
    void M()
    {
        var {|Rename:tickCount|} = Environment.TickCount;
        var l = new List<int>() { tickCount };
    }
}",
compareTokens: false);
        }

        [WorkItem(884961, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/884961")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestInCollectionInitializerNoVar()
        {
            await TestAsync(
@"using System;
using System.Collections.Generic;
class C
{
    void M()
    {
        var l = new List<int>() { [|Environment.TickCount|] };
    }
}",
@"using System;
using System.Collections.Generic;
class C
{
    void M()
    {
        int {|Rename:tickCount|} = Environment.TickCount;
        var l = new List<int>() { tickCount };
    }
}",
compareTokens: false,
options: new Dictionary<OptionKey, object> { { new OptionKey(CSharpCodeStyleOptions.UseVarWhenDeclaringLocals), false } });
        }

        [WorkItem(854662, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/854662")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestInNestedCollectionInitializers()
        {
            await TestAsync(
@"using System;
using System.Collections.Generic;
class C
{
    public Dictionary<int, int> A { get; private set; }
    static int Main(string[] args)
    {
        int a = 0;
        return new Program { A = { { [|a + 2|], 0 } } }.A.Count;
    }
}",
@"using System;
using System.Collections.Generic;
class C
{
    public Dictionary<int, int> A { get; private set; }
    static int Main(string[] args)
    {
        int a = 0;
        var {|Rename:v|} = a + 2;
        return new Program { A = { { v, 0 } } }.A.Count;
    }
}",
compareTokens: false);
        }

        [WorkItem(884961, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/884961")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestInArrayInitializer()
        {
            await TestAsync(
@"using System;
using System.Collections.Generic;
class C
{
    void M()
    {
        var a = new int[] { [|Environment.TickCount|] };
    }
}",
@"using System;
using System.Collections.Generic;
class C
{
    void M()
    {
        var {|Rename:tickCount|} = Environment.TickCount;
        var a = new int[] { tickCount };
    }
}",
compareTokens: false);
        }

        [WorkItem(884961, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/884961")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestInArrayInitializerWithoutVar()
        {
            await TestAsync(
@"using System;
using System.Collections.Generic;
class C
{
    void M()
    {
        var a = new int[] { [|Environment.TickCount|] };
    }
}",
@"using System;
using System.Collections.Generic;
class C
{
    void M()
    {
        int {|Rename:tickCount|} = Environment.TickCount;
        var a = new int[] { tickCount };
    }
}",
compareTokens: false,
options: new Dictionary<OptionKey, object> { { new OptionKey(CSharpCodeStyleOptions.UseVarWhenDeclaringLocals), false } });
        }

        [WorkItem(1022447, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1022447")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestFormattingOfIntroduceLocal2()
        {
            await TestAsync(
@"using System;
class C
{
    void M()
    {
        var s = ""Text"";
        var x = 42;
        if ([|s.Length|].CompareTo(x) > 0 &&
            s.Length.CompareTo(x) > 0)
        {
        }
    }
}",
@"using System;
class C
{
    void M()
    {
        var s = ""Text"";
        var x = 42;
        var {|Rename:length|} = s.Length;
        if (length.CompareTo(x) > 0 &&
            length.CompareTo(x) > 0)
        {
        }
    }
}",
index: 1,
compareTokens: false);
        }

        [WorkItem(939259, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939259")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalWithTriviaInMultiLineStatements()
        {
            var code =
    @"class C
{
    void Foo()
    {
        var d = [|true|] // TODO: comment
            ? 1
            : 2;
    }
}";

            var expected =
    @"class C
{
    void Foo()
    {
        const bool {|Rename:V|} = true;
        var d = V // TODO: comment
            ? 1
            : 2;
    }
}";

            await TestAsync(code, expected, index: 3, compareTokens: false);
        }

        [WorkItem(939259, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939259")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalWithTriviaInMultiLineStatements2()
        {
            var code =
    @"class C
{
    void Foo()
    {
        var d = true
            ? 1
            : [|2|]; // TODO: comment
    }
}";

            var expected =
    @"class C
{
    void Foo()
    {
        const int {|Rename:V|} = 2;
        var d = true
            ? 1
            : V; // TODO: comment
    }
}";

            await TestAsync(code, expected, index: 3, compareTokens: false);
        }

        [WorkItem(1064803, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064803")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInStringInterpolation()
        {
            var code =
    @"class C
{
    void Foo()
    {
        var s = $""Alpha Beta { [|int.Parse(""12345"")|] } Gamma"";
    }
}";

            var expected =
    @"class C
{
    void Foo()
    {
        var {|Rename:v|} = int.Parse(""12345"");
        var s = $""Alpha Beta { v } Gamma"";
    }
}";

            await TestAsync(code, expected, index: 0, compareTokens: false);
        }

        [WorkItem(1037057, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1037057")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalWithBlankLine()
        {
            await TestAsync(@"
class C
{
    void M()
    {
        int x = 5;

        // comment
        int y = [|(x + 5)|] * (x + 5);
    }
}
", @"
class C
{
    void M()
    {
        int x = 5;

        // comment
        var {|Rename:v|} = (x + 5);
        int y = v * (x + 5);
    }
}
", index: 0, compareTokens: false);
        }

        [WorkItem(1065661, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1065661")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceVariableTextDoesntSpanLines()
        {
            await TestSmartTagTextAsync(@"
class C
{
    void M()
    {
        var s = [|@""a

b
c""|];
    }
}",
string.Format(FeaturesResources.IntroduceLocalConstantFor, @"@""a b c"""),
index: 2);
        }

        [WorkItem(1097147, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1097147")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestSmartNameForNullablesInConditionalAccessExpressionContext()
        {
            var code =
    @"using System;
class C
{
    static void Foo(string s)
    {
        var l = [|s?.Length|] ?? 0;
    }
}";

            var expected =
    @"using System;
class C
{
    static void Foo(string s)
    {
        var {|Rename:length|} = s?.Length;
        var l = length ?? 0;
    }
}";

            await TestAsync(code, expected, index: 0, compareTokens: false);
        }

        [WorkItem(1097147, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1097147")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestSmartNameForNullablesInConditionalAccessExpressionContext2()
        {
            var code =
    @"using System;
class C
{
    static void Foo(string s)
    {
        var l = [|s?.ToLower()|] ?? string.Empty;
    }
}";

            var expected =
    @"using System;
class C
{
    static void Foo(string s)
    {
        var {|Rename:v|} = s?.ToLower();
        var l = v ?? string.Empty;
    }
}";

            await TestAsync(code, expected, index: 0, compareTokens: false);
        }

        [WorkItem(1097147, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1097147")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestSmartNameForNullablesInConditionalAccessExpressionContext3()
        {
            var code =
    @"using System;
class Program
{
    static void Main(string[] args)
    {
        A a = new A();
        var l = [|a?.Prop?.Length|] ?? 0;
    }
}
class A
{
    public B Prop { get; set; }
}
class B
{
    public int Length { get; set; }
}";

            var expected =
    @"using System;
class Program
{
    static void Main(string[] args)
    {
        A a = new A();
        var {|Rename:length|} = a?.Prop?.Length;
        var l = length ?? 0;
    }
}
class A
{
    public B Prop { get; set; }
}
class B
{
    public int Length { get; set; }
}";

            await TestAsync(code, expected, index: 0, compareTokens: false);
        }

        [WorkItem(1097147, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1097147")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestSmartNameForNullablesInConditionalAccessExpressionContext4()
        {
            var code =
    @"using System;
class Program
{
    static void Main(string[] args)
    {
        A a = new A();
        var age = [|a?.Prop?.GetAge()|] ?? 0;
    }
}
class A
{
    public B Prop { get; set; }
}
class B
{
    int age;
    public int GetAge() { return age; }
}";

            var expected =
    @"using System;
class Program
{
    static void Main(string[] args)
    {
        A a = new A();
        var {|Rename:v|} = a?.Prop?.GetAge();
        var age = v ?? 0;
    }
}
class A
{
    public B Prop { get; set; }
}
class B
{
    int age;
    public int GetAge() { return age; }
}";

            await TestAsync(code, expected, index: 0, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInExpressionBodiedMethod()
        {
            var code =
    @"using System;
class T
{
    int m;
    int M1() => [|1|] + 2 + 3 + m;
}";

            var expected =
    @"using System;
class T
{
    int m;
    int M1()
    {
        const int {|Rename:V|} = 1;
        return V + 2 + 3 + m;
    }
}";

            await TestAsync(code, expected, index: 2, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceFieldInExpressionBodiedMethod()
        {
            var code =
    @"using System;
class T
{
    int m;
    int M1() => [|1|] + 2 + 3 + m;
}";

            var expected =
    @"using System;
class T
{
    private const int {|Rename:V|} = 1;
    int m;
    int M1() => V + 2 + 3 + m;
}";

            await TestAsync(code, expected, index: 0, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInExpressionBodiedOperator()
        {
            var code =
    @"using System;
class Complex
{
    int real; int imaginary;
    public static Complex operator +(Complex a, Complex b) => a.Add([|b.real + 1|]);

    private Complex Add(int b)
    {
        throw new NotImplementedException();
    }
}";

            var expected =
    @"using System;
class Complex
{
    int real; int imaginary;
    public static Complex operator +(Complex a, Complex b)
    {
        var {|Rename:v|} = b.real + 1;
        return a.Add(v);
    }

    private Complex Add(int b)
    {
        throw new NotImplementedException();
    }
}";

            await TestAsync(code, expected, index: 0, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceFieldInExpressionBodiedOperator()
        {
            var code =
    @"using System;
class Complex
{
    int real; int imaginary;
    public static Complex operator +(Complex a, Complex b) => a.Add(b.real + [|1|]);

    private Complex Add(int b)
    {
        throw new NotImplementedException();
    }
}";

            var expected =
    @"using System;
class Complex
{
    private const int {|Rename:V|} = 1;
    int real; int imaginary;
    public static Complex operator +(Complex a, Complex b) => a.Add(b.real + V);

    private Complex Add(int b)
    {
        throw new NotImplementedException();
    }
}";

            await TestAsync(code, expected, index: 0, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceFieldInExpressionBodiedConversionOperator()
        {
            var code =
    @"using System;
public struct DBBool
{
    public static readonly DBBool dbFalse = new DBBool(-1);
    int value;

    DBBool(int value)
    {
        this.value = value;
    }

    public static implicit operator DBBool(bool x) => x ? new DBBool([|1|]) : dbFalse;
}";

            var expected =
    @"using System;
public struct DBBool
{
    private const int {|Rename:V|} = 1;
    public static readonly DBBool dbFalse = new DBBool(-1);
    int value;

    DBBool(int value)
    {
        this.value = value;
    }

    public static implicit operator DBBool(bool x) => x ? new DBBool(V) : dbFalse;
}";

            await TestAsync(code, expected, index: 0, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceFieldInExpressionBodiedProperty()
        {
            var code =
    @"using System;
class T
{
    int M1 => [|1|] + 2;
}";

            var expected =
    @"using System;
class T
{
    private const int {|Rename:V|} = 1;

    int M1 => V + 2;
}";

            await TestAsync(code, expected, index: 0, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInExpressionBodiedProperty()
        {
            var code =
    @"using System;
class T
{
    int M1 => [|1|] + 2;
}";

            var expected =
    @"using System;
class T
{
    int M1
    {
        get
        {
            const int {|Rename:V|} = 1;
            return V + 2;
        }
    }
}";

            await TestAsync(code, expected, index: 2, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceFieldInExpressionBodiedIndexer()
        {
            var code =
    @"using System;
class SampleCollection<T>
{
    private T[] arr = new T[100];
    public T this[int i] => i > [|0|] ? arr[i + 1] : arr[i + 2];
}";

            var expected =
    @"using System;
class SampleCollection<T>
{
    private const int {|Rename:V|} = 0;
    private T[] arr = new T[100];
    public T this[int i] => i > V ? arr[i + 1] : arr[i + 2];
}";

            await TestAsync(code, expected, index: 0, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInExpressionBodiedIndexer()
        {
            var code =
    @"using System;
class SampleCollection<T>
{
    private T[] arr = new T[100];
    public T this[int i] => i > 0 ? arr[[|i + 1|]] : arr[i + 2];
}";

            var expected =
    @"using System;
class SampleCollection<T>
{
    private T[] arr = new T[100];
    public T this[int i]
    {
        get
        {
            var {|Rename:v|} = i + 1;
            return i > 0 ? arr[v] : arr[i + 2];
        }
    }
}";

            await TestAsync(code, expected, index: 0, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestTrailingTriviaOnExpressionBodiedMethodRewrites()
        {
            var code =
    @"using System;
class T
{
    int M1() => 1 + 2 + [|3|] /*not moved*/; /*moved to end of block*/

    // rewrite should preserve newline above this.
    void Cat() { }
}";

            var expected =
    @"using System;
class T
{
    int M1()
    {
        const int {|Rename:V|} = 3;
        return 1 + 2 + V /*not moved*/;
    } /*moved to end of block*/

    // rewrite should preserve newline above this.
    void Cat() { }
}";

            await TestAsync(code, expected, index: 2, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestLeadingTriviaOnExpressionBodiedMethodRewrites()
        {
            var code =
    @"using System;
class T
{
    /*not moved*/
    int M1() => 1 + 2 + /*not moved*/ [|3|];
}";

            var expected =
    @"using System;
class T
{
    /*not moved*/
    int M1()
    {
        const int {|Rename:V|} = 3;
        return 1 + 2 + /*not moved*/ V;
    }
}";

            await TestAsync(code, expected, index: 2, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestTriviaAroundArrowTokenInExpressionBodiedMemberSyntax()
        {
            var code =
    @"using System;
class T
{
    // comment
    int M1() /*c1*/ => /*c2*/ 1 + 2 + /*c3*/ [|3|];
}";

            var expected =
    @"using System;
class T
{
    // comment
    int M1() /*c1*/  /*c2*/
    {
        const int {|Rename:V|} = 3;
        return 1 + 2 + /*c3*/ V;
    }
}";

            await TestAsync(code, expected, index: 2, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [WorkItem(971, "http://github.com/dotnet/roslyn/issues/971")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInExpressionBodiedMethodWithBlockBodiedAnonymousMethodExpression()
        {
            var code =
    @"using System;
class TestClass
{
    Func<int, int> Y() => delegate (int x)
    {
        return [|9|];
    };
}";

            var expected =
    @"using System;
class TestClass
{
    Func<int, int> Y() => delegate (int x)
    {
        const int {|Rename:V|} = 9;
        return V;
    };
}";

            await TestAsync(code, expected, index: 2, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [WorkItem(971, "http://github.com/dotnet/roslyn/issues/971")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInExpressionBodiedMethodWithSingleLineBlockBodiedAnonymousMethodExpression()
        {
            var code =
    @"using System;
class TestClass
{
    Func<int, int> Y() => delegate (int x) { return [|9|]; };
}";

            var expected =
    @"using System;
class TestClass
{
    Func<int, int> Y() => delegate (int x) { const int {|Rename:V|} = 9; return V; };
}";

            await TestAsync(code, expected, index: 2, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [WorkItem(971, "http://github.com/dotnet/roslyn/issues/971")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInExpressionBodiedMethodWithBlockBodiedSimpleLambdaExpression()
        {
            var code =
    @"using System;
class TestClass
{
    Func<int, int> Y() => f =>
    {
        return f * [|9|];
    };
}";

            var expected =
    @"using System;
class TestClass
{
    Func<int, int> Y() => f =>
    {
        const int {|Rename:V|} = 9;
        return f * V;
    };
}";

            await TestAsync(code, expected, index: 2, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInExpressionBodiedMethodWithExpressionBodiedSimpleLambdaExpression()
        {
            var code =
    @"using System;
class TestClass
{
    Func<int, int> Y() => f => f * [|9|];
}";

            var expected =
    @"using System;
class TestClass
{
    Func<int, int> Y()
    {
        const int {|Rename:V|} = 9;
        return f => f * V;
    }
}";

            await TestAsync(code, expected, index: 2, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [WorkItem(971, "http://github.com/dotnet/roslyn/issues/971")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInExpressionBodiedMethodWithBlockBodiedParenthesizedLambdaExpression()
        {
            var code =
    @"using System;
class TestClass
{
    Func<int, int> Y() => (f) =>
    {
        return f * [|9|];
    };
}";

            var expected =
    @"using System;
class TestClass
{
    Func<int, int> Y() => (f) =>
    {
        const int {|Rename:V|} = 9;
        return f * V;
    };
}";

            await TestAsync(code, expected, index: 2, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInExpressionBodiedMethodWithExpressionBodiedParenthesizedLambdaExpression()
        {
            var code =
    @"using System;
class TestClass
{
    Func<int, int> Y() => (f) => f * [|9|];
}";

            var expected =
    @"using System;
class TestClass
{
    Func<int, int> Y()
    {
        const int {|Rename:V|} = 9;
        return (f) => f * V;
    }
}";

            await TestAsync(code, expected, index: 2, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [WorkItem(971, "http://github.com/dotnet/roslyn/issues/971")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestIntroduceLocalInExpressionBodiedMethodWithBlockBodiedAnonymousMethodExpressionInMethodArgs()
        {
            var code =
    @"using System;
class TestClass
{
    public int Prop => Method1(delegate()
    {
        return [|8|];
    });
}";

            var expected =
    @"using System;
class TestClass
{
    public int Prop => Method1(delegate()
    {
        const int {|Rename:V|} = 8;
        return V;
    });
}";

            await TestAsync(code, expected, index: 2, compareTokens: false);
        }

        [WorkItem(976, "https://github.com/dotnet/roslyn/issues/976")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNoConstantForInterpolatedStrings1()
        {
            var code =
    @"using System;
class TestClass
{
    static void Test(string[] args)
    {
        Console.WriteLine([|$""{DateTime.Now.ToString()}Text{args[0]}""|]);
    }
}";

            var expected =
    @"using System;
class TestClass
{
    static void Test(string[] args)
    {
        var {|Rename:v|} = $""{DateTime.Now.ToString()}Text{args[0]}"";
        Console.WriteLine(v);
    }
}";

            await TestAsync(code, expected, index: 0, compareTokens: false);
        }

        [WorkItem(976, "https://github.com/dotnet/roslyn/issues/976")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestNoConstantForInterpolatedStrings2()
        {
            var code =
    @"using System;
class TestClass
{
    static void Test(string[] args)
    {
        Console.WriteLine([|$""Text{{s}}""|]);
        Console.WriteLine($""Text{{s}}"");
    }
}";

            var expected =
    @"using System;
class TestClass
{
    static void Test(string[] args)
    {
        var {|Rename:v|} = $""Text{{s}}"";
        Console.WriteLine(v);
        Console.WriteLine(v);
    }
}";

            await TestAsync(code, expected, index: 1, compareTokens: false);
        }

        [WorkItem(909152, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/909152")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task TestMissingOnNullLiteral()
        {
            await TestMissingAsync(
@"class C1 { }
class C2 { }
class Test
{
    void M()
    {
        C1 c1 = [|null|];
        C2 c2 = null;
    }
}
");
        }

        [WorkItem(1130990, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1130990")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task InParentConditionalAccessExpressions()
        {
            var code =
    @"using System;
class C
{
    public T F<T>(T x)
    {
        var y = [|F(new C())|]?.F(new C())?.F(new C());
        return x;
    }
}";

            var expected =
    @"using System;
class C
{
    public T F<T>(T x)
    {
        var {|Rename:c|} = F(new C());
        var y = c?.F(new C())?.F(new C());
        return x;
    }
}";

            await TestAsync(code, expected, index: 0, compareTokens: false);
        }

        [WorkItem(1130990, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1130990")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task InParentConditionalAccessExpression2()
        {
            var code =
    @"using System;
class C
{
    public T F<T>(T x)
    {
        var y = [|F(new C()).F(new C())|]?.F(new C());
        return x;
    }
}";

            var expected =
    @"using System;
class C
{
    public T F<T>(T x)
    {
        var {|Rename:c|} = F(new C()).F(new C());
        var y = c?.F(new C());
        return x;
    }
}";

            await TestAsync(code, expected, index: 0, compareTokens: false);
        }

        [WorkItem(1130990, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1130990")]
        [WorkItem(3110, "https://github.com/dotnet/roslyn/issues/3110")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task MissingAcrossMultipleParentConditionalAccessExpressions()
        {
            await TestMissingAsync(
    @"using System;
class C
{
    public T F<T>(T x)
    {
        var y = [|F(new C())?.F(new C())|]?.F(new C());
        return x;
    }
}");
        }

        [WorkItem(1130990, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1130990")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task MissingOnInvocationExpressionInParentConditionalAccessExpressions()
        {
            await TestMissingAsync(
    @"using System;
class C
{
    public T F<T>(T x)
    {
        var y = F(new C())?.[|F(new C())|]?.F(new C());
        return x;
    }
}");
        }

        [WorkItem(1130990, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1130990")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task MissingOnMemberBindingExpressionInParentConditionalAccessExpressions()
        {
            await TestMissingAsync(
    @"using System;
class C
{
    static void Test(string s)
    {
        var l = s?.[|Length|] ?? 0;
    }
}");
        }

        [WorkItem(3147, "https://github.com/dotnet/roslyn/issues/3147")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task HandleFormattableStringTargetTyping1()
        {
            const string code = CodeSnippets.FormattableStringType + @"
namespace N
{
    using System;

    class C
    {
        public async Task M()
        {
            var f = FormattableString.Invariant([|$""""|]);
        }
    }
}";

            const string expected = CodeSnippets.FormattableStringType + @"
namespace N
{
    using System;

    class C
    {
        public async Task M()
        {
            FormattableString {|Rename:v|} = $"""";
            var f = FormattableString.Invariant(v);
        }
    }
}";

            await TestAsync(code, expected, index: 0, compareTokens: false);
        }

        [WorkItem(936, "https://github.com/dotnet/roslyn/issues/936")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task InAutoPropertyInitializer()
        {
            var code =
@"using System;
class C
{
    int Prop1 { get; } = [|1 + 2|];
}";
            var expected =
@"using System;
class C
{
    private const int {|Rename:V|} = 1 + 2;

    int Prop1 { get; } = V;
}";
            await TestAsync(code, expected, index: 0, compareTokens: false);
        }

        [WorkItem(936, "https://github.com/dotnet/roslyn/issues/936")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task InAutoPropertyInitializer2()
        {
            var code =
@"using System;
class C
{
    public DateTime TimeStamp { get; } = [|DateTime.UtcNow|];
}";
            var expected =
@"using System;
class C
{
    private static readonly DateTime {|Rename:utcNow|} = DateTime.UtcNow;

    public DateTime TimeStamp { get; } = utcNow;
}";
            await TestAsync(code, expected, index: 0, compareTokens: false);
        }

        [WorkItem(936, "https://github.com/dotnet/roslyn/issues/936")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public async Task BlockContextPreferredOverAutoPropertyInitializerContext()
        {
            var code =
@"using System;
class C
{
    Func<int, int> X { get; } = a => { return [|7|]; };
}";
            var expected =
@"using System;
class C
{
    Func<int, int> X { get; } = a => { const int {|Rename:V|} = 7; return V; };
}";
            await TestAsync(code, expected, index: 2, compareTokens: false);
        }
    }
}