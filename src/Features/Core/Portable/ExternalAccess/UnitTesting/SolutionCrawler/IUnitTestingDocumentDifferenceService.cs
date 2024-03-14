// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.SolutionCrawler;

internal class UnitTestingDocumentDifferenceResult(UnitTestingInvocationReasons changeType, SyntaxNode? changedMember = null)
{
    public UnitTestingInvocationReasons ChangeType { get; } = changeType;
    public SyntaxNode? ChangedMember { get; } = changedMember;
}

internal interface IUnitTestingDocumentDifferenceService : ILanguageService
{
    UnitTestingDocumentDifferenceResult? GetDifference(Document oldDocument, Document newDocument, CancellationToken cancellationToken);
}
