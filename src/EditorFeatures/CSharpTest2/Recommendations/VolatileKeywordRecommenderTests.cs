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
    public class VolatileKeywordRecommenderTests : KeywordRecommenderTests
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
        public async Task TestNotInCompilationUnit()
            => VerifyAbsence(SourceCodeKind.Regular, @"$$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterExtern()
        {
            VerifyAbsence(SourceCodeKind.Regular, @"extern alias Goo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterExtern_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script, @"extern alias Goo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterUsing()
        {
            VerifyAbsence(SourceCodeKind.Regular, @"using Goo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterUsing_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script, @"using Goo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterNamespace()
        {
            VerifyAbsence(SourceCodeKind.Regular, @"namespace N {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterTypeDeclaration()
        {
            VerifyAbsence(SourceCodeKind.Regular, @"class C {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterDelegateDeclaration()
        {
            VerifyAbsence(SourceCodeKind.Regular, @"delegate void Goo();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterMethod()
        {
            VerifyKeyword(
@"class C {
  void Goo() {}
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterField()
        {
            VerifyKeyword(
@"class C {
  int i;
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterProperty()
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
        public async Task TestNotAfterAssemblyAttribute()
        {
            VerifyAbsence(SourceCodeKind.Regular, @"[assembly: goo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterAssemblyAttribute_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script, @"[assembly: goo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterRootAttribute()
        {
            VerifyAbsence(SourceCodeKind.Regular, @"[goo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterRootAttribute_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script, @"[goo]
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
        public async Task TestNotInsideEnum()
        {
            VerifyAbsence(@"enum E {
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
        public async Task TestNotAfterInternal()
            => VerifyAbsence(SourceCodeKind.Regular, @"internal $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterInternal_Interactive()
            => VerifyKeyword(SourceCodeKind.Script, @"internal $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedInternal()
        {
            VerifyKeyword(
@"class C {
    internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterPublic()
            => VerifyAbsence(SourceCodeKind.Regular, @"public $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPublic_Interactive()
            => VerifyKeyword(SourceCodeKind.Script, @"public $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedPublic()
        {
            VerifyKeyword(
@"class C {
    public $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterPrivate()
        {
            VerifyAbsence(SourceCodeKind.Regular,
@"private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPrivate_Script()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedPrivate()
        {
            VerifyKeyword(
@"class C {
    private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterProtected()
        {
            VerifyAbsence(
@"protected $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedProtected()
        {
            VerifyKeyword(
@"class C {
    protected $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterSealed()
            => VerifyAbsence(@"sealed $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterNestedSealed()
        {
            VerifyAbsence(
@"class C {
    sealed $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterStatic()
            => VerifyAbsence(SourceCodeKind.Regular, @"static $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterStatic_Interactive()
            => VerifyKeyword(SourceCodeKind.Script, @"static $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedStatic()
        {
            VerifyKeyword(
@"class C {
    static $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterStaticPublic()
            => VerifyAbsence(SourceCodeKind.Regular, @"static public $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterStaticPublic_Interactive()
            => VerifyKeyword(SourceCodeKind.Script, @"static public $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedStaticPublic()
        {
            VerifyKeyword(
@"class C {
    static public $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterDelegate()
            => VerifyAbsence(@"delegate $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterEvent()
        {
            VerifyAbsence(
@"class C {
    event $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterConst()
        {
            VerifyAbsence(
@"class C {
    const $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterVolatile()
        {
            VerifyAbsence(
@"class C {
    volatile $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterNew()
        {
            VerifyAbsence(
@"new $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedNew()
        {
            VerifyKeyword(
@"class C {
   new $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInMethod()
        {
            VerifyAbsence(
@"class C {
   void Goo() {
     $$");
        }
    }
}
