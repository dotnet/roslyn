// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class ReturnKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAtRoot_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterClass_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobalStatement_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInUsingAlias()
        {
            VerifyAbsence(
@"using Goo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestIncompleteStatementAttributeList()
        {
            VerifyKeyword(AddInsideMethod(
@"[$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestStatementAttributeList()
        {
            VerifyKeyword(AddInsideMethod(
@"[$$Attr]"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestLocalFunctionAttributeList()
        {
            VerifyKeyword(AddInsideMethod(
@"[$$Attr] void local1() { }"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInLocalFunctionParameterAttributeList()
        {
            VerifyAbsence(AddInsideMethod(
@"void local1([$$Attr] int i) { }"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInLocalFunctionTypeParameterAttributeList()
        {
            VerifyAbsence(AddInsideMethod(
@"void local1<[$$Attr] T>() { }"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestEmptyStatement()
        {
            VerifyKeyword(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterAwait()
        {
            VerifyAbsence(
@"class C
{
    async void M()
    {
        await $$
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestBeforeStatement()
        {
            VerifyKeyword(AddInsideMethod(
@"$$
return true;"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterStatement()
        {
            VerifyKeyword(AddInsideMethod(
@"return true;
$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterBlock()
        {
            VerifyKeyword(AddInsideMethod(
@"if (true) {
}
$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterReturn()
        {
            VerifyAbsence(AddInsideMethod(
@"return $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterYield()
        {
            VerifyKeyword(AddInsideMethod(
@"yield $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInClass()
        {
            VerifyAbsence(@"class C
{
  $$
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInAttributeInsideClass()
        {
            VerifyKeyword(
@"class C {
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInAttributeAfterAttributeInsideClass()
        {
            VerifyKeyword(
@"class C {
    [Goo]
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInAttributeAfterMethod()
        {
            VerifyKeyword(
@"class C {
    void Goo() {
    }
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInAttributeAfterProperty()
        {
            VerifyKeyword(
@"class C {
    int Goo {
        get;
    }
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInAttributeAfterField()
        {
            VerifyKeyword(
@"class C {
    int Goo;
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInAttributeAfterEvent()
        {
            VerifyKeyword(
@"class C {
    event Action<int> Goo;
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInOuterAttribute()
        {
            VerifyAbsence(SourceCodeKind.Regular,
@"[$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInOuterAttributeScripting()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"[$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInParameterAttribute()
        {
            VerifyAbsence(
@"class C {
    void Goo([$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPropertyAttribute()
        {
            VerifyAbsence(
@"class C {
    int Goo { [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInEventAttribute()
        {
            VerifyAbsence(
@"class C {
    event Action<int> Goo { [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInClassReturnParameters()
        {
            VerifyAbsence(
@"class C<[$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInDelegateReturnParameters()
        {
            VerifyAbsence(
@"delegate void D<[$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInMethodReturnParameters()
        {
            VerifyAbsence(
@"class C {
    void M<[$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInInterface()
        {
            VerifyKeyword(
@"interface I {
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInStruct()
        {
            VerifyKeyword(
@"struct S {
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInEnum()
        {
            VerifyAbsence(
@"enum E {
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterElse()
        {
            VerifyKeyword(AddInsideMethod(
@"if (goo) {
} else $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterElseClause()
        {
            VerifyKeyword(AddInsideMethod(
@"if (goo) {
} else {
}
$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterFixed()
        {
            VerifyKeyword(AddInsideMethod(
@"fixed (byte* pResult = result) {
}
$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterSwitch()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (goo) {
}
$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterCatch()
        {
            VerifyKeyword(AddInsideMethod(
@"try {
} catch {
}
$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterFinally()
        {
            VerifyKeyword(AddInsideMethod(
@"try {
} finally {
}
$$"));
        }
    }
}
