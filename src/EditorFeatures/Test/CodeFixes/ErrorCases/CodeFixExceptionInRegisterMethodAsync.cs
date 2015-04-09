using System;
using System.Composition;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeFixes.ErrorCases
{
    public class ExceptionInRegisterMethodAsync : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(CodeFixServiceTests.MockFixer.Id); }
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            throw new Exception($"Exception thrown in register method of {nameof(ExceptionInRegisterMethodAsync)}");
        }
    }
}
