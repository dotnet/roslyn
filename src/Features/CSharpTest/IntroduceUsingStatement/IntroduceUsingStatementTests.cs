// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.IntroduceUsingStatement;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.IntroduceUsingStatement;

[Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceUsingStatement)]
public sealed class IntroduceUsingStatementTests : AbstractCSharpCodeActionTest_NoEditor
{
    private static OptionsCollection DoNotPreferSimpleUsingStatement => new(LanguageNames.CSharp)
    {
        { CSharpCodeStyleOptions.PreferSimpleUsingStatement, new CodeStyleOption2<bool>(false, NotificationOption2.Silent) }
    };

    private static OptionsCollection PreferSimpleUsingStatement => new(LanguageNames.CSharp)
    {
        { CSharpCodeStyleOptions.PreferSimpleUsingStatement, new CodeStyleOption2<bool>(true, NotificationOption2.Silent) }
    };

    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new CSharpIntroduceUsingStatementCodeRefactoringProvider();

    private Task TestAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string initialMarkup,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string expectedMarkup,
        LanguageVersion languageVersion = LanguageVersion.CSharp7,
        OptionsCollection? options = null)
        => TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, new(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(languageVersion), options: options));

    [Theory]
    [InlineData("v[||]ar name = disposable;")]
    [InlineData("var[||] name = disposable;")]
    [InlineData("var [||]name = disposable;")]
    [InlineData("var na[||]me = disposable;")]
    [InlineData("var name[||] = disposable;")]
    [InlineData("var name [||]= disposable;")]
    [InlineData("var name =[||] disposable;")]
    [InlineData("var name = [||]disposable;")]
    [InlineData("[|var name = disposable;|]")]
    [InlineData("var name = disposable[||];")]
    [InlineData("var name = disposable;[||]")]
    [InlineData("var name = disposable[||]")]
    public Task RefactoringIsAvailableForSelection(string declaration)
        => TestAsync(
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
            """ + declaration + """
                }
            }
            """,
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    using (var name = disposable)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task RefactoringIsAvailableForVerticalSelection()
        => TestAsync(
            """
            class C
            {
                void M(System.IDisposable disposable)
                {                             [|
            """ + """
                    var name = disposable;    |]
                }
            }
            """,
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    using (var name = disposable)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task RefactoringIsAvailableForSelectionAtStartOfStatementWithPrecedingDeclaration()
        => TestAsync(
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    var ignore = disposable;
                    [||]var name = disposable;
                }
            }
            """,
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    var ignore = disposable;
                    using (var name = disposable)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task RefactoringIsAvailableForSelectionAtStartOfLineWithPrecedingDeclaration()
        => TestAsync(
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    var ignore = disposable;
            [||]        var name = disposable;
                }
            }
            """,
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    var ignore = disposable;
                    using (var name = disposable)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task RefactoringIsAvailableForSelectionAtEndOfStatementWithFollowingDeclaration()
        => TestAsync(
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    var name = disposable;[||]
                    var ignore = disposable;
                }
            }
            """,
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    using (var name = disposable)
                    {
                    }

                    var ignore = disposable;
                }
            }
            """);

    [Fact]
    public Task RefactoringIsAvailableForSelectionAtEndOfLineWithFollowingDeclaration()
        => TestAsync(
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    var name = disposable;    [||]
                    var ignore = disposable;
                }
            }
            """,
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    using (var name = disposable)
                    {
                    }

                    var ignore = disposable;
                }
            }
            """);

    [Theory]
    [InlineData("var name = d[||]isposable;")]
    [InlineData("var name = disposabl[||]e;")]
    [InlineData("var name=[|disposable|];")]
    public Task RefactoringIsNotAvailableForSelection(string declaration)
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
            """ + declaration + """
                }
            }
            """);

    [Fact]
    public Task RefactoringIsNotAvailableForDeclarationMissingInitializerExpression()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    System.IDisposable name =[||]
                }
            }
            """);

    [Fact]
    public Task RefactoringIsNotAvailableForUsingStatementDeclaration()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    using ([||]var name = disposable)
                    {
                    }
                }
            }
            """);

    [Theory]
    [InlineData("[||]System.IDisposable x = disposable, y = disposable;")]
    [InlineData("System.IDisposable [||]x = disposable, y = disposable;")]
    [InlineData("System.IDisposable x = disposable, [||]y = disposable;")]
    [InlineData("System.IDisposable x = disposable, y = disposable;[||]")]
    public Task RefactoringIsNotAvailableForMultiVariableDeclaration(string declaration)
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
            """ + declaration + """
                }
            }
            """);

    [Fact]
    public Task RefactoringIsAvailableForConstrainedGenericTypeParameter()
        => TestAsync(
            """
            class C<T> where T : System.IDisposable
            {
                void M(T disposable)
                {
                    var x = disposable;[||]
                }
            }
            """,
            """
            class C<T> where T : System.IDisposable
            {
                void M(T disposable)
                {
                    using (var x = disposable)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task RefactoringIsNotAvailableForUnconstrainedGenericTypeParameter()
        => TestMissingAsync(
            """
            class C<T>
            {
                void M(T disposable)
                {
                    var x = disposable;[||]
                }
            }
            """);

    [Fact]
    public Task LeadingCommentTriviaIsPlacedOnUsingStatement()
        => TestAsync(
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    // Comment
                    var x = disposable;[||]
                }
            }
            """,
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    // Comment
                    using (var x = disposable)
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task CommentOnTheSameLineStaysOnTheSameLine()
        => TestAsync(
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    var x = disposable;[||] // Comment
                }
            }
            """,
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    using (var x = disposable) // Comment
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task TrailingCommentTriviaOnNextLineGoesAfterBlock()
        => TestAsync(
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    var x = disposable;[||]
                    // Comment
                }
            }
            """,
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    using (var x = disposable)
                    {
                    }
                    // Comment
                }
            }
            """);

    [Fact]
    public Task ValidPreprocessorStaysValid()
        => TestAsync(
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
            #if true
                    var x = disposable;[||]
            #endif
                }
            }
            """,
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
            #if true
                    using (var x = disposable)
                    {
                    }
            #endif
                }
            }
            """);

    [Fact]
    public Task InvalidPreprocessorStaysInvalid()
        => TestAsync(
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
            #if true
                    var x = disposable;[||]
            #endif
                    _ = x;
                }
            }
            """,
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
            #if true
                    using (var x = disposable)
                    {
            #endif
                        _ = x;
                    }
                }
            }
            """);

    [Fact]
    public Task InvalidPreprocessorStaysInvalid_CSharp8()
        => TestAsync(
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
            #if true
                    var x = disposable;[||]
            #endif
                    _ = x;
                }
            }
            """,
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
            #if true
                    using var x = disposable;
            #endif
                    _ = x;
                }
            }
            """, LanguageVersion.CSharp8);

    [Fact]
    public Task StatementsAreSurroundedByMinimalScope()
        => TestAsync(
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    M(null);
                    var x = disposable;[||]
                    M(null);
                    M(x);
                    M(null);
                }
            }
            """,
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    M(null);
                    using (var x = disposable)
                    {
                        M(null);
                        M(x);
                    }

                    M(null);
                }
            }
            """);

    [Fact]
    public Task CommentsAreSurroundedExceptLinesFollowingLastUsage()
        => TestAsync(
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    var x = disposable;[||]
                    // A
                    M(x); // B
                    // C
                }
            }
            """,
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    using (var x = disposable)
                    {
                        // A
                        M(x); // B
                    }
                    // C
                }
            }
            """);

    [Fact]
    public Task WorksInSwitchSections()
        => TestAsync(
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    switch (disposable)
                    {
                        default:
                            var x = disposable;[||]
                            M(x);
                            break;
                    }
                }
            }
            """,
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    switch (disposable)
                    {
                        default:
                            using (var x = disposable)
                            {
                                M(x);
                            }

                            break;
                    }
                }
            }
            """);

    [Fact]
    public Task WorksOnStatementWithInvalidEmbeddingInIf()
        => TestAsync(
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    if (disposable != null)
                        var x = disposable;[||]
                }
            }
            """,
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    if (disposable != null)
                        using (var x = disposable)
                        {
                        }
                }
            }
            """);

    [Fact]
    public Task RefactoringIsNotAvailableOnStatementWithInvalidEmbeddingInLambda()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    new Action(() => var x = disposable[||]);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35237")]
    public Task ExpandsToIncludeSurroundedVariableDeclarations()
        => TestAsync(
            """
            using System.IO;

            class C
            {
                void M()
                {
                    var reader = new MemoryStream()[||];
                    var buffer = reader.GetBuffer();
                    buffer.Clone();
                    var a = 1;
                }
            }
            """,
            """
            using System.IO;

            class C
            {
                void M()
                {
                    using (var reader = new MemoryStream())
                    {
                        var buffer = reader.GetBuffer();
                        buffer.Clone();
                    }

                    var a = 1;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35237")]
    public Task ExpandsToIncludeSurroundedOutVariableDeclarations()
        => TestAsync(
            """
            using System.IO;

            class C
            {
                void M()
                {
                    var reader = new MemoryStream()[||];
                    var buffer = reader.GetBuffer();
                    if (!int.TryParse(buffer[0].ToString(), out var number))
                    {
                        return;
                    }
                    var a = number;
                    var b = a;
                    var c = 1;
                }
            }
            """,
            """
            using System.IO;

            class C
            {
                void M()
                {
                    using (var reader = new MemoryStream())
                    {
                        var buffer = reader.GetBuffer();
                        if (!int.TryParse(buffer[0].ToString(), out var number))
                        {
                            return;
                        }
                        var a = number;
                        var b = a;
                    }

                    var c = 1;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35237")]
    public Task ExpandsToIncludeSurroundedPatternVariableDeclarations()
        => TestAsync(
            """
            using System.IO;

            class C
            {
                void M()
                {
                    var reader = new MemoryStream()[||];
                    var buffer = reader.GetBuffer();
                    if (!(buffer[0] is int number))
                    {
                        return;
                    }
                    var a = number;
                    var b = a;
                    var c = 1;
                }
            }
            """,
            """
            using System.IO;

            class C
            {
                void M()
                {
                    using (var reader = new MemoryStream())
                    {
                        var buffer = reader.GetBuffer();
                        if (!(buffer[0] is int number))
                        {
                            return;
                        }
                        var a = number;
                        var b = a;
                    }

                    var c = 1;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35237")]
    public Task ExpandsToIncludeSurroundedMultiVariableDeclarations()
        => TestAsync(
            """
            using System.IO;

            class C
            {
                void M()
                {
                    var reader = new MemoryStream()[||];
                    var buffer = reader.GetBuffer();
                    int a = buffer[0], b = a;
                    var c = b;
                    var d = 1;
                }
            }
            """,
            """
            using System.IO;

            class C
            {
                void M()
                {
                    using (var reader = new MemoryStream())
                    {
                        var buffer = reader.GetBuffer();
                        int a = buffer[0], b = a;
                        var c = b;
                    }

                    var d = 1;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43001")]
    public Task ConsumeFollowingTryStatement1()
        => TestAsync(
            """
            using System.IO;

            class C
            {
                void M()
                {
                    var reader = new MemoryStream()[||];
                    try
                    {
                        var buffer = reader.GetBuffer();
                        int a = buffer[0], b = a;
                        var c = b;
                        var d = 1;
                    }
                    finally
                    {
                        reader.Dispose();
                    }
                }
            }
            """,
            """
            using System.IO;

            class C
            {
                void M()
                {
                    using (var reader = new MemoryStream())
                    {
                        var buffer = reader.GetBuffer();
                        int a = buffer[0], b = a;
                        var c = b;
                        var d = 1;
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43001")]
    public Task ConsumeFollowingTryStatement2()
        => TestAsync(
            """
            using System;
            using System.IO;

            class C
            {
                void M()
                {
                    var reader = new MemoryStream()[||];
                    try
                    {
                        var buffer = reader.GetBuffer();
                        int a = buffer[0], b = a;
                        var c = b;
                        var d = 1;
                    }
                    catch (Exception e)
                    {
                    }
                    finally
                    {
                        reader.Dispose();
                    }
                }
            }
            """,
            """
            using System;
            using System.IO;

            class C
            {
                void M()
                {
                    using (var reader = new MemoryStream())
                    {
                        try
                        {
                            var buffer = reader.GetBuffer();
                            int a = buffer[0], b = a;
                            var c = b;
                            var d = 1;
                        }
                        catch (Exception e)
                        {
                        }
                        finally
                        {
                            reader.Dispose();
                        }
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43001")]
    public Task ConsumeFollowingTryStatement3()
        => TestAsync(
            """
            using System;
            using System.IO;

            class C
            {
                void M()
                {
                    var reader = new MemoryStream()[||];
                    try
                    {
                        var buffer = reader.GetBuffer();
                        int a = buffer[0], b = a;
                        var c = b;
                        var d = 1;
                    }
                    finally
                    {
                    }
                }
            }
            """,
            """
            using System;
            using System.IO;

            class C
            {
                void M()
                {
                    using (var reader = new MemoryStream())
                    {
                        try
                        {
                            var buffer = reader.GetBuffer();
                            int a = buffer[0], b = a;
                            var c = b;
                            var d = 1;
                        }
                        finally
                        {
                        }
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43001")]
    public Task ConsumeFollowingTryStatement4()
        => TestAsync(
            """
            using System;
            using System.IO;

            class C
            {
                void M()
                {
                    var reader = new MemoryStream()[||];
                    try
                    {
                        var buffer = reader.GetBuffer();
                        int a = buffer[0], b = a;
                        var c = b;
                        var d = 1;
                    }
                    finally
                    {
                        return;
                    }
                }
            }
            """,
            """
            using System;
            using System.IO;

            class C
            {
                void M()
                {
                    using (var reader = new MemoryStream())
                    {
                        try
                        {
                            var buffer = reader.GetBuffer();
                            int a = buffer[0], b = a;
                            var c = b;
                            var d = 1;
                        }
                        finally
                        {
                            return;
                        }
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43001")]
    public Task ConsumeFollowingTryStatement5()
        => TestAsync(
            """
            using System;
            using System.IO;

            class C
            {
                void M()
                {
                    var reader = new MemoryStream()[||];
                    try
                    {
                        var buffer = reader.GetBuffer();
                        int a = buffer[0], b = a;
                        var c = b;
                        var d = 1;
                    }
                    finally
                    {
                        reader = null;
                    }
                }
            }
            """,
            """
            using System;
            using System.IO;

            class C
            {
                void M()
                {
                    using (var reader = new MemoryStream())
                    {
                        try
                        {
                            var buffer = reader.GetBuffer();
                            int a = buffer[0], b = a;
                            var c = b;
                            var d = 1;
                        }
                        finally
                        {
                            reader = null;
                        }
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43001")]
    public Task ConsumeFollowingTryStatement6()
        => TestAsync(
            """
            using System;
            using System.IO;

            class C
            {
                void M()
                {
                    var reader = new MemoryStream()[||];
                    try
                    {
                        var buffer = reader.GetBuffer();
                        int a = buffer[0], b = a;
                        var c = b;
                        var d = 1;
                    }
                    finally
                    {
                        Dispose();
                    }
                }
            }
            """,
            """
            using System;
            using System.IO;

            class C
            {
                void M()
                {
                    using (var reader = new MemoryStream())
                    {
                        try
                        {
                            var buffer = reader.GetBuffer();
                            int a = buffer[0], b = a;
                            var c = b;
                            var d = 1;
                        }
                        finally
                        {
                            Dispose();
                        }
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43001")]
    public Task ConsumeFollowingTryStatement7()
        => TestAsync(
            """
            using System;
            using System.IO;

            class C
            {
                void M()
                {
                    var reader = new MemoryStream()[||];
                    try
                    {
                        var buffer = reader.GetBuffer();
                        int a = buffer[0], b = a;
                        var c = b;
                        var d = 1;
                    }
                    finally
                    {
                        reader.X();
                    }
                }
            }
            """,
            """
            using System;
            using System.IO;

            class C
            {
                void M()
                {
                    using (var reader = new MemoryStream())
                    {
                        try
                        {
                            var buffer = reader.GetBuffer();
                            int a = buffer[0], b = a;
                            var c = b;
                            var d = 1;
                        }
                        finally
                        {
                            reader.X();
                        }
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43001")]
    public Task ConsumeFollowingTryStatement8()
        => TestAsync(
            """
            using System;
            using System.IO;

            class C
            {
                void M()
                {
                    var reader = new MemoryStream()[||];
                    try
                    {
                        var buffer = reader.GetBuffer();
                        int a = buffer[0], b = a;
                        var c = b;
                        var d = 1;
                    }
                    finally
                    {
                        other.Dispose();
                    }
                }
            }
            """,
            """
            using System;
            using System.IO;

            class C
            {
                void M()
                {
                    using (var reader = new MemoryStream())
                    {
                        try
                        {
                            var buffer = reader.GetBuffer();
                            int a = buffer[0], b = a;
                            var c = b;
                            var d = 1;
                        }
                        finally
                        {
                            other.Dispose();
                        }
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33699")]
    public Task StatementsAreSurroundedByMinimalScope1_CSharp8()
        => TestAsync(
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    M(null);
                    var x = disposable;[||]
                    M(null);
                    M(x);
                    M(null);
                }
            }
            """,
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    M(null);
                    using (var x = disposable)
                    {
                        M(null);
                        M(x);
                    }

                    M(null);
                }
            }
            """, LanguageVersion.CSharp8);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33699")]
    public Task StatementsAreSurroundedByMinimalScope2_CSharp8()
        => TestAsync(
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    M(null);
                    var x = disposable;[||]
                    M(null);
                    M(x);
                }
            }
            """,
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    M(null);
                    using var x = disposable;
                    M(null);
                    M(x);
                }
            }
            """, LanguageVersion.CSharp8);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33699")]
    public Task StatementsAreSurroundedByMinimalScope3_CSharp8()
        => TestAsync(
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    M(null);
                    // leading comment
                    var x = disposable;[||]
                    M(null);
                    M(x);
                }
            }
            """,
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    M(null);
                    // leading comment
                    using var x = disposable;
                    M(null);
                    M(x);
                }
            }
            """, LanguageVersion.CSharp8);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33699")]
    public Task StatementsAreSurroundedByMinimalScope4_CSharp8()
        => TestAsync(
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    switch (0)
                    {
                        case 0:
                            M(null);
                            var x = disposable;[||]
                            M(null);
                            M(x);

                        case 1:
                    }
                }
            }
            """,
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    switch (0)
                    {
                        case 0:
                            M(null);
                            using (var x = disposable)
                            {
                                M(null);
                                M(x);
                            }

                        case 1:
                    }
                }
            }
            """, LanguageVersion.CSharp8);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33699")]
    public Task StatementsAreSurroundedByMinimalScope5_CSharp8()
        => TestAsync(
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    switch (0)
                    {
                        case 0:
                        {
                            M(null);
                            var x = disposable;[||]
                            M(null);
                            M(x);
                        }

                        case 1:
                    }
                }
            }
            """,
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    switch (0)
                    {
                        case 0:
                        {
                            M(null);
                            using var x = disposable;
                            M(null);
                            M(x);
                        }

                        case 1:
                    }
                }
            }
            """, LanguageVersion.CSharp8);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37260")]
    public Task TestExpressionStatement()
        => TestAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    [||]MethodThatReturnsDisposableThing();
                    Console.WriteLine();
                }

                IDisposable MethodThatReturnsDisposableThing() => null;
            }
            """,
            """
            using System;
            
            class C
            {
                void M()
                {
                    using (MethodThatReturnsDisposableThing())
                    {
                        Console.WriteLine();
                    }
                }
            
                IDisposable MethodThatReturnsDisposableThing() => null;
            }
            """, options: DoNotPreferSimpleUsingStatement);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37260")]
    public Task TestExpressionStatement_PreferSimpleUsingStatement1()
        => TestAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    [||]MethodThatReturnsDisposableThing();
                    Console.WriteLine();
                }

                IDisposable MethodThatReturnsDisposableThing() => null;
            }
            """,
            """
            using System;
            
            class C
            {
                void M()
                {
                    using var _ = MethodThatReturnsDisposableThing();
                    Console.WriteLine();
                }
            
                IDisposable MethodThatReturnsDisposableThing() => null;
            }
            """, options: PreferSimpleUsingStatement);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37260")]
    public Task TestExpressionStatement_PreferSimpleUsingStatement2()
        => TestAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    var _ = true;
                    [||]MethodThatReturnsDisposableThing();
                    Console.WriteLine();
                }

                IDisposable MethodThatReturnsDisposableThing() => null;
            }
            """,
            """
            using System;
            
            class C
            {
                void M()
                {
                    var _ = true;
                    using var _1 = MethodThatReturnsDisposableThing();
                    Console.WriteLine();
                }
            
                IDisposable MethodThatReturnsDisposableThing() => null;
            }
            """, options: PreferSimpleUsingStatement);
}
