using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeRefactoringService.ErrorCases
{
    class ExceptionInCodeActions : CodeRefactoringProvider
    {
        public override Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            throw new Exception($"Exception thrown from ComputeRefactoringsAsync in {nameof(ExceptionInCodeActions)}");
        }
    }
}
