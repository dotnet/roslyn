// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.SolutionCrawler;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;

internal sealed partial class NewUnitTestingIncrementalAnalyzerProvider
{
    private sealed class NewUnitTestingIncrementalAnalyzer(INewUnitTestingIncrementalAnalyzerImplementation implementation) : IUnitTestingIncrementalAnalyzer
    {
        private readonly INewUnitTestingIncrementalAnalyzerImplementation _implementation = implementation;

        public Task AnalyzeDocumentAsync(
            Document document,
            UnitTestingInvocationReasons reasons,
            CancellationToken cancellationToken)
        {
            return _implementation.AnalyzeDocumentAsync(
                document,
                reasons,
                cancellationToken);
        }

        public Task AnalyzeProjectAsync(
            Project project,
            UnitTestingInvocationReasons reasons,
            CancellationToken cancellationToken)
        {
            return _implementation.AnalyzeProjectAsync(
                project,
                reasons,
                cancellationToken);
        }

        public async Task RemoveDocumentAsync(DocumentId documentId, CancellationToken cancellationToken)
        {
            _implementation.RemoveDocument(documentId);
        }
    }
}
