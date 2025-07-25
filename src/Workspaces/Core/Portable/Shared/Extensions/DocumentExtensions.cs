// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class DocumentExtensions
{
    extension(Document document)
    {
        public async ValueTask<SyntaxTreeIndex> GetSyntaxTreeIndexAsync(CancellationToken cancellationToken)
        {
            var result = await SyntaxTreeIndex.GetIndexAsync(document, loadOnly: false, cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(result);
            return result;
        }

        public ValueTask<SyntaxTreeIndex?> GetSyntaxTreeIndexAsync(bool loadOnly, CancellationToken cancellationToken)
            => SyntaxTreeIndex.GetIndexAsync(document, loadOnly, cancellationToken);

        internal Document WithSolutionOptions(OptionSet options)
            => document.Project.Solution.WithOptions(options).GetRequiredDocument(document.Id);
    }
}
