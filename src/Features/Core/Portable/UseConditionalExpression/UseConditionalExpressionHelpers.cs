// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.UseConditionalExpression
{
    internal static class UseConditionalExpressionHelpers
    {
        public static readonly SyntaxAnnotation SpecializedFormattingAnnotation = new SyntaxAnnotation();

        public static async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, 
            Func<Document, Diagnostic, SyntaxEditor, CancellationToken, Task<bool>> fixOneAsync,
            IFormattingRule multiLineFormattingRule, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var nestedEditor = new SyntaxEditor(root, document.Project.Solution.Workspace);
            var needsFormatting = false;
            foreach (var diagnostic in diagnostics)
            {
                needsFormatting |= await fixOneAsync(
                    document, diagnostic, nestedEditor, cancellationToken).ConfigureAwait(false);
            }

            var changedRoot = nestedEditor.GetChangedRoot();
            if (needsFormatting)
            {
                var rules = new List<IFormattingRule> { multiLineFormattingRule };
                rules.AddRange(Formatter.GetDefaultFormattingRules(document));

                var formattedRoot = await Formatter.FormatAsync(changedRoot,
                    SpecializedFormattingAnnotation,
                    document.Project.Solution.Workspace,
                    await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false),
                    rules, cancellationToken).ConfigureAwait(false);
                changedRoot = formattedRoot;
            }

            editor.ReplaceNode(root, changedRoot);
        }

        public static IOperation UnwrapSingleStatementBlock(IOperation statement)
            => statement is IBlockOperation block && block.Operations.Length == 1
                ? block.Operations[0]
                : statement;
    }
}
