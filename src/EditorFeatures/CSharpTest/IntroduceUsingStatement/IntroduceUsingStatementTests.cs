// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.IntroduceUsingStatement;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.IntroduceUsingStatement
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceUsingStatement)]
    public sealed class IntroduceUsingStatementTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpIntroduceUsingStatementCodeRefactoringProvider();

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
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(System.IDisposable disposable)
    {
        " + declaration + @"
    }
}",
@"class C
{
    void M(System.IDisposable disposable)
    {
        using (var name = disposable)
        {
        }
    }
}");
        }

        [Fact]
        public async Task RefactoringIsAvailableForVerticalSelection()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(System.IDisposable disposable)
    {                             [|    " + @"
        var name = disposable;    |]
    }
}",
@"class C
{
    void M(System.IDisposable disposable)
    {
        using (var name = disposable)
        {
        }
    }
}");
        }

        [Fact]
        public async Task RefactoringIsAvailableForSelectionAtStartOfStatementWithPrecedingDeclaration()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(System.IDisposable disposable)
    {
        var ignore = disposable;
        [||]var name = disposable;
    }
}",
@"class C
{
    void M(System.IDisposable disposable)
    {
        var ignore = disposable;
        using (var name = disposable)
        {
        }
    }
}");
        }

        [Fact]
        public async Task RefactoringIsAvailableForSelectionAtStartOfLineWithPrecedingDeclaration()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(System.IDisposable disposable)
    {
        var ignore = disposable;
[||]        var name = disposable;
    }
}",
@"class C
{
    void M(System.IDisposable disposable)
    {
        var ignore = disposable;
        using (var name = disposable)
        {
        }
    }
}");
        }

        [Fact]
        public async Task RefactoringIsAvailableForSelectionAtEndOfStatementWithFollowingDeclaration()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(System.IDisposable disposable)
    {
        var name = disposable;[||]
        var ignore = disposable;
    }
}",
@"class C
{
    void M(System.IDisposable disposable)
    {
        using (var name = disposable)
        {
        }
        var ignore = disposable;
    }
}");
        }

        [Fact]
        public async Task RefactoringIsAvailableForSelectionAtEndOfLineWithFollowingDeclaration()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(System.IDisposable disposable)
    {
        var name = disposable;    [||]
        var ignore = disposable;
    }
}",
@"class C
{
    void M(System.IDisposable disposable)
    {
        using (var name = disposable)
        {
        }
        var ignore = disposable;
    }
}");
        }

        [Theory]
        [InlineData("var name = d[||]isposable;")]
        [InlineData("var name = disposabl[||]e;")]
        [InlineData("var name=[|disposable|];")]
        public async Task RefactoringIsNotAvailableForSelection(string declaration)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(System.IDisposable disposable)
    {
        " + declaration + @"
    }
}");
        }

        [Fact]
        public async Task RefactoringIsNotAvailableForDeclarationMissingInitializerExpression()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(System.IDisposable disposable)
    {
        System.IDisposable name =[||]
    }
}");
        }

        [Fact]
        public async Task RefactoringIsNotAvailableForUsingStatementDeclaration()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(System.IDisposable disposable)
    {
        using ([||]var name = disposable)
        {
        }
    }
}");
        }

        [Theory]
        [InlineData("[||]System.IDisposable x = disposable, y = disposable;")]
        [InlineData("System.IDisposable [||]x = disposable, y = disposable;")]
        [InlineData("System.IDisposable x = disposable, [||]y = disposable;")]
        [InlineData("System.IDisposable x = disposable, y = disposable;[||]")]
        public async Task RefactoringIsNotAvailableForMultiVariableDeclaration(string declaration)
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(System.IDisposable disposable)
    {
        " + declaration + @"
    }
}");
        }

        [Fact]
        public async Task RefactoringIsAvailableForConstrainedGenericTypeParameter()
        {
            await TestInRegularAndScriptAsync(
@"class C<T> where T : System.IDisposable
{
    void M(T disposable)
    {
        var x = disposable;[||]
    }
}",
@"class C<T> where T : System.IDisposable
{
    void M(T disposable)
    {
        using (var x = disposable)
        {
        }
    }
}");
        }

        [Fact]
        public async Task RefactoringIsNotAvailableForUnconstrainedGenericTypeParameter()
        {
            await TestMissingAsync(
@"class C<T>
{
    void M(T disposable)
    {
        var x = disposable;[||]
    }
}");
        }

        [Fact]
        public async Task LeadingCommentTriviaIsPlacedOnUsingStatement()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(System.IDisposable disposable)
    {
        // Comment
        var x = disposable;[||]
    }
}",
@"class C
{
    void M(System.IDisposable disposable)
    {
        // Comment
        using (var x = disposable)
        {
        }
    }
}");
        }

        [Fact]
        public async Task CommentOnTheSameLineStaysOnTheSameLine()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(System.IDisposable disposable)
    {
        var x = disposable;[||] // Comment
    }
}",
@"class C
{
    void M(System.IDisposable disposable)
    {
        using (var x = disposable) // Comment
        {
        }
    }
}");
        }

        [Fact]
        public async Task TrailingCommentTriviaOnNextLineGoesAfterBlock()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(System.IDisposable disposable)
    {
        var x = disposable;[||]
        // Comment
    }
}",
@"class C
{
    void M(System.IDisposable disposable)
    {
        using (var x = disposable)
        {
        }
        // Comment
    }
}");
        }

        [Fact]
        public async Task ValidPreprocessorStaysValid()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(System.IDisposable disposable)
    {
#if true
        var x = disposable;[||]
#endif
    }
}",
@"class C
{
    void M(System.IDisposable disposable)
    {
#if true
        using (var x = disposable)
        {
        }
#endif
    }
}");
        }

        [Fact]
        public async Task InvalidPreprocessorStaysInvalid()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(System.IDisposable disposable)
    {
#if true
        var x = disposable;[||]
#endif
        _ = x;
    }
}",
@"class C
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
}");
        }

        [Fact]
        public async Task StatementsAreSurroundedByMinimalScope()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(System.IDisposable disposable)
    {
        M(null);
        var x = disposable;[||]
        M(null);
        M(x);
        M(null);
    }
}",
@"class C
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
}");
        }

        [Fact]
        public async Task CommentsAreSurroundedExceptLinesFollowingLastUsage()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(System.IDisposable disposable)
    {
        var x = disposable;[||]
        // A
        M(x); // B
        // C
    }
}",
@"class C
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
}");
        }

        [Fact]
        public async Task WorksInSwitchSections()
        {
            await TestInRegularAndScriptAsync(
@"class C
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
}",
@"class C
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
}");
        }

        [Fact]
        public async Task WorksOnStatementWithInvalidEmbeddingInIf()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(System.IDisposable disposable)
    {
        if (disposable != null)
            var x = disposable;[||]
    }
}",
@"class C
{
    void M(System.IDisposable disposable)
    {
        if (disposable != null)
            using (var x = disposable)
            {
            }
    }
}");
        }

        [Fact]
        public async Task RefactoringIsNotAvailableOnStatementWithInvalidEmbeddingInLambda()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(System.IDisposable disposable)
    {
        new Action(() => var x = disposable[||]);
    }
}");
        }

        [Fact]
        [WorkItem(35237, "https://github.com/dotnet/roslyn/issues/35237")]
        public async Task ExpandsToIncludeSurroundedVariableDeclarations()
        {
            await TestInRegularAndScriptAsync(
@"using System.IO;

class C
{
    void M()
    {
        var reader = new MemoryStream()[||];
        var buffer = reader.GetBuffer();
        buffer.Clone();
        var a = 1;
    }
}",
@"using System.IO;

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
}");
        }

        [Fact]
        [WorkItem(35237, "https://github.com/dotnet/roslyn/issues/35237")]
        public async Task ExpandsToIncludeSurroundedOutVariableDeclarations()
        {
            await TestInRegularAndScriptAsync(
@"using System.IO;

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
}",
@"using System.IO;

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
}");
        }

        [Fact]
        [WorkItem(35237, "https://github.com/dotnet/roslyn/issues/35237")]
        public async Task ExpandsToIncludeSurroundedPatternVariableDeclarations()
        {
            await TestInRegularAndScriptAsync(
@"using System.IO;

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
}",
@"using System.IO;

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
}");
        }

        [Fact]
        [WorkItem(35237, "https://github.com/dotnet/roslyn/issues/35237")]
        public async Task ExpandsToIncludeSurroundedMultiVariableDeclarations()
        {
            await TestInRegularAndScriptAsync(
@"using System.IO;

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
}",
@"using System.IO;

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
}");
        }
    }
}
