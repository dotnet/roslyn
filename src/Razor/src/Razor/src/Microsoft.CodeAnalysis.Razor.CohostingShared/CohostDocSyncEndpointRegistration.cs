// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

/// <summary>
/// This class provides dynamic registration for Razor files, for LSP methods where the endpoint implementation
/// is provided by Roslyn
/// </summary>
[Export(typeof(IDynamicRegistrationProvider))]
internal sealed class CohostDocSyncEndpointRegistration : IDynamicRegistrationProvider
{
    public ImmutableArray<Registration> GetRegistrations(VSInternalClientCapabilities clientCapabilities, RazorCohostRequestContext requestContext)
    {
        return [
            // DidOpen, DidChange, DidClose, for document synchronization
            new Registration
            {
                Method = Methods.TextDocumentDidOpenName,
                RegisterOptions = new TextDocumentRegistrationOptions()
            },
            new Registration
            {
                Method = Methods.TextDocumentDidChangeName,
                RegisterOptions = new TextDocumentChangeRegistrationOptions()
                {
                    SyncKind = TextDocumentSyncKind.Incremental
                }
            },
            new Registration
            {
                Method = Methods.TextDocumentDidCloseName,
                RegisterOptions = new TextDocumentRegistrationOptions()
            },
        ];
    }
}
