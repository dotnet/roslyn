using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeFixes.ErrorCases
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ExceptionInRegisterMethod)), Shared]
    public class ExceptionInRegisterMethod : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(CodeFixServiceTests.MockFixer.Id); }
        }

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            throw new Exception($"Exception thrown in register method of {nameof(ExceptionInRegisterMethod)}");
        }
    }
}