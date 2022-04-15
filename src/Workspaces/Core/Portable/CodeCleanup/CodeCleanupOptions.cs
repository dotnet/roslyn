// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.AddImport;

namespace Microsoft.CodeAnalysis.CodeCleanup
{
    [DataContract]
    internal readonly record struct CodeCleanupOptions(
        [property: DataMember(Order = 0)] SyntaxFormattingOptions FormattingOptions,
        [property: DataMember(Order = 1)] SimplifierOptions SimplifierOptions,
        [property: DataMember(Order = 2)] AddImportPlacementOptions AddImportOptions)
    {
        public static async ValueTask<CodeCleanupOptions> FromDocumentAsync(Document document, CodeCleanupOptions? fallbackOptions, CancellationToken cancellationToken)
        {
            var formattingOptions = await SyntaxFormattingOptions.FromDocumentAsync(document, cancellationToken).ConfigureAwait(false);
            var simplifierOptions = await SimplifierOptions.FromDocumentAsync(document, fallbackOptions?.SimplifierOptions, cancellationToken).ConfigureAwait(false);
            var addImportOptions = await AddImportPlacementOptions.FromDocumentAsync(document, cancellationToken).ConfigureAwait(false);
            return new CodeCleanupOptions(formattingOptions, simplifierOptions, addImportOptions);
        }
    }
}
