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
    public class PublicKeywordRecommenderTests : KeywordRecommenderTests
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
                """
                class C { }
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalStatement_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
                """
                System.Console.WriteLine();
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
                """
                int i = 0;
                $$
                """);
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
        public async Task TestNotInEmptyStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
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
                """
                extern alias Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterUsing()
        {
            await VerifyKeywordAsync(
                """
                using Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalUsing()
        {
            await VerifyKeywordAsync(
                """
                global using Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterNamespace()
        {
            await VerifyKeywordAsync(
                """
                namespace N {}
                $$
                """);
        }

        [Fact]
        public async Task TestAfterFileScopedNamespace()
        {
            await VerifyKeywordAsync(
                """
                namespace N;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterTypeDeclaration()
        {
            await VerifyKeywordAsync(
                """
                class C {}
                $$
                """);
        }

        [Fact]
        public async Task TestAfterDelegateDeclaration()
        {
            await VerifyKeywordAsync(
                """
                delegate void Goo();
                $$
                """);
        }

        [Fact]
        public async Task TestAfterMethod()
        {
            await VerifyKeywordAsync(
                """
                class C {
                  void Goo() {}
                  $$
                """);
        }

        [Fact]
        public async Task TestAfterField()
        {
            await VerifyKeywordAsync(
                """
                class C {
                  int i;
                  $$
                """);
        }

        [Fact]
        public async Task TestAfterProperty()
        {
            await VerifyKeywordAsync(
                """
                class C {
                  int i { get; }
                  $$
                """);
        }

        [Fact]
        public async Task TestNotBeforeUsing()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
                """
                $$
                using Goo;
                """);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9880")]
        public async Task TestNotBeforeUsing_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                $$
                using Goo;
                """);
        }

        [Fact]
        public async Task TestNotBeforeGlobalUsing()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
                """
                $$
                global using Goo;
                """);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9880")]
        public async Task TestNotBeforeGlobalUsing_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                $$
                global using Goo;
                """);
        }

        [Fact]
        public async Task TestAfterAssemblyAttribute()
        {
            await VerifyKeywordAsync(
                """
                [assembly: goo]
                $$
                """);
        }

        [Fact]
        public async Task TestAfterRootAttribute()
        {
            await VerifyKeywordAsync(
                """
                [goo]
                $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedAttribute()
        {
            await VerifyKeywordAsync(
                """
                class C {
                  [goo]
                  $$
                """);
        }

        // This will be fixed once we have accessibility for members
        [Fact]
        public async Task TestInsideStruct()
        {
            await VerifyKeywordAsync(
                """
                struct S {
                   $$
                """);
        }

        [Fact]
        public async Task TestInsideInterface()
        {
            await VerifyKeywordAsync(
                """
                interface I {
                   $$
                """);
        }

        [Fact]
        public async Task TestInsideClass()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   $$
                """);
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
        public async Task TestNotAfterStaticInUsingDirective()
        {
            await VerifyAbsenceAsync(
@"using static $$");
        }

        [Fact]
        public async Task TestNotAfterStaticInGlobalUsingDirective()
        {
            await VerifyAbsenceAsync(
@"global using static $$");
        }

        [Fact]
        public async Task TestNotAfterClass()
            => await VerifyAbsenceAsync(@"class $$");

        [Fact]
        public async Task TestNotAfterDelegate()
            => await VerifyAbsenceAsync(@"delegate $$");

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32214")]
        public async Task TestNotBetweenUsings()
        {
            // Recommendation in scripting is not stable. See https://github.com/dotnet/roslyn/issues/32214
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
                """
                using Goo;
                $$
                using Bar;
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32214")]
        public async Task TestNotBetweenGlobalUsings_01()
        {
            // Recommendation in scripting is not stable. See https://github.com/dotnet/roslyn/issues/32214
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
                """
                global using Goo;
                $$
                using Bar;
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32214")]
        public async Task TestNotBetweenGlobalUsings_02()
        {
            // Recommendation in scripting is not stable. See https://github.com/dotnet/roslyn/issues/32214
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
                """
                global using Goo;
                $$
                global using Bar;
                """);
        }

        [Fact]
        public async Task TestAfterNestedAbstract()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    abstract $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedVirtual()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    virtual $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedOverride()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    override $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedSealed()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    sealed $$
                """);
        }

        [Fact]
        public async Task TestAfterEnum()
        {
            await VerifyKeywordAsync(
                """
                enum E {
                }
                $$
                """);
        }

        [Fact]
        public async Task TestAfterDocComment1()
        {
            await VerifyKeywordAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Text;

                /// <summary>
                /// Subclass this class and define private fields for OptionSet
                /// </summary>
                $$
                """);
        }

        [Fact]
        public async Task TestAfterDocComment2()
        {
            await VerifyKeywordAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Text;

                /// <summary>
                /// Subclass this class and define private fields for OptionSet
                /// </summary>

                $$
                """);
        }

        [Fact]
        public async Task TestAfterDocComment3()
        {
            await VerifyKeywordAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Text;

                /// <summary>
                /// Subclass this class and define private fields for OptionSet
                /// </summary>

                $$
                """);
        }

        [Fact]
        public async Task TestAfterDocComment4()
        {
            await VerifyKeywordAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Text;

                /// <summary>
                /// Subclass this class and define private fields for OptionSet
                /// </summary>

                $$
                """);
        }

        [Fact]
        public async Task TestAfterComment1()
        {
            await VerifyKeywordAsync(
                """
                // </summary>  
                $$
                """);
        }

        [Fact]
        public async Task TestAfterComment2()
        {
            await VerifyKeywordAsync(
                """
                // </summary>  

                $$
                """);
        }

        [Fact]
        public async Task TestAfterComment3()
        {
            await VerifyKeywordAsync(
                """
                // </summary>  
                $$
                """);
        }

        [Fact]
        public async Task TestAfterComment4()
        {
            await VerifyKeywordAsync(
                """
                // </summary>  

                $$
                """);
        }
    }
}
