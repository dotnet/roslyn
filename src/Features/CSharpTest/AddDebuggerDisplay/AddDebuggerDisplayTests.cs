// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.AddDebuggerDisplay;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddDebuggerDisplay;

using VerifyCS = CSharpCodeRefactoringVerifier<CSharpAddDebuggerDisplayCodeRefactoringProvider>;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.CodeActionsAddDebuggerDisplay)]
public sealed class AddDebuggerDisplayTests
{
    [Fact]
    public Task OfferedOnEmptyClass()
        => VerifyCS.VerifyRefactoringAsync("""
            [||]class C
            {
            }
            """, """
            using System.Diagnostics;

            [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
            class C
            {
                private string GetDebuggerDisplay()
                {
                    return ToString();
                }
            }
            """);

    [Fact]
    public Task SupportsConstantInterpolatedStrings()
        => new VerifyCS.Test()
        {
            LanguageVersion = LanguageVersion.CSharp12,
            TestCode = """
            [||]class C
            {
            }
            """,
            FixedCode = """
            using System.Diagnostics;

            [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
            class C
            {
                private string GetDebuggerDisplay()
                {
                    return ToString();
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task OfferedOnEmptyRecord()
        => new VerifyCS.Test()
        {
            LanguageVersion = LanguageVersion.CSharp9,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            TestCode = """
            [||]record C;
            """,
            FixedCode = """
            using System.Diagnostics;

            [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
            record C
            {
                private string GetDebuggerDisplay()
                {
                    return ToString();
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task OfferedOnEmptyStruct()
        => VerifyCS.VerifyRefactoringAsync("""
            [||]struct Foo
            {
            }
            """, """
            using System.Diagnostics;

            [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
            struct Foo
            {
                private string GetDebuggerDisplay()
                {
                    return ToString();
                }
            }
            """);

    [Fact]
    public async Task NotOfferedOnStaticClass()
    {
        var code = """
            [||]static class Foo
            {
            }
            """;

        await VerifyCS.VerifyRefactoringAsync(code, code);
    }

    [Fact]
    public async Task NotOfferedOnInterfaceWithToString()
    {
        var code = """
            [||]interface IFoo
            {
                string ToString();
            }
            """;

        await VerifyCS.VerifyRefactoringAsync(code, code);
    }

    [Fact]
    public async Task NotOfferedOnEnum()
    {
        var code = """
            [||]enum Foo
            {
            }
            """;

        await VerifyCS.VerifyRefactoringAsync(code, code);
    }

    [Fact]
    public async Task NotOfferedOnDelegate()
    {
        var code = """
            [||]delegate void Foo();
            """;

        await VerifyCS.VerifyRefactoringAsync(code, code);
    }

    [Fact]
    public async Task NotOfferedOnUnrelatedClassMembers()
    {
        var code = """
            class C
            {
                [||]public int Foo { get; }
            }
            """;

        await VerifyCS.VerifyRefactoringAsync(code, code);
    }

    [Fact]
    public Task OfferedOnToString()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                public override string [||]ToString() => "Foo";
            }
            """, """
            using System.Diagnostics;

            [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
            class C
            {
                public override string ToString() => "Foo";

                private string GetDebuggerDisplay()
                {
                    return ToString();
                }
            }
            """);

    [Fact]
    public Task OfferedOnShadowingToString()
        => VerifyCS.VerifyRefactoringAsync("""
            class A
            {
                public new string [||]ToString() => "Foo";
            }
            """, """
            using System.Diagnostics;

            [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
            class A
            {
                public new string ToString() => "Foo";

                private string GetDebuggerDisplay()
                {
                    return ToString();
                }
            }
            """);

    [Fact]
    public async Task NotOfferedOnWrongOverloadOfToString()
    {
        var code = """
            class A
            {
                public virtual string ToString(int bar = 0) => "Foo";
            }

            class B : A
            {
                public override string [||]ToString(int bar = 0) => "Bar";
            }
            """;

        await VerifyCS.VerifyRefactoringAsync(code, code);
    }

    [Fact]
    public Task OfferedOnExistingDebuggerDisplayMethod()
        => VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                private string [||]GetDebuggerDisplay() => "Foo";
            }
            """, """
            using System.Diagnostics;

            [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
            class C
            {
                private string GetDebuggerDisplay() => "Foo";
            }
            """);

    [Fact]
    public async Task NotOfferedOnWrongOverloadOfDebuggerDisplayMethod()
    {
        var code = """
            class A
            {
                private string [||]GetDebuggerDisplay(int bar = 0) => "Foo";
            }
            """;

        await VerifyCS.VerifyRefactoringAsync(code, code);
    }

    [Fact]
    public Task NamespaceImportIsNotDuplicated()
        => VerifyCS.VerifyRefactoringAsync("""
            using System.Diagnostics;

            [||]class C
            {
            }
            """, """
            using System.Diagnostics;

            [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
            class C
            {
                private string GetDebuggerDisplay()
                {
                    return ToString();
                }
            }
            """);

    [Fact]
    public Task NamespaceImportIsSorted()
        => VerifyCS.VerifyRefactoringAsync("""
            using System.Xml;

            [||]class C
            {
            }
            """, """
            using System.Diagnostics;
            using System.Xml;

            [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
            class C
            {
                private string GetDebuggerDisplay()
                {
                    return ToString();
                }
            }
            """);

    [Fact]
    public async Task NotOfferedWhenAlreadySpecified()
    {
        var code = """
            [System.Diagnostics.DebuggerDisplay("Foo")]
            [||]class C
            {
            }
            """;

        await VerifyCS.VerifyRefactoringAsync(code, code);
    }

    [Fact]
    public async Task NotOfferedWhenAlreadySpecifiedWithSuffix()
    {
        var code = """
            [System.Diagnostics.DebuggerDisplayAttribute("Foo")]
            [||]class C
            {
            }
            """;

        await VerifyCS.VerifyRefactoringAsync(code, code);
    }

    [Fact]
    public Task OfferedWhenAttributeWithTheSameNameIsSpecified()
        => VerifyCS.VerifyRefactoringAsync("""
            [{|CS0246:BrokenCode|}.DebuggerDisplay("Foo")]
            [||]class C
            {
            }
            """, """
            using System.Diagnostics;

            [{|CS0246:BrokenCode|}.DebuggerDisplay("Foo")]
            [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
            [||]class C
            {
                private string GetDebuggerDisplay()
                {
                    return ToString();
                }
            }
            """);

    [Fact]
    public Task OfferedWhenAttributeWithTheSameNameIsSpecifiedWithSuffix()
        => VerifyCS.VerifyRefactoringAsync("""
            [{|CS0246:BrokenCode|}.DebuggerDisplayAttribute("Foo")]
            [||]class C
            {
            }
            """, """
            using System.Diagnostics;

            [{|CS0246:BrokenCode|}.DebuggerDisplayAttribute("Foo")]
            [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
            [||]class C
            {
                private string GetDebuggerDisplay()
                {
                    return ToString();
                }
            }
            """);

    [Fact]
    public async Task AliasedTypeIsRecognized()
    {
        var code = """
            using DD = System.Diagnostics.DebuggerDisplayAttribute;

            [DD("Foo")]
            [||]class C
            {
            }
            """;

        await VerifyCS.VerifyRefactoringAsync(code, code);
    }

    [Fact]
    public Task OfferedWhenBaseClassHasDebuggerDisplay()
        => VerifyCS.VerifyRefactoringAsync("""
            using System.Diagnostics;

            [DebuggerDisplay("Foo")]
            class A
            {
            }

            [||]class B : A
            {
            }
            """, """
            using System.Diagnostics;

            [DebuggerDisplay("Foo")]
            class A
            {
            }

            [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
            class B : A
            {
                private string GetDebuggerDisplay()
                {
                    return ToString();
                }
            }
            """);

    [Fact]
    public Task ExistingDebuggerDisplayMethodIsUsedEvenWhenPublicStaticNonString()
        => VerifyCS.VerifyRefactoringAsync("""
            [||]class C
            {
                public static object GetDebuggerDisplay() => "Foo";
            }
            """, """
            using System.Diagnostics;

            [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
            class C
            {
                public static object GetDebuggerDisplay() => "Foo";
            }
            """);

    [Fact]
    public Task ExistingDebuggerDisplayMethodWithParameterIsNotUsed()
        => VerifyCS.VerifyRefactoringAsync("""
            [||]class C
            {
                private string GetDebuggerDisplay(int foo = 0) => foo.ToString();
            }
            """, """
            using System.Diagnostics;

            [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
            class C
            {
                private string GetDebuggerDisplay(int foo = 0) => foo.ToString();

                private string GetDebuggerDisplay()
                {
                    return ToString();
                }
            }
            """);
}
