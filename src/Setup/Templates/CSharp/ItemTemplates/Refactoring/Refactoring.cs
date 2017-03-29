using System;
using System.Composition;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace $rootnamespace$
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof($safeitemname$)), Shared]
    internal class $safeitemname$ : CodeRefactoringProvider
    {
        public sealed override Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            throw new NotImplementedException();
        }
    }
}