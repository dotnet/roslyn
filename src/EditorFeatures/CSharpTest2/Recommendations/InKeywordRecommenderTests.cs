// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class InKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAtRoot_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterClass_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterGlobalStatement_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInUsingAlias()
        {
            VerifyAbsence(
@"using Goo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInEmptyStatement()
        {
            VerifyAbsence(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterFrom()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = from $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterFromIdentifier()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterFromAndTypeAndIdentifier()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from int x $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterJoin()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = from x in y
          join $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterJoinIdentifier()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          join z $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterJoinAndTypeAndIdentifier()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          join int z $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterJoinNotAfterIn()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = from x in y
          join z in $$"));
        }

        [WorkItem(544158, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544158")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterJoinPredefinedType()
        {
            VerifyAbsence(
@"using System;
using System.Linq;
class C {
    void M()
    {
        var q = from x in y
                join int $$");
        }

        [WorkItem(544158, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544158")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterJoinType()
        {
            VerifyAbsence(
@"using System;
using System.Linq;
class C {
    void M()
    {
        var q = from x in y
                join Int32 $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInForEach()
        {
            VerifyKeyword(AddInsideMethod(
@"foreach (var v $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInForEach1()
        {
            VerifyKeyword(AddInsideMethod(
@"foreach (var v $$ c"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInForEach2()
        {
            VerifyKeyword(AddInsideMethod(
@"foreach (var v $$ c"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInForEach()
        {
            VerifyAbsence(AddInsideMethod(
@"foreach ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInForEach1()
        {
            VerifyAbsence(AddInsideMethod(
@"foreach (var $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInForEach2()
        {
            VerifyAbsence(AddInsideMethod(
@"foreach (var v in $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInForEach3()
        {
            VerifyAbsence(AddInsideMethod(
@"foreach (var v in c $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInterfaceTypeVarianceAfterAngle()
        {
            VerifyKeyword(
@"interface IGoo<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInterfaceTypeVarianceNotAfterIn()
        {
            VerifyAbsence(
@"interface IGoo<in $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInterfaceTypeVarianceAfterComma()
        {
            VerifyKeyword(
@"interface IGoo<Goo, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInterfaceTypeVarianceAfterAttribute()
        {
            VerifyKeyword(
@"interface IGoo<[Goo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestDelegateTypeVarianceAfterAngle()
        {
            VerifyKeyword(
@"delegate void D<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestDelegateTypeVarianceAfterComma()
        {
            VerifyKeyword(
@"delegate void D<Goo, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestDelegateTypeVarianceAfterAttribute()
        {
            VerifyKeyword(
@"delegate void D<[Goo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInClassTypeVarianceAfterAngle()
        {
            VerifyAbsence(
@"class IGoo<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInStructTypeVarianceAfterAngle()
        {
            VerifyAbsence(
@"struct IGoo<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInBaseListAfterAngle()
        {
            VerifyAbsence(
@"interface IGoo : Bar<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInGenericMethod()
        {
            VerifyAbsence(
@"interface IGoo {
    void Goo<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestFrom2()
        {
            VerifyKeyword(AddInsideMethod(
@"var q2 = from int x $$ ((IEnumerable)src))"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestFrom3()
        {
            VerifyKeyword(AddInsideMethod(
@"var q2 = from x $$ ((IEnumerable)src))"));
        }

        [WorkItem(544158, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544158")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterFromPredefinedType()
        {
            VerifyAbsence(
@"using System;
using System.Linq;
class C {
    void M()
    {
        var q = from int $$");
        }

        [WorkItem(544158, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544158")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterFromType()
        {
            VerifyAbsence(
@"using System;
using System.Linq;
class C {
    void M()
    {
        var q = from Int32 $$");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TestInAsParameterModifierInMethods()
        {
            VerifyKeyword(@"
class Program
{
    public static void Test($$ p) { }
}");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TestInAsParameterModifierInSecondParameter()
        {
            VerifyKeyword(@"
class Program
{
    public static void Test(int p1, $$ p2) { }
}");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TestInAsParameterModifierInDelegates()
        {
            VerifyKeyword(@"
public delegate int Delegate($$ int p);");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TestInAsParameterModifierInLocalFunctions()
        {
            VerifyKeyword(@"
class Program
{
    public static void Test()
    {
        void localFunc($$ int p) { }
    }
}");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TestInAsParameterModifierInLambdaExpressions()
        {
            VerifyKeyword(@"
public delegate int Delegate(in int p);

class Program
{
    public static void Test()
    {
        Delegate lambda = ($$ int p) => p;
    }
}");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TestInAsParameterModifierInAnonymousMethods()
        {
            VerifyKeyword(@"
public delegate int Delegate(in int p);

class Program
{
    public static void Test()
    {
        Delegate anonymousDelegate = delegate ($$ int p) { return p; };
    }
}");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TestInAsModifierInMethodReturnTypes()
        {
            VerifyAbsence(@"
class Program
{
    public $$ int Test()
    {
        return ref x;
    }
}");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TestInAsModifierInGlobalMemberDeclaration()
        {
            VerifyAbsence(SourceCodeKind.Script, @"
public $$ ");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TestInAsModifierInDelegateReturnType()
        {
            VerifyAbsence(@"
public delegate $$ int Delegate();

class Program
{
}");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TestInAsModifierInMemberDeclaration()
        {
            VerifyAbsence(@"
class Program
{
    public $$ int Test { get; set; }
}");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInMethodFirstArgumentModifier()
        {
            VerifyKeyword(@"
class C {
    void M() {
        Call($$");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInMethodSecondArgumentModifier()
        {
            VerifyKeyword(@"
class C {
    void M(object arg1) {
        Call(arg1, $$");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInBaseCallFirstArgumentModifier()
        {
            VerifyKeyword(@"
class C {
    public C() : base($$");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInBaseCallSecondArgumentModifier()
        {
            VerifyKeyword(@"
class C {
    public C(object arg1) : base(arg1, $$");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInThisCallFirstArgumentModifier()
        {
            VerifyKeyword(@"
class C {
    public C() : this($$");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInThisCallSecondArgumentModifier()
        {
            VerifyKeyword(@"
class C {
    public C(object arg1) : this(arg1, $$");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(24079, "https://github.com/dotnet/roslyn/issues/24079")]
        public void TestInAsParameterModifierInConversionOperators()
        {
            VerifyKeyword(@"
class Program
{
    public static explicit operator double($$) { }
}");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(24079, "https://github.com/dotnet/roslyn/issues/24079")]
        public void TestInAsParameterModifierInBinaryOperators()
        {
            VerifyKeyword(@"
class Program
{
    public static Program operator +($$) { }
}");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInConstructorCallFirstArgumentModifier()
        {
            VerifyKeyword(@"
class C {
    void M() {
        new MyType($$");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInConstructorSecondArgumentModifier()
        {
            VerifyKeyword(@"
class C {
    void M(object arg1) {
        new MyType(arg1, $$");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInMethodFirstNamedArgumentModifier()
        {
            VerifyKeyword(@"
class C {
    void M() {
        Call(a: $$");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInMethodSecondNamedArgumentModifier()
        {
            VerifyKeyword(@"
class C {
    void M(object arg1) {
        Call(a: arg1, b: $$");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInBaseCallFirstNamedArgumentModifier()
        {
            VerifyKeyword(@"
class C {
    public C() : base(a: $$");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInBaseCallSecondNamedArgumentModifier()
        {
            VerifyKeyword(@"
class C {
    public C(object arg1) : base(a: arg1, b: $$");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInThisCallFirstNamedArgumentModifier()
        {
            VerifyKeyword(@"
class C {
    public C() : this(a: $$");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInThisCallSecondNamedArgumentModifier()
        {
            VerifyKeyword(@"
class C {
    public C(object arg1) : this(a: arg1, b: $$");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInConstructorCallFirstNamedArgumentModifier()
        {
            VerifyKeyword(@"
class C {
    void M() {
        new MyType(a: $$");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInConstructorSecondNamedArgumentModifier()
        {
            VerifyKeyword(@"
class C {
    void M(object arg1) {
        new MyType(a: arg1, b: $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestExtensionMethods_FirstParameter()
        {
            VerifyKeyword(
@"static class Extensions {
    static void Extension($$");
        }

        [WorkItem(30339, "https://github.com/dotnet/roslyn/issues/30339")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestExtensionMethods_FirstParameter_AfterThisKeyword()
        {
            VerifyKeyword(
@"static class Extensions {
    static void Extension(this $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestExtensionMethods_SecondParameter()
        {
            VerifyKeyword(
@"static class Extensions {
    static void Extension(this int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestExtensionMethods_SecondParameter_AfterThisKeyword()
        {
            VerifyAbsence(
@"static class Extensions {
    static void Extension(this int i, this $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestExtensionMethods_FirstParameter_NonStaticClass()
        {
            VerifyKeyword(
@"class Extensions {
    static void Extension($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestExtensionMethods_FirstParameter_AfterThisKeyword_NonStaticClass()
        {
            VerifyAbsence(
@"class Extensions {
    static void Extension(this $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestExtensionMethods_SecondParameter_NonStaticClass()
        {
            VerifyKeyword(
@"class Extensions {
    static void Extension(this int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestExtensionMethods_SecondParameter_AfterThisKeyword_NonStaticClass()
        {
            VerifyAbsence(
@"class Extensions {
    static void Extension(this int i, this $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestExtensionMethods_FirstParameter_NonStaticMethod()
        {
            VerifyKeyword(
@"static class Extensions {
    void Extension($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestExtensionMethods_FirstParameter_AfterThisKeyword_NonStaticMethod()
        {
            VerifyAbsence(
@"static class Extensions {
    void Extension(this $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestExtensionMethods_SecondParameter_NonStaticMethod()
        {
            VerifyKeyword(
@"static class Extensions {
    void Extension(this int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestExtensionMethods_SecondParameter_AfterThisKeyword_NonStaticMethod()
        {
            VerifyAbsence(
@"static class Extensions {
    void Extension(this int i, this $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInFunctionPointerTypeNoExistingModifiers()
        {
            VerifyKeyword(@"
class C
{
    delegate*<$$");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [InlineData("in")]
        [InlineData("out")]
        [InlineData("ref")]
        [InlineData("ref readonly")]
        public void TestNotInFunctionPointerTypeExistingModifiers(string modifier)
        {
            VerifyAbsence($@"
class C
{{
    delegate*<{modifier} $$");
        }
    }
}
