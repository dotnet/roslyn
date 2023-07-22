// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class UnsafeKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact]
        public async Task TestAtRoot_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"$$");
        }

        [Fact]
        public async Task TestAfterClass_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact]
        public async Task TestAfterGlobalStatement_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact]
        public async Task TestAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact]
        public async Task TestNotInUsingAlias()
        {
            await VerifyAbsenceAsync(
@"using Goo = $$");
        }

        [Fact]
        public async Task TestNotInGlobalUsingAlias()
        {
            await VerifyAbsenceAsync(
@"global using Goo = $$");
        }

        [Fact]
        public async Task TestInEmptyStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$"));
        }

        [Fact]
        public async Task TestInCompilationUnit()
        {
            await VerifyKeywordAsync(
@"$$");
        }

        [Fact]
        public async Task TestAfterExtern()
        {
            await VerifyKeywordAsync(
@"extern alias Goo;
$$");
        }

        [Fact]
        public async Task TestAfterUsing()
        {
            await VerifyKeywordAsync(
@"using Goo;
$$");
        }

        [Fact]
        public async Task TestAfterGlobalUsing()
        {
            await VerifyKeywordAsync(
@"global using Goo;
$$");
        }

        [Fact]
        public async Task TestAfterNamespace()
        {
            await VerifyKeywordAsync(
@"namespace N {}
$$");
        }

        [Fact]
        public async Task TestAfterFileScopedNamespace()
        {
            await VerifyKeywordAsync(
@"namespace N;
$$");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66319")]
        public async Task TestFileKeywordInsideNamespace()
        {
            await VerifyKeywordAsync(
@"namespace N {
file $$
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66319")]
        public async Task TestFileKeywordInsideNamespaceBeforeClass()
        {
            await VerifyKeywordAsync(
@"namespace N {
file $$
class C {}
}");
        }

        [Fact]
        public async Task TestAfterTypeDeclaration()
        {
            await VerifyKeywordAsync(
@"class C {}
$$");
        }

        [Fact]
        public async Task TestAfterDelegateDeclaration()
        {
            await VerifyKeywordAsync(
@"delegate void Goo();
$$");
        }

        [Fact]
        public async Task TestAfterMethod()
        {
            await VerifyKeywordAsync(
@"class C {
  void Goo() {}
  $$");
        }

        [Fact]
        public async Task TestAfterField()
        {
            await VerifyKeywordAsync(
@"class C {
  int i;
  $$");
        }

        [Fact]
        public async Task TestAfterProperty()
        {
            await VerifyKeywordAsync(
@"class C {
  int i { get; }
  $$");
        }

        [Fact]
        public async Task TestNotBeforeUsing()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
@"$$
using Goo;");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9880")]
        public async Task TestNotBeforeUsing_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$
using Goo;");
        }

        [Fact]
        public async Task TestNotBeforeGlobalUsing()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
@"$$
global using Goo;");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9880")]
        public async Task TestNotBeforeGlobalUsing_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$
global using Goo;");
        }

        [Fact]
        public async Task TestAfterAssemblyAttribute()
        {
            await VerifyKeywordAsync(
@"[assembly: goo]
$$");
        }

        [Fact]
        public async Task TestAfterRootAttribute()
        {
            await VerifyKeywordAsync(
@"[goo]
$$");
        }

        [Fact]
        public async Task TestAfterNestedAttribute()
        {
            await VerifyKeywordAsync(
@"class C {
  [goo]
  $$");
        }

        // This will be fixed once we have accessibility for members
        [Fact]
        public async Task TestInsideStruct()
        {
            await VerifyKeywordAsync(
@"struct S {
   $$");
        }

        [Fact]
        public async Task TestInsideInterface()
        {
            await VerifyKeywordAsync(
@"interface I {
   $$");
        }

        [Fact]
        public async Task TestInsideClass()
        {
            await VerifyKeywordAsync(
@"class C {
   $$");
        }

        [Fact]
        public async Task TestNotAfterPartial()
            => await VerifyAbsenceAsync(@"partial $$");

        [Fact]
        public async Task TestAfterAbstract()
        {
            await VerifyKeywordAsync(
@"abstract $$");
        }

        [Fact]
        public async Task TestAfterInternal()
        {
            await VerifyKeywordAsync(
@"internal $$");
        }

        [Fact]
        public async Task TestNotAfterUnsafe()
            => await VerifyAbsenceAsync(@"unsafe $$");

        [Fact]
        public async Task TestNotAfterStaticUnsafe()
            => await VerifyAbsenceAsync(@"static unsafe $$");

        [Fact]
        public async Task TestNotAfterUnsafeStatic()
            => await VerifyAbsenceAsync(@"unsafe static $$");

        [Fact]
        public async Task TestAfterPrivate()
        {
            await VerifyKeywordAsync(
@"private $$");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66319")]
        public async Task TestAfterFile()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
@"file $$");
        }

        [Fact]
        public async Task TestAfterProtected()
        {
            await VerifyKeywordAsync(
@"protected $$");
        }

        [Fact]
        public async Task TestAfterSealed()
        {
            await VerifyKeywordAsync(
@"sealed $$");
        }

        [Fact]
        public async Task TestAfterStatic()
        {
            await VerifyKeywordAsync(
@"static $$");
        }

        [Fact]
        public async Task TestNotAfterClass()
            => await VerifyAbsenceAsync(@"class $$");

        [Fact]
        public async Task TestNotAfterDelegate()
            => await VerifyAbsenceAsync(@"delegate $$");

        [Fact]
        public async Task TestAfterNestedAbstract()
        {
            await VerifyKeywordAsync(
@"class C {
    abstract $$");
        }

        [Fact]
        public async Task TestAfterNestedVirtual()
        {
            await VerifyKeywordAsync(
@"class C {
    virtual $$");
        }

        [Fact]
        public async Task TestAfterNestedOverride()
        {
            await VerifyKeywordAsync(
@"class C {
    override $$");
        }

        [Fact]
        public async Task TestAfterNestedSealed()
        {
            await VerifyKeywordAsync(
@"class C {
    sealed $$");
        }

        [Fact]
        public async Task TestBeforeStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$
return true;"));
        }

        [Fact]
        public async Task TestAfterStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"return true;
$$"));
        }

        [Fact]
        public async Task TestAfterBlock()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"if (true) {
}
$$"));
        }

        [Fact]
        public async Task TestInsideSwitchBlock()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"switch (E) {
  case 0:
    $$"));
        }

        [Fact]
        public async Task TestNotAfterUnsafe_InMethod()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"unsafe $$"));
        }

        [Fact]
        public async Task TestAfterInterfaceModifiers()
        {
            await VerifyKeywordAsync(
@"public $$ interface IBinaryDocumentMemoryBlock {");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67984")]
        public async Task TestAfterUsingKeywordInUsingDirective()
        {
            await VerifyKeywordAsync("using $$");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67984")]
        public async Task TestAfterStaticKeywordInUsingDirective()
        {
            await VerifyKeywordAsync("using static $$");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67984")]
        public async Task TestAfterUsingKeywordInGlobalUsingDirective()
        {
            await VerifyKeywordAsync("global using $$");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67984")]
        public async Task TestAfterStaticKeywordInGlobalUsingDirective()
        {
            await VerifyKeywordAsync("global using static $$");
        }
    }
}
