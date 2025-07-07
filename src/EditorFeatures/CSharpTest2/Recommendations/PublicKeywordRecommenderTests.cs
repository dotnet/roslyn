// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class PublicKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestAtRoot_Interactive()
        => VerifyKeywordAsync(SourceCodeKind.Script,
@"$$");

    [Fact]
    public Task TestAfterClass_Interactive()
        => VerifyKeywordAsync(SourceCodeKind.Script,
            """
            class C { }
            $$
            """);

    [Fact]
    public Task TestAfterGlobalStatement()
        => VerifyKeywordAsync(
            """
            System.Console.WriteLine();
            $$
            """);

    [Fact]
    public Task TestAfterGlobalVariableDeclaration_Interactive()
        => VerifyKeywordAsync(SourceCodeKind.Script,
            """
            int i = 0;
            $$
            """);

    [Fact]
    public Task TestNotInUsingAlias()
        => VerifyAbsenceAsync(
@"using Goo = $$");

    [Fact]
    public Task TestNotInGlobalUsingAlias()
        => VerifyAbsenceAsync(
@"global using Goo = $$");

    [Fact]
    public Task TestNotInEmptyStatement()
        => VerifyAbsenceAsync(AddInsideMethod(
@"$$"));

    [Fact]
    public Task TestInCompilationUnit()
        => VerifyKeywordAsync(
@"$$");

    [Fact]
    public Task TestAfterExtern()
        => VerifyKeywordAsync(
            """
            extern alias Goo;
            $$
            """);

    [Fact]
    public Task TestAfterUsing()
        => VerifyKeywordAsync(
            """
            using Goo;
            $$
            """);

    [Fact]
    public Task TestAfterGlobalUsing()
        => VerifyKeywordAsync(
            """
            global using Goo;
            $$
            """);

    [Fact]
    public Task TestAfterNamespace()
        => VerifyKeywordAsync(
            """
            namespace N {}
            $$
            """);

    [Fact]
    public Task TestAfterFileScopedNamespace()
        => VerifyKeywordAsync(
            """
            namespace N;
            $$
            """);

    [Fact]
    public Task TestAfterTypeDeclaration()
        => VerifyKeywordAsync(
            """
            class C {}
            $$
            """);

    [Fact]
    public Task TestAfterDelegateDeclaration()
        => VerifyKeywordAsync(
            """
            delegate void Goo();
            $$
            """);

    [Fact]
    public Task TestAfterMethod()
        => VerifyKeywordAsync(
            """
            class C {
              void Goo() {}
              $$
            """);

    [Fact]
    public Task TestAfterField()
        => VerifyKeywordAsync(
            """
            class C {
              int i;
              $$
            """);

    [Fact]
    public Task TestAfterProperty()
        => VerifyKeywordAsync(
            """
            class C {
              int i { get; }
              $$
            """);

    [Fact]
    public Task TestNotBeforeUsing()
        => VerifyAbsenceAsync(SourceCodeKind.Regular,
            """
            $$
            using Goo;
            """);

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9880")]
    public Task TestNotBeforeUsing_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            $$
            using Goo;
            """);

    [Fact]
    public Task TestNotBeforeGlobalUsing()
        => VerifyAbsenceAsync(SourceCodeKind.Regular,
            """
            $$
            global using Goo;
            """);

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9880")]
    public Task TestNotBeforeGlobalUsing_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            $$
            global using Goo;
            """);

    [Fact]
    public Task TestAfterAssemblyAttribute()
        => VerifyKeywordAsync(
            """
            [assembly: goo]
            $$
            """);

    [Fact]
    public Task TestAfterRootAttribute()
        => VerifyKeywordAsync(
            """
            [goo]
            $$
            """);

    [Fact]
    public Task TestAfterNestedAttribute()
        => VerifyKeywordAsync(
            """
            class C {
              [goo]
              $$
            """);

    // This will be fixed once we have accessibility for members
    [Fact]
    public Task TestInsideStruct()
        => VerifyKeywordAsync(
            """
            struct S {
               $$
            """);

    [Fact]
    public Task TestInsideInterface()
        => VerifyKeywordAsync(
            """
            interface I {
               $$
            """);

    [Fact]
    public Task TestInsideClass()
        => VerifyKeywordAsync(
            """
            class C {
               $$
            """);

    [Fact]
    public async Task TestNotAfterPartial()
        => await VerifyAbsenceAsync(@"partial $$");

    [Fact]
    public Task TestAfterAbstract()
        => VerifyKeywordAsync(
@"abstract $$");

    [Fact]
    public async Task TestNotAfterInternal()
        => await VerifyAbsenceAsync(@"internal $$");

    [Fact]
    public async Task TestNotAfterPublic()
        => await VerifyAbsenceAsync(@"public $$");

    [Fact]
    public async Task TestNotAfterStaticPublic()
        => await VerifyAbsenceAsync(@"static public $$");

    [Fact]
    public async Task TestNotAfterPublicStatic()
        => await VerifyAbsenceAsync(@"public static $$");

    [Fact]
    public async Task TestNotAfterInvalidPublic()
        => await VerifyAbsenceAsync(@"virtual public $$");

    [Fact]
    public async Task TestNotAfterPrivate()
        => await VerifyAbsenceAsync(@"private $$");

    [Fact]
    public async Task TestNotAfterProtected()
        => await VerifyAbsenceAsync(@"protected $$");

    [Fact]
    public Task TestAfterSealed()
        => VerifyKeywordAsync(
@"sealed $$");

    [Fact]
    public Task TestAfterStatic()
        => VerifyKeywordAsync(
@"static $$");

    [Fact]
    public Task TestNotAfterStaticInUsingDirective()
        => VerifyAbsenceAsync(
@"using static $$");

    [Fact]
    public Task TestNotAfterStaticInGlobalUsingDirective()
        => VerifyAbsenceAsync(
@"global using static $$");

    [Fact]
    public async Task TestNotAfterClass()
        => await VerifyAbsenceAsync(@"class $$");

    [Fact]
    public async Task TestNotAfterDelegate()
        => await VerifyAbsenceAsync(@"delegate $$");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32214")]
    public Task TestNotBetweenUsings()
        => VerifyAbsenceAsync(SourceCodeKind.Regular,
            """
            using Goo;
            $$
            using Bar;
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32214")]
    public Task TestNotBetweenGlobalUsings_01()
        => VerifyAbsenceAsync(SourceCodeKind.Regular,
            """
            global using Goo;
            $$
            using Bar;
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32214")]
    public Task TestNotBetweenGlobalUsings_02()
        => VerifyAbsenceAsync(SourceCodeKind.Regular,
            """
            global using Goo;
            $$
            global using Bar;
            """);

    [Fact]
    public Task TestAfterNestedAbstract()
        => VerifyKeywordAsync(
            """
            class C {
                abstract $$
            """);

    [Fact]
    public Task TestAfterNestedVirtual()
        => VerifyKeywordAsync(
            """
            class C {
                virtual $$
            """);

    [Fact]
    public Task TestAfterNestedOverride()
        => VerifyKeywordAsync(
            """
            class C {
                override $$
            """);

    [Fact]
    public Task TestAfterNestedSealed()
        => VerifyKeywordAsync(
            """
            class C {
                sealed $$
            """);

    [Fact]
    public Task TestAfterEnum()
        => VerifyKeywordAsync(
            """
            enum E {
            }
            $$
            """);

    [Fact]
    public Task TestAfterDocComment1()
        => VerifyKeywordAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Text;

            /// <summary>
            /// Subclass this class and define private fields for OptionSet
            /// </summary>
            $$
            """);

    [Fact]
    public Task TestAfterDocComment2()
        => VerifyKeywordAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Text;

            /// <summary>
            /// Subclass this class and define private fields for OptionSet
            /// </summary>

            $$
            """);

    [Fact]
    public Task TestAfterDocComment3()
        => VerifyKeywordAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Text;

            /// <summary>
            /// Subclass this class and define private fields for OptionSet
            /// </summary>

            $$
            """);

    [Fact]
    public Task TestAfterDocComment4()
        => VerifyKeywordAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Text;

            /// <summary>
            /// Subclass this class and define private fields for OptionSet
            /// </summary>

            $$
            """);

    [Fact]
    public Task TestAfterComment1()
        => VerifyKeywordAsync(
            """
            // </summary>  
            $$
            """);

    [Fact]
    public Task TestAfterComment2()
        => VerifyKeywordAsync(
            """
            // </summary>  

            $$
            """);

    [Fact]
    public Task TestAfterComment3()
        => VerifyKeywordAsync(
            """
            // </summary>  
            $$
            """);

    [Fact]
    public Task TestAfterComment4()
        => VerifyKeywordAsync(
            """
            // </summary>  

            $$
            """);

    [Fact]
    public Task TestWithinExtension()
        => VerifyKeywordAsync(
            """
            static class C
            {
                extension(string s)
                {
                    $$
                }
            }
            """,
            CSharpNextParseOptions,
            CSharpNextScriptParseOptions);
}
