// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.ConvertNamespace;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertNamespace
{
    using VerifyCS = CSharpCodeFixVerifier<ConvertToFileScopedNamespaceDiagnosticAnalyzer, ConvertNamespaceCodeFixProvider>;

    public class ConvertToFileScopedNamespaceAnalyzerTests
    {
        #region Convert To File Scoped

        [Fact]
        public async Task TestNoConvertToFileScopedInCSharp9()
        {
            var code = @"
namespace N
{
}
";
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp9,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestNoConvertToFileScopedInCSharp10WithBlockScopedPreference()
        {
            var code = @"
namespace N
{
}
";
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestConvertToFileScopedInCSharp10WithBlockScopedPreference()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
[|namespace N|]
{
}
",
                FixedCode = @"
namespace $$N;
",
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestConvertToFileScopedInCSharp10WithBlockScopedPreference_NotSilent()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
namespace [|N|]
{
}
",
                FixedCode = @"
namespace $$N;
",
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped, NotificationOption2.Suggestion }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestNoConvertWithMultipleNamespaces()
        {
            var code = @"
namespace N
{
}

namespace N2
{
}
";
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestNoConvertWithNestedNamespaces1()
        {
            var code = @"
namespace N
{
    namespace N2
    {
    }
}
";
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestNoConvertWithTopLevelStatement1()
        {
            var code = @"
{|CS8805:int i = 0;|}

namespace N
{
}
";
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestNoConvertWithTopLevelStatement2()
        {
            var code = @"
namespace N
{
}

int i = 0;
";
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp10,
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(6,1): error CS8803: Top-level statements must precede namespace and type declarations.
                    DiagnosticResult.CompilerError("CS8803").WithSpan(6, 1, 6, 11),
                    // /0/Test0.cs(6,1): error CS8805: Program using top-level statements must be an executable.
                    DiagnosticResult.CompilerError("CS8805").WithSpan(6, 1, 6, 11),
                },
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestConvertToFileScopedWithUsing1()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

[|namespace N|]
{
}
",
                FixedCode = @"
using System;

namespace $$N;
",
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestConvertToFileScopedWithUsing2()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
[|namespace N|]
{
    using System;
}
",
                FixedCode = @"
namespace $$N;

using System;
",
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestConvertToFileScopedWithClass()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
[|namespace N|]
{
    class C
    {
    }
}
",
                FixedCode = @"
namespace $$N;

class C
{
}
",
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestConvertToFileScopedWithClassWithDocComment()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
[|namespace N|]
{
    /// <summary/>
    class C
    {
    }
}
",
                FixedCode = @"
namespace $$N;

/// <summary/>
class C
{
}
",
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestConvertToFileScopedWithMissingCloseBrace()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
[|namespace N|]
{
    /// <summary/>
    class C
    {
    }{|CS1513:|}",
                FixedCode = @"
namespace N;

/// <summary/>
class C
{
}",
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestConvertToFileScopedWithCommentOnOpenCurly()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
[|namespace N|]
{ // comment
    class C
    {
    }
}
",
                FixedCode = @"
namespace $$N;

// comment
class C
{
}
",
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestConvertToFileScopedWithLeadingComment()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
// copyright
[|namespace N|]
{
    class C
    {
    }
}
",
                FixedCode = @"
// copyright
namespace $$N;

class C
{
}
",
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestConvertToFileScopedWithDocComment()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
[|namespace N|]
{
    /// <summary/>
    class C
    {
    }
}
",
                FixedCode = @"
namespace $$N;

/// <summary/>
class C
{
}
",
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestConvertToFileScopedWithPPDirective1()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
[|namespace N|]
{
#if X
    class C
    {
    }
#else
    class C
    {
    }
#endif
}
",
                FixedCode = @"
namespace $$N;

#if X
class C
{
}
#else
class C
{
}
#endif
",
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestConvertToFileScopedWithBlockComment()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
[|namespace N|]
{
    /* x
     * x
     */
    class C
    {
    }
}
",
                FixedCode = @"
namespace $$N;

/* x
 * x
 */
class C
{
}
",
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestConvertToFileScopedWithBlockComment2()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
[|namespace N|]
{
    /* x
       x
     */
    class C
    {
    }
}
",
                FixedCode = @"
namespace $$N;

/* x
   x
 */
class C
{
}
",
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestConvertToFileScopedWithMultilineString()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
[|namespace N|]
{
    class C
    {
        void M()
        {
            System.Console.WriteLine(@""
    a
        b
            c
                d
                    e
                        "");
        }
    }
}
",
                FixedCode = @"
namespace $$N;

class C
{
    void M()
    {
        System.Console.WriteLine(@""
    a
        b
            c
                d
                    e
                        "");
    }
}
",
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestConvertToFileScopedWithMultilineString2()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
[|namespace N|]
{
    class C
    {
        void M()
        {
            System.Console.WriteLine($@""
    a
        b
            c{1 + 1}
                d
                    e
                        "");
        }
    }
}
",
                FixedCode = @"
namespace $$N;

class C
{
    void M()
    {
        System.Console.WriteLine($@""
    a
        b
            c{1 + 1}
                d
                    e
                        "");
    }
}
",
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestConvertToFileScopedWithMultilineString3()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
[|namespace N|]
{
    class C
    {
        void M()
        {
            System.Console.WriteLine($@""
    a
        b
            c{
                1 + 1
             }d
                    e
                        "");
        }
    }
}
",
                FixedCode = @"
namespace $$N;

class C
{
    void M()
    {
        System.Console.WriteLine($@""
    a
        b
            c{
            1 + 1
         }d
                    e
                        "");
    }
}
",
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestConvertToFileScopedSingleLineNamespace1()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
[|namespace N|] { class C { } }
",
                FixedCode = @"
namespace $$N; 
class C { } ",
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestConvertToFileScopedSingleLineNamespace2()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
[|namespace N|]
{ class C { } }
",
                FixedCode = @"
namespace $$N;

class C { } ",
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
                }
            }.RunAsync();
        }

        [Fact]
        [WorkItem(59728, "https://github.com/dotnet/roslyn/issues/59728")]
        public async Task TestConvertToFileScopedWithNoNewlineAtEnd()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
[|namespace N|]
{
    class C
    {
    }
}",
                FixedCode = @"
namespace $$N;

class C
{
}",
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
                }
            }.RunAsync();
        }

        [Fact]
        [WorkItem(59728, "https://github.com/dotnet/roslyn/issues/59728")]
        public async Task TestConvertToFileScopedWithNoMembersAndNoNewlineAtEnd()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
[|namespace N|]
{
}",
                FixedCode = @"
namespace $$N;",
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
                }
            }.RunAsync();
        }

        [Fact]
        [WorkItem(59728, "https://github.com/dotnet/roslyn/issues/59728")]
        public async Task TestConvertToFileScopedPreserveNewlineAtEnd()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
[|namespace N|]
{
    class C
    {
    }
}
",
                FixedCode = @"
namespace $$N;

class C
{
}
",
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
                }
            }.RunAsync();
        }

        [Fact]
        [WorkItem(59728, "https://github.com/dotnet/roslyn/issues/59728")]
        public async Task TestConvertToFileScopedWithNoMembersPreserveNewlineAtEnd()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
[|namespace N|]
{
}
",
                FixedCode = @"
namespace $$N;
",
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
                }
            }.RunAsync();
        }

        #endregion
    }
}
