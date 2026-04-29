// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

/// <summary>
/// This class provides dynamic registration for Razor files, for LSP methods where the endpoint implementation
/// is provided by Roslyn, and are VS specific.
/// </summary>
#pragma warning disable RS0030 // Do not use banned APIs
[Shared]
[Export(typeof(IDynamicRegistrationProvider))]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class CohostVSEndpointRegistration : IDynamicRegistrationProvider
{
    public ImmutableArray<Registration> GetRegistrations(VSInternalClientCapabilities clientCapabilities, RazorCohostRequestContext requestContext)
    {
        return [
            // Project Context, for the nav bar
            new Registration
            {
                Method = VSMethods.GetProjectContextsName,
                RegisterOptions = new TextDocumentRegistrationOptions()
            },
        ];
    }
}
