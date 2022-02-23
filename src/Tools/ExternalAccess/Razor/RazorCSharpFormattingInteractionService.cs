// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal readonly record struct RazorFormattingOptions(bool UseTabs, int TabSize, int IndentationSize);

    /// <summary>
    /// Enables Razor to utilize Roslyn's C# formatting service.
    /// </summary>
    internal static class RazorCSharpFormattingInteractionService
    {
        /// <summary>
        /// Returns the text changes necessary to format the document after the user enters a 
        /// character.  The position provided is the position of the caret in the document after
        /// the character been inserted into the document.
        /// </summary>
        [Obsolete("Use the other overload")]
        public static Task<ImmutableArray<TextChange>> GetFormattingChangesAsync(
            Document document,
            char typedChar,
            int position,
            DocumentOptionSet documentOptions,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(document.Project.Language is LanguageNames.CSharp);
            var formattingService = document.GetRequiredLanguageService<IFormattingInteractionService>();
            var services = document.Project.Solution.Workspace.Services;

            var globalOptions = document.Project.Solution.Workspace.Services.GetRequiredService<ILegacyGlobalOptionsWorkspaceService>();

            var indentationOptions = new IndentationOptions(
               SyntaxFormattingOptions.Create(documentOptions, services, document.Project.Language),
               globalOptions.GlobalOptions.GetAutoFormattingOptions(document.Project.Language));

            return formattingService.GetFormattingChangesAsync(document, typedChar, position, indentationOptions, cancellationToken);
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
            RazorFormattingOptions options,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(document.Project.Language is LanguageNames.CSharp);
            var formattingService = document.GetRequiredLanguageService<IFormattingInteractionService>();
            var formattingOptions = await SyntaxFormattingOptions.FromDocumentAsync(document, cancellationToken).ConfigureAwait(false);

            // TODO: get auto-formatting options from Razor
            var globalOptions = document.Project.Solution.Workspace.Services.GetRequiredService<ILegacyGlobalOptionsWorkspaceService>();

            var indentationOptions = new IndentationOptions(
                formattingOptions.With(options.UseTabs, options.TabSize, options.IndentationSize),
                globalOptions.GlobalOptions.GetAutoFormattingOptions(document.Project.Language));

            return await formattingService.GetFormattingChangesAsync(document, typedChar, position, indentationOptions, cancellationToken).ConfigureAwait(false);
        }
    }
}
