using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeRefactoringService.ErrorCases
{
    class ExceptionInComputeRefactoringsAsync : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            await Task.Yield();
            throw new Exception($"Exception thrown from ComputeRefactoringsAsync in {nameof(ExceptionInCodeActions)}");
        }
    }
}
