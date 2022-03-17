// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Indentation
{
    internal readonly record struct IndentationOptions(
        SyntaxFormattingOptions FormattingOptions,
        AutoFormattingOptions AutoFormattingOptions)
    {
        public static async Task<IndentationOptions> FromDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            return From(documentOptions, document.Project.Solution.Workspace.Services, document.Project.Language);
        }

        public static IndentationOptions From(OptionSet options, HostWorkspaceServices services, string language)
            => new(
                SyntaxFormattingOptions.Create(options, services, language),
                AutoFormattingOptions.From(options, language));
    }
}
