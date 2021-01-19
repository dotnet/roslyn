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
    public class AbstractKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAtRoot_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterClass_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGlobalStatement_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
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
        public async Task TestInCompilationUnit()
        {
            VerifyKeyword(
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterExtern()
        {
            VerifyKeyword(
@"extern alias Goo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterUsing()
        {
            VerifyKeyword(
@"using Goo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNamespace()
        {
            VerifyKeyword(
@"namespace N {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterTypeDeclaration()
        {
            VerifyKeyword(
@"class C {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterDelegateDeclaration()
        {
            VerifyKeyword(
@"delegate void Goo();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterMethodInClass()
        {
            VerifyKeyword(
@"class C {
  void Goo() {}
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterFieldInClass()
        {
            VerifyKeyword(
@"class C {
  int i;
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPropertyInClass()
        {
            VerifyKeyword(
@"class C {
  int i { get; }
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotBeforeUsing()
        {
            VerifyAbsence(SourceCodeKind.Regular,
@"$$
using Goo;");
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/9880"), Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotBeforeUsing_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$
using Goo;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterAssemblyAttribute()
        {
            VerifyKeyword(
@"[assembly: goo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterRootAttribute()
        {
            VerifyKeyword(
@"[goo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedAttribute()
        {
            VerifyKeyword(
@"class C {
  [goo]
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideStruct()
        {
            VerifyKeyword(
@"struct S {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideInterface()
        {
            VerifyKeyword(@"interface I {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideClass()
        {
            VerifyKeyword(
@"class C {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterPartial()
            => VerifyAbsence(@"partial $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterAbstract()
            => VerifyAbsence(@"abstract $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterInternal()
        {
            VerifyKeyword(
@"internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPublic()
        {
            VerifyKeyword(
@"public $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterStaticInternal()
            => VerifyAbsence(@"static internal $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterInternalStatic()
            => VerifyAbsence(@"internal static $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterInvalidInternal()
            => VerifyAbsence(@"virtual internal $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterClass()
            => VerifyAbsence(@"class $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPrivate()
        {
            VerifyKeyword(
@"private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterSealed()
            => VerifyAbsence(@"sealed $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterStatic()
            => VerifyAbsence(@"static $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterNestedStatic()
        {
            VerifyAbsence(@"class C {
    static $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedInternal()
        {
            VerifyKeyword(
@"class C {
    internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterDelegate()
            => VerifyAbsence(@"delegate $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterNestedAbstract()
        {
            VerifyAbsence(@"class C {
    abstract $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterNestedVirtual()
        {
            VerifyAbsence(@"class C {
    virtual $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterNestedSealed()
        {
            VerifyAbsence(@"class C {
    sealed $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInProperty()
        {
            VerifyAbsence(
@"class C {
    int Goo { $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPropertyAfterAccessor()
        {
            VerifyAbsence(
@"class C {
    int Goo { get; $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPropertyAfterAccessibility()
        {
            VerifyAbsence(
@"class C {
    int Goo { get; protected $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPropertyAfterInternal()
        {
            VerifyAbsence(
@"class C {
    int Goo { get; internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterOverride()
        {
            VerifyKeyword(
@"class C {
    override $$");
        }
    }
}
