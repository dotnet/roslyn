// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EncapsulateField;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class CodeAnalysisService : IRemoteEncapsulateFieldService
    {
        public Task<(DocumentId, TextChange[])[]> EncapsulateFieldsAsync(
            PinnedSolutionInfo solutionInfo,
            DocumentId documentId,
            ImmutableArray<string> fieldSymbolKeys,
            bool updateReferences,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                    var document = solution.GetRequiredDocument(documentId);

                    using var _ = ArrayBuilder<IFieldSymbol>.GetInstance(out var fields);
                    var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

                    foreach (var key in fieldSymbolKeys)
                    {
                        var resolved = SymbolKey.ResolveString(key, compilation, cancellationToken: cancellationToken).GetAnySymbol() as IFieldSymbol;
                        if (resolved == null)
                            return Array.Empty<(DocumentId, TextChange[])>();

                        fields.Add(resolved);
                    }

                    var service = document.GetLanguageService<AbstractEncapsulateFieldService>();

                    var newSolution = await service.EncapsulateFieldsAsync(
                        document, fields.ToImmutable(), updateReferences, cancellationToken).ConfigureAwait(false);
                    return await RemoteUtilities.GetDocumentTextChangesAsync(
                        solution, newSolution, cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }
    }
}
