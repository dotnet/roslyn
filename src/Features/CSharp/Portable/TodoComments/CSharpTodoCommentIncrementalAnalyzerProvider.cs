// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.TodoComments;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.TodoComments
{
    [ExportLanguageServiceFactory(typeof(ITodoCommentService), LanguageNames.CSharp), Shared]
    internal class CSharpTodoCommentServiceFactory : ILanguageServiceFactory
    {
        [ImportingConstructor]
        public CSharpTodoCommentServiceFactory()
        {
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
            => new CSharpTodoCommentService(languageServices.WorkspaceServices.Workspace);
    }

    internal class CSharpTodoCommentService : AbstractTodoCommentService
    {
        private static readonly int s_multilineCommentPostfixLength = "*/".Length;
        private const string SingleLineCommentPrefix = "//";

        public CSharpTodoCommentService(Workspace workspace) : base(workspace)
        {
        }

        protected override void AppendTodoComments(IList<TodoCommentDescriptor> commentDescriptors, SyntacticDocument document, SyntaxTrivia trivia, List<TodoComment> todoList)
        {
            if (PreprocessorHasComment(trivia))
            {
                var message = trivia.ToFullString();

                var index = message.IndexOf(SingleLineCommentPrefix, StringComparison.Ordinal);
                var start = trivia.FullSpan.Start + index;

                AppendTodoCommentInfoFromSingleLine(commentDescriptors, document, message.Substring(index), start, todoList);
                return;
            }

            if (IsSingleLineComment(trivia))
            {
                ProcessMultilineComment(commentDescriptors, document, trivia, postfixLength: 0, todoList: todoList);
                return;
            }

            if (IsMultilineComment(trivia))
            {
                ProcessMultilineComment(commentDescriptors, document, trivia, s_multilineCommentPostfixLength, todoList);
                return;
            }

            throw ExceptionUtilities.Unreachable;
        }

        protected override string GetNormalizedText(string message)
        {
            return message;
        }

        protected override bool IsIdentifierCharacter(char ch)
        {
            return SyntaxFacts.IsIdentifierPartCharacter(ch);
        }

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
        {
            return trivia.IsSingleLineComment() || trivia.IsSingleLineDocComment();
        }

        protected override bool IsMultilineComment(SyntaxTrivia trivia)
        {
            return trivia.IsMultiLineComment() || trivia.IsMultiLineDocComment();
        }
    }
}
