// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class InternalKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAtRoot_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterClass_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGlobalStatement_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInUsingAlias()
        {
            await VerifyAbsenceAsync(
@"using Foo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInEmptyStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInCompilationUnit()
        {
            await VerifyKeywordAsync(
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterExtern()
        {
            await VerifyKeywordAsync(
@"extern alias Foo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterUsing()
        {
            await VerifyKeywordAsync(
@"using Foo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNamespace()
        {
            await VerifyKeywordAsync(
@"namespace N {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterTypeDeclaration()
        {
            await VerifyKeywordAsync(
@"class C {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterDelegateDeclaration()
        {
            await VerifyKeywordAsync(
@"delegate void Foo();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterMethod()
        {
            await VerifyKeywordAsync(
@"class C {
  void Foo() {}
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterField()
        {
            await VerifyKeywordAsync(
@"class C {
  int i;
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterProperty()
        {
            await VerifyKeywordAsync(
@"class C {
  int i { get; }
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotBeforeUsing()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
@"$$
using Foo;");
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/9880"), Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotBeforeUsing_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$
using Foo;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterAssemblyAttribute()
        {
            await VerifyKeywordAsync(
@"[assembly: foo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterRootAttribute()
        {
            await VerifyKeywordAsync(
@"[foo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedAttribute()
        {
            await VerifyKeywordAsync(
@"class C {
  [foo]
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideStruct()
        {
            await VerifyKeywordAsync(
@"struct S {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInsideInterface()
        {
            await VerifyAbsenceAsync(@"interface I {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideClass()
        {
            await VerifyKeywordAsync(
@"class C {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterPartial()
        {
            await VerifyAbsenceAsync(@"partial $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterAbstract()
        {
            await VerifyKeywordAsync(
@"abstract $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterInternal()
        {
            await VerifyAbsenceAsync(@"internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterPublic()
        {
            await VerifyAbsenceAsync(@"public $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterStaticInternal()
        {
            await VerifyAbsenceAsync(@"static internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterInternalStatic()
        {
            await VerifyAbsenceAsync(@"internal static $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterInvalidInternal()
        {
            await VerifyAbsenceAsync(@"virtual internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterPrivate()
        {
            await VerifyAbsenceAsync(@"private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterProtected()
        {
            await VerifyKeywordAsync(
@"protected $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterSealed()
        {
            await VerifyKeywordAsync(
@"sealed $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterStatic()
        {
            await VerifyKeywordAsync(
@"static $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterStaticInUsingDirective()
        {
            await VerifyAbsenceAsync(
@"using static $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterClass()
        {
            await VerifyAbsenceAsync(@"class $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterDelegate()
        {
            await VerifyAbsenceAsync(@"delegate $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedAbstract()
        {
            await VerifyKeywordAsync(
@"class C {
    abstract $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedVirtual()
        {
            await VerifyKeywordAsync(
@"class C {
    virtual $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedOverride()
        {
            await VerifyKeywordAsync(
@"class C {
    override $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedSealed()
        {
            await VerifyKeywordAsync(
@"class C {
    sealed $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInProperty()
        {
            await VerifyKeywordAsync(
@"class C {
    int Foo { $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInPropertyAfterAccessor()
        {
            await VerifyKeywordAsync(
@"class C {
    int Foo { get; $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPropertyAfterInternal()
        {
            await VerifyAbsenceAsync(
@"class C {
    int Foo { get; internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInPropertyAfterProtected()
        {
            await VerifyKeywordAsync(
@"class C {
    int Foo { get; protected $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInIndexer()
        {
            await VerifyKeywordAsync(
@"class C {
    int this[int i] { $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInIndexerAfterAccessor()
        {
            await VerifyKeywordAsync(
@"class C {
    int this[int i] { get { } $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInIndexerAfterInternal()
        {
            await VerifyAbsenceAsync(
@"class C {
    int this[int i] { get { } internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInIndexerAfterProtected()
        {
            await VerifyKeywordAsync(
@"class C {
    int this[int i] { get { } protected $$");
        }
    }
}
