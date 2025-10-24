// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers;

/// <summary>
/// Base type for completion of "app directives" used in file-based programs.
/// See also https://github.com/dotnet/sdk/blob/main/documentation/general/dotnet-run-file.md#directives-for-project-metadata
/// Examples:
/// - '#:property LangVersion=preview'
/// - '#:project path/to/OtherProject.csproj'
/// - '#:package MyNugetPackage@Version'
/// </summary>
internal abstract class AbstractAppDirectiveCompletionProvider : LSPCompletionProvider
{
    /// <summary>The directive kind. For example, `package` in `#:package MyNugetPackage@Version`.</summary>
    /// <remarks>Term defined in feature doc: https://github.com/dotnet/sdk/blob/main/documentation/general/dotnet-run-file.md#directives-for-project-metadata</remarks>
    protected abstract string DirectiveKind { get; }

    public sealed override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
    {
        return TriggerCharacters.Contains(text[characterPosition])
            || (options.TriggerOnTypingLetters && CompletionUtilities.IsStartingNewWord(text, characterPosition));
    }

    public override ImmutableHashSet<char> TriggerCharacters { get; } = [':'];

    internal sealed override string Language => LanguageNames.CSharp;

    public override async Task ProvideCompletionsAsync(CompletionContext context)
    {
        var tree = await context.Document.GetRequiredSyntaxTreeAsync(context.CancellationToken).ConfigureAwait(false);
        if (!tree.Options.Features.ContainsKey("FileBasedProgram"))
            return;

        var token = tree.GetRoot(context.CancellationToken).FindTokenOnLeftOfPosition(context.Position, includeDirectives: true);
        if (token.Parent is not IgnoredDirectiveTriviaSyntax ignoredDirective)
            return;

        // Note that in the `#: $$` case, the whitespace is trailing trivia on the colon-token.
        if (token == ignoredDirective.ColonToken)
        {
            AddDirectiveKindCompletion(context);
        }
        else if (token == ignoredDirective.Content)
        {
            // Consider a test case like '#: pro$$ Name=Value', where we may want to offer 'property' as a completion item:
            // We know that 'token.Text == "pro Name=Value"', and, the below expressions correspond to text positions as shown:
            // #: pro Name=Value
            //    │  │
            //    │  └─context.Position
            //    └────token.SpanStart
            var textLeftOfCaret = token.Text.AsMemory(start: 0, length: context.Position - token.SpanStart);

            if (textLeftOfCaret.Span.StartsWith(DirectiveKind))
            {
                // We have a case like the following:
                // - '#: project$$'
                // - '#: project $$'
                // - '#: project  path$$'
                // - '#: project path/to/proj$$'
                var textAfterDirectiveKind = textLeftOfCaret.Slice(start: DirectiveKind.Length);
                // Ensure there is at least one space between 'project' and $$.
                var contentStartIndex = ClampStart(textAfterDirectiveKind.Span);
                if (contentStartIndex > 0)
                {
                    await AddDirectiveContentCompletionsAsync(context, textAfterDirectiveKind.Slice(start: contentStartIndex)).ConfigureAwait(false);
                }
            }
            else if (DirectiveKind.StartsWith(textLeftOfCaret.Span))
            {
                // We have a case like '#: pro$$'.
                AddDirectiveKindCompletion(context);
            }
        }

        static int ClampStart(ReadOnlySpan<char> span)
        {
            for (var i = 0; i < span.Length; i++)
            {
                if (!char.IsWhiteSpace(span[i]))
                    return i;
            }

            return span.Length;
        }
    }

    protected abstract void AddDirectiveKindCompletion(CompletionContext context);
    protected abstract Task AddDirectiveContentCompletionsAsync(CompletionContext context, ReadOnlyMemory<char> contentPrefix);
}
