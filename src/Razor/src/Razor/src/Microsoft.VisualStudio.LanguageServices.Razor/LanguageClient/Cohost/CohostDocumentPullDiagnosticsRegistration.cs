// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

[Export(typeof(IDynamicRegistrationProvider))]
internal sealed class CohostDocumentPullDiagnosticsRegistration : IDynamicRegistrationProvider
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
                    Identifier = PullDiagnosticCategories.DocumentCompilerSyntax,
                    InterFileDependencies = true,
                }
            },
            new Registration()
            {
                Method = Methods.TextDocumentDiagnosticName,
                RegisterOptions = new DiagnosticRegistrationOptions()
                {
                    Identifier = PullDiagnosticCategories.Task,
                }
            }];
        }

        return [];
    }
}
