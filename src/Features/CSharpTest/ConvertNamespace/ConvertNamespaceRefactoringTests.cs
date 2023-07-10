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
    using VerifyCS = CSharpCodeRefactoringVerifier<ConvertNamespaceCodeRefactoringProvider>;

    public class ConvertNamespaceRefactoringTests
    {
        #region Convert To File Scoped

        [Fact]
        public async Task TestNoConvertToFileScopedInCSharp9()
        {
            var code = @"
namespace $$N
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
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestNoConvertToFileScopedInCSharp10WithFileScopedPreference()
        {
            var code = @"
namespace $$N
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
        public async Task TestConvertToFileScopedInCSharp10WithBlockScopedPreference()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
namespace $$N
{
}
",
                FixedCode = @"
namespace $$N;
",
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestOnNamespaceToken()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
$$namespace N
{
}
",
                FixedCode = @"
namespace N;
",
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestNotBeforeNamespaceToken()
        {
            var code = @"
$$
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
        public async Task TestNotOnOpenBrace()
        {
            var code = @"
namespace N
$${
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
        public async Task TestNoConvertWithMultipleNamespaces()
        {
            var code = @"
namespace $$N
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
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestNoConvertWithNestedNamespaces1()
        {
            var code = @"
namespace $$N
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
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestNoConvertWithNestedNamespaces2()
        {
            var code = @"
namespace N
{
    namespace $$N2
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
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestNoConvertWithTopLevelStatement1()
        {
            var code = @"
int i = 0;

namespace $$N
{
}
";
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp10,
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(2,1): error CS8805: Program using top-level statements must be an executable.
                    DiagnosticResult.CompilerError("CS8805").WithSpan(2, 1, 2, 11),
                },
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestNoConvertWithTopLevelStatement2()
        {
            var code = @"
namespace $$N
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
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
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

namespace $$N
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
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestConvertToFileScopedWithUsing2()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
namespace $$N
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
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestConvertToFileScopedWithClass()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
namespace $$N
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
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestConvertToFileScopedWithClassWithDocComment()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
namespace $$N
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
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestConvertToFileScopedWithMissingCloseBrace()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
namespace $$N
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
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestConvertToFileScopedWithCommentOnOpenCurly()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
namespace $$N
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
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
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
namespace $$N
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
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
                }
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57564")]
        public async Task TextConvertToFileScopedWithCommentedOutContents()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
$$namespace N
{
    // public class C
    // {
    // }
}
",
                FixedCode = @"
namespace N;

// public class C
// {
// }
",
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
                }
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57564")]
        public async Task TextConvertToFileScopedWithCommentedAfterContents()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
$$namespace N
{
    public class C
    {
    }

    // I'll probably write some more code here later
}
",
                FixedCode = @"
namespace N;

public class C
{
}

// I'll probably write some more code here later
",
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
                }
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57564")]
        public async Task TextConvertToFileScopedWithTriviaAroundNamespace1()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
#if !NONEXISTENT
$$namespace NDebug
#else
namespace NRelease
#endif
{
    public class C
    {
    }
}
",
                FixedCode = @"
#if !NONEXISTENT
namespace NDebug;
#else
namespace NRelease
#endif

public class C
{
}
",
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
                }
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57564")]
        public async Task TextConvertToFileScopedWithTriviaAroundNamespace2()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
#if NONEXISTENT
namespace NDebug
#else
$$namespace NRelease
#endif
{
    public class C
    {
    }
}
",
                FixedCode = @"
#if NONEXISTENT
namespace NDebug
#else
namespace NRelease;
#endif

public class C
{
}
",
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
                }
            }.RunAsync();
        }

        #endregion

        #region Convert To Block Scoped

        [Fact]
        public async Task TestConvertToBlockScopedInCSharp9()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
{|CS8773:namespace|} $$N;
",
                FixedCode = @"
namespace $$N
{
}",
                LanguageVersion = LanguageVersion.CSharp9,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestNoConvertToBlockScopedInCSharp10WithBlockScopedPreference()
        {
            var code = @"
namespace $$N;
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
        public async Task TestConvertToBlockScopedInCSharp10WithFileScopedPreference()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
namespace $$N;
",
                FixedCode = @"
namespace $$N
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
        public async Task TestConvertToBlockOnNamespaceToken2()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
$$namespace N;
",
                FixedCode = @"
namespace N
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
        public async Task TestConvertToBlockNotBeforeNamespaceToken2()
        {
            var code = @"
$$
namespace N;
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
        public async Task TestConvertToBlockNotAfterSemicolon()
        {
            var code = @"
namespace N;
$$
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
        public async Task TestConvertToBlockAfterSemicolon()
        {
            var code = @"
namespace N; $$
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
        public async Task TestConvertToBlockWithMultipleNamespaces()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
namespace $$N;

namespace {|CS8955:N2|}
{
}
",
                FixedCode = @"
namespace $$N
{
    namespace N2
    {
    }
}",
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestConvertToBlockWithNestedNamespaces1()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
namespace $$N;

namespace {|CS8954:N2|};",
                FixedCode = @"
namespace $$N
{
    namespace {|CS8955:N2|};
}",
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestConvertToBlockWithNestedNamespaces2()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
namespace N
{
    namespace $${|CS8955:N2|};
}
",
                FixedCode = @"
namespace N
{
    namespace $$N2
    {
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
        public async Task TestConvertToBlockWithTopLevelStatement1()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
{|CS8805:int i = 0;|}

namespace $${|CS8956:N|};
",
                FixedCode = @"
{|CS8805:int i = 0;|}

namespace $$N
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
        public async Task TestConvertToBlockWithTopLevelStatement2()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
namespace $$N;

int {|CS0116:i|} = 0;
",
                FixedCode = @"
namespace $$N
{
    int {|CS0116:i|} = 0;
}",
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestConvertToBlockScopedWithUsing1()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
using System;

namespace $$N;
",
                FixedCode = @"
using System;

namespace $$N
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
        public async Task TestConvertToBlockScopedWithUsing2()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
namespace $$N;

using System;
",
                FixedCode = @"
namespace $$N
{
    using System;
}",
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestConvertToBlockScopedWithClass()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
namespace $$N;

class C
{
}
",
                FixedCode = @"
namespace $$N
{
    class C
    {
    }
}",
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestConvertToBlockScopedWithClassWithDocComment()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
namespace $$N;

/// <summary/>
class C
{
}
",
                FixedCode = @"
namespace $$N
{
    /// <summary/>
    class C
    {
    }
}",
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestConvertToBlockScopedWithMissingCloseBrace()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
namespace $$N;

/// <summary/>
class C
{{|CS1513:|}",
                FixedCode = @"
namespace N
{
    /// <summary/>
    class C
    {}{|CS1513:|}",
                LanguageVersion = LanguageVersion.CSharp10,
                CodeActionValidationMode = CodeActionValidationMode.None,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestConvertToBlockScopedWithCommentOnSemicolon()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
namespace $$N; // comment

class C
{
}
",
                FixedCode = @"
namespace $$N
{ // comment
    class C
    {
    }
}",
                LanguageVersion = LanguageVersion.CSharp10,
                Options =
                {
                    { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestConvertToBlockScopedWithLeadingComment()
        {
            await new VerifyCS.Test
            {
                TestCode = @"
// copyright
namespace $$N;

class C
{
}
",
                FixedCode = @"
// copyright
namespace $$N
{
    class C
    {
    }
}",
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
