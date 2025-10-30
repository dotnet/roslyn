// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UseSystemHashCode;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseSystemHashCode;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseSystemHashCode)]
public sealed partial class UseSystemHashCodeTests(ITestOutputHelper logger)
    : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new UseSystemHashCodeDiagnosticAnalyzer(), new UseSystemHashCodeCodeFixProvider());

    private new Task TestInRegularAndScriptAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string initialMarkup,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string expectedMarkup,
        int index = 0,
        TestParameters? parameters = null)
    {
        return base.TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, index, parameters);
    }

    private new Task TestMissingAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string initialMarkup,
        TestParameters? parameters = null,
        int codeActionIndex = 0)
    {
        return base.TestMissingAsync(initialMarkup, parameters, codeActionIndex);
    }

    [Fact]
    public Task TestDerivedClassWithFieldWithBase()
        => TestInRegularAndScriptAsync(
            """
            namespace System { public struct HashCode { } }

            class B
            {
                public override int GetHashCode() => 0;
            }

            class C : B
            {
                int j;

                public override int $$GetHashCode()
                {
                    var hashCode = 339610899;
                    hashCode = hashCode * -1521134295 + base.GetHashCode();
                    hashCode = hashCode * -1521134295 + j.GetHashCode();
                    return hashCode;
                }
            }
            """,
            """
            namespace System { public struct HashCode { } }

            class B
            {
                public override int GetHashCode() => 0;
            }

            class C : B
            {
                int j;

                public override int GetHashCode()
                {
                    return System.HashCode.Combine(base.GetHashCode(), j);
                }
            }
            """);

    [Fact]
    public Task TestDerivedClassWithFieldWithNoBase()
        => TestInRegularAndScriptAsync(
            """
            namespace System { public struct HashCode { } }

            class B
            {
                public override int GetHashCode() => 0;
            }

            class C : B
            {
                int j;

                public override int $$GetHashCode()
                {
                    var hashCode = 339610899;
                    hashCode = hashCode * -1521134295 + j.GetHashCode();
                    return hashCode;
                }
            }
            """,
            """
            namespace System { public struct HashCode { } }

            class B
            {
                public override int GetHashCode() => 0;
            }

            class C : B
            {
                int j;

                public override int GetHashCode()
                {
                    return System.HashCode.Combine(j);
                }
            }
            """);

    [Fact]
    public Task TestDerivedClassWithNoFieldWithBase()
        => TestInRegularAndScriptAsync(
            """
            namespace System { public struct HashCode { } }

            class B
            {
                public override int GetHashCode() => 0;
            }

            class C : B
            {
                int j;

                public override int $$GetHashCode()
                {
                    var hashCode = 339610899;
                    hashCode = hashCode * -1521134295 + base.GetHashCode();
                    return hashCode;
                }
            }
            """,
            """
            namespace System { public struct HashCode { } }

            class B
            {
                public override int GetHashCode() => 0;
            }

            class C : B
            {
                int j;

                public override int GetHashCode()
                {
                    return System.HashCode.Combine(base.GetHashCode());
                }
            }
            """);

    [Fact]
    public Task TestFieldAndProp()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            class C 
            {
                int i;

                string S { get; }

                public override int $$GetHashCode()
                {
                    var hashCode = -538000506;
                    hashCode = hashCode * -1521134295 + i.GetHashCode();
                    hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(S);
                    return hashCode;
                }
            }
            """,
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            class C 
            {
                int i;

                string S { get; }

                public override int GetHashCode()
                {
                    return System.HashCode.Combine(i, S);
                }
            }
            """);

    [Fact]
    public Task TestUnchecked()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            class C 
            {
                int i;

                string S { get; }

                public override int $$GetHashCode()
                {
                    unchecked
                    {
                        var hashCode = -538000506;
                        hashCode = hashCode * -1521134295 + i.GetHashCode();
                        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(S);
                        return hashCode;
                    }
                }
            }
            """,
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            class C 
            {
                int i;

                string S { get; }

                public override int GetHashCode()
                {
                    return System.HashCode.Combine(i, S);
                }
            }
            """);

    [Fact]
    public Task TestNotOnNonGetHashCode()
        => TestMissingAsync(
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            class C 
            {
                int i;

                string S { get; }

                public override int $$GetHashCode1()
                {
                    var hashCode = -538000506;
                    hashCode = hashCode * -1521134295 + i.GetHashCode();
                    hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(S);
                    return hashCode;
                }
            }
            """);

    [Fact]
    public Task TestNotWithoutReturn()
        => TestMissingAsync(
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            class C 
            {
                int i;

                string S { get; }

                public override int $$GetHashCode()
                {
                    var hashCode = -538000506;
                    hashCode = hashCode * -1521134295 + i.GetHashCode();
                    hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(S);
                }
            }
            """);

    [Fact]
    public Task TestNotWithoutLocal()
        => TestMissingAsync(
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            class C 
            {
                int i;

                string S { get; }

                public override int $$GetHashCode()
                {
                    hashCode = hashCode * -1521134295 + i.GetHashCode();
                    hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(S);
                    return hashCode;
                }
            }
            """);

    [Fact]
    public Task TestNotWithMultipleLocals()
        => TestMissingAsync(
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            class C 
            {
                int i;

                string S { get; }

                public override int $$GetHashCode()
                {
                    var hashCode = -538000506, x;
                    hashCode = hashCode * -1521134295 + i.GetHashCode();
                    hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(S);
                    return hashCode;
                }
            }
            """);

    [Fact]
    public Task TestNotWithoutInitializer()
        => TestMissingAsync(
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            class C 
            {
                int i;

                string S { get; }

                public override int $$GetHashCode()
                {
                    var hashCode;
                    hashCode = hashCode * -1521134295 + i.GetHashCode();
                    hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(S);
                    return hashCode;
                }
            }
            """);

    [Fact]
    public Task TestNotReturningAccumulator()
        => TestMissingAsync(
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            class C 
            {
                int i;

                string S { get; }

                public override int $$GetHashCode()
                {
                    var hashCode = -538000506;
                    hashCode = hashCode * -1521134295 + i.GetHashCode();
                    hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(S);
                    return 0;
                }
            }
            """);

    [Fact]
    public Task TestAcumulatorInitializedToField()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            class C 
            {
                int i;

                string S { get; }

                public override int $$GetHashCode()
                {
                    var hashCode = i;
                    hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(S);
                    return hashCode;
                }
            }
            """,
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            class C 
            {
                int i;

                string S { get; }

                public override int GetHashCode()
                {
                    return System.HashCode.Combine(i, S);
                }
            }
            """);

    [Fact]
    public Task TestAcumulatorInitializedToHashedField()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            class C 
            {
                int i;

                string S { get; }

                public override int $$GetHashCode()
                {
                    var hashCode = i.GetHashCode();
                    hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(S);
                    return hashCode;
                }
            }
            """,
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            class C 
            {
                int i;

                string S { get; }

                public override int GetHashCode()
                {
                    return System.HashCode.Combine(i, S);
                }
            }
            """);

    [Fact]
    public Task TestMissingOnThisGetHashCode()
        => TestMissingAsync(
            """
            namespace System { public struct HashCode { } }

            class B
            {
                public override int GetHashCode() => 0;
            }

            class C : B
            {
                int j;

                public override int $$GetHashCode()
                {
                    var hashCode = 339610899;
                    hashCode = hashCode * -1521134295 + this.GetHashCode();
                    hashCode = hashCode * -1521134295 + j.GetHashCode();
                    return hashCode;
                }
            }
            """);

    [Fact]
    public Task TestMissingWithNoSystemHashCode()
        => TestMissingAsync(
            """
            class B
            {
                public override int GetHashCode() => 0;
            }

            class C : B
            {
                int j;

                public override int $$GetHashCode()
                {
                    var hashCode = 339610899;
                    hashCode = hashCode * -1521134295 + base.GetHashCode();
                    hashCode = hashCode * -1521134295 + j.GetHashCode();
                    return hashCode;
                }
            }
            """);

    [Fact]
    public Task TestDirectNullCheck1()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            class C 
            {
                int i;

                string S { get; }

                public override int $$GetHashCode()
                {
                    var hashCode = -538000506;
                    hashCode = hashCode * -1521134295 + i.GetHashCode();
                    hashCode = hashCode * -1521134295 + (S != null ? S.GetHashCode() : 0);
                    return hashCode;
                }
            }
            """,
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            class C 
            {
                int i;

                string S { get; }

                public override int GetHashCode()
                {
                    return System.HashCode.Combine(i, S);
                }
            }
            """);

    [Fact]
    public Task TestDirectNullCheck2()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            class C 
            {
                int i;

                string S { get; }

                public override int $$GetHashCode()
                {
                    var hashCode = -538000506;
                    hashCode = hashCode * -1521134295 + i.GetHashCode();
                    hashCode = hashCode * -1521134295 + (S == null ? 0 : S.GetHashCode());
                    return hashCode;
                }
            }
            """,
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            class C 
            {
                int i;

                string S { get; }

                public override int GetHashCode()
                {
                    return System.HashCode.Combine(i, S);
                }
            }
            """);

    [Fact]
    public Task TestInt64Pattern()
        => TestInRegularAndScriptAsync(
            """
            namespace System { public struct HashCode { } }

            class C
            {
                int j;

                public override int $$GetHashCode()
                {
                    long hashCode = -468965076;
                    hashCode = (hashCode * -1521134295 + j.GetHashCode()).GetHashCode();
                    return hashCode;
                }
            }
            """,
            """
            namespace System { public struct HashCode { } }

            class C
            {
                int j;

                public override int GetHashCode()
                {
                    return System.HashCode.Combine(j);
                }
            }
            """);

    [Fact]
    public Task TestInt64Pattern2()
        => TestInRegularAndScriptAsync(
            """
            namespace System { public struct HashCode { } }

            class C
            {
                int j;

                public override int $$GetHashCode()
                {
                    long hashCode = -468965076;
                    hashCode = (hashCode * -1521134295 + j.GetHashCode()).GetHashCode();
                    return (int)hashCode;
                }
            }
            """,
            """
            namespace System { public struct HashCode { } }

            class C
            {
                int j;

                public override int GetHashCode()
                {
                    return System.HashCode.Combine(j);
                }
            }
            """);

    [Fact]
    public Task TestTuple()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            class C 
            {
                int i;

                string S { get; }

                public override int $$GetHashCode()
                {
                    return (i, S).GetHashCode();
                }
            }
            """,
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            class C 
            {
                int i;

                string S { get; }

                public override int GetHashCode()
                {
                    return System.HashCode.Combine(i, S);
                }
            }
            """);

    [Fact]
    public Task TestNullable1()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            class C 
            {
                int i;

                string? S { get; }

                public override int $$GetHashCode()
                {
                    var hashCode = -538000506;
                    hashCode = hashCode * -1521134295 + i.GetHashCode();
                    hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(S);
                    return hashCode;
                }
            }
            """,
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            class C 
            {
                int i;

                string? S { get; }

                public override int GetHashCode()
                {
                    return System.HashCode.Combine(i, S);
                }
            }
            """);

    [Fact]
    public Task TestNullable2()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            class C 
            {
                int i;

                string? S { get; }

                public override int $$GetHashCode()
                {
                    var hashCode = -538000506;
                    hashCode = hashCode * -1521134295 + i.GetHashCode();
                    hashCode = hashCode * -1521134295 + EqualityComparer<string?>.Default.GetHashCode(S);
                    return hashCode;
                }
            }
            """,
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            class C 
            {
                int i;

                string? S { get; }

                public override int GetHashCode()
                {
                    return System.HashCode.Combine(i, S);
                }
            }
            """);

    [Fact]
    public Task TestNullable3()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            class C 
            {
                int i;

                string? S { get; }

                public override int $$GetHashCode()
                {
                    var hashCode = -538000506;
                    hashCode = hashCode * -1521134295 + i.GetHashCode();
                    hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(S)!;
                    return hashCode;
                }
            }
            """,
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            class C 
            {
                int i;

                string? S { get; }

                public override int GetHashCode()
                {
                    return System.HashCode.Combine(i, S);
                }
            }
            """);

    [Fact]
    public Task TestNullable4()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            class C 
            {
                int i;

                string? S { get; }

                public override int $$GetHashCode()
                {
                    var hashCode = -538000506;
                    hashCode = hashCode * -1521134295 + i.GetHashCode();
                    hashCode = hashCode * -1521134295 + EqualityComparer<string?>.Default.GetHashCode(S)!;
                    return hashCode;
                }
            }
            """,
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            class C 
            {
                int i;

                string? S { get; }

                public override int GetHashCode()
                {
                    return System.HashCode.Combine(i, S);
                }
            }
            """);

    [Fact]
    public Task TestNullable_Enable_1()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            #nullable enable

            class C 
            {
                int i;

                string? S { get; }

                public override int $$GetHashCode()
                {
                    var hashCode = -538000506;
                    hashCode = hashCode * -1521134295 + i.GetHashCode();
                    hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(S);
                    return hashCode;
                }
            }
            """,
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            #nullable enable

            class C 
            {
                int i;

                string? S { get; }

                public override int GetHashCode()
                {
                    return System.HashCode.Combine(i, S);
                }
            }
            """);

    [Fact]
    public Task TestNullable_Enable_2()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            #nullable enable

            class C 
            {
                int i;

                string? S { get; }

                public override int $$GetHashCode()
                {
                    var hashCode = -538000506;
                    hashCode = hashCode * -1521134295 + i.GetHashCode();
                    hashCode = hashCode * -1521134295 + EqualityComparer<string?>.Default.GetHashCode(S);
                    return hashCode;
                }
            }
            """,
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            #nullable enable

            class C 
            {
                int i;

                string? S { get; }

                public override int GetHashCode()
                {
                    return System.HashCode.Combine(i, S);
                }
            }
            """);

    [Fact]
    public Task TestNullable_Enable_3()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            #nullable enable

            class C 
            {
                int i;

                string? S { get; }

                public override int $$GetHashCode()
                {
                    var hashCode = -538000506;
                    hashCode = hashCode * -1521134295 + i.GetHashCode();
                    hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(S)!;
                    return hashCode;
                }
            }
            """,
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            #nullable enable

            class C 
            {
                int i;

                string? S { get; }

                public override int GetHashCode()
                {
                    return System.HashCode.Combine(i, S);
                }
            }
            """);

    [Fact]
    public Task TestNullable_Enable_4()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            #nullable enable

            class C 
            {
                int i;

                string? S { get; }

                public override int $$GetHashCode()
                {
                    var hashCode = -538000506;
                    hashCode = hashCode * -1521134295 + i.GetHashCode();
                    hashCode = hashCode * -1521134295 + EqualityComparer<string?>.Default.GetHashCode(S)!;
                    return hashCode;
                }
            }
            """,
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            #nullable enable

            class C 
            {
                int i;

                string? S { get; }

                public override int GetHashCode()
                {
                    return System.HashCode.Combine(i, S);
                }
            }
            """);

    [Fact]
    public Task TestNullable_Disable_1()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            #nullable disable

            class C 
            {
                int i;

                string? S { get; }

                public override int $$GetHashCode()
                {
                    var hashCode = -538000506;
                    hashCode = hashCode * -1521134295 + i.GetHashCode();
                    hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(S);
                    return hashCode;
                }
            }
            """,
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            #nullable disable

            class C 
            {
                int i;

                string? S { get; }

                public override int GetHashCode()
                {
                    return System.HashCode.Combine(i, S);
                }
            }
            """);

    [Fact]
    public Task TestNullable_Disable_2()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            #nullable disable

            class C 
            {
                int i;

                string? S { get; }

                public override int $$GetHashCode()
                {
                    var hashCode = -538000506;
                    hashCode = hashCode * -1521134295 + i.GetHashCode();
                    hashCode = hashCode * -1521134295 + EqualityComparer<string?>.Default.GetHashCode(S);
                    return hashCode;
                }
            }
            """,
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            #nullable disable

            class C 
            {
                int i;

                string? S { get; }

                public override int GetHashCode()
                {
                    return System.HashCode.Combine(i, S);
                }
            }
            """);

    [Fact]
    public Task TestNullable_Disable_3()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            #nullable disable

            class C 
            {
                int i;

                string? S { get; }

                public override int $$GetHashCode()
                {
                    var hashCode = -538000506;
                    hashCode = hashCode * -1521134295 + i.GetHashCode();
                    hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(S)!;
                    return hashCode;
                }
            }
            """,
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            #nullable disable

            class C 
            {
                int i;

                string? S { get; }

                public override int GetHashCode()
                {
                    return System.HashCode.Combine(i, S);
                }
            }
            """);

    [Fact]
    public Task TestNullable_Disable_4()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            #nullable disable

            class C 
            {
                int i;

                string? S { get; }

                public override int $$GetHashCode()
                {
                    var hashCode = -538000506;
                    hashCode = hashCode * -1521134295 + i.GetHashCode();
                    hashCode = hashCode * -1521134295 + EqualityComparer<string?>.Default.GetHashCode(S)!;
                    return hashCode;
                }
            }
            """,
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            #nullable disable

            class C 
            {
                int i;

                string? S { get; }

                public override int GetHashCode()
                {
                    return System.HashCode.Combine(i, S);
                }
            }
            """);

    [Fact]
    public Task TestNotOnExistingUsageOfSystemHashCode()
        => TestMissingAsync(
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            class C 
            {
                int i;

                string S { get; }

                public override int $$GetHashCode1()
                {
                    return HashCode.Combine(i, S);
                }
            }
            """);

    [Fact]
    public Task TestNotOnExistingUsageOfSystemHashCode2()
        => TestMissingAsync(
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            class C 
            {
                int i;

                string S { get; }

                public override int $$GetHashCode1()
                {
                    var hash = new HashCode();
                    hash.Add(i);
                    hash.Add(S);
                    return hash.ToHashCode();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39916")]
    public Task TestManyFields_ImplicitType()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            class C 
            {
                int a, b, c, d, e, f, g, h, i;

                public override int $$GetHashCode()
                {
                    var hashCode = -538000506;
                    hashCode = hashCode * -1521134295 + a.GetHashCode();
                    hashCode = hashCode * -1521134295 + b.GetHashCode();
                    hashCode = hashCode * -1521134295 + c.GetHashCode();
                    hashCode = hashCode * -1521134295 + d.GetHashCode();
                    hashCode = hashCode * -1521134295 + e.GetHashCode();
                    hashCode = hashCode * -1521134295 + f.GetHashCode();
                    hashCode = hashCode * -1521134295 + g.GetHashCode();
                    hashCode = hashCode * -1521134295 + h.GetHashCode();
                    hashCode = hashCode * -1521134295 + i.GetHashCode();
                    return hashCode;
                }
            }
            """,
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            class C 
            {
                int a, b, c, d, e, f, g, h, i;

                public override int GetHashCode()
                {
                    var hash = new System.HashCode();
                    hash.Add(a);
                    hash.Add(b);
                    hash.Add(c);
                    hash.Add(d);
                    hash.Add(e);
                    hash.Add(f);
                    hash.Add(g);
                    hash.Add(h);
                    hash.Add(i);
                    return hash.ToHashCode();
                }
            }
            """, new TestParameters(options: this.PreferImplicitTypeWithInfo()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39916")]
    public Task TestManyFields_ExplicitType()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            class C 
            {
                int a, b, c, d, e, f, g, h, i;

                public override int $$GetHashCode()
                {
                    var hashCode = -538000506;
                    hashCode = hashCode * -1521134295 + a.GetHashCode();
                    hashCode = hashCode * -1521134295 + b.GetHashCode();
                    hashCode = hashCode * -1521134295 + c.GetHashCode();
                    hashCode = hashCode * -1521134295 + d.GetHashCode();
                    hashCode = hashCode * -1521134295 + e.GetHashCode();
                    hashCode = hashCode * -1521134295 + f.GetHashCode();
                    hashCode = hashCode * -1521134295 + g.GetHashCode();
                    hashCode = hashCode * -1521134295 + h.GetHashCode();
                    hashCode = hashCode * -1521134295 + i.GetHashCode();
                    return hashCode;
                }
            }
            """,
            """
            using System.Collections.Generic;
            namespace System { public struct HashCode { } }

            class C 
            {
                int a, b, c, d, e, f, g, h, i;

                public override int GetHashCode()
                {
                    System.HashCode hash = new System.HashCode();
                    hash.Add(a);
                    hash.Add(b);
                    hash.Add(c);
                    hash.Add(d);
                    hash.Add(e);
                    hash.Add(f);
                    hash.Add(g);
                    hash.Add(h);
                    hash.Add(i);
                    return hash.ToHashCode();
                }
            }
            """);

    [Fact]
    public Task TestNotOnSingleReturnedMember()
        => TestMissingAsync(
            """
            namespace System { public struct HashCode { } }

            class C
            {
                int j;

                public override int $$GetHashCode()
                {
                    return j;
                }
            }
            """);

    [Fact]
    public Task TestNotOnSingleMemberWithInvokedGetHashCode()
        => TestMissingAsync(
            """
            namespace System { public struct HashCode { } }

            class C
            {
                int j;

                public override int $$GetHashCode()
                {
                    return j.GetHashCode();
                }
            }
            """);

    [Fact]
    public Task TestNotOnSimpleBaseReturn()
        => TestMissingAsync(
            """
            namespace System { public struct HashCode { } }

            class C
            {
                int j;

                public override int $$GetHashCode()
                {
                    return base.GetHashCode();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74315")]
    public Task TestMissingBaseType()
        => TestMissingAsync("""
            using System;

            namespace System { public struct HashCode { } }

            record $$B(int I) : A(I);
            """);
}
