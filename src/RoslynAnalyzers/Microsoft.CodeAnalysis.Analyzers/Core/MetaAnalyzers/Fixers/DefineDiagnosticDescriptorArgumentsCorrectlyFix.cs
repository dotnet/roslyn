// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic), Shared]
    [method: ImportingConstructor]
    [method: Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
    public sealed partial class DefineDiagnosticDescriptorArgumentsCorrectlyFix() : CodeFixProvider
    {
        private const string SourceDocumentEquivalenceKeySuffix = nameof(SourceDocumentEquivalenceKeySuffix);
        private const string AdditionalDocumentEquivalenceKeySuffix = nameof(AdditionalDocumentEquivalenceKeySuffix);

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            [
                DiagnosticIds.DefineDiagnosticTitleCorrectlyRuleId,
                DiagnosticIds.DefineDiagnosticMessageCorrectlyRuleId,
                DiagnosticIds.DefineDiagnosticDescriptionCorrectlyRuleId,
            ];

        public override FixAllProvider GetFixAllProvider() => CustomFixAllProvider.Instance;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetRequiredSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(context.Span, getInnermostNodeForTie: true);
            if (node is null)
            {
                return;
            }

            var additionalDocuments = context.Document.Project.AdditionalDocuments.ToImmutableArray();
            var semanticModel = await context.Document.GetRequiredSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                if (!TryGetFixInfo(diagnostic, root, semanticModel, additionalDocuments,
                        context.CancellationToken, out var fixInfo))
                {
                    continue;
                }

                var codeFixTitle = diagnostic.Id switch
                {
                    DiagnosticIds.DefineDiagnosticTitleCorrectlyRuleId => CodeAnalysisDiagnosticsResources.DefineDiagnosticTitleCorrectlyTitle,
                    DiagnosticIds.DefineDiagnosticMessageCorrectlyRuleId => CodeAnalysisDiagnosticsResources.DefineDiagnosticMessageCorrectlyTitle,
                    DiagnosticIds.DefineDiagnosticDescriptionCorrectlyRuleId => CodeAnalysisDiagnosticsResources.DefineDiagnosticDescriptionCorrectlyTitle,
                    _ => throw new InvalidOperationException()
                };

                var equivalenceKeySuffix = fixInfo.Value.AdditionalDocumentToFix != null ? AdditionalDocumentEquivalenceKeySuffix : SourceDocumentEquivalenceKeySuffix;
                var equivalenceKey = codeFixTitle + equivalenceKeySuffix;

                var codeAction = CodeAction.Create(
                   codeFixTitle,
                   ct => ApplyFixAsync(context.Document, root, fixInfo.Value, ct),
                   equivalenceKey);
                context.RegisterCodeFix(codeAction, diagnostic);
            }
        }

#pragma warning disable CA1815 // Override equals and operator equals on value types
        private readonly struct FixInfo
#pragma warning restore CA1815 // Override equals and operator equals on value types
        {
            public string FixValue { get; }

            public FixInfo(string fixValue, ILiteralOperation sourceLiteralAtLocationToFix)
            {
                FixValue = fixValue;
                SourceLiteralAtLocationToFix = sourceLiteralAtLocationToFix;
                AdditionalDocumentToFix = null;
                AdditionalDocumentSpanToFix = null;
            }

            public FixInfo(string fixValue, TextDocument additionalDocumentToFix, TextSpan additionalDocumentSpanToFix)
            {
                FixValue = fixValue;
                AdditionalDocumentToFix = additionalDocumentToFix;
                AdditionalDocumentSpanToFix = additionalDocumentSpanToFix;
                SourceLiteralAtLocationToFix = null;
            }

            public ILiteralOperation? SourceLiteralAtLocationToFix { get; }
            public TextDocument? AdditionalDocumentToFix { get; }
            public TextSpan? AdditionalDocumentSpanToFix { get; }
        }

        private static bool TryGetFixInfo(
            Diagnostic diagnostic,
            SyntaxNode root,
            SemanticModel model,
            ImmutableArray<TextDocument> additionalDocuments,
            CancellationToken cancellationToken,
            [NotNullWhen(returnValue: true)] out FixInfo? fixInfo)
        {
            fixInfo = null;

            if (!TryGetFixValue(diagnostic, out var fixValue))
            {
                return false;
            }

            if (diagnostic.AdditionalLocations.Count == 1)
            {
                var locationToFix = diagnostic.AdditionalLocations[0];
                if (locationToFix.IsInSource &&
                    root.FindNode(locationToFix.SourceSpan, getInnermostNodeForTie: true) is { } fixNode &&
                    model.GetOperation(fixNode, cancellationToken) is ILiteralOperation literal &&
                    literal.ConstantValue.HasValue &&
                    literal.ConstantValue.Value is string)
                {
                    fixInfo = new FixInfo(fixValue, literal);
                    return true;
                }

                return false;
            }

            return TryGetAdditionalDocumentFixInfo(diagnostic, fixValue, additionalDocuments, out fixInfo);
        }

        private static bool TryGetFixValue(Diagnostic diagnostic, [NotNullWhen(returnValue: true)] out string? fixValue)
            => diagnostic.Properties.TryGetValue(DiagnosticDescriptorCreationAnalyzer.DefineDescriptorArgumentCorrectlyFixValue, out fixValue) &&
               !string.IsNullOrEmpty(fixValue);

        private static bool TryGetAdditionalDocumentFixInfo(
            Diagnostic diagnostic,
            string fixValue,
            ImmutableArray<TextDocument> additionalDocuments,
            [NotNullWhen(returnValue: true)] out FixInfo? fixInfo)
        {
            if (DiagnosticDescriptorCreationAnalyzer.TryGetAdditionalDocumentLocationInfo(diagnostic, out var path, out var fixSpan) &&
                additionalDocuments.FirstOrDefault(a => string.Equals(a.FilePath, path, StringComparison.Ordinal)) is { } additionalDocument)
            {
                fixInfo = new FixInfo(fixValue, additionalDocument, fixSpan.Value);
                return true;
            }

            fixInfo = null;
            return false;
        }

        private static async Task<Solution> ApplyFixAsync(Document document, SyntaxNode root, FixInfo fixInfo, CancellationToken cancellationToken)
        {
            if (fixInfo.SourceLiteralAtLocationToFix is { } literal)
            {
                RoslynDebug.Assert(literal.ConstantValue.HasValue && literal.ConstantValue.Value is string);

                var generator = SyntaxGenerator.GetGenerator(document);
                var newLiteral = generator.LiteralExpression(fixInfo.FixValue).WithTriviaFrom(literal.Syntax);
                var newRoot = root.ReplaceNode(literal.Syntax, newLiteral);
                return document.WithSyntaxRoot(newRoot).Project.Solution;
            }
            else
            {
                RoslynDebug.Assert(fixInfo.AdditionalDocumentToFix != null);
                RoslynDebug.Assert(fixInfo.AdditionalDocumentSpanToFix != null);

                var text = await fixInfo.AdditionalDocumentToFix.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var textChange = new TextChange(fixInfo.AdditionalDocumentSpanToFix.Value, fixInfo.FixValue);
                var newText = text.WithChanges(textChange);
                return document.Project.Solution.WithAdditionalDocumentText(fixInfo.AdditionalDocumentToFix.Id, newText);
            }
        }
    }
}
