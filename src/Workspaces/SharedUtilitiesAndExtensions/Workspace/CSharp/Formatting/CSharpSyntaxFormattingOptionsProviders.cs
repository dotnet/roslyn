// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Formatting;

internal static class CSharpSyntaxFormattingOptionsProviders
{
    extension(Document document)
    {
        public async ValueTask<CSharpSyntaxFormattingOptions> GetCSharpSyntaxFormattingOptionsAsync(CancellationToken cancellationToken)
        {
            var configOptions = await document.GetHostAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
            return new CSharpSyntaxFormattingOptions(configOptions);
        }
    }
}
