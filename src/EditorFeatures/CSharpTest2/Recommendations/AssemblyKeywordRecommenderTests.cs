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
    public class AssemblyKeywordRecommenderTests : KeywordRecommenderTests
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
        public void TestNotInAttributeInsideClass()
        {
            VerifyAbsence(
@"class C {
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInAttributeAfterAttributeInsideClass()
        {
            VerifyAbsence(
@"class C {
    [Goo]
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInAttributeAfterMethod()
        {
            VerifyAbsence(
@"class C {
    void Goo() {
    }
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInAttributeAfterProperty()
        {
            VerifyAbsence(
@"class C {
    int Goo {
        get;
    }
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInAttributeAfterField()
        {
            VerifyAbsence(
@"class C {
    int Goo;
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInAttributeAfterEvent()
        {
            VerifyAbsence(
@"class C {
    event Action<int> Goo;
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInOuterAttribute()
        {
            VerifyKeyword(
@"[$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(362, "https://github.com/dotnet/roslyn/issues/362")]
        public void TestNotInAttributeNestClass()
        {
            VerifyAbsence(
@"class A
{
    [$$
    class B
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(362, "https://github.com/dotnet/roslyn/issues/362")]
        public void TestInAttributeBeforeNamespace()
        {
            VerifyKeyword(
@"[$$
namespace Goo {");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(362, "https://github.com/dotnet/roslyn/issues/362")]
        public void TestNotInAttributeBeforeNamespaceWithoutOpenBracket()
        {
            VerifyAbsence(
@"$$
namespace Goo {}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(362, "https://github.com/dotnet/roslyn/issues/362")]
        public void TestNotInAttributeBeforeNamespaceAndAfterUsingWithNoOpenBracket()
        {
            VerifyAbsence(
@"
using System;

$$
namespace Goo {}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(362, "https://github.com/dotnet/roslyn/issues/362")]
        public void TestInAttributeBeforeNamespaceAndAfterUsingWithOpenBracket()
        {
            VerifyKeyword(
@"
using System;

[$$
namespace Goo {}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(362, "https://github.com/dotnet/roslyn/issues/362")]
        public void TestInAttributeBeforeAssemblyWithOpenBracket()
        {
            VerifyKeyword(
@"
[$$
[assembly: Whatever]
namespace Goo {}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(362, "https://github.com/dotnet/roslyn/issues/362")]
        public void TestInAttributeBeforeClass()
        {
            VerifyKeyword(
@"
[$$
class Goo {}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(362, "https://github.com/dotnet/roslyn/issues/362")]
        public void TestInAttributeBeforeInterface()
        {
            VerifyKeyword(
@"
[$$
interface IGoo {}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(362, "https://github.com/dotnet/roslyn/issues/362")]
        public void TestInAttributeBeforeStruct()
        {
            VerifyKeyword(
@"
[$$
struct Goo {}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(362, "https://github.com/dotnet/roslyn/issues/362")]
        public void TestInAttributeBeforeEnum()
        {
            VerifyKeyword(
@"
[$$
enum Goo {}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(362, "https://github.com/dotnet/roslyn/issues/362")]
        public void TestNotInAttributeBeforeOtherAttributeWithoutOpenBracket()
        {
            VerifyAbsence(
@"
$$
[assembly: Whatever]
namespace Goo {}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(362, "https://github.com/dotnet/roslyn/issues/362")]
        public void TestNotInAttributeBeforeAssemblyAttributeAndAfterUsingWithoutOpenBracket()
        {
            VerifyAbsence(
@"
using System;

$$
[assembly: Whatever]
namespace Goo {}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(362, "https://github.com/dotnet/roslyn/issues/362")]
        public void TestInBeforeAttributeAssemblyAttributeAndAfterUsingWithoutOpenBracket()
        {
            VerifyKeyword(
@"
using System;

[$$
[assembly: Whatever]
namespace Goo {}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInOuterAttributeInNamespace()
        {
            VerifyAbsence(
@"namespace Goo {
     [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInParameterAttribute()
        {
            VerifyAbsence(
@"class C {
    void Goo([$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(362, "https://github.com/dotnet/roslyn/issues/362")]
        public void TestNotInElementAccess()
        {
            VerifyAbsence(
@"class C {
    void Goo(string[] array) {
        array[$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(362, "https://github.com/dotnet/roslyn/issues/362")]
        public void TestNotInIndex()
        {
            VerifyAbsence(
@"class C {
    public int this[$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInPropertyAttribute()
        {
            VerifyAbsence(
@"class C {
    int Goo { [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInEventAttribute()
        {
            VerifyAbsence(
@"class C {
    event Action<int> Goo { [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInClassAssemblyParameters()
        {
            VerifyAbsence(
@"class C<[$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInDelegateAssemblyParameters()
        {
            VerifyAbsence(
@"delegate void D<[$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInMethodAssemblyParameters()
        {
            VerifyAbsence(
@"class C {
    void M<[$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInInterface()
        {
            VerifyAbsence(
@"interface I {
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInStruct()
        {
            VerifyAbsence(
@"struct S {
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInEnum()
        {
            VerifyAbsence(
@"enum E {
    [$$");
        }
    }
}
