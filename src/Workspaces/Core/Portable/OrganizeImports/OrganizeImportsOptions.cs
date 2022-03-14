// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.OrganizeImports
{
    internal readonly record struct OrganizeImportsOptions(
        bool PlaceSystemNamespaceFirst,
        bool SeparateImportDirectiveGroups,
        string NewLine)
    {
        public static async ValueTask<OrganizeImportsOptions> FromDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            return new(
                PlaceSystemNamespaceFirst: options.GetOption(GenerationOptions.PlaceSystemNamespaceFirst),
                SeparateImportDirectiveGroups: options.GetOption(GenerationOptions.SeparateImportDirectiveGroups),
                NewLine: options.GetOption(FormattingOptions2.NewLine));
        }
    }
}
