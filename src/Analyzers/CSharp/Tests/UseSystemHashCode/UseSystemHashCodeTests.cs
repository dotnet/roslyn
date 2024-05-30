// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
public partial class UseSystemHashCodeTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public UseSystemHashCodeTests(ITestOutputHelper logger)
       : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new UseSystemHashCodeDiagnosticAnalyzer(), new UseSystemHashCodeCodeFixProvider());

    [Fact]
    public async Task TestDerivedClassWithFieldWithBase()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestDerivedClassWithFieldWithNoBase()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestDerivedClassWithNoFieldWithBase()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestFieldAndProp()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestUnchecked()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestNotOnNonGetHashCode()
    {
        await TestMissingAsync(
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
    }

    [Fact]
    public async Task TestNotWithoutReturn()
    {
        await TestMissingAsync(
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
    }

    [Fact]
    public async Task TestNotWithoutLocal()
    {
        await TestMissingAsync(
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
    }

    [Fact]
    public async Task TestNotWithMultipleLocals()
    {
        await TestMissingAsync(
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
    }

    [Fact]
    public async Task TestNotWithoutInitializer()
    {
        await TestMissingAsync(
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
    }

    [Fact]
    public async Task TestNotReturningAccumulator()
    {
        await TestMissingAsync(
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
    }

    [Fact]
    public async Task TestAcumulatorInitializedToField()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestAcumulatorInitializedToHashedField()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestMissingOnThisGetHashCode()
    {
        await TestMissingAsync(
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
    }

    [Fact]
    public async Task TestMissingWithNoSystemHashCode()
    {
        await TestMissingAsync(
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
    }

    [Fact]
    public async Task TestDirectNullCheck1()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestDirectNullCheck2()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestInt64Pattern()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestInt64Pattern2()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestTuple()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestNullable1()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestNullable2()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestNullable3()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestNullable4()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestNullable_Enable_1()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestNullable_Enable_2()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestNullable_Enable_3()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestNullable_Enable_4()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestNullable_Disable_1()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestNullable_Disable_2()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestNullable_Disable_3()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestNullable_Disable_4()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestNotOnExistingUsageOfSystemHashCode()
    {
        await TestMissingAsync(
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
    }

    [Fact]
    public async Task TestNotOnExistingUsageOfSystemHashCode2()
    {
        await TestMissingAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39916")]
    public async Task TestManyFields_ImplicitType()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39916")]
    public async Task TestManyFields_ExplicitType()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestNotOnSingleReturnedMember()
    {
        await TestMissingAsync(
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
    }

    [Fact]
    public async Task TestNotOnSingleMemberWithInvokedGetHashCode()
    {
        await TestMissingAsync(
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
    }

    [Fact]
    public async Task TestNotOnSimpleBaseReturn()
    {
        await TestMissingAsync(
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
    }
}
