// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.Remote
{
    internal enum WellKnownServiceHubService
    {
        None,
        RemoteHost,
        CodeAnalysis,
        RemoteSymbolSearchUpdateEngine,
        RemoteDesignerAttributeService,
        RemoteProjectTelemetryService,
        RemoteTodoCommentsService,
        LanguageServer,
        IntelliCode,
        Razor,

        // owned by Unit Testing team:
        UnitTestingAnalysisService,
        LiveUnitTestingBuildService,
        UnitTestingSourceLookupService,
    }
}
