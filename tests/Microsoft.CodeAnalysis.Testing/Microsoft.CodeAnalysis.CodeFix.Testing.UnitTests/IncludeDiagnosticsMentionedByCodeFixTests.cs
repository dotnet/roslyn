// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Testing.TestFixes;
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
            /// <inheritdoc />
            public override FixAllProvider GetFixAllProvider()
            {
                return WellKnownFixAllProviders.BatchFixer;
            }

            public override Task RegisterCodeFixesAsync(CodeFixContext context)
            {
                foreach (var diagnostic in context.Diagnostics)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            nameof(SomeCodeFix),
                            cancellationToken => CreateChangedDocument(context.Document, diagnostic.Location.SourceSpan, cancellationToken),
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
                var tree = (await document.GetSyntaxTreeAsync(cancellationToken))!;
                var root = (await tree.GetRootAsync(cancellationToken))!;
                var node = root.FindNode(sourceSpan);
                var targetNode = node.AncestorsAndSelf().OfType<FieldDeclarationSyntax>().First();
                return document.WithSyntaxRoot(root.RemoveNode(targetNode, SyntaxRemoveOptions.AddElasticMarker)!);
            }

            public override ImmutableArray<string> FixableDiagnosticIds => new[] { "CS0169" }.ToImmutableArray();
        }

        /// <summary>
        /// Verifies that a test case will automatically include compiler diagnostics which are part of the the provided
        /// codefix.
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

            var diagnostic = DiagnosticResult.CompilerWarning("CS0169").WithSpan(8, 21, 8, 30).WithArguments("ConsoleApp1.TestClass.someField");
            await Verify<SomeCodeFix>.VerifyCodeFixAsync(before, diagnostic, after);
        }

        /// <summary>
        /// Verifies that a test case will automatically include compiler diagnostics which are part of the the provided
        /// codefix, and the markup will ignore the severity of the diagnostic.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        [WorkItem(419, "https://github.com/dotnet/roslyn-sdk/issues/419")]
        public async Task VerifySimpleMarkupSyntaxWorks()
        {
            var before = @"
using System;

namespace ConsoleApp1
{
    public class TestClass
    {
        private int {|CS0169:someField|};

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

            await Verify<SomeCodeFix>.VerifyCodeFixAsync(before, after);
        }
    }
}
