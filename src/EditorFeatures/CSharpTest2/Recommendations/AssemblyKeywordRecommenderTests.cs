// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterClass_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobalStatement_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInUsingAlias()
        {
            await VerifyAbsenceAsync(
@"using Goo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInEmptyStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInAttributeInsideClass()
        {
            await VerifyAbsenceAsync(
@"class C {
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInAttributeAfterAttributeInsideClass()
        {
            await VerifyAbsenceAsync(
@"class C {
    [Goo]
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInAttributeAfterMethod()
        {
            await VerifyAbsenceAsync(
@"class C {
    void Goo() {
    }
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInAttributeAfterProperty()
        {
            await VerifyAbsenceAsync(
@"class C {
    int Goo {
        get;
    }
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInAttributeAfterField()
        {
            await VerifyAbsenceAsync(
@"class C {
    int Goo;
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInAttributeAfterEvent()
        {
            await VerifyAbsenceAsync(
@"class C {
    event Action<int> Goo;
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInOuterAttribute()
        {
            await VerifyKeywordAsync(
@"[$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(362, "https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestBeforeAttributeNestClass()
        {
            await VerifyAbsenceAsync(
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
        public async Task TestBeforeAttributeNamespace()
        {
            await VerifyKeywordAsync(
@"[$$
namespace Goo {}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(362, "https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestNotBeforeAttributeNamespaceWithNoOpenBracket()
        {
            await VerifyAbsenceAsync(
@"$$
namespace Goo {}");
        }


        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(362, "https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestNotBeforeAttributeNamespaceAndAfterUsingWithNoOpenBracket()
        {
            await VerifyAbsenceAsync(
@"
using System;

$$
namespace Goo {}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(362, "https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestBeforeAttributeNamespaceAndAfterUsingWithNoOpenBracket()
        {
            await VerifyKeywordAsync(
@"
using System;

[$$
namespace Goo {}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(362, "https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestBeforeAssemblyAttributeWithOpenBracket()
        {
            await VerifyKeywordAsync(
@"
[$$
[assembly: Whatever]
namespace Goo {}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(362, "https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestNotBeforeAssemblyAttributeWithoutOpenBracket()
        {
            await VerifyAbsenceAsync(
@"
$$
[assembly: Whatever]
namespace Goo {}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(362, "https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestNotBeforeAssemblyAttributeAndAfterUsingWithoutOpenBracket()
        {
            await VerifyAbsenceAsync(
@"
using System;

$$
[assembly: Whatever]
namespace Goo {}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(362, "https://github.com/dotnet/roslyn/issues/362")]
        public async Task TestBeforeAssemblyAttributeAndAfterUsingWithoutOpenBracket()
        {
            await VerifyKeywordAsync(
@"
using System;

[$$
[assembly: Whatever]
namespace Goo {}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInOuterAttributeInNamespace()
        {
            await VerifyAbsenceAsync(
@"namespace Goo {
     [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInParameterAttribute()
        {
            await VerifyAbsenceAsync(
@"class C {
    void Goo([$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPropertyAttribute()
        {
            await VerifyAbsenceAsync(
@"class C {
    int Goo { [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInEventAttribute()
        {
            await VerifyAbsenceAsync(
@"class C {
    event Action<int> Goo { [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInClassAssemblyParameters()
        {
            await VerifyAbsenceAsync(
@"class C<[$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInDelegateAssemblyParameters()
        {
            await VerifyAbsenceAsync(
@"delegate void D<[$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInMethodAssemblyParameters()
        {
            await VerifyAbsenceAsync(
@"class C {
    void M<[$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInInterface()
        {
            await VerifyAbsenceAsync(
@"interface I {
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInStruct()
        {
            await VerifyAbsenceAsync(
@"struct S {
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInEnum()
        {
            await VerifyAbsenceAsync(
@"enum E {
    [$$");
        }
    }
}
