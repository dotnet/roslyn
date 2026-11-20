// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Xunit;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

[Export(typeof(IIncompatibleProjectService)), PartNotDiscoverable]
internal class TestIncompatibleProjectService() : IIncompatibleProjectService
{
    public Task HandleMissingDocumentAsync(TextDocumentIdentifier? textDocumentIdentifier, RequestContext context, CancellationToken cancellationToken)
    {
        Assert.Fail($"Incorrect test setup? No TextDocument for {textDocumentIdentifier} was found");
        return Task.CompletedTask;
    }
}
