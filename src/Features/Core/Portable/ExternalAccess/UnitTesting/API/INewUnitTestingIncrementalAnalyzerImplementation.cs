// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.SolutionCrawler;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;

internal interface INewUnitTestingIncrementalAnalyzerImplementation
{
    Task AnalyzeDocumentAsync(
        Document document,
        UnitTestingInvocationReasons reasons,
        CancellationToken cancellationToken);

    Task AnalyzeProjectAsync(
        Project project,
        UnitTestingInvocationReasons reasons,
        CancellationToken cancellationToken);

    void RemoveDocument(DocumentId documentId);
}
