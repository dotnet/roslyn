// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.ConvertProgram;
using Microsoft.CodeAnalysis.CSharp.TopLevelStatements;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertProgram
{
    using VerifyCS = CSharpCodeFixVerifier<ConvertToProgramMainDiagnosticAnalyzer, ConvertToProgramMainCodeFixProvider>;

    public class ConvertToProgramMainAnalyzerTests
    {
        [Fact]
        public async Task NotOfferedWhenUserPrefersTopLevelStatements()
        {
            var code = @"
System.Console.WriteLine(0);
";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp9,
                TestState = { OutputKind = OutputKind.ConsoleApplication },
                Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true } },
            }.RunAsync();
        }

        [Fact]
        public async Task NotOfferedWhenUserPrefersProgramMainButNoTopLevelStatements()
        {
            var code = @"
class C
{
    void M()
    {
        System.Console.WriteLine(0);
    }
}
";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp9,
                Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false } },
            }.RunAsync();
        }

        [Fact]
        public async Task OfferedWhenUserPrefersProgramMainAndTopLevelStatements_Silent()
        {
            await new VerifyCS.Test
            {
                TestCode = @"{|IDE0211:
System.Console.WriteLine(0);
|}",
                FixedCode = @"
internal class Program
{
    private static void Main(string[] args)
    {
        System.Console.WriteLine(0);
    }
}",
                LanguageVersion = LanguageVersion.CSharp9,
                TestState = { OutputKind = OutputKind.ConsoleApplication },
                Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Silent } },
            }.RunAsync();
        }

        [Fact]
        public async Task TestHeader1()
        {
            await new VerifyCS.Test
            {
                TestCode = @"{|IDE0211:// This is a file banner

System.Console.WriteLine(0);
|}",
                FixedCode = @"// This is a file banner

internal class Program
{
    private static void Main(string[] args)
    {
        System.Console.WriteLine(0);
    }
}",
                LanguageVersion = LanguageVersion.CSharp9,
                TestState = { OutputKind = OutputKind.ConsoleApplication },
                Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Silent } },
            }.RunAsync();
        }

        [Fact]
        public async Task TestHeader2()
        {
            await new VerifyCS.Test
            {
                TestCode = @"{|IDE0211:// This is a file banner
using System;

System.Console.WriteLine(0);
|}",
                FixedCode = @"// This is a file banner
using System;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine(0);
    }
}",
                LanguageVersion = LanguageVersion.CSharp9,
                TestState = { OutputKind = OutputKind.ConsoleApplication },
                Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Silent } },
            }.RunAsync();
        }

        [Fact]
        public async Task NotOfferedInLibrary()
        {
            var code = @"
{|CS8805:System.Console.WriteLine(0);|}
";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp9,
                Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Silent } },
            }.RunAsync();
        }

        [Fact]
        public async Task NotOfferedWhenSuppressed()
        {
            var code = @"
System.Console.WriteLine(0);
";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp9,
                TestState = { OutputKind = OutputKind.ConsoleApplication },
                Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.None } },
            }.RunAsync();
        }

        [Fact]
        public async Task OfferedWhenUserPrefersProgramMainAndTopLevelStatements_Suggestion()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
{|IDE0211:System|}.Console.WriteLine(0);
",
                FixedCode = @"
internal class Program
{
    private static void Main(string[] args)
    {
        System.Console.WriteLine(0);
    }
}",
                LanguageVersion = LanguageVersion.CSharp9,
                TestState = { OutputKind = OutputKind.ConsoleApplication },
                Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion } },
            }.RunAsync();
        }

        [Fact]
        public async Task PreferNoAccessibility()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
{|IDE0211:System|}.Console.WriteLine(0);
",
                FixedCode = @"
class Program
{
    static void Main(string[] args)
    {
        System.Console.WriteLine(0);
    }
}",
                LanguageVersion = LanguageVersion.CSharp9,
                TestState = { OutputKind = OutputKind.ConsoleApplication },
                Options =
                {
                    { CodeStyleOptions2.RequireAccessibilityModifiers, AccessibilityModifiersRequired.Never },
                    { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TestWithExistingUsings()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

{|IDE0211:Console|}.WriteLine(0);
",
                FixedCode = @"
using System;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine(0);
    }
}",
                LanguageVersion = LanguageVersion.CSharp9,
                TestState = { OutputKind = OutputKind.ConsoleApplication },
                Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion } },
            }.RunAsync();
        }

        [Fact]
        public async Task TestWithNumericReturn()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

{|IDE0211:Console|}.WriteLine(0);

return 0;
",
                FixedCode = @"
using System;

internal class Program
{
    private static int Main(string[] args)
    {
        Console.WriteLine(0);

        return 0;
    }
}",
                LanguageVersion = LanguageVersion.CSharp9,
                TestState = { OutputKind = OutputKind.ConsoleApplication },
                Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion } },
            }.RunAsync();
        }

        [Fact]
        public async Task TestWithLocalFunction()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

{|IDE0211:Console|}.WriteLine(0);

void M()
{
}

return 0;
",
                FixedCode = @"
using System;

internal class Program
{
    private static int Main(string[] args)
    {
        Console.WriteLine(0);

        void M()
        {
        }

        return 0;
    }
}",
                LanguageVersion = LanguageVersion.CSharp9,
                TestState = { OutputKind = OutputKind.ConsoleApplication },
                Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion } },
            }.RunAsync();
        }

        [Fact]
        public async Task TestWithAwait()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

{|IDE0211:await|} Console.Out.WriteLineAsync();
",
                FixedCode = @"
using System;
using System.Threading.Tasks;

internal class Program
{
    private static async Task Main(string[] args)
    {
        await Console.Out.WriteLineAsync();
    }
}",
                LanguageVersion = LanguageVersion.CSharp9,
                TestState = { OutputKind = OutputKind.ConsoleApplication },
                Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion } },
            }.RunAsync();
        }

        [Fact]
        public async Task TestWithAwaitAndNumericReturn()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

{|IDE0211:await|} Console.Out.WriteLineAsync();

return 0;
",
                FixedCode = @"
using System;
using System.Threading.Tasks;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        await Console.Out.WriteLineAsync();

        return 0;
    }
}",
                LanguageVersion = LanguageVersion.CSharp9,
                TestState = { OutputKind = OutputKind.ConsoleApplication },
                Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion } },
            }.RunAsync();
        }

        [Fact]
        public async Task TestNormalCommentMovedToProgram()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

// This comment describes the program
{|IDE0211:Console|}.WriteLine(0);
",
                FixedCode = @"
using System;

// This comment describes the program
internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine(0);
    }
}",
                LanguageVersion = LanguageVersion.CSharp9,
                TestState = { OutputKind = OutputKind.ConsoleApplication },
                Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion } },
            }.RunAsync();
        }

        [Fact]
        public async Task TestTopLevelStatementExplanationCommentRemoved()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

// See https://aka.ms/new-console-template for more information
{|IDE0211:Console|}.WriteLine(0);
",
                FixedCode = @"
using System;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine(0);
    }
}",
                LanguageVersion = LanguageVersion.CSharp9,
                TestState = { OutputKind = OutputKind.ConsoleApplication },
                Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion } },
            }.RunAsync();
        }

        [Fact]
        public async Task TestPreprocessorDirective1()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

#if true

{|IDE0211:Console|}.WriteLine(0);

#endif
",
                FixedCode = @"
using System;

#if true

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine(0);
    }
}

#endif
",
                LanguageVersion = LanguageVersion.CSharp9,
                TestState = { OutputKind = OutputKind.ConsoleApplication },
                Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion } },
            }.RunAsync();
        }

        [Fact]
        public async Task TestPreprocessorDirective2()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

#if true

{|IDE0211:Console|}.WriteLine(0);

return;

#endif
",
                FixedCode = @"
using System;

#if true

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine(0);

        return;
    }
}

#endif
",
                LanguageVersion = LanguageVersion.CSharp9,
                TestState = { OutputKind = OutputKind.ConsoleApplication },
                Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion } },
            }.RunAsync();
        }

        [Fact]
        public async Task TestHasExistingPart()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

{|IDE0211:Console|}.WriteLine(0);

partial class Program
{
    int x;
}
",
                FixedCode = @"
using System;

internal partial class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine(0);
    }
}

partial class Program
{
    int x;
}
",
                LanguageVersion = LanguageVersion.CSharp9,
                TestState = { OutputKind = OutputKind.ConsoleApplication },
                Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion } },
            }.RunAsync();
        }

        [Fact]
        public async Task TestHasExistingPublicPart()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

{|IDE0211:Console|}.WriteLine(0);

public partial class Program
{
    int x;
}
",
                FixedCode = @"
using System;

public partial class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine(0);
    }
}

public partial class Program
{
    int x;
}
",
                LanguageVersion = LanguageVersion.CSharp9,
                TestState = { OutputKind = OutputKind.ConsoleApplication },
                Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion } },
            }.RunAsync();
        }

        [Fact]
        public async Task TestBeforeExistingClass()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

{|IDE0211:Console|}.WriteLine(0);

class X
{
    int x;
}
",
                FixedCode = @"
using System;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine(0);
    }
}

class X
{
    int x;
}
",
                LanguageVersion = LanguageVersion.CSharp9,
                TestState = { OutputKind = OutputKind.ConsoleApplication },
                Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion } },
            }.RunAsync();
        }
    }
}
