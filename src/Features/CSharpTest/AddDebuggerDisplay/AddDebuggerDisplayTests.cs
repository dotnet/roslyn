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
    public async Task OfferedOnEmptyClass()
    {
        await VerifyCS.VerifyRefactoringAsync("""
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
    }

    [Fact]
    public async Task SupportsConstantInterpolatedStrings()
    {
        var code = """
            [||]class C
            {
            }
            """;
        var fixedCode = """
            using System.Diagnostics;

            [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
            class C
            {
                private string GetDebuggerDisplay()
                {
                    return ToString();
                }
            }
            """;

        await new VerifyCS.Test()
        {
            LanguageVersion = LanguageVersion.CSharp12,
            TestCode = code,
            FixedCode = fixedCode,
        }.RunAsync();
    }

    [Fact]
    public async Task OfferedOnEmptyRecord()
    {
        var code = """
            [||]record C;
            """;
        var fixedCode = """
            using System.Diagnostics;

            [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
            record C
            {
                private string GetDebuggerDisplay()
                {
                    return ToString();
                }
            }
            """;

        await new VerifyCS.Test()
        {
            LanguageVersion = LanguageVersion.CSharp9,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            TestCode = code,
            FixedCode = fixedCode,
        }.RunAsync();
    }

    [Fact]
    public async Task OfferedOnEmptyStruct()
    {
        await VerifyCS.VerifyRefactoringAsync("""
            [||]struct Goo
            {
            }
            """, """
            using System.Diagnostics;

            [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
            struct Goo
            {
                private string GetDebuggerDisplay()
                {
                    return ToString();
                }
            }
            """);
    }

    [Fact]
    public async Task NotOfferedOnStaticClass()
    {
        var code = """
            [||]static class Goo
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
            [||]enum Goo
            {
            }
            """;

        await VerifyCS.VerifyRefactoringAsync(code, code);
    }

    [Fact]
    public async Task NotOfferedOnDelegate()
    {
        var code = """
            [||]delegate void Goo();
            """;

        await VerifyCS.VerifyRefactoringAsync(code, code);
    }

    [Fact]
    public async Task NotOfferedOnUnrelatedClassMembers()
    {
        var code = """
            class C
            {
                [||]public int Goo { get; }
            }
            """;

        await VerifyCS.VerifyRefactoringAsync(code, code);
    }

    [Fact]
    public async Task OfferedOnToString()
    {
        await VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                public override string [||]ToString() => "Goo";
            }
            """, """
            using System.Diagnostics;

            [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
            class C
            {
                public override string ToString() => "Goo";

                private string GetDebuggerDisplay()
                {
                    return ToString();
                }
            }
            """);
    }

    [Fact]
    public async Task OfferedOnShadowingToString()
    {
        await VerifyCS.VerifyRefactoringAsync("""
            class A
            {
                public new string [||]ToString() => "Goo";
            }
            """, """
            using System.Diagnostics;

            [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
            class A
            {
                public new string ToString() => "Goo";

                private string GetDebuggerDisplay()
                {
                    return ToString();
                }
            }
            """);
    }

    [Fact]
    public async Task NotOfferedOnWrongOverloadOfToString()
    {
        var code = """
            class A
            {
                public virtual string ToString(int bar = 0) => "Goo";
            }

            class B : A
            {
                public override string [||]ToString(int bar = 0) => "Bar";
            }
            """;

        await VerifyCS.VerifyRefactoringAsync(code, code);
    }

    [Fact]
    public async Task OfferedOnExistingDebuggerDisplayMethod()
    {
        await VerifyCS.VerifyRefactoringAsync("""
            class C
            {
                private string [||]GetDebuggerDisplay() => "Goo";
            }
            """, """
            using System.Diagnostics;

            [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
            class C
            {
                private string GetDebuggerDisplay() => "Goo";
            }
            """);
    }

    [Fact]
    public async Task NotOfferedOnWrongOverloadOfDebuggerDisplayMethod()
    {
        var code = """
            class A
            {
                private string [||]GetDebuggerDisplay(int bar = 0) => "Goo";
            }
            """;

        await VerifyCS.VerifyRefactoringAsync(code, code);
    }

    [Fact]
    public async Task NamespaceImportIsNotDuplicated()
    {
        await VerifyCS.VerifyRefactoringAsync("""
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
    }

    [Fact]
    public async Task NamespaceImportIsSorted()
    {
        await VerifyCS.VerifyRefactoringAsync("""
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
    }

    [Fact]
    public async Task NotOfferedWhenAlreadySpecified()
    {
        var code = """
            [System.Diagnostics.DebuggerDisplay("Goo")]
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
            [System.Diagnostics.DebuggerDisplayAttribute("Goo")]
            [||]class C
            {
            }
            """;

        await VerifyCS.VerifyRefactoringAsync(code, code);
    }

    [Fact]
    public async Task OfferedWhenAttributeWithTheSameNameIsSpecified()
    {
        await VerifyCS.VerifyRefactoringAsync("""
            [{|CS0246:BrokenCode|}.DebuggerDisplay("Goo")]
            [||]class C
            {
            }
            """, """
            using System.Diagnostics;

            [{|CS0246:BrokenCode|}.DebuggerDisplay("Goo")]
            [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
            [||]class C
            {
                private string GetDebuggerDisplay()
                {
                    return ToString();
                }
            }
            """);
    }

    [Fact]
    public async Task OfferedWhenAttributeWithTheSameNameIsSpecifiedWithSuffix()
    {
        await VerifyCS.VerifyRefactoringAsync("""
            [{|CS0246:BrokenCode|}.DebuggerDisplayAttribute("Goo")]
            [||]class C
            {
            }
            """, """
            using System.Diagnostics;

            [{|CS0246:BrokenCode|}.DebuggerDisplayAttribute("Goo")]
            [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
            [||]class C
            {
                private string GetDebuggerDisplay()
                {
                    return ToString();
                }
            }
            """);
    }

    [Fact]
    public async Task AliasedTypeIsRecognized()
    {
        var code = """
            using DD = System.Diagnostics.DebuggerDisplayAttribute;

            [DD("Goo")]
            [||]class C
            {
            }
            """;

        await VerifyCS.VerifyRefactoringAsync(code, code);
    }

    [Fact]
    public async Task OfferedWhenBaseClassHasDebuggerDisplay()
    {
        await VerifyCS.VerifyRefactoringAsync("""
            using System.Diagnostics;

            [DebuggerDisplay("Goo")]
            class A
            {
            }

            [||]class B : A
            {
            }
            """, """
            using System.Diagnostics;

            [DebuggerDisplay("Goo")]
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
    }

    [Fact]
    public async Task ExistingDebuggerDisplayMethodIsUsedEvenWhenPublicStaticNonString()
    {
        await VerifyCS.VerifyRefactoringAsync("""
            [||]class C
            {
                public static object GetDebuggerDisplay() => "Goo";
            }
            """, """
            using System.Diagnostics;

            [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
            class C
            {
                public static object GetDebuggerDisplay() => "Goo";
            }
            """);
    }

    [Fact]
    public async Task ExistingDebuggerDisplayMethodWithParameterIsNotUsed()
    {
        await VerifyCS.VerifyRefactoringAsync("""
            [||]class C
            {
                private string GetDebuggerDisplay(int goo = 0) => goo.ToString();
            }
            """, """
            using System.Diagnostics;

            [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
            class C
            {
                private string GetDebuggerDisplay(int goo = 0) => goo.ToString();

                private string GetDebuggerDisplay()
                {
                    return ToString();
                }
            }
            """);
    }
}
