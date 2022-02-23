// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Formatting
{
    public partial class CodeCleanupTests
    {
        private abstract class TestThirdPartyCodeFix : CodeFixProvider
        {
            public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create("HasDefaultCase");

            public override Task RegisterCodeFixesAsync(CodeFixContext context)
            {
                foreach (var diagnostic in context.Diagnostics)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Remove default case",
                            async cancellationToken =>
                            {
                                var root = await context.Document.GetSyntaxRootAsync(cancellationToken);
                                var node = (await diagnostic.Location.SourceTree.GetRootAsync(cancellationToken)).FindNode(diagnostic.Location.SourceSpan);
                                return context.Document.WithSyntaxRoot(root.RemoveNode(node.Parent, SyntaxRemoveOptions.KeepNoTrivia));
                            },
                            nameof(TestThirdPartyCodeFix)),
                        diagnostic);
                }

                return Task.CompletedTask;
            }
        }

        [Shared, ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        private class TestThirdPartyCodeFixWithFixAll : TestThirdPartyCodeFix
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public TestThirdPartyCodeFixWithFixAll()
            {
            }

            public override FixAllProvider GetFixAllProvider() => BatchFixAllProvider.Instance;
        }

        [Shared, ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        private class TestThirdPartyCodeFixWithOutFixAll : TestThirdPartyCodeFix
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public TestThirdPartyCodeFixWithOutFixAll()
            {
            }
        }
    }
}
