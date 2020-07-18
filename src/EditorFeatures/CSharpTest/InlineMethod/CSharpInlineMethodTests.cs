using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InlineMethod;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.InlineMethod
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsInlineMethod)]
    public class CSharpInlineMethodTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => ((TestWorkspace)workspace).ExportProvider.GetExportedValue<CSharpInlineMethodRefactoringProvider>();

        [Fact]
        public Task TestInlineSingleStatement()
            => TestInRegularAndScript1Async(@"
public class TestClass
{
    private void Caller(int i, int j)
    {
        Ca[||]llee(i, j);
    }

    private void Callee(int i, int j)
    {
        System.Console.WriteLine(i + j);
    }
}"
, @"
public class TestClass
{
    private void Caller(int i, int j)
    {
        System.Console.WriteLine(i + j);
    }

    private void Callee(int i, int j)
    {
        System.Console.WriteLine(i + j);
    }
}");

        [Fact]
        public Task TestInlineArrowExpression()
            => TestInRegularAndScript1Async(@"
public class TestClass
{
    private void Caller(int i, int j)
    {
        Ca[||]llee(i, j);
    }

    private void Callee(int i, int j)
        => System.Console.WriteLine(i + j);
}",
                // TODO: Handle the indentation correctly. 
                @"
public class TestClass
{
    private void Caller(int i, int j)
    {
System.Console.WriteLine(i + j);
    }

    private void Callee(int i, int j)
        => System.Console.WriteLine(i + j);
}");

        [Fact]
        public Task TestInlineArrowExpressionWitReturnValue()
            => TestInRegularAndScript1Async(@"
public class TestClass
{
    private void Caller(int i, int j)
    {
        var x = Ca[||]llee(i, j);
    }

    private int Callee(int i, int j)
        => i + j;
}"
, @"
public class TestClass
{
    private void Caller(int i, int j)
    {
        var x = i + j;
    }

    private int Callee(int i, int j)
        => i + j;
}");
    }
}
