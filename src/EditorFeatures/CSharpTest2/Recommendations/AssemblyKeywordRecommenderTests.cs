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
        public async Task TestNotInEmptyStatement()
        {
            VerifyAbsence(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInAttributeInsideClass()
        {
            VerifyAbsence(
@"class C {
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInAttributeAfterAttributeInsideClass()
        {
            VerifyAbsence(
@"class C {
    [Goo]
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInAttributeAfterMethod()
        {
            VerifyAbsence(
@"class C {
    void Goo() {
    }
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInAttributeAfterProperty()
        {
            VerifyAbsence(
@"class C {
    int Goo {
        get;
    }
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInAttributeAfterField()
        {
            VerifyAbsence(
@"class C {
    int Goo;
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInAttributeAfterEvent()
        {
            VerifyAbsence(
@"class C {
    event Action<int> Goo;
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInOuterAttribute()
        {
            VerifyKeyword(
@"[$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(362, "https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestNotInAttributeNestClass()
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
        public async Task TestInAttributeBeforeNamespace()
        {
            VerifyKeyword(
@"[$$
namespace Goo {");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(362, "https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestNotInAttributeBeforeNamespaceWithoutOpenBracket()
        {
            VerifyAbsence(
@"$$
namespace Goo {}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(362, "https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestNotInAttributeBeforeNamespaceAndAfterUsingWithNoOpenBracket()
        {
            VerifyAbsence(
@"
using System;

$$
namespace Goo {}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(362, "https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestInAttributeBeforeNamespaceAndAfterUsingWithOpenBracket()
        {
            VerifyKeyword(
@"
using System;

[$$
namespace Goo {}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(362, "https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestInAttributeBeforeAssemblyWithOpenBracket()
        {
            VerifyKeyword(
@"
[$$
[assembly: Whatever]
namespace Goo {}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(362, "https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestInAttributeBeforeClass()
        {
            VerifyKeyword(
@"
[$$
class Goo {}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(362, "https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestInAttributeBeforeInterface()
        {
            VerifyKeyword(
@"
[$$
interface IGoo {}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(362, "https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestInAttributeBeforeStruct()
        {
            VerifyKeyword(
@"
[$$
struct Goo {}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(362, "https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestInAttributeBeforeEnum()
        {
            VerifyKeyword(
@"
[$$
enum Goo {}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(362, "https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestNotInAttributeBeforeOtherAttributeWithoutOpenBracket()
        {
            VerifyAbsence(
@"
$$
[assembly: Whatever]
namespace Goo {}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(362, "https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestNotInAttributeBeforeAssemblyAttributeAndAfterUsingWithoutOpenBracket()
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
        public async Task TestInBeforeAttributeAssemblyAttributeAndAfterUsingWithoutOpenBracket()
        {
            VerifyKeyword(
@"
using System;

[$$
[assembly: Whatever]
namespace Goo {}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInOuterAttributeInNamespace()
        {
            VerifyAbsence(
@"namespace Goo {
     [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInParameterAttribute()
        {
            VerifyAbsence(
@"class C {
    void Goo([$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(362, "https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestNotInElementAccess()
        {
            VerifyAbsence(
@"class C {
    void Goo(string[] array) {
        array[$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(362, "https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestNotInIndex()
        {
            VerifyAbsence(
@"class C {
    public int this[$$");
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
        public async Task TestNotInClassAssemblyParameters()
        {
            VerifyAbsence(
@"class C<[$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInDelegateAssemblyParameters()
        {
            VerifyAbsence(
@"delegate void D<[$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInMethodAssemblyParameters()
        {
            VerifyAbsence(
@"class C {
    void M<[$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInInterface()
        {
            VerifyAbsence(
@"interface I {
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInStruct()
        {
            VerifyAbsence(
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
    }
}
