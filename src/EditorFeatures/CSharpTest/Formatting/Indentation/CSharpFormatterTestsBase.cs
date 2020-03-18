﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Indentation;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.UnitTests.Formatting;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Formatting.Indentation
{
    [UseExportProvider]
    public class CSharpFormatterTestsBase : CSharpFormattingEngineTestBase
    {
        protected const string HtmlMarkup = @"<html>
    <body>
        <%{|S1:|}%>
    </body>
</html>";
        protected const int BaseIndentationOfNugget = 8;

        protected static async Task<int> GetSmartTokenFormatterIndentationWorkerAsync(
            TestWorkspace workspace,
            ITextBuffer buffer,
            int indentationLine,
            char ch)
        {
            await TokenFormatWorkerAsync(workspace, buffer, indentationLine, ch);

            return buffer.CurrentSnapshot.GetLineFromLineNumber(indentationLine).GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(TestEditorOptions.Instance);
        }

        protected static async Task<string> TokenFormatAsync(
            TestWorkspace workspace,
            ITextBuffer buffer,
            int indentationLine,
            char ch)
        {
            await TokenFormatWorkerAsync(workspace, buffer, indentationLine, ch);

            return buffer.CurrentSnapshot.GetText();
        }

        private static async Task TokenFormatWorkerAsync(TestWorkspace workspace, ITextBuffer buffer, int indentationLine, char ch)
        {
            var document = buffer.CurrentSnapshot.GetRelatedDocumentsWithChanges().First();
            var root = (CompilationUnitSyntax)await document.GetSyntaxRootAsync();

            var line = root.GetText().Lines[indentationLine];

            var index = line.ToString().LastIndexOf(ch);
            Assert.InRange(index, 0, int.MaxValue);

            // get token
            var position = line.Start + index;
            var token = root.FindToken(position);

            var formattingRuleProvider = workspace.Services.GetService<IHostDependentFormattingRuleFactoryService>();

            var rules = formattingRuleProvider.CreateRule(document, position).Concat(Formatter.GetDefaultFormattingRules(document));

            var documentOptions = await document.GetOptionsAsync();
            var formatter = new CSharpSmartTokenFormatter(documentOptions, rules, root);
            var changes = await formatter.FormatTokenAsync(workspace, token, CancellationToken.None);

            ApplyChanges(buffer, changes);
        }

        private static void ApplyChanges(ITextBuffer buffer, IList<TextChange> changes)
        {
            using var edit = buffer.CreateEdit();
            foreach (var change in changes)
            {
                edit.Replace(change.Span.ToSpan(), change.NewText);
            }

            edit.Apply();
        }

        protected async Task<int> GetSmartTokenFormatterIndentationAsync(
            string code,
            int indentationLine,
            char ch,
            bool useTabs,
            int? baseIndentation = null,
            TextSpan span = default)
        {
            // create tree service
            using var workspace = TestWorkspace.CreateCSharp(code);
            workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(workspace.Options
                .WithChangedOption(FormattingOptions.UseTabs, LanguageNames.CSharp, useTabs)));

            if (baseIndentation.HasValue)
            {
                var factory = workspace.Services.GetService<IHostDependentFormattingRuleFactoryService>()
                            as TestFormattingRuleFactoryServiceFactory.Factory;

                factory.BaseIndentation = baseIndentation.Value;
                factory.TextSpan = span;
            }

            var buffer = workspace.Documents.First().GetTextBuffer();
            return await GetSmartTokenFormatterIndentationWorkerAsync(workspace, buffer, indentationLine, ch);
        }
    }
}
