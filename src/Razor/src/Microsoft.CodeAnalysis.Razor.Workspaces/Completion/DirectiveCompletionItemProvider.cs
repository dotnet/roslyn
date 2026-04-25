// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal class DirectiveCompletionItemProvider : IRazorCompletionItemProvider
{
    internal static readonly ImmutableArray<RazorCommitCharacter> SingleLineDirectiveCommitCharacters = RazorCommitCharacter.CreateArray([" "]);
    internal static readonly ImmutableArray<RazorCommitCharacter> BlockDirectiveCommitCharacters = RazorCommitCharacter.CreateArray([" ", "{"]);

    // internal for testing
    internal static readonly ImmutableArray<DirectiveDescriptor> MvcDefaultDirectives = [
        CSharpCodeParser.AddTagHelperDirectiveDescriptor,
        CSharpCodeParser.RemoveTagHelperDirectiveDescriptor,
        CSharpCodeParser.TagHelperPrefixDirectiveDescriptor,
        CSharpCodeParser.UsingDirectiveDescriptor
    ];

    // internal for testing
    internal static readonly ImmutableArray<DirectiveDescriptor> ComponentDefaultDirectives = [
        CSharpCodeParser.UsingDirectiveDescriptor
    ];

    // internal for testing
    // Do not forget to update both insert and display text !important
    internal static readonly FrozenDictionary<string, (string InsertText, string DisplayText)> SingleLineDirectiveSnippets = new Dictionary<string, (string InsertText, string DisplayText)>(StringComparer.Ordinal)
    {
        ["addTagHelper"] = ("addTagHelper ${1:*}, ${2:Microsoft.AspNetCore.Mvc.TagHelpers}", "addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers"),
        ["attribute"] = ("attribute [${1:Authorize}]$0", "attribute [Authorize]"),
        ["implements"] = ("implements ${1:IDisposable}$0", "implements IDisposable"),
        ["inherits"] = ("inherits ${1:ComponentBase}$0", "inherits ComponentBase"),
        ["inject"] = ("inject ${1:IService} ${2:MyService}", "inject IService MyService"),
        ["layout"] = ("layout ${1:MainLayout}$0", "layout MainLayout"),
        ["model"] = ("model ${1:MyModelClass}$0", "model MyModelClass"),
        ["namespace"] = ("namespace ${1:MyNameSpace}$0", "namespace MyNameSpace"),
        ["page"] = ("page \"/${1:page}\"$0", "page \"/page\""),
        ["preservewhitespace"] = ("preservewhitespace ${1:true}$0", "preservewhitespace true"),
        ["removeTagHelper"] = ("removeTagHelper ${1:*}, ${2:Microsoft.AspNetCore.Mvc.TagHelpers}", "removeTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers"),
        ["tagHelperPrefix"] = ("tagHelperPrefix ${1:prefix}$0", "tagHelperPrefix prefix"),
        ["typeparam"] = ("typeparam ${1:T}$0", "typeparam T"),
        ["using"] = ("using ${1:MyNamespace}$0", "using MyNamespace")
    }
    .ToFrozenDictionary();

    public ImmutableArray<RazorCompletionItem> GetCompletionItems(RazorCompletionContext context)
    {
        return ShouldProvideCompletions(context)
            ? GetDirectiveCompletionItems(context.SyntaxTree)
            : [];
    }

    // Internal for testing
    internal static bool ShouldProvideCompletions(RazorCompletionContext context)
    {
        if (context is null)
        {
            return false;
        }

        var owner = context.Owner;
        if (owner is null)
        {
            return false;
        }

        // Do not provide IntelliSense for explicit expressions. Explicit expressions will usually look like:
        // [@] [(] [DateTime.Now] [)]
        var implicitExpression = owner.FirstAncestorOrSelf<CSharpImplicitExpressionSyntax>();
        if (implicitExpression is null)
        {
            return false;
        }

        if (implicitExpression.Width > 2 && context.Reason != CompletionReason.Invoked)
        {
            // We only want to provide directive completions if the implicit expression is empty "@|" or at the beginning of a word "@i|", this ensures
            // we're consistent with how C# typically provides completion items.
            return false;
        }

        if (owner.ChildNodesAndTokens().Any(static x => !x.AsToken(out var token) || !IsDirectiveCompletableToken(token)))
        {
            // Implicit expression contains invalid directive tokens
            return false;
        }

        if (implicitExpression.FirstAncestorOrSelf<BaseRazorDirectiveSyntax>() != null)
        {
            // Implicit expression is nested in a directive
            return false;
        }

        if (implicitExpression.FirstAncestorOrSelf<CSharpStatementSyntax>() != null)
        {
            // Implicit expression is nested in a statement
            return false;
        }

        if (implicitExpression.FirstAncestorOrSelf<MarkupElementSyntax>() != null)
        {
            // Implicit expression is nested in an HTML element
            return false;
        }

        if (implicitExpression.FirstAncestorOrSelf<MarkupTagHelperElementSyntax>() != null)
        {
            // Implicit expression is nested in a TagHelper
            return false;
        }

        return true;
    }

    // Internal for testing
    internal static ImmutableArray<RazorCompletionItem> GetDirectiveCompletionItems(RazorSyntaxTree syntaxTree)
    {
        var defaultDirectives = syntaxTree.Options.FileKind.IsComponent()
            ? ComponentDefaultDirectives
            : MvcDefaultDirectives;

        ReadOnlySpan<DirectiveDescriptor> directives = [.. syntaxTree.Options.Directives, .. defaultDirectives];

        using var completionItems = new PooledArrayBuilder<RazorCompletionItem>(capacity: directives.Length + SingleLineDirectiveSnippets.Count);

        foreach (var directive in directives)
        {
            var completionDisplayText = directive.DisplayName ?? directive.Directive;
            var commitCharacters = GetDirectiveCommitCharacters(directive.Kind);

            var completionItem = RazorCompletionItem.CreateDirective(
                displayText: completionDisplayText,
                insertText: directive.Directive,
                // Make sort text one less than display text so if there are any delegated completion items
                // with the same display text in the combined completion list, they will be sorted below
                // our items.
                sortText: completionDisplayText,
                descriptionInfo: new(directive.Description),
                commitCharacters,
                isSnippet: false);

            completionItems.Add(completionItem);

            if (SingleLineDirectiveSnippets.TryGetValue(directive.Directive, out var snippetTexts))
            {
                var snippetDescription = $"@{snippetTexts.DisplayText}{Environment.NewLine}{SR.DirectiveSnippetDescription}";

                var snippetCompletionItem = RazorCompletionItem.CreateDirective(
                    displayText: $"{completionDisplayText} {SR.Directive} ...",
                    insertText: snippetTexts.InsertText,
                    // Use the same sort text here as the directive completion item so both items are grouped together
                    sortText: completionDisplayText,
                    descriptionInfo: new(snippetDescription),
                    commitCharacters,
                    isSnippet: true);

                completionItems.Add(snippetCompletionItem);
            }
        }

        return completionItems.ToImmutableAndClear();
    }

    private static ImmutableArray<RazorCommitCharacter> GetDirectiveCommitCharacters(DirectiveKind directiveKind)
    {
        return directiveKind switch
        {
            DirectiveKind.CodeBlock or DirectiveKind.RazorBlock => BlockDirectiveCommitCharacters,
            _ => SingleLineDirectiveCommitCharacters,
        };
    }

    // Internal for testing
    internal static bool IsDirectiveCompletableToken(AspNetCore.Razor.Language.Syntax.SyntaxToken token)
    {
        return token is { Kind: SyntaxKind.Identifier or SyntaxKind.Marker or SyntaxKind.Keyword }
                     or { Kind: SyntaxKind.Transition, Parent.Kind: SyntaxKind.CSharpTransition };
    }
}
