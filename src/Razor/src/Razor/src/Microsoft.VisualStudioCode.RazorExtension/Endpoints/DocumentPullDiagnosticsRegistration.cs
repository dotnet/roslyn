// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.LanguageServer.Handler;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

[Export(typeof(IDynamicRegistrationProvider))]
internal sealed class DocumentPullDiagnosticsRegistration : IDynamicRegistrationProvider
{
    public ImmutableArray<Registration> GetRegistrations(VSInternalClientCapabilities clientCapabilities, RequestContext requestContext)
    {
        if (clientCapabilities.TextDocument?.Diagnostic?.DynamicRegistration is true)
        {
            return [new Registration()
            {
                Method = Methods.TextDocumentDiagnosticName,
                RegisterOptions = new DiagnosticRegistrationOptions()
                {
                    InterFileDependencies = true,
                    WorkspaceDiagnostics = false
                }
            }];
        }

        return [];
    }
}
