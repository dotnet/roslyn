// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Indentation;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Formatting;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Formatting.Indentation
{
    [UseExportProvider]
    public class CSharpFormatterTestsBase : CSharpFormattingEngineTestBase
    {
        private static readonly TestComposition s_composition = EditorTestCompositions.EditorFeatures.AddParts(typeof(TestFormattingRuleFactoryServiceFactory));

        public CSharpFormatterTestsBase(ITestOutputHelper output) : base(output) { }

        protected const string HtmlMarkup = """
            <html>
                <body>
                    <%{|S1:|}%>
                </body>
            </html>
            """;
        protected const int BaseIndentationOfNugget = 8;

        protected static async Task<int> GetSmartTokenFormatterIndentationWorkerAsync(
            EditorTestWorkspace workspace,
            ITextBuffer buffer,
            int indentationLine,
            char ch,
            bool useTabs)
        {
            await TokenFormatWorkerAsync(workspace, buffer, indentationLine, ch, useTabs);

            return buffer.CurrentSnapshot.GetLineFromLineNumber(indentationLine).GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(TestEditorOptions.Instance);
        }

        protected static async Task<string> TokenFormatAsync(
            EditorTestWorkspace workspace,
            ITextBuffer buffer,
            int indentationLine,
            char ch,
            bool useTabs)
        {
            await TokenFormatWorkerAsync(workspace, buffer, indentationLine, ch, useTabs);

            return buffer.CurrentSnapshot.GetText();
        }

        private static async Task TokenFormatWorkerAsync(EditorTestWorkspace workspace, ITextBuffer buffer, int indentationLine, char ch, bool useTabs)
        {
            var document = buffer.CurrentSnapshot.GetRelatedDocumentsWithChanges().First();
            var documentSyntax = await ParsedDocument.CreateAsync(document, CancellationToken.None);

            var line = documentSyntax.Text.Lines[indentationLine];

            var index = line.ToString().LastIndexOf(ch);
            Assert.InRange(index, 0, int.MaxValue);

            // get token
            var position = line.Start + index;
            var token = documentSyntax.Root.FindToken(position);

            var formattingRuleProvider = workspace.Services.GetService<IHostDependentFormattingRuleFactoryService>();

            var rules = ImmutableArray.Create(formattingRuleProvider.CreateRule(documentSyntax, position)).AddRange(Formatter.GetDefaultFormattingRules(document));

            var options = new IndentationOptions(
                new CSharpSyntaxFormattingOptions
                {
                    LineFormatting = new() { UseTabs = useTabs }
                });

            var formatter = new CSharpSmartTokenFormatter(options, rules, (CompilationUnitSyntax)documentSyntax.Root, documentSyntax.Text);
            var changes = formatter.FormatToken(token, CancellationToken.None);

            buffer.ApplyChanges(changes);
        }

        protected static async Task<int> GetSmartTokenFormatterIndentationAsync(
            string code,
            int indentationLine,
            char ch,
            bool useTabs,
            int? baseIndentation = null,
            TextSpan span = default)
        {
            // create tree service
            using var workspace = EditorTestWorkspace.CreateCSharp(code, composition: s_composition);

            if (baseIndentation.HasValue)
            {
                var factory = (TestFormattingRuleFactoryServiceFactory.Factory)workspace.Services.GetService<IHostDependentFormattingRuleFactoryService>();
                factory.BaseIndentation = baseIndentation.Value;
                factory.TextSpan = span;
            }

            var buffer = workspace.Documents.First().GetTextBuffer();
            return await GetSmartTokenFormatterIndentationWorkerAsync(workspace, buffer, indentationLine, ch, useTabs);
        }
    }
}
