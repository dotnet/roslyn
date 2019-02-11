﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.SmartIndent
{
    internal abstract partial class AbstractIndentationService<TSyntaxRoot>
        : ISynchronousIndentationService, IBlankLineIndentationService
        where TSyntaxRoot : SyntaxNode, ICompilationUnitSyntax
    {
        protected abstract AbstractFormattingRule GetSpecializedIndentationFormattingRule();

        private IEnumerable<AbstractFormattingRule> GetFormattingRules(Document document, int position)
        {
            var workspace = document.Project.Solution.Workspace;
            var formattingRuleFactory = workspace.Services.GetService<IHostDependentFormattingRuleFactoryService>();
            var baseIndentationRule = formattingRuleFactory.CreateRule(document, position);

            var formattingRules = new[] { baseIndentationRule, this.GetSpecializedIndentationFormattingRule() }.Concat(Formatter.GetDefaultFormattingRules(document));
            return formattingRules;
        }

        public virtual IndentationResult? GetDesiredIndentation(Document document, int lineNumber, CancellationToken cancellationToken)
        {
            var indenter = GetIndenter(document, lineNumber, cancellationToken);

            var indentStyle = indenter.OptionSet.GetOption(FormattingOptions.SmartIndent, document.Project.Language);
            if (indentStyle == FormattingOptions.IndentStyle.None)
            {
                // If there is no indent style, then do nothing.
                return null;
            }

            // There are two important cases for indentation.  The first is when we're simply
            // trying to figure out the appropriate indentation on a blank line (i.e. after
            // hitting enter at the end of a line, or after moving to a blank line).  The 
            // second is when we're trying to figure out indentation for a non-blank line
            // (i.e. after hitting enter in the middle of a line, causing tokens to move to
            // the next line).  If we're in the latter case, we defer to the Formatting engine
            // as we need it to use all its rules to determine where the appropriate location is
            // for the following tokens to go.
            if (indenter.ShouldUseFormatterIfAvailable())
            {
                return null;
            }

            return indenter.GetDesiredIndentation(indentStyle);
        }

        public IndentationResult GetBlankLineIndentation(
            Document document, int lineNumber, FormattingOptions.IndentStyle indentStyle, CancellationToken cancellationToken)
        {
            var indenter = GetIndenter(document, lineNumber, cancellationToken);
            return indenter.GetDesiredIndentation(indentStyle);
        }

        private AbstractIndenter GetIndenter(Document document, int lineNumber, CancellationToken cancellationToken)
        {
            var documentOptions = document.GetOptionsAsync(cancellationToken).WaitAndGetResult_CanCallOnBackground(cancellationToken);
            var root = document.GetSyntaxRootSynchronously(cancellationToken);

            var sourceText = root.SyntaxTree.GetText(cancellationToken);
            var lineToBeIndented = sourceText.Lines[lineNumber];

            var formattingRules = GetFormattingRules(document, lineToBeIndented.Start);

            var indenter = GetIndenter(
                document.GetLanguageService<ISyntaxFactsService>(),
                root.SyntaxTree, lineToBeIndented, formattingRules,
                documentOptions, cancellationToken);
            return indenter;
        }

        protected abstract AbstractIndenter GetIndenter(
            ISyntaxFactsService syntaxFacts, SyntaxTree syntaxTree, TextLine lineToBeIndented, IEnumerable<AbstractFormattingRule> formattingRules, OptionSet optionSet, CancellationToken cancellationToken);
    }
}
