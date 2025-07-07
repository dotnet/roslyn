// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers;

[ExportCompletionProvider(nameof(ProjectDirectiveCompletionProvider), LanguageNames.CSharp)]
[Shared]
internal sealed class ProjectDirectiveCompletionProvider : AbstractDirectivePathCompletionProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ProjectDirectiveCompletionProvider()
    {
    }

    private static ImmutableArray<char> GetCommitCharacters()
    {
        using var builder = TemporaryArray<char>.Empty;
        builder.Add('"');
        if (PathUtilities.IsUnixLikePlatform)
        {
            builder.Add('/');
        }
        else
        {
            builder.Add('/');
            builder.Add('\\');
        }

        return builder.ToImmutableAndClear();
    }

    private static readonly CompletionItemRules s_rules = CompletionItemRules.Create(
         filterCharacterRules: [],
         commitCharacterRules: [CharacterSetModificationRule.Create(CharacterSetModificationKind.Replace, GetCommitCharacters())],
         enterKeyRule: EnterKeyRule.Never,
         selectionBehavior: CompletionItemSelectionBehavior.HardSelection);

    protected override bool RequireQuotes => false;
    protected override string DirectiveName => ":project";

    protected override bool TryGetCompletionPrefix(SyntaxTree tree, int position, [NotNullWhen(true)] out string? literalValue, out TextSpan textSpan, CancellationToken cancellationToken)
    {
        if (tree.IsEntirelyWithinStringLiteral(position, cancellationToken))
        {
            var token = tree.GetRoot(cancellationToken).FindToken(position, findInsideTrivia: true);
            if (token.Kind() is SyntaxKind.EndOfDirectiveToken or SyntaxKind.EndOfFileToken)
            {
                token = token.GetPreviousToken(includeSkipped: true, includeDirectives: true);
            }

            const string tokenValuePrefix = "project ";
            if (token.Kind() == SyntaxKind.StringLiteralToken
                && token.Parent!.Kind() == SyntaxKind.IgnoredDirectiveTrivia
                && token.ToString() is var wholeValue
                && wholeValue.StartsWith(tokenValuePrefix)
                && token.SpanStart + tokenValuePrefix.Length <= position)
            {
                literalValue = wholeValue.Substring(startIndex: tokenValuePrefix.Length);
                textSpan = TextSpan.FromBounds(token.SpanStart + tokenValuePrefix.Length, token.Span.End);
                return true;
            }
        }

        literalValue = null;
        textSpan = default;
        return false;
    }

    protected override async Task ProvideCompletionsAsync(CompletionContext context, string pathThroughLastSlash)
    {
        var helper = GetFileSystemCompletionHelper(context.Document, Glyph.CSharpFile, extensions: [".csproj", ".vbproj"], s_rules);
        context.AddItems(await helper.GetItemsAsync(pathThroughLastSlash, context.CancellationToken).ConfigureAwait(false));
    }
}
