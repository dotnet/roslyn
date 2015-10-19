﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestMethodFix1()
        {
            Test(
                @"class C { void Foo() { Bar([|1 + 1|]); Bar(1 + 1); } }",
                @"class C { void Foo() { const int {|Rename:V|} = 1 + 1; Bar(V); Bar(1 + 1); } }",
                index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestMethodFix2()
        {
            Test(
                @"class C { void Foo() { Bar([|1 + 1|]); Bar(1 + 1); } }",
                @"class C { void Foo() { const int {|Rename:V|} = 1 + 1; Bar(V); Bar(V); } }",
                index: 3);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestMethodFix3()
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

            Test(code, expected, index: 2, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestMethodFix4()
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

            Test(code, expected, index: 3, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestFieldFix1()
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

            Test(code, expected, index: 0, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestFieldFix2()
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

            Test(code, expected, index: 1, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestConstFieldFix1()
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

            Test(code, expected, index: 0, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestConstFieldFix2()
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

            Test(code, expected, index: 1, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestConstructorFix1()
        {
            Test(
                @"class C { public C() : this([|1 + 1|], 1 + 1) { } }",
                @"class C { private const int {|Rename:V|} = 1 + 1; public C() : this(V, 1 + 1) { } }",
                index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestConstructorFix2()
        {
            Test(
                @"class C { public C() : this([|1 + 1|], 1 + 1) { } }",
                @"class C { private const int {|Rename:V|} = 1 + 1; public C() : this(V, V) { } }",
                index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestParameterFix1()
        {
            Test(
                @"class C { void Bar(int i = [|1 + 1|], int j = 1 + 1) { } }",
                @"class C { private const int {|Rename:V|} = 1 + 1; void Bar(int i = V, int j = 1 + 1) { } }",
                index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestParameterFix2()
        {
            Test(
                @"class C { void Bar(int i = [|1 + 1|], int j = 1 + 1) { } }",
                @"class C { private const int {|Rename:V|} = 1 + 1; void Bar(int i = V, int j = V) { } }",
                index: 1);
        }

        [WpfFact]
        public void TestAttributeFix1()
        {
            Test(
                @"class C { [Foo([|1 + 1|], 1 + 1)]void Bar() { } }",
                @"class C { private const int {|Rename:V|} = 1 + 1; [Foo(V, 1 + 1)]void Bar() { } }",
                index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestAttributeFix2()
        {
            Test(
                @"class C { [Foo([|1 + 1|], 1 + 1)]void Bar() { } }",
                @"class C { private const int {|Rename:V|} = 1 + 1; [Foo(V, V)]void Bar() { } }",
                index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestMethodFixExistingName1()
        {
            Test(
                @"class C { void Foo() { int V = 0; Bar([|1 + 1|]); Bar(1 + 1); } }",
                @"class C { void Foo() { int V = 0; const int {|Rename:V1|} = 1 + 1; Bar(V1); Bar(1 + 1); } }",
                index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestFieldExistingName1()
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

            Test(
                code,
                expected,
                index: 0,
                compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestMethodFixComplexName1()
        {
            Test(
                @"class C { static int Baz; void Foo() { Bar([|C.Baz|]); Bar(1 + 1); } }",
                @"class C { static int Baz; void Foo() { var {|Rename:baz|} = C.Baz; Bar(baz); Bar(1 + 1); } }",
                index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestMethodFixComplexName1NotVar()
        {
            Test(
                @"class C { static int Baz; void Foo() { Bar([|C.Baz|]); Bar(1 + 1); } }",
                @"class C { static int Baz; void Foo() { int {|Rename:baz|} = C.Baz; Bar(baz); Bar(1 + 1); } }",
                index: 0,
                options: new Dictionary<OptionKey, object> { { new OptionKey(CSharpCodeStyleOptions.UseVarWhenDeclaringLocals), false } });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestNameConflict1()
        {
            Test(
                @"class C { public C(int V) : this([|1 + 1|]) { } }",
                @"class C { private const int {|Rename:V|} = 1 + 1; public C(int V) : this(C.V) { } }",
                index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestNameConflict2()
        {
            Test(
@"using System ; class Program { private static int v = 5 ; static void Main ( string [ ] args ) { Func < int , int > d = ( x ) => { return [|x * v|] ; } ; d . Invoke ( v ) ; } } ",
@"using System ; class Program { private static int v = 5 ; static void Main ( string [ ] args ) { Func < int , int > d = ( x ) => { var {|Rename:v1|} = x * v; return v1 ; } ; d . Invoke ( v ) ; } } ",
index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestNameConflict2NotVar()
        {
            Test(
@"using System ; class Program { private static int v = 5 ; static void Main ( string [ ] args ) { Func < int , int > d = ( x ) => { return [|x * v|] ; } ; d . Invoke ( v ) ; } } ",
@"using System ; class Program { private static int v = 5 ; static void Main ( string [ ] args ) { Func < int , int > d = ( x ) => { int {|Rename:v1|} = x * v; return v1 ; } ; d . Invoke ( v ) ; } } ",
index: 0,
options: new Dictionary<OptionKey, object> { { new OptionKey(CSharpCodeStyleOptions.UseVarWhenDeclaringLocals), false } });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestNameVerbatimIdentifier1()
        {
            Test(
@"static class G<T> { public class @class { } public static void Add(object t) { } } class Program { static void Main() { G<int>.Add([|new G<int>.@class()|]); } }",
@"static class G<T> { public class @class { } public static void Add(object t) { } } class Program { static void Main() { var {|Rename:@class|} = new G<int>.@class(); G<int>.Add(@class); } }",
index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestNameVerbatimIdentifier1NoVar()
        {
            Test(
@"static class G<T> { public class @class { } public static void Add(object t) { } } class Program { static void Main() { G<int>.Add([|new G<int>.@class()|]); } }",
@"static class G<T> { public class @class { } public static void Add(object t) { } } class Program { static void Main() { G<int>.@class {|Rename:@class|} = new G<int>.@class(); G<int>.Add(@class); } }",
index: 0,
options: new Dictionary<OptionKey, object> { { new OptionKey(CSharpCodeStyleOptions.UseVarWhenDeclaringLocals), false } });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestNameVerbatimIdentifier2()
        {
            Test(
@"static class G<T> { public class @class { } public static void Add(object t) { } static void Main() { G<int>.Add([|new G<int>.@class()|]); } }",
@"static class G<T> { public class @class { } public static void Add(object t) { } static void Main() { var {|Rename:class1|} = new G<int>.@class(); G<int>.Add(class1); } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestNameVerbatimIdentifier2NoVar()
        {
            Test(
@"static class G<T> { public class @class { } public static void Add(object t) { } static void Main() { G<int>.Add([|new G<int>.@class()|]); } }",
@"static class G<T> { public class @class { } public static void Add(object t) { } static void Main() { G<int>.@class {|Rename:class1|} = new G<int>.@class(); G<int>.Add(class1); } }",
options: new Dictionary<OptionKey, object> { { new OptionKey(CSharpCodeStyleOptions.UseVarWhenDeclaringLocals), false } });
        }

        [WorkItem(540078)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestConstantField1()
        {
            Test(
@"class C { int [ ] array = new int [ [|10|] ] ; } ",
@"class C { private const int {|Rename:V|} = 10 ; int [ ] array = new int [ V ] ; } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        [WorkItem(540079)]
        public void TestFormattingOfReplacedExpression1()
        {
            Test(
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

        [WorkItem(540468)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestCantExtractMethodTypeParameterToField()
        {
            Test(
@"using System ; using System . Collections . Generic ; using System . Linq ; class Program { static void Main < T > ( string [ ] args ) { Foo ( [|( T ) 2 . ToString ( )|] ) ; } } ",
@"using System ; using System . Collections . Generic ; using System . Linq ; class Program { static void Main < T > ( string [ ] args ) { var {|Rename:t|} = ( T ) 2 . ToString ( ) ; Foo ( t ) ; } } ");
        }

        [WorkItem(540468)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestCantExtractMethodTypeParameterToFieldCount()
        {
            TestActionCount(
@"using System ; using System . Collections . Generic ; using System . Linq ; class Program { static void Main < T > ( string [ ] args ) { Foo ( [|( T ) 2 . ToString ( )|] ) ; } } ",
count: 2);
        }

        [WorkItem(552389)]
        [WorkItem(540482)]
        [WpfFact(Skip = "552389"), Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestConstantForFixedBufferInitializer()
        {
            Test(
@"unsafe struct S { fixed int buffer [ [|10|] ] ; } ",
@"unsafe struct S { private const int p = 10 ; fixed int buffer [ p ] ; } ",
index: 0);
        }

        [WorkItem(540486)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestFormattingOfIntroduceLocal()
        {
            Test(
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestLocalConstant()
        {
            Test(
@"class Program { static void Main ( string [ ] args ) { const int i = [|1|] + 1 ; } } ",
@"class Program { static void Main ( string [ ] args ) { const int {|Rename:V|} = 1 ; const int i = V + 1 ; } } ",
index: 2);
        }

        [WorkItem(542699)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestFieldConstant()
        {
            Test(
@"[ Foo ( 2 + 3 + 4 ) ] class Program { int x = [|2 + 3|] + 4 ; } internal class FooAttribute : System . Attribute { public FooAttribute ( int x ) { } } ",
@"[ Foo ( V + 4 ) ] class Program { private const int {|Rename:V|} = 2 + 3 ; int x = V + 4 ; } internal class FooAttribute : System . Attribute { public FooAttribute ( int x ) { } } ",
index: 1);
        }

        [WorkItem(542781)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestMissingOnExpressionStatement()
        {
            TestMissing(
@"class Program
{
    static void Main(string[] args)
    {
        int i; [|i = 2|]; i = 3;
    }
}
");
        }

        [WorkItem(542780)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestSingleQueryClause()
        {
            Test(
@"using System . Linq ; class Program { void Main ( ) { var query = from i in new int [ ] { 1 } from j in new int [ ] { 2 } select [|i + j|] ; } } ",
@"using System . Linq ; class Program { void Main ( ) { var query = from i in new int [ ] { 1 } from j in new int [ ] { 2 } let {|Rename:v|} = i + j select v ; } } ",
index: 0);
        }

        [WorkItem(542780)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestSingleQuerySelectOrGroupByClause()
        {
            Test(
@"using System . Linq ; class Program { void Main ( ) { var query = from i in new int [ ] { 1 } from j in new int [ ] { 2 } where [|i + j|] > 5 select i + j ; } } ",
@"using System . Linq ; class Program { void Main ( ) { var query = from i in new int [ ] { 1 } from j in new int [ ] { 2 } let {|Rename:v|} = i + j where v > 5 select i + j ; } } ",
index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestLinqQuery()
        {
            Test(
@"using System . Linq ; class Program { void Main ( ) { var query = from i in new int [ ] { 1 } from j in new int [ ] { 2 } where [|i + j|] > 5 let x = j + i select i + j ; } } ",
@"using System . Linq ; class Program { void Main ( ) { var query = from i in new int [ ] { 1 } from j in new int [ ] { 2 } let {|Rename:v|} = i + j where v > 5 let x = j + i select v ; } } ",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestSingleQueryReplaceAll()
        {
            Test(
@"using System . Linq ; class Program { void Main ( ) { var query = from i in new int [ ] { 1 } from j in new int [ ] { 2 } where i + j > 5 let x = j + i select [|i + j|] ; } } ",
@"using System . Linq ; class Program { void Main ( ) { var query = from i in new int [ ] { 1 } from j in new int [ ] { 2 } let {|Rename:v|} = i + j where v > 5 let x = j + i select v ; } } ",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestNestedQueryReplaceOne1()
        {
            Test(
@"using System . Linq ; class Program { void Main ( ) { var query = from i in new int [ ] { 1 } from j in new int [ ] { 2 } where i > ( from k in new int [ ] { 3 } select [|i + j|] ) . Max ( ) where j > ( from m in new int [ ] { 4 } select i + j ) . Max ( ) let x = j + i select i + j ; } } ",
@"using System . Linq ; class Program { void Main ( ) { var query = from i in new int [ ] { 1 } from j in new int [ ] { 2 } where i > ( from k in new int [ ] { 3 } let {|Rename:v|} = i + j select v ) . Max ( ) where j > ( from m in new int [ ] { 4 } select i + j ) . Max ( ) let x = j + i select i + j ; } } ",
index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestNestedQueryReplaceAll1()
        {
            Test(
@"using System . Linq ; class Program { void Main ( ) { var query = from i in new int [ ] { 1 } from j in new int [ ] { 2 } where i > ( from k in new int [ ] { 3 } select [|i + j|] ) . Max ( ) where j > ( from m in new int [ ] { 4 } select i + j ) . Max ( ) let x = j + i select i + j ; } } ",
@"using System . Linq ; class Program { void Main ( ) { var query = from i in new int [ ] { 1 } from j in new int [ ] { 2 } let {|Rename:v|} = i + j where i > ( from k in new int [ ] { 3 } select v ) . Max ( ) where j > ( from m in new int [ ] { 4 } select v ) . Max ( ) let x = j + i select v ; } } ",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestNestedQueryReplaceOne2()
        {
            Test(
@"using System . Linq ; class Program { void Main ( ) { var query = from i in new int [ ] { 1 } from j in new int [ ] { 2 } where i > ( from k in new int [ ] { 3 } select i + j ) . Max ( ) where j > ( from m in new int [ ] { 4 } select [|i + j|] ) . Max ( ) let x = j + i select i + j ; } } ",
@"using System . Linq ; class Program { void Main ( ) { var query = from i in new int [ ] { 1 } from j in new int [ ] { 2 } where i > ( from k in new int [ ] { 3 } select i + j ) . Max ( ) where j > ( from m in new int [ ] { 4 } let {|Rename:v|} = i + j select v ) . Max ( ) let x = j + i select i + j ; } } ",
index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestNestedQueryReplaceAll2()
        {
            Test(
@"using System . Linq ; class Program { void Main ( ) { var query = from i in new int [ ] { 1 } from j in new int [ ] { 2 } where i > ( from k in new int [ ] { 3 } select i + j ) . Max ( ) where j > ( from m in new int [ ] { 4 } select [|i + j|] ) . Max ( ) let x = j + i select i + j ; } } ",
@"using System . Linq ; class Program { void Main ( ) { var query = from i in new int [ ] { 1 } from j in new int [ ] { 2 } let {|Rename:v|} = i + j where i > ( from k in new int [ ] { 3 } select v ) . Max ( ) where j > ( from m in new int [ ] { 4 } select v ) . Max ( ) let x = j + i select v ; } } ",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        [WorkItem(10742, "DevDiv_Projects/Roslyn")]
        public void TestAnonymousTypeMemberAssignment()
        {
            TestMissing(
@"class C { void M ( ) { var a = new { [|A = 0|] } ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        [WorkItem(10743, "DevDiv_Projects/Roslyn")]
        public void TestAnonymousTypeBody()
        {
            TestMissing(
@"class C { void M ( ) { var a = new [|{ A = 0 }|] ; } } ");
        }

        [WorkItem(543477)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestImplicitlyTypedArraysUsedInCheckedExpression()
        {
            Test(
@"class Program { static void Main ( string [ ] args ) { int [ ] a = null ; int [ ] temp = checked ( [|a = new [ ] { 1 , 2 , 3 }|] ) ; } } ",
@"class Program { static void Main ( string [ ] args ) { int [ ] a = null ; var {|Rename:v|} = a = new [ ] { 1 , 2 , 3 } ; int [ ] temp = checked ( v ) ; } } ");
        }

        [WorkItem(543832)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestMissingOnGenericTypeParameter()
        {
            TestMissing(
@"class C { void M() { F<[|int?|], int?>(3); } R F<T, R>(T arg1) { return default(R); } }");
        }

        [WorkItem(543941)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestAnonymousType1()
        {
            Test(
@"class Program { void Main ( ) { WriteLine ( [|new { X = 1 }|] ) ; } } ",
@"class Program { void Main ( ) { var {|Rename:p|} = new { X = 1 }; WriteLine(p); } } ");
        }

        [WorkItem(544099)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestMissingOnAttributeNameEquals()
        {
            TestMissing(
@"using System;
using System.Runtime.InteropServices; 

class M
{
    [DllImport(""user32.dll"", [|CharSet|] = CharSet.Auto)]
    public static extern IntPtr FindWindow(string className, string windowTitle);
}");
        }

        [WorkItem(544162)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestMissingOnRightOfDot()
        {
            TestMissing(
@"using System ; using System . Runtime . InteropServices ; class M { [ DllImport ( ""user32.dll"" , CharSet = CharSet . [|Auto|] ) ] public static extern IntPtr FindWindow ( string className , string windowTitle ) ; } ");
        }

        [WorkItem(544209)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestMissingOnAttributeNamedParameter()
        {
            TestMissing(
@"using System ; class TestAttribute : Attribute { public TestAttribute ( int a = 42 ) { } } [ Test ( [|a|] : 1 ) ] class Foo { } ");
        }

        [WorkItem(544264)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestMissingOnVariableWrite()
        {
            TestMissing(
@"class Program { void Main ( ) { var x = new int [ 3 ] ; [|x [ 1 ]|] = 2 ; } } ");
        }

        [WorkItem(544577)]
        [WorkItem(909152)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestExpressionTLambda()
        {
            TestMissing(
@"using System ; using System . Linq . Expressions ; class Program { static Expression < Func < int ? , char ? > > e1 = c => [|null|] ; } ");
        }

        [WorkItem(544915)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestMissingOnTypeSyntax()
        {
            TestMissing(
@"using System ; class Program { void Main ( ) { int [ , ] array2Da = new [|int [ 1 , 2 ]|] { { 1 , 2 } } ; } } ");
        }

        [WorkItem(544610)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void ParenthesizeIfParseChanges()
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

            Test(code, expected, index: 2, compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestMissingInPartiallyHiddenMethod()
        {
            TestMissing(
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestInVisibleMethod()
        {
            Test(
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestMissingInFieldInPartiallyHiddenType()
        {
            TestMissing(
@"class Program
{
    int i = [|1 + 1|];

#line hidden
}
#line default", parseOptions: Options.Regular);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestMissingInAttributeInPartiallyHiddenType()
        {
            TestMissing(
@"[Foo([|1 + 1|])]
class Program
{
#line hidden
}
#line default", parseOptions: Options.Regular);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestMissingInConstructorInitializerInPartiallyHiddenType()
        {
            TestMissing(
@"class Program
{
    public Program() : this([|1 + 1|])
    {
    }

#line hidden
}
#line default", parseOptions: Options.Regular);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestMissingInParameterInPartiallyHiddenType()
        {
            TestMissing(
@"class Program
{
    public Program(int i = [|1 + 1|])
    {
    }

#line hidden
}
#line default", parseOptions: Options.Regular);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestMissingInQueryInPartiallyHiddenType()
        {
            TestMissing(
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestInVisibleQueryInHiddenType()
        {
            Test(
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestMissingOnNamespace()
        {
            TestMissing(
@"class Program { void Main ( ) { [|System|] . Console . WriteLine ( 4 ) ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestMissingOnType()
        {
            TestMissing(
@"class Program { void Main ( ) { [|System . Console|] . WriteLine ( 4 ) ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestMissingOnBase()
        {
            TestMissing(
@"class Program { void Main ( ) { [|base|] . ToString ( ) ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestVenusGeneration1()
        {
            TestMissing(
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestVenusGeneration2()
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

            TestExactActionSetOffered(code, new[] { string.Format(FeaturesResources.IntroduceLocalConstantFor, "5") });

            Test(code,
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestVenusGeneration3()
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

            TestExactActionSetOffered(code,
                new[] { string.Format(FeaturesResources.IntroduceLocalConstantFor, "5"), string.Format(FeaturesResources.IntroduceLocalConstantForAll, "5") });
        }

        [WorkItem(529795)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestMissingOnNegatedLiteral()
        {
            TestMissing(
@"class A { void Main ( ) { long x = - [|9223372036854775808|] ; } } ");
        }

        [WorkItem(546091)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestNotOnInterfaceAttribute()
        {
            TestMissing(
@"[ GuidAttribute ( [|""1A585C4D-3371-48dc-AF8A-AFFECC1B0967""|] ) ] public interface I { } ");
        }

        [WorkItem(546095)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestNotOnTypeOfInAttribute()
        {
            TestMissing(
@"using System . Runtime . InteropServices ; [ ComSourceInterfaces ( [|typeof ( GuidAttribute )|] ) ] public class Button { } ");
        }

        [WorkItem(530109)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestPreferGenerateConstantField1()
        {
            Test(
@"class C { void foo ( string s = ""Hello"" ) { var s2 = [|""Hello""|] + ""World"" ; } } ",
@"class C { private const string {|Rename:V|} = ""Hello"" ; void foo ( string s = ""Hello"" ) { var s2 = V + ""World"" ; } } ");
        }

        [WorkItem(530109)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestPreferGenerateConstantField2()
        {
            Test(
@"class C { void foo ( string s = ""Hello"" ) { var s2 = [|""Hello""|] + ""World"" ; } } ",
@"class C { private const string {|Rename:V|} = ""Hello"" ; void foo ( string s = V ) { var s2 = V + ""World"" ; } } ",
index: 1);
        }

        [WorkItem(530109)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestPreferGenerateConstantField3()
        {
            Test(
@"class C { void foo ( string s = ""Hello"" ) { var s2 = [|""Hello""|] + ""World"" ; } } ",
@"class C { void foo ( string s = ""Hello"" ) { const string {|Rename:V|} = ""Hello"" ; var s2 = V + ""World"" ; } } ",
index: 2);
        }

        [WorkItem(530109)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestPreferGenerateConstantField4()
        {
            Test(
@"class C { void foo ( string s = ""Hello"" ) { var s2 = [|""Hello""|] + ""World"" ; } } ",
@"class C { void foo ( string s = ""Hello"" ) { const string {|Rename:V|} = ""Hello"" ; var s2 = V + ""World"" ; } } ",
index: 3);
        }

        [WorkItem(530109)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestNoGenerateConstantFieldIfAccessingLocal1()
        {
            Test(
@"class C { void foo ( string s = ""Hello"" ) { const string s1 = ""World"" ; var s2 = [|""Hello"" + s1|] ; } } ",
@"class C { void foo ( string s = ""Hello"" ) { const string s1 = ""World"" ; const string {|Rename:V|} = ""Hello"" + s1 ; var s2 = V ; } } ",
index: 0);
        }

        [WorkItem(530109)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestNoGenerateConstantFieldIfAccessingLocal2()
        {
            Test(
@"class C { void foo ( string s = ""Hello"" ) { const string s1 = ""World"" ; var s2 = [|""Hello"" + s1|] ; } } ",
@"class C { void foo ( string s = ""Hello"" ) { const string s1 = ""World"" ; const string {|Rename:V|} = ""Hello"" + s1 ; var s2 = V ; } } ",
index: 1);
        }

        [WorkItem(530109)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestNoGenerateConstantFieldIfNotAccessingLocal1()
        {
            Test(
@"class C { const string s1 = ""World"" ; void foo ( string s = ""Hello"" ) { var s2 = [|""Hello"" + s1|] ; } } ",
@"class C { const string s1 = ""World"" ; private const string {|Rename:V|} = ""Hello"" + s1 ; void foo ( string s = ""Hello"" ) { var s2 = V ; } } ");
        }

        [WorkItem(530109)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestNoGenerateConstantFieldIfNotAccessingLocal2()
        {
            Test(
@"class C { const string s1 = ""World"" ; void foo ( string s = ""Hello"" ) { var s2 = [|""Hello"" + s1|] ; } } ",
@"class C { const string s1 = ""World"" ; private const string {|Rename:V|} = ""Hello"" + s1 ; void foo ( string s = ""Hello"" ) { var s2 = V ; } } ",
index: 1);
        }

        [WorkItem(530109)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestNoGenerateConstantFieldIfNotAccessingLocal3()
        {
            Test(
@"class C { const string s1 = ""World"" ; void foo ( string s = ""Hello"" ) { var s2 = [|""Hello"" + s1|] ; } } ",
@"class C { const string s1 = ""World"" ; void foo ( string s = ""Hello"" ) { const string {|Rename:V|} = ""Hello"" + s1 ; var s2 = V ; } } ",
index: 2);
        }

        [WorkItem(530109)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestNoGenerateConstantFieldIfNotAccessingLocal4()
        {
            Test(
@"class C { const string s1 = ""World"" ; void foo ( string s = ""Hello"" ) { var s2 = [|""Hello"" + s1|] ; } } ",
@"class C { const string s1 = ""World"" ; void foo ( string s = ""Hello"" ) { const string {|Rename:V|} = ""Hello"" + s1 ; var s2 = V ; } } ",
index: 3);
        }

        [WorkItem(606347)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void InsertNeededCast1()
        {
            Test(
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

        [WorkItem(606347)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void InsertNeededCast1NotVar()
        {
            Test(
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

        [WorkItem(606347), WorkItem(714632)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void InsertNeededCast2()
        {
            Test(
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

        [WorkItem(546512)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestInSwitchSection()
        {
            Test(
@"class Program { int Main ( int i ) { switch ( 1 ) { case 0 : var f = Main ( [|1 + 1|] ) ; Console . WriteLine ( f ) ; } } } ",
@"class Program { int Main ( int i ) { switch ( 1 ) { case 0 : const int {|Rename:V|} = 1 + 1 ; var f = Main ( V ) ; Console . WriteLine ( f ) ; } } } ",
index: 2);
        }

        [WorkItem(530480)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestLambdaParameter1()
        {
            Test(
@"using System ; class Program { static void Main ( string [ ] args ) { Func < int , int > f = x => [|x + 1|] ; } } ",
@"using System ; class Program { static void Main ( string [ ] args ) { Func < int , int > f = x => { var {|Rename:v|} = x + 1 ; return v; }; } } ");
        }

        [WorkItem(530480)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestLambdaParameter2()
        {
            Test(
@"using System ; class Program { static void Main ( string [ ] args ) { Func < int , Func < int , int > > f = x => y => [|x + 1|] ; } } ",
@"using System ; class Program { static void Main ( string [ ] args ) { Func < int , Func < int , int > > f = x => { var {|Rename:v|} = x + 1 ; return y => v; }; } } ");
        }

        [WorkItem(530480)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestLambdaParameter3()
        {
            Test(
@"using System ; class Program { static void Main ( string [ ] args ) { Func < int , Func < int , int > > f = x => y => [|y + 1|] ; } } ",
@"using System ; class Program { static void Main ( string [ ] args ) { Func < int , Func < int , int > > f = x => y =>{ var {|Rename:v|} =  y + 1 ; return v; }; } } ");
        }

        [WorkItem(530480)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestLambdaParameter4()
        {
            Test(
@"using System ; class Program { static void Main ( string [ ] args ) { Func < int , Func < int , int > > f = x => [|y => y + 1|] ; } } ",
            @"using System ; class Program { static void Main ( string [ ] args ) { Func < int , int > {|Rename:p|} = y => y + 1; Func < int , Func < int , int > > f = x => p ; } } ");
        }

        [WorkItem(530480)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestLambdaParameter5()
        {
            Test(
@"using System ; class Program { static void Main ( string [ ] args ) { Func < int , Func < int , int > > f = x => [|y => x + 1|] ; } } ",
@"using System ; class Program { static void Main ( string [ ] args ) { Func < int , Func < int , int > > f = x => { Func<int,int> {|Rename:p|} = y => x + 1 ; return p; }; } } ");
        }

        [WorkItem(530721)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestIntroVarInAction1()
        {
            Test(
@"using System ; class Program { void M ( ) { Action < int > foo = x => [|x . Foo|] ; } } ",
@"using System ; class Program { void M ( ) { Action < int > foo = x => { object {|Rename:foo1|} = x . Foo ; } ; } } ");
        }

        [WorkItem(530919)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestNullableOfPointerType()
        {
            Test(
@"using System ; class Program { static void Main ( ) { [|new Nullable < int * > ( )|] . GetValueOrDefault ( ) ; } } ",
@"using System ; class Program { static void Main ( ) { var {|Rename:v|} = new Nullable < int * > ( ) ; v . GetValueOrDefault ( ) ; } } ");
        }

        [WorkItem(530919)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestNullableOfPointerTypeNotVar()
        {
            Test(
@"using System ; class Program { static void Main ( ) { [|new Nullable < int * > ( )|] . GetValueOrDefault ( ) ; } } ",
@"using System ; class Program { static void Main ( ) { Nullable < int * > {|Rename:v|} = new Nullable < int * > ( ) ; v . GetValueOrDefault ( ) ; } } ",
options: new Dictionary<OptionKey, object> { { new OptionKey(CSharpCodeStyleOptions.UseVarWhenDeclaringLocals), false } });
        }

        [WorkItem(830885)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestIntroduceLocalRemovesUnnecessaryCast()
        {
            Test(
@"using System.Collections.Generic; class C { static void Main(string[] args) { var set = new HashSet<string>(); set.Add([|set.ToString()|]); } } ",
@"using System.Collections.Generic; class C { static void Main(string[] args) { var set = new HashSet<string>(); var {|Rename:v|} = set.ToString(); set.Add(v); } } ");
        }

        [WorkItem(655498)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void HandleParenthesizedExpression()
        {
            Test(
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

        [WorkItem(682683)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void DontRemoveParenthesesIfOperatorPrecedenceWouldBeBroken()
        {
            Test(
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

        [WorkItem(828108)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void UseNewSemanticModelForSimplification()
        {
            Test(
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

        [WorkItem(884961)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestInCollectionInitializer()
        {
            Test(
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

        [WorkItem(884961)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestInCollectionInitializerNoVar()
        {
            Test(
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

        [WorkItem(854662)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestInNestedCollectionInitializers()
        {
            Test(
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

        [WorkItem(884961)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestInArrayInitializer()
        {
            Test(
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

        [WorkItem(884961)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestInArrayInitializerWithoutVar()
        {
            Test(
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

        [WorkItem(1022447)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestFormattingOfIntroduceLocal2()
        {
            Test(
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

        [WorkItem(939259)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestIntroduceLocalWithTriviaInMultiLineStatements()
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

            Test(code, expected, index: 3, compareTokens: false);
        }

        [WorkItem(939259)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestIntroduceLocalWithTriviaInMultiLineStatements2()
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

            Test(code, expected, index: 3, compareTokens: false);
        }

        [WorkItem(1064803)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestIntroduceLocalInStringInterpolation()
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

            Test(code, expected, index: 0, compareTokens: false);
        }

        [WorkItem(1037057)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestIntroduceLocalWithBlankLine()
        {
            Test(@"
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

        [WorkItem(1065661)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestIntroduceVariableTextDoesntSpanLines()
        {
            TestSmartTagText(@"
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

        [WorkItem(1097147)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestSmartNameForNullablesInConditionalAccessExpressionContext()
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

            Test(code, expected, index: 0, compareTokens: false);
        }

        [WorkItem(1097147)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestSmartNameForNullablesInConditionalAccessExpressionContext2()
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

            Test(code, expected, index: 0, compareTokens: false);
        }

        [WorkItem(1097147)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestSmartNameForNullablesInConditionalAccessExpressionContext3()
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

            Test(code, expected, index: 0, compareTokens: false);
        }

        [WorkItem(1097147)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestSmartNameForNullablesInConditionalAccessExpressionContext4()
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

            Test(code, expected, index: 0, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestIntroduceLocalInExpressionBodiedMethod()
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

            Test(code, expected, index: 2, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestIntroduceFieldInExpressionBodiedMethod()
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

            Test(code, expected, index: 0, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestIntroduceLocalInExpressionBodiedOperator()
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

            Test(code, expected, index: 0, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestIntroduceFieldInExpressionBodiedOperator()
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

            Test(code, expected, index: 0, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestIntroduceFieldInExpressionBodiedConversionOperator()
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

            Test(code, expected, index: 0, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestIntroduceFieldInExpressionBodiedProperty()
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

            Test(code, expected, index: 0, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestIntroduceLocalInExpressionBodiedProperty()
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

            Test(code, expected, index: 2, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestIntroduceFieldInExpressionBodiedIndexer()
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

            Test(code, expected, index: 0, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestIntroduceLocalInExpressionBodiedIndexer()
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

            Test(code, expected, index: 0, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestTrailingTriviaOnExpressionBodiedMethodRewrites()
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

            Test(code, expected, index: 2, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestLeadingTriviaOnExpressionBodiedMethodRewrites()
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

            Test(code, expected, index: 2, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestTriviaAroundArrowTokenInExpressionBodiedMemberSyntax()
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

            Test(code, expected, index: 2, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [WorkItem(971, "http://github.com/dotnet/roslyn/issues/971")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestIntroduceLocalInExpressionBodiedMethodWithBlockBodiedAnonymousMethodExpression()
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

            Test(code, expected, index: 2, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [WorkItem(971, "http://github.com/dotnet/roslyn/issues/971")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestIntroduceLocalInExpressionBodiedMethodWithSingleLineBlockBodiedAnonymousMethodExpression()
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

            Test(code, expected, index: 2, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [WorkItem(971, "http://github.com/dotnet/roslyn/issues/971")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestIntroduceLocalInExpressionBodiedMethodWithBlockBodiedSimpleLambdaExpression()
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

            Test(code, expected, index: 2, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestIntroduceLocalInExpressionBodiedMethodWithExpressionBodiedSimpleLambdaExpression()
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

            Test(code, expected, index: 2, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [WorkItem(971, "http://github.com/dotnet/roslyn/issues/971")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestIntroduceLocalInExpressionBodiedMethodWithBlockBodiedParenthesizedLambdaExpression()
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

            Test(code, expected, index: 2, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestIntroduceLocalInExpressionBodiedMethodWithExpressionBodiedParenthesizedLambdaExpression()
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

            Test(code, expected, index: 2, compareTokens: false);
        }

        [WorkItem(528, "http://github.com/dotnet/roslyn/issues/528")]
        [WorkItem(971, "http://github.com/dotnet/roslyn/issues/971")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestIntroduceLocalInExpressionBodiedMethodWithBlockBodiedAnonymousMethodExpressionInMethodArgs()
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

            Test(code, expected, index: 2, compareTokens: false);
        }

        [WorkItem(976, "https://github.com/dotnet/roslyn/issues/976")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestNoConstantForInterpolatedStrings1()
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

            Test(code, expected, index: 0, compareTokens: false);
        }

        [WorkItem(976, "https://github.com/dotnet/roslyn/issues/976")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestNoConstantForInterpolatedStrings2()
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

            Test(code, expected, index: 1, compareTokens: false);
        }

        [WorkItem(909152)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void TestMissingOnNullLiteral()
        {
            TestMissing(
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

        [WorkItem(1130990)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void InParentConditionalAccessExpressions()
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

            Test(code, expected, index: 0, compareTokens: false);
        }

        [WorkItem(1130990)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void InParentConditionalAccessExpression2()
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

            Test(code, expected, index: 0, compareTokens: false);
        }

        [WorkItem(1130990)]
        [WorkItem(3110, "https://github.com/dotnet/roslyn/issues/3110")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void MissingAcrossMultipleParentConditionalAccessExpressions()
        {
            TestMissing(
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

        [WorkItem(1130990)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void MissingOnInvocationExpressionInParentConditionalAccessExpressions()
        {
            TestMissing(
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

        [WorkItem(1130990)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void MissingOnMemberBindingExpressionInParentConditionalAccessExpressions()
        {
            TestMissing(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void HandleFormattableStringTargetTyping1()
        {
            const string code = CodeSnippets.FormattableStringType + @"
namespace N
{
    using System;

    class C
    {
        public void M()
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
        public void M()
        {
            FormattableString {|Rename:v|} = $"""";
            var f = FormattableString.Invariant(v);
        }
    }
}";

            Test(code, expected, index: 0, compareTokens: false);
        }

        [WorkItem(936, "https://github.com/dotnet/roslyn/issues/936")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void InAutoPropertyInitializer()
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
            Test(code, expected, index: 0, compareTokens: false);
        }

        [WorkItem(936, "https://github.com/dotnet/roslyn/issues/936")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void InAutoPropertyInitializer2()
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
            Test(code, expected, index: 0, compareTokens: false);
        }

        [WorkItem(936, "https://github.com/dotnet/roslyn/issues/936")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceVariable)]
        public void BlockContextPreferredOverAutoPropertyInitializerContext()
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
            Test(code, expected, index: 2, compareTokens: false);
        }
    }
}
