// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.TaskList;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.TaskList;

[ExportLanguageService(typeof(ITaskListService), LanguageNames.CSharp), Shared]
internal class CSharpTaskListService : AbstractTaskListService
{
    private static readonly int s_multilineCommentPostfixLength = "*/".Length;
    private const string SingleLineCommentPrefix = "//";

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpTaskListService()
    {
    }

    protected override void AppendTaskListItems(
        ImmutableArray<TaskListItemDescriptor> commentDescriptors,
        SyntacticDocument document,
        SyntaxTrivia trivia,
        ArrayBuilder<TaskListItem> items)
    {
        if (PreprocessorHasComment(trivia))
        {
            var message = trivia.ToFullString();

            var index = message.IndexOf(SingleLineCommentPrefix, StringComparison.Ordinal);
            var start = trivia.FullSpan.Start + index;

            AppendTaskListItemsOnSingleLine(commentDescriptors, document, message[index..], start, items);
            return;
        }

        if (IsSingleLineComment(trivia))
        {
            ProcessMultilineComment(commentDescriptors, document, trivia, postfixLength: 0, items);
            return;
        }

        if (IsMultilineComment(trivia))
        {
            ProcessMultilineComment(commentDescriptors, document, trivia, s_multilineCommentPostfixLength, items);
            return;
        }

        throw ExceptionUtilities.Unreachable();
    }

    protected override string GetNormalizedText(string message)
        => message;

    protected override bool IsIdentifierCharacter(char ch)
        => SyntaxFacts.IsIdentifierPartCharacter(ch);

    protected override int GetCommentStartingIndex(string message)
    {
        for (var i = 0; i < message.Length; i++)
        {
            var ch = message[i];
            if (!SyntaxFacts.IsWhitespace(ch) &&
                ch != '*' && ch != '/')
            {
                return i;
            }
        }

        return message.Length;
    }

    protected override bool PreprocessorHasComment(SyntaxTrivia trivia)
    {
        return trivia.Kind() != SyntaxKind.RegionDirectiveTrivia &&
               SyntaxFacts.IsPreprocessorDirective(trivia.Kind()) && trivia.ToString().IndexOf(SingleLineCommentPrefix, StringComparison.Ordinal) > 0;
    }

    protected override bool IsSingleLineComment(SyntaxTrivia trivia)
        => trivia.IsSingleLineComment() || trivia.IsSingleLineDocComment();

    protected override bool IsMultilineComment(SyntaxTrivia trivia)
        => trivia.IsMultiLineComment() || trivia.IsMultiLineDocComment();
}
