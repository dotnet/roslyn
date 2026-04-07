// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

public sealed class BatchFixAllProviderTests
{
    [Fact]
    public Task TestDefaultSelectionNestedFixers()
        => new CSharpTest([[1], [2], [3]], nested: true)
        {
            TestCode = """
            class TestClass {
              int field = [|0|];
            }
            """,
            FixedCode = $$"""
            class TestClass {
              int field = 1;
            }
            """,
        }.RunAsync();

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    private sealed class LiteralZeroAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly DiagnosticDescriptor Descriptor =
            new("LiteralZero", "title", "message", "category", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Descriptor];

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(HandleNumericLiteralExpression, SyntaxKind.NumericLiteralExpression);
        }

        private void HandleNumericLiteralExpression(SyntaxNodeAnalysisContext context)
        {
            var node = (LiteralExpressionSyntax)context.Node;
            if (node.Token.ValueText == "0")
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, node.Token.GetLocation()));
            }
        }
    }

    private sealed class ReplaceZeroFix : CodeFixProvider
    {
        private readonly ImmutableArray<int> _replacements;
        private readonly bool _nested;

        public ReplaceZeroFix(ImmutableArray<int> replacements, bool nested)
        {
            Debug.Assert(replacements.All(replacement => replacement >= 0), $"Assertion failed: {nameof(replacements)}.All(replacement => replacement >= 0)");
            _replacements = replacements;
            _nested = nested;
        }

        public override ImmutableArray<string> FixableDiagnosticIds => [LiteralZeroAnalyzer.Descriptor.Id];

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                var fixes = new List<CodeAction>();
                foreach (var replacement in _replacements)
                {
                    fixes.Add(CodeAction.Create(
                        "ThisToBase",
                        cancellationToken => CreateChangedDocument(context.Document, diagnostic.Location.SourceSpan, replacement, cancellationToken),
                        $"{nameof(ReplaceZeroFix)}_{replacement}"));
                }

                if (_nested)
                {
                    fixes = [CodeAction.Create("Container", [.. fixes], isInlinable: false)];
                }

                foreach (var fix in fixes)
                {
                    context.RegisterCodeFix(fix, diagnostic);
                }
            }
        }

        private static async Task<Document> CreateChangedDocument(Document document, TextSpan sourceSpan, int replacement, CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken);
            var root = await tree.GetRootAsync(cancellationToken);
            var token = root.FindToken(sourceSpan.Start);
            var newToken = SyntaxFactory.Literal(token.LeadingTrivia, replacement.ToString(), replacement, token.TrailingTrivia);
            return document.WithSyntaxRoot(root.ReplaceToken(token, newToken));
        }
    }

    private sealed class CSharpTest : CodeFixTest<DefaultVerifier>
    {
        private readonly ImmutableArray<ImmutableArray<int>> _replacementGroups;
        private readonly bool _nested;

        public CSharpTest(ImmutableArray<ImmutableArray<int>> replacementGroups, bool nested = false)
        {
            _replacementGroups = replacementGroups;
            _nested = nested;
        }

        public override string Language => LanguageNames.CSharp;

        public override Type SyntaxKindType => typeof(SyntaxKind);

        protected override string DefaultFileExt => "cs";

        protected override CompilationOptions CreateCompilationOptions()
        {
            return new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        }

        protected override ParseOptions CreateParseOptions()
        {
            return new CSharpParseOptions(LanguageVersion.Default, DocumentationMode.Diagnose);
        }

        protected override IEnumerable<CodeFixProvider> GetCodeFixProviders()
        {
            foreach (var replacementGroup in _replacementGroups)
            {
                yield return new ReplaceZeroFix(replacementGroup, _nested);
            }
        }

        protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
        {
            yield return new LiteralZeroAnalyzer();
        }
    }
}
