// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Xunit;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

[Export(typeof(IIncompatibleProjectService)), PartNotDiscoverable]
internal class TestIncompatibleProjectService() : IIncompatibleProjectService
{
    public void HandleMissingDocument(RazorTextDocumentIdentifier? textDocumentIdentifier, RazorCohostRequestContext context)
    {
        Assert.Fail($"Incorrect test setup? No TextDocument for {textDocumentIdentifier} was found");
    }
}
