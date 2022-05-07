// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
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
        public static async Task<ImmutableArray<TextChange>> GetFormattingChangesAsync(
            Document document,
            char typedChar,
            int position,
            DocumentOptionSet documentOptions,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(document.Project.Language is LanguageNames.CSharp);
            var formattingService = document.GetRequiredLanguageService<ISyntaxFormattingService>();

            if (!await formattingService.ShouldFormatOnTypedCharacterAsync(document, typedChar, position, cancellationToken).ConfigureAwait(false))
            {
                return ImmutableArray<TextChange>.Empty;
            }

            var services = document.Project.Solution.Workspace.Services;

            var globalOptions = document.Project.Solution.Workspace.Services.GetRequiredService<ILegacyGlobalOptionsWorkspaceService>().GlobalOptions;

            var indentationOptions = new IndentationOptions(
               SyntaxFormattingOptions.Create(documentOptions, globalOptions.GetSyntaxFormattingOptions(document.Project.LanguageServices), document.Project.LanguageServices),
               globalOptions.GetAutoFormattingOptions(document.Project.Language));

            return await formattingService.GetFormattingChangesOnTypedCharacterAsync(document, position, indentationOptions, cancellationToken).ConfigureAwait(false);
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
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(document.Project.Language is LanguageNames.CSharp);
            var formattingService = document.GetRequiredLanguageService<ISyntaxFormattingService>();

            if (!await formattingService.ShouldFormatOnTypedCharacterAsync(document, typedChar, position, cancellationToken).ConfigureAwait(false))
            {
                return ImmutableArray<TextChange>.Empty;
            }

            var formattingOptions = GetFormattingOptions(indentationOptions);
            var roslynIndentationOptions = new IndentationOptions(formattingOptions, autoFormattingOptions.UnderlyingObject, (FormattingOptions2.IndentStyle)indentStyle);

            return await formattingService.GetFormattingChangesOnTypedCharacterAsync(document, position, roslynIndentationOptions, cancellationToken).ConfigureAwait(false);
        }

        public static IList<TextChange> GetFormattedTextChanges(
            HostWorkspaceServices services,
            SyntaxNode root,
            TextSpan span,
            RazorIndentationOptions indentationOptions,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(root.Language is LanguageNames.CSharp);
            return Formatter.GetFormattedTextChanges(root, span, services, GetFormattingOptions(indentationOptions), cancellationToken);
        }

        public static SyntaxNode Format(
            HostWorkspaceServices services,
            SyntaxNode root,
            RazorIndentationOptions indentationOptions,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(root.Language is LanguageNames.CSharp);
            return Formatter.Format(root, services, GetFormattingOptions(indentationOptions), cancellationToken: cancellationToken);
        }

        private static SyntaxFormattingOptions GetFormattingOptions(RazorIndentationOptions indentationOptions)
            => CSharpSyntaxFormattingOptions.Default.With(new LineFormattingOptions(
                UseTabs: indentationOptions.UseTabs,
                TabSize: indentationOptions.TabSize,
                IndentationSize: indentationOptions.IndentationSize,
                NewLine: CSharpSyntaxFormattingOptions.Default.NewLine));
    }
}
