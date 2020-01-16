// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Testing
{
    public class IncludeDiagnosticsMentionedByCodeFixTests
    {
        private class Verify<TCodeFix> : CodeFixVerifier<EmptyDiagnosticAnalyzer, TCodeFix, CSharpCodeFixTest<EmptyDiagnosticAnalyzer, TCodeFix>, DefaultVerifier>
            where TCodeFix : CodeFixProvider, new()
        {
        }

        [ExportCodeFixProvider(LanguageNames.CSharp)]
        [PartNotDiscoverable]
        internal class SomeCodeFix : CodeFixProvider
        {
            public override Task RegisterCodeFixesAsync(CodeFixContext context)
            {
                foreach (var diagnostic in context.Diagnostics)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            nameof(SomeCodeFix),
                            cancellationToken => CreateChangedDocument(context.Document, diagnostic.Location.SourceSpan,
                                cancellationToken),
                            nameof(SomeCodeFix)),
                        diagnostic);
                }

                return Task.CompletedTask;
            }

            private async Task<Document> CreateChangedDocument(
                Document document,
                TextSpan sourceSpan,
                CancellationToken cancellationToken)
            {
                var tree = await document.GetSyntaxTreeAsync(cancellationToken);
                var root = await tree.GetRootAsync(cancellationToken);
                var node = root.FindNode(sourceSpan);
                return document.WithSyntaxRoot(root.RemoveNode(node, SyntaxRemoveOptions.AddElasticMarker));
            }

            public override ImmutableArray<string> FixableDiagnosticIds => new[] { "CS0169" }.ToImmutableArray();
        }

        /// <summary>
        /// Verifies that a test case with automatically include compiler diagnostics which are part of the the provided codefix
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        [WorkItem(419, "https://github.com/dotnet/roslyn-sdk/issues/419")]
        public async Task VerifySimpleSyntaxWorks()
        {
            var before = @"
using System;

namespace ConsoleApp1
{
    public class TestClass
    {
            private int someField;

            public void SomeMethod(){}
    }
}";

            var after = @"
using System;

namespace ConsoleApp1
{
    public class TestClass
    {
            public void SomeMethod(){}
    }
}";

            await Verify<SomeCodeFix>.VerifyCodeFixAsync(before, DiagnosticResult.CompilerWarning("CS0169"), after);
        }
    }
}
