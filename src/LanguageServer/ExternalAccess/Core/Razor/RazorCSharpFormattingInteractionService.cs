// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor;

/// <summary>
/// Enables Razor to utilize Roslyn's C# formatting service.
/// </summary>
internal static class RazorCSharpFormattingInteractionService
{
    public static RazorCSharpSyntaxFormattingOptions GetRazorCSharpSyntaxFormattingOptions(SolutionServices services)
    {
        var legacyOptionsService = services.GetService<ILegacyGlobalOptionsWorkspaceService>();
        var options = legacyOptionsService?.GetSyntaxFormattingOptions(services.GetLanguageServices(LanguageNames.CSharp))
            ?? CSharpSyntaxFormattingOptions.Default;
        return new RazorCSharpSyntaxFormattingOptions((CSharpSyntaxFormattingOptions)options);
    }

    /// <summary>
    /// Returns the text changes necessary to format the document after the user enters a 
    /// character.  The position provided is the position of the caret in the document after
    /// the character been inserted into the document.
    /// </summary>
    public static async Task<ImmutableArray<TextChange>> GetFormattingChangesAsync(
        Document document,
        char typedChar,
        int position,
        RazorIndentationOptions indentationOptions,
        RazorAutoFormattingOptions autoFormattingOptions,
        FormattingOptions.IndentStyle indentStyle,
        RazorCSharpSyntaxFormattingOptions? csharpSyntaxFormattingOptionsOverride,
        CancellationToken cancellationToken)
    {
        Contract.ThrowIfFalse(document.Project.Language is LanguageNames.CSharp);
        var formattingService = document.GetRequiredLanguageService<ISyntaxFormattingService>();
        var documentSyntax = await ParsedDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        if (!formattingService.ShouldFormatOnTypedCharacter(documentSyntax, typedChar, position, cancellationToken))
        {
            return [];
        }

        var formattingOptions = GetFormattingOptions(document.Project.Solution.Services, indentationOptions, csharpSyntaxFormattingOptionsOverride);
        var roslynIndentationOptions = new IndentationOptions(formattingOptions)
        {
            AutoFormattingOptions = autoFormattingOptions.UnderlyingObject,
            IndentStyle = (FormattingOptions2.IndentStyle)indentStyle
        };

        return formattingService.GetFormattingChangesOnTypedCharacter(documentSyntax, position, roslynIndentationOptions, cancellationToken);
    }

    public static IList<TextChange> GetFormattedTextChanges(
        HostWorkspaceServices services,
        SyntaxNode root,
        TextSpan span,
        RazorIndentationOptions indentationOptions,
        RazorCSharpSyntaxFormattingOptions? csharpSyntaxFormattingOptionsOverride,
        CancellationToken cancellationToken)
    {
        Contract.ThrowIfFalse(root.Language is LanguageNames.CSharp);
        return Formatter.GetFormattedTextChanges(root, span, services.SolutionServices, GetFormattingOptions(services.SolutionServices, indentationOptions, csharpSyntaxFormattingOptionsOverride), cancellationToken);
    }

    public static SyntaxNode Format(
        HostWorkspaceServices services,
        SyntaxNode root,
        RazorIndentationOptions indentationOptions,
        RazorCSharpSyntaxFormattingOptions? csharpSyntaxFormattingOptionsOverride,
        CancellationToken cancellationToken)
    {
        Contract.ThrowIfFalse(root.Language is LanguageNames.CSharp);
        return Formatter.Format(root, services.SolutionServices, GetFormattingOptions(services.SolutionServices, indentationOptions, csharpSyntaxFormattingOptionsOverride), cancellationToken: cancellationToken);
    }

    private static SyntaxFormattingOptions GetFormattingOptions(SolutionServices services, RazorIndentationOptions indentationOptions, RazorCSharpSyntaxFormattingOptions? csharpSyntaxFormattingOptionsOverride)
    {
        var legacyOptionsService = services.GetService<ILegacyGlobalOptionsWorkspaceService>();
        var formattingOptions = csharpSyntaxFormattingOptionsOverride?.ToCSharpSyntaxFormattingOptions()
            ?? legacyOptionsService?.GetSyntaxFormattingOptions(services.GetLanguageServices(LanguageNames.CSharp))
            ?? CSharpSyntaxFormattingOptions.Default;

        return formattingOptions with
        {
            LineFormatting = formattingOptions.LineFormatting with
            {
                UseTabs = indentationOptions.UseTabs,
                TabSize = indentationOptions.TabSize,
                IndentationSize = indentationOptions.IndentationSize,
                NewLine = CSharpSyntaxFormattingOptions.Default.NewLine
            }
        };
    }
}
