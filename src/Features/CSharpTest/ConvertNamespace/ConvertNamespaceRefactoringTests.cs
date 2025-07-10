// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.ConvertNamespace;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertNamespace;

using VerifyCS = CSharpCodeRefactoringVerifier<ConvertNamespaceCodeRefactoringProvider>;

[UseExportProvider]
public sealed class ConvertNamespaceRefactoringTests
{
    public static IEnumerable<object[]> EndOfDocumentSequences => [[""], ["\r\n"]];

    #region Convert To File Scoped

    [Fact]
    public Task TestNoConvertToFileScopedInCSharp9()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace $$N
            {
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestNoConvertToFileScopedInCSharp10WithFileScopedPreference()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace $$N
            {
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToFileScopedInCSharp10WithBlockScopedPreference()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace $$N
            {
            }
            """,
            FixedCode = """
            namespace $$N;
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestOnNamespaceToken()
        => new VerifyCS.Test
        {
            TestCode = """
            $$namespace N
            {
            }
            """,
            FixedCode = """
            namespace N;
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestNotBeforeNamespaceToken()
        => new VerifyCS.Test
        {
            TestCode = """
            $$
            namespace N
            {
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestNotOnOpenBrace()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace N
            $${
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestNoConvertWithMultipleNamespaces()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace $$N
            {
            }

            namespace N2
            {
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestNoConvertWithNestedNamespaces1()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace $$N
            {
                namespace N2
                {
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestNoConvertWithNestedNamespaces2()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace N
            {
                namespace $$N2
                {
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestNoConvertWithTopLevelStatement1()
        => new VerifyCS.Test
        {
            TestCode = """
            {|CS8805:int i = 0;|}

            namespace $$N
            {
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestNoConvertWithTopLevelStatement2()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace $$N
            {
            }

            {|CS8805:{|CS8803:int i = 0;|}|}
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToFileScopedWithUsing1()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            namespace $$N
            {
            }
            """,
            FixedCode = """
            using System;

            namespace $$N;
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToFileScopedWithUsing2()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace $$N
            {
                using System;
            }
            """,
            FixedCode = """
            namespace $$N;

            using System;
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToFileScopedWithClass()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace $$N
            {
                class C
                {
                }
            }
            """,
            FixedCode = """
            namespace $$N;

            class C
            {
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToFileScopedWithClassWithDocComment()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace $$N
            {
                /// <summary/>
                class C
                {
                }
            }
            """,
            FixedCode = """
            namespace $$N;

            /// <summary/>
            class C
            {
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToFileScopedWithMissingCloseBrace()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace $$N
            {
                /// <summary/>
                class C
                {
                }{|CS1513:|}
            """,
            FixedCode = """
            namespace N;

            /// <summary/>
            class C
            {
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToFileScopedWithCommentOnOpenCurly()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace $$N
            { // comment
                class C
                {
                }
            }
            """,
            FixedCode = """
            namespace $$N;
            // comment
            class C
            {
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToFileScopedWithLeadingComment()
        => new VerifyCS.Test
        {
            TestCode = """
            // copyright
            namespace $$N
            {
                class C
                {
                }
            }
            """,
            FixedCode = """
            // copyright
            namespace $$N;

            class C
            {
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
            }
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57564")]
    public Task TextConvertToFileScopedWithCommentedOutContents()
        => new VerifyCS.Test
        {
            TestCode = """
            $$namespace N
            {
                // public class C
                // {
                // }
            }
            """,
            FixedCode = """
            namespace N;

            // public class C
            // {
            // }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
            }
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57564")]
    public Task TextConvertToFileScopedWithCommentedAfterContents()
        => new VerifyCS.Test
        {
            TestCode = """
            $$namespace N
            {
                public class C
                {
                }

                // I'll probably write some more code here later
            }
            """,
            FixedCode = """
            namespace N;

            public class C
            {
            }

            // I'll probably write some more code here later
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
            }
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57564")]
    public Task TextConvertToFileScopedWithTriviaAroundNamespace1()
        => new VerifyCS.Test
        {
            TestCode = """
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
            """,
            FixedCode = """
            #if !NONEXISTENT
            namespace NDebug;
            #else
            namespace NRelease
            #endif

            public class C
            {
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
            }
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57564")]
    public Task TextConvertToFileScopedWithTriviaAroundNamespace2()
        => new VerifyCS.Test
        {
            TestCode = """
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
            """,
            FixedCode = """
            #if NONEXISTENT
            namespace NDebug
            #else
            namespace NRelease;
            #endif

            public class C
            {
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
            }
        }.RunAsync();

    #endregion

    #region Convert To Block Scoped

    [Theory]
    [MemberData(nameof(EndOfDocumentSequences))]
    public Task TestConvertToBlockScopedInCSharp9(string endOfDocumentSequence)
        => new VerifyCS.Test
        {
            TestCode = $$"""
            {|CS8773:namespace|} $$N;{{endOfDocumentSequence}}
            """,
            FixedCode = $$"""
            namespace $$N
            {
            }{{endOfDocumentSequence}}
            """,
            LanguageVersion = LanguageVersion.CSharp9,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestNoConvertToBlockScopedInCSharp10WithBlockScopedPreference()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace $$N;
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.BlockScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToBlockScopedInCSharp10WithFileScopedPreference()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace $$N;
            """,
            FixedCode = """
            namespace $$N
            {
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToBlockOnNamespaceToken2()
        => new VerifyCS.Test
        {
            TestCode = """
            $$namespace N;
            """,
            FixedCode = """
            namespace N
            {
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToBlockNotBeforeNamespaceToken2()
        => new VerifyCS.Test
        {
            TestCode = """
            $$
            namespace N;
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToBlockNotAfterSemicolon()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace N;
            $$
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToBlockAfterSemicolon()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace N; $$
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToBlockWithMultipleNamespaces()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace $$N;

            namespace {|CS8955:N2|}
            {
            }
            """,
            FixedCode = """
            namespace $$N
            {
                namespace N2
                {
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToBlockWithNestedNamespaces1()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace $$N;

            namespace {|CS8954:N2|};
            """,
            FixedCode = """
            namespace $$N
            {
                namespace {|CS8955:N2|};
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToBlockWithNestedNamespaces2()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace N
            {
                namespace $${|CS8955:N2|};
            }
            """,
            FixedCode = """
            namespace N
            {
                namespace $$N2
                {
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToBlockWithTopLevelStatement1()
        => new VerifyCS.Test
        {
            TestCode = """
            {|CS8805:int i = 0;|}

            namespace $${|CS8956:N|};
            """,
            FixedCode = """
            {|CS8805:int i = 0;|}

            namespace $$N
            {
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToBlockWithTopLevelStatement2()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace $$N;

            int {|CS0116:i|} = 0;
            """,
            FixedCode = """
            namespace $$N
            {
                int {|CS0116:i|} = 0;
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToBlockScopedWithUsing1()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            namespace $$N;
            """,
            FixedCode = """
            using System;

            namespace $$N
            {
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToBlockScopedWithUsing2()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace $$N;

            using System;
            """,
            FixedCode = """
            namespace $$N
            {
                using System;
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToBlockScopedWithClass()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace $$N;

            class C
            {
            }
            """,
            FixedCode = """
            namespace $$N
            {
                class C
                {
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToBlockScopedWithClassWithDocComment()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace $$N;

            /// <summary/>
            class C
            {
            }
            """,
            FixedCode = """
            namespace $$N
            {
                /// <summary/>
                class C
                {
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToBlockScopedWithMissingCloseBrace()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace $$N;

            /// <summary/>
            class C
            {{|CS1513:|}
            """,
            FixedCode = """
            namespace N
            {
                /// <summary/>
                class C
                {
            }{|CS1513:|}
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            CodeActionValidationMode = CodeActionValidationMode.None,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToBlockScopedWithCommentOnSemicolon()
        => new VerifyCS.Test
        {
            TestCode = """
            namespace $$N; // comment

            class C
            {
            }
            """,
            FixedCode = """
            namespace $$N
            { // comment
                class C
                {
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToBlockScopedWithLeadingComment()
        => new VerifyCS.Test
        {
            TestCode = """
            // copyright
            namespace $$N;

            class C
            {
            }
            """,
            FixedCode = """
            // copyright
            namespace $$N
            {
                class C
                {
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.NamespaceDeclarations, NamespaceDeclarationPreference.FileScoped }
            }
        }.RunAsync();

    #endregion
}
