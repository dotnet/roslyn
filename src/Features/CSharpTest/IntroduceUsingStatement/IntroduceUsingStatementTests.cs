// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.IntroduceUsingStatement;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.IntroduceUsingStatement;

[Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceUsingStatement)]
public sealed class IntroduceUsingStatementTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new CSharpIntroduceUsingStatementCodeRefactoringProvider();

    private Task TestAsync(string initialMarkup, string expectedMarkup, LanguageVersion languageVersion = LanguageVersion.CSharp7)
        => TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, parseOptions: CSharpParseOptions.Default.WithLanguageVersion(languageVersion));

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
    public async Task RefactoringIsAvailableForSelection(string declaration)
    {
        await TestAsync(
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
    }

    [Fact]
    public async Task RefactoringIsAvailableForVerticalSelection()
    {
        await TestAsync(
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
    }

    [Fact]
    public async Task RefactoringIsAvailableForSelectionAtStartOfStatementWithPrecedingDeclaration()
    {
        await TestAsync(
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
    }

    [Fact]
    public async Task RefactoringIsAvailableForSelectionAtStartOfLineWithPrecedingDeclaration()
    {
        await TestAsync(
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
    }

    [Fact]
    public async Task RefactoringIsAvailableForSelectionAtEndOfStatementWithFollowingDeclaration()
    {
        await TestAsync(
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
    }

    [Fact]
    public async Task RefactoringIsAvailableForSelectionAtEndOfLineWithFollowingDeclaration()
    {
        await TestAsync(
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
    }

    [Theory]
    [InlineData("var name = d[||]isposable;")]
    [InlineData("var name = disposabl[||]e;")]
    [InlineData("var name=[|disposable|];")]
    public async Task RefactoringIsNotAvailableForSelection(string declaration)
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
            """ + declaration + """
                }
            }
            """);
    }

    [Fact]
    public async Task RefactoringIsNotAvailableForDeclarationMissingInitializerExpression()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    System.IDisposable name =[||]
                }
            }
            """);
    }

    [Fact]
    public async Task RefactoringIsNotAvailableForUsingStatementDeclaration()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Theory]
    [InlineData("[||]System.IDisposable x = disposable, y = disposable;")]
    [InlineData("System.IDisposable [||]x = disposable, y = disposable;")]
    [InlineData("System.IDisposable x = disposable, [||]y = disposable;")]
    [InlineData("System.IDisposable x = disposable, y = disposable;[||]")]
    public async Task RefactoringIsNotAvailableForMultiVariableDeclaration(string declaration)
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
            """ + declaration + """
                }
            }
            """);
    }

    [Fact]
    public async Task RefactoringIsAvailableForConstrainedGenericTypeParameter()
    {
        await TestAsync(
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
    }

    [Fact]
    public async Task RefactoringIsNotAvailableForUnconstrainedGenericTypeParameter()
    {
        await TestMissingAsync(
            """
            class C<T>
            {
                void M(T disposable)
                {
                    var x = disposable;[||]
                }
            }
            """);
    }

    [Fact]
    public async Task LeadingCommentTriviaIsPlacedOnUsingStatement()
    {
        await TestAsync(
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
    }

    [Fact]
    public async Task CommentOnTheSameLineStaysOnTheSameLine()
    {
        await TestAsync(
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
    }

    [Fact]
    public async Task TrailingCommentTriviaOnNextLineGoesAfterBlock()
    {
        await TestAsync(
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
    }

    [Fact]
    public async Task ValidPreprocessorStaysValid()
    {
        await TestAsync(
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
    }

    [Fact]
    public async Task InvalidPreprocessorStaysInvalid()
    {
        await TestAsync(
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
    }

    [Fact]
    public async Task InvalidPreprocessorStaysInvalid_CSharp8()
    {
        await TestAsync(
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
    }

    [Fact]
    public async Task StatementsAreSurroundedByMinimalScope()
    {
        await TestAsync(
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
    }

    [Fact]
    public async Task CommentsAreSurroundedExceptLinesFollowingLastUsage()
    {
        await TestAsync(
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
    }

    [Fact]
    public async Task WorksInSwitchSections()
    {
        await TestAsync(
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
    }

    [Fact]
    public async Task WorksOnStatementWithInvalidEmbeddingInIf()
    {
        await TestAsync(
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
    }

    [Fact]
    public async Task RefactoringIsNotAvailableOnStatementWithInvalidEmbeddingInLambda()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(System.IDisposable disposable)
                {
                    new Action(() => var x = disposable[||]);
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35237")]
    public async Task ExpandsToIncludeSurroundedVariableDeclarations()
    {
        await TestAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35237")]
    public async Task ExpandsToIncludeSurroundedOutVariableDeclarations()
    {
        await TestAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35237")]
    public async Task ExpandsToIncludeSurroundedPatternVariableDeclarations()
    {
        await TestAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35237")]
    public async Task ExpandsToIncludeSurroundedMultiVariableDeclarations()
    {
        await TestAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43001")]
    public async Task ConsumeFollowingTryStatement1()
    {
        await TestAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43001")]
    public async Task ConsumeFollowingTryStatement2()
    {
        await TestAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43001")]
    public async Task ConsumeFollowingTryStatement3()
    {
        await TestAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43001")]
    public async Task ConsumeFollowingTryStatement4()
    {
        await TestAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43001")]
    public async Task ConsumeFollowingTryStatement5()
    {
        await TestAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43001")]
    public async Task ConsumeFollowingTryStatement6()
    {
        await TestAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43001")]
    public async Task ConsumeFollowingTryStatement7()
    {
        await TestAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43001")]
    public async Task ConsumeFollowingTryStatement8()
    {
        await TestAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33699")]
    public async Task StatementsAreSurroundedByMinimalScope1_CSharp8()
    {
        await TestAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33699")]
    public async Task StatementsAreSurroundedByMinimalScope2_CSharp8()
    {
        await TestAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33699")]
    public async Task StatementsAreSurroundedByMinimalScope3_CSharp8()
    {
        await TestAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33699")]
    public async Task StatementsAreSurroundedByMinimalScope4_CSharp8()
    {
        await TestAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33699")]
    public async Task StatementsAreSurroundedByMinimalScope5_CSharp8()
    {
        await TestAsync(
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
    }
}
